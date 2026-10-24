using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Construye el bundle de "le creatura" (M4 SOPMOD II Jr) a partir de Assets/Models/jr/jr.fbx. El FBX trae colores planos de Maya
// (lambertN) sin textura: aqui cada material recibe el color del peluche y una textura de pelusa generada.
//   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildJr.Run      (sin -nographics: hace la vista previa)
// Salida: Build/jr/jr.bundle (prefab "jr") y Preview/jr_<giro>.png
namespace TarkovaCola
{
    public static class BuildJr
    {
        private const string Dir = "Assets/Models/jr/";
        private const float Height = 0.13f;       // alto final (m): un poco mas grande que una RGD-5

        // material del FBX -> color del peluche
        private static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            ["lambert25"] = Hex("EDCBAE"), ["pasted__lambert29"] = Hex("EDCBAE"), ["pasted__lambert47"] = Hex("EDCBAE"),   // mechones de pelo
            ["lambert17"] = Hex("F6E4D4"),                                   // cara
            ["lambert4"] = Hex("3A3A40"), ["lambert9"] = Hex("3A3A40"),      // cuerpo / ropa
            ["lambert10"] = Hex("2A2A2E"),
            ["lambert12"] = Hex("55555C"),                                   // cascos / orejas
            ["lambert13"] = Hex("C8C8CC"),
            ["lambert14"] = Hex("D0232B"), ["lambert5"] = Hex("D0232B"), ["lambert6"] = Hex("E0442A"),   // detalles rojos
            ["lambert18"] = Hex("7A2A2A"), ["lambert19"] = Hex("2A1A1A"),    // boca / cejas
            ["lambert1"] = Hex("8A8A90"),                                    // contorno de los ojos
            ["lambert22"] = Hex("FAFAFA"), ["pasted__lambert22"] = Hex("FAFAFA"),   // ojos (blanco)
            ["lambert21"] = Hex("6A6A72"), ["pasted__lambert21"] = Hex("6A6A72"),
            ["lambert20"] = Hex("6A6A72"), ["pasted__lambert20"] = Hex("6A6A72"),
            ["lambert23"] = Hex("6E6E76"), ["lambert24"] = Hex("D8D8DC"),    // cuello
        };

        private static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString("#" + h, out c); return c; }

        public static void Run()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/Prefabs");
            var fur = FurTexture();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "jr.fbx");
            var src = Object.Instantiate(model);

            var root = new GameObject("jr");
            var visual = new GameObject("visual");
            visual.transform.SetParent(root.transform, false);
            var mats = new Dictionary<string, Material>();
            int parts = 0;
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
                go.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials.Select(m => Mat(m != null ? m.name : "x", fur, mats)).ToArray();
                parts++;
            }
            Object.DestroyImmediate(src);

            var b = Bounds(root);
            Debug.Log("[JR] piezas=" + parts + " tamano original=" + b.size + " centro=" + b.center);
            visual.transform.localScale *= Height / b.size.y;
            b = Bounds(root);
            visual.transform.position -= b.center;
            b = Bounds(root);
            Debug.Log("[JR] tamano final=" + b.size);

            var col = root.AddComponent<BoxCollider>();
            col.center = b.center; col.size = b.size;

            string prefabPath = "Assets/Prefabs/jr.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            Directory.CreateDirectory("Build/jr");
            BuildPipeline.BuildAssetBundles("Build/jr", new[] { new AssetBundleBuild { assetBundleName = "jr.bundle", assetNames = new[] { prefabPath } } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Debug.Log("[JR] bundle generado");
            if (!Application.isBatchMode || SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) Preview();
        }

        private static Material Mat(string name, Texture2D fur, Dictionary<string, Material> cache)
        {
            Material m;
            if (cache.TryGetValue(name, out m)) return m;
            Color c;
            if (!Colors.TryGetValue(name, out c)) { c = Color.magenta; Debug.Log("[JR] material sin color: " + name); }
            m = new Material(Shader.Find("Standard")) { name = "jr_" + name };
            m.color = c;
            m.mainTexture = fur;
            m.mainTextureScale = new Vector2(6f, 6f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", name.Contains("lambert22") ? 0.35f : 0.08f);     // los ojos bordados brillan un poco
            string path = Dir + "mat_" + name + ".mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            cache[name] = m;
            return m;
        }

        // Pelusa: ruido suave en gris claro (se multiplica por el color de cada pieza)
        private static Texture2D FurTexture()
        {
            const int N = 256;
            var t = new Texture2D(N, N, TextureFormat.RGB24, true) { name = "jr_fur", wrapMode = TextureWrapMode.Repeat };
            var rnd = new System.Random(7);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.5f + Mathf.PerlinNoise(x * 0.31f + 17f, y * 0.31f) * 0.3f + (float)rnd.NextDouble() * 0.2f;
                    float v = Mathf.Lerp(0.82f, 1f, n);
                    px[y * N + x] = new Color(v, v, v);
                }
            t.SetPixels(px); t.Apply();
            string path = Dir + "jr_fur.png";
            File.WriteAllBytes(path, t.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.wrapMode = TextureWrapMode.Repeat; ti.maxTextureSize = 256; ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/jr.prefab");
            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 0.085f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.25f, 0.27f, 0.3f, 1f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 10f;
            camGo.transform.position = new Vector3(0f, 0f, 1f);
            camGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);        // mira hacia -Z (la cara del modelo apunta a +Z)
            var lightGo = new GameObject("light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.0f;
            lightGo.transform.rotation = Quaternion.Euler(30f, 200f, 0f);
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
                File.WriteAllBytes("Preview/jr_" + yaw + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex); Object.DestroyImmediate(inst);
            }
            Debug.Log("[JR] vista previa generada");
        }
    }
}
