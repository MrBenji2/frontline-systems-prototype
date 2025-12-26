using UnityEngine;

namespace Frontline.Tactical
{
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Rig")]
        [SerializeField] private Vector3 offset = new Vector3(0, 18f, -14f);
        [SerializeField] private float followSharpness = 12f;
        [SerializeField] private float yawDegrees = 0f;
        [SerializeField] private float pitchDegrees = 55f;

        [Header("Milestone 7.3: Camera Lock")]
        [Tooltip("When enabled, camera locks behind the player. A/D strafes instead of rotating.")]
        [SerializeField] private bool cameraLocked = false;
        [SerializeField] private KeyCode toggleLockKey = KeyCode.C;
        [SerializeField] private float lockedYawSmoothing = 8f;

        private float _currentLockedYaw;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        /// Milestone 7.3: Whether the camera is locked behind the player.
        /// </summary>
        public bool IsCameraLocked
        {
            get => cameraLocked;
            set => cameraLocked = value;
        }

        private void Update()
        {
            // Milestone 7.3: Toggle camera lock with C key.
            if (Input.GetKeyDown(toggleLockKey))
            {
                cameraLocked = !cameraLocked;
                if (cameraLocked && target != null)
                {
                    // Initialize locked yaw to current player facing.
                    _currentLockedYaw = target.eulerAngles.y;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            float effectiveYaw;

            if (cameraLocked)
            {
                // Milestone 7.3: Camera locks behind player.
                // Smoothly interpolate to the player's current facing direction.
                var targetYaw = target.eulerAngles.y;
                _currentLockedYaw = Mathf.LerpAngle(_currentLockedYaw, targetYaw, 1f - Mathf.Exp(-lockedYawSmoothing * Time.deltaTime));
                effectiveYaw = _currentLockedYaw;
            }
            else
            {
                // Free camera mode (default).
                effectiveYaw = yawDegrees;
            }

            var yaw = Quaternion.Euler(0f, effectiveYaw, 0f);
            var rotatedOffset = yaw * offset;

            var desiredPos = target.position + rotatedOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            transform.rotation = Quaternion.Euler(pitchDegrees, effectiveYaw, 0f);
        }

        private void OnGUI()
        {
            // Milestone 7.3: Show camera lock status.
            if (!cameraLocked)
                return;

            var rect = new Rect(Screen.width - 160, 10, 150, 24);
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 18), "Camera: LOCKED (C)");
        }
    }
}

