using UnityEngine;
using Frontline.Tactical;
using Frontline.UI;

namespace Frontline.Buildables
{
    /// <summary>
    /// Milestone 5.3: Gate testability.
    /// - Press E while aiming at gate (within range) to toggle open/closed.
    /// - Visible animation (swing 90 degrees).
    /// - Closed blocks movement; open disables the blocking collider.
    /// </summary>
    public sealed class GateController : MonoBehaviour
    {
        [SerializeField] private float interactRange = 3.3f; // +1.5m
        [SerializeField] private float swingDuration = 0.25f;
        [SerializeField] private float autoCloseSeconds = 2.0f;

        private Transform _pivot;
        private Collider _blockingCollider;
        private Collider _interactCollider;

        private bool _isOpen;
        private bool _animating;
        private float _t0;
        private Quaternion _from;
        private Quaternion _to;
        private float _autoCloseAt = -1f;

        public bool IsOpen => _isOpen;

        public void Configure(Transform pivot, Collider blockingCollider, Collider interactCollider)
        {
            _pivot = pivot;
            _blockingCollider = blockingCollider;
            _interactCollider = interactCollider;
            ApplyStateInstant(_isOpen);
        }

        public void SetOpenForLoad(bool open)
        {
            _isOpen = open;
            ApplyStateInstant(_isOpen);
        }

        private void Update()
        {
            if (_pivot == null)
                return;

            // Auto-close after opening.
            if (!_animating && _isOpen && _autoCloseAt > 0f && Time.time >= _autoCloseAt)
            {
                _autoCloseAt = -1f;
                _isOpen = false;
                AnimateToState(false);
                return;
            }

            if (_animating)
            {
                var t = (Time.time - _t0) / Mathf.Max(0.01f, swingDuration);
                if (t >= 1f)
                {
                    _animating = false;
                    _pivot.localRotation = _to;
                    ApplyCollisionForState(_isOpen);
                }
                else
                {
                    _pivot.localRotation = Quaternion.Slerp(_from, _to, t);
                }
                return;
            }

            // Don't allow interaction while UI modals are open or while placing buildables.
            if (UiModalManager.Instance != null && UiModalManager.Instance.HasOpenModal)
                return;
            if (BuildablesService.Instance != null && BuildablesService.Instance.IsBuildModeActive)
                return;

            if (!Input.GetKeyDown(KeyCode.E))
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            // Must be aiming at this gate (or its collider).
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 200f, ~0, QueryTriggerInteraction.Collide))
                return;

            var gate = hit.collider != null ? hit.collider.GetComponentInParent<GateController>() : null;
            if (gate != this)
                return;

            var player = FindFirstObjectByType<TacticalPlayerController>();
            if (player == null)
                return;

            var p = player.transform.position;
            p.y = 0f;
            var g = transform.position;
            g.y = 0f;
            if (Vector3.Distance(p, g) > interactRange)
                return;

            Toggle();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _autoCloseAt = _isOpen ? (Time.time + Mathf.Max(0.1f, autoCloseSeconds)) : -1f;
            AnimateToState(_isOpen);
        }

        private void AnimateToState(bool open)
        {
            if (_pivot == null)
                return;

            // Keep collider enabled during swing, then apply final state.
            if (_blockingCollider != null)
                _blockingCollider.enabled = true;

            _from = _pivot.localRotation;
            _to = open ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            _t0 = Time.time;
            _animating = true;
        }

        private void ApplyStateInstant(bool open)
        {
            if (_pivot != null)
                _pivot.localRotation = open ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            ApplyCollisionForState(open);
        }

        private void ApplyCollisionForState(bool open)
        {
            // Interaction collider always enabled.
            if (_interactCollider != null)
                _interactCollider.enabled = true;

            // Blocking collider disabled when open.
            if (_blockingCollider != null)
                _blockingCollider.enabled = !open;
        }
    }
}

