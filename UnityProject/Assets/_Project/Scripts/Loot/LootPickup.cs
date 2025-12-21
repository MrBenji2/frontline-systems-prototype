using UnityEngine;

namespace Frontline.Loot
{
    public sealed class LootPickup : MonoBehaviour
    {
        [SerializeField] private string itemId = "";
        [SerializeField] private int quantity = 1;

        public string ItemId => itemId;
        public int Quantity => Mathf.Max(1, quantity);

        public static LootPickup Spawn(Vector3 position, string itemId)
        {
            return Spawn(position, itemId, 1);
        }

        public static LootPickup Spawn(Vector3 position, string itemId, int quantity)
        {
            var q = Mathf.Max(1, quantity);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = q > 1 ? $"Loot_{itemId}_x{q}" : $"Loot_{itemId}";
            go.transform.position = position + Vector3.up * 0.5f;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            var col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = new Color(0.7f, 0.85f, 0.95f);
            }

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = q > 1 ? $"{itemId} x{q}" : (itemId ?? "");
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.black;

            var p = go.AddComponent<LootPickup>();
            p.itemId = itemId ?? "";
            p.quantity = q;
            return p;
        }
    }
}

