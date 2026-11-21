using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Cuerpo a cuerpo con animaciones de BO7, horneadas sobre el rig de cada arma cuerpo a cuerpo de EFT.
//   MELEE_W=fulcrum MELEE_CLIP=melee_miss_01 [MELEE_FRAMES=0-end/2]  -executeMethod TarkovaCola.Bo7Anim.MeleeRun   hoja de un clip de BO7 sobre el arma
//   [MELEE_W=fulcrum,kukri,...] [MELEE_SHEET=fire1,fire2]             -executeMethod TarkovaCola.Bo7Anim.MeleeBake  hornea me_<arma>_<clip de EFT> + bo7_melee.json
// Brazos: los dos siguen a los de BO7 respecto a la camara (palma por los nudillos, codo como polo, falanges como en las armas).
// Arma: el hueso "weapon" de EFT sigue al j_gun de BO7, con el agarre de EFT (en el reposo de BO7 el arma queda en la palma derecha
// igual que en el reposo de EFT). La camara se mueve como tag_camera de BO7.
// El cliente (Client/Bo7Melee.cs) pone los clips del arma en su controlador con bo7_melee.json (tpl -> prefijo).
namespace TarkovaCola
{
    public static partial class Bo7Anim
    {
        // grupos: clips de BO7 por accion de EFT (sufijo del clip de EFT -> clip de BO7)
        internal sealed class MeleeGroup
        {
            public string Cod;                                   // prefijo de los clips de BO7
            public Dictionary<string, string> Map;               // sufijo de EFT -> clip de BO7
        }
        private static Dictionary<string, string> MeleeMap(string fire1, string fire2, string quick) => new Dictionary<string, string>
        {
            ["idle"] = "idle", ["fire1"] = fire1, ["fire1_collidder"] = fire1, ["fire1_collider"] = fire1, ["fire2"] = fire2, ["fire2_collidder"] = fire2, ["fire2_collider"] = fire2,
            ["fire_quick"] = quick, ["fire_quick_collider"] = quick, ["fire_quick_colidder"] = quick, ["look"] = "inspect",
            ["idle_armed"] = "idle", ["fire_0"] = fire1, ["fire_0_collider"] = fire1, ["fire_0_collidder"] = fire1, ["fire_1"] = fire2, ["fire_1_collider"] = fire2, ["fire_1_collidder"] = fire2,
            ["out_to_idle"] = "raise", ["out_to_idle_alt"] = "raise_first", ["idle_to_out"] = "drop",
        };
        // mazo, guadana y Labris: golpes con direccion (Attack_*) y reposo/sacar/guardar con nombres propios
        private static Dictionary<string, string> Heavy(Dictionary<string, string> m)
        {
            m["Attack_Left"] = "swing_miss_01"; m["Attack_Right"] = "swing_miss_02"; m["Attack_Down"] = "swing_heavy_01";
            m["Attack_Left_to_Idle"] = "idle"; m["Attack_Right_to_Idle"] = "idle"; m["Attack_Down_to_Idle"] = "idle";
            m["Idle_Simple"] = "idle"; m["Equip"] = "raise"; m["Unequip"] = "drop";
            return m;
        }
        internal static readonly Dictionary<string, MeleeGroup> MeleeGroups = new Dictionary<string, MeleeGroup>
        {
            ["knife"] = new MeleeGroup { Cod = "sat_vm_me_knife_", Map = MeleeMap("melee_miss_01", "swipe_heavy_01", "melee_hit_02") },
            ["cleaver"] = new MeleeGroup { Cod = "vm_p30_me_cleaver_", Map = MeleeMap("melee_miss_01", "heavy_melee_01", "melee_hit_02") },
            ["hatchet"] = new MeleeGroup { Cod = "vm_p48_me_pickaxe_", Map = MeleeMap("melee_miss_01", "melee_miss_03", "melee_hit_02") },
            ["bat"] = new MeleeGroup { Cod = "vm_p16_me_bat_", Map = MeleeMap("swing_miss_01", "swing_heavy_01", "swing_hit_02") },
            ["pickaxe"] = new MeleeGroup { Cod = "vm_p51_me_pickaxe_", Map = Heavy(MeleeMap("swing_miss_01", "swing_heavy_01", "swing_hit_02")) },
        };

