using UnityEngine;

namespace Frontline.Missions
{
    /// <summary>
    /// A trigger zone that reports when a player enters.
    /// Used for "reach_location" mission objectives.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class MissionLocationTrigger : MonoBehaviour
    {
        [SerializeField] private string locationId = "training_range";
        [SerializeField] private string displayName = "Training Range";
        [SerializeField] private bool showPrompt = true;
        [SerializeField] private bool oneTimeOnly = true;

        private bool _hasTriggered;
        private bool _playerInside;

        public string LocationId => locationId;
        public string DisplayName => displayName;
        public bool PlayerInside => _playerInside;

        private void Awake()
        {
            // Ensure collider is a trigger
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if it's the player
            if (!other.CompareTag("Player") && other.GetComponent<CharacterController>() == null)
                return;

            _playerInside = true;

            if (oneTimeOnly && _hasTriggered)
                return;

            _hasTriggered = true;

            // Report to mission system
            var missionService = MissionService.Instance;
            if (missionService != null)
            {
                missionService.ReportProgress("reach_location", locationId, 1);
                Debug.Log($"MissionLocationTrigger: Player reached '{locationId}' ({displayName})");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") && other.GetComponent<CharacterController>() == null)
                return;

            _playerInside = false;
        }

        /// <summary>
        /// Resets the trigger so it can fire again.
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }

        private void OnDrawGizmos()
        {
            // Draw a visual representation in the editor
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);

            var col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}
