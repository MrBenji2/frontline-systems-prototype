using System;
using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Combat;
using Frontline.Definitions;
using Frontline.Economy;
using Frontline.Gameplay;
using Frontline.Harvesting;
using Frontline.Loot;
using Frontline.Tactical;
using Frontline.UI;
using Frontline.World;
using UnityEngine;

namespace Frontline.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Health))]
    public sealed class TransportTruckController : MonoBehaviour
    {
        private const string TruckDefinitionId = "transport_truck";

        [Header("Interact")]
        [SerializeField] private float enterExitDistance = 2.0f;

        [Header("Drive Tuning")]
        [SerializeField] private float accel = 22f;
        [SerializeField] private float maxSpeed = 12f;
        [SerializeField] private float maxReverseSpeed = 6.5f;
        [SerializeField] private float turnRateDegreesPerSecond = 90f;
        [SerializeField] private float forwardDrag = 0.6f;
        [SerializeField] private float lateralDamping = 2.5f;

        [Header("Drive Feel (6.2)")]
        [SerializeField] private float throttleResponse = 3.5f;
        [SerializeField] private float steerResponse = 6.0f;
        [SerializeField] private float brakeResponse = 10.0f;
        [Tooltip("Low speed accel multiplier; tapers toward 1.0 as speed approaches cap.")]
        [SerializeField] private float lowSpeedAccelMultiplier = 1.35f;
        [Tooltip("Extra braking force when braking (W against reverse / S against forward).")]
        [SerializeField] private float brakeStrength = 28f;
        [SerializeField] private float engineDrag = 1.1f;

        [Header("Steering (6.2)")]
        [Tooltip("Steer scale at ~0 speed.")]
        [SerializeField] private float lowSpeedSteerMultiplier = 1.25f;
        [Tooltip("Steer scale near top speed.")]
        [SerializeField] private float highSpeedSteerMultiplier = 0.45f;
        [Tooltip("How strongly velocity is nudged toward facing (0..1-ish). Disabled while handbraking.")]
        [SerializeField] private float steeringAssist = 1.25f;

        [Header("Grip / Handbrake (6.2)")]
        [SerializeField] private float normalGrip = 4.0f;
        [SerializeField] private float handbrakeGrip = 1.3f;
        [SerializeField] private float handbrakeBrakeStrength = 40f;
        [SerializeField] private float gripReturnSpeed = 6.0f;
        [SerializeField] private float angularDamping = 6.0f;
        [SerializeField] private float fullStopSpeed = 0.18f;

        [Header("HP")]
        [SerializeField] private int maxHp = 250;

        [Header("Storage")]
        [SerializeField] private int maxSlots = 12;
        [SerializeField] private int maxTotalCount = 80;

        [Header("Anchors")]
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform exitPoint;

        private readonly Dictionary<string, int> _items = new(StringComparer.Ordinal);

        private Rigidbody _rb;
        private Health _health;
        private bool _registeredContents;

        private TacticalPlayerController _driver;
        private CharacterController _driverCc;
        private PlayerCombatVitals _driverVitals;
        private readonly List<Behaviour> _disabledWhileDriving = new();
        private Renderer[] _driverRenderers;

        // Drive state
        private float _throttleInput;
        private float _steerInput;
        private float _brakeInput;
        private float _targetGrip;
        private float _currentGrip;
        private bool _handbrake;
        private bool _inputsLocked;

        public bool IsOccupied => _driver != null;

        public float EnterExitDistance => enterExitDistance;
        public int MaxHp => _health != null ? _health.MaxHp : maxHp;
        public int CurrentHp => _health != null ? _health.CurrentHp : maxHp;

        public int MaxSlots => maxSlots;
        public int MaxTotalCount => maxTotalCount;
        public IReadOnlyDictionary<string, int> Items => _items;
        public int SlotsUsed => _items.Count;
        public int TotalCount => _items.Values.Sum();

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _health = GetComponent<Health>();

            // Ensure health is configured as "vehicle-ish".
            _health.Configure(maxHp, destroyOnDeath: true);
            _health.Died += OnDied;

            EnsureAnchorsExist();
            EnsureStablePhysicsDefaults();
            EnsureMinimalVisualsExist();

            // Ensure DestroyedPool wiring exists and uses the correct definition ID.
            var destructible = GetComponent<Destructible>();
            if (destructible == null)
                destructible = gameObject.AddComponent<Destructible>();
            destructible.SetDefinitionId(TruckDefinitionId);

            _targetGrip = Mathf.Max(0.01f, normalGrip);
            _currentGrip = _targetGrip;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;

            // Safety: never leave player stuck parented/disabled if this truck is destroyed externally.
            if (IsOccupied)
                ForceExitAndClear(stopTruck: true);
        }

        private void Update()
        {
            // If a gameplay modal is open, don't process enter/exit/storage/driving inputs.
            _inputsLocked = UiModalManager.Instance != null && UiModalManager.Instance.HasOpenModal;
            if (_inputsLocked)
                return;

            var player = FindDriverCandidate();
            if (!IsOccupied)
            {
                if (player != null && IsPlayerInRange(player.transform, enterExitDistance) && Input.GetKeyDown(KeyCode.E))
                    Enter(player);
            }
            else
            {
                // Safety: if driver is dead or disabled, force-exit and stop the truck.
                if (_driver == null || !_driver.gameObject.activeInHierarchy || (_driverVitals != null && _driverVitals.IsDead))
                {
                    ForceExitAndClear(stopTruck: true);
                    return;
                }

                // Keep driver pinned to seat (camera continues to track player).
                if (seatAnchor != null)
                    _driver.transform.SetPositionAndRotation(seatAnchor.position, seatAnchor.rotation);

                if (Input.GetKeyDown(KeyCode.E))
                    Exit();
            }

            // Storage (F): near truck or while inside.
            if (Input.GetKeyDown(KeyCode.F))
            {
                var canInteract =
                    (IsOccupied) ||
                    (player != null && IsPlayerInRange(player.transform, enterExitDistance));

                if (canInteract && TransportTruckPanel.Instance != null)
                {
                    if (TransportTruckPanel.Instance.IsOpen)
                        TransportTruckPanel.Instance.Close();
                    else
                        TransportTruckPanel.Instance.Open(this);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsOccupied)
                return;
            if (_rb == null)
                return;

            // Modal lock: ignore inputs (smoothly return to neutral).
            var rawV = _inputsLocked ? 0f : Input.GetAxisRaw("Vertical");
            var rawH = _inputsLocked ? 0f : Input.GetAxisRaw("Horizontal");
            _handbrake = !_inputsLocked && Input.GetKey(KeyCode.Space);

            var dt = Time.fixedDeltaTime;
            var forward = transform.forward;
            var right = transform.right;

            var v = _rb.velocity;
            var planar = new Vector3(v.x, 0f, v.z);
            var speed = planar.magnitude;
            var forwardSpeed = Vector3.Dot(planar, forward);

            // Part D: braking vs reverse logic (heavy truck hybrid).
            var wantsForward = rawV > 0.01f;
            var wantsReverse = rawV < -0.01f;
            var isMovingForward = forwardSpeed > 0.2f;
            var isMovingReverse = forwardSpeed < -0.2f;
            var braking = (wantsReverse && isMovingForward) || (wantsForward && isMovingReverse);

            var throttleTarget = braking ? 0f : Mathf.Clamp(rawV, -1f, 1f);
            var brakeTarget = 0f;
            if (braking)
                brakeTarget = Mathf.Clamp01(Mathf.Abs(rawV));
            if (_handbrake)
                brakeTarget = 1f;

            // Part A: input smoothing.
            _throttleInput = Mathf.MoveTowards(_throttleInput, throttleTarget, Mathf.Max(0.01f, throttleResponse) * dt);
            _steerInput = Mathf.MoveTowards(_steerInput, Mathf.Clamp(rawH, -1f, 1f), Mathf.Max(0.01f, steerResponse) * dt);
            _brakeInput = Mathf.MoveTowards(_brakeInput, brakeTarget, Mathf.Max(0.01f, brakeResponse) * dt);

            // Grip blend (handbrake reduces grip, then returns quickly).
            _targetGrip = _handbrake ? Mathf.Max(0.01f, handbrakeGrip) : Mathf.Max(0.01f, normalGrip);
            _currentGrip = Mathf.MoveTowards(_currentGrip, _targetGrip, Mathf.Max(0.01f, gripReturnSpeed) * dt);

            // Part A: acceleration curve (punchy low speed, taper high speed).
            var maxThisDir = _throttleInput >= 0f ? Mathf.Max(0.1f, maxSpeed) : Mathf.Max(0.1f, maxReverseSpeed);
            var speedFactor = Mathf.Clamp01(speed / maxThisDir);
            var accelMul = Mathf.Lerp(Mathf.Max(0.5f, lowSpeedAccelMultiplier), 1.0f, speedFactor * speedFactor);

            // Apply engine acceleration along facing direction.
            if (Mathf.Abs(_throttleInput) > 0.001f && !_handbrake)
            {
                var engineForce = forward * (_throttleInput * accel * accelMul);
                _rb.AddForce(engineForce, ForceMode.Acceleration);
            }

            // Part D: braking (also used by handbrake).
            if (_brakeInput > 0.001f && planar.sqrMagnitude > 0.0001f)
            {
                var brake = Mathf.Lerp(brakeStrength, handbrakeBrakeStrength, _handbrake ? 1f : 0f);
                _rb.AddForce(-planar.normalized * (brake * _brakeInput), ForceMode.Acceleration);
            }

            // Part C: lateral grip / anti-slide (Foxhole-like weight).
            // Dampen sideways velocity component with grip (handbrake lowers it).
            v = _rb.velocity;
            planar = new Vector3(v.x, 0f, v.z);
            var sideways = Vector3.Dot(planar, right);
            _rb.AddForce(-right * (sideways * _currentGrip), ForceMode.Acceleration);

            // Engine drag (settles smoothly when you let go).
            var fwdVel = Vector3.Dot(planar, forward);
            _rb.AddForce(-forward * (fwdVel * engineDrag), ForceMode.Acceleration);

            // Part B: speed-correct steering (scale with speed, influenced by velocity direction).
            v = _rb.velocity;
            planar = new Vector3(v.x, 0f, v.z);
            speed = planar.magnitude;
            var speedFactorForSteer = Mathf.Clamp01(speed / Mathf.Max(0.01f, maxSpeed));
            var steerScale = Mathf.Lerp(lowSpeedSteerMultiplier, highSpeedSteerMultiplier, speedFactorForSteer);

            // Use velocity direction when moving, otherwise facing direction (prevents "sideways steering weirdness").
            var velDir = planar.sqrMagnitude > 0.01f ? planar.normalized : forward;
            var velForwardDot = Vector3.Dot(velDir, forward);
            var movingMostlyForward = velForwardDot >= 0f;

            // Invert steering while reversing for intuitive controls.
            var steerSign = movingMostlyForward ? 1f : -1f;
            var yaw = _steerInput * steerSign * turnRateDegreesPerSecond * steerScale * dt;
            if (Mathf.Abs(yaw) > 0.0001f)
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, yaw, 0f));

            // Steering assist: gently align velocity toward facing when not handbraking.
            if (!_handbrake && steeringAssist > 0.001f && planar.sqrMagnitude > 0.05f)
            {
                var desiredDir = forward * (movingMostlyForward ? 1f : -1f);
                var aligned = Vector3.Lerp(planar.normalized, desiredDir.normalized, Mathf.Clamp01(steeringAssist * dt));
                var newPlanar = aligned.normalized * planar.magnitude;
                _rb.velocity = new Vector3(newPlanar.x, v.y, newPlanar.z);
            }

            // Part C: angular damping (prevents endless rotation).
            var av = _rb.angularVelocity;
            _rb.AddTorque(-av * Mathf.Max(0f, angularDamping), ForceMode.Acceleration);

            // Part E: speed caps (separate forward/reverse).
            v = _rb.velocity;
            planar = new Vector3(v.x, 0f, v.z);
            forwardSpeed = Vector3.Dot(planar, forward);
            var cap = forwardSpeed >= 0f ? Mathf.Max(0.1f, maxSpeed) : Mathf.Max(0.1f, maxReverseSpeed);
            if (planar.magnitude > cap)
            {
                planar = planar.normalized * cap;
                _rb.velocity = new Vector3(planar.x, v.y, planar.z);
            }

            // Part D: full stop clamp.
            if (!_handbrake && Mathf.Abs(_throttleInput) < 0.01f && _brakeInput < 0.01f)
            {
                v = _rb.velocity;
                planar = new Vector3(v.x, 0f, v.z);
                if (planar.magnitude <= Mathf.Max(0.01f, fullStopSpeed))
                {
                    _rb.velocity = new Vector3(0f, v.y, 0f);
                    _rb.angularVelocity = new Vector3(0f, _rb.angularVelocity.y * 0.2f, 0f);
                }
            }
        }

        private void OnGUI()
        {
            if (!IsOccupied || _rb == null)
                return;

            // Part G: debug tuning readout while driving.
            var v = _rb.velocity;
            var speed = new Vector3(v.x, 0f, v.z).magnitude;
            var rect = new Rect(10, 10, 320, 68);
            GUI.Box(rect, "");
            GUI.Label(
                new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12),
                $"Truck Debug\n" +
                $"Speed: {speed:0.00} m/s\n" +
                $"Throttle: {_throttleInput:0.00} | Steer: {_steerInput:0.00} | Handbrake: {(_handbrake ? "ON" : "off")}");
        }

        public bool CanAdd(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            if (count <= 0)
                return false;
            if (TotalCount + count > maxTotalCount)
                return false;
            if (_items.ContainsKey(itemId))
                return true;
            return SlotsUsed + 1 <= maxSlots;
        }

        public bool TryAdd(string itemId, int count)
        {
            if (!CanAdd(itemId, count))
                return false;

            _items[itemId] = GetCount(itemId) + count;
            return true;
        }

        public bool TryRemove(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            if (count <= 0)
                return false;

            var have = GetCount(itemId);
            if (have < count)
                return false;

            var next = have - count;
            if (next <= 0)
                _items.Remove(itemId);
            else
                _items[itemId] = next;
            return true;
        }

        public int GetCount(string itemId)
        {
            return _items.TryGetValue(itemId, out var c) ? c : 0;
        }

        public void LoadFromSnapshot(IEnumerable<TransportTruckWorldSnapshot.ItemStack> stacks)
        {
            _items.Clear();
            if (stacks == null)
                return;

            foreach (var s in stacks)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.itemId) || s.count <= 0)
                    continue;
                if (_items.ContainsKey(s.itemId))
                    _items[s.itemId] += s.count;
                else
                    _items[s.itemId] = s.count;
            }
        }

        public List<TransportTruckWorldSnapshot.ItemStack> ToSnapshot()
        {
            return _items
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new TransportTruckWorldSnapshot.ItemStack { itemId = kv.Key, count = kv.Value })
                .ToList();
        }

        public void SetCurrentHpForLoad(int currentHp)
        {
            if (_health == null)
                return;
            _health.SetCurrentHpForLoad(currentHp);
        }

        public bool TryRepairHammerTick(int hpPerTick, float range, Transform player)
        {
            if (_health == null || _health.IsDead)
                return false;
            if (player == null)
                return false;
            if (PlayerInventoryService.Instance == null)
                return false;

            var missing = _health.MaxHp - _health.CurrentHp;
            if (missing <= 0)
                return false;

            var p = player.position;
            p.y = 0f;
            var t = transform.position;
            t.y = 0f;
            if (Vector3.Distance(p, t) > range)
                return false;

            var hp = Mathf.Clamp(hpPerTick, 1, missing);
            var primary = GetPrimaryRepairResource();
            if (string.IsNullOrWhiteSpace(primary))
                return false;

            if (!PlayerInventoryService.Instance.TryRemoveResource(primary, 1))
                return false;

            _health.Restore(hp);
            return true;
        }

        private string GetPrimaryRepairResource()
        {
            // Prefer the first craftCost listed in vehicle definitions.
            if (DefinitionRegistry.Instance != null)
            {
                var def = DefinitionRegistry.Instance.Definitions.vehicles.FirstOrDefault(v => v != null && v.id == TruckDefinitionId);
                if (def != null && def.craftCosts != null && def.craftCosts.Count > 0)
                {
                    var first = def.craftCosts[0];
                    if (first != null && !string.IsNullOrWhiteSpace(first.materialId))
                        return first.materialId;
                }
            }

            // Fallback
            return ToolRecipes.Wood;
        }

        private void OnDied(Health h)
        {
            // Part A.1: destroyed while occupied -> force-exit before object is destroyed.
            if (IsOccupied)
                ForceExitAndClear(stopTruck: true);

            if (_registeredContents)
                return;
            _registeredContents = true;

            // On truck destruction, all storage contents are destroyed into the pool (no world spill).
            if (DestroyedPoolService.Instance != null)
            {
                foreach (var kv in _items)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value <= 0)
                        continue;
                    DestroyedPoolService.Instance.RegisterDestroyed(kv.Key, kv.Value);
                }
            }

            _items.Clear();
        }

        /// <summary>
        /// Part B: anti-dupe removal path for deleting a truck outside normal damage death.
        /// Dumps storage into DestroyedPool (once) and clears inventory.
        /// </summary>
        public void DumpContentsToDestroyedPoolAndClear()
        {
            if (_registeredContents)
                return;
            _registeredContents = true;

            if (DestroyedPoolService.Instance != null)
            {
                foreach (var kv in _items)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value <= 0)
                        continue;
                    DestroyedPoolService.Instance.RegisterDestroyed(kv.Key, kv.Value);
                }
            }

            _items.Clear();
        }

        public void DebugDamage(int amount)
        {
            if (_health == null || _health.IsDead)
                return;
            _health.ApplyDamage(Mathf.Max(0, amount), gameObject);
        }

        public void DebugRepair(int amount)
        {
            if (_health == null || _health.IsDead)
                return;
            _health.Restore(Mathf.Max(0, amount));
        }

        /// <summary>
        /// Part A.3: load sanity - force truck to unoccupied state.
        /// </summary>
        public void ForceUnoccupiedForLoad()
        {
            if (IsOccupied)
                ForceExitAndClear(stopTruck: true);
        }

        private TacticalPlayerController FindDriverCandidate()
        {
            // If already driving, this is the driver.
            if (_driver != null)
                return _driver;
            return FindFirstObjectByType<TacticalPlayerController>();
        }

        private bool IsPlayerInRange(Transform player, float range)
        {
            if (player == null)
                return false;
            var p = player.position;
            p.y = 0f;
            var t = transform.position;
            t.y = 0f;
            return Vector3.Distance(p, t) <= range;
        }

        private void Enter(TacticalPlayerController player)
        {
            if (player == null)
                return;
            if (IsOccupied)
                return;

            _driver = player;
            _driverCc = player.GetComponent<CharacterController>();
            _driverVitals = player.GetComponent<PlayerCombatVitals>();
            _driverRenderers = player.GetComponentsInChildren<Renderer>(true);

            _disabledWhileDriving.Clear();
            foreach (var b in player.GetComponents<Behaviour>())
            {
                if (b == null)
                    continue;
                if (!b.enabled)
                    continue;
                if (b is TransportTruckController)
                    continue;

                // Keep enabled list minimal and explicit for stability.
                if (b is TacticalPlayerController
                    || b is TacticalHarvestInteractor
                    || b is CraftingStationInteractor
                    || b is LootInteractor
                    || b is PlayerCombatController
                    )
                {
                    b.enabled = false;
                    _disabledWhileDriving.Add(b);
                }
            }

            if (_driverCc != null)
                _driverCc.enabled = false;

            if (_driverRenderers != null)
            {
                foreach (var r in _driverRenderers)
                    r.enabled = false;
            }

            // Parent to seat so camera (tracking player) follows truck.
            if (seatAnchor != null)
            {
                player.transform.SetParent(seatAnchor, worldPositionStays: true);
                player.transform.SetPositionAndRotation(seatAnchor.position, seatAnchor.rotation);
            }
        }

        private void Exit()
        {
            if (!IsOccupied)
                return;
            ForceExitAndClear(stopTruck: false);
        }

        private void ForceExitAndClear(bool stopTruck)
        {
            var player = _driver;
            var cc = _driverCc;
            var renderers = _driverRenderers;

            _driver = null;
            _driverCc = null;
            _driverVitals = null;
            _driverRenderers = null;

            if (stopTruck && _rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            if (player != null)
                player.transform.SetParent(null, worldPositionStays: true);

            if (renderers != null)
            {
                foreach (var r in renderers)
                    r.enabled = true;
            }

            if (cc != null)
                cc.enabled = true;

            foreach (var b in _disabledWhileDriving)
            {
                if (b != null)
                    b.enabled = true;
            }
            _disabledWhileDriving.Clear();

            // Close truck UI if it was open.
            if (TransportTruckPanel.Instance != null && TransportTruckPanel.Instance.IsOpen)
                TransportTruckPanel.Instance.Close();

            // Place at safe exit position.
            if (player != null)
            {
                var pos = FindSafeExitPosition();
                player.transform.position = pos;
            }
        }

        private Vector3 FindSafeExitPosition()
        {
            var basePos = exitPoint != null
                ? exitPoint.position
                : (transform.position + transform.right * 2.0f);

            basePos.y = transform.position.y;

            const float radius = 0.45f;
            var candidates = new List<Vector3>
            {
                basePos,
                transform.position - transform.right * 2.0f,
                transform.position + transform.forward * 2.0f,
                transform.position - transform.forward * 2.0f
            };

            foreach (var c in candidates)
            {
                var p = c;
                p.y = transform.position.y + 0.5f;
                if (!Physics.CheckSphere(p, radius, ~0, QueryTriggerInteraction.Ignore))
                    return p;
            }

            // Fallback: above truck.
            return transform.position + Vector3.up * 2.0f;
        }

        private void EnsureAnchorsExist()
        {
            if (seatAnchor == null)
            {
                var go = new GameObject("SeatAnchor");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0.25f, 1.1f, 0.6f);
                seatAnchor = go.transform;
            }

            if (exitPoint == null)
            {
                var go = new GameObject("ExitPoint");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(2.0f, 0.5f, 0.0f);
                exitPoint = go.transform;
            }
        }

        private void EnsureStablePhysicsDefaults()
        {
            if (_rb == null)
                return;

            _rb.mass = Mathf.Clamp(_rb.mass <= 0f ? 2500f : _rb.mass, 1500f, 4000f);
            _rb.drag = Mathf.Max(_rb.drag, 0.9f);
            _rb.angularDrag = Mathf.Max(_rb.angularDrag, 3.0f);
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // Stability guards (6.2): reduce spinouts/launches on collision.
            _rb.maxAngularVelocity = Mathf.Min(_rb.maxAngularVelocity, 2.5f);
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.centerOfMass = new Vector3(0f, 0.45f, 0f);
        }

        private void EnsureMinimalVisualsExist()
        {
            // If the prefab authoring added visuals, don't duplicate them.
            // Heuristic: any MeshRenderer under children named "Body"/"Cab"/"Wheels" etc.
            var existing = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            if (existing != null && existing.Any(r => r != null && r.transform != transform))
                return;

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            body.transform.localScale = new Vector3(2.8f, 0.8f, 5.0f);
            Destroy(body.GetComponent<Collider>()); // root collider handles blocking

            // Cab
            var cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cab.name = "Cab";
            cab.transform.SetParent(transform, false);
            cab.transform.localPosition = new Vector3(0.0f, 1.15f, 1.3f);
            cab.transform.localScale = new Vector3(2.2f, 0.9f, 1.8f);
            Destroy(cab.GetComponent<Collider>());

            // Wheels
            void MakeWheel(string name, float x, float z)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                w.name = name;
                w.transform.SetParent(transform, false);
                w.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                w.transform.localPosition = new Vector3(x, 0.35f, z);
                Destroy(w.GetComponent<Collider>());
            }

            MakeWheel("Wheel_FL", -1.35f, 1.6f);
            MakeWheel("Wheel_FR", 1.35f, 1.6f);
            MakeWheel("Wheel_RL", -1.35f, -1.6f);
            MakeWheel("Wheel_RR", 1.35f, -1.6f);
        }
    }
}

