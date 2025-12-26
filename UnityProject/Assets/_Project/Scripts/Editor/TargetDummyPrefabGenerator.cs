// Editor-only helper to generate the TargetDummy prefab asset.
#if UNITY_EDITOR
using System.IO;
using Frontline.DebugTools;
using Frontline.World;
using UnityEditor;
using UnityEngine;

namespace Frontline.EditorTools
{
    [InitializeOnLoad]
    public static class TargetDummyPrefabGenerator
    {
        private const string PrefabAssetPath = "Assets/_Project/Resources/TargetDummy.prefab";

        static TargetDummyPrefabGenerator()
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

            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "TargetDummy";
            root.transform.localScale = new Vector3(1.0f, 1.2f, 1.0f);

            // Ensure collider exists (primitive has one). Add health + dummy logic.
            root.AddComponent<Health>();
            root.AddComponent<TargetDummy>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif

