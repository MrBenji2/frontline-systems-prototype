using UnityEngine;

namespace Frontline.Loot
{
    public sealed class LootPickup : MonoBehaviour
    {
        [SerializeField] private string itemId = "";

        public string ItemId => itemId;

        public static LootPickup Spawn(Vector3 position, string itemId)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Loot_{itemId}";
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
            tm.text = itemId ?? "";
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.black;

            var p = go.AddComponent<LootPickup>();
            p.itemId = itemId ?? "";
            return p;
        }
    }
}

