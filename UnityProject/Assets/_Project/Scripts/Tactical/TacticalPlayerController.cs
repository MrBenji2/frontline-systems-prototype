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

        private CharacterController _cc;
        private Vector3 _velocity;
        private float _currentStepHeight;

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

        private void Update()
        {
            // Milestone 7.2: Update step height based on carried weight.
            UpdateWeightEffects();

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);

            // Milestone 7.2: Apply weight-based speed multiplier.
            var effectiveSpeed = moveSpeed * GetSpeedMultiplier();
            var desired = input * effectiveSpeed;

            _velocity.x = Mathf.MoveTowards(_velocity.x, desired.x, acceleration * Time.deltaTime);
            _velocity.z = Mathf.MoveTowards(_velocity.z, desired.z, acceleration * Time.deltaTime);

            if (_cc.isGrounded)
                _velocity.y = -1.0f;
            else
                _velocity.y -= gravity * Time.deltaTime;

            _cc.Move(_velocity * Time.deltaTime);
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

