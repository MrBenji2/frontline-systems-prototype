using System;
using UnityEngine;
using Frontline.World;

namespace Frontline.Buildables
{
    [RequireComponent(typeof(Health))]
    public sealed class Buildable : MonoBehaviour
    {
        [SerializeField] private string itemId = "";
        [SerializeField] private int ownerTeam;

        private Health _health;

        public string ItemId => itemId;
        public int OwnerTeam => ownerTeam;
        public Health Health => _health;

        public event Action<Buildable> Died;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        public void Configure(string id, int maxHp, int team)
        {
            itemId = id ?? "";
            ownerTeam = team;
            _health.Configure(Mathf.Max(1, maxHp), true);
        }

        public void SetCurrentHpForLoad(int currentHp)
        {
            if (_health == null)
                _health = GetComponent<Health>();
            _health.SetCurrentHpForLoad(currentHp);
        }

        private void OnDied(Health h)
        {
            Died?.Invoke(this);
        }
    }
}

