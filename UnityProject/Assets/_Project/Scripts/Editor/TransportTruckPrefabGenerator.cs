// Editor-only helper to generate the TransportTruck prefab asset.
#if UNITY_EDITOR
using System.IO;
using Frontline.Vehicles;
using Frontline.World;
using UnityEditor;
using UnityEngine;

namespace Frontline.EditorTools
{
    /// <summary>
    /// Milestone 6 helper:
    /// Generates Resources/TransportTruck.prefab if it doesn't exist (so runtime can Resources.Load it).
    /// This avoids needing manual prefab authoring for verification.
    /// </summary>
    [InitializeOnLoad]
    public static class TransportTruckPrefabGenerator
    {
        private const string PrefabAssetPath = "Assets/_Project/Resources/TransportTruck.prefab";

        static TransportTruckPrefabGenerator()
        {
            EditorApplication.delayCall += EnsurePrefabExists;
        }

        private static void EnsurePrefabExists()
        {
            EditorApplication.delayCall -= EnsurePrefabExists;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath) != null)
                return;

            var dir = Path.GetDirectoryName(PrefabAssetPath);
            if (!string.IsNullOrWhiteSpace(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                // Create folder chain if missing.
                var parts = dir.Split('/');
                var current = parts[0];
                for (var i = 1; i < parts.Length; i++)
                {
                    var next = $"{current}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            var root = new GameObject("TransportTruck");

            // Stable physics.
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 2500f;
            rb.drag = 1.0f;
            rb.angularDrag = 3.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // Blocking collider.
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.75f, 0f);
            box.size = new Vector3(2.8f, 1.6f, 5.0f);

            // Health + controller (controller will ensure destructible + visuals if needed).
            root.AddComponent<Health>();
            var ctrl = root.AddComponent<TransportTruckController>();

            // Anchors (optional but nicer to have explicitly authored).
            var seat = new GameObject("SeatAnchor");
            seat.transform.SetParent(root.transform, false);
            seat.transform.localPosition = new Vector3(0.25f, 1.1f, 0.6f);

            var exit = new GameObject("ExitPoint");
            exit.transform.SetParent(root.transform, false);
            exit.transform.localPosition = new Vector3(2.0f, 0.5f, 0.0f);

            // Assign serialized anchor refs.
            var so = new SerializedObject(ctrl);
            so.FindProperty("seatAnchor").objectReferenceValue = seat.transform;
            so.FindProperty("exitPoint").objectReferenceValue = exit.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif

