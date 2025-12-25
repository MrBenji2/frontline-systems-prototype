using Frontline.Gameplay;
using Frontline.Tactical;
using UnityEngine;

namespace Frontline.Harvesting
{
    public sealed class ResourcePickup : MonoBehaviour
    {
        // Patch 5.2: pickup range tuning constant.
        public const float PICKUP_RADIUS = 1.1f;
        private const float VisualScale = 0.35f;

        [SerializeField] private string resourceId = "mat_wood";
        [SerializeField] private int amount = 1;
        [SerializeField] private float spinSpeed = 120f;

        public string ResourceId => resourceId;
        public int Amount => amount;

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (PlayerInventoryService.Instance == null)
                return;

            // Only the tactical player collects for now.
            if (other.GetComponent<TacticalPlayerController>() == null)
                return;

            PlayerInventoryService.Instance.AddResource(resourceId, amount);
            Destroy(gameObject);
        }

        public static ResourcePickup Spawn(Vector3 position, string resourceId, int amount)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Pickup_{resourceId}";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * VisualScale;

            var col = go.GetComponent<Collider>();
            col.isTrigger = true;

            // Increase trigger radius without scaling up the visual.
            if (col is SphereCollider sphere)
            {
                var safeScale = Mathf.Max(0.0001f, VisualScale);
                sphere.radius = PICKUP_RADIUS / safeScale;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var p = go.AddComponent<ResourcePickup>();
            p.resourceId = resourceId ?? "";
            p.amount = Mathf.Max(1, amount);

            // Simple visual tint by resource
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = GetColor(resourceId);
            }

            return p;
        }

        private static Color GetColor(string id)
        {
            return id switch
            {
                "mat_wood" => new Color(0.55f, 0.32f, 0.15f),
                "mat_stone" => new Color(0.55f, 0.55f, 0.55f),
                "mat_iron" => new Color(0.70f, 0.70f, 0.78f),
                "mat_coal" => new Color(0.15f, 0.15f, 0.15f),
                "mat_diesel" => new Color(0.95f, 0.85f, 0.20f),
                _ => new Color(0.8f, 0.8f, 0.8f),
            };
        }
    }
}

