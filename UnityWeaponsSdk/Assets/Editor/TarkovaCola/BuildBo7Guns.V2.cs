using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Armas de BO7, segunda generacion (docs/Plan_armas_BO7.md): tabla de armas, piezas con partes moviles y agarres propios.
//   BO7_GUN=pk  Unity.exe -batchmode -quit -projectPath UnityWeaponsSdk -executeMethod TarkovaCola.BuildBo7Guns.RunGun
// Partes moviles: los huesos de BO7 de GunDef.Movers (cerrojo, selector...) salen de la malla como nodos "bo7mv:<hueso>"; en el juego (y en
// el laboratorio) siguen a un nodo guia "bo7_<hueso>" hijo del hueso weapon, que es el que animan los clips horneados.
namespace TarkovaCola
{
    public static partial class BuildBo7Guns
    {
        internal sealed class GunDef
        {
            public string Id, Cod, Bundle, Model;
            public string[] Movers = new string[0];
            public string[] Drop = { "tag_sight_off", "tag_sight_off2", "tag_laser_hide" };   // mallas que no se dibujan (miras plegadas)
            public System.Func<IEnumerable<string>> Build;
            public System.Action<Transform, bool> Mount;
            public string[] Mods = new string[0];           // prefabs propios (para la vista previa)
            public string GripL, GripR;                     // prefabs que llevan el agarre de cada mano
            public string StockSlot = "mod_stock";          // ranura del arma base donde va la culata (fusil generico)
            public float GripRollL;                         // grados que baja el agarre de la mano izquierda girando alrededor del canon
            // altura (z, espacio del hueso weapon) de la linea de mira: centro de la abertura trasera y punta del punto de mira, medidas con
            // IronProbe (Preview/irons_<arma>.png). Irons() toma el vertice mas alto de cada mira (orejas, aro del punto): queda alta.
            public float SightZ = float.NaN;
            public string[] DropMeshes = new string[0];
            // metros que se aleja el arma (con las manos) de la camara en todas las animaciones: la base de EFT la deja mas cerca de lo que
            // BO7 la encuadra y al inspeccionar/recargar se metia en la camara (Bo7Anim.ViewProbe mide lo mas cerca que llega)
            public float ViewPush;
            public float LookPush;                          // metros de mas al inspeccionar (estado LOOK)
            public bool GripInUpper;                        // el arma base no tiene ranura de empunadura (MP9): la empunadura va en el receptor      // "<pieza>/<malla>" que no se dibujan (accesorios que trae el modelo de BO7)
        }

        private const string WeaponsDir = "assets/content/weapons/";
        internal static readonly Dictionary<string, GunDef> Guns = new Dictionary<string, GunDef>
        {
            ["mk35"] = new GunDef { Id = "mk35", Cod = "ar_finch", Bundle = WeaponsDir + "hk416/weapon_hk_416a5_556x45_container.bundle", Model = "weapon_hk_416a5_556x45_model.generated", Build = BuildMk35, Mount = Mk35 },
            ["vmp"] = new GunDef { Id = "vmp", Cod = "sm_civet", Bundle = Mp5, Model = Mp5Model, Build = BuildVmp, Mount = Vmp },
            ["pk"] = new GunDef
            {
                Id = "pk", Cod = "ar_condor", Bundle = WeaponsDir + "ak12/weapon_izhmash_ak12_545x39_container.bundle", Model = "weapon_izhmash_ak12_545x39_model.generated",
                Movers = new[] { "j_bolt", "j_fire_select", "j_mag_release_le", "j_mag_release_ri", "bone_ca7ec13876768fbf", "bone_cae4e93876cd86e9" },
                GripL = "pk_barrel", GripR = "pk_pgrip", GripRollL = 45f, StockSlot = "mod_stock_001",
                SightZ = 0.0594f,   // aro trasero y punta del poste (Irons daba 0.0624: las alas del punto)
            },
            // Voyak KT-3 (ar_heron) sobre el AK-74M
            ["vk"] = new GunDef
            {
                Id = "vk", Cod = "ar_heron", Bundle = WeaponsDir + "ak74m/weapon_izhmash_ak74m_545x39_container.bundle", Model = "weapon_izhmash_ak74m_545x39_model.generated",
                Movers = new[] { "j_bolt", "j_fire_select", "j_mag_release", "j_safety" }, GripL = "vk_barrel", GripR = "vk_pgrip",
                LookPush = 0.07f,   // y 7 cm mas al inspeccionar: ocupaba 62 % de la pantalla (p90; VST 51, MXR 48); ahora 42
                ViewPush = 0.07f,   // al inspeccionar el receptor llegaba a 8.5 cm de la camara y la mano a 11 (PK: 12 y 16); ahora 14 y 18
                SightZ = 0.0463f,   // ventana trasera y punta del poste (Irons daba 0.0488)
                DropMeshes = new[] { "sat_vm_ar_heron_rec/PorterMesh4", "sat_vm_ar_heron_rec/PorterMesh7" },   // caja de bateria (con cable y pulsador) y su pantalla, lado izquierdo
            },
            // MXR-17 (ar_eagle; modelos base "att_sat_vm_ar_eagle_*" importados como sat_vm_ar_eagle_*) sobre la SCAR-L Mk16.
            // El controlador de la SCAR va en el bundle _fde_container de su carpeta (EftBundles.Family).
            ["mx"] = new GunDef
            {
                Id = "mx", Cod = "ar_eagle", Bundle = WeaponsDir + "scar/weapon_fn_mk16_556x45_container.bundle", Model = "weapon_fn_mk16_556x45_model.generated",
                Movers = new[] { "j_bolt_carrier", "j_bolt", "j_charginghandle", "j_boltrel", "j_firesel", "j_magrel_l", "j_magrel_r" }, GripL = "mx_barrel", GripR = "mx_pgrip",
                ViewPush = 0.04f,   // la mano izquierda llegaba a 11.7 cm de la camara al sacarla y en la recarga en vacio; ahora 15
                SightZ = 0.0786f,   // orejas de la trasera y punta del poste (Irons daba 0.0839: el aro que rodea el poste)
            },
            // M10 Breacher (sh_corso) sobre la Mossberg 590A1: la 590 no tiene receptor como pieza (canon, guardamanos, tubo y culata van en el
            // arma). Receptor + canon de BO7 van juntos en la pieza del canon; la bomba (tag_guard_attach del canon) es una parte movil.
            ["br"] = new GunDef
            {
                Id = "br", Cod = "sh_corso", Bundle = WeaponsDir + "m590/weapon_mossberg_590a1_12g_container.bundle", Model = "weapon_mossberg_590a1_12g_model.generated",
                Movers = new[] { "j_bolt", "tag_guard_attach", "j_carrier", "j_gear", "j_trigger", "j_mag_rel" }, GripL = "br_barrel", GripR = "br_stock",
                ViewPush = 0.04f,   // la culata llegaba a 10.8 cm de la camara al inspeccionar; ahora 13
                Build = () => BuildShotgun(Guns["br"]), Mount = (w, e) => MountShotgun(Guns["br"], w, e),
            },
            // VST (sm_bear) sobre la MP9: sin ranura de empunadura, la de BO7 va dentro del receptor
            ["vs"] = new GunDef
            {
                Id = "vs", Cod = "sm_bear", Bundle = WeaponsDir + "mp9/weapon_bt_mp9_9x19_container.bundle", Model = "weapon_bt_mp9_9x19_model.generated",
                Movers = new[] { "j_bolt", "j_boltrelease", "j_firesel", "j_magrelease", "j_trigger" }, GripL = "vs_barrel", GripR = "vs_upper", GripInUpper = true,
            },
            // Hawker HX (sn_aden) sobre la AXMC (.338): una pieza (receptor, canon, empunadura y culata) en la ranura de empunadura
            ["hw"] = new GunDef
            {
                Id = "hw", Cod = "sn_aden", Bundle = WeaponsDir + "axmc/weapon_accuracy_international_axmc_86x70_container.bundle", Model = "weapon_accuracy_international_axmc_86x70_model.generated",
                Movers = new[] { "j_bolt", "j_trigger", "j_magrelease" }, GripL = "hw_pgrip", GripR = "hw_pgrip",
                Build = () => BuildSniper(Guns["hw"]), Mount = (w, e) => MountSniper(Guns["hw"], w, e),
            },
        };
        internal static GunDef Gun(string id) { GunDef d; if (!Guns.TryGetValue(id, out d)) throw new System.Exception("arma desconocida: " + id); return d; }

        internal const string AnimDir = "Assets/TarkovaCola/Anim/";
        internal static string[] _movers, _drop = new string[0], _dropMeshes = new string[0];

