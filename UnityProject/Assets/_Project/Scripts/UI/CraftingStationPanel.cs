using System.Collections.Generic;
using System.Linq;
using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.World;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Minimal IMGUI window for world crafting stations (Milestone 4).
    /// Reuses ToolRecipes + CraftingService; only filters by station type.
    /// </summary>
    public sealed class CraftingStationPanel : MonoBehaviour
    {
        private const string ModalId = "crafting_station";

        public static CraftingStationPanel Instance { get; private set; }

        [SerializeField] private bool visible;

        private CraftingStation _activeStation;
        private Vector2 _scroll;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsOpen => visible && _activeStation != null;

        public void Open(CraftingStation station)
        {
            _activeStation = station;
            visible = station != null;
            _scroll = Vector2.zero;

            if (visible && UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterOpen(ModalId, Close, openedByInteract: true);
        }

        public void Close()
        {
            visible = false;
            _activeStation = null;

            if (UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterClosed(ModalId);
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            // Universal close rules are handled by UiModalManager.
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;
            if (PlayerInventoryService.Instance == null)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(520, Screen.width - 20);
            var panelHeight = Mathf.Min(720, Screen.height - 20);
            var rect = new Rect((Screen.width - panelWidth) * 0.5f, pad, panelWidth, panelHeight);

            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label($"{_activeStation.DisplayName} (E toggles, Esc closes)");

            GUILayout.Space(6);
            DrawResources();

            GUILayout.Space(6);
            GUILayout.Label("Crafting:");
            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawRecipesForStation();
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Close"))
                Close();

            GUILayout.EndArea();
        }

        private void DrawResources()
        {
            GUILayout.Label("Resources:");
            var inv = PlayerInventoryService.Instance;
            foreach (var id in new[] { ToolRecipes.Wood, ToolRecipes.Stone, ToolRecipes.Iron, ToolRecipes.Coal, ToolRecipes.Diesel })
                GUILayout.Label($"- {id}: {inv.GetResource(id)}");
        }

        private void DrawRecipesForStation()
        {
            var inv = PlayerInventoryService.Instance;
            var stationType = _activeStation.StationType;

            foreach (var recipe in ToolRecipes.All.Where(r => r != null && r.stationType == stationType))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(recipe.displayName, GUILayout.Width(170));
                GUILayout.Label(CostString(recipe.costs), GUILayout.Width(200));

                var canCraft = inv.CanAfford(recipe.costs);
                var prev = GUI.enabled;
                GUI.enabled = canCraft;
                if (GUILayout.Button("Craft", GUILayout.Width(70)))
                    CraftingService.TryCraft(recipe);
                GUI.enabled = prev;

                GUILayout.EndHorizontal();
            }
        }

        private static string CostString(List<ToolRecipe.Cost> costs)
        {
            if (costs == null || costs.Count == 0)
                return "";
            return string.Join(", ", costs.Select(c => $"{c.amount} {c.resourceId}"));
        }
    }
}

