using Frontline.Crafting;
using Frontline.Tactical;
using UnityEngine;

namespace Frontline.Harvesting
{
    public static class DevSpawnHarvestNodes
    {
        private const int DefaultLayer = 0;

        public static void SpawnNodeSetNearPlayer()
        {
            var player = Object.FindFirstObjectByType<TacticalPlayerController>();
            var center = player != null ? player.transform.position : Vector3.zero;
            center.y = 0f;

            // Spread in a small arc in front of the player (camera is top-down, so just offset).
            SpawnTree(center + new Vector3(-3, 0, 3));
            SpawnRock(center + new Vector3(0, 0, 3));
            SpawnWreckage(center + new Vector3(3, 0, 3));
            SpawnCoalVein(center + new Vector3(-1.5f, 0, 6));
            SpawnFuelContainer(center + new Vector3(1.5f, 0, 6));
        }

        private static void SpawnTree(Vector3 pos)
        {
            var go = CreateNode("Node_Tree", PrimitiveType.Cylinder, new Color(0.40f, 0.24f, 0.10f), pos, new Vector3(1.0f, 2.5f, 1.0f));
            var node = go.AddComponent<HarvestNode>();
            node.Configure(ToolType.Axe, "mat_wood", 5, 30);
        }

        private static void SpawnRock(Vector3 pos)
        {
            var go = CreateNode("Node_Rock", PrimitiveType.Sphere, new Color(0.55f, 0.55f, 0.55f), pos, new Vector3(1.6f, 1.2f, 1.6f));
            var node = go.AddComponent<HarvestNode>();
            node.Configure(ToolType.Shovel, "mat_stone", 5, 30);
        }

        private static void SpawnWreckage(Vector3 pos)
        {
            var go = CreateNode("Node_Wreckage", PrimitiveType.Cube, new Color(0.35f, 0.45f, 0.55f), pos, new Vector3(2.0f, 1.0f, 1.5f));
            var node = go.AddComponent<HarvestNode>();
            node.Configure(ToolType.Wrench, "mat_iron", 5, 36);
        }

        private static void SpawnCoalVein(Vector3 pos)
        {
            var go = CreateNode("Node_CoalVein", PrimitiveType.Capsule, new Color(0.15f, 0.15f, 0.15f), pos, new Vector3(1.2f, 1.4f, 1.2f));
            var node = go.AddComponent<HarvestNode>();
            node.Configure(ToolType.Hammer, "mat_coal", 5, 30);
        }

        private static void SpawnFuelContainer(Vector3 pos)
        {
            var go = CreateNode("Node_FuelContainer", PrimitiveType.Cylinder, new Color(0.85f, 0.75f, 0.15f), pos, new Vector3(1.0f, 1.2f, 1.0f));
            var node = go.AddComponent<HarvestNode>();
            node.Configure(ToolType.GasCan, "mat_diesel", 5, 30);
        }

        private static GameObject CreateNode(string name, PrimitiveType prim, Color tint, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(prim);
            go.name = name;
            go.layer = DefaultLayer; // raycastable; fog ignores triggers
            go.transform.position = pos + Vector3.up * (scale.y * 0.5f);
            go.transform.localScale = scale;

            var col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true; // important: do not occlude fog (fog raycasts ignore triggers)

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = tint;
            }

            return go;
        }
    }
}

