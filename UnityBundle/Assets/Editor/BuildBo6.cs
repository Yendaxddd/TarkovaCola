using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Modelos de Black Ops 6 (exportados con Saluki, copiados con tools/bo6_import.py a Assets/Models/bo6/<modelo>/).
//   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildBo6.Run   (sin -nographics: vistas previas)
// Genera:
//   Build/machines/machines.bundle  maquinas "machine_<perk>" (hijos "off" y "on": apagada / encendida) + "power_switch" (hijo "lever")
//                                   + "machine_mysterybox" (hijo "lid": la tapa) + "machine_tedd" (BO7)
//                                   (Widow's Wine no existe en BO6: se usa la de BO3 de Assets/Prefabs/machine_widowswine.prefab)
//   Build/<perk>/<perk>.bundle y Build/<perk>_hand/<perk>_hand.bundle   latas de BO6 (objeto en el mundo / en la mano)
namespace TarkovaCola
{
    public static class BuildBo6
    {
        private const string Dir = "Assets/Models/bo6/";
        private const float MachineHeight = 2.2f;     // m
        internal const float CanHeight = 0.13f;       // m (la lata de Tar-Cola mide 0.122)

        private static readonly Dictionary<string, string> MachineModel = new Dictionary<string, string>
        {
            ["speedcola"] = "t10_zm_machine_speed_cola_lava_all_fxanim", ["staminup"] = "t10_zm_machine_staminup",
            ["juggernog"] = "t10_zm_machine_juggernog", ["phdflopper"] = "t10_zm_machine_phd_flopper", ["deadshot"] = "t10_zm_machine_deadshot",
            ["wunderfizz"] = "sat_zm_machine_wunderfizz_fxanim",     // BO7; va la ultima: usa la escala de las demas en vez de medir 2.2 m
        };
        private static readonly Dictionary<string, string> CanModel = new Dictionary<string, string>
        {
            ["speedcola"] = "offhand_t10_vm_zmb_perk_speed_cola_can_static", ["staminup"] = "offhand_t10_vm_zmb_perk_stamina_up_can_static",
            ["juggernog"] = "offhand_t10_vm_zmb_perk_juggernog_can_static", ["phdflopper"] = "offhand_t10_vm_zmb_perk_phd_flopper_can_static",
            ["deadshot"] = "offhand_t10_vm_zmb_perk_deadshot_can_static",
            ["vultureaid"] = "offhand_t10_vm_zmb_perk_vulture_aid_can_static", ["meleemacchiato"] = "offhand_t10_vm_zmb_perk_melee_macchiato_can_static",
            ["bandolier"] = "offhand_t10_vm_zmb_perk_can", ["widowswine"] = "offhand_t10_vm_zmb_perk_can",
        };

