using TMPro;
using UnityEngine;

namespace Frontline.UI
{
    /// <summary>
    /// Patch 5.2: Floating damage number that rises and fades, then destroys itself.
    /// Spawned from Health.ApplyDamage().
    /// </summary>
    public sealed class DamageNumber : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.8f;
        [SerializeField] private float floatSpeed = 1.2f;

        private TextMeshPro _tmp;
        private float _spawnTime;
        private Color _baseColor;

        public static void Spawn(Vector3 worldPos, int amount)
        {
            amount = Mathf.Abs(amount);
            if (amount <= 0)
                return;

            var go = new GameObject($"Dmg_{amount}");
            go.transform.position = worldPos;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = amount.ToString();
            tmp.fontSize = 4.0f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.35f, 0.35f, 1f);
            tmp.enableWordWrapping = false;

            // Make sure it's visible in world space without needing a Canvas.
            tmp.sortingOrder = 5000;

            var fx = go.AddComponent<DamageNumber>();
            fx._tmp = tmp;
            fx._spawnTime = Time.time;
            fx._baseColor = tmp.color;
        }

        private void Update()
        {
            if (_tmp == null)
            {
                Destroy(gameObject);
                return;
            }

            // Float upwards.
            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            // Face camera.
            var cam = Camera.main;
            if (cam != null)
            {
                var forward = cam.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }

            var t = (Time.time - _spawnTime) / Mathf.Max(0.01f, lifetime);
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            var c = _baseColor;
            c.a = Mathf.SmoothStep(1f, 0f, t);
            _tmp.color = c;
        }
    }
}

