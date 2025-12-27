using Frontline.Gameplay;
using UnityEngine;

namespace Frontline.Tactical
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TacticalPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7.0f;
        [SerializeField] private float acceleration = 25.0f;
        [SerializeField] private float gravity = 20.0f;

        [Header("View")]
        [SerializeField] private float eyeHeight = 1.6f;

        [Header("Milestone 7.2: Weight-Aware Climbing")]
        [SerializeField] private float baseStepHeight = 0.5f;
        [Tooltip("Slope limit for ramps and inclines.")]
        [SerializeField] private float slopeLimit = 45f;

        [Header("Milestone 7.3: Camera Lock Movement")]
        [SerializeField] private float turnSpeed = 180f;

        private CharacterController _cc;
        private Vector3 _velocity;
        private float _currentStepHeight;
        private TopDownCameraController _cameraController;

        public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

        /// <summary>
        /// Milestone 7.2: Current effective step height based on weight.
        /// </summary>
        public float CurrentStepHeight => _currentStepHeight;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _currentStepHeight = baseStepHeight;

            // Configure CharacterController for ramps.
            if (_cc != null)
            {
                _cc.slopeLimit = slopeLimit;
                _cc.stepOffset = baseStepHeight;
            }
        }

        private void Start()
        {
            // Milestone 7.3: Find camera controller for lock mode.
            _cameraController = FindFirstObjectByType<TopDownCameraController>();
        }

        private void Update()
        {
            // Milestone 7.2: Update step height based on carried weight.
            UpdateWeightEffects();

            var inputH = Input.GetAxisRaw("Horizontal");
            var inputV = Input.GetAxisRaw("Vertical");

            // Camera lock mode changes movement behavior.
            var isCameraLocked = _cameraController != null && _cameraController.IsCameraLocked;

            Vector3 moveDir;
            if (isCameraLocked)
            {
                // In camera lock mode: W/S moves forward/back relative to player facing.
                // A/D strafes (moves perpendicular) without turning.
                var forward = transform.forward;
                var right = transform.right;
                moveDir = (forward * inputV + right * inputH).normalized;
                moveDir = Vector3.ClampMagnitude(moveDir, 1f);

                // Turn the player to face mouse cursor (only in locked mode).
                FaceMouseCursor();
            }
            else
            {
                // Milestone 7.4: Free camera mode - movement is camera-relative.
                // Player moves relative to where the camera is looking.
                var cam = Camera.main;
                if (cam != null && (Mathf.Abs(inputH) > 0.01f || Mathf.Abs(inputV) > 0.01f))
                {
                    // Get camera forward/right on XZ plane.
                    var camForward = cam.transform.forward;
                    camForward.y = 0f;
                    camForward.Normalize();
                    var camRight = cam.transform.right;
                    camRight.y = 0f;
                    camRight.Normalize();

                    moveDir = (camForward * inputV + camRight * inputH).normalized;
                    moveDir = Vector3.ClampMagnitude(moveDir, 1f);
                }
                else
                {
                    moveDir = Vector3.zero;
                }
            }

            // Milestone 7.2: Apply weight-based speed multiplier.
            var effectiveSpeed = moveSpeed * GetSpeedMultiplier();
            var desired = moveDir * effectiveSpeed;

            _velocity.x = Mathf.MoveTowards(_velocity.x, desired.x, acceleration * Time.deltaTime);
            _velocity.z = Mathf.MoveTowards(_velocity.z, desired.z, acceleration * Time.deltaTime);

            if (_cc.isGrounded)
                _velocity.y = -1.0f;
            else
                _velocity.y -= gravity * Time.deltaTime;

            _cc.Move(_velocity * Time.deltaTime);
        }

        /// <summary>
        /// Milestone 7.3: Face the player toward the mouse cursor position.
        /// </summary>
        private void FaceMouseCursor()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out var distance))
            {
                var hitPoint = ray.GetPoint(distance);
                var lookDir = hitPoint - transform.position;
                lookDir.y = 0f;

                if (lookDir.sqrMagnitude > 0.01f)
                {
                    var targetRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// Milestone 7.2: Updates CharacterController step height based on carried weight.
        /// Heavier players can't climb as high.
        /// </summary>
        private void UpdateWeightEffects()
        {
            if (_cc == null)
                return;

            // Get weight-adjusted step height from inventory.
            if (PlayerInventoryService.Instance != null)
            {
                _currentStepHeight = PlayerInventoryService.Instance.GetWeightMaxStepHeight(baseStepHeight);
            }
            else
            {
                _currentStepHeight = baseStepHeight;
            }

            // Apply to CharacterController (clamped to valid range).
            _cc.stepOffset = Mathf.Clamp(_currentStepHeight, 0.01f, _cc.height * 0.5f);
        }

        /// <summary>
        /// Milestone 7.2: Gets the speed multiplier based on inventory weight.
        /// </summary>
        private float GetSpeedMultiplier()
        {
            if (PlayerInventoryService.Instance == null)
                return 1f;

            return PlayerInventoryService.Instance.GetWeightSpeedMultiplier();
        }

        /// <summary>
        /// Milestone 7.2: Hook for AI pathfinding - returns whether player can step up to a given height.
        /// AI will use similar logic when implemented.
        /// </summary>
        public bool CanStepUp(float height)
        {
            return height <= _currentStepHeight;
        }

        /// <summary>
        /// Milestone 7.2: Hook for AI pathfinding - returns current movement capability.
        /// AI will use similar logic when implemented.
        /// </summary>
        public float GetEffectiveMoveSpeed()
        {
            return moveSpeed * GetSpeedMultiplier();
        }
    }
}