        private static void ReadableModels()
        {
            foreach (var path in Directory.GetFiles(Dir, "*.fbx", SearchOption.AllDirectories))
            {
                var mi = AssetImporter.GetAtPath(path.Replace('\\', '/')) as ModelImporter;
                if (mi == null || mi.isReadable) continue;
                mi.isReadable = true; mi.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- construir un arma
        public static void RunGun()
        {
            var def = Gun(System.Environment.GetEnvironmentVariable("BO7_GUN") ?? "pk");
            AssetDatabase.Refresh();
            ConfigureTextures();
            Directory.CreateDirectory("Assets/TarkovaCola/Prefabs");
            var mods = new List<string>();
            try
            {
                ReadableModels();
                _movers = def.Movers; _drop = def.Drop; _dropMeshes = def.DropMeshes;
                mods.AddRange(def.Build != null ? def.Build() : BuildRifle(def));
                _movers = null;
                EftBundles.UnloadAll();
                if (def.GripL != null || def.GripR != null) Bo7Anim.AddGripPoses(def);
            }
            catch (System.Exception e) { Debug.LogError("[BO7] " + def.Id + ": " + e); return; }
            finally { _movers = null; EftBundles.UnloadAll(); }
            var builds = mods.Select(p => new AssetBundleBuild { assetBundleName = BundleDir + Path.GetFileNameWithoutExtension(p) + ".bundle", assetNames = new[] { p } }).ToList();
            BuildWithImposters("Build/guns", builds);
            Debug.Log("[BO7] " + def.Id + ": " + builds.Count + " bundles");
        }

        // Desplazamiento del arma en el espacio del hueso weapon: el anima (tag_flash, a 'bore' del origen del receptor en BO7) a la altura
        // del anima del arma base (su fireport), centrada, y a lo largo del canon donde la palma derecha de BO7 en reposo (la muneca mas el
        // desplazamiento de palma de Bo7Anim.PalmShift) cae a la altura de la palma derecha de EFT. La mano de EFT sube o baja con la empunadura.
        internal static Vector3 AnchorFromRightHand(GunDef def, BoPart rec, float S, Vector3 bore, float fireportZ)
        {
            var rig = EftLab.LoadRig(def.Bundle);
            Idle(rig);
            var w = rig.Bones["weapon"];
            var palm = w.InverseTransformPoint(rig.Bones["Base HumanRPalm"].position);
            Object.DestroyImmediate(rig.Root);
            var skel = Bo7Anim.LoadSkel(AnimDir + "bo7_" + def.Cod + "_skel.json");
            var fk = new Bo7Anim.Fk(skel, Bo7Anim.LoadClip(AnimDir + "sat_vm_" + def.Cod + "_idle.json"), 0f);
            var P = Bo7Anim.FbxBasis(Bo7Anim.FbxScale(rec, skel));
            System.Func<string, Vector3> pt = b => R * (P.MultiplyPoint3x4(fk.Rel("j_gun", b).GetColumn(3)) * S);
            var frame = Bo7Anim.HandFrame(pt("j_wrist_ri"), pt("j_index_ri_1"), pt("j_pinky_ri_1"));
            Vector3 hand = frame.MultiplyPoint3x4(Bo7Anim.PalmShift);
            var res = new Vector3(0f, palm.y - hand.y, fireportZ - (R * (bore * S)).z);
            Debug.Log("[BO7] " + def.Id + " anclaje: palma derecha EFT " + palm.ToString("F3") + ", palma BO7 " + hand.ToString("F3") + " -> " + res.ToString("F3") + " (la mano queda " + ((hand.z + res.z - palm.z) * 100f).ToString("F1") + " cm por encima de la de EFT)");
            return res;
        }

        // ---------------------------------------------------------------- Peacekeeper Mk1 (ar_condor) sobre el AK-12
        // Fusil generico: receptor (ranura mod_reciever) con canon propio, cargador, empunadura y culata (def.StockSlot) sobre el arma base
        private static IEnumerable<string> BuildRifle(GunDef def)
        {
            string id = def.Id + "_";
            var slots = Slots(def.Bundle, def.Model);
            _scale = _mk35Scale; float S = _scale;
            string c = "sat_vm_" + def.Cod + "_";
            var rec = Part(c + "rec"); var bar = Part(c + "bar");
            var anchor = AnchorFromRightHand(def, rec, S, rec.Tag("tag_barrel_attach") + bar.Tag("tag_flash"), (slots.ContainsKey("fireport") ? slots["fireport"] : slots["patron_in_weapon"]).p.z);
            System.Func<Vector3, Vector3> Wp = v => R * (v * S) + anchor;

            var res = new List<string>();
            var recRoot = ModRoot(slots["mod_reciever"]);
            var barRoot = new Pose(Wp(rec.Tag("tag_barrel_attach")), recRoot.q);
            var irons = Irons((rec, Wp(Vector3.zero)), (bar, barRoot.p));
            if (!float.IsNaN(def.SightZ)) irons.lineZ = def.SightZ;
            var (rearPath, alignUp) = RearSight(id + "sight_rear"); res.Add(rearPath);
            var upParts = def.GripInUpper ? new[] { (rec, Wp(Vector3.zero)), (Part(c + "pgrip"), Wp(rec.Tag("tag_pistolgrip_attach"))) } : new[] { (rec, Wp(Vector3.zero)) };
            res.Add(MakeMod(id + "upper", recRoot, upParts, new Dictionary<string, Vector3>
            {
                ["mod_barrel"] = Wp(rec.Tag("tag_barrel_attach")),
                ["mod_scope"] = Wp(rec.Tag("tag_scope")),
                ["mod_sight_rear"] = new Vector3(0f, irons.rearY, irons.lineZ - alignUp),
            }, null));
            var hk = Slots(Guns["mk35"].Bundle, Guns["mk35"].Model);
            var hg = Handguard(hk, "assets/content/items/mods/recievers/reciever_ar15_hk_hk416a5_std.bundle", "handguards/handguard_416_hk_quad_rail.bundle");
            var muzzle = barRoot.p + R * (bar.Tag("tag_flash") * S);
            var laser = barRoot.p + R * (bar.Tag("tag_laser_attach") * S);
            res.Add(MakeMod(id + "barrel", barRoot, new[] { (bar, barRoot.p) }, new Dictionary<string, Vector3> { ["mod_muzzle"] = muzzle, ["fireport"] = muzzle }, null, hot: true,
                extra: go =>
                {
                    AddChild(go, barRoot, "mod_foregrip", new Pose(barRoot.p + R * (bar.Tag("tag_grip_attach") * S), hg["mod_foregrip"].q));
                    AddChild(go, barRoot, "mod_tactical_002", new Pose(new Vector3(Mathf.Abs(laser.x), laser.y, laser.z), hg["mod_tactical_002"].q));
                    AddChild(go, barRoot, "mod_tactical_001", new Pose(new Vector3(-Mathf.Abs(laser.x), laser.y, laser.z), hg["mod_tactical_001"].q));
                    AddChild(go, barRoot, "mod_align_front", new Pose(new Vector3(0f, irons.frontY, irons.lineZ), barRoot.q * Quaternion.Inverse(E90)));
                }));
            // sin balas de EFT (patron_NNN): las del cargador del AK-12 no caben en este y asomaban por fuera
            res.Add(MakeMod(id + "mag", ModRoot(slots["mod_magazine"]), new[] { (Part(c + "mag"), Wp(rec.Tag("tag_mag_attach"))) }, null, null));
            if (!def.GripInUpper) res.Add(MakeMod(id + "pgrip", ModRoot(slots["mod_pistol_grip"]), new[] { (Part(c + "pgrip"), Wp(rec.Tag("tag_pistolgrip_attach"))) }, null, null));
            res.Add(MakeMod(id + "stock", ModRoot(slots[def.StockSlot]), new[] { (Part(c + "stock"), Wp(rec.Tag("tag_stock_attach"))) }, null, null));
            Debug.Log("[BO7] " + def.Id + ": boca " + muzzle.ToString("F3") + ", miras z=" + irons.lineZ.ToString("F3") + ", laser " + laser.ToString("F3"));
            return res;
        }

        // sonda de miras: alturas (z) de los vertices del eje central (|x| < 1.5 mm) cerca de la mira trasera y del punto de mira, con sus huecos
        //   tools/u.sh BuildBo7Guns.IronProbe BO7_GUN=mx,vk,pk
        public static void IronProbe()
        {
            foreach (var gun in (System.Environment.GetEnvironmentVariable("BO7_GUN") ?? "pk").Split(','))
            {
                var def = Guns[gun];
                var slots = Slots(def.Bundle, def.Model);
                _scale = _mk35Scale; float S = _scale;
                string c = "sat_vm_" + def.Cod + "_";
                var rec = Part(c + "rec"); var bar = Part(c + "bar");
                var anchor = AnchorFromRightHand(def, rec, S, rec.Tag("tag_barrel_attach") + bar.Tag("tag_flash"), (slots.ContainsKey("fireport") ? slots["fireport"] : slots["patron_in_weapon"]).p.z);
                System.Func<Vector3, Vector3> Wp = v => R * (v * S) + anchor;
                var barP = Wp(rec.Tag("tag_barrel_attach"));
                var irons = Irons((rec, Wp(Vector3.zero)), (bar, barP));
                var pts = new List<Vector3>(); var vis = new List<GameObject>();
                foreach (var (part, at) in new[] { (rec, Wp(Vector3.zero)), (bar, barP) })
                {
                    var v = Visual(part); v.transform.position = at; v.transform.rotation = R; v.transform.localScale = Vector3.one * _scale;
                    foreach (var mf in v.GetComponentsInChildren<MeshFilter>()) foreach (var p in mf.sharedMesh.vertices) pts.Add(mf.transform.TransformPoint(p));
                    foreach (var mr in v.GetComponentsInChildren<Renderer>())
                        if (Mathf.Abs(mr.bounds.center.x) > 0.015f) Debug.Log("[AIM] " + gun + " malla lateral: " + mr.name + " (padre " + mr.transform.parent.name + ") centro " + mr.bounds.center.ToString("F3") + " tam " + mr.bounds.size.ToString("F3"));
                    vis.Add(v);
                }
                // cortes ortograficos desde atras (mirando a -Y): solo la franja de cada mira; fila r -> z = lineZ + 0.01 - r * 0.05 / 800
                var cam = new GameObject("c").AddComponent<Camera>();
                cam.orthographic = true; cam.orthographicSize = 0.025f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.white;
                var light = new GameObject("l").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
                RenderSettings.ambientLight = Color.gray;
                var rt = new RenderTexture(800, 800, 24); cam.targetTexture = rt;
                var tex = new Texture2D(1600, 800, TextureFormat.RGB24, false);
                int col = 0;
                foreach (var (y0, span) in new[] { (irons.rearY, float.Parse(Bo7Anim.Env("AIM_REAR_SPAN", "0.03"), System.Globalization.CultureInfo.InvariantCulture)), (irons.frontY, float.Parse(Bo7Anim.Env("AIM_FRONT_SPAN", "0.02"), System.Globalization.CultureInfo.InvariantCulture)) })
                {
                    cam.transform.position = new Vector3(0f, y0 + 0.1f, irons.lineZ - 0.015f);
                    cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                    light.transform.rotation = cam.transform.rotation * Quaternion.Euler(20f, 20f, 0f);
                    cam.nearClipPlane = 0.1f - span; cam.farClipPlane = 0.1f + span;
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 800, 800), col * 800, 0); col++;
                }
                for (int r = 0; r < 800; r += 40) for (int x = 0; x < 1600; x += 2) tex.SetPixel(x, 799 - r, (r / 40) % 5 == 0 ? Color.red : new Color(1f, 0.6f, 0.6f));
                for (int y = 0; y < 800; y++) { tex.SetPixel(400, y, Color.blue); tex.SetPixel(1200, y, Color.blue); }
                tex.Apply(); File.WriteAllBytes("Preview/irons_" + gun + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(light.gameObject); foreach (var v in vis) Object.DestroyImmediate(v);
                Debug.Log("[AIM] " + gun + ": Irons() trasera y=" + irons.rearY.ToString("F3") + " punto y=" + irons.frontY.ToString("F3") + " linea z=" + irons.lineZ.ToString("F4"));
                foreach (var (name, y0) in new[] { ("trasera", irons.rearY), ("punto", irons.frontY) })
                {
                    var zs = pts.Where(p => Mathf.Abs(p.x) < 0.0015f && Mathf.Abs(p.y - y0) < 0.02f).Select(p => p.z).OrderBy(z => z).ToList();
                    var sb = new System.Text.StringBuilder();
                    for (int i = 1; i < zs.Count; i++) if (zs[i] - zs[i - 1] > 0.002f) sb.Append(" hueco " + zs[i - 1].ToString("F4") + ".." + zs[i].ToString("F4"));
                    Debug.Log("[AIM] " + gun + " " + name + ": z " + (zs.Count > 0 ? zs[0].ToString("F4") + ".." + zs[zs.Count - 1].ToString("F4") : "-") + " (" + zs.Count + " vertices)" + sb);
                    // perfil lateral: por cada 2 mm de altura, el ancho |x| minimo y maximo de los vertices cercanos (ver aberturas)
                    var near = pts.Where(p => Mathf.Abs(p.y - y0) < 0.02f && p.z > irons.lineZ - 0.03f).ToList();
                    var prof = new System.Text.StringBuilder();
                    for (float z = irons.lineZ - 0.03f; z <= irons.lineZ + 0.004f; z += 0.002f)
                    {
                        var row = near.Where(p => p.z >= z && p.z < z + 0.002f).Select(p => Mathf.Abs(p.x)).ToList();
                        prof.Append(" | z" + z.ToString("F3") + ":" + (row.Count > 0 ? row.Min().ToString("F3") + "-" + row.Max().ToString("F3") : "vacio"));
                    }
                    Debug.Log("[AIM] " + gun + " " + name + " perfil" + prof);
                }
            }
            EftBundles.UnloadAll();
        }

        // huesos de una pieza de BO7 y la caja de los vertices de cada uno (espacio de la malla)   tools/u.sh BuildBo7Guns.PartBones BO7_PART=sat_vm_ar_heron_rec
        public static void PartBones()
        {
            var part = Part(System.Environment.GetEnvironmentVariable("BO7_PART"));
            var src = Object.Instantiate(part.Fbx);
            foreach (var smr in src.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh; if (mesh == null || !mesh.isReadable) { Debug.Log("[AIM] " + smr.name + " no legible"); continue; }
                var v = mesh.vertices; var bw = mesh.boneWeights;
                foreach (var g in Enumerable.Range(0, v.Length).GroupBy(i => bw[i].boneIndex0))
                {
                    var b = new Bounds(v[g.First()], Vector3.zero); foreach (var i in g) b.Encapsulate(v[i]);
                    Debug.Log("[AIM] " + smr.name + " hueso " + (g.Key < smr.bones.Length && smr.bones[g.Key] ? smr.bones[g.Key].name : "?" + g.Key) + ": " + g.Count() + " vertices, centro " + b.center.ToString("F2") + " tam " + b.size.ToString("F2"));
                }
            }
            // BO7_SHOW=<malla>: dos vistas (lado y arriba) de la pieza entera con esa malla en rojo -> Preview/part_<malla>.png
            string show = System.Environment.GetEnvironmentVariable("BO7_SHOW");
            if (!string.IsNullOrEmpty(show))
            {
                var all = new Bounds(); bool first = true;
                foreach (var r in src.GetComponentsInChildren<Renderer>(true))
                {
                    if (first) { all = r.bounds; first = false; } else all.Encapsulate(r.bounds);
                    var mats = r.sharedMaterials.Select(_ => new Material(Shader.Find("Unlit/Color")) { color = r.name == show ? Color.red : new Color(0.3f, 0.3f, 0.3f) }).ToArray();
                    r.sharedMaterials = mats;
                }
                var cam = new GameObject("c").AddComponent<Camera>(); cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.white;
                cam.orthographicSize = all.extents.magnitude * 0.6f; cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f;
                var rt = new RenderTexture(800, 600, 24); cam.targetTexture = rt;
                var tex = new Texture2D(2400, 600, TextureFormat.RGB24, false); int col = 0;
                foreach (var dir in new[] { Vector3.right, Vector3.up, Vector3.forward })
                {
                    cam.transform.position = all.center - dir * 10f; cam.transform.rotation = Quaternion.LookRotation(dir, dir == Vector3.up ? Vector3.forward : Vector3.up);
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 800, 600), col++ * 800, 0);
                }
                tex.Apply(); File.WriteAllBytes("Preview/part_" + show + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(cam.gameObject);
            }
            Object.DestroyImmediate(src);
        }

