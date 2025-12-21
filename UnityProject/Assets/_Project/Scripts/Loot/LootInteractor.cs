using Frontline.UI;
using UnityEngine;

namespace Frontline.Loot
{
    /// <summary>
    /// Minimal player loot interaction:
    /// - prompt within 1m
    /// - press E to open LootWindow
    /// </summary>
    public sealed class LootInteractor : MonoBehaviour
    {
        [SerializeField] private float lootRange = 1.0f;
        [SerializeField] private KeyCode lootKey = KeyCode.E;

        private LootPickup _nearest;

        private void Update()
        {
            if (LootWindowPanel.Instance != null && LootWindowPanel.Instance.IsOpen)
                return;

            _nearest = FindNearestLoot();
            if (_nearest == null)
                return;

            if (Input.GetKeyDown(lootKey))
            {
                if (LootWindowPanel.Instance != null)
                    LootWindowPanel.Instance.Open(_nearest);
            }
        }

        private void OnGUI()
        {
            if (LootWindowPanel.Instance != null && LootWindowPanel.Instance.IsOpen)
                return;

            if (_nearest == null)
                return;

            var label = $"{lootKey}: Loot";
            var size = GUI.skin.label.CalcSize(new GUIContent(label));
            var rect = new Rect((Screen.width - size.x) * 0.5f, Screen.height - 88, size.x + 12, 24);
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 6, rect.y + 3, rect.width - 12, rect.height - 6), label);
        }

        private LootPickup FindNearestLoot()
        {
            var playerPos = transform.position;
            playerPos.y = 0f;

            LootPickup best = null;
            var bestDist = float.MaxValue;

            foreach (var loot in Object.FindObjectsByType<LootPickup>(FindObjectsSortMode.None))
            {
                if (loot == null)
                    continue;
                var p = loot.transform.position;
                p.y = 0f;
                var d = Vector3.Distance(playerPos, p);
                if (d > lootRange)
                    continue;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = loot;
                }
            }

            return best;
        }
    }
}

