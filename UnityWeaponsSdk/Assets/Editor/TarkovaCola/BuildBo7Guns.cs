using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Armas de BO7 como accesorios de EFT:
//   Unity.exe -batchmode -quit -projectPath UnityWeapons -executeMethod TarkovaCola.BuildBo7Guns.Run   (sin -nographics: vista previa)
// MK35 ISR (ar_finch) sobre la HK416A5: receptor superior, canon, cargador, empunadura y culata son accesorios (ranuras de la HK416).
// VMP (sm_civet) sobre la MP5 Navy: receptor (cuerpo + canon + empunadura de BO7), cargador y culata son accesorios.
// Las piezas se colocan con los tag_* de BO7 (el receptor en el origen) y todo se lleva al espacio del hueso "weapon" de EFT con
// R (BO7 en Unity: canon -X, arriba +Y, derecha +Z  ->  EFT: canon -Y, arriba +Z, derecha +X) y un desplazamiento que pone la
// empunadura de BO7 donde EFT pone la mano derecha.
// Salida: Build/guns/assets/content/items/mods/tarkovacola/<pieza>.bundle, Preview/bo7_<arma>.png
namespace TarkovaCola
{
    public static class BuildBo7Guns
    {
        private const string Dir = "Assets/TarkovaCola/Models/bo7/";
        private const string BundleDir = "assets/content/items/mods/tarkovacola/";
        private static readonly Quaternion R = Quaternion.LookRotation(Vector3.right, Vector3.forward);

        private class Pose { public Vector3 p; public Quaternion q = Quaternion.identity; public Pose(Vector3 p, Quaternion q) { this.p = p; this.q = q; } }
        // EFT monta cada accesorio en su ranura con localRotation = Euler(90,0,0) (ContainerCollectionView.SlotView.InsertItem)
        private static readonly Quaternion E90 = Quaternion.Euler(90f, 0f, 0f);
        private static Pose ModRoot(Pose slot) => new Pose(slot.p, slot.q * E90);
        private static Pose Rel(Pose parent, Pose child) => new Pose(Quaternion.Inverse(parent.q) * (child.p - parent.p), Quaternion.Inverse(parent.q) * child.q);

        public static void Run()
        {
            AssetDatabase.Refresh();
            ConfigureTextures();
            Directory.CreateDirectory("Assets/TarkovaCola/Prefabs");
            var mods = new List<string>();
            try { mods.AddRange(BuildMk35()); } catch (System.Exception e) { Debug.LogError("[BO7] MK35: " + e); }
            try { mods.AddRange(BuildVmp()); } catch (System.Exception e) { Debug.LogError("[BO7] VMP: " + e); }
            EftBundles.UnloadAll();

            var builds = mods.Select(p => new AssetBundleBuild { assetBundleName = BundleDir + Path.GetFileNameWithoutExtension(p) + ".bundle", assetNames = new[] { p } }).ToList();
            Directory.CreateDirectory("Build/guns");
            BuildWithImposters("Build/guns", builds);
            Debug.Log("[BO7] " + builds.Count + " bundles");
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) Preview();
        }

        private const string EftShader = "Assets/Shader Assets/Bumped Specular SMap.shader", EftCube = "Assets/Cubemaps/obshaga_blurred_DXT1.png";

        // Como el "imposter" del SDK (com.bmpq.assetbundlebrowser-imposter): los shaders y cubemaps de EFT del proyecto se construyen como los
        // bundles "shaders" y "cubemaps" con los mismos CAB/PathID que los del juego, se borran, y nuestras piezas quedan enlazadas a los de verdad
        // (en bundles.json: dependencyKeys "shaders" y "cubemaps")
        private static void BuildWithImposters(string outDir, List<AssetBundleBuild> builds)
        {
            var gen = new AssetBundleBrowser.ImposterIdentifierGenerator();
            var imposters = new[] { "shaders", "cubemaps" };
            var all = new List<AssetBundleBuild>(builds);
            foreach (var name in imposters)
            {
                var paths = AssetDatabase.GetAssetPathsFromAssetBundle(name);
                if (paths.Length == 0) continue;
                all.Add(new AssetBundleBuild { assetBundleName = name, assetNames = paths });
                var cab = global::AssetUserDataHelper.GetData<string>(paths[0], AssetBundleBrowser.Imposter.ImposterBuilder.CanonicalCabIDKey);
                if (!string.IsNullOrEmpty(cab)) gen.CustomCabIdMap[name] = cab;
            }
            Directory.CreateDirectory(outDir);
            var prms = new UnityEditor.Build.Pipeline.BundleBuildParameters(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, outDir)
                { BundleCompression = UnityEngine.BuildCompression.LZ4, UseCache = false };
            var code = UnityEditor.Build.Pipeline.ContentPipeline.BuildAssetBundles(prms, new UnityEditor.Build.Pipeline.BundleBuildContent(all), out var res,
                UnityEditor.Build.Pipeline.DefaultBuildTasks.Create(UnityEditor.Build.Pipeline.DefaultBuildTasks.Preset.AssetBundleCompatible), gen);
            if (code != UnityEditor.Build.Pipeline.ReturnCode.Success) throw new System.Exception("fallo al construir los bundles: " + code);
            foreach (var name in imposters) { var f = Path.Combine(outDir, name); if (File.Exists(f)) File.Delete(f); }
        }