        // ---------------------------------------------------------------- escopeta de bomba (M590): canon = receptor + canon de BO7
        private static IEnumerable<string> BuildShotgun(GunDef def)
        {
            string id = def.Id + "_";
            var slots = Slots(def.Bundle, def.Model);
            _scale = _mk35Scale; float S = _scale;
            string c = "sat_vm_" + def.Cod + "_";
            var rec = Part(c + "rec"); var bar = Part(c + "bar");
            var anchor = AnchorFromRightHand(def, rec, S, rec.Tag("tag_barrel_attach") + bar.Tag("tag_flash"), slots["patron_in_weapon"].p.z);
            System.Func<Vector3, Vector3> Wp = v => R * (v * S) + anchor;
            var res = new List<string>();
            var barRoot = ModRoot(slots["mod_barrel"]);
            var barAt = Wp(rec.Tag("tag_barrel_attach"));
            var irons = Irons((rec, Wp(Vector3.zero)), (bar, barAt));
            if (!float.IsNaN(def.SightZ)) irons.lineZ = def.SightZ;
            var (rearPath, alignUp) = RearSight(id + "sight_rear"); res.Add(rearPath);
            var muzzle = barAt + R * (bar.Tag("tag_flash") * S);
            var laser = barAt + R * (bar.Tag("tag_laser_attach") * S);
            var hk = Slots(Guns["mk35"].Bundle, Guns["mk35"].Model);
            var hg = Handguard(hk, "assets/content/items/mods/recievers/reciever_ar15_hk_hk416a5_std.bundle", "handguards/handguard_416_hk_quad_rail.bundle");
            res.Add(MakeMod(id + "barrel", barRoot, new[] { (rec, Wp(Vector3.zero)), (bar, barAt) }, new Dictionary<string, Vector3>
            {
                ["mod_muzzle"] = muzzle, ["fireport"] = muzzle, ["mod_scope"] = Wp(rec.Tag("tag_scope")),
                ["mod_sight_rear"] = new Vector3(0f, irons.rearY, irons.lineZ - alignUp),
            }, null, hot: true, extra: go =>
            {
                AddChild(go, barRoot, "mod_tactical_001", new Pose(new Vector3(-Mathf.Abs(laser.x) - 0.02f, laser.y, laser.z), hg["mod_tactical_001"].q));
                AddChild(go, barRoot, "mod_align_front", new Pose(new Vector3(0f, irons.frontY, irons.lineZ), barRoot.q * Quaternion.Inverse(E90)));
            }));
            res.Add(MakeMod(id + "stock", ModRoot(slots["mod_stock"]), new[] { (Part(c + "stock"), Wp(rec.Tag("tag_stock_attach"))) }, null, null));
            // el tubo no se ve (va dentro del guardamanos de BO7): pieza vacia para el cargador de la 590
            res.Add(MakeMod(id + "mag", ModRoot(slots["mod_magazine"]), new (BoPart, Vector3)[0], null, null));
            Debug.Log("[BO7] " + def.Id + ": boca " + muzzle.ToString("F3") + ", miras z=" + irons.lineZ.ToString("F3") + " trasera y=" + irons.rearY.ToString("F3") + " punto y=" + irons.frontY.ToString("F3"));
            return res;
        }

