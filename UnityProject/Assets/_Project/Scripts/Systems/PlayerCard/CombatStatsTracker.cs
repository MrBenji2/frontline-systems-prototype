using Frontline.Combat;
using Frontline.Tactical;
using Frontline.World;
using UnityEngine;

namespace Frontline.PlayerCard
{
    /// <summary>
    /// Tracks combat statistics by hooking into the Health system.
    /// Attach to the player or use as a singleton service.
    /// </summary>
    public sealed class CombatStatsTracker : MonoBehaviour
    {
        public static CombatStatsTracker Instance { get; private set; }

        private Health _playerHealth;
        private TacticalPlayerController _playerController;

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

        private void Start()
        {
            // Find and track player health
            FindAndTrackPlayer();
        }

        private void Update()
        {
            // Re-find player if lost
            if (_playerController == null || _playerHealth == null)
            {
                FindAndTrackPlayer();
            }
        }

        private void FindAndTrackPlayer()
        {
            _playerController = FindFirstObjectByType<TacticalPlayerController>();
            if (_playerController == null)
                return;

            _playerHealth = _playerController.GetComponent<Health>();
            if (_playerHealth != null)
            {
                // Subscribe to player damage events
                _playerHealth.Damaged -= OnPlayerDamaged;
                _playerHealth.Damaged += OnPlayerDamaged;

                _playerHealth.Died -= OnPlayerDied;
                _playerHealth.Died += OnPlayerDied;
            }
        }

        private void OnPlayerDamaged(Health health, int amount)
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            statsService.RecordDamageTaken(amount);
        }

        private void OnPlayerDied(Health health)
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            statsService.RecordDeath();
        }

        /// <summary>
        /// Called when the player deals damage to a target.
        /// Call this from combat controllers.
        /// </summary>
        public void RecordDamageDealt(int amount, GameObject target)
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            statsService.RecordDamageDealt(amount);

            // Check if target is an NPC or player
            // Could also check faction for friendly fire
        }

        /// <summary>
        /// Called when the player kills a target.
        /// Call this from Health.Died event handlers.
        /// </summary>
        public void RecordKill(GameObject target, bool isPlayer = false)
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            statsService.RecordKill(isPlayer);
        }

        /// <summary>
        /// Called when the player commits friendly fire.
        /// </summary>
        public void RecordFriendlyFire()
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            statsService.RecordFriendlyFire();
        }

        /// <summary>
        /// Called when the player revives a teammate.
        /// </summary>
        public void RecordRevive()
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            statsService.RecordRevive();
        }

        /// <summary>
        /// Called when the player heals someone.
        /// </summary>
        public void RecordHealing(int amount)
        {
            var statsService = PlayerStatsService.Instance;
            if (statsService == null)
                return;

            statsService.RecordHealing(amount);
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Damaged -= OnPlayerDamaged;
                _playerHealth.Died -= OnPlayerDied;
            }
        }
    }
}
