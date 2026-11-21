using System.IO;
using UnityEditor;
using UnityEngine;

// Shaders del contorno de Death Perception -> Build/outline/outline.bundle (y copia en Client/assets).
//   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildOutline.Run
namespace TarkovaCola
{
    public static class BuildOutline
    {
        public static void Run()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Build/outline");
            var b = new AssetBundleBuild { assetBundleName = "outline.bundle", assetNames = new[] { "Assets/Shaders/TcOutlineMask.shader", "Assets/Shaders/TcOutlineComposite.shader" } };
            BuildPipeline.BuildAssetBundles("Build/outline", new[] { b }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            File.Copy("Build/outline/outline.bundle", "../Client/assets/outline.bundle", true);
            foreach (var s in new[] { "TarkovaCola/OutlineMask", "TarkovaCola/OutlineComposite" }) Debug.Log("[OUTLINE] " + s + " soportado: " + (Shader.Find(s) != null && Shader.Find(s).isSupported));
            Debug.Log("[OUTLINE] outline.bundle -> Client/assets");
        }
    }
}