        // ---------------------------------------------------------------- MK35 ISR
        private static IEnumerable<string> BuildMk35()
        {
            var slots = Slots("assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle", "weapon_hk_416a5_556x45_model.generated");
            var rec = Part("sat_vm_ar_finch_rec");
            // el cargador y la empunadura de BO7 encima de un cargador STANAG y una empunadura de HK416 montados (asi encajan las piezas de AR)
            var anchor = Fit("assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle", "weapon_hk_416a5_556x45_model.generated",
                rec, Part("sat_vm_ar_finch_mag"), Part("sat_vm_ar_finch_pgrip"),
                "magazines/mag_stanag_hk_416_steel_maritime_556x45_30.bundle", "pistol grips/pistolgrip_ar15_hk_battle_grip_beavertail.bundle");
            float S = _scale; _mk35Scale = S;
            System.Func<Vector3, Vector3> W = v => R * (v * S) + anchor;

            var res = new List<string>();
            var recRoot = ModRoot(slots["mod_reciever"]);
            var bar = Part("sat_vm_ar_finch_bar");
            var barRoot = new Pose(W(rec.Tag("tag_barrel_attach")), recRoot.q);
            // miras de hierro: la mira trasera (objeto de mira) en una ranura del receptor, con su linea a la altura de la punta del punto de mira
            var irons = Irons((rec, W(Vector3.zero)), (bar, barRoot.p));
            var (rearPath, alignUp) = RearSight("mk35_sight_rear"); res.Add(rearPath);
            float sightZ = irons.lineZ;
            res.Add(MakeMod("mk35_upper", recRoot, new[] { (rec, W(Vector3.zero)) }, new Dictionary<string, Vector3>
            {
                ["mod_barrel"] = W(rec.Tag("tag_barrel_attach")),
                ["mod_scope"] = W(rec.Tag("tag_scope")),
                ["mod_sight_rear"] = new Vector3(0f, irons.rearY, irons.lineZ - alignUp),
            }, null));
            Debug.Log("[BO7] MK35 miras: trasera y=" + irons.rearY.ToString("F3") + ", punto y=" + irons.frontY.ToString("F3") + ", linea z=" + irons.lineZ.ToString("F3"));
            // ranuras y pose de la mano izquierda: las del guardamanos de la HK416, llevadas a la superficie del de BO7
            var hg = Handguard(slots, "assets/content/items/mods/recievers/reciever_ar15_hk_hk416a5_std.bundle", "handguards/handguard_416_hk_quad_rail.bundle");
            float bottom = W(rec.Tag("tag_barrel_attach")).z + (R * (bar.Tag("tag_grip_attach") * S)).z;
            float top = W(rec.Tag("tag_scope")).z;
            var side = R * (bar.Tag("tag_laser_attach") * S);
            float dzBottom = bottom - hg["mod_foregrip"].p.z, dzTop = top - hg["mod_tactical_000"].p.z, dx = Mathf.Abs(side.x) - Mathf.Abs(hg["mod_tactical_002"].p.x);
            float dzSide = (barRoot.p + side).z - hg["mod_tactical_002"].p.z;
            Debug.Log("[BO7] MK35 guardamanos: abajo " + dzBottom.ToString("F3") + ", arriba " + dzTop.ToString("F3") + ", lados " + dx.ToString("F3") + "/" + dzSide.ToString("F3") + ", miras a z=" + sightZ.ToString("F3"));
            res.Add(MakeMod("mk35_barrel", barRoot, new[] { (bar, barRoot.p) }, new Dictionary<string, Vector3> { ["mod_muzzle"] = barRoot.p + R * (bar.Tag("tag_flash") * S), ["fireport"] = barRoot.p + R * (bar.Tag("tag_flash") * S) }, null, hot: true,
                extra: go =>
                {
                    foreach (var kv in hg)
                    {
                        var pz = kv.Value.p;
                        if (kv.Key == "mod_foregrip" || kv.Key == "mod_bipod") pz.z += dzBottom;
                        else if (kv.Key == "mod_tactical_000" || kv.Key == "mod_sight_front" || kv.Key == "mod_scope") pz.z += dzTop;
                        else if (kv.Key == "mod_tactical_001" || kv.Key == "mod_tactical_002") { pz.x += Mathf.Sign(pz.x) * dx; pz.z += dzSide; }
                        else pz.z += (dzBottom + dzTop) * 0.5f;                         // las manos (GripPose): entre medias
                        if (kv.Key == "mod_scope") continue;                             // ya hay mira en el receptor
                        AddChild(go, barRoot, kv.Key, new Pose(pz, kv.Value.q), _grips.TryGetValue(kv.Key, out var g) ? g : null);
                    }
                    // la punta del punto de mira de BO7 (EFT alinea la vista entre mod_align_rear de la mira y este punto)
                    AddChild(go, barRoot, "mod_align_front", new Pose(new Vector3(0f, irons.frontY, irons.lineZ), barRoot.q * Quaternion.Inverse(E90)));
                }));
            res.Add(MakeMod("mk35_mag", ModRoot(slots["mod_magazine"]), new[] { (Part("sat_vm_ar_finch_mag"), W(rec.Tag("tag_mag_attach"))) }, null,
                "assets/content/items/mods/magazines/mag_stanag_hk_416_steel_maritime_556x45_30.bundle"));
            res.Add(MakeMod("mk35_pgrip", ModRoot(slots["mod_pistol_grip"]), new[] { (Part("sat_vm_ar_finch_pgrip"), W(rec.Tag("tag_pistolgrip_attach"))) }, null, null));
            res.Add(MakeMod("mk35_stock", ModRoot(slots["mod_stock"]), new[] { (Part("sat_vm_ar_finch_stock"), W(rec.Tag("tag_stock_attach"))) }, null, null));
            return res;
        }

        // hijo con pose (espacio del arma) en un prefab cuya raiz esta en 'root'; si se da 'copy', el hijo es ese objeto (con sus hijos)
        private static void AddChild(GameObject go, Pose root, string name, Pose w, GameObject copy = null)
        {
            var t = copy != null ? Object.Instantiate(copy).transform : new GameObject(name).transform;
            t.name = name;
            // los GripPose copiados siguen apuntando al script dentro del bundle de EFT: se cambian por el de este proyecto con los mismos datos
            foreach (var gp in t.GetComponentsInChildren<GripPose>(true))
            {
                var host = gp.gameObject; var data = JsonUtility.ToJson(gp);
                Object.DestroyImmediate(gp);
                JsonUtility.FromJsonOverwrite(data, host.AddComponent<GripPose>());
            }
            t.SetParent(go.transform, false);
            var l = Rel(root, w);
            t.localPosition = l.p; t.localRotation = l.q;
        }

        // Miras de hierro de BO7: la mira trasera es lo mas alto de la mitad de atras de las piezas, el punto de mira lo mas alto de la de
        // delante (en el espacio del arma: delante = -Y). Devuelve (y de la trasera, y del punto, z de la linea de mira = punta del punto).
        private static (float rearY, float frontY, float lineZ) Irons(params (BoPart part, Vector3 at)[] parts)
        {
            var pts = new List<Vector3>();
            foreach (var (part, at) in parts)
            {
                var v = Visual(part);
                v.transform.position = at; v.transform.rotation = R; v.transform.localScale = Vector3.one * _scale;
                foreach (var mf in v.GetComponentsInChildren<MeshFilter>())
                    foreach (var p in mf.sharedMesh.vertices) pts.Add(mf.transform.TransformPoint(p));
                Object.DestroyImmediate(v);
            }
            float minY = pts.Min(p => p.y), maxY = pts.Max(p => p.y), mid = (minY + maxY) * 0.5f;
            var rear = pts.Where(p => p.y > mid).OrderByDescending(p => p.z).First();
            var front = pts.Where(p => p.y <= mid).OrderByDescending(p => p.z).First();
            return (rear.y, front.y, front.z);
        }

