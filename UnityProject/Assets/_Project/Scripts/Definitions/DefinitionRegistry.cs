using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontline.Definitions
{
    /// <summary>
    /// Loads and validates core gameplay definitions.
    /// For now, definitions are shipped as a JSON TextAsset in Resources to avoid editor-time assets.
    /// </summary>
    public sealed class DefinitionRegistry : MonoBehaviour
    {
        public static DefinitionRegistry Instance { get; private set; }

        [Header("Resources")]
        [SerializeField] private string resourcesJsonPath = "definitions";

        public GameDefinitions Definitions { get; private set; } = new();

        private readonly Dictionary<string, string> _idToType = new(StringComparer.Ordinal);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadFromResources();
            ValidateOrThrow();
        }

        public bool IsKnownId(string id) => !string.IsNullOrWhiteSpace(id) && _idToType.ContainsKey(id);

        public string GetTypeForId(string id)
        {
            return _idToType.TryGetValue(id, out var t) ? t : "";
        }

        public IEnumerable<string> AllDefinitionIds()
        {
            return _idToType.Keys.OrderBy(x => x, StringComparer.Ordinal);
        }

        private void LoadFromResources()
        {
            var ta = Resources.Load<TextAsset>(resourcesJsonPath);
            if (ta == null)
                throw new InvalidOperationException($"Missing definitions JSON at Resources/{resourcesJsonPath}.json");

            Definitions = JsonUtility.FromJson<GameDefinitions>(ta.text) ?? new GameDefinitions();

            _idToType.Clear();
            Index(Definitions.materials.Select(x => x.id), "material");
            Index(Definitions.items.Select(x => x.id), "item");
            Index(Definitions.weapons.Select(x => x.id), "weapon");
            Index(Definitions.structures.Select(x => x.id), "structure");
            Index(Definitions.vehicles.Select(x => x.id), "vehicle");
        }

        private void Index(IEnumerable<string> ids, string type)
        {
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                _idToType[id] = type;
            }
        }

        private void ValidateOrThrow()
        {
            if (Definitions.materials.Count == 0)
                throw new InvalidOperationException("Definitions missing materials.");

            var allIds = _idToType.Keys.ToList();
            if (allIds.Count == 0)
                throw new InvalidOperationException("Definitions contain no IDs.");

            var dupes = allIds
                .GroupBy(x => x, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (dupes.Count > 0)
                throw new InvalidOperationException($"Duplicate definition IDs: {string.Join(", ", dupes)}");

            // Validate craft costs reference real materials
            var materialIds = new HashSet<string>(Definitions.materials.Select(m => m.id), StringComparer.Ordinal);
            foreach (var w in Definitions.weapons)
                ValidateCosts(w.id, w.craftCosts, materialIds);
            foreach (var s in Definitions.structures)
                ValidateCosts(s.id, s.craftCosts, materialIds);
            foreach (var v in Definitions.vehicles)
                ValidateCosts(v.id, v.craftCosts, materialIds);
        }

        private static void ValidateCosts(string ownerId, List<CraftCost> costs, HashSet<string> materialIds)
        {
            foreach (var c in costs)
            {
                if (!materialIds.Contains(c.materialId))
                    throw new InvalidOperationException($"Definition '{ownerId}' references unknown material '{c.materialId}'.");
                if (c.amount <= 0)
                    throw new InvalidOperationException($"Definition '{ownerId}' has non-positive craft cost for '{c.materialId}'.");
            }
        }
    }
}

