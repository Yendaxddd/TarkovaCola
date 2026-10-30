using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Maquinas de perks (Black Ops 3, exportadas con Greyhound y convertidas a OBJ con tools/semodel2obj.py).
//   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildMachines.Run
// Cada maquina: Assets/Models/machines/<perk>/<perk>.obj + _images/<material>/i_<material>_c|n|... .png
// Salida: Build/machines/machines.bundle (prefabs "machine_<perk>") y Preview/machine_<perk>.png
namespace TarkovaCola
{
    public static class BuildMachines
    {
        private const string Dir = "Assets/Models/machines/";
        private const float Height = 2.2f;                   // alto de la maquina en metros
        // En BO3 algunas pinturas son un tinte del shader (sin textura de color propia): se ponen a mano
        private static readonly Dictionary<string, Color> Tints = new Dictionary<string, Color>
        {
            ["mtl_p7_zm_vending_marathon_paint"] = new Color(0.93f, 0.52f, 0.12f),       // Stamin-Up: naranja
            ["mtl_p7_zm_vending_widows_wine_paint_black"] = new Color(0.07f, 0.06f, 0.08f),
            ["mtl_p7_zm_vending_widows_wine_chrome"] = new Color(0.82f, 0.82f, 0.85f),
            ["mtl_p7_zm_vending_widows_wine_aluminum"] = new Color(0.68f, 0.68f, 0.70f),
        };
        private static readonly string[] Ids = { "widowswine" };     // las demas maquinas son de BO6 (BuildBo6)

        public static void Run()
        {
            AssetDatabase.Refresh();
            ConfigureTextures();
            Directory.CreateDirectory("Assets/Prefabs");
            var paths = new List<string>();
            foreach (var id in Ids)
            {
                try { paths.Add(BuildOne(id)); }
                catch (System.Exception e) { Debug.LogError("[MAQ] " + id + ": " + e); }
            }
            Directory.CreateDirectory("Build/machines");
            BuildPipeline.BuildAssetBundles("Build/machines", new[] { new AssetBundleBuild { assetBundleName = "machines.bundle", assetNames = paths.ToArray() } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Debug.Log("[MAQ] bundle generado con " + paths.Count + " maquinas");
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) Preview();
        }

        private static void ConfigureTextures()
        {
            foreach (var path in Directory.GetFiles(Dir, "*.png", SearchOption.AllDirectories))
            {
                var p = path.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;
                bool normal = p.EndsWith("_n.png");
                if ((ti.textureType == TextureImporterType.NormalMap) == normal && ti.maxTextureSize == 1024) continue;
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal && !p.EndsWith("_g.png") && !p.EndsWith("_s.png");
                ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
            }
        }

        private static Texture2D Tex(string id, string mat, params string[] suffixes)
        {
            string folder = Dir + id + "/_images/" + mat + "/";
            if (!Directory.Exists(folder)) return null;
            foreach (var f in Directory.GetFiles(folder, "*.png"))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (n.StartsWith("$")) continue;
                foreach (var s in suffixes)
                    if (n.EndsWith(s)) return AssetDatabase.LoadAssetAtPath<Texture2D>(f.Replace('\\', '/'));
            }
            return null;
        }

