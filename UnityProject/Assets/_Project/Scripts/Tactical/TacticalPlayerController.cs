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

        private CharacterController _cc;
        private Vector3 _velocity;

        public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);

            var desired = input * moveSpeed;
            _velocity.x = Mathf.MoveTowards(_velocity.x, desired.x, acceleration * Time.deltaTime);
            _velocity.z = Mathf.MoveTowards(_velocity.z, desired.z, acceleration * Time.deltaTime);

            if (_cc.isGrounded)
                _velocity.y = -1.0f;
            else
                _velocity.y -= gravity * Time.deltaTime;

            _cc.Move(_velocity * Time.deltaTime);
        }
    }
}