        // armas cuerpo a cuerpo de EFT: clave (prefijo), tpl, bundle, grupo
        internal static readonly (string key, string tpl, string bundle, string group)[] MeleeWeapons =
        {
            ("fulcrum", "54491bb74bdc2d09088b4567", "fulcrum/weapon_extrema_ratio_fulcrum_container", "knife"),
            ("damask", "57e26ea924597715ca604a09", "a-2607/weapon_bars_a-2607_damask_container", "knife"),
            ("a2607", "57e26fc7245977162a14b800", "a-2607/weapon_bars_a-2607_95x18_container", "knife"),
            ("6x5", "5bffdc370db834001d23eca8", "6x5/weapon_izhmash_6x5_container", "knife"),
            ("ritual", "5fc64ea372b0dd78d51159dc", "knife_ritual/weapon_knife_ritual_container", "knife"),
            ("dagger", "674d90b55704568fe60bc8f5", "dagger/weapon_semin_push_dagger_container", "knife"),
            ("kukri", "65ca457b4aafb5d7fc0dcb5d", "m48_kukri/weapon_united_cutlery_m48_kukri_container", "cleaver"),
            ("sp8", "5c010e350db83400232feec7", "sp-8/weapon_ontario_sp-8_container", "cleaver"),
            ("taiga", "601948682627df266209af05", "taiga/weapon_usvr_taiga_container", "cleaver"),
            ("gladius", "664a5428d5e33a713b622379", "gladius/weapon_apoc_wasteland_gladius_container", "cleaver"),
            ("miller", "5bffdd7e0db834001b734a1a", "miller/weapon_miller_bros_m-2_container", "cleaver"),
            ("tomahawk", "57cd379a24597778e7682ecf", "kiba_axe/weapon_kiba_arms_axe_container", "hatchet"),
            ("wycc", "5bc9c1e2d4351e00367fbcf0", "wycc/weapon_valuable_wyccaxe_container", "hatchet"),
            ("crashaxe", "5bffe7930db834001b734a39", "strider/weapon_strider_crash_axe_container", "hatchet"),
            ("rebel", "5c0126f40db834002a125382", "rebel/weapon_dmm_rebel_adze_axe_container", "hatchet"),
            ("utilaxe", "5c012ffc0db834001d23f03f", "utility_axe/weapon_utility_axe_container", "hatchet"),
            ("voodoo", "63920105a83e15700a00f168", "voodoo/weapon_sog_voodoo_hawk_container", "hatchet"),
            ("crowbar", "5c07df7f0db834001b73588a", "crowbar/weapon_gf_crowbar_container", "bat"),
            ("mpl40", "5bead2e00db834001c062938", "mpl40/weapon_voentorg_mpl-40_container", "bat"),
            ("taran", "6540d2162ae6d96b540afcaf", "taran/weapon_pr_taran_container", "bat"),
            ("sledge", "6087e570b998180e9f76dc24", "hultafors/weapon_hultafors_db5000_container", "pickaxe"),
            ("scythe", "63495c500c297e20065a08b1", "scythe/weapon_scythe_container", "pickaxe"),
            ("labris", "679ba90d269ddfea47012159", "labris/weapon_labris_axe_container", "pickaxe"),
        };

        internal sealed class MeleeSolver
        {
            public EftLab.Rig Rig; public Skel Skel; public MeleeGroup Group;
            public Transform Weapon;
            public Dictionary<Transform, (Vector3 p, Quaternion r)> Base = new Dictionary<Transform, (Vector3, Quaternion)>();
            public Dictionary<Transform, Matrix4x4> MarkerOff = new Dictionary<Transform, Matrix4x4>();
            public Dictionary<string, Matrix4x4> KnuckleInv = new Dictionary<string, Matrix4x4>();
            public Dictionary<string, List<(string cod, Transform eft)>> Fingers = new Dictionary<string, List<(string, Transform)>>();
            public Matrix4x4 CamW, S, FingerConv, GunToWeapon;
            public Quaternion Cam0, CodCam0Inv;
            public Vector3 Push;

            public void Reset() { foreach (var kv in Base) { kv.Key.localPosition = kv.Value.p; kv.Key.localRotation = kv.Value.r; } }
            public Matrix4x4 W(Fk fk, string b) => CamW * Matrix4x4.Translate(Push) * Basis(S, fk.Rel("tag_view", b));

            public void Arms(Fk fk)
            {
                foreach (var side in new[] { "le", "ri" })
                {
                    string sd = side == "le" ? "L" : "R";
                    System.Func<string, Vector3> cp = b => Pos(W(fk, b));
                    var anim = HandFrameAt(cp("j_wrist_" + side), cp("j_index_" + side + "_1"), cp("j_pinky_" + side + "_1"), 1f) * KnuckleInv[side];
                    var palm = Rig.Bones["Base Human" + sd + "Palm"];
                    TwoBone(Rig.Bones["Base Human" + sd + "Upperarm"], Rig.Bones["Base Human" + sd + "Forearm1"], palm, Pos(anim), cp("j_elbow_" + side));
                    palm.rotation = Rot(anim);
                    foreach (var (cod, eft) in Fingers[side]) eft.rotation = Rot(W(fk, cod) * FingerConv) * FingerC;
                }
            }

