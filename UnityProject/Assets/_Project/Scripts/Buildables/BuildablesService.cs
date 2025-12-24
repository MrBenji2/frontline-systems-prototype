using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Crafting;
using Frontline.Definitions;
using Frontline.Economy;
using Frontline.Gameplay;
using Frontline.Tactical;
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

        private GameObject _preview;
        private Renderer _previewRenderer;
        private Material _previewMaterial;
        private Vector3 _previewPos;
        private Quaternion _previewRot = Quaternion.identity;
        private bool _previewValid;

        private float _nextRepairTime;

        private bool _dirty;
        private float _nextAutosaveTime;

        private Transform _player;

        private string SavePath => Path.Combine(Application.persistentDataPath, "buildables_world.json");

        public bool IsBuildModeActive => _buildMode;
        public bool IsInputLockedForCombatOrHarvest => _buildMode || (StorageCratePanel.Instance != null && StorageCratePanel.Instance.IsOpen);

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

            if (Input.GetKeyDown(KeyCode.R))
                _rotationSteps = (_rotationSteps + 1) % 4;

            UpdatePreview();

            if (Input.GetMouseButtonDown(0) && _previewValid)
                ConfirmPlacement();
        }

        private void OnGUI()
        {
            var rect = new Rect(10, Screen.height - 58, 320, 48);
            GUI.Box(rect, "");

            var mode = _buildMode ? "Build" : "Combat/Harvest";
            GUI.Label(new Rect(rect.x + 6, rect.y + 5, rect.width - 12, 18), $"Mode: {mode}");
            GUI.Label(new Rect(rect.x + 6, rect.y + 24, rect.width - 12, 18),
                _buildMode
                    ? $"Build: 1 Foundation / 2 Wall / 3 Gate / 4 Storage | R rotate | LMB place | RMB/Esc exit"
                    : $"B: Build Mode | Hammer+LMB: Repair | E: Open Storage Crate");
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
                        stored = new List<BuildablesWorldSnapshot.ItemStack>()
                    };

                    var crate = b.GetComponent<StorageCrate>();
                    if (crate != null)
                        e.stored = crate.ToSnapshot();

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
                b.SetCurrentHpForLoad(e.currentHp);

            var crate = go.GetComponent<StorageCrate>();
            if (crate != null)
                crate.LoadFromSnapshot(e.stored);
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
            EnsurePreview();
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

            if (!TryGetMouseWorld(out var p))
                return;

            var snap = Mathf.Max(0.25f, gridSizeMeters);
            p.x = Mathf.Round(p.x / snap) * snap;
            p.z = Mathf.Round(p.z / snap) * snap;

            GetShape(_selectedItemId, out var scale, out _);
            p.y = scale.y * 0.5f;

            _previewPos = p;
            _previewRot = Quaternion.Euler(0f, 90f * _rotationSteps, 0f);

            _preview.transform.SetPositionAndRotation(_previewPos, _previewRot);

            _previewValid = IsPlacementValid(_selectedItemId, _previewPos, _previewRot);

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

            MarkDirty();
        }

        private GameObject SpawnBuildableGameObject(string itemId, Vector3 pos, Quaternion rot, bool applyEconomy)
        {
            if (string.IsNullOrWhiteSpace(itemId))
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

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = itemId;
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = tint;
            }

            var buildable = go.AddComponent<Buildable>();
            buildable.Configure(itemId, maxHp, team: 0);
            buildable.Died += OnBuildableDied;

            var destructible = go.AddComponent<Destructible>();
            destructible.SetDefinitionId(itemId);

            if (itemId == "build_storage")
            {
                go.AddComponent<StorageCrate>();
            }

            return go;
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

            if (StorageCratePanel.Instance != null)
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
            if (PlayerInventoryService.Instance == null)
                return false;

            var costs = GetBuildCosts(itemId);
            if (!PlayerInventoryService.Instance.CanAfford(costs))
                return false;

            GetShape(itemId, out var scale, out _);
            var half = scale * 0.5f;
            half.y = Mathf.Max(0.1f, half.y);

            // Check for blocking colliders (ignore triggers and non-gameplay colliders).
            var hits = Physics.OverlapBox(pos, half * 0.95f, rot, ~0, QueryTriggerInteraction.Ignore);
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

        private bool TryGetMouseWorld(out Vector3 point)
        {
            point = Vector3.zero;
            var cam = Camera.main;
            if (cam == null)
                return false;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, placementRayDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                point.y = 0f;
                return true;
            }

            // Fallback to XZ plane at y=0
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var enter))
            {
                point = ray.GetPoint(enter);
                point.y = 0f;
                return true;
            }

            return false;
        }

        private static int GetMaxHp(string itemId)
        {
            return itemId switch
            {
                "build_foundation" => 220,
                "build_wall" => 160,
                "build_gate" => 190,
                "build_storage" => 140,
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