        private static Material MakeMaterial(string id, string mat)
        {
            string path = Dir + id + "/m_" + mat + ".mat";
            AssetDatabase.DeleteAsset(path);
            var m = new Material(Shader.Find("Standard")) { name = mat };
            var albedo = Tex(id, mat, "_c", "_color", "_diffuse");
            var normal = Tex(id, mat, "_n");
            Color tint;
            if (Tints.TryGetValue(mat, out tint)) { m.color = tint; if (l0(mat).Contains("paint")) albedo = null; }
            if (albedo != null) m.mainTexture = albedo;
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            string l = mat.ToLowerInvariant();
            bool metal = l.Contains("metal") || l.Contains("chrome") || l.Contains("iron") || l.Contains("aluminum") || l.Contains("coil");
            m.SetFloat("_Metallic", metal ? 0.6f : 0.1f);
            m.SetFloat("_Glossiness", metal ? 0.55f : 0.35f);
            if (l.Contains("glass") || l.Contains("window"))
            {
                m.color = new Color(0.8f, 0.85f, 0.9f, 0.25f);
                m.SetFloat("_Mode", 3f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); m.renderQueue = 3000;
                m.SetFloat("_Glossiness", 0.9f);
            }
            // letreros, luces y el panel de PHD: brillan un poco (con su propia textura)
            var emissive = Tex(id, mat, "emissive_c");
            if (emissive == null && id == "phdflopper" && l.Contains("phd")) emissive = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + id + "/_images/i_wardog_t7_perk_machine_phd_emissive_c.png");
            if (emissive == null && (l.Contains("sign") || l.Contains("light") || l.Contains("backlit") || l.Contains("logo"))) emissive = albedo;
            if (emissive != null)
            {
                m.SetTexture("_EmissionMap", emissive); m.SetColor("_EmissionColor", Color.white * 0.45f); m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[MAQ] " + id + "/" + mat + ": color=" + (albedo != null ? albedo.name : "-") + " normal=" + (normal != null) + " brillo=" + (emissive != null));
            return m;
        }

        private static string BuildOne(string id)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + id + "/" + id + ".obj");
            if (model == null) throw new System.Exception("no se encontro el OBJ");
            var src = Object.Instantiate(model);
            var root = new GameObject("machine_" + id);
            var visual = new GameObject("visual");
            visual.transform.SetParent(root.transform, false);
            var mats = new Dictionary<string, Material>();
            foreach (var mr in src.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var go = new GameObject(mr.name);
                go.transform.SetParent(visual.transform, false);
                go.transform.localPosition = mr.transform.localPosition; go.transform.localRotation = mr.transform.localRotation; go.transform.localScale = mr.transform.localScale;
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials.Select(m =>
                {
                    string n = m != null ? m.name : "x";
                    Material mm;
                    if (!mats.TryGetValue(n, out mm)) mats[n] = mm = MakeMaterial(id, n);
                    return mm;
                }).ToArray();
            }
            Object.DestroyImmediate(src);

            // CoD es Z arriba: el eje mas largo pasa a ser Y
            var b = Bounds(root);
            if (b.size.z > b.size.y) visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            b = Bounds(root);
            visual.transform.localScale *= Height / b.size.y;
            b = Bounds(root);
            visual.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);       // el pivote, en el suelo y centrado
            b = Bounds(root);
            Debug.Log("[MAQ] " + id + ": tamano " + b.size);

            var col = root.AddComponent<BoxCollider>();
            col.center = b.center; col.size = b.size;
            string prefab = "Assets/Prefabs/machine_" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefab);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static string l0(string s) { return s.ToLowerInvariant(); }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        // Las 6 maquinas en fila, vistas desde cuatro lados (para saber cual es el frente)
        public static void Preview()
        {
            Directory.CreateDirectory("Preview");
            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 1.6f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.24f, 0.27f, 1f);
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 50f;
            var lightGo = new GameObject("light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var rt = new RenderTexture(1800, 600, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.orthographicSize = 1.5f;
            var insts = new List<GameObject>();
            for (int i = 0; i < Ids.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/machine_" + Ids[i] + ".prefab");
                if (prefab == null) continue;
                var inst = (GameObject)Object.Instantiate(prefab);
                inst.transform.position = new Vector3((i - 2.5f) * 1.5f, 0f, 0f);
                insts.Add(inst);
            }
            foreach (int yaw in new[] { 0, 90, 180, 270 })
            {
                var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                foreach (var inst in insts) inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                camGo.transform.position = new Vector3(0f, 1.1f, -10f);
                camGo.transform.rotation = Quaternion.identity;
                lightGo.transform.rotation = Quaternion.Euler(25f, 20f, 0f);
                cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(1800, 600, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1800, 600), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes("Preview/machines_" + yaw + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            foreach (var i in insts) Object.DestroyImmediate(i);
            Debug.Log("[MAQ] vistas previas generadas");
        }
    }
}
