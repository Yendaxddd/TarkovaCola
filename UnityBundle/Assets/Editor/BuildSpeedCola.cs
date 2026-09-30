using System.IO;
using UnityEditor;
using UnityEngine;

// Construye el asset bundle de Speed Cola. Se lanza sin interfaz:
//   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildSpeedCola.Run
// Las texturas las deja preparadas prepare_model.py en Assets/Models/cola.
namespace TarkovaCola
{
    public static class BuildSpeedCola
    {
        private const string Dir = "Assets/Models/cola/";
        private const string ModelPath = Dir + "cola.fbx";
        private const string MaterialPath = Dir + "speedcola_mat.mat";
        private const string PrefabPath = "Assets/Prefabs/speedcola.prefab";
        private const string OutDir = "Build";
        private const float CanHeightMeters = 0.122f; // altura real de una lata de TarCola

        public static void Run()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory(OutDir);

            ConfigureTextures();

            var mat = new Material(Shader.Find("Standard")) { name = "speedcola_mat" };
            mat.mainTexture = Load("cola_albedo.png");
            mat.SetTexture("_MetallicGlossMap", Load("cola_metal_smooth.png"));
            mat.SetTexture("_BumpMap", Load("cola_normal.png"));
            mat.SetTexture("_OcclusionMap", Load("cola_ao.png"));
            mat.SetTexture("_EmissionMap", Load("cola_emissive.png"));
            mat.SetColor("_EmissionColor", Color.white);
            mat.SetFloat("_Metallic", 1f);
            mat.SetFloat("_GlossMapScale", 1f);
            mat.EnableKeyword("_METALLICGLOSSMAP");
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(mat, MaterialPath);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var root = new GameObject("speedcola");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root.transform, false);
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = FillMaterials(r.sharedMaterials.Length, mat);

            Debug.Log("[TarkovaCola] tamano original del modelo: " + Bounds(root).size);
            Normalize(root, visual);
            Debug.Log("[TarkovaCola] tamano final: " + Bounds(root).size);

            var col = root.AddComponent<BoxCollider>();
            var b = Bounds(root);
            col.center = root.transform.InverseTransformPoint(b.center);
            col.size = b.size;

            // El juego exige PreviewPivot para renderizar el icono de inventario.
            var pivot = root.AddComponent<PreviewPivot>();
            pivot.pivotPosition = Vector3.zero;
            pivot.pivotRotation = Quaternion.identity;
            pivot.scale = Vector3.one;
            // La botella lleva la etiqueta en DOS lados opuestos (a +-90 grados del giro por defecto): girada 90 en Y, el icono
            // ensena la etiqueta de frente. Casi vertical (el item ocupa 1x2) y muy poco inclinada.
            pivot.Icon.rotation = Quaternion.Euler(0f, 90f, 10f);
            pivot.Icon.boundsScale = 1f;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            var build = new AssetBundleBuild { assetBundleName = "speedcola.bundle", assetNames = new[] { PrefabPath } };
            BuildPipeline.BuildAssetBundles(OutDir, new[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Debug.Log("[TarkovaCola] bundle generado en " + Path.GetFullPath(OutDir));

            // Copia con otro nombre para el modelo en mano: el cliente la carga por su cuenta y Unity no permite cargar
            // dos veces un bundle con el mismo nombre (el otro ya lo carga el sistema de bundles de SPT).
            Directory.CreateDirectory(OutDir + "/hand");
            var hand = new AssetBundleBuild { assetBundleName = "speedcola_hand.bundle", assetNames = new[] { PrefabPath } };
            BuildPipeline.BuildAssetBundles(OutDir + "/hand", new[] { hand },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Debug.Log("[TarkovaCola] bundle de mano generado");
        }

        private static Texture2D Load(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + file);

        // El normal debe importarse como mapa de normales; el resto se limita a 2048 para no inflar el bundle.
        private static void ConfigureTextures()
        {
            foreach (var f in new[] { "cola_albedo.png", "cola_normal.png", "cola_ao.png", "cola_emissive.png", "cola_metal_smooth.png" })
            {
                var ti = AssetImporter.GetAtPath(Dir + f) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = f == "cola_normal.png" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = f == "cola_albedo.png" || f == "cola_emissive.png";
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
        }

        private static Material[] FillMaterials(int count, Material m)
        {
            var arr = new Material[count];
            for (int i = 0; i < count; i++) arr[i] = m;
            return arr;
        }

        // Endereza (eje mas largo -> Y), escala a la altura real de una lata y centra el pivote.
        private static void Normalize(GameObject root, GameObject visual)
        {
            var size = Bounds(root).size;
            if (size.x >= size.y && size.x >= size.z) visual.transform.localRotation = Quaternion.Euler(0, 0, 90);
            else if (size.z >= size.x && size.z >= size.y) visual.transform.localRotation = Quaternion.Euler(90, 0, 0);

            float h = Bounds(root).size.y;
            if (h > 0f) visual.transform.localScale *= CanHeightMeters / h;

            var c = Bounds(root).center;
            visual.transform.position -= c - root.transform.position;
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
