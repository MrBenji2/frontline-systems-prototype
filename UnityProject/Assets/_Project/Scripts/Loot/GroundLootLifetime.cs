using Frontline.Economy;
using UnityEngine;

namespace Frontline.Loot
{
    /// <summary>
    /// Milestone 7.2: Ground loot lifetime and salvage conversion.
    /// Dropped items on the ground have a lifetime. After timeout, they convert to Salvage.
    /// </summary>
    public sealed class GroundLootLifetime : MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField] private float lifetimeSeconds = 120f; // 2 minutes default
        [SerializeField] private bool convertToSalvage = true;

        [Header("Salvage Settings")]
        [SerializeField] private string salvageResourceId = "mat_salvage";
        [SerializeField] private int salvageAmount = 1;

        private float _spawnTime;
        private LootPickup _loot;

        /// <summary>
        /// Gets the remaining lifetime in seconds.
        /// </summary>
        public float RemainingLifetime => Mathf.Max(0f, lifetimeSeconds - (Time.time - _spawnTime));

        /// <summary>
        /// Gets the lifetime as a ratio (0 = expired, 1 = just spawned).
        /// </summary>
        public float LifetimeRatio => Mathf.Clamp01(RemainingLifetime / lifetimeSeconds);

        private void Awake()
        {
            _spawnTime = Time.time;
            _loot = GetComponent<LootPickup>();
        }

        private void Update()
        {
            if (Time.time - _spawnTime < lifetimeSeconds)
                return;

            // Lifetime expired.
            OnLifetimeExpired();
        }

        private void OnLifetimeExpired()
        {
            if (convertToSalvage)
            {
                ConvertToSalvage();
            }
            else
            {
                // Just destroy the loot.
                DestroyLoot();
            }
        }

        private void ConvertToSalvage()
        {
            // Register original item as destroyed (if applicable).
            if (_loot != null && !string.IsNullOrWhiteSpace(_loot.ItemId))
            {
                if (DestroyedPoolService.Instance != null)
                {
                    DestroyedPoolService.Instance.RegisterDestroyed(_loot.ItemId, _loot.Quantity);
                }

                Debug.Log($"[GroundLoot] '{_loot.ItemId}' x{_loot.Quantity} expired -> Salvage x{salvageAmount}");
            }

            // Spawn salvage pickup at same location.
            var pos = transform.position;
            SalvagePickup.Spawn(pos, salvageAmount);

            // Destroy this loot.
            Destroy(gameObject);
        }

        private void DestroyLoot()
        {
            // Register as destroyed.
            if (_loot != null && !string.IsNullOrWhiteSpace(_loot.ItemId))
            {
                if (DestroyedPoolService.Instance != null)
                {
                    DestroyedPoolService.Instance.RegisterDestroyed(_loot.ItemId, _loot.Quantity);
                }

                Debug.Log($"[GroundLoot] '{_loot.ItemId}' x{_loot.Quantity} expired (destroyed)");
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Resets the lifetime (e.g., when player interacts with the loot).
        /// </summary>
        public void RefreshLifetime()
        {
            _spawnTime = Time.time;
        }

        /// <summary>
        /// Sets custom lifetime values.
        /// </summary>
        public void Configure(float lifetime, bool toSalvage = true, int salvageAmt = 1)
        {
            lifetimeSeconds = Mathf.Max(1f, lifetime);
            convertToSalvage = toSalvage;
            salvageAmount = Mathf.Max(1, salvageAmt);
            _spawnTime = Time.time;
        }
    }
}
