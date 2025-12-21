using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Frontline.Definitions;
using UnityEngine;

namespace Frontline.Economy
{
    /// <summary>
    /// Tracks "created/crafted" counts for later eligibility rules (Milestone 4+).
    /// This is intentionally separate from DestroyedPoolService (which is the closed-economy gate).
    /// </summary>
    public sealed class CreatedPoolService : MonoBehaviour
    {
        public static CreatedPoolService Instance { get; private set; }

        public event Action Changed;

        private readonly Dictionary<string, int> _createdCounts = new(StringComparer.Ordinal);
        private string SavePath => Path.Combine(Application.persistentDataPath, "created_pool.json");

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

        public IReadOnlyDictionary<string, int> GetAllCreatedCounts() => _createdCounts;

        public int GetCreatedCount(string definitionId)
        {
            return _createdCounts.TryGetValue(definitionId, out var c) ? c : 0;
        }

        public void RegisterCreated(string definitionId, int amount = 1)
        {
            if (!IsValidId(definitionId))
                return;
            if (amount <= 0)
                return;

            _createdCounts[definitionId] = GetFrom(_createdCounts, definitionId) + amount;
            SaveToDisk();
            Changed?.Invoke();
        }

        public void ResetAll()
        {
            _createdCounts.Clear();
            SaveToDisk();
            Changed?.Invoke();
        }

        private static int GetFrom(Dictionary<string, int> dict, string id)
        {
            return dict.TryGetValue(id, out var c) ? c : 0;
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
                var snap = JsonUtility.FromJson<CreatedPoolSnapshot>(json);
                if (snap == null)
                    return;

                _createdCounts.Clear();
                foreach (var e in snap.createdCounts)
                    _createdCounts[e.id] = Math.Max(0, e.count);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CreatedPool: failed to load '{SavePath}': {ex.Message}");
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var snap = new CreatedPoolSnapshot
                {
                    createdCounts = _createdCounts
                        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .Select(kv => new CreatedPoolSnapshot.Entry { id = kv.Key, count = kv.Value })
                        .ToList()
                };

                var json = JsonUtility.ToJson(snap, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CreatedPool: failed to save '{SavePath}': {ex.Message}");
            }
        }
    }
}