        // ---------------------------------------------------------------- francotirador de cerrojo (AXMC): en la AXMC todo cuelga de la
        // empunadura (chasis: culata, receptor, guardamanos, canon); la de BO7 lleva receptor, canon, empunadura y culata en una pieza con la
        // ranura de mira, la bocacha y una linterna. El cerrojo de EFT (mod_charge, obligatorio) lo oculta el cliente; el de BO7 es parte movil.
        private static IEnumerable<string> BuildSniper(GunDef def)
        {
            string id = def.Id + "_";
            var slots = Slots(def.Bundle, def.Model);
            _scale = _mk35Scale; float S = _scale;
            string c = "sat_vm_" + def.Cod + "_";
            var rec = Part(c + "rec"); var bar = Part(c + "bar");
            var anchor = AnchorFromRightHand(def, rec, S, rec.Tag("tag_barrel_attach") + bar.Tag("tag_flash"), slots["patron_in_weapon"].p.z);
            System.Func<Vector3, Vector3> Wp = v => R * (v * S) + anchor;
            var res = new List<string>();
            var root = ModRoot(slots["mod_pistol_grip"]);
            var barAt = Wp(rec.Tag("tag_barrel_attach"));
            var muzzle = barAt + R * (bar.Tag("tag_flash") * S);
            Vector3 laser;
            if (bar.Tags.ContainsKey("tag_laser_attach")) laser = barAt + R * (bar.Tag("tag_laser_attach") * S);
            else if (rec.Tags.ContainsKey("tag_laser_attach")) laser = Wp(rec.Tag("tag_laser_attach"));
            else laser = Vector3.Lerp(barAt, muzzle, 0.3f) + new Vector3(0f, 0f, -0.01f);
            var hk = Slots(Guns["mk35"].Bundle, Guns["mk35"].Model);
            var hg = Handguard(hk, "assets/content/items/mods/recievers/reciever_ar15_hk_hk416a5_std.bundle", "handguards/handguard_416_hk_quad_rail.bundle");
            res.Add(MakeMod(id + "pgrip", root, new[]
            {
                (rec, Wp(Vector3.zero)), (bar, barAt), (Part(c + "pgrip"), Wp(rec.Tag("tag_pistolgrip_attach"))), (Part(c + "stock"), Wp(rec.Tag("tag_stock_attach"))),
            }, new Dictionary<string, Vector3> { ["mod_muzzle"] = muzzle, ["fireport"] = muzzle, ["mod_scope"] = Wp(rec.Tag("tag_scope")) }, null, hot: true,
            extra: go => AddChild(go, root, "mod_tactical_001", new Pose(new Vector3(-Mathf.Abs(laser.x) - 0.02f, laser.y, laser.z), hg["mod_tactical_001"].q))));
            res.Add(MakeMod(id + "mag", ModRoot(slots["mod_magazine"]), new[] { (Part(c + "mag"), Wp(rec.Tag("tag_mag_attach"))) }, null, null));
            Debug.Log("[BO7] " + def.Id + ": boca " + muzzle.ToString("F3") + ", mira " + Wp(rec.Tag("tag_scope")).ToString("F3") + ", laser " + laser.ToString("F3"));
            return res;
        }

