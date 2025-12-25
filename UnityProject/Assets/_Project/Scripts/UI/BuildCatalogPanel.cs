using System.Collections.Generic;
using System.Linq;
using Frontline.Buildables;
using Frontline.Definitions;
using Frontline.Gameplay;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Milestone 5.3: Build Catalog window (browse buildables + metadata).
    /// Keybind: V toggles while in Construction Mode (Build Mode).
    /// Esc closes (via UiModalManager).
    /// </summary>
    public sealed class BuildCatalogPanel : MonoBehaviour
    {
        private const string ModalId = "build_catalog";

        public static BuildCatalogPanel Instance { get; private set; }

        [SerializeField] private bool visible;
        [SerializeField] private KeyCode toggleKey = KeyCode.V;

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private int _selectedIdx;

        private readonly Dictionary<string, Texture2D> _icons = new();

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

        public bool IsOpen => visible;

        private void Update()
        {
            // Only available while in Construction Mode (Build Mode).
            if (BuildablesService.Instance == null || !BuildablesService.Instance.IsBuildModeActive)
            {
                if (visible)
                    Close();
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                if (visible)
                    Close();
                else
                    Open();
            }
        }

        public void Open()
        {
            visible = true;
            if (UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterOpen(ModalId, Close, openedByInteract: false);
        }

        public void Close()
        {
            visible = false;
            if (UiModalManager.Instance != null)
                UiModalManager.Instance.RegisterClosed(ModalId);
        }

        private void OnGUI()
        {
            if (!visible)
                return;
            if (DefinitionRegistry.Instance == null)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(860, Screen.width - 20);
            var panelHeight = Mathf.Min(560, Screen.height - 20);
            var rect = new Rect((Screen.width - panelWidth) * 0.5f, pad, panelWidth, panelHeight);

            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("BUILD CATALOG (V)");

            var buildables = GetBuildableDefs();
            if (buildables.Count == 0)
            {
                GUILayout.Label("(no buildables)");
                GUILayout.EndArea();
                return;
            }

            _selectedIdx = Mathf.Clamp(_selectedIdx, 0, buildables.Count - 1);
            var selected = buildables[_selectedIdx];

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();

            // Left list.
            GUILayout.BeginVertical(GUILayout.Width(260));
            _leftScroll = GUILayout.BeginScrollView(_leftScroll);
            for (var i = 0; i < buildables.Count; i++)
            {
                var def = buildables[i];
                var locked = !IsUnlocked(def);

                var label = def.displayName + (locked ? " (LOCKED)" : "");
                var prev = GUI.enabled;
                GUI.enabled = !locked;
                if (GUILayout.Button(label, GUILayout.Height(44)))
                {
                    _selectedIdx = i;
                    if (BuildablesService.Instance != null)
                    {
                        BuildablesService.Instance.SetSelectedBuildItem(def.id);
                        SelectionUIState.SetSelected($"Selected: {def.id}");
                    }
                }
                GUI.enabled = prev;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Right detail.
            GUILayout.BeginVertical();
            _rightScroll = GUILayout.BeginScrollView(_rightScroll);

            GUILayout.BeginHorizontal();
            GUILayout.Box(GetIcon(selected.id), GUILayout.Width(64), GUILayout.Height(64));
            GUILayout.BeginVertical();
            GUILayout.Label($"{selected.displayName}");
            GUILayout.Label(selected.id);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"Cost: {CostString(selected.craftCosts)}");
            GUILayout.Label($"Built at: {StationsString(selected.builtAtStations)}");
            GUILayout.Label($"Required skill: {(string.IsNullOrWhiteSpace(selected.requiredSkillId) ? "(none)" : selected.requiredSkillId)}");
            if (!string.IsNullOrWhiteSpace(selected.description))
                GUILayout.Label(selected.description);

            if (!IsUnlocked(selected))
                GUILayout.Label("Locked");
            else if (!CanAfford(selected))
                GUILayout.Label("Insufficient materials (selection allowed, placement will be blocked)");

            GUILayout.Space(8);
            GUILayout.Label("Click an entry on the left to select.");

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            if (GUILayout.Button("Close", GUILayout.Width(120)))
                Close();

            GUILayout.EndArea();
        }

        private static List<StructureDef> GetBuildableDefs()
        {
            var defs = DefinitionRegistry.Instance.Definitions.structures;
            return defs
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.id) && s.id.StartsWith("build_"))
                .OrderBy(s => s.id, System.StringComparer.Ordinal)
                .ToList();
        }

        private static bool CanAfford(StructureDef def)
        {
            if (PlayerInventoryService.Instance == null)
                return false;
            if (def == null || def.craftCosts == null)
                return true;
            var costs = def.craftCosts
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.materialId) && c.amount > 0)
                .Select(c => new Crafting.ToolRecipe.Cost { resourceId = c.materialId, amount = c.amount })
                .ToList();
            return PlayerInventoryService.Instance.CanAfford(costs);
        }

        private static bool IsUnlocked(StructureDef def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.requiredSkillId))
                return true;
            return PlayerSkillsService.Instance != null && PlayerSkillsService.Instance.HasSkill(def.requiredSkillId);
        }

        private Texture2D GetIcon(string id)
        {
            id ??= "";
            if (_icons.TryGetValue(id, out var tex) && tex != null)
                return tex;

            var c = id switch
            {
                "build_foundation" => new Color(0.55f, 0.55f, 0.58f),
                "build_wall" => new Color(0.53f, 0.34f, 0.18f),
                "build_gate" => new Color(0.45f, 0.32f, 0.18f),
                "build_storage" => new Color(0.60f, 0.42f, 0.20f),
                "build_ramp" => new Color(0.55f, 0.40f, 0.20f),
                _ => new Color(0.75f, 0.75f, 0.75f),
            };

            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, c);
            tex.Apply(false, true);
            _icons[id] = tex;
            return tex;
        }

        private static string CostString(List<CraftCost> costs)
        {
            if (costs == null || costs.Count == 0)
                return "(none)";
            return string.Join(", ", costs.Where(c => c != null).Select(c => $"{c.amount} {c.materialId}"));
        }

        private static string StationsString(List<string> stations)
        {
            if (stations == null || stations.Count == 0)
                return "(any)";
            return string.Join(", ", stations.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}

