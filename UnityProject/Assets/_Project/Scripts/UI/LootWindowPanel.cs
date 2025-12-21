using Frontline.Crafting;
using Frontline.Gameplay;
using Frontline.Loot;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Minimal IMGUI loot window (Milestone 4).
    /// </summary>
    public sealed class LootWindowPanel : MonoBehaviour
    {
        public static LootWindowPanel Instance { get; private set; }

        [SerializeField] private bool visible;
        private LootPickup _active;

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

        public bool IsOpen => visible && _active != null;

        public void Open(LootPickup pickup)
        {
            _active = pickup;
            visible = pickup != null;
        }

        public void Close()
        {
            visible = false;
            _active = null;
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;
            if (PlayerInventoryService.Instance == null)
                return;

            const int pad = 10;
            var panelWidth = Mathf.Min(520, Screen.width - 20);
            var panelHeight = Mathf.Min(260, Screen.height - 20);
            var rect = new Rect((Screen.width - panelWidth) * 0.5f, pad, panelWidth, panelHeight);

            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("Loot");

            GUILayout.Space(6);
            GUILayout.Label($"- {_active.ItemId} x{Mathf.Max(1, _active.Quantity)}");

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Loot All", GUILayout.Width(120)))
                LootAll();
            if (GUILayout.Button("Close", GUILayout.Width(120)))
                Close();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void LootAll()
        {
            if (_active == null)
                return;
            if (PlayerInventoryService.Instance == null)
                return;

            var id = _active.ItemId;
            if (string.IsNullOrWhiteSpace(id))
            {
                Destroy(_active.gameObject);
                Close();
                return;
            }

            // If it's a tool recipe, add tool with full durability.
            var recipe = ToolRecipes.Get(id);
            if (recipe != null)
            {
                PlayerInventoryService.Instance.AddTool(recipe.itemId, recipe.maxDurability, recipe.toolType, recipe.tier, recipe.hitDamage);
            }
            else if (id.StartsWith("mat_"))
            {
                // Minimal support for material IDs (in case they enter DestroyedPool via dev tools).
                PlayerInventoryService.Instance.AddResource(id, Mathf.Max(1, _active.Quantity));
            }
            else
            {
                Debug.LogWarning($"Loot '{id}' not supported by PlayerInventoryService (ignored).");
            }

            Destroy(_active.gameObject);
            Close();
        }
    }
}