        private static void MountSniper(GunDef def, Transform w, bool eftParts)
        {
            string id = def.Id + "_";
            var pg = Mount(Slot(w, "mod_pistol_grip"), id + "pgrip");
            Mount(Slot(w, "mod_magazine"), id + "mag");
            if (!eftParts) return;
            Mount(Slot(pg.transform, "mod_scope"), null, "scopes/scope_all_eotech_exps3.bundle");
        }

        private static void MountShotgun(GunDef def, Transform w, bool eftParts)
        {
            string id = def.Id + "_";
            var bar = Mount(Slot(w, "mod_barrel"), id + "barrel");
            Mount(Slot(bar.transform, "mod_sight_rear"), id + "sight_rear");
            Mount(Slot(w, "mod_stock"), id + "stock"); Mount(Slot(w, "mod_magazine"), id + "mag");
        }

        private static void MountRifle(GunDef def, Transform w, bool eftParts)
        {
            string id = def.Id + "_";
            var up = Mount(Slot(w, "mod_reciever"), id + "upper");
            var bar = Mount(Slot(up.transform, "mod_barrel"), id + "barrel");
            Mount(Slot(up.transform, "mod_sight_rear"), id + "sight_rear");
            Mount(Slot(w, "mod_magazine"), id + "mag"); if (!def.GripInUpper) Mount(Slot(w, "mod_pistol_grip"), id + "pgrip"); Mount(Slot(w, def.StockSlot), id + "stock");
            if (!eftParts) return;
            Mount(Slot(up.transform, "mod_scope"), null, "scopes/scope_all_eotech_exps3.bundle");
            Mount(Slot(bar.transform, "mod_foregrip"), null, "foregrips/foregrip_all_bcm_mod_3.bundle");
            Mount(Slot(bar.transform, "mod_tactical_002"), null, "tactical/tactical_all_surefire_x400_vis_laser.bundle");
        }

