using Frontline.Economy;
using Frontline.Definitions;
using UnityEngine;

namespace Frontline.World
{
    /// <summary>
    /// Bridges in-world destruction to the closed-economy DestroyedPool.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class Destructible : MonoBehaviour
    {
        [Tooltip("Definition ID that this object represents (weapon/item/structure/vehicle).")]
        [SerializeField] private string definitionId = "";

        private Health _health;
        private bool _registered;

        public string DefinitionId => definitionId;

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

        public void SetDefinitionId(string id)
        {
            definitionId = id ?? "";
        }

        private void OnDied(Health h)
        {
            if (_registered)
                return;
            _registered = true;

            if (DestroyedPoolService.Instance == null)
            {
                Debug.LogWarning($"Destructible '{name}' died but DestroyedPoolService is missing.");
                return;
            }

            if (DefinitionRegistry.Instance != null && !DefinitionRegistry.Instance.IsKnownId(definitionId))
            {
                Debug.LogWarning($"Destructible '{name}' has unknown definitionId '{definitionId}'.");
                return;
            }

            DestroyedPoolService.Instance.RegisterDestroyed(definitionId, 1);
        }
    }
}

