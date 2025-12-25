using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Core;
using Frontline.Crafting;
using Frontline.Definitions;
using Frontline.Economy;
using Frontline.Gameplay;
using Frontline.Tactical;
using Frontline.UI;
using Frontline.World;
using UnityEngine;

namespace Frontline.Buildables
{
    /// <summary>
    /// Milestone 5 buildables runtime:
    /// - Build mode (B) + placement preview, grid snap, rotate (R)
    /// - Placement costs (PlayerInventoryService), CreatedPool, DestroyedPool.MarkCrafted
    /// - Damage via Health (already supported), destruction via Destructible
    /// - Repair via Hammer (hold LMB), consuming small resources
    /// - Storage crate interaction (E) with minimal UI
    /// - Local persistence (JSON) in Application.persistentDataPath
    /// </summary>
    public sealed class BuildablesService : MonoBehaviour
    {
        public static BuildablesService Instance { get; private set; }

        [Header("Build Mode")]
        [SerializeField] private KeyCode toggleBuildModeKey = KeyCode.B;
        [SerializeField] private float gridSizeMeters = 1.0f;

        [Header("Placement")]
        [SerializeField] private float placementRayDistance = 200f;

        [Header("Repair (Hammer)")]
        [SerializeField] private float repairRange = 2.0f;
        [SerializeField] private float repairTicksPerSecond = 6f;
        [SerializeField] private int repairHpPerTick = 5;

        private bool _buildMode;
        private string _selectedItemId = "build_foundation";
        private int _rotationSteps;
        private int _heightLevel;
        private string _placementBlockedReason = "";

        private GameObject _preview;
        private Renderer _previewRenderer;
        private Material _previewMaterial;
        private Vector3 _previewPos;
        private Quaternion _previewRot = Quaternion.identity;
        private bool _previewValid;
        private bool _previewSupported;

        private float _nextRepairTime;

        private bool _dirty;
        private float _nextAutosaveTime;

        private Transform _player;

        private string SavePath => Path.Combine(Application.persistentDataPath, "buildables_world.json");

        public bool IsBuildModeActive => _buildMode;
        public bool IsInputLockedForCombatOrHarvest =>
            _buildMode
            || (UiModalManager.Instance != null && UiModalManager.Instance.HasOpenModal)
            || (StorageCratePanel.Instance != null && StorageCratePanel.Instance.IsOpen);
        public string SelectedBuildItemId => _selectedItemId;

        public void SetSelectedBuildItem(string itemId)
        {
            Select(itemId);
        }

        public void ToggleBuildMode()
        {
            if (_buildMode)
                ExitBuildMode();
            else
                EnterBuildMode();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Patch 6: ensure buildables can block player/NPC movement even if they use IgnoreRaycast layer (2).
            Physics.IgnoreLayerCollision(0, 2, false);
        }

        private void Update()
        {
            RefreshPlayer();

            // Autosave debounce
            if (_dirty && Time.unscaledTime >= _nextAutosaveTime)
            {
                SaveWorld();
                _dirty = false;
                _nextAutosaveTime = Time.unscaledTime + 0.25f;
            }

            // If any gameplay modal is open, don't process build/crate/repair inputs.
            // (This allows Esc to close the modal without exiting Construction Mode.)
            if (UiModalManager.Instance != null && UiModalManager.Instance.HasOpenModal)
                return;

            // If crate UI is open, don't process build mode/repair inputs.
            if (StorageCratePanel.Instance != null && StorageCratePanel.Instance.IsOpen)
                return;

            if (!_buildMode)
            {
                if (Input.GetKeyDown(toggleBuildModeKey))
                    EnterBuildMode();

                HandleCrateInteract();
                HandleRepair();
                return;
            }

            // Build mode input
            if (Input.GetKeyDown(toggleBuildModeKey) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitBuildMode();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) Select("build_foundation");
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select("build_wall");
            if (Input.GetKeyDown(KeyCode.Alpha3)) Select("build_gate");
            if (Input.GetKeyDown(KeyCode.Alpha4)) Select("build_storage");
            if (Input.GetKeyDown(KeyCode.Alpha5)) Select("build_ramp");
            if (Input.GetKeyDown(KeyCode.Alpha0)) ClearSelectionForRepair();

            if (Input.GetKeyDown(KeyCode.R))
                _rotationSteps = (_rotationSteps + 1) % 4;

            // Height stacking (mouse wheel).
            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                _heightLevel = Mathf.Max(0, _heightLevel + (wheel > 0 ? 1 : -1));
            }

