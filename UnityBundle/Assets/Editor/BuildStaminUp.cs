using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Construye el asset bundle de Stamin-Up a partir de perks.fbx (paquete de botellas de CoD Zombies). Sin interfaz:
//   Unity.exe -batchmode -nographics -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildStaminUp.Run
// Cada botella del FBX son 4 mallas (tapa, liquido, etiqueta, cristal) con texturas de color; aqui se queda solo la de Stamin-Up.
namespace TarkovaCola
{
    public static class BuildStaminUp
    {
        private const string Dir = "Assets/Models/perks/";
        private const string Tex = Dir + "tex/";
        private const string PrefabPath = "Assets/Prefabs/staminup.prefab";
        private const string OutDir = "Build/staminup";
        private const string HandDir = "Build/staminup_hand";
        private const float BottleHeightMeters = 0.17f;   // altura real aproximada de la botella (la lata de Speed Cola mide 0.122)

        public static void Run()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(HandDir);
            ConfigureTextures();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "perks.fbx");
            var src = Object.Instantiate(model);

            // las 4 mallas de Stamin-Up (el FBX trae todas las perks y sus esqueletos)
            Transform cap = null, liquid = null, label = null, glass = null;
            foreach (var t in src.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.Contains("_stamin_view_LOD0_SEModelMesh")) continue;
                var mats = t.GetComponent<Renderer>().sharedMaterials;
                string m = mats.Length > 0 && mats[0] != null ? mats[0].name : "";
                if (m.Contains("metal")) cap = t; else if (m.Contains("water")) liquid = t; else if (m.Contains("paper")) label = t; else if (m.Contains("glass")) glass = t;
            }
            if (cap == null || liquid == null || label == null || glass == null) throw new System.Exception("No se encontraron las 4 mallas de Stamin-Up");

            var root = new GameObject("staminup");
            var visual = new GameObject("visual");
            visual.transform.SetParent(root.transform, false);
            Add(visual, "cap", MeshOf(cap), CapMaterial());
            Add(visual, "liquid", MeshOf(liquid), LiquidMaterial());
            Add(visual, "label", MeshOf(label), LabelMaterial());
            Add(visual, "glass", MeshOf(glass), GlassMaterial());

            // el eje largo de la malla es Z: se endereza dejando la tapa ARRIBA
            float capZ = MeshOf(cap).bounds.center.z, bodyZ = MeshOf(glass).bounds.center.z;
            visual.transform.localRotation = Quaternion.Euler(capZ > bodyZ ? -90f : 90f, 0f, 0f);
            Debug.Log("[TarkovaCola] stamin: capZ=" + capZ + " bodyZ=" + bodyZ + " tamano malla=" + Bounds(root).size);

            float h = Bounds(root).size.y;
            if (h > 0f) visual.transform.localScale *= BottleHeightMeters / h;
            var c = Bounds(root).center;
            visual.transform.position -= c - root.transform.position;
            AssetDatabase.SaveAssets();
            Debug.Log("[TarkovaCola] stamin: tamano final=" + Bounds(root).size);
            Object.DestroyImmediate(src);

            var col = root.AddComponent<BoxCollider>();
            var b = Bounds(root);
            col.center = root.transform.InverseTransformPoint(b.center);
            col.size = b.size;

            var pivot = root.AddComponent<PreviewPivot>();
            pivot.pivotPosition = Vector3.zero;
            pivot.pivotRotation = Quaternion.identity;
            pivot.scale = Vector3.one;
            pivot.Icon.rotation = Quaternion.Euler(0f, IconYaw(), 8f);
            pivot.Icon.boundsScale = 1f;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            BuildPipeline.BuildAssetBundles(OutDir, new[] { new AssetBundleBuild { assetBundleName = "staminup.bundle", assetNames = new[] { PrefabPath } } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            // copia con otro nombre para el modelo en mano (Unity no deja cargar dos veces un bundle con el mismo nombre)
            BuildPipeline.BuildAssetBundles(HandDir, new[] { new AssetBundleBuild { assetBundleName = "staminup_hand.bundle", assetNames = new[] { PrefabPath } } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Debug.Log("[TarkovaCola] bundles de Stamin-Up generados");
        }

        // Giro (en Y) con el que el icono del inventario ensena la etiqueta de frente; se ajusta mirando los previews.
        private static float IconYaw() { return float.Parse(System.Environment.GetEnvironmentVariable("STAMIN_ICON_YAW") ?? "90"); }

        private static Mesh MeshOf(Transform t)
        {
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null) return mf.sharedMesh;
            return t.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        }

        private static void Add(GameObject parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static Texture2D Load(string file) { return AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + file); }

        private static void ConfigureTextures()
        {
            foreach (var path in Directory.GetFiles(Tex).Where(f => !f.EndsWith(".meta")))
            {
                var ti = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
                if (ti == null) continue;
                bool normal = path.EndsWith("_n.jpg");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal;
                ti.alphaIsTransparency = path.Contains("paper");
                ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
            }
        }

        private static Material Std(string name)
        {
            var m = new Material(Shader.Find("Standard")) { name = name };
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            // el material debe existir como asset para que el prefab (y el bundle) lo referencien
            string path = Dir + name + ".mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material CapMaterial()
        {
            var m = Std("staminup_cap");
            m.mainTexture = Load("i_wardog_t7_perk_bottle_metal_stamin_c.png");
            m.SetTexture("_BumpMap", Load("i_wpn_t7_zmb_perk_bottle_metal_n.jpg")); m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Metallic", 0.25f); m.SetFloat("_Glossiness", 0.45f);   // el metal completo se ve casi negro en el icono del inventario
            return m;
        }

        private static Material LiquidMaterial()
        {
            var m = Std("staminup_liquid");
            m.mainTexture = Load("i_wardog_t7_perk_bottle_water_stamin_c.png");
            m.color = new Color(0.82f, 0.74f, 0.45f);   // el liquido real es amarillo oliva; la iluminacion plana lo deja pastel
            m.SetFloat("_Metallic", 0f); m.SetFloat("_Glossiness", 0.35f);
            // un poco de brillo propio para que se lea el color del liquido a traves del cristal
            m.SetTexture("_EmissionMap", m.mainTexture); m.SetColor("_EmissionColor", new Color(0.12f, 0.12f, 0.12f)); m.EnableKeyword("_EMISSION");
            return m;
        }

        // la etiqueta es un medallon con transparencia: recorte por alfa
        private static Material LabelMaterial()
        {
            var m = Std("staminup_label");
            m.mainTexture = Load("i_wardog_t7_perk_bottle_paper_stamin_c.png");
            m.SetFloat("_Mode", 1f); m.SetFloat("_Cutoff", 0.5f);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero); m.SetInt("_ZWrite", 1);
            m.EnableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 2450;
            m.SetFloat("_Metallic", 0f); m.SetFloat("_Glossiness", 0.25f);
            return m;
        }

        // cristal casi transparente
        private static Material GlassMaterial()
        {
            var m = Std("staminup_glass");
            m.mainTexture = Load("i_wardog_t7_perk_bottle_glass_c.jpg");
            m.color = new Color(1f, 1f, 1f, 0.22f);
            m.SetTexture("_BumpMap", Load("i_wpn_t7_zmb_perk_bottle_glass_n.jpg")); m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Mode", 2f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON"); m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            m.SetFloat("_Metallic", 0f); m.SetFloat("_Glossiness", 0.9f);
            return m;
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        // Renderiza el prefab en 4 giros para elegir la rotacion del icono. Salida: Preview/stamin_yawXXX.png
        public static void Preview()
        {
            Directory.CreateDirectory("Preview");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.27f, 0.3f, 1f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 10f;
            cam.orthographicSize = 0.11f;
            camGo.transform.position = new Vector3(0f, 0f, -1f);
            var lightGo = new GameObject("light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(30f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var rt = new RenderTexture(256, 512, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            foreach (int yaw in new[] { 0, 90, 180, 270 })
            {
                var inst = (GameObject)Object.Instantiate(prefab);
                foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 8f);
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(256, 512, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 256, 512), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes("Preview/stamin_yaw" + yaw.ToString("000") + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(inst);
            }
            Debug.Log("[TarkovaCola] previews de Stamin-Up generados");
        }
    }
}