        // Mira trasera "de verdad" (objeto de mira de EFT, para poder apuntar con las miras de hierro): la plantilla es la mira plegable de la
        // HK416 sin sus mallas (las miras ya las dibuja la pieza de BO7); solo quedan su mod_aim_camera (el ojo) y su mod_align_rear.
        // Devuelve el prefab y la altura de mod_align_rear sobre la base de la mira (para colocar la ranura).
        private static (string path, float alignUp) RearSight(string id)
        {
            var src = EftBundles.Load(Mods + "sights rear/sight_rear_all_hk_416_flip_up.bundle").LoadAllAssets<GameObject>().First(g => g.GetComponent<PreviewPivot>() != null);
            var go = Object.Instantiate(src); go.name = id;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) if (r != null && r.gameObject != go) Object.DestroyImmediate(r.gameObject);
            var lod = go.GetComponent<LODGroup>(); if (lod != null) Object.DestroyImmediate(lod);
            float up = go.transform.Find("mod_align_rear").localPosition.y;
            string path = "Assets/TarkovaCola/Prefabs/" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[BO7] " + id + ": mira trasera (alineacion a " + up.ToString("F3") + " sobre su base)");
            return (path, up);
        }

        // lo mas alto de una pieza de BO7 colocada en 'at' (espacio del arma), con su escala
        private static float TopZ(BoPart part, Vector3 at, float s)
        {
            var v = Visual(part);
            v.transform.position = at; v.transform.rotation = R; v.transform.localScale = Vector3.one * s;
            float z = float.MinValue;
            foreach (var r in v.GetComponentsInChildren<Renderer>()) z = Mathf.Max(z, r.bounds.max.z);
            Object.DestroyImmediate(v);
            return z;
        }

        // Guardamanos de EFT montado en su receptor sobre el arma: pose (espacio del arma) de sus ranuras y de sus GripPose (manos).
        // Las GripPose (palma con dedos) se guardan en _grips para copiarlas tal cual.
        private static readonly Dictionary<string, GameObject> _grips = new Dictionary<string, GameObject>();
        private static Dictionary<string, Pose> Handguard(Dictionary<string, Pose> weaponSlots, string receiverBundle, string handguardBundle)
        {
            System.Func<string, GameObject> load = b => EftBundles.Load(b).LoadAllAssets<GameObject>().First(g => g.GetComponent<PreviewPivot>() != null);
            var upRoot = ModRoot(weaponSlots["mod_reciever"]);
            var upSlot = load(receiverBundle).transform.GetComponentsInChildren<Transform>(true).First(t => t.name == "mod_handguard");
            var hgSlotW = new Pose(upRoot.p + upRoot.q * upSlot.localPosition, upRoot.q * upSlot.localRotation);
            var hgRoot = ModRoot(hgSlotW);
            var res = new Dictionary<string, Pose>();
            foreach (Transform ch in load(Mods + handguardBundle).transform)
            {
                bool grip = ch.GetComponent<GripPose>() != null;
                if (!grip && !ch.name.StartsWith("mod_")) continue;
                res[ch.name] = new Pose(hgRoot.p + hgRoot.q * ch.localPosition, hgRoot.q * ch.localRotation);
                if (grip) _grips[ch.name] = ch.gameObject;
            }
            return res;
        }

        // escala de las piezas de BO7 (la fija Fit; 1 = la del modelo)
        private static float _scale = 1f;

        // Encaje: con el arma de EFT montada con 'eftMag' y 'eftGrip', se busca escala y posicion (sin giro) para que la parte de arriba del
        // cargador de BO7 y el centro de su empunadura caigan donde los de EFT. Devuelve el desplazamiento de W y deja la escala en _scale.
        private static Vector3 Fit(string bundle, string model, BoPart rec, BoPart mag, BoPart grip, string eftMag, string eftGrip)
        {
            var (eft, w) = Base(bundle, model);
            var em = LocalBounds(Mount(Slot(w, "mod_magazine"), null, eftMag), w);
            var eg = LocalBounds(Mount(Slot(w, "mod_pistol_grip"), null, eftGrip), w);
            // BO7 con escala 1 y la empunadura en la ranura (solo para medir)
            var a0 = Slot(w, "mod_pistol_grip").localPosition - R * rec.Tag("tag_pistolgrip_attach");
            System.Func<BoPart, string, Bounds> bo = (p, tag) =>
            {
                var v = Visual(p); v.transform.SetParent(w, false); v.transform.localPosition = R * rec.Tag(tag) + a0; v.transform.localRotation = R;
                var b = LocalBounds(v, w); Object.DestroyImmediate(v); return b;
            };
            var bm = bo(mag, "tag_mag_attach"); var bg = bo(grip, "tag_pistolgrip_attach");
            Object.DestroyImmediate(eft);
            System.Func<Bounds, Vector3> top = b => new Vector3(0f, b.center.y, b.max.z);
            System.Func<Bounds, Vector3> mid = b => new Vector3(0f, b.center.y, b.center.z);
            Vector3 e1 = top(em), e2 = mid(eg), b1 = top(bm), b2 = mid(bg);
            _scale = (e1 - e2).magnitude / (b1 - b2).magnitude;
            var anchor = e1 - _scale * (b1 - a0); anchor.x = 0f;
            Debug.Log("[BO7] encaje " + model + ": escala " + _scale.ToString("F3") + ", cargador EFT " + em.size.ToString("F3") + " BO7 " + bm.size.ToString("F3") + ", empunadura EFT " + eg.size.ToString("F3") + " BO7 " + bg.size.ToString("F3"));
            return anchor;
        }

        // caja de las mallas (sin LOD1+) en el espacio de 'space'. Las mallas de EFT no se pueden leer: se gira todo para que 'space' quede
        // alineado con el mundo y se usan las cajas de los renderers (las piezas van alineadas con el arma, asi que salen justas)
        private static Bounds LocalBounds(GameObject go, Transform space)
        {
            var root = space.root; var keep = root.rotation;
            root.rotation = Quaternion.Inverse(space.rotation) * root.rotation;
            bool any = false; var b = new Bounds();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r.name.Contains("LOD1") || r.name.Contains("LOD2") || r.name.Contains("LOD3")) continue;
                var lb = new Bounds(space.InverseTransformPoint(r.bounds.center), r.bounds.size);
                if (!any) { b = lb; any = true; } else b.Encapsulate(lb);
            }
            root.rotation = keep;
            return b;
        }

        // ---------------------------------------------------------------- VMP (sobre la MP5)
        // En la MP5 la empunadura va en el armazon y el receptor superior es un accesorio (guardamanos, culata, bocacha, montura).
        // La VMP: "vmp_upper" en la ranura del receptor con el cuerpo, el canon y la empunadura de BO7 y ranuras propias de culata, bocacha y
        // mira; cargador y culata aparte. Misma escala que la MK35; colocada para que la mano derecha de la MP5 coja su empunadura.
        private const string Mp5 = "assets/content/weapons/mp5/weapon_hk_mp5_navy3_9x19_container.bundle", Mp5Model = "weapon_hk_mp5_navy3_9x19_model.generated";
        private static float _mk35Scale = 0.834f;

        private static IEnumerable<string> BuildVmp()
        {
            var slots = Slots(Mp5, Mp5Model);
            _scale = _mk35Scale; float S = _scale;
            var rec = Part("sat_vm_sm_civet_rec"); var bar = Part("sat_vm_sm_civet_bar");
            var grip = GripFromPalm(Mp5);
            var anchor = grip - R * (rec.Tag("tag_pistolgrip_attach") * S);
            System.Func<Vector3, Vector3> W = v => R * (v * S) + anchor;

            var res = new List<string>();
            var upRoot = ModRoot(slots["mod_reciever"]);
            var muzzle = W(rec.Tag("tag_barrel_attach")) + R * (bar.Tag("tag_flash") * S);
            var hk = Slots("assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle", "weapon_hk_416a5_556x45_model.generated");
            var hg = Handguard(hk, "assets/content/items/mods/recievers/reciever_ar15_hk_hk416a5_std.bundle", "handguards/handguard_416_hk_quad_rail.bundle");
            var laser = W(rec.Tag("tag_laser_attach"));
            var irons = Irons((rec, W(Vector3.zero)));
            var (rearPath, alignUp) = RearSight("vmp_sight_rear"); res.Add(rearPath);
            float sightZ = irons.lineZ;
            res.Add(MakeMod("vmp_upper", upRoot, new[] { (rec, W(Vector3.zero)), (bar, W(rec.Tag("tag_barrel_attach"))), (Part("sat_vm_sm_civet_pgrip"), W(rec.Tag("tag_pistolgrip_attach"))) },
                new Dictionary<string, Vector3> { ["mod_stock"] = W(rec.Tag("tag_stock_attach")), ["mod_muzzle"] = muzzle, ["fireport"] = muzzle, ["mod_scope"] = W(rec.Tag("tag_scope")),
                                                  ["mod_sight_rear"] = new Vector3(0f, irons.rearY, irons.lineZ - alignUp), ["mod_align_front"] = new Vector3(0f, irons.frontY, irons.lineZ) }, null, hot: true,
                extra: go =>
                {
                    AddChild(go, upRoot, "mod_foregrip", new Pose(W(rec.Tag("tag_grip_attach")), hg["mod_foregrip"].q));
                    AddChild(go, upRoot, "mod_tactical_002", new Pose(new Vector3(Mathf.Abs(laser.x), laser.y, laser.z), hg["mod_tactical_002"].q));
                    AddChild(go, upRoot, "mod_tactical_001", new Pose(new Vector3(-Mathf.Abs(laser.x), laser.y, laser.z), hg["mod_tactical_001"].q));
                }));
            Debug.Log("[BO7] VMP miras: trasera y=" + irons.rearY.ToString("F3") + ", punto y=" + irons.frontY.ToString("F3") + ", linea z=" + sightZ.ToString("F3") + "; tactico lateral " + laser.ToString("F3"));
            res.Add(MakeMod("vmp_mag", ModRoot(slots["mod_magazine"]), new[] { (Part("sat_vm_sm_civet_mag"), W(rec.Tag("tag_mag_attach"))) }, null,
                "assets/content/items/mods/magazines/mag_mp5_hk_std_curved_9x19_30.bundle"));
            var stockRoot = new Pose(W(rec.Tag("tag_stock_attach")), upRoot.q);
            res.Add(MakeMod("vmp_stock", stockRoot, new[] { (Part("sat_vm_sm_civet_stock"), stockRoot.p) }, null, null));
            Debug.Log("[BO7] VMP sobre MP5: empunadura " + grip.ToString("F3") + ", boca " + muzzle.ToString("F3"));
            return res;
        }

        // donde debe ir la empunadura de BO7 para la mano derecha del arma: palma en reposo + (empunadura - palma) medido en la HK416
        private static Vector3 GripFromPalm(string bundle)
        {
            System.Func<string, (Vector3 palm, Vector3 grip)> measure = bnd =>
            {
                var rig = EftLab.LoadRig(bnd);
                Idle(rig);
                var w = rig.Bones["weapon"];
                var palm = w.InverseTransformPoint(rig.Bones["Base HumanRPalm"].position);
                var g = w.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "mod_pistol_grip");
                var gp = g != null ? w.InverseTransformPoint(g.position) : Vector3.zero;
                Object.DestroyImmediate(rig.Root);
                return (palm, gp);
            };
            var hk = measure("assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle");
            var res = measure(bundle).palm + (hk.grip - hk.palm); res.x = 0f;
            return res;
        }

        // ---------------------------------------------------------------- piezas de BO7
        private class BoPart
        {
            public string Name; public GameObject Fbx; public Dictionary<string, Vector3> Tags = new Dictionary<string, Vector3>();
            public Vector3 Tag(string n) { Vector3 v; if (!Tags.TryGetValue(n, out v)) throw new System.Exception(Name + " no tiene " + n); return v; }
        }

        private static BoPart Part(string model)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + model + "/" + model + ".fbx");
            if (fbx == null) throw new System.Exception("falta el FBX de " + model);
            var p = new BoPart { Name = model, Fbx = fbx };
            var go = Object.Instantiate(fbx);
            foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("tag_")) p.Tags[t.name] = t.position;
            Object.DestroyImmediate(go);
            return p;
        }

        // Prefab de accesorio de EFT: raiz [PreviewPivot] en 'root' (espacio del hueso weapon), las mallas de cada pieza de BO7 con su
        // origen en la posicion dada, ranuras hijas "mod_*", colision, y (cargadores) los patron_NNN del cargador de EFT
        private static string MakeMod(string id, Pose root, (BoPart part, Vector3 at)[] parts, Dictionary<string, Vector3> childSlots, string magBundle, bool hot = false, bool pivot = true, System.Action<GameObject> extra = null)
        {
            var go = new GameObject(id);
            var lod0 = new GameObject(id + "_LOD0");
            lod0.transform.SetParent(go.transform, false);
            foreach (var (part, at) in parts)
            {
                var vis = Visual(part);
                // pose en el espacio weapon: R * (pose en BO7) + at ; relativa a la raiz
                var local = Rel(root, new Pose(at, R));
                vis.transform.SetParent(lod0.transform, false);
                vis.transform.localPosition = local.p; vis.transform.localRotation = local.q; vis.transform.localScale = Vector3.one * _scale;
            }
            if (childSlots != null)
                foreach (var kv in childSlots)
                {
                    var s = new GameObject(kv.Key);
                    s.transform.SetParent(go.transform, false);
                    // la ranura hija gira -90 en X: lo que EFT monte en ella queda con el mismo giro que esta pieza
                    var l = Rel(root, new Pose(kv.Value, root.q * Quaternion.Inverse(E90)));
                    s.transform.localPosition = l.p; s.transform.localRotation = l.q;
                }
            extra?.Invoke(go);
            if (magBundle != null) CopyPatrons(go, magBundle, Rel(root, new Pose(parts[0].at, root.q)).p);
            // colision (como el _LOD1 de EFT)
            var rs = lod0.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var col = new GameObject(id + "_LOD1");
                col.transform.SetParent(go.transform, false);
                var bc = col.AddComponent<BoxCollider>(); bc.center = go.transform.InverseTransformPoint(b.center); bc.size = b.size;
                bc.enabled = false;                                                     // como en EFT: apagada; el juego la enciende solo cuando la pieza es un objeto suelto (en la mano chocaria con el jugador)
            }
            if (hot) foreach (var r in rs) r.gameObject.AddComponent<HotObject>();
            if (pivot) go.AddComponent<PreviewPivot>();
            string path = "Assets/TarkovaCola/Prefabs/" + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[BO7] " + id);
            return path;
        }

        private static void CopyPatrons(GameObject go, string magBundle, Vector3 shift)
        {
            var ab = EftBundles.Load(magBundle);
            var src = ab.LoadAllAssets<GameObject>().FirstOrDefault();
            if (src == null) { Debug.LogWarning("[BO7] sin cargador de EFT en " + magBundle); return; }
            int n = 0;
            foreach (Transform t in src.transform)
            {
                if (!t.name.StartsWith("patron_")) continue;
                var c = new GameObject(t.name);
                c.transform.SetParent(go.transform, false);
                c.transform.localPosition = t.localPosition + shift; c.transform.localRotation = t.localRotation;
                n++;
            }
            Debug.Log("[BO7] " + go.name + ": " + n + " balas copiadas de " + Path.GetFileName(magBundle));
        }

        // posiciones y giros de las ranuras del arma de EFT, en el espacio del hueso "weapon"
        private static Dictionary<string, Pose> Slots(string bundle, string model)
        {
            var ab = EftBundles.Load(bundle);
            var prefab = ab.LoadAllAssets<GameObject>().FirstOrDefault(g => g.name == model);
            if (prefab == null) throw new System.Exception("no esta " + model + " en " + bundle);
            var go = Object.Instantiate(prefab);
            var weapon = go.GetComponentsInChildren<Transform>().First(t => t.name == "weapon");
            var wp = new Pose(weapon.position, weapon.rotation);
            var res = new Dictionary<string, Pose>();
            foreach (var t in weapon.GetComponentsInChildren<Transform>()) res[t.name] = Rel(wp, new Pose(t.position, t.rotation));
            Object.DestroyImmediate(go);
            return res;
        }

        // ---------------------------------------------------------------- mallas y materiales (como BuildBo6)
        private static GameObject Visual(BoPart part)
        {
            string model = part.Name;
            var mats = ReadMats(model);
            var cache = new Dictionary<string, Material>();
            var src = Object.Instantiate(part.Fbx);
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
                    if (!cache.TryGetValue(n, out res)) cache[n] = res = MakeMaterial(model, n, mats);
                    return res;
                }).ToArray();
            }
            Object.DestroyImmediate(src);
            return root;
        }

        private static void ConfigureTextures()
        {
            foreach (var path in Directory.GetFiles(Dir, "*.png", SearchOption.AllDirectories))
            {
                var p = path.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;
                bool normal = p.EndsWith("_normal.png");
                if ((ti.textureType == TextureImporterType.NormalMap) == normal && ti.maxTextureSize == 2048) continue;
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal && !p.EndsWith("_gloss.png");
                ti.maxTextureSize = 2048;
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

        private static Material MakeMaterial(string model, string matName, Dictionary<string, Dictionary<string, string>> mats)
        {
            var key = mats.Keys.Where(k => matName.Contains(k) || k.Contains(matName)).OrderByDescending(k => k.Length).FirstOrDefault();
            var tex = key != null ? mats[key] : new Dictionary<string, string>();
            string path = Dir + model + "/m_" + Regex.Replace(matName, "[^A-Za-z0-9_]", "_") + ".mat";
            AssetDatabase.DeleteAsset(path);
            // el shader de las armas de EFT (impostor del SDK: en el juego enlaza al de verdad del bundle "shaders"); asi el mod de camuflajes
            // reconoce las piezas y la luz es la de las demas armas
            var m = new Material(AssetDatabase.LoadAssetAtPath<Shader>(EftShader)) { name = matName };
            string v;
            if (tex.TryGetValue("albedo", out v)) m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + model + "/" + v));
            if (tex.TryGetValue("normal", out v)) m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + model + "/" + v));
            if (tex.TryGetValue("gloss", out v)) m.SetTexture("_SpecMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + model + "/" + v));
            m.SetTexture("_Cube", AssetDatabase.LoadAssetAtPath<Texture>(EftCube));
            m.SetColor("_SpecColor", new Color(0.35f, 0.35f, 0.35f, 1f));             // el alfa del color de BO7 no es brillo: brillo moderado
            m.SetColor("_ReflectColor", new Color(1f, 1f, 1f, 0.15f));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ---------------------------------------------------------------- vista previa, montada como en el juego
        //   Unity.exe -batchmode -quit -projectPath UnityWeapons -executeMethod TarkovaCola.BuildBo7Guns.Preview
        // Por arma, 4 vistas: [configuracion de fabrica | con accesorios de EFT] x [de lado | tres cuartos]. Lo de EFT sale en gris.
        private const string Mods = "assets/content/items/mods/";
        public static void Preview()
        {
            var cam = new GameObject("cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            cam.orthographic = true; cam.orthographicSize = float.Parse(System.Environment.GetEnvironmentVariable("PREV_SIZE") ?? "0.3", System.Globalization.CultureInfo.InvariantCulture);
            var light = new GameObject("l").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(0.75f, 0.75f, 0.8f);
            var rt = new RenderTexture(800, 450, 24);
            var guns = new[] { "mk35", "vmp" };
            foreach (var gun in guns)
            {
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                for (int variant = 0; variant < 2; variant++)
                {
                    var (eft, weapon) = gun == "mk35" ? Base("assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle", "weapon_hk_416a5_556x45_model.generated")
                                                      : Base(Mp5, Mp5Model);
                    Assemble(gun, weapon, variant == 1);
                    var grey = new Material(Shader.Find("Standard")) { color = new Color(0.55f, 0.56f, 0.58f) };
                    foreach (var r in eft.GetComponentsInChildren<Renderer>()) if (!HasBo(r.transform) && r.enabled) r.sharedMaterials = r.sharedMaterials.Select(_ => grey).ToArray();
                    var c = weapon.position + weapon.rotation * new Vector3(0f, float.Parse(System.Environment.GetEnvironmentVariable("PREV_Y_" + gun.ToUpperInvariant()) ?? (gun == "mk35" ? "-0.3" : "-0.33"), System.Globalization.CultureInfo.InvariantCulture), float.Parse(System.Environment.GetEnvironmentVariable("PREV_Z") ?? "0", System.Globalization.CultureInfo.InvariantCulture));
                    for (int view = 0; view < 2; view++)
                    {
                        var dir = view == 0 ? new Vector3(1f, 0f, 0f) : new Vector3(0.8f, -0.55f, 0.35f).normalized;
                        cam.transform.position = c + weapon.rotation * dir * 1.5f;
                        cam.transform.LookAt(c, weapon.rotation * Vector3.forward);
                        light.transform.rotation = cam.transform.rotation * Quaternion.Euler(30f, -35f, 0f);
                        cam.targetTexture = rt; cam.Render();
                        RenderTexture.active = rt;
                        tex.ReadPixels(new Rect(0, 0, 800, 450), view * 800, (1 - variant) * 450, false);
                    }
                    Object.DestroyImmediate(eft);
                }
                tex.Apply();
                File.WriteAllBytes("Preview/bo7_" + gun + (System.Environment.GetEnvironmentVariable("PREV_TAG") ?? "") + ".png", tex.EncodeToPNG());
            }
            EftBundles.UnloadAll();
        }

        // lo que ve el ojo al apuntar sin mira: camara donde el cliente pone el aim_camera (altura de la marca bo7_aim), mirando por el canon
        //   Unity.exe -batchmode -quit -projectPath UnityWeapons -executeMethod TarkovaCola.BuildBo7Guns.AimView   -> Preview/aim_<arma>.png
        public static void AimView()
        {
            var cam = new GameObject("cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.62f, 0.7f);
            cam.nearClipPlane = 0.01f; cam.fieldOfView = 30f;
            var light = new GameObject("l").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.75f);
            var rt = new RenderTexture(960, 540, 24) { antiAliasing = 4 }; cam.targetTexture = rt;
            foreach (var gun in new[] { "mk35", "vmp" })
            {
                var (eft, w) = gun == "mk35" ? Base("assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle", "weapon_hk_416a5_556x45_model.generated") : Base(Mp5, Mp5Model);
                Assemble(gun, w, false);
                // el ojo: el mod_aim_camera de la mira trasera montada (lo que usa EFT al apuntar con miras de hierro)
                var aim = w.GetComponentsInChildren<Transform>(true).First(t => t.name == "mod_aim_camera");
                cam.transform.position = aim.position;
                cam.transform.rotation = Quaternion.LookRotation(w.rotation * Vector3.down, w.rotation * Vector3.forward);   // por el canon (-Y), arriba +Z
                light.transform.rotation = cam.transform.rotation * Quaternion.Euler(30f, -30f, 0f);
                cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                for (int x = 470; x <= 490; x++) tex.SetPixel(x, 270, Color.red); for (int y = 260; y <= 280; y++) tex.SetPixel(480, y, Color.red);
                tex.Apply(); File.WriteAllBytes("Preview/aim_" + gun + ".png", tex.EncodeToPNG());
                Debug.Log("[AIM] " + gun + ": ojo en " + w.InverseTransformPoint(aim.position).ToString("F3"));
                Object.DestroyImmediate(eft);
            }
            EftBundles.UnloadAll();
        }

        private static (GameObject, Transform) Base(string bundle, string model)
        {
            var eft = Object.Instantiate(EftBundles.Load(bundle).LoadAllAssets<GameObject>().First(g => g.name == model));
            var weapon = eft.GetComponentsInChildren<Transform>().First(t => t.name == "weapon");
            HideBase(weapon);
            return (eft, weapon);
        }

        internal static void HideBase(Transform weapon)
        {
            // como el cliente: fuera las mallas del arma base que no cuelgan de una ranura mod_*
            foreach (var r in weapon.GetComponentsInChildren<Renderer>(true))
            {
                bool mod = false;
                for (var t = r.transform; t != null && t != weapon; t = t.parent) if (t.name.StartsWith("mod_")) mod = true;
                if (!mod) r.enabled = false;
            }
        }

        internal static void Assemble(string gun, Transform w, bool eftParts) { if (gun == "mk35") Mk35(w, eftParts); else Vmp(w, eftParts); }

        private static Transform Slot(Transform under, string name) => under.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        // como SlotView.InsertItem: hijo de la ranura, en su origen, girado 90 en X
        private static GameObject Mount(Transform slot, string ours, string eftBundle = null)
        {
            GameObject inst = ours != null ? (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TarkovaCola/Prefabs/" + ours + ".prefab"))
                                           : Object.Instantiate(EftBundles.Load(Mods + eftBundle).LoadAllAssets<GameObject>().First(g => g.GetComponent<PreviewPivot>() != null));
            inst.transform.SetParent(slot, false);
            inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = E90; inst.transform.localScale = Vector3.one;
            return inst;
        }

        private static void Mk35(Transform w, bool eftParts)
        {
            var up = Mount(Slot(w, "mod_reciever"), "mk35_upper");
            var bar = Mount(Slot(up.transform, "mod_barrel"), "mk35_barrel");
            Mount(Slot(up.transform, "mod_sight_rear"), "mk35_sight_rear");
            Mount(Slot(bar.transform, "mod_muzzle"), null, "muzzle/muzzle_ar15_colt_usgi_a2_556x45.bundle");
            if (!eftParts) { Mount(Slot(w, "mod_stock"), "mk35_stock"); Mount(Slot(w, "mod_magazine"), "mk35_mag"); Mount(Slot(w, "mod_pistol_grip"), "mk35_pgrip"); }
            else
            {
                var tube = Mount(Slot(w, "mod_stock"), null, "stocks/stock_ar15_hk_enhanced_stock_tube.bundle");
                Mount(Slot(tube.transform, "mod_stock_000"), null, "stocks/stock_ar15_hk_slim_line.bundle");
                Mount(Slot(w, "mod_magazine"), null, "magazines/mag_stanag_hk_416_steel_maritime_556x45_30.bundle");
                Mount(Slot(w, "mod_pistol_grip"), null, "pistol grips/pistolgrip_ar15_hk_battle_grip_beavertail.bundle");
                Mount(Slot(up.transform, "mod_scope"), null, "scopes/scope_all_eotech_exps3.bundle");
                Mount(Slot(bar.transform, "mod_foregrip"), null, "foregrips/foregrip_all_bcm_mod_3.bundle");
                Mount(Slot(bar.transform, "mod_tactical_002"), null, "tactical/tactical_all_surefire_x400_vis_laser.bundle");
                Mount(Slot(bar.transform, "mod_tactical_001"), null, "tactical/tactical_all_surefire_xc1.bundle");
            }
        }

        private static void Vmp(Transform w, bool eftParts)
        {
            var up = Mount(Slot(w, "mod_reciever"), "vmp_upper");
            Mount(Slot(up.transform, "mod_sight_rear"), "vmp_sight_rear");
            Mount(Slot(up.transform, "mod_stock"), "vmp_stock");
            Mount(Slot(w, "mod_magazine"), "vmp_mag");
            var lug = Mount(Slot(up.transform, "mod_muzzle"), null, "muzzle/muzzle_mp5_hk_lug_adapter_9x19.bundle");
            if (eftParts)
            {
                Mount(Slot(up.transform, "mod_scope"), null, "scopes/scope_all_eotech_exps3.bundle");
                Mount(Slot(up.transform, "mod_foregrip"), null, "foregrips/foregrip_all_bcm_mod_3.bundle");
                Mount(Slot(up.transform, "mod_tactical_002"), null, "tactical/tactical_all_surefire_x400_vis_laser.bundle");
                Mount(Slot(up.transform, "mod_tactical_001"), null, "tactical/tactical_all_surefire_xc1.bundle");
            }
        }

        // ---------------------------------------------------------------- con las manos de EFT (rig de 1a persona del arma base)
        //   Unity.exe -batchmode -quit -projectPath UnityWeapons -executeMethod TarkovaCola.BuildBo7Guns.Hands
        //   HANDS_CLIPS="idle:0,reload:10/40/70" (clip:fotogramas a 30 fps)  HANDS_LIST=1 lista los clips   HANDS_EFT=1 con accesorios de EFT
        public static void Hands()
        {
            try
            {
                foreach (var gun in new[] { "mk35", "vmp" })
                {
                    var rig = EftLab.LoadRig(gun == "mk35" ? "assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle" : Mp5);
                    var weapon = rig.Bones["weapon"];
                    if (System.Environment.GetEnvironmentVariable("HANDS_BASE") != "1") { HideBase(weapon); Assemble(gun, weapon, System.Environment.GetEnvironmentVariable("HANDS_EFT") == "1"); }
                    if (System.Environment.GetEnvironmentVariable("HANDS_LIST") == "1") Debug.Log("[HANDS] " + gun + " clips: " + string.Join(", ", rig.Clips.Select(c => c.name + "(" + Mathf.RoundToInt(c.length * 30f) + ")")));
                    var spec = System.Environment.GetEnvironmentVariable("HANDS_CLIPS_" + gun.ToUpperInvariant()) ?? System.Environment.GetEnvironmentVariable("HANDS_CLIPS") ?? "idle:0";
                    var poses = new List<(AnimationClip, int)>();
                    foreach (var part in spec.Split(','))
                    {
                        var kv = part.Split(':');
                        if (kv[0] == "idle") { var ic = Idle(rig); if (ic != null) poses.Add((ic, ic.name.EndsWith("_out_to_idle") ? Mathf.RoundToInt(ic.length * 30f) : 0)); continue; }
                        var clip =rig.Clips.FirstOrDefault(c => c.name == kv[0]) ?? rig.Clips.FirstOrDefault(c => c.name.Contains(kv[0]) && !c.name.Contains("generated"));
                        if (clip == null) { Debug.LogWarning("[HANDS] " + gun + ": no hay clip " + kv[0]); continue; }
                        foreach (var fr in (kv.Length > 1 ? kv[1] : "0").Split('/')) poses.Add((clip, int.Parse(fr)));
                    }
                    if (poses.Count == 0) { Object.DestroyImmediate(rig.Root); continue; }
                    System.Environment.SetEnvironmentVariable("LAB_VIEW", "both");
                    System.Environment.SetEnvironmentVariable("LAB_COLS", "2"); System.Environment.SetEnvironmentVariable("LAB_W", "960"); System.Environment.SetEnvironmentVariable("LAB_H", "540");
                    EftLab.RenderSheet(rig, i => poses[i].Item1.SampleAnimation(rig.Root, poses[i].Item2 / 30f), Enumerable.Range(0, poses.Count).ToList(), "Preview/hands_" + gun + (System.Environment.GetEnvironmentVariable("HANDS_BASE") == "1" ? "_base" : "") + ".png");
                    Debug.Log("[HANDS] " + gun + ": " + string.Join(", ", poses.Select(p => p.Item1.name + "@" + p.Item2)));
                    Object.DestroyImmediate(rig.Root);
                }
            }
            catch (System.Exception e) { Debug.LogError("[HANDS] " + e); }
            finally { EftBundles.UnloadAll(); }
        }
        // ---------------------------------------------------------------- la VMP sobre otras armas de EFT (para elegir base)
        //   CMP_GUNS="mp7:assets/...container.bundle;uzi:..."   -> Preview/cmp_<nombre>.png (reposo de 1a persona, de lado y la base sola)
        // reposo: el clip _idle, o el final de _out_to_idle
        private static AnimationClip Idle(EftLab.Rig rig)
        {
            var clips = rig.Clips.Where(c => !c.name.Contains("generated")).ToList();
            var idle = clips.FirstOrDefault(c => c.name.EndsWith("_idle")) ?? clips.FirstOrDefault(c => c.name.EndsWith("_out_to_idle"));
            if (idle != null) idle.SampleAnimation(rig.Root, idle.name.EndsWith("_out_to_idle") ? idle.length : 0f);
            return idle;
        }

        public static void Compare()
        {
            try
            {
                var rec = Part("sat_vm_sm_civet_rec"); var bar = Part("sat_vm_sm_civet_bar");
                // donde queda la palma derecha respecto a la empunadura en reposo, medido en la HK416
                Vector3 palmToGrip;
                {
                    var hk = EftLab.LoadRig("assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle");
                    Idle(hk);
                    var w = hk.Bones["weapon"];
                    palmToGrip = w.InverseTransformPoint(w.GetComponentsInChildren<Transform>(true).First(t => t.name == "mod_pistol_grip").position) - w.InverseTransformPoint(hk.Bones["Base HumanRPalm"].position);
                    Object.DestroyImmediate(hk.Root);
                }
                foreach (var entry in (System.Environment.GetEnvironmentVariable("CMP_GUNS") ?? "").Split(';').Where(s => s.Contains(":")))
                {
                    var name = entry.Split(':')[0]; var bundle = entry.Substring(name.Length + 1);
                    for (int pass = 0; pass < 2; pass++)
                    {
                        var rig = EftLab.LoadRig(bundle);
                        var weapon = rig.Bones["weapon"];
                        var wp = new Pose(weapon.position, weapon.rotation);
                        var slots = weapon.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => Rel(wp, new Pose(g.First().position, g.First().rotation)));
                        var idle = Idle(rig);
                        var palm = weapon.InverseTransformPoint(rig.Bones["Base HumanRPalm"].position);
                        string how = "";
                        if (pass == 1)
                        {
                            HideBase(weapon);
                            foreach (var r in weapon.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                            // con empunadura propia: la de BO7 en su sitio; si no (cargador en la empunadura), el cargador de BO7 en el suyo
                            var grip = palm + palmToGrip; grip.x = 0f;
                            Vector3 anchor = grip - R * rec.Tag("tag_pistolgrip_attach");
                            how = "mano " + (grip - (slots.ContainsKey("mod_magazine") ? slots["mod_magazine"].p : grip)).ToString("F3") + " del cargador";
                            System.Func<Vector3, Vector3> W = v => R * v + anchor;
                            foreach (var (part, at) in new[] { (rec, W(Vector3.zero)), (bar, W(rec.Tag("tag_barrel_attach"))), (Part("sat_vm_sm_civet_pgrip"), W(rec.Tag("tag_pistolgrip_attach"))),
                                                               (Part("sat_vm_sm_civet_mag"), W(rec.Tag("tag_mag_attach"))), (Part("sat_vm_sm_civet_stock"), W(rec.Tag("tag_stock_attach"))) })
                            {
                                var vis = Visual(part); vis.name = "vmp_" + vis.name;
                                vis.transform.SetParent(weapon, false); vis.transform.localPosition = at; vis.transform.localRotation = R;
                            }
                        }
                        System.Environment.SetEnvironmentVariable("LAB_VIEW", System.Environment.GetEnvironmentVariable("CMP_VIEW") ?? "both"); System.Environment.SetEnvironmentVariable("LAB_COLS", "2");
                        System.Environment.SetEnvironmentVariable("LAB_W", System.Environment.GetEnvironmentVariable("CMP_W") ?? "640"); System.Environment.SetEnvironmentVariable("LAB_H", System.Environment.GetEnvironmentVariable("CMP_H") ?? "360");
                        EftLab.RenderSheet(rig, i => Idle(rig), new List<int> { 0 }, "Preview/cmp_" + name + (pass == 0 ? "_base" : "") + ".png");
                        Debug.Log("[CMP] " + name + " pasada " + pass + ": clip " + (idle != null ? idle.name : "ninguno") + " anclaje " + how);
                        Object.DestroyImmediate(rig.Root);
                    }
                }
            }
            catch (System.Exception e) { Debug.LogError("[CMP] " + e); }
            finally { EftBundles.UnloadAll(); }
        }
        // ---------------------------------------------------------------- escala: tamanos de piezas de EFT y de BO7 (en m, en su espacio)
        public static void Measure()
        {
            System.Func<GameObject, Vector3> size = go => { var rs = go.GetComponentsInChildren<Renderer>().Where(r => !r.name.Contains("LOD1") && !r.name.Contains("LOD2")).ToArray(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b.size; };
            foreach (var e in new[] { "magazines/mag_stanag_hk_416_steel_maritime_556x45_30.bundle", "pistol grips/pistolgrip_ar15_hk_battle_grip_beavertail.bundle", "magazines/mag_mp7_hk_std_46x30_40.bundle", "stocks/stock_ar15_hk_slim_line.bundle" })
            {
                var go = Object.Instantiate(EftBundles.Load(Mods + e).LoadAllAssets<GameObject>().First(g => g.GetComponent<PreviewPivot>() != null));
                Debug.Log("[MEDIDA] EFT " + Path.GetFileNameWithoutExtension(e) + " " + size(go).ToString("F3")); Object.DestroyImmediate(go);
            }
            foreach (var n in new[] { "sat_vm_ar_finch_mag", "sat_vm_ar_finch_pgrip", "sat_vm_sm_civet_mag", "sat_vm_ar_finch_stock", "sat_vm_ar_finch_rec" })
            {
                var go = Visual(Part(n)); Debug.Log("[MEDIDA] BO7 " + n + " " + size(go).ToString("F3")); Object.DestroyImmediate(go);
            }
            EftBundles.UnloadAll();
        }
        private static bool HasBo(Transform t) { for (; t != null; t = t.parent) if (t.name.StartsWith("mk35_") || t.name.StartsWith("vmp_")) return true; return false; }
    }
}
