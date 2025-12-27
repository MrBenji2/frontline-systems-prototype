using UnityEngine;

namespace Frontline.Tactical
{
    /// <summary>
    /// Milestone 7.5: Foxhole-style camera with clean lock/unlock behavior.
    /// - Free mode: mouse orbits camera around player, camera-relative movement.
    /// - Locked mode: camera instantly snaps behind player and follows player rotation cleanly.
    /// </summary>
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Rig")]
        [SerializeField] private Vector3 offset = new Vector3(0, 18f, -14f);
        [SerializeField] private float followSharpness = 12f;
        [SerializeField] private float pitchDegrees = 55f;

        [Header("Camera Lock (C key)")]
        [Tooltip("When enabled, camera locks behind the player and follows player rotation.")]
        [SerializeField] private bool cameraLocked = false;
        [SerializeField] private KeyCode toggleLockKey = KeyCode.C;

        [Header("Milestone 7.5: Free Camera")]
        [Tooltip("Mouse sensitivity for free camera orbit.")]
        [SerializeField] private float mouseSensitivity = 2.0f;

        // Current camera yaw (single source of truth).
        private float _cameraYaw;

        // Track the last frame's yaw to detect sudden jumps.
        private bool _initialized;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        /// Whether the camera is locked behind the player.
        /// </summary>
        public bool IsCameraLocked
        {
            get => cameraLocked;
            set => cameraLocked = value;
        }

        /// <summary>
        /// Current camera yaw for external queries.
        /// </summary>
        public float CameraYaw => _cameraYaw;

        private void Start()
        {
            // Initialize camera yaw to player facing or 0.
            _cameraYaw = target != null ? target.eulerAngles.y : 0f;
            _initialized = true;
        }

        private void Update()
        {
            // Toggle camera lock with C key.
            if (Input.GetKeyDown(toggleLockKey))
            {
                cameraLocked = !cameraLocked;

                if (cameraLocked && target != null)
                {
                    // Milestone 7.5: Switching to locked mode - instantly snap to player facing.
                    // No smoothing, no drift - just snap.
                    _cameraYaw = target.eulerAngles.y;
                }
                // When unlocking: preserve current camera yaw exactly (no spin).
                // _cameraYaw stays unchanged, so camera doesn't move.
            }

            // Handle camera yaw updates based on mode.
            if (cameraLocked)
            {
                // Milestone 7.5: In locked mode, camera yaw is driven directly by player yaw.
                // No smoothing to prevent twitch - just use player yaw directly.
                if (target != null)
                {
                    _cameraYaw = target.eulerAngles.y;
                }
            }
            else
            {
                // Free camera mode: mouse controls orbit.
                var mouseX = Input.GetAxis("Mouse X");
                if (Mathf.Abs(mouseX) > 0.001f)
                {
                    _cameraYaw += mouseX * mouseSensitivity;
                }
            }

            // Normalize yaw to prevent floating-point drift over time.
            _cameraYaw = NormalizeAngle(_cameraYaw);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            // Calculate camera position and rotation from current yaw.
            var yaw = Quaternion.Euler(0f, _cameraYaw, 0f);
            var rotatedOffset = yaw * offset;

            var desiredPos = target.position + rotatedOffset;

            // Smooth position follow to reduce jitter from player movement.
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            // Set rotation directly (no smoothing needed since yaw is already stable).
            transform.rotation = Quaternion.Euler(pitchDegrees, _cameraYaw, 0f);
        }

        /// <summary>
        /// Normalize angle to -180 to 180 range to prevent wrap-around issues.
        /// </summary>
        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
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

