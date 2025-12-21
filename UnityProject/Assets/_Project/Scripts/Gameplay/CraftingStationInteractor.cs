using Frontline.Tactical;
using Frontline.UI;
using Frontline.World;
using UnityEngine;

namespace Frontline.Gameplay
{
    /// <summary>
    /// Minimal interaction for world crafting stations:
    /// - shows prompt within range
    /// - press E to open station UI
    /// </summary>
    public sealed class CraftingStationInteractor : MonoBehaviour
    {
        [SerializeField] private float useRange = 1.5f;
        [SerializeField] private KeyCode useKey = KeyCode.E;

        private CraftingStation _nearest;

        private void Update()
        {
            if (CraftingStationPanel.Instance != null && CraftingStationPanel.Instance.IsOpen)
                return;

            _nearest = FindNearestStation();
            if (_nearest == null)
                return;

            if (Input.GetKeyDown(useKey))
            {
                if (CraftingStationPanel.Instance != null)
                    CraftingStationPanel.Instance.Open(_nearest);
            }
        }

        private void OnGUI()
        {
            if (CraftingStationPanel.Instance != null && CraftingStationPanel.Instance.IsOpen)
                return;

            if (_nearest == null)
                return;

            var label = $"{useKey}: Use {_nearest.DisplayName}";
            var size = GUI.skin.label.CalcSize(new GUIContent(label));
            var rect = new Rect((Screen.width - size.x) * 0.5f, Screen.height - 60, size.x + 12, 24);
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 6, rect.y + 3, rect.width - 12, rect.height - 6), label);
        }

        private CraftingStation FindNearestStation()
        {
            var playerPos = transform.position;
            playerPos.y = 0f;

            CraftingStation best = null;
            var bestDist = float.MaxValue;

            foreach (var station in Object.FindObjectsByType<CraftingStation>(FindObjectsSortMode.None))
            {
                if (station == null)
                    continue;

                var p = station.transform.position;
                p.y = 0f;
                var d = Vector3.Distance(playerPos, p);
                if (d > useRange)
                    continue;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = station;
                }
            }

            return best;
        }
    }
}

