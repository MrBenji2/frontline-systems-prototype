using Frontline.Harvesting;
using Frontline.Tactical;
using Frontline.Buildables;
using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.PlayerCard;
using Frontline.World;
using UnityEngine;

namespace Frontline.Combat
{
    /// <summary>
    /// Minimal player combat:
    /// - LMB: ranged hitscan with cooldown
    /// - RMB: melee hitscan with cooldown
    ///
    /// Context priority (to not break harvesting):
    /// - If aiming at a HarvestNode within harvest range and holding LMB, harvesting wins and ranged does not fire.
    /// </summary>
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [Header("Ranged")]
        [SerializeField] private float rangedCooldown = 0.20f;
        [SerializeField] private int rangedDamage = 15;

        [Header("Melee")]
        [SerializeField] private float meleeCooldown = 0.70f;
        [SerializeField] private float meleeRange = 2.0f;
        [SerializeField] private int meleeDamage = 25;

        [Header("Harvest Compatibility")]
        [SerializeField] private float harvestRange = 3.0f;

        private float _nextRangedTime;
        private float _nextMeleeTime;
        private float _nextWeaponMeleeTime;

        private void Update()
        {
            if (BuildablesService.Instance != null && BuildablesService.Instance.IsInputLockedForCombatOrHarvest)
                return;

            if (Input.GetMouseButton(0))
            {
                // Context priority: harvesting wins.
                if (!IsHarvestingTargetUnderCursor())
                {
                    if (IsEquippedMeleeWeapon())
                        TryWeaponMelee();
                    else
                        TryRanged();
                }
            }
            if (Input.GetMouseButton(1))
                TryMelee();
        }

