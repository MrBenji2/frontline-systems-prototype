using UnityEngine;

namespace Frontline.Tactical
{
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Rig")]
        [SerializeField] private Vector3 offset = new Vector3(0, 18f, -14f);
        [SerializeField] private float followSharpness = 12f;
        [SerializeField] private float pitchDegrees = 55f;

        [Header("Camera Lock (C key)")]
        [Tooltip("When enabled, camera locks behind the player. A/D strafes instead of rotating.")]
        [SerializeField] private bool cameraLocked = false;
        [SerializeField] private KeyCode toggleLockKey = KeyCode.C;
        [SerializeField] private float lockedYawSmoothing = 8f;

        [Header("Milestone 7.4: Free Camera (Unlocked)")]
        [Tooltip("Mouse sensitivity for free camera rotation.")]
        [SerializeField] private float mouseSensitivity = 2.0f;

        // Milestone 7.4: Separate yaw tracking for locked and free modes.
        private float _lockedYaw;   // Used when camera is locked (follows player facing)
        private float _freeYaw;     // Used when camera is unlocked (mouse-controlled)

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

        /// <summary>
        /// Milestone 7.4: Current free camera yaw (for external queries).
        /// </summary>
        public float FreeYaw => _freeYaw;

        private void Start()
        {
            // Initialize free yaw to 0 (looking forward on world Z axis).
            _freeYaw = 0f;
            _lockedYaw = target != null ? target.eulerAngles.y : 0f;
        }

        private void Update()
        {
            // Toggle camera lock with C key.
            if (Input.GetKeyDown(toggleLockKey))
            {
                cameraLocked = !cameraLocked;

                if (cameraLocked && target != null)
                {
                    // Switching to locked mode: snap locked yaw to player facing.
                    _lockedYaw = target.eulerAngles.y;
                }
                else
                {
                    // Milestone 7.4: Switching to free mode: preserve current camera angle.
                    // This prevents jarring rotation when unlocking.
                    _freeYaw = _lockedYaw;
                }
            }

            // Milestone 7.4: Handle mouse input for free camera mode.
            if (!cameraLocked)
            {
                // Only rotate camera when right mouse button is held OR always (choose one).
                // For Foxhole-like gameplay, we'll use direct mouse delta without button requirement.
                var mouseX = Input.GetAxis("Mouse X");
                if (Mathf.Abs(mouseX) > 0.001f)
                {
                    _freeYaw += mouseX * mouseSensitivity;
                    // Normalize to 0-360 range to prevent floating-point issues.
                    _freeYaw = Mathf.Repeat(_freeYaw, 360f);
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
                // Camera locks behind player, smoothly interpolating to player's facing.
                var targetYaw = target.eulerAngles.y;
                _lockedYaw = Mathf.LerpAngle(_lockedYaw, targetYaw, 1f - Mathf.Exp(-lockedYawSmoothing * Time.deltaTime));
                effectiveYaw = _lockedYaw;
            }
            else
            {
                // Milestone 7.4: Free camera mode - use mouse-controlled yaw.
                // No smoothing needed since it's directly controlled by mouse delta.
                effectiveYaw = _freeYaw;
            }

            var yaw = Quaternion.Euler(0f, effectiveYaw, 0f);
            var rotatedOffset = yaw * offset;

            var desiredPos = target.position + rotatedOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            transform.rotation = Quaternion.Euler(pitchDegrees, effectiveYaw, 0f);
        }

        private void OnGUI()
        {
            // Show camera lock status.
            if (!cameraLocked)
                return;

            var rect = new Rect(Screen.width - 160, 10, 150, 24);
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 18), "Camera: LOCKED (C)");
        }
    }
}

