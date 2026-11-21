using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TarkovaCola
{
    // Carga bundles ya compilados (como hace el juego) y los renderiza por delante y por detras.
    //   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.PreviewBundles.Run   (env BUNDLES=ruta1;ruta2)
    public static class PreviewBundles
    {
        public static void Run()
        {
            Directory.CreateDirectory("Preview");
            var paths = (System.Environment.GetEnvironmentVariable("BUNDLES") ?? "").Split(';');
            var cam = new GameObject("cam").AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 0.12f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.24f, 0.27f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 50f; cam.transform.position = new Vector3(0f, 0f, -10f);
            var light = new GameObject("light").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(25f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var insts = new List<GameObject>(); var loaded = new List<AssetBundle>();
            int n = 0;
            foreach (var p in paths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var ab = AssetBundle.LoadFromFile(p); loaded.Add(ab);
                if (ab == null) { Debug.LogError("[PB] no se pudo cargar " + p); continue; }
                foreach (var go in ab.LoadAllAssets<GameObject>())
                {
                    for (int side = 0; side < 2; side++)
                    {
                        var g = Object.Instantiate(go);
                        g.transform.position = new Vector3((n - 1.5f) * 0.09f, 0f, 0f);
                        g.transform.rotation = Quaternion.Euler(0f, side * 180f, 0f);
                        insts.Add(g); n++;
                    }
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                        foreach (var m in r.sharedMaterials)
                            Debug.Log("[PB] " + p + " " + (m != null ? m.name + " shader=" + (m.shader != null ? m.shader.name : "null") + " tex=" + (m.mainTexture != null ? m.mainTexture.name : "null") : "null"));
                    break;
                }
            }
            var rt = new RenderTexture(2000, 600, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(2000, 600, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 2000, 600), 0, 0); tex.Apply();
            File.WriteAllBytes("Preview/bundles.png", tex.EncodeToPNG());
            foreach (var g in insts) Object.DestroyImmediate(g);
            foreach (var ab in loaded) if (ab != null) ab.Unload(true);
        }
    }
}