        private void OnGUI()
        {
            if (BuildablesService.Instance != null && BuildablesService.Instance.IsInputLockedForCombatOrHarvest)
                return;

            var rect = new Rect(10, Screen.height - 34, 220, 24);
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 6, rect.y + 3, rect.width - 12, rect.height - 6), "Combat: LMB shoot(or melee) / RMB melee");
        }

        private void TryRanged()
        {
            if (Time.unscaledTime < _nextRangedTime)
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            _nextRangedTime = Time.unscaledTime + Mathf.Max(0f, rangedCooldown);

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                return;

            var h = hit.collider != null ? hit.collider.GetComponentInParent<Health>() : null;
            if (h == null)
                return;

            // Track damage dealt
            var wasAlive = !h.IsDead;
            h.ApplyDamage(rangedDamage, gameObject);

            // Record stats
            var tracker = CombatStatsTracker.Instance;
            if (tracker != null)
            {
                tracker.RecordDamageDealt(rangedDamage, h.gameObject);
                if (wasAlive && h.IsDead)
                {
                    var isPlayer = h.GetComponent<TacticalPlayerController>() != null;
                    tracker.RecordKill(h.gameObject, isPlayer);
                }
            }
        }

        private bool IsEquippedMeleeWeapon()
        {
            if (PlayerInventoryService.Instance == null)
                return false;
            var t = PlayerInventoryService.Instance.EquippedTool;
            return t != null && t.toolType == ToolType.MeleeWeapon && MeleeWeaponStats.TryGet(t.itemId, out _);
        }

        private void TryWeaponMelee()
        {
            if (Time.unscaledTime < _nextWeaponMeleeTime)
                return;
            if (PlayerInventoryService.Instance == null)
                return;

            var tool = PlayerInventoryService.Instance.EquippedTool;
            if (tool == null || tool.toolType != ToolType.MeleeWeapon)
                return;
            if (!MeleeWeaponStats.TryGet(tool.itemId, out var s))
                return;

            // Higher speed = faster attacks.
            var cooldown = 1f / Mathf.Max(0.1f, s.speed);
            _nextWeaponMeleeTime = Time.unscaledTime + Mathf.Clamp(cooldown, 0.05f, 2.0f);

            var cam = Camera.main;
            if (cam == null)
                return;

            // Patch 7.1F: True melee attack - use area-based attack (cone/arc in front of player).
            // Determine facing direction based on cursor position for aiming.
            var dir = transform.forward;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var aimHit, 200f, ~0, QueryTriggerInteraction.Collide))
            {
                var to = aimHit.point - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                    dir = to.normalized;
            }
            else
            {
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    dir.Normalize();
            }

            // Use OverlapSphere to find all targets within weapon range (true melee).
            var origin = transform.position;
            origin.y = 0f;
            var weaponRange = Mathf.Max(0.5f, s.rangeMeters);

            var hits = Physics.OverlapSphere(origin, weaponRange, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return;

            // Patch 7.1F: Use a cone/arc filter - only hit targets within ~120 degree arc in front.
            const float coneAngleDegrees = 120f;
            var coneAngleCos = Mathf.Cos(coneAngleDegrees * 0.5f * Mathf.Deg2Rad);

            Health target = null;
            float closestDist = float.MaxValue;

            foreach (var c in hits)
            {
                if (c == null || c.isTrigger)
                    continue;

                var hh = c.GetComponentInParent<Health>();
                if (hh == null || hh.IsDead)
                    continue;
                if (hh.GetComponent<TacticalPlayerController>() != null)
                    continue;

                // Check if target is within the attack cone.
                var targetPos = hh.transform.position;
                targetPos.y = 0f;
                var toTarget = targetPos - origin;
                var dist = toTarget.magnitude;

                if (dist > weaponRange)
                    continue;
                if (dist < 0.01f)
                    continue; // Too close (self)

                var dot = Vector3.Dot(dir, toTarget.normalized);
                if (dot < coneAngleCos)
                    continue; // Outside the attack arc

                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = hh;
                }
            }

            if (target == null)
                return;

            // Track damage dealt
            var wasAlive = !target.IsDead;
            target.ApplyDamage(s.damage, gameObject);
            PlayerInventoryService.Instance.ConsumeEquippedDurability(1);

            // Record stats
            var tracker = CombatStatsTracker.Instance;
            if (tracker != null)
            {
                tracker.RecordDamageDealt(s.damage, target.gameObject);
                if (wasAlive && target.IsDead)
                {
                    var isPlayer = target.GetComponent<TacticalPlayerController>() != null;
                    tracker.RecordKill(target.gameObject, isPlayer);
                }
            }
        }

        private void TryMelee()
        {
            if (Time.unscaledTime < _nextMeleeTime)
                return;

            _nextMeleeTime = Time.unscaledTime + Mathf.Max(0f, meleeCooldown);

            var origin = transform.position;
            origin.y = 0f;

            // Milestone 7.4: Use cone-filtered melee like weapon melee.
            // Determine facing direction from player forward or mouse aim.
            var dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                dir.Normalize();
            else
                dir = Vector3.forward;

            var cam = Camera.main;
            if (cam != null)
            {
                var ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var aimHit, 200f, ~0, QueryTriggerInteraction.Collide))
                {
                    var to = aimHit.point - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.001f)
                        dir = to.normalized;
                }
            }

            var hits = Physics.OverlapSphere(origin, meleeRange, ~0, QueryTriggerInteraction.Ignore);

            // Cone filter: 120 degree arc in front of player.
            const float coneAngleDegrees = 120f;
            var coneAngleCos = Mathf.Cos(coneAngleDegrees * 0.5f * Mathf.Deg2Rad);

            Health best = null;
            var bestDist = float.MaxValue;
            foreach (var c in hits)
            {
                if (c == null)
                    continue;
                var h = c.GetComponentInParent<Health>();
                if (h == null || h.IsDead)
                    continue;
                if (h.GetComponent<TacticalPlayerController>() != null)
                    continue;

                var targetPos = h.transform.position;
                targetPos.y = 0f;
                var toTarget = targetPos - origin;
                var d = toTarget.magnitude;

                if (d > meleeRange || d < 0.01f)
                    continue;

                // Milestone 7.4: Check if target is within the attack cone.
                var dot = Vector3.Dot(dir, toTarget.normalized);
                if (dot < coneAngleCos)
                    continue; // Outside the attack arc

                if (d < bestDist)
                {
                    bestDist = d;
                    best = h;
                }
            }

            if (best != null)
            {
                // Track damage dealt
                var wasAlive = !best.IsDead;
                best.ApplyDamage(meleeDamage, gameObject);

                // Record stats
                var tracker = CombatStatsTracker.Instance;
                if (tracker != null)
                {
                    tracker.RecordDamageDealt(meleeDamage, best.gameObject);
                    if (wasAlive && best.IsDead)
                    {
                        var isPlayer = best.GetComponent<TacticalPlayerController>() != null;
                        tracker.RecordKill(best.gameObject, isPlayer);
                    }
                }
            }
        }

        private bool IsHarvestingTargetUnderCursor()
        {
            var cam = Camera.main;
            if (cam == null)
                return false;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 200f, ~0, QueryTriggerInteraction.Collide))
                return false;

            var node = hit.collider != null ? hit.collider.GetComponentInParent<HarvestNode>() : null;
            if (node == null)
                return false;

            var playerPos = transform.position;
            playerPos.y = 0f;
            var hitPos = hit.point;
            hitPos.y = 0f;

            return Vector3.Distance(playerPos, hitPos) <= harvestRange;
        }
    }
}

