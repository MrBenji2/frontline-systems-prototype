using Frontline.Crafting;
using Frontline.Buildables;
using Frontline.Gameplay;
using UnityEngine;

namespace Frontline.Harvesting
{
    /// <summary>
    /// Minimal player interaction for Milestone 3:
    /// - Hold LMB to use equipped tool via raycast (includes triggers).
    /// - Hotkeys 1-5 equip best tool of each type.
    /// </summary>
    public sealed class TacticalHarvestInteractor : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private float harvestRange = 3.0f;
        [SerializeField] private float hitsPerSecond = 6f;

        private float _nextHitTime;

        private void Update()
        {
            if (BuildablesService.Instance != null && BuildablesService.Instance.IsInputLockedForCombatOrHarvest)
                return;

            HandleHotkeys();

            if (!Input.GetMouseButton(0))
                return;

            var interval = hitsPerSecond <= 0f ? 0f : (1f / hitsPerSecond);
            if (interval > 0f && Time.unscaledTime < _nextHitTime)
                return;
            _nextHitTime = Time.unscaledTime + interval;

            TryHarvestUnderCursor();
        }

        private void HandleHotkeys()
        {
            if (PlayerInventoryService.Instance == null)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
                PlayerInventoryService.Instance.EquipBestOfType(ToolType.Axe);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                PlayerInventoryService.Instance.EquipBestOfType(ToolType.Shovel);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                PlayerInventoryService.Instance.EquipBestOfType(ToolType.Wrench);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                PlayerInventoryService.Instance.EquipBestOfType(ToolType.Hammer);
            if (Input.GetKeyDown(KeyCode.Alpha5))
                PlayerInventoryService.Instance.EquipBestOfType(ToolType.GasCan);
        }

        private void TryHarvestUnderCursor()
        {
            if (PlayerInventoryService.Instance == null)
                return;

            var tool = PlayerInventoryService.Instance.EquippedTool;
            if (tool == null)
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                return;

            var node = hit.collider != null ? hit.collider.GetComponentInParent<HarvestNode>() : null;
            if (node == null)
                return;

            var playerPos = transform.position;
            playerPos.y = 0f;
            var hitPos = hit.point;
            hitPos.y = 0f;
            if (Vector3.Distance(playerPos, hitPos) > harvestRange)
                return;

            var didHit = node.ApplyHarvestHit(tool.toolType, tool.hitDamage, hit.point);
            if (!didHit)
                return;

            // Consume durability only on a successful hit.
            PlayerInventoryService.Instance.ConsumeEquippedDurability(1);
        }
    }
}

