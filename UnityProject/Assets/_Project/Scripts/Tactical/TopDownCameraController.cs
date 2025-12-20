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

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            var yaw = Quaternion.Euler(0f, yawDegrees, 0f);
            var rotatedOffset = yaw * offset;

            var desiredPos = target.position + rotatedOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        }
    }
}

