using Frontline.Harvesting;
using Frontline.Tactical;
using Frontline.Buildables;
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

        private void Update()
        {
            if (BuildablesService.Instance != null && BuildablesService.Instance.IsInputLockedForCombatOrHarvest)
                return;

            if (Input.GetMouseButton(0))
                TryRanged();
            if (Input.GetMouseButton(1))
                TryMelee();
        }

        private void OnGUI()
        {
            if (BuildablesService.Instance != null && BuildablesService.Instance.IsInputLockedForCombatOrHarvest)
                return;

            var rect = new Rect(10, Screen.height - 34, 220, 24);
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 6, rect.y + 3, rect.width - 12, rect.height - 6), "Combat: LMB shoot / RMB melee");
        }

        private void TryRanged()
        {
            if (Time.unscaledTime < _nextRangedTime)
                return;

            if (IsHarvestingTargetUnderCursor())
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

            h.ApplyDamage(rangedDamage, gameObject);
        }

        private void TryMelee()
        {
            if (Time.unscaledTime < _nextMeleeTime)
                return;

            _nextMeleeTime = Time.unscaledTime + Mathf.Max(0f, meleeCooldown);

            var origin = transform.position;
            origin.y = 0f;

            var hits = Physics.OverlapSphere(origin, meleeRange, ~0, QueryTriggerInteraction.Ignore);
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

                var p = h.transform.position;
                p.y = 0f;
                var d = Vector3.Distance(origin, p);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = h;
                }
            }

            if (best != null)
                best.ApplyDamage(meleeDamage, gameObject);
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

