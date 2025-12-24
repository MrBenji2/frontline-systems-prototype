using Frontline.Tactical;
using Frontline.World;
using UnityEngine;

namespace Frontline.Combat
{
    [RequireComponent(typeof(Health))]
    public sealed class NpcController : MonoBehaviour
    {
        [SerializeField] private string npcType = "Easy_Ranged";
        [SerializeField] private NpcDifficulty difficulty = NpcDifficulty.Easy;
        [SerializeField] private NpcAttackType attackType = NpcAttackType.Ranged;

        [Header("Damage")]
        [SerializeField] private int rangedDamage = 10;
        [SerializeField] private int meleeDamage = 12;

        private Health _health;
        private float _nextAttackTime;
        private CharacterController _cc;
        private Vector3 _ccVelocity;
        private const float Gravity = 20f;

        public string NpcType => npcType;
        public NpcDifficulty Difficulty => difficulty;
        public NpcAttackType AttackType => attackType;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _cc = GetComponent<CharacterController>();
        }

        public void Configure(NpcDifficulty d, NpcAttackType t)
        {
            difficulty = d;
            attackType = t;
            npcType = NpcCombatConfig.NpcTypeId(d, t);

            NpcCombatConfig.Get(d, t, out var hp, out var moveSpeed, out var aggroRange, out var attackInterval, out var aimErrorDegrees, out var meleeRange);
            _health.Configure(hp, true);

            _moveSpeed = moveSpeed;
            _aggroRange = aggroRange;
            _attackInterval = attackInterval;
            _aimErrorDegrees = aimErrorDegrees;
            _meleeRange = meleeRange;

            rangedDamage = d switch
            {
                NpcDifficulty.Easy => 6,
                NpcDifficulty.Medium => 9,
                _ => 12,
            };

            meleeDamage = d switch
            {
                NpcDifficulty.Easy => 8,
                NpcDifficulty.Medium => 12,
                _ => 16,
            };
        }

        private float _moveSpeed = 5.5f;
        private float _aggroRange = 10f;
        private float _attackInterval = 1.0f;
        private float _aimErrorDegrees = 10f;
        private float _meleeRange = 2f;

        private void Update()
        {
            if (_health != null && _health.IsDead)
                return;

            var player = FindFirstObjectByType<TacticalPlayerController>();
            if (player == null)
                return;

            var playerPos = player.transform.position;
            var myPos = transform.position;

            playerPos.y = 0f;
            myPos.y = 0f;

            var dist = Vector3.Distance(myPos, playerPos);
            if (dist > _aggroRange)
                return;

            // Move toward player.
            if (dist > 0.25f)
            {
                var step = _moveSpeed * Time.deltaTime;
                var next = Vector3.MoveTowards(myPos, playerPos, step);
                next.y = transform.position.y;

                // Patch 6: if a CharacterController exists, use it for collision-aware movement.
                if (_cc != null)
                {
                    var delta = next - transform.position;
                    if (_cc.isGrounded)
                        _ccVelocity.y = -1f;
                    else
                        _ccVelocity.y -= Gravity * Time.deltaTime;

                    // Keep horizontal move from the AI step.
                    _ccVelocity.x = delta.x / Mathf.Max(0.0001f, Time.deltaTime);
                    _ccVelocity.z = delta.z / Mathf.Max(0.0001f, Time.deltaTime);
                    _cc.Move(new Vector3(delta.x, _ccVelocity.y * Time.deltaTime, delta.z));
                }
                else
                {
                    transform.position = next;
                }
            }

            // Face/aim toward player.
            var dir = (playerPos - myPos);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-12f * Time.deltaTime));
            }

            if (Time.unscaledTime < _nextAttackTime)
                return;

            if (attackType == NpcAttackType.Melee)
            {
                if (dist <= _meleeRange)
                    DoMelee(player);
            }
            else
            {
                DoRanged(player);
            }
        }

        private void DoMelee(TacticalPlayerController player)
        {
            _nextAttackTime = Time.unscaledTime + Mathf.Max(0.05f, _attackInterval);

            var vitals = player.GetComponent<PlayerCombatVitals>();
            if (vitals == null || vitals.IsDead)
                return;
            vitals.ApplyDamage(meleeDamage);
        }

        private void DoRanged(TacticalPlayerController player)
        {
            _nextAttackTime = Time.unscaledTime + Mathf.Max(0.05f, _attackInterval);

            var vitals = player.GetComponent<PlayerCombatVitals>();
            if (vitals == null || vitals.IsDead)
                return;

            var origin = transform.position + Vector3.up * 1.2f;
            var target = player.transform.position + Vector3.up * 1.2f;

            // Add aim error by offsetting the target point.
            if (_aimErrorDegrees > 0.01f)
            {
                var yaw = Random.Range(-_aimErrorDegrees, _aimErrorDegrees);
                var pitch = Random.Range(-_aimErrorDegrees * 0.25f, _aimErrorDegrees * 0.25f);
                var q = Quaternion.Euler(pitch, yaw, 0f);
                var dir = (target - origin).normalized;
                dir = q * dir;
                target = origin + dir * Vector3.Distance(origin, target);
            }

            var dir2 = target - origin;
            var dist = dir2.magnitude;
            if (dist <= 0.01f)
                return;

            // Use ~0 so we can hit IgnoreRaycast-layer player/NPCs if needed.
            if (Physics.Raycast(origin, dir2 / dist, out var hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.GetComponentInParent<TacticalPlayerController>() != null)
                    vitals.ApplyDamage(rangedDamage);
                return;
            }
        }
    }
}

