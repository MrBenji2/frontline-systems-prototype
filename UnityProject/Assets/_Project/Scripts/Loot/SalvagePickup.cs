using UnityEngine;

namespace Frontline.Loot
{
    /// <summary>
    /// Milestone 7.2: Salvage pickup that spawns when ground loot expires.
    /// Salvage must be hauled to a Scrap Yard (future system) for reprocessing.
    /// </summary>
    public sealed class SalvagePickup : MonoBehaviour
    {
        public const string SALVAGE_RESOURCE_ID = "mat_salvage";

        [SerializeField] private int quantity = 1;

        public int Quantity => Mathf.Max(1, quantity);

        /// <summary>
        /// Spawns a salvage pickup at the given position.
        /// </summary>
        public static SalvagePickup Spawn(Vector3 position, int amount = 1)
        {
            var q = Mathf.Max(1, amount);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = q > 1 ? $"Salvage_x{q}" : "Salvage";
            go.transform.position = position + Vector3.up * 0.4f;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = new Color(0.4f, 0.35f, 0.3f); // Brownish/rusty color
            }

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = q > 1 ? $"Salvage x{q}" : "Salvage";
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.3f, 0.25f, 0.2f);

            var p = go.AddComponent<SalvagePickup>();
            p.quantity = q;

            // Salvage doesn't expire (it's already the final form).
            // Can be collected by player or left indefinitely.

            return p;
        }

        /// <summary>
        /// Gets the resource ID for salvage.
        /// </summary>
        public string GetResourceId()
        {
            return SALVAGE_RESOURCE_ID;
        }
    }
}
