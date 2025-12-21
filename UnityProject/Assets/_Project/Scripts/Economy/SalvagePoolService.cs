using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Definitions;
using UnityEngine;

namespace Frontline.Economy
{
    /// <summary>
    /// Approach B: SalvagePool stores resource credits created from broken tools (50% of recipe cost).
    /// These credits are NOT granted to the player; they are stored for later loot eligibility rules.
    /// </summary>
    public sealed class SalvagePoolService : MonoBehaviour
    {
        public static SalvagePoolService Instance { get; private set; }

        public event Action Changed;

        private readonly Dictionary<string, int> _credits = new(StringComparer.Ordinal);
        private string SavePath => Path.Combine(Application.persistentDataPath, "salvage_pool.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromDisk();
        }

        public IReadOnlyDictionary<string, int> GetAllCredits() => _credits;

        public int GetCredits(string resourceId)
        {
            return _credits.TryGetValue(resourceId, out var a) ? a : 0;
        }

        public void AddCredits(string resourceId, int amount)
        {
            if (!IsValidId(resourceId))
                return;
            if (amount <= 0)
                return;

            _credits[resourceId] = GetFrom(_credits, resourceId) + amount;
            SaveToDisk();
            Changed?.Invoke();
        }

        public void ResetAll()
        {
            _credits.Clear();
            SaveToDisk();
            Changed?.Invoke();
        }

        private static int GetFrom(Dictionary<string, int> dict, string id)
        {
            return dict.TryGetValue(id, out var a) ? a : 0;
        }

        private static bool IsValidId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            if (DefinitionRegistry.Instance == null)
                return true;
            return DefinitionRegistry.Instance.IsKnownId(id);
        }

        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return;

                var json = File.ReadAllText(SavePath);
                var snap = JsonUtility.FromJson<SalvagePoolSnapshot>(json);
                if (snap == null)
                    return;

                _credits.Clear();
                foreach (var e in snap.credits)
                    _credits[e.id] = Math.Max(0, e.amount);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SalvagePool: failed to load '{SavePath}': {ex.Message}");
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var snap = new SalvagePoolSnapshot
                {
                    credits = _credits
                        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .Select(kv => new SalvagePoolSnapshot.Entry { id = kv.Key, amount = kv.Value })
                        .ToList()
                };

                var json = JsonUtility.ToJson(snap, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SalvagePool: failed to save '{SavePath}': {ex.Message}");
            }
        }
    }
}