            public void Pose(CodClip clip, float f)
            {
                Reset();
                var fk = new Fk(Skel, clip, f);
                Arms(fk);
                var w = W(fk, "j_gun") * GunToWeapon;
                if (Env("MELEE_DBG", "0") == "1") Debug.Log("[MELEE] f" + f + " muneca der " + ((Vector3)(CamW.inverse * W(fk, "j_wrist_ri")).GetColumn(3)).ToString("F2") + " j_gun " + ((Vector3)(CamW.inverse * W(fk, "j_gun")).GetColumn(3)).ToString("F2") + " camara " + (CodCam0Inv * Rot(Basis(CamBasis(), fk.Rel("tag_view", "tag_camera")))).eulerAngles.ToString("F0"));
                Weapon.SetPositionAndRotation(Pos(w), Rot(w));
                if (Env("MELEE_CAM", "1") == "1") Rig.CamAnim.rotation = Cam0 * CodCam0Inv * Rot(Basis(CamBasis(), fk.Rel("tag_view", "tag_camera"))) * Quaternion.Inverse(Quaternion.Euler(90f, 0f, 0f));
                foreach (var kv in MarkerOff) { var bone = Rig.Bones[Markers[kv.Key.name]]; var m = World(bone) * kv.Value; kv.Key.SetPositionAndRotation(Pos(m), Rot(m)); }
            }
        }

        // clips del arma (nombre base, sin _simple.generated): solo los suyos, no los gestos ni la brujula que trae todo controlador de manos
        private static readonly string[] NotMelee = { "hand_", "compass_", "generic_", "gestures_", "fshield_", "rot_", "weapon_root" };
        internal static List<(string name, AnimationClip clip)> EftMeleeClips(EftLab.Rig rig)
        {
            var res = new Dictionary<string, AnimationClip>();
            foreach (var c in rig.Clips)
            {
                if (c.name.Contains("_spirit") || c.name.Contains("_3rd")) continue;
                string n = c.name.Replace("_simple.generated", "");
                if (NotMelee.Any(x => n.StartsWith(x))) continue;
                if (!res.ContainsKey(n) || c.name == n) res[n] = c;
            }
            return res.Select(kv => (kv.Key, kv.Value)).ToList();
        }

