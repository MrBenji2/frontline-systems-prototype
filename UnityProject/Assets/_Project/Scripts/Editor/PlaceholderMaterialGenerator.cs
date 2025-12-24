// Editor-only utility to generate placeholder materials as project assets.
// Patch 10 requirement: create simple materials (Wood, Stone, Metal, Ground).
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Frontline.EditorTools
{
    [InitializeOnLoad]
    public static class PlaceholderMaterialGenerator
    {
        private const string TargetFolder = "Assets/_Project/Art/Placeholder/Materials";

        static PlaceholderMaterialGenerator()
        {
            // Run once per editor load; safe if assets already exist.
            EditorApplication.delayCall += EnsureMaterials;
        }

        private static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                return;

            EnsureFolderPath(TargetFolder);

            CreateIfMissing("M_Placeholder_Wood.mat", new Color(0.53f, 0.34f, 0.18f));
            CreateIfMissing("M_Placeholder_Stone.mat", new Color(0.55f, 0.55f, 0.58f));
            CreateIfMissing("M_Placeholder_Metal.mat", new Color(0.55f, 0.62f, 0.68f));
            CreateIfMissing("M_Placeholder_Ground.mat", new Color(0.23f, 0.35f, 0.20f));
        }

        private static void CreateIfMissing(string fileName, Color albedo)
        {
            var path = $"{TargetFolder}/{fileName}";
            if (File.Exists(path))
                return;

            var shader = Shader.Find("Standard");
            if (shader == null)
                return;

            var mat = new Material(shader)
            {
                color = albedo
            };

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolderPath(string assetPath)
        {
            // Create nested folders under Assets/...
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            var parts = assetPath.Split('/');
            var current = parts[0]; // "Assets"
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif

