using Frontline.UI;
using Frontline.World;
using UnityEngine;

namespace Frontline.Missions
{
    /// <summary>
    /// A training target that reports hits to the MissionService.
    /// Used for rifle training and other target-practice missions.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class TrainingTarget : MonoBehaviour
    {
        [SerializeField] private int maxHp = 50;
        [SerializeField] private string targetId = "training_target";
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private bool autoRespawn = true;

        private Health _health;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private bool _isDestroyed;

        public string TargetId => targetId;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _health.Configure(Mathf.Max(1, maxHp), destroyOnDeath: false);
            _health.Died += HandleTargetHit;

            _originalPosition = transform.position;
            _originalRotation = transform.rotation;

            // Add visual feedback
            if (GetComponent<WorldHealthPipBar>() == null)
                gameObject.AddComponent<WorldHealthPipBar>();
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= HandleTargetHit;
        }

        private void HandleTargetHit(Health health)
        {
            if (_isDestroyed)
                return;

            _isDestroyed = true;

            // Report to mission system
            var missionService = MissionService.Instance;
            if (missionService != null)
            {
                missionService.ReportProgress("hit_target", targetId, 1);
                Debug.Log($"TrainingTarget: Hit reported for '{targetId}'");
            }

            // Visual feedback - "fall over" or shrink
            StartCoroutine(DestroyAnimation());
        }

        private System.Collections.IEnumerator DestroyAnimation()
        {
            // Simple animation: shrink and fall
            var elapsed = 0f;
            var duration = 0.5f;
            var startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                transform.Rotate(Vector3.forward, 180f * Time.deltaTime);
                yield return null;
            }

            // Hide
            gameObject.SetActive(false);

            if (autoRespawn)
            {
                yield return new WaitForSeconds(respawnDelay);
                Respawn();
            }
        }

        /// <summary>
        /// Respawns the target at its original position.
        /// </summary>
        public void Respawn()
        {
            _isDestroyed = false;
            transform.position = _originalPosition;
            transform.rotation = _originalRotation;
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);

            if (_health != null)
                _health.Configure(Mathf.Max(1, maxHp), destroyOnDeath: false);
        }
    }
}