        internal static MeleeSolver BuildMelee(string key)
        {
            var wd = MeleeWeapons.First(x => x.key == key);
            var rig = EftLab.LoadRig("assets/content/weapons/" + wd.bundle + ".bundle");
            var g = MeleeGroups[wd.group];
            var s = new MeleeSolver { Rig = rig, Group = g, Skel = LoadSkel("../UnityBundle/Assets/Anim/viewhands_skel.json"), Weapon = rig.Bones["weapon"] };
            // algunos bundles no traen el controlador en su familia: se toma el del Animator del prefab
            foreach (var an in rig.Root.GetComponentsInChildren<Animator>(true))
            {
                var rc = an.runtimeAnimatorController; if (rc == null) continue;
                foreach (var c in rc.animationClips) if (c != null && !rig.Clips.Contains(c)) rig.Clips.Add(c);
                var ov = rc as AnimatorOverrideController;
                if (ov != null) { var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(); ov.GetOverrides(pairs); foreach (var kv in pairs) if (kv.Key != null && !rig.Clips.Contains(kv.Key)) rig.Clips.Add(kv.Key); }
            }
            if (EftMeleeClips(rig).Count == 0)
            {
                // sin controlador propio en el editor (el prefab lo referencia desde un script de EFT): se usan los clips de un arma del
                // mismo tipo; el cliente solo cambia los clips cuyo nombre exista en el controlador real
                string donor = wd.group == "knife" ? "fulcrum" : wd.group == "cleaver" ? "kukri" : wd.group == "hatchet" ? "tomahawk" : wd.group == "bat" ? "crowbar" : "sledge";
                var family = EftBundles.Family("assets/content/weapons/" + MeleeWeapons.First(x => x.key == donor).bundle + ".bundle");
                foreach (var ov in family.SelectMany(b => b.LoadAllAssets<AnimatorOverrideController>()))
                { var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(); ov.GetOverrides(pairs); foreach (var kv in pairs) if (kv.Value != null && !rig.Clips.Contains(kv.Value)) rig.Clips.Add(kv.Value); }
                foreach (var ctrl in family.SelectMany(b => b.LoadAllAssets<RuntimeAnimatorController>())) foreach (var c in ctrl.animationClips) if (c != null && !rig.Clips.Contains(c)) rig.Clips.Add(c);
                Debug.Log("[MELEE] " + key + ": sin controlador propio, clips de " + donor);
            }
            var clips = EftMeleeClips(rig);
            var idleEft = clips.Where(c => c.name == "Idle_Simple").Select(c => c.clip).FirstOrDefault() ?? clips.Where(c => (c.name.EndsWith("_idle") || c.name.EndsWith("_idle_armed")) && !c.name.Contains("patrol") && !c.name.Contains("out_to")).Select(c => c.clip).FirstOrDefault();
            if (idleEft == null) throw new System.Exception("sin reposo; clips: " + string.Join(",", rig.Clips.Select(c => c.name)) + " | prefab " + rig.Root.name + ", animadores " + string.Join(",", rig.Root.GetComponentsInChildren<Animator>(true).Select(a => a.name + "=" + (a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "null"))) + " | dependencias " + string.Join(",", EftBundles.Family("assets/content/weapons/" + wd.bundle + ".bundle").Select(b => b.name)));
            idleEft.SampleAnimation(rig.Root, 0f);
            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) s.Base[t] = (t.localPosition, t.localRotation);
            foreach (var kv in Markers) { Transform m, bn; if (rig.Bones.TryGetValue(kv.Key, out m) && rig.Bones.TryGetValue(kv.Value, out bn)) s.MarkerOff[m] = World(bn).inverse * World(m); }
            var rPalm = rig.Bones["Base HumanRPalm"];
            var weaponInPalm = World(rPalm).inverse * World(s.Weapon);
            foreach (var side in new[] { "le", "ri" })
            {
                string sd = side == "le" ? "L" : "R";
                var palm = rig.Bones["Base Human" + sd + "Palm"];
                s.KnuckleInv[side] = (World(palm).inverse * HandFrameAt(palm.position, rig.Bones["Base Human" + sd + "Digit21"].position, rig.Bones["Base Human" + sd + "Digit51"].position, 1f)).inverse;
                s.Fingers[side] = HandMap(side).Skip(1).Where(p => rig.Bones.ContainsKey(p.eft) && s.Skel.Parent.ContainsKey(p.cod)).Select(p => (p.cod, rig.Bones[p.eft])).ToList();
            }
            s.CamW = Matrix4x4.TRS(rig.CamAnim.position, rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            s.S = Matrix4x4.Scale(Vector3.one * 0.01f) * CamBasis();
            s.FingerConv = CamBasis() * FbxBasis(1f).inverse;
            var pv = Env("MELEE_PUSH", "0,-0.03,0.08").Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            s.Push = s.CamW.MultiplyVector(new Vector3(pv[0], pv[1], pv[2]));
            s.Cam0 = rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f);
            // agarre: en el reposo de BO7 el arma de EFT queda en la palma derecha como en el reposo de EFT
            var idle = LoadClip(Dir + g.Cod + "idle.json");
            var fk0 = new Fk(s.Skel, idle, 0f);
            s.CodCam0Inv = Quaternion.Inverse(Rot(Basis(CamBasis(), fk0.Rel("tag_view", "tag_camera"))));
            s.Reset(); s.Arms(fk0);
            s.GunToWeapon = s.W(fk0, "j_gun").inverse * World(rPalm) * weaponInPalm;
            Debug.Log("[MELEE] " + key + " (" + wd.group + "): reposo de EFT " + idleEft.name + ", clips " + string.Join(",", clips.Select(c => c.name)));
            return s;
        }

        public static void MeleeRun()
        {
            try
            {
                var s = BuildMelee(Env("MELEE_W", "fulcrum"));
                foreach (var cn in Env("MELEE_CLIP", "melee_miss_01").Split(','))
                {
                    var clip = LoadClip(Dir + s.Group.Cod + cn + ".json");
                    var frames = EftLab.ParseFrames(Env("MELEE_FRAMES", "0-end/2"), clip.Frames - 1);
                    EftLab.RenderSheet(s.Rig, f => s.Pose(clip, f), frames, Env("MELEE_OUT", "Preview/melee_" + Env("MELEE_W", "fulcrum") + "_" + cn + ".png"));
                }
            }
            catch (System.Exception e) { Debug.LogError("[MELEE] " + e); }
            finally { EftBundles.UnloadAll(); }
        }

