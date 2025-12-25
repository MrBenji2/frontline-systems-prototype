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
        [SerializeField] private string ownerId = "player";

        private Health _health;

        public string ItemId => itemId;
        public int OwnerTeam => ownerTeam;
        public string OwnerId => ownerId;
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
            ownerId = "player";
            _health.Configure(Mathf.Max(1, maxHp), true);
        }

        public void SetOwnerForLoad(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                ownerId = id;
        }

        // Milestone 5.3 foundation: ownership/authorization hooks (single-player always true for now).
        public bool CanInteract(string actorId) => true;
        public bool CanDamage(string actorId) => true;

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