        // ---------------------------------------------------------------- partes moviles
        // Como Visual, pero los triangulos de cada hueso de 'movers' van a un nodo propio "bo7mv:<hueso>" colocado en el hueso (malla en su espacio)
        internal static GameObject VisualSplit(BoPart part, string[] movers)
        {
            string model = part.Name;
            var mats = ReadMats(model);
            var cache = new Dictionary<string, Material>();
            System.Func<Material, Material> conv = mm =>
            {
                string n = mm != null ? mm.name : "x";
                Material m;
                if (!cache.TryGetValue(n, out m)) cache[n] = m = MakeMaterial(model, n, mats);
                return m;
            };
            var src = Object.Instantiate(part.Fbx);
            var root = new GameObject(model);
            foreach (var r in src.GetComponentsInChildren<Renderer>(true))
            {
                var smr = r as SkinnedMeshRenderer;
                Mesh mesh = smr != null ? smr.sharedMesh : (r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null);
                if (mesh == null || _dropMeshes.Contains(model + "/" + r.name)) continue;
                var moving = smr != null ? Enumerable.Range(0, smr.bones.Length).Where(i => smr.bones[i] != null && movers.Contains(smr.bones[i].name)).ToList() : new List<int>();
                var dropped = smr != null ? Enumerable.Range(0, smr.bones.Length).Where(i => smr.bones[i] != null && _drop.Contains(smr.bones[i].name)).ToList() : new List<int>();
                if (System.Environment.GetEnvironmentVariable("BO7_VERBOSE") == "1") Debug.Log("[BO7] " + model + " malla " + r.name + ": " + r.GetType().Name + (smr != null ? " huesos " + smr.bones.Length + " [" + string.Join(",", smr.bones.Where(b => b != null).Select(b => b.name).Take(40)) + "]" : "") + " legible " + mesh.isReadable + " moviles " + moving.Count);
                if ((moving.Count == 0 && dropped.Count == 0) || !mesh.isReadable)
                {
                    Node(root, r.name, r.transform.position, r.transform.rotation, r.transform.lossyScale, mesh, r.sharedMaterials.Select(conv).ToArray());
                    continue;
                }
                var bw = mesh.boneWeights; var bind = mesh.bindposes;
                var groupOf = bw.Select(b => dropped.Contains(b.boneIndex0) ? -2 : moving.Contains(b.boneIndex0) && b.weight0 > 0.5f ? b.boneIndex0 : -1).ToArray();
                foreach (int g in new[] { -1 }.Concat(moving))
                {
                    var sub = SubMesh(mesh, groupOf, g, g >= 0 ? bind[g] : Matrix4x4.identity, out var used);
                    if (sub == null) continue;
                    var mm = used.Select(i => conv(r.sharedMaterials[Mathf.Min(i, r.sharedMaterials.Length - 1)])).ToArray();
                    if (g < 0) { sub.name = mesh.name + "_fijo"; Save(sub, model, r.name + "_fijo"); Node(root, r.name, r.transform.position, r.transform.rotation, r.transform.lossyScale, sub, mm); }
                    else
                    {
                        var b = smr.bones[g];
                        sub.name = mesh.name + "_" + b.name; Save(sub, model, r.name + "_" + b.name);
                        Node(root, "bo7mv:" + b.name, b.position, b.rotation, b.lossyScale, sub, mm);
                        Debug.Log("[BO7] " + model + ": parte movil " + b.name + " (" + sub.vertexCount + " vertices)");
                    }
                }
            }
            Object.DestroyImmediate(src);
            return root;
        }

