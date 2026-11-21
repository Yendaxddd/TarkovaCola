using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Bundle de la granada "Mii" (duplicado de le creatura con otra piel), a partir de Assets/Models/mii/mii.fbx
// (exportado de mii.blend: curvas de ojos/boca pasadas a malla, pose aplicada, pelo y cara reducidos). Las texturas originales
// (msk_alb/msk_h) no venian, asi que cada material lleva el color plano que tenia en Blender.
//   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildMii.Run
// Salida: Build/mii/mii.bundle (prefab "mii") y Preview/mii_<giro>.png
namespace TarkovaCola
{
    public static class BuildMii
    {
        private const string Dir = "Assets/Models/mii/";
        private const float Height = 0.13f;

        private static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            ["Material.001"] = Hex("D8202A"),        // camiseta roja (y manos)
            ["Material.002"] = Hex("1E1E22"),        // pantalon / zapatos
            ["msk_alb"] = Hex("E8B98A"),             // piel
            ["msk_h"] = Hex("4A2C17"),               // pelo
            ["Material.003"] = Hex("101010"), ["SVGMat"] = Hex("101010"), ["SVGMat.001"] = Hex("3A2216"), ["SVGMat.003"] = Hex("101010"),   // ojos, cejas, boca
        };

        private static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString("#" + h, out c); return c; }

        public static void Run()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/Prefabs");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "mii.fbx");
            var src = Object.Instantiate(model);
            var root = new GameObject("mii");
            var visual = new GameObject("visual");
            visual.transform.SetParent(root.transform, false);
            var mats = new Dictionary<string, Material>();
            foreach (var mr in src.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0) continue;
                var go = new GameObject(mr.name);
                go.transform.SetParent(visual.transform, false);
                go.transform.position = mr.transform.position;
                go.transform.rotation = mr.transform.rotation;
                go.transform.localScale = mr.transform.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials.Select(m => Mat(m != null ? m.name : "x", mats)).ToArray();
            }
            Object.DestroyImmediate(src);

            var b = Bounds(root);
            Debug.Log("[MII] tamano original=" + b.size);
            visual.transform.localScale *= Height / b.size.y;
            b = Bounds(root);
            visual.transform.position -= b.center;
            b = Bounds(root);
            var col = root.AddComponent<BoxCollider>();
            col.center = b.center; col.size = b.size;

            string prefabPath = "Assets/Prefabs/mii.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Build/mii");
            BuildPipeline.BuildAssetBundles("Build/mii", new[] { new AssetBundleBuild { assetBundleName = "mii.bundle", assetNames = new[] { prefabPath } } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Debug.Log("[MII] bundle generado");
            Preview();
        }

        private static Material Mat(string name, Dictionary<string, Material> cache)
        {
            Material m;
            if (cache.TryGetValue(name, out m)) return m;
            Color c;
            if (!Colors.TryGetValue(name, out c)) { c = Color.magenta; Debug.Log("[MII] material sin color: " + name); }
            m = new Material(Shader.Find("Standard")) { name = "mii_" + name, color = c };
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", name == "msk_alb" ? 0.35f : 0.2f);
            string path = Dir + "mat_" + name.Replace(".", "_") + ".mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            cache[name] = m;
            return m;
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        public static void Preview()
        {
            Directory.CreateDirectory("Preview");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/mii.prefab");
            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 0.085f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.25f, 0.27f, 0.3f, 1f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 10f;
            camGo.transform.position = new Vector3(0f, 0f, 1f);
            camGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var light = new GameObject("light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.0f;
            light.transform.rotation = Quaternion.Euler(30f, 200f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
            var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            foreach (int yaw in new[] { 0, 90, 180 })
            {
                var inst = (GameObject)Object.Instantiate(prefab);
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(512, 512, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes("Preview/mii_" + yaw + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex); Object.DestroyImmediate(inst);
            }
            Object.DestroyImmediate(camGo); Object.DestroyImmediate(light.gameObject);
        }
    }
}