        public static void MeleeBake()
        {
            try
            {
                Directory.CreateDirectory(Dir + "Baked");
                string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
                var keys = Env("MELEE_W", string.Join(",", MeleeWeapons.Select(x => x.key))).Split(',');
                foreach (var key in keys)
                {
                    MeleeSolver s;
                    try { s = BuildMelee(key); } catch (System.Exception e) { Debug.LogError("[MELEE] " + key + ": " + e.Message); EftBundles.UnloadAll(); continue; }
                    try
                    {
                        foreach (var old in Directory.GetFiles(Dir + "Baked", "me_" + key + "_*.anim")) AssetDatabase.DeleteAsset(old.Replace('\\', '/'));
                        var rec = s.Rig.Root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Base Human") || t.name == "Camera_animated" || t.name == "weapon" || Markers.ContainsKey(t.name)).ToList();
                        var codClips = new Dictionary<string, CodClip>();
                        var baked = new List<AnimationClip>();
                        foreach (var (eftName, eftClip) in EftMeleeClips(s.Rig))
                        {
                            var suffix = s.Group.Map.Keys.OrderByDescending(k => k.Length).FirstOrDefault(k => eftName == k || eftName.EndsWith("_" + k));
                            if (suffix == null) continue;
                            string cn = s.Group.Map[suffix];
                            if (!codClips.ContainsKey(cn)) { var path = Dir + s.Group.Cod + cn + ".json"; if (!File.Exists(path)) { Debug.LogWarning("[MELEE] falta " + path); continue; } codClips[cn] = LoadClip(path); }
                            var cc = codClips[cn];
                            float secs = Mathf.Max(cc.Frames - 1, 10) / 30f;    // el reposo de BO7 es un solo fotograma: 1/3 s quieto
                            int n = Mathf.Max(2, Mathf.RoundToInt(secs * 30f) + 1);
                            var curves = rec.ToDictionary(t => t, t => new AnimationCurve[7].Select(_ => new AnimationCurve()).ToArray());
                            for (int i = 0; i < n; i++)
                            {
                                float u = i / (float)(n - 1);
                                s.Pose(cc, u * (cc.Frames - 1));
                                foreach (var kv in curves)
                                {
                                    var p = kv.Key.localPosition; var q = kv.Key.localRotation; var v = new[] { p.x, p.y, p.z, q.x, q.y, q.z, q.w };
                                    for (int c = 0; c < 7; c++) kv.Value[c].AddKey(new Keyframe(u * secs, v[c]));
                                }
                            }
                            var clip = new AnimationClip { name = "me_" + key + "_" + eftName, frameRate = 30f };
                            foreach (var kv in curves) { string path = AnimationUtility.CalculateTransformPath(kv.Key, s.Rig.Root.transform); for (int c = 0; c < 7; c++) clip.SetCurve(path, typeof(Transform), props[c], kv.Value[c]); }
                            clip.EnsureQuaternionContinuity();
                            if (suffix == "idle") { var st = AnimationUtility.GetAnimationClipSettings(clip); st.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, st); }
                            AssetDatabase.CreateAsset(clip, Dir + "Baked/" + clip.name + ".anim");
                            baked.Add(clip);
                            Debug.Log("[MELEE] horneado " + clip.name + " <- " + s.Group.Cod + cn + " = " + clip.length.ToString("F2") + " s (EFT " + eftClip.length.ToString("F2") + ")");
                        }
                        var sheet = Env("MELEE_SHEET", "").Split(',').Where(x => x.Length > 0).ToList();
                        if (sheet.Count > 0)
                        {
                            var seq = new List<(AnimationClip c, float t)>();
                            foreach (var c in baked.Where(c => sheet.Any(x => c.name.EndsWith("_" + x)))) for (float t = 0f; t <= c.length + 1e-4f; t += 3f / 30f) seq.Add((c, t));
                            EftLab.RenderSheet(s.Rig, i => { s.Reset(); seq[i].c.SampleAnimation(s.Rig.Root, seq[i].t); }, Enumerable.Range(0, seq.Count).ToList(), "Preview/meleeb_" + key + ".png");
                        }
                    }
                    finally { Object.DestroyImmediate(s.Rig.Root); EftBundles.UnloadAll(); }
                }
                File.WriteAllText(Dir + "Baked/bo7_melee.json", "{" + string.Join(",", MeleeWeapons.Select(x => "\"" + x.tpl + "\":\"me_" + x.key + "_\"")) + "}");
                AssetDatabase.ImportAsset(Dir + "Baked/bo7_melee.json");
                AssetDatabase.SaveAssets();
                BuildAnimBundles();
            }
            catch (System.Exception e) { Debug.LogError("[MELEE] " + e); }
            finally { EftBundles.UnloadAll(); }
        }
    }
}