        private static void Save(Mesh m, string model, string name)
        {
            string path = Dir + model + "/mesh_" + System.Text.RegularExpressions.Regex.Replace(name, "[^A-Za-z0-9_]", "_") + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
        }

        private static void Node(GameObject root, string name, Vector3 p, Quaternion q, Vector3 s, Mesh mesh, Material[] mats)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = p; go.transform.rotation = q; go.transform.localScale = s;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        }

        // los triangulos de 'mesh' cuyos vertices son del grupo g, con los vertices pasados por 'm'; 'used' = submallas originales que quedan
        private static Mesh SubMesh(Mesh mesh, int[] groupOf, int g, Matrix4x4 m, out List<int> used)
        {
            used = new List<int>();
            var vs = mesh.vertices; var ns = mesh.normals; var ts = mesh.tangents; var uv = mesh.uv;
            var map = new Dictionary<int, int>();
            var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nt = new List<Vector4>(); var nu = new List<Vector2>();
            var tris = new List<int[]>();
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var t = mesh.GetTriangles(s); var o = new List<int>();
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    if (groupOf[t[i]] != g) continue;
                    for (int k = 0; k < 3; k++)
                    {
                        int v = t[i + k], idx;
                        if (!map.TryGetValue(v, out idx))
                        {
                            map[v] = idx = nv.Count;
                            nv.Add(m.MultiplyPoint3x4(vs[v]));
                            if (ns.Length > 0) nn.Add(m.MultiplyVector(ns[v]).normalized);
                            if (ts.Length > 0) { var tv = m.MultiplyVector(ts[v]).normalized; nt.Add(new Vector4(tv.x, tv.y, tv.z, ts[v].w)); }
                            if (uv.Length > 0) nu.Add(uv[v]);
                        }
                        o.Add(idx);
                    }
                }
                if (o.Count > 0) { tris.Add(o.ToArray()); used.Add(s); }
            }
            if (nv.Count == 0) return null;
            var res = new Mesh { indexFormat = nv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            res.SetVertices(nv); if (nn.Count > 0) res.SetNormals(nn); if (nt.Count > 0) res.SetTangents(nt); if (nu.Count > 0) res.SetUVs(0, nu);
            res.subMeshCount = tris.Count;
            for (int i = 0; i < tris.Count; i++) res.SetTriangles(tris[i], i);
            res.RecalculateBounds();
            return res;
        }

        // nodos guia "bo7_<hueso>" bajo weapon y la lista de (nodo movil, guia, pose de reposo): como el cliente
        internal sealed class Follower { public Transform Node, Driver, Weapon; public Matrix4x4 Rest; }
        internal static List<Follower> Followers(Transform weapon)
        {
            var res = new List<Follower>();
            foreach (var t in weapon.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("bo7mv:")).ToList())
            {
                string dn = "bo7_" + t.name.Substring(6);
                var d = weapon.Find(dn);
                if (d == null) { d = new GameObject(dn).transform; d.SetParent(weapon, false); }
                res.Add(new Follower { Node = t, Driver = d, Weapon = weapon, Rest = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) });
            }
            return res;
        }

        internal static void Follow(List<Follower> fs)
        {
            foreach (var f in fs)
            {
                var c = f.Weapon.worldToLocalMatrix * f.Node.parent.localToWorldMatrix;
                var m = c.inverse * Matrix4x4.TRS(f.Driver.localPosition, f.Driver.localRotation, Vector3.one) * c * f.Rest;
                f.Node.localPosition = m.GetColumn(3);
                f.Node.localRotation = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
            }
        }
    }
}
