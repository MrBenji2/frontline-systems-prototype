using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontline.Gameplay
{
    /// <summary>
    /// Milestone 5.3 foundation: in-memory player skills registry (debug-grantable).
    /// </summary>
    public sealed class PlayerSkillsService : MonoBehaviour
    {
        public static PlayerSkillsService Instance { get; private set; }

        public event Action Changed;

        private readonly HashSet<string> _skills = new(StringComparer.Ordinal);

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

        public bool HasSkill(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return true; // empty skill means unlocked
            return _skills.Contains(skillId);
        }

        public void GrantSkill(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return;
            if (_skills.Add(skillId))
                Changed?.Invoke();
        }
    }
}

