using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Construye los asset bundles de las perks "de botella" (Stamin-Up, Juggernog, PHD Flopper, Widow's Wine) a partir de perks.fbx
// (paquete de botellas de CoD Zombies). Sin interfaz:
//   Unity.exe -batchmode -nographics -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildPerkBottles.Run
// Cada botella del FBX son 4 mallas (tapa, liquido, etiqueta, cristal) con texturas de color: aqui se deja solo la de cada perk.
// Salida por perk: Build/<id>/<id>.bundle (item en el mundo) y Build/<id>_hand/<id>_hand.bundle (modelo en la mano).
namespace TarkovaCola
{
    public static class BuildPerkBottles
    {
        private const string Dir = "Assets/Models/perks/";
        private const string Tex = Dir + "tex/";
        private const float BottleHeightMeters = 0.17f;   // altura real aproximada de la botella (la lata de Speed Cola mide 0.122)

        private class Bottle
        {
            public string Id;            // id de la perk: nombre del bundle y del prefab
            public string Key;           // como se llama la perk en las mallas del FBX ("..._<Key>_view_LOD0_SEModelMesh")
            public string Metal, Water, Paper;       // texturas (en Assets/Models/perks/tex)
            public Color LiquidTint;     // multiplica el color del liquido (la iluminacion plana lo deja pastel)
            public Color LiquidEmission; // un poco de brillo propio para que el liquido se lea a traves del cristal
        }

        private static readonly Bottle[] All =
        {
            new Bottle { Id = "staminup", Key = "stamin",
                Metal = "i_wardog_t7_perk_bottle_metal_stamin_c.png", Water = "i_wardog_t7_perk_bottle_water_stamin_c.png", Paper = "i_wardog_t7_perk_bottle_paper_stamin_c.png",
                LiquidTint = new Color(0.72f, 0.52f, 0.16f), LiquidEmission = new Color(0.05f, 0.04f, 0.02f) },
            new Bottle { Id = "juggernog", Key = "jugg",
                Metal = "i_wardog_t7_perk_bottle_metal_jugg_c.png", Water = "i_wardog_t7_perk_bottle_water_jugg_c.png", Paper = "i_wardog_t7_perk_bottle_paper_jugg_c_upsca.png",
                LiquidTint = new Color(0.58f, 0.26f, 0.24f), LiquidEmission = new Color(0.04f, 0.0f, 0.0f) },
            new Bottle { Id = "phdflopper", Key = "phd",
                Metal = "i_wardog_t7_perk_bottle_metal_phd_c.png", Water = "i_wardog_t7_perk_bottle_water_phd_c.png", Paper = "i_wardog_t7_perk_bottle_paper_phd_c_alpha.png",
                LiquidTint = new Color(0.70f, 0.62f, 0.80f), LiquidEmission = new Color(0.02f, 0.01f, 0.05f) },
            new Bottle { Id = "widowswine", Key = "widow",
                Metal = "i_wardog_t7_perk_bottle_metal_widow_c.png", Water = "i_wardog_t7_perk_bottle_water_widow_c.png", Paper = "i_wardog_t7_perk_bottle_paper_widow_c.png",
                LiquidTint = new Color(0.42f, 0.05f, 0.10f), LiquidEmission = new Color(0.04f, 0.0f, 0.01f) },
        };

        // Todas las botellas (o solo la indicada en la variable de entorno BOTTLE).
        public static void Run()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            ConfigureTextures();
            string only = System.Environment.GetEnvironmentVariable("BOTTLE");
            foreach (var b in All)
                if (string.IsNullOrEmpty(only) || only == b.Id) BuildOne(b);
        }

        private static void BuildOne(Bottle b)
        {
            string prefabPath = "Assets/Prefabs/" + b.Id + ".prefab";
            string outDir = "Build/" + b.Id, handDir = "Build/" + b.Id + "_hand";
            Directory.CreateDirectory(outDir);
            Directory.CreateDirectory(handDir);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "perks.fbx");
            var src = Object.Instantiate(model);

            // las 4 mallas de la perk (el FBX trae todas las perks y sus esqueletos)
            Transform cap = null, liquid = null, label = null, glass = null;
            foreach (var t in src.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.Contains("_" + b.Key + "_view_LOD0_SEModelMesh")) continue;
                var mats = t.GetComponent<Renderer>().sharedMaterials;
                string m = mats.Length > 0 && mats[0] != null ? mats[0].name : "";
                if (m.Contains("metal")) cap = t; else if (m.Contains("water")) liquid = t; else if (m.Contains("paper")) label = t; else if (m.Contains("glass")) glass = t;
            }
            if (cap == null || liquid == null || label == null || glass == null) throw new System.Exception("No se encontraron las 4 mallas de " + b.Id);

            var root = new GameObject(b.Id);
            var visual = new GameObject("visual");
            visual.transform.SetParent(root.transform, false);
            Add(visual, "cap", MeshOf(cap), CapMaterial(b));
            Add(visual, "liquid", MeshOf(liquid), LiquidMaterial(b));
            Add(visual, "label", MeshOf(label), LabelMaterial(b));
            Add(visual, "glass", MeshOf(glass), GlassMaterial(b));

            // el eje largo de la malla es Z: se endereza dejando la tapa ARRIBA
            float capZ = MeshOf(cap).bounds.center.z, bodyZ = MeshOf(glass).bounds.center.z;
            visual.transform.localRotation = Quaternion.Euler(capZ > bodyZ ? -90f : 90f, 0f, 0f);
            Debug.Log("[TarkovaCola] " + b.Id + ": capZ=" + capZ + " bodyZ=" + bodyZ + " tamano malla=" + Bounds(root).size);

            float h = Bounds(root).size.y;
            if (h > 0f) visual.transform.localScale *= BottleHeightMeters / h;
            var c = Bounds(root).center;
            visual.transform.position -= c - root.transform.position;
            AssetDatabase.SaveAssets();
            Debug.Log("[TarkovaCola] " + b.Id + ": tamano final=" + Bounds(root).size);
            Object.DestroyImmediate(src);

            var col = root.AddComponent<BoxCollider>();
            var bb = Bounds(root);
            col.center = root.transform.InverseTransformPoint(bb.center);
            col.size = bb.size;

            // El juego exige PreviewPivot para renderizar el icono de inventario. La etiqueta queda centrada con este giro (en el
            // renderizador de iconos del juego el giro va al reves que en los previews; el mismo valor sirve para todas las botellas).
            var pivot = root.AddComponent<PreviewPivot>();
            pivot.pivotPosition = Vector3.zero;
            pivot.pivotRotation = Quaternion.identity;
            pivot.scale = Vector3.one;
            pivot.Icon.rotation = Quaternion.Euler(0f, IconYaw(), 0f);
            pivot.Icon.boundsScale = 1f;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            BuildPipeline.BuildAssetBundles(outDir, new[] { new AssetBundleBuild { assetBundleName = b.Id + ".bundle", assetNames = new[] { prefabPath } } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            // copia con otro nombre para el modelo en mano (Unity no deja cargar dos veces un bundle con el mismo nombre)
            BuildPipeline.BuildAssetBundles(handDir, new[] { new AssetBundleBuild { assetBundleName = b.Id + "_hand.bundle", assetNames = new[] { prefabPath } } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Debug.Log("[TarkovaCola] bundles de " + b.Id + " generados");
        }

        private static float IconYaw() { return float.Parse(System.Environment.GetEnvironmentVariable("BOTTLE_ICON_YAW") ?? "40"); }

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

        private static Material CapMaterial(Bottle b)
        {
            var m = Std(b.Id + "_cap");
            m.mainTexture = Load(b.Metal);
            m.SetTexture("_BumpMap", Load("i_wpn_t7_zmb_perk_bottle_metal_n.jpg")); m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Metallic", 0.25f); m.SetFloat("_Glossiness", 0.45f);   // el metal completo se ve casi negro en el icono del inventario
            return m;
        }

        private static Material LiquidMaterial(Bottle b)
        {
            var m = Std(b.Id + "_liquid");
            m.mainTexture = Load(b.Water);
            m.color = b.LiquidTint;
            m.SetFloat("_Metallic", 0f); m.SetFloat("_Glossiness", 0.35f);
            m.SetTexture("_EmissionMap", m.mainTexture); m.SetColor("_EmissionColor", b.LiquidEmission); m.EnableKeyword("_EMISSION");
            return m;
        }

        // la etiqueta es un medallon con transparencia: recorte por alfa
        private static Material LabelMaterial(Bottle b)
        {
            var m = Std(b.Id + "_label");
            m.mainTexture = Load(b.Paper);
            m.SetFloat("_Mode", 1f); m.SetFloat("_Cutoff", 0.5f);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero); m.SetInt("_ZWrite", 1);
            m.EnableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 2450;
            m.SetFloat("_Metallic", 0f); m.SetFloat("_Glossiness", 0.25f);
            return m;
        }

        // cristal casi transparente
        private static Material GlassMaterial(Bottle b)
        {
            var m = Std(b.Id + "_glass");
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

        // Renderiza cada botella con la etiqueta centrada (giro 130 en este previsualizador) en una sola fila. Salida: Preview/bottles_<yaw>.png
        // Se lanza SIN -nographics (hace falta la GPU):  ... -executeMethod TarkovaCola.BuildPerkBottles.Preview
        public static void Preview()
        {
            Directory.CreateDirectory("Preview");
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

            int yaw = int.Parse(System.Environment.GetEnvironmentVariable("PREVIEW_YAW") ?? "130");
            foreach (var b in All)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + b.Id + ".prefab");
                if (prefab == null) continue;
                var inst = (GameObject)Object.Instantiate(prefab);
                foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(256, 512, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 256, 512), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes("Preview/bottle_" + b.Id + "_" + yaw + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(inst);
            }
            Debug.Log("[TarkovaCola] previews de las botellas generados");
        }
    }
}