        public static void Run()
        {
            AssetDatabase.Refresh();
            ConfigureTextures();
            Directory.CreateDirectory("Assets/Prefabs");
            var machinePrefabs = new List<string>();
            float unit = 0f;                                  // escala comun (metros por unidad del FBX), sacada de la primera maquina
            foreach (var kv in MachineModel)
            {
                try { machinePrefabs.Add(BuildMachine(kv.Key, kv.Value, ref unit)); }
                catch (System.Exception e) { Debug.LogError("[BO6] maquina " + kv.Key + ": " + e); }
            }
            if (File.Exists("Assets/Prefabs/machine_widowswine.prefab")) machinePrefabs.Add("Assets/Prefabs/machine_widowswine.prefab");
            try { machinePrefabs.Add(BuildSwitch(unit)); } catch (System.Exception e) { Debug.LogError("[BO6] interruptor: " + e); }
            try { machinePrefabs.Add(BuildBox(unit)); } catch (System.Exception e) { Debug.LogError("[BO6] caja misteriosa: " + e); }
            try { machinePrefabs.Add(BuildTedd(unit)); } catch (System.Exception e) { Debug.LogError("[BO6] TEDD: " + e); }
            Directory.CreateDirectory("Build/machines");
            BuildPipeline.BuildAssetBundles("Build/machines", new[] { new AssetBundleBuild { assetBundleName = "machines.bundle", assetNames = machinePrefabs.ToArray() } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Debug.Log("[BO6] machines.bundle: " + machinePrefabs.Count + " prefabs");

            foreach (var kv in CanModel)
            {
                try { BuildCan(kv.Key, kv.Value); }
                catch (System.Exception e) { Debug.LogError("[BO6] lata " + kv.Key + ": " + e); }
            }
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) Preview();
        }

        // ---------------------------------------------------------------- texturas y materiales
        private static void ConfigureTextures()
        {
            foreach (var path in Directory.GetFiles(Dir, "*.png", SearchOption.AllDirectories))
            {
                var p = path.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;
                bool normal = p.EndsWith("_normal.png");
                bool can = p.Contains("_can_static/") || p.Contains("perk_can_half") || p.Contains("perk_can/");
                int max = can ? 1024 : 2048;
                if ((ti.textureType == TextureImporterType.NormalMap) == normal && ti.maxTextureSize == max) continue;
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal && !p.EndsWith("_gloss.png");
                ti.maxTextureSize = max;
                ti.SaveAndReimport();
            }
        }

        private static Dictionary<string, Dictionary<string, string>> ReadMats(string model)
        {
            var res = new Dictionary<string, Dictionary<string, string>>();
            string json = File.ReadAllText(Dir + model + "/mats.json");
            foreach (Match m in Regex.Matches(json, "\"([^\"]+)\":\\s*\\{([^}]*)\\}"))
            {
                var d = new Dictionary<string, string>();
                foreach (Match t in Regex.Matches(m.Groups[2].Value, "\"(\\w+)\":\\s*\"([^\"]+)\"")) d[t.Groups[1].Value] = t.Groups[2].Value;
                res[m.Groups[1].Value] = d;
            }
            return res;
        }

        private static Material MakeMaterial(string model, string matName, Dictionary<string, Dictionary<string, string>> mats, bool lit)
        {
            // el nombre que da Unity al material del FBX puede llevar prefijos o sufijos: se busca el que contiene el nombre
            var key = mats.Keys.Where(k => matName.Contains(k) || k.Contains(matName)).OrderByDescending(k => k.Length).FirstOrDefault();
            Dictionary<string, string> tex = key != null ? mats[key] : new Dictionary<string, string>();
            string path = Dir + model + "/m_" + Regex.Replace(matName, "[^A-Za-z0-9_]", "_") + ".mat";
            AssetDatabase.DeleteAsset(path);
            var m = new Material(Shader.Find("Standard")) { name = matName };
            string v;
            if (tex.TryGetValue("albedo", out v)) m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + model + "/" + v);
            if (tex.TryGetValue("normal", out v)) { m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + model + "/" + v)); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Metallic", 0.15f);
            m.SetFloat("_Glossiness", 0.4f);
            string l = matName.ToLowerInvariant();
            if (l.Contains("glass"))
            {
                m.color = new Color(1f, 1f, 1f, 0.3f);
                m.SetFloat("_Mode", 3f); m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); m.renderQueue = 3000; m.SetFloat("_Glossiness", 0.85f);
            }
            if (lit && tex.TryGetValue("emissive", out v))
            {
                m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + model + "/" + v));
                m.SetColor("_EmissionColor", Color.white * 0.8f); m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // Copia las mallas de un FBX (estaticas o con piel) a un objeto nuevo con los materiales de BO6
        private static GameObject Load(string model, bool lit)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + model + "/" + model + ".fbx");
            if (fbx == null) throw new System.Exception("no se encontro el FBX de " + model);
            var mats = ReadMats(model);
            var cache = new Dictionary<string, Material>();
            var src = Object.Instantiate(fbx);
            var root = new GameObject(model);
            foreach (var r in src.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh = r is SkinnedMeshRenderer smr ? smr.sharedMesh : (r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null);
                if (mesh == null) continue;
                var go = new GameObject(r.name);
                go.transform.SetParent(root.transform, false);
                go.transform.position = r.transform.position; go.transform.rotation = r.transform.rotation; go.transform.localScale = r.transform.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = r.sharedMaterials.Select(mm =>
                {
                    string n = mm != null ? mm.name : "x";
                    Material res;
                    if (!cache.TryGetValue(n, out res)) cache[n] = res = MakeMaterial(model, n, mats, lit);
                    return res;
                }).ToArray();
            }
            Object.DestroyImmediate(src);
            return root;
        }

        // El FBX tal cual (mallas con huesos incluidas) con los materiales de BO6/BO7
        private static GameObject LoadSkinned(string model)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + model + "/" + model + ".fbx");
            if (fbx == null) throw new System.Exception("no se encontro el FBX de " + model);
            var mats = ReadMats(model);
            var cache = new Dictionary<string, Material>();
            var go = Object.Instantiate(fbx);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(mm =>
                {
                    string n = mm != null ? mm.name : "x";
                    Material res;
                    if (!cache.TryGetValue(n, out res)) cache[n] = res = MakeMaterial(model, n, mats, true);
                    return res;
                }).ToArray();
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }
            return go;
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        // ---------------------------------------------------------------- maquinas
        private static string BuildMachine(string id, string model, ref float unit)
        {
            var root = new GameObject("machine_" + id);
            var off = Load(model, false); off.name = "off";
            var on = Load(model + "_on", true); on.name = "on";
            off.transform.SetParent(root.transform, false); on.transform.SetParent(root.transform, false);
            var b = Bounds(off);
            float factor = id == "wunderfizz" && unit > 0f ? unit : MachineHeight / b.size.y;
            if (unit <= 0f) unit = factor;
            foreach (var t in new[] { off.transform, on.transform }) { t.localScale = Vector3.one * factor; t.localRotation = Quaternion.Euler(0f, id == "wunderfizz" ? 270f : 90f, 0f); }    // el frente de BO6 mira a +X (el de la Wunderfizz de BO7 a -X): se deja mirando a -Z como las de BO3
            b = Bounds(off);
            var shift = new Vector3(b.center.x, b.min.y, b.center.z);
            off.transform.position -= shift; on.transform.position -= shift;
            on.SetActive(false);
            b = Bounds(off);
            var col = root.AddComponent<BoxCollider>(); col.center = b.center; col.size = b.size;
            Debug.Log("[BO6] maquina " + id + ": " + b.size);
            string path = "Assets/Prefabs/machine_" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }

        // ---------------------------------------------------------------- caja misteriosa y TEDD (BO7)
        // La caja: la tapa (la malla mas alta) cuelga de "lid", una bisagra en el borde de atras; el cliente la abre girando "lid" en X.
        // TEDD: el modelo tal cual. Los dos con la escala comun de las maquinas y el frente mirando a -Z.
        internal const string BoxModel = "sat_zm_mystery_box_fxanim", TeddModel = "prop_sat_tedd_trials_machine_execution";
        private static float Yaw(string k, float d) { float v; return float.TryParse(System.Environment.GetEnvironmentVariable(k), out v) ? v : d; }

        private static string BuildBox(float unit)
        {
            // La caja de BO7 es una malla con huesos: la tapa (en 5 piezas) cuelga del hueso j_lid. Se conservan los huesos y la bisagra "lid"
            // (borde de atras, arriba) hace de padre de j_lid: al girarla gira toda la tapa (antes se giraba una sola malla y quedaban dos tapas).
            var root = new GameObject("machine_mysterybox");
            var body = LoadSkinned(BoxModel); body.name = "body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * unit; body.transform.localRotation = Quaternion.Euler(0f, Yaw("BOX_YAW", 0f), 0f);
            var b = Bounds(body);
            body.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            b = Bounds(body);
            var jLid = body.GetComponentsInChildren<Transform>(true).First(t => t.name == "j_lid");
            var pivot = new GameObject("lid");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.position = new Vector3(b.center.x, jLid.position.y, b.max.z);       // bisagra: borde de atras (+Z), a la altura de la tapa
            jLid.SetParent(pivot.transform, true);
            var col = root.AddComponent<BoxCollider>(); col.center = b.center; col.size = b.size;
            Debug.Log("[BO6] caja misteriosa: " + b.size + ", bisagra a " + pivot.transform.localPosition);
            string path = "Assets/Prefabs/machine_mysterybox.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }

        private static string BuildTedd(float unit)
        {
            var root = new GameObject("machine_tedd");
            var body = Load(TeddModel, true); body.name = "body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * unit; body.transform.localRotation = Quaternion.Euler(0f, Yaw("TEDD_YAW", 270f), 0f);     // la cara mira a -X
            var b = Bounds(body);
            body.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            b = Bounds(root);
            var col = root.AddComponent<BoxCollider>(); col.center = b.center; col.size = b.size;
            Debug.Log("[BO6] TEDD: " + b.size);
            string path = "Assets/Prefabs/machine_tedd.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }

        // la escala comun sin reconstruir las maquinas: la del modelo de la Speed Cola ya guardado
        private static float SavedUnit()
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/machine_speedcola.prefab");
            var off = p != null ? p.transform.Find("off") : null;
            return off != null ? off.localScale.x : 0.0254f;
        }

        // Solo la caja y TEDD, con vista previa (4 giros; la caja cerrada y abierta):
        //   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildBo6.BoxTedd   [BOX_YAW / TEDD_YAW]
        public static void BoxTedd()
        {
            AssetDatabase.Refresh();
            ConfigureTextures();
            float unit = SavedUnit();
            Debug.Log("[BO6] escala comun " + unit);
            var paths = new[] { BuildBox(unit), BuildTedd(unit) };
            Directory.CreateDirectory("Preview");
            var camGo = new GameObject("cam"); var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.24f, 0.27f, 1f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 50f;
            var lightGo = new GameObject("light"); var light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(25f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var rt = new RenderTexture(2400, 900, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            var insts = new List<GameObject>();
            int[] yaws = { 0, 90, 180, 270 };
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 4; i++)
                {
                    var g = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(paths[row == 2 ? 1 : 0]));
                    g.transform.SetPositionAndRotation(new Vector3((i - 1.5f) * 4f, (2 - row) * 3.2f, 0f), Quaternion.Euler(0f, yaws[i], 0f));
                    if (row == 1) g.transform.Find("lid").localRotation = Quaternion.Euler(90f, 0f, 0f);
                    insts.Add(g);
                }
            cam.orthographicSize = 4.8f; camGo.transform.position = new Vector3(0f, 3.6f, -20f);
            // en batch Unity no actualiza las mallas con huesos: se hornean con la pose (tapa abierta incluida)
            var baked = new List<GameObject>();
            foreach (var g in insts)
                foreach (var sk in g.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var m = new Mesh(); sk.BakeMesh(m, true);
                    var bk = new GameObject("baked"); bk.transform.SetPositionAndRotation(sk.transform.position, sk.transform.rotation);
                    bk.AddComponent<MeshFilter>().sharedMesh = m; bk.AddComponent<MeshRenderer>().sharedMaterials = sk.sharedMaterials;
                    sk.enabled = false; baked.Add(bk);
                }
            insts.AddRange(baked);
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(2400, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 2400, 900), 0, 0); tex.Apply();
            File.WriteAllBytes("Preview/box_tedd.png", tex.EncodeToPNG());
            foreach (var g in insts) Object.DestroyImmediate(g);
            Debug.Log("[BO6] vista previa: Preview/box_tedd.png (giros 0, 90, 180, 270; fila 1 caja, fila 2 caja abierta, fila 3 TEDD)");
        }

        private static string BuildSwitch(float unit)
        {
            var root = new GameObject("power_switch");
            var sw = Load("t10_zm_power_switch", true); sw.name = "body";
            var lever = Load("t10_zm_power_switch_handle", false); lever.name = "lever";
            sw.transform.SetParent(root.transform, false); lever.transform.SetParent(root.transform, false);
            if (unit <= 0f) unit = 0.0254f;
            sw.transform.localScale = Vector3.one * unit; lever.transform.localScale = Vector3.one * unit;
            sw.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); lever.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // el frente miraba a +Z
            var b = Bounds(sw);
            var shift = new Vector3(b.center.x, b.min.y, b.center.z);
            sw.transform.position -= shift; lever.transform.position -= shift;
            b = Bounds(root);
            var col = root.AddComponent<BoxCollider>(); col.center = b.center; col.size = b.size;
            Debug.Log("[BO6] interruptor: " + b.size);
            string path = "Assets/Prefabs/power_switch.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }

        // ---------------------------------------------------------------- latas (objeto del juego y modelo en la mano)
        private static readonly Dictionary<string, Color> CanTint = new Dictionary<string, Color> { ["bandolier"] = new Color(0.45f, 0.55f, 0.25f), ["widowswine"] = new Color(0.55f, 0.12f, 0.2f) };

        private static void BuildCan(string id, string model)
        {
            var root = new GameObject(id);
            var visual = Load(model, true); visual.name = "visual";
            // la lata generica (sin perk propia en BO6) se tine con el color de la perk
            Color tint;
            if (CanTint.TryGetValue(id, out tint))
                foreach (var r in visual.GetComponentsInChildren<Renderer>())
                    foreach (var m in r.sharedMaterials) if (m != null && m.name.Contains("perk_can")) { m.color = tint; EditorUtility.SetDirty(m); }
            visual.transform.SetParent(root.transform, false);
            visual.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);         // el logo mirando al frente (con 90 salia la etiqueta de atras)
            var b = Bounds(root);
            // el eje mas largo es el alto de la lata
            if (b.size.z > b.size.y && b.size.z > b.size.x) visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            else if (b.size.x > b.size.y) visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            b = Bounds(root);
            visual.transform.localScale *= CanHeight / b.size.y;
            b = Bounds(root);
            visual.transform.position -= b.center - root.transform.position;
            b = Bounds(root);
            var col = root.AddComponent<BoxCollider>(); col.center = b.center; col.size = b.size;
            var pivot = root.AddComponent<PreviewPivot>();
            pivot.pivotPosition = Vector3.zero; pivot.pivotRotation = Quaternion.identity; pivot.scale = Vector3.one;
            pivot.Icon.rotation = Quaternion.Euler(0f, float.Parse(System.Environment.GetEnvironmentVariable("CAN_ICON_YAW") ?? "180"), 0f);
            pivot.Icon.boundsScale = 1f;
            string path = "Assets/Prefabs/bo6_can_" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[BO6] lata " + id + ": " + b.size);
            foreach (var (dir, bundle) in new[] { ("Build/" + id, id + ".bundle"), ("Build/" + id + "_hand", id + "_hand.bundle") })
            {
                Directory.CreateDirectory(dir);
                BuildPipeline.BuildAssetBundles(dir, new[] { new AssetBundleBuild { assetBundleName = bundle, assetNames = new[] { path } } },
                    BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            }
        }

        // ---------------------------------------------------------------- inyectores (opcion de F12: las perks como inyectores)
        //   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.BuildBo6.Injectors
        // Genera Build/<perk>_inj/<perk>_inj.bundle (objeto del mundo) y Build/<perk>_inj_hand/<perk>_inj_hand.bundle (modelo en la mano): el stim de BO6 con el liquido del color de la perk.
        internal const float InjectorLength = 0.15f;  // m (el Propital mide unos 0.13)
        private const string InjectorModel = "offhand_t10_vm_stim_shot";
        internal static readonly Dictionary<string, Color> PerkColor = new Dictionary<string, Color>
        {
            ["speedcola"] = new Color(0.25f, 0.85f, 0.3f), ["staminup"] = new Color(1f, 0.75f, 0.15f), ["juggernog"] = new Color(0.9f, 0.15f, 0.12f),
            ["phdflopper"] = new Color(0.55f, 0.25f, 0.9f), ["widowswine"] = new Color(0.55f, 0.08f, 0.2f), ["deadshot"] = new Color(0.35f, 0.35f, 0.38f),
            ["bandolier"] = new Color(0.45f, 0.55f, 0.25f), ["vultureaid"] = new Color(0.15f, 0.65f, 0.6f), ["meleemacchiato"] = new Color(0.5f, 0.3f, 0.15f),
        };

        public static void Injectors()
        {
            AssetDatabase.Refresh();
            ConfigureTextures();
            Directory.CreateDirectory("Assets/Prefabs");
            var paths = new List<string>();
            foreach (var kv in PerkColor)
            {
                try { paths.Add(BuildInjector(kv.Key, kv.Value)); }
                catch (System.Exception e) { Debug.LogError("[BO6] inyector " + kv.Key + ": " + e); }
            }
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) PreviewRow("bo6_injectors", paths.ToArray(), 0.09f, 0.18f);
        }

        private static string BuildInjector(string id, Color color)
        {
            var root = new GameObject(id + "_inj");
            var visual = Load(InjectorModel, true); visual.name = "visual";
            // cada inyector necesita sus propios materiales del liquido (Load los comparte por modelo)
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = r.sharedMaterials.Select(m =>
                {
                    if (m == null || !(m.name.Contains("vial") || m.name.Contains("glass"))) return m;
                    var c = new Material(m) { name = m.name + "_" + id };
                    var tint = m.name.Contains("vial") ? color : new Color(color.r, color.g, color.b, 0.35f);
                    c.color = tint;
                    if (m.name.Contains("vial")) { c.SetColor("_EmissionColor", color * 0.6f); c.EnableKeyword("_EMISSION"); c.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
                    string p = Dir + InjectorModel + "/m_" + id + "_" + Regex.Replace(m.name, "[^A-Za-z0-9_]", "_") + ".mat";
                    AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(c, p);
                    return c;
                }).ToArray();
            // una malla del FBX (la tapa de la aguja) viene sin material: se le pone el plastico
            var plastic = visual.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).FirstOrDefault(m => m != null && m.name.Contains("plastic"));
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                if (r.sharedMaterials.Any(m => m == null)) r.sharedMaterials = r.sharedMaterials.Select(m => m ?? plastic).ToArray();
            visual.transform.SetParent(root.transform, false);
            // el eje mas largo queda vertical (como las latas: HandModel alinea el eje largo del objeto original con el nuestro)
            var b = Bounds(root);
            if (b.size.z >= b.size.x && b.size.z >= b.size.y) visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            else if (b.size.x >= b.size.y) visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            b = Bounds(root);
            visual.transform.localScale *= InjectorLength / b.size.y;
            b = Bounds(root);
            visual.transform.position -= b.center - root.transform.position;
            b = Bounds(root);
            var col = root.AddComponent<BoxCollider>(); col.center = b.center; col.size = b.size;
            var pivot = root.AddComponent<PreviewPivot>();
            pivot.pivotPosition = Vector3.zero; pivot.pivotRotation = Quaternion.identity; pivot.scale = Vector3.one;
            pivot.Icon.rotation = Quaternion.Euler(0f, 0f, 90f);      // icono tumbado, que en 1x2 casillas se vea entero
            pivot.Icon.boundsScale = 1f;
            string path = "Assets/Prefabs/bo6_inj_" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            // dos copias con distinto nombre: la del servidor (mundo/inventario) y la del cliente (en la mano); el juego no deja cargar dos veces el mismo bundle
            foreach (var name in new[] { id + "_inj", id + "_inj_hand" })
            {
                string dir = "Build/" + name;
                Directory.CreateDirectory(dir);
                BuildPipeline.BuildAssetBundles(dir, new[] { new AssetBundleBuild { assetBundleName = name + ".bundle", assetNames = new[] { path } } },
                    BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            }
            Debug.Log("[BO6] inyector " + id + ": " + b.size);
            return path;
        }

        private static void PreviewRow(string file, string[] prefabs, float spacing, float size)
        {
            Directory.CreateDirectory("Preview");
            var cam = new GameObject("cam").AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = size; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.24f, 0.27f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 50f; cam.transform.position = new Vector3(0f, 0f, -10f);
            var light = new GameObject("light").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(25f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var insts = new List<GameObject>();
            for (int i = 0; i < prefabs.Length; i++)
            {
                var g = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]));
                g.transform.position = new Vector3((i - (prefabs.Length - 1) / 2f) * spacing, 0f, 0f);
                insts.Add(g);
            }
            var rt = new RenderTexture(2000, 600, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(2000, 600, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 2000, 600), 0, 0); tex.Apply();
            File.WriteAllBytes("Preview/" + file + ".png", tex.EncodeToPNG());
            foreach (var g in insts) Object.DestroyImmediate(g);
            Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(light.gameObject);
        }

        // ---------------------------------------------------------------- vistas previas
        public static void Preview()
        {
            Directory.CreateDirectory("Preview");
            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.24f, 0.27f, 1f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 50f;
            var lightGo = new GameObject("light"); var light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(25f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var rt = new RenderTexture(2000, 600, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;

            void Shot(string file, string[] prefabs, float spacing, float size, float height, bool lit)
            {
                var insts = new List<GameObject>();
                for (int i = 0; i < prefabs.Length; i++)
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]); if (p == null) continue;
                    var g = (GameObject)Object.Instantiate(p);
                    g.transform.position = new Vector3((i - (prefabs.Length - 1) / 2f) * spacing, 0f, 0f);
                    var on = g.transform.Find("on"); var off = g.transform.Find("off");
                    if (on != null && off != null) { on.gameObject.SetActive(lit); off.gameObject.SetActive(!lit); }
                    insts.Add(g);
                }
                foreach (int yaw in new[] { 0 })
                {
                    foreach (var g in insts) g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    cam.orthographicSize = size; camGo.transform.position = new Vector3(0f, height, -10f); camGo.transform.rotation = Quaternion.identity;
                    cam.Render();
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var tex = new Texture2D(2000, 600, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 2000, 600), 0, 0); tex.Apply();
                    RenderTexture.active = prev;
                    File.WriteAllBytes("Preview/" + file + "_" + yaw + ".png", tex.EncodeToPNG()); Object.DestroyImmediate(tex);
                }
                foreach (var g in insts) Object.DestroyImmediate(g);
            }
            var machines = MachineModel.Keys.Select(k => "Assets/Prefabs/machine_" + k + ".prefab").Concat(new[] { "Assets/Prefabs/machine_widowswine.prefab", "Assets/Prefabs/power_switch.prefab" }).ToArray();
            Shot("bo6_machines_off", machines, 1.45f, 1.5f, 1.1f, false);
            Shot("bo6_machines_on", machines, 1.45f, 1.5f, 1.1f, true);
            Shot("bo6_cans", CanModel.Keys.Select(k => "Assets/Prefabs/bo6_can_" + k + ".prefab").ToArray(), 0.12f, 0.1f, 0f, true);
            Debug.Log("[BO6] vistas previas generadas");
        }
    }
}