            // Construction Mode rules:
            // - If build selection active: preview + place.
            // - If no selection: allow hammer repair while staying in construction mode.
            if (string.IsNullOrWhiteSpace(_selectedItemId))
            {
                DestroyPreview();
                HandleRepair();
                return;
            }

            UpdatePreview();

            if (Input.GetMouseButtonDown(0) && _previewValid)
                ConfirmPlacement();
        }

        private void OnGUI()
        {
            // Draw HUD only once per frame (Repaint) to avoid IMGUI multi-event overlap.
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            DrawHelpBottomLeft();
            DrawSelectedBottomRight();
        }

        private void DrawHelpBottomLeft()
        {
            const int pad = 12;
            const int width = 420;
            const int height = 90;
            var rect = new Rect(pad, Screen.height - height - pad, width, height);

            var style = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8)
            };
            GUI.Box(rect, "", style);

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };

            var mode = _buildMode ? "Build" : "Combat/Harvest";
            var lines =
                _buildMode
                    ? $"Mode: {mode}\n" +
                      $"Keys: 1 Foundation | 2 Wall | 3 Gate | 4 Storage | 5 Ramp | 0 Repair\n" +
                      $"R rotate | MouseWheel height ({_heightLevel}) | LMB place | (0: Hammer+LMB repair) | RMB/Esc exit"
                    : $"Mode: {mode}\n" +
                      $"B: Build Mode\n" +
                      $"Hammer+LMB: Repair | E: Open/Close Storage Crate | Esc: Close UI";

            if (_buildMode && !string.IsNullOrWhiteSpace(_placementBlockedReason))
                lines += $"\nBlocked: {_placementBlockedReason}";

            GUI.Label(new Rect(rect.x + 10, rect.y + 8, rect.width - 20, rect.height - 16), lines, labelStyle);
        }

        private void DrawSelectedBottomRight()
        {
            var selected = SelectionUIState.SelectedText;
            if (string.IsNullOrWhiteSpace(selected))
            {
                // Default to build selection when in build mode.
                if (_buildMode && !string.IsNullOrWhiteSpace(_selectedItemId))
                    selected = $"Selected: {_selectedItemId}";
            }

            if (string.IsNullOrWhiteSpace(selected))
                return;

            const int pad = 12;
            const int width = 360;
            const int height = 34;
            var rect = new Rect(Screen.width - width - pad, Screen.height - height - pad, width, height);

            var style = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 7, 7)
            };
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 10, rect.y + 7, rect.width - 20, rect.height - 14), selected);
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public void SaveWorld()
        {
            try
            {
                var snap = new BuildablesWorldSnapshot();

                var all = FindObjectsOfType<Buildable>();
                foreach (var b in all)
                {
                    if (b == null || string.IsNullOrWhiteSpace(b.ItemId) || b.Health == null)
                        continue;

                    var e = new BuildablesWorldSnapshot.BuildableEntry
                    {
                        itemId = b.ItemId,
                        position = b.transform.position,
                        rotation = b.transform.rotation,
                        currentHp = b.Health.CurrentHp,
                        ownerTeam = b.OwnerTeam,
                        ownerId = b.OwnerId,
                        stored = new List<BuildablesWorldSnapshot.ItemStack>()
                    };

                    var crate = b.GetComponent<StorageCrate>();
                    if (crate != null)
                        e.stored = crate.ToSnapshot();

                    var gate = b.GetComponent<GateController>();
                    if (gate != null)
                        e.gateOpen = gate.IsOpen;

                    snap.buildables.Add(e);
                }

                var json = JsonUtility.ToJson(snap, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Buildables: failed to save '{SavePath}': {ex.Message}");
            }
        }

        public void LoadWorld()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return;

                var json = File.ReadAllText(SavePath);
                var snap = JsonUtility.FromJson<BuildablesWorldSnapshot>(json);
                if (snap == null)
                    return;

                ClearAllBuildablesRuntime();

                foreach (var e in snap.buildables)
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.itemId))
                        continue;
                    SpawnFromLoad(e);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Buildables: failed to load '{SavePath}': {ex.Message}");
            }
        }

        public void ClearAllBuildablesDev()
        {
            ClearAllBuildablesRuntime();

            try
            {
                if (File.Exists(SavePath))
                    File.Delete(SavePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Buildables: failed to delete '{SavePath}': {ex.Message}");
            }
        }

        private void SpawnFromLoad(BuildablesWorldSnapshot.BuildableEntry e)
        {
            var go = SpawnBuildableGameObject(e.itemId, e.position, e.rotation, applyEconomy: false);
            if (go == null)
                return;

            var b = go.GetComponent<Buildable>();
            if (b != null)
            {
                b.SetCurrentHpForLoad(e.currentHp);
                b.SetOwnerForLoad(e.ownerId);
            }

            var crate = go.GetComponent<StorageCrate>();
            if (crate != null)
                crate.LoadFromSnapshot(e.stored);

            var gate = go.GetComponent<GateController>();
            if (gate != null)
                gate.SetOpenForLoad(e.gateOpen);
        }

        private void ClearAllBuildablesRuntime()
        {
            var all = FindObjectsOfType<Buildable>();
            foreach (var b in all)
            {
                if (b != null)
                    Destroy(b.gameObject);
            }
        }

        private void RefreshPlayer()
        {
            if (_player != null)
                return;
            var p = FindFirstObjectByType<TacticalPlayerController>();
            _player = p != null ? p.transform : null;
        }

        private void EnterBuildMode()
        {
            _buildMode = true;
            SelectionUIState.SetSelected(string.IsNullOrWhiteSpace(_selectedItemId) ? "" : $"Selected: {_selectedItemId}");
            EnsurePreview();
        }

        private void ExitBuildMode()
        {
            _buildMode = false;
            DestroyPreview();
        }

        private void Select(string itemId)
        {
            _selectedItemId = itemId ?? "";
            _rotationSteps = 0;
            _heightLevel = 0;
            SelectionUIState.SetSelected(string.IsNullOrWhiteSpace(_selectedItemId) ? "" : $"Selected: {_selectedItemId}");
            EnsurePreview();
        }

        private void ClearSelectionForRepair()
        {
            _selectedItemId = "";
            _rotationSteps = 0;
            _heightLevel = 0;
            _placementBlockedReason = "";
            SelectionUIState.SetSelected("Selected: repair");
            DestroyPreview();
        }

        private void EnsurePreview()
        {
            if (_preview != null)
            {
                ApplyPreviewShape();
                return;
            }

            _preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _preview.name = "_BuildPreview";
            _preview.layer = 2; // Ignore Raycast

            var col = _preview.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            _previewRenderer = _preview.GetComponent<Renderer>();
            if (_previewRenderer != null)
            {
                _previewMaterial = new Material(Shader.Find("Standard"));
                ConfigureTransparent(_previewMaterial);
                _previewRenderer.material = _previewMaterial;
            }

            ApplyPreviewShape();
        }

        private void DestroyPreview()
        {
            if (_preview != null)
                Destroy(_preview);
            _preview = null;
            _previewRenderer = null;
            _previewMaterial = null;
        }

        private void ApplyPreviewShape()
        {
            if (_preview == null)
                return;
            GetShape(_selectedItemId, out var scale, out _);
            _preview.transform.localScale = scale;
        }

        private void UpdatePreview()
        {
            if (_preview == null)
                EnsurePreview();
            if (_preview == null)
                return;

            if (!TryGetMouseWorld(out var p, out var supportTopY, out _previewSupported))
                return;

            var snap = Mathf.Max(0.25f, gridSizeMeters);
            p.x = Mathf.Round(p.x / snap) * snap;
            p.z = Mathf.Round(p.z / snap) * snap;

            GetShape(_selectedItemId, out var scale, out _);
            var halfY = scale.y * 0.5f;

            // Re-sample support at snapped XZ to prevent "floaty" snap.
            if (TryGetSupportTopYAt(p.x, p.z, out var snappedSupportTopY))
            {
                supportTopY = snappedSupportTopY;
                _previewSupported = true;
            }
            else
            {
                _previewSupported = false;
            }

            // Place bottom of buildable on top of support surface, plus optional height levels.
            var bottomY = supportTopY + (_heightLevel * WorldConstants.WORLD_LEVEL_HEIGHT);
            p.y = bottomY + halfY;

            _previewPos = p;
            _previewRot = Quaternion.Euler(0f, 90f * _rotationSteps, 0f);

            _preview.transform.SetPositionAndRotation(_previewPos, _previewRot);

            _previewValid = _previewSupported && IsPlacementValid(_selectedItemId, _previewPos, _previewRot);

            if (_previewMaterial != null)
            {
                GetShape(_selectedItemId, out _, out var tint);
                var c = _previewValid ? new Color(0.2f, 0.9f, 0.2f, 0.35f) : new Color(0.9f, 0.2f, 0.2f, 0.35f);
                // blend the buildable tint with validity color
                var blended = Color.Lerp(tint, c, 0.6f);
                blended.a = c.a;
                _previewMaterial.color = blended;
            }
        }

        private void ConfirmPlacement()
        {
            var go = SpawnBuildableGameObject(_selectedItemId, _previewPos, _previewRot, applyEconomy: true);
            if (go == null)
                return;

            // Patch 5.2: settle buildables vertically to eliminate occasional floating.
            StartCoroutine(SettlePlacedBuildable(go));

            MarkDirty();
        }

        private System.Collections.IEnumerator SettlePlacedBuildable(GameObject go)
        {
            if (go == null)
                yield break;

            // Add a temporary rigidbody to let physics settle only in Y.
            var rb = go.GetComponent<Rigidbody>();
            var added = rb == null;
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            rb.drag = 6f;
            rb.angularDrag = 8f;

            var start = Time.time;
            var maxTime = 0.85f;
            while (Time.time - start < maxTime)
            {
                if (rb == null)
                    yield break;
                if (rb.IsSleeping())
                    break;
                yield return null;
            }

            if (rb == null)
                yield break;

            // Lock it in place after settling.
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            // If we added it purely for settling, we keep it kinematic so the object never "wanders".
            // (Removing the rigidbody can change contact behavior across Unity versions.)
            if (added)
                yield break;
        }

        private GameObject SpawnBuildableGameObject(string itemId, Vector3 pos, Quaternion rot, bool applyEconomy)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            // Skill/lock gate.
            if (!CanPlaceBySkill(itemId, out _))
                return null;

            var costs = GetBuildCosts(itemId);

            if (applyEconomy)
            {
                if (PlayerInventoryService.Instance == null)
                    return null;
                if (!PlayerInventoryService.Instance.CanAfford(costs))
                    return null;

                PlayerInventoryService.Instance.Spend(costs);

                if (CreatedPoolService.Instance != null)
                    CreatedPoolService.Instance.RegisterCreated(itemId, 1);
                if (DestroyedPoolService.Instance != null)
                    DestroyedPoolService.Instance.MarkCrafted(itemId);
            }

            GetShape(itemId, out var scale, out var tint);
            var maxHp = GetMaxHp(itemId);

            GameObject go;
            if (itemId == "build_ramp")
                go = SpawnRamp(pos, rot);
            else if (itemId == "build_gate")
                go = SpawnGate(pos, rot);
            else
                go = SpawnSimpleCube(itemId, pos, rot, scale, tint);

            if (go == null)
                return null;

            var buildable = go.AddComponent<Buildable>();
            buildable.Configure(itemId, maxHp, team: 0);
            buildable.Died += OnBuildableDied;

            var destructible = go.AddComponent<Destructible>();
            destructible.SetDefinitionId(itemId);

            // Patch 5.2: health visibility on buildables.
            if (go.GetComponent<WorldHealthPipBar>() == null)
                go.AddComponent<WorldHealthPipBar>();

            if (itemId == "build_storage")
            {
                go.AddComponent<StorageCrate>();
            }

            return go;
        }

        private static GameObject SpawnSimpleCube(string itemId, Vector3 pos, Quaternion rot, Vector3 scale, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = itemId;
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            go.layer = 0; // Default: blocks movement + participates in occlusion and raycasts.

            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                // Placeholder visuals: prefer stable, reusable materials (no gray primitives).
                r.material = GetPlaceholderMaterialForBuildable(itemId, tint);
            }

            return go;
        }

        private static GameObject SpawnRamp(Vector3 pos, Quaternion rot)
        {
            var root = new GameObject("build_ramp");
            root.layer = 0;
            root.transform.SetPositionAndRotation(pos, rot);

            // Approximate a wedge with a sloped top (walkable).
            // Rise = WORLD_LEVEL_HEIGHT, run = 2.0m to match foundation footprint.
            const float width = 2.0f;
            const float run = 2.0f;
            var rise = WorldConstants.WORLD_LEVEL_HEIGHT;
            var angleDeg = Mathf.Atan2(rise, run) * Mathf.Rad2Deg; // ~26.565
            var slopeLen = Mathf.Sqrt(run * run + rise * rise);   // ~2.236
            const float thickness = 0.25f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "RampBody";
            body.layer = 0;
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(width, thickness, slopeLen);
            body.transform.localRotation = Quaternion.Euler(-angleDeg, 0f, 0f);

            // Center height so the lowest point sits on the support surface.
            var a = angleDeg * Mathf.Deg2Rad;
            var t = thickness * 0.5f;
            var l = slopeLen * 0.5f;
            var centerY = t * Mathf.Cos(a) + l * Mathf.Sin(a);
            body.transform.localPosition = new Vector3(0f, centerY, 0f);

            var r = body.GetComponent<Renderer>();
            if (r != null)
                r.material = GetPlaceholderMaterialForBuildable("build_ramp", new Color(0.55f, 0.40f, 0.20f));

            return root;
        }

        private static GameObject SpawnGate(Vector3 pos, Quaternion rot)
        {
            var root = new GameObject("build_gate");
            root.layer = 0;
            root.transform.SetPositionAndRotation(pos, rot);

            // Always-on interaction volume (raycastable, but non-blocking).
            var interact = root.AddComponent<BoxCollider>();
            interact.isTrigger = true;
            interact.size = new Vector3(2.0f, 2.0f, 0.35f);
            interact.center = Vector3.zero;

            // Pivot at left hinge.
            var pivot = new GameObject("DoorPivot");
            pivot.layer = 0;
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(-1.0f, 0f, 0f);
            pivot.transform.localRotation = Quaternion.identity;

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.layer = 0;
            door.transform.SetParent(pivot.transform, false);
            door.transform.localScale = new Vector3(2.0f, 2.0f, 0.35f);
            door.transform.localPosition = new Vector3(1.0f, 0f, 0f);

            var doorCol = door.GetComponent<Collider>();
            if (doorCol != null)
                doorCol.isTrigger = false;

            var r = door.GetComponent<Renderer>();
            if (r != null)
                r.material = GetPlaceholderMaterialForBuildable("build_gate", new Color(0.35f, 0.25f, 0.15f));

            var gate = root.AddComponent<GateController>();
            gate.Configure(pivot.transform, doorCol, interact);

            return root;
        }

        private static Material _matWood;
        private static Material _matStone;
        private static Material _matMetal;

        private static Material GetPlaceholderMaterialForBuildable(string itemId, Color fallbackTint)
        {
            // Runtime placeholder materials (verifiable in Play Mode).
            // Editor assets are generated separately (see placeholder art patch).
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                var m = new Material(Shader.Find("Diffuse"));
                m.color = fallbackTint;
                return m;
            }

            _matWood ??= MakeMat(shader, new Color(0.53f, 0.34f, 0.18f));
            _matStone ??= MakeMat(shader, new Color(0.55f, 0.55f, 0.58f));
            _matMetal ??= MakeMat(shader, new Color(0.55f, 0.62f, 0.68f));

            if (itemId == "build_foundation")
                return _matStone;
            if (itemId == "build_wall" || itemId == "build_gate" || itemId == "build_storage")
                return _matWood;
            return _matMetal;
        }

        private static Material MakeMat(Shader shader, Color c)
        {
            var m = new Material(shader);
            m.color = c;
            return m;
        }

        private void OnBuildableDied(Buildable b)
        {
            MarkDirty();

            // Close UI if the active crate died.
            if (StorageCratePanel.Instance != null && StorageCratePanel.Instance.IsOpen)
            {
                // If the open crate belongs to this buildable, close.
                var crate = b != null ? b.GetComponent<StorageCrate>() : null;
                if (crate != null)
                    StorageCratePanel.Instance.Close();
            }
        }

        private void HandleCrateInteract()
        {
            if (!Input.GetKeyDown(KeyCode.E))
                return;

            // Patch 5.2: if an interact-opened modal is open, E should close it (handled centrally).
            if (UiModalManager.Instance != null && UiModalManager.Instance.HasOpenModal)
                return;

            if (_player == null || Camera.main == null)
                return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, placementRayDistance, ~0, QueryTriggerInteraction.Collide))
                return;

            var crate = hit.collider != null ? hit.collider.GetComponentInParent<StorageCrate>() : null;
            if (crate == null)
                return;

            var p = _player.position;
            p.y = 0f;
            var h = crate.transform.position;
            h.y = 0f;
            if (Vector3.Distance(p, h) > 1.5f)
                return;

            if (StorageCratePanel.Instance == null)
                return;

            if (UiModalManager.Instance != null)
            {
                UiModalManager.Instance.TryToggleInteractModal(
                    modalId: "storage_crate",
                    tryOpen: () =>
                    {
                        StorageCratePanel.Instance.Open(crate);
                        return StorageCratePanel.Instance.IsOpen;
                    },
                    closeAction: StorageCratePanel.Instance.Close);
                return;
            }

            StorageCratePanel.Instance.Open(crate);
        }

        private void HandleRepair()
        {
            if (!Input.GetMouseButton(0))
                return;
            if (Time.unscaledTime < _nextRepairTime)
                return;
            _nextRepairTime = Time.unscaledTime + (repairTicksPerSecond <= 0f ? 0.2f : (1f / repairTicksPerSecond));

            if (PlayerInventoryService.Instance == null)
                return;

            var tool = PlayerInventoryService.Instance.EquippedTool;
            if (tool == null || tool.toolType != ToolType.Hammer)
                return;

            if (_player == null || Camera.main == null)
                return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, placementRayDistance, ~0, QueryTriggerInteraction.Ignore))
                return;

            var buildable = hit.collider != null ? hit.collider.GetComponentInParent<Buildable>() : null;
            if (buildable == null || buildable.Health == null)
                return;

            var p = _player.position;
            p.y = 0f;
            var b = buildable.transform.position;
            b.y = 0f;
            if (Vector3.Distance(p, b) > repairRange)
                return;

            var missing = buildable.Health.MaxHp - buildable.Health.CurrentHp;
            if (missing <= 0)
                return;

            var hp = Mathf.Clamp(repairHpPerTick, 1, missing);

            // "Small resources" per repair tick: consume 1 of the primary build cost resource.
            var primary = GetPrimaryRepairResource(buildable.ItemId);
            if (string.IsNullOrWhiteSpace(primary))
                return;

            if (!PlayerInventoryService.Instance.TryRemoveResource(primary, 1))
                return;

            buildable.Health.Restore(hp);
            MarkDirty();
        }

        private static string GetPrimaryRepairResource(string itemId)
        {
            // Prefer the first craftCost listed in definitions.
            if (DefinitionRegistry.Instance != null)
            {
                var def = DefinitionRegistry.Instance.Definitions.structures.FirstOrDefault(s => s != null && s.id == itemId);
                if (def != null && def.craftCosts != null && def.craftCosts.Count > 0)
                {
                    var first = def.craftCosts[0];
                    if (first != null && !string.IsNullOrWhiteSpace(first.materialId))
                        return first.materialId;
                }
            }

            // Fallbacks
            if (itemId == "build_foundation")
                return ToolRecipes.Stone;
            return ToolRecipes.Wood;
        }

        private static List<ToolRecipe.Cost> GetBuildCosts(string itemId)
        {
            if (DefinitionRegistry.Instance == null)
                return new List<ToolRecipe.Cost>();

            var def = DefinitionRegistry.Instance.Definitions.structures.FirstOrDefault(s => s != null && s.id == itemId);
            if (def == null || def.craftCosts == null)
                return new List<ToolRecipe.Cost>();

            return def.craftCosts
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.materialId) && c.amount > 0)
                .Select(c => new ToolRecipe.Cost { resourceId = c.materialId, amount = c.amount })
                .ToList();
        }

        private bool IsPlacementValid(string itemId, Vector3 pos, Quaternion rot)
        {
            _placementBlockedReason = "";

            if (PlayerInventoryService.Instance == null)
                return false;

            if (!CanPlaceBySkill(itemId, out var skillReason))
            {
                _placementBlockedReason = skillReason;
                return false;
            }

            var costs = GetBuildCosts(itemId);
            if (!PlayerInventoryService.Instance.CanAfford(costs))
            {
                _placementBlockedReason = "Insufficient materials";
                return false;
            }

            GetShape(itemId, out var scale, out _);
            var half = scale * 0.5f;
            half.y = Mathf.Max(0.1f, half.y);

            // Patch 5.3A: placement validity must work when rotated and allow flush adjacency.
            // Use an oriented box (OverlapBox with rotation) and a small "skin" so touching faces aren't treated as overlap.
            var skin = Mathf.Max(0.005f, gridSizeMeters * 0.02f);
            var ext = new Vector3(
                Mathf.Max(0.05f, half.x - skin),
                Mathf.Max(0.05f, half.y - skin),
                Mathf.Max(0.05f, half.z - skin));

            // Check for blocking colliders (ignore triggers and non-gameplay colliders).
            var hits = Physics.OverlapBox(pos, ext, rot, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in hits)
            {
                if (c == null)
                    continue;
                if (c.isTrigger)
                    continue;
                if (_preview != null && c.transform.IsChildOf(_preview.transform))
                    continue;

                if (c.GetComponentInParent<Buildable>() != null)
                    return false;
                if (c.GetComponentInParent<Harvesting.HarvestNode>() != null)
                    return false;
                if (c.GetComponentInParent<TacticalPlayerController>() != null)
                    return false;
                if (c.GetComponentInParent<Combat.NpcController>() != null)
                    return false;
            }

            return true;
        }

        private bool CanPlaceBySkill(string itemId, out string reason)
        {
            reason = "";
            if (DefinitionRegistry.Instance == null)
                return true;

            var def = DefinitionRegistry.Instance.Definitions.structures.FirstOrDefault(s => s != null && s.id == itemId);
            if (def == null || string.IsNullOrWhiteSpace(def.requiredSkillId))
                return true;

            if (PlayerSkillsService.Instance == null)
            {
                reason = $"Locked: {def.requiredSkillId}";
                return false;
            }

            if (!PlayerSkillsService.Instance.HasSkill(def.requiredSkillId))
            {
                reason = $"Locked: {def.requiredSkillId}";
                return false;
            }

            return true;
        }

        private bool TryGetMouseWorld(out Vector3 point, out float supportTopY, out bool supported)
        {
            point = Vector3.zero;
            supportTopY = 0f;
            supported = false;
            var cam = Camera.main;
            if (cam == null)
                return false;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, placementRayDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                // Initial support comes from the raycast hit surface.
                supportTopY = hit.collider != null ? hit.collider.bounds.max.y : hit.point.y;
                supported = hit.collider != null;
                return true;
            }

            // Fallback to XZ plane at y=0
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var enter))
            {
                point = ray.GetPoint(enter);
                supportTopY = 0f;
                supported = true;
                return true;
            }

            return false;
        }

        private bool TryGetSupportTopYAt(float x, float z, out float topY)
        {
            topY = 0f;

            // Raycast straight down from above to find the topmost support surface at XZ.
            // Reject triggers to prevent "floating" support (harvest nodes use triggers).
            var origin = new Vector3(x, 1000f, z);
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 2000f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            if (hit.collider == null || hit.collider.isTrigger)
                return false;

            topY = hit.collider.bounds.max.y;
            return true;
        }

        private static int GetMaxHp(string itemId)
        {
            return itemId switch
            {
                "build_foundation" => 220,
                "build_wall" => 160,
                "build_gate" => 190,
                "build_storage" => 140,
                "build_ramp" => 180,
                _ => 120
            };
        }

        private static void GetShape(string itemId, out Vector3 scale, out Color tint)
        {
            switch (itemId)
            {
                case "build_foundation":
                    scale = new Vector3(2.0f, 0.25f, 2.0f);
                    tint = new Color(0.45f, 0.45f, 0.45f);
                    break;
                case "build_wall":
                    scale = new Vector3(2.0f, 2.0f, 0.25f);
                    tint = new Color(0.55f, 0.35f, 0.18f);
                    break;
                case "build_gate":
                    scale = new Vector3(2.0f, 2.0f, 0.35f);
                    tint = new Color(0.35f, 0.25f, 0.15f);
                    break;
                case "build_storage":
                    scale = new Vector3(1.0f, 1.0f, 1.0f);
                    tint = new Color(0.55f, 0.40f, 0.20f);
                    break;
                case "build_ramp":
                    scale = new Vector3(2.0f, 1.25f, 2.0f);
                    tint = new Color(0.55f, 0.40f, 0.20f);
                    break;
                default:
                    scale = new Vector3(1.0f, 1.0f, 1.0f);
                    tint = new Color(0.5f, 0.5f, 0.5f);
                    break;
            }
        }

        private static void ConfigureTransparent(Material mat)
        {
            if (mat == null)
                return;

            // Standard shader transparent mode (common Unity pattern).
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }
}

