using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Horneador de animaciones de BO7, segunda generacion (docs/Plan_armas_BO7.md).
//   BO7_GUN=pk BO7_CLIP=reload BO7_FRAMES=0-end/4 [BO7_PINS=0]   -executeMethod TarkovaCola.Bo7Anim.Run2      hoja de un clip de BO7 sobre el rig
//   BO7_GUN=pk [BO7_SHEET=clip1,clip2 BO7_STEP=4]                -executeMethod TarkovaCola.Bo7Anim.BakeRun2  hornea Jobs2[arma] y construye los bundles
// Diferencias con la primera:
//  - Manos: la palma de EFT va donde la muneca de BO7 con un marco anatomico (muneca, nudillos del indice y del menique) igual para todas
//    las armas; el arma se ancla para que la mano derecha de BO7 en reposo caiga donde EFT tiene la suya (BuildBo7Guns.AnchorFromRightHand).
//  - Fijacion: BO7 pega las manos al arma (tag_ik_loc_*) salvo entre las notas ik_out/ik_in (y fingers_out/in para los dedos); aqui igual:
//    fijada = pose de reposo respecto al arma.
//  - Marcadores de EFT: weapon_*_IK_marker sigue a la palma; weapon_*_hand_marker queda en el reposo (referencia del agarre: Player.IkProcess).
//  - Partes moviles: nodos guia "bo7_<hueso>" bajo weapon con el movimiento del hueso respecto a su reposo.
//  - Tiempo natural: cada tramo dura lo que en BO7 (por la velocidad del estado); los eventos de EFT se recolocan en el cliente (bo7_events.json).
namespace TarkovaCola
{
    public static partial class Bo7Anim
    {
        internal static float FbxScale(BuildBo7Guns.BoPart rec, Skel skel)
        {
            var rest = new Fk(skel, new CodClip(), 0f);
            System.Func<string, Vector3> t = n => rest.Rel("j_gun", n).GetColumn(3);
            return (rec.Tag("tag_mag_attach") - rec.Tag("tag_stock_attach")).magnitude / (t("tag_mag_attach") - t("tag_stock_attach")).magnitude;
        }

        // marco anatomico de una mano: origen en la muneca, x hacia el centro de los nudillos, z normal a la palma
        internal static Matrix4x4 HandFrame(Vector3 o, Vector3 index, Vector3 pinky)
        {
            var x = ((index + pinky) * 0.5f - o).normalized;
            var z = Vector3.Cross(x, index - pinky).normalized;
            var y = Vector3.Cross(z, x);
            var m = Matrix4x4.identity;
            m.SetColumn(0, x); m.SetColumn(1, y); m.SetColumn(2, z); m.SetColumn(3, new Vector4(o.x, o.y, o.z, 1f));
            return m;
        }

        internal static Vector3 PalmShift = Vector3.zero;   // (-0.058, 0.0075, -0.019) medido en la MK35 NO vale: la mano quedaba 6 cm corta y cogia el cargador con la punta de los dedos

        internal sealed class Pins
        {
            public float[] IkL, IkR, FgL, FgR;
            public float At(float[] a, float f) { int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, a.Length - 1), j = Mathf.Min(i + 1, a.Length - 1); return Mathf.Lerp(a[i], a[j], f - i); }
            public static Pins All(int n) { var one = Enumerable.Repeat(1f, Mathf.Max(n, 1)).ToArray(); return new Pins { IkL = one, IkR = one, FgL = one, FgR = one }; }
        }

        // peso de fijacion por fotograma (1 = pegada al arma) a partir de las notas <kind>_out_start/end_<mano>_hand y <kind>_in_start/end
        private static float[] Track(CodClip c, string kind, string hand)
        {
            System.Func<string, int[]> n = k => { int[] v; return c.Notes.TryGetValue(kind + "_" + k + "_" + hand + "_hand", out v) ? v : new int[0]; };
            var ramps = new List<(float a, float b, float v0, float v1)>();
            foreach (int a in n("out_start")) { var e = n("out_end").Where(x => x >= a).DefaultIfEmpty(a + 3).Min(); ramps.Add((a, Mathf.Max(e, a + 1), 1f, 0f)); }
            foreach (int a in n("in_start")) { var e = n("in_end").Where(x => x >= a).DefaultIfEmpty(a + 3).Min(); ramps.Add((a, Mathf.Max(e, a + 1), 0f, 1f)); }
            ramps = ramps.OrderBy(r => r.a).ToList();
            float init = ramps.Count > 0 && ramps[0].v0 < 0.5f ? 0f : 1f;
            var res = new float[Mathf.Max(c.Frames, 1)];
            for (int f = 0; f < res.Length; f++)
            {
                float v = init;
                foreach (var r in ramps) { if (f >= r.b) v = r.v1; else if (f > r.a) v = Mathf.Lerp(r.v0, r.v1, Mathf.SmoothStep(0f, 1f, (f - r.a) / (r.b - r.a))); }
                res[f] = v;
            }
            return res;
        }
        internal static Pins PinsOf(CodClip c) => new Pins { IkL = Track(c, "ik", "left"), IkR = Track(c, "ik", "right"), FgL = Track(c, "fingers", "left"), FgR = Track(c, "fingers", "right") };

        internal sealed class Solver2
        {
            public BuildBo7Guns.GunDef Def;
            public EftLab.Rig Rig; public Skel Skel; public CodClip Idle;
            public Transform Rec, Weapon, Mag;
            public Matrix4x4 P, PC;
            public Dictionary<Transform, (Vector3 p, Quaternion r)> Base = new Dictionary<Transform, (Vector3, Quaternion)>();
            public Matrix4x4 Rec0, WeaponInRec, MagSlot0, G0inv;
            public Dictionary<string, Matrix4x4> MagRest = new Dictionary<string, Matrix4x4>();
            public Quaternion Cam0, CodCam0Inv;
            public Dictionary<string, Matrix4x4> PalmOff = new Dictionary<string, Matrix4x4>(), RestPalm = new Dictionary<string, Matrix4x4>();
            // marco anatomico de la palma de EFT con origen en el centro de los nudillos, inverso (en el espacio local de la palma)
            public Dictionary<string, Matrix4x4> KnuckleInv = new Dictionary<string, Matrix4x4>();
            public Dictionary<Transform, Quaternion> FingerOff = new Dictionary<Transform, Quaternion>(), RestFinger = new Dictionary<Transform, Quaternion>();
            public Dictionary<string, List<(string cod, Transform eft)>> Fingers = new Dictionary<string, List<(string, Transform)>>();
            public Dictionary<Transform, Vector3> ElbowOff = new Dictionary<Transform, Vector3>();
            public Dictionary<Transform, Matrix4x4> MarkerOff = new Dictionary<Transform, Matrix4x4>();
            public Dictionary<string, Transform> IkMarker = new Dictionary<string, Transform>(), HandMarker = new Dictionary<string, Transform>();
            public List<BuildBo7Guns.Follower> Followers;
            public Dictionary<string, Matrix4x4> MoverRestInv = new Dictionary<string, Matrix4x4>();
            public Vector3 Push;                                                     // GunDef.ViewPush, en el mundo (hacia delante del arma)
            public float ExtraPush;                                                  // fraccion de Push que se suma (inspeccion: GunDef.LookPush)
            public Dictionary<string, float> ReachErr = new Dictionary<string, float> { ["le"] = 0f, ["ri"] = 0f };   // mano que no llega (m)

            public void Reset() { foreach (var kv in Base) { kv.Key.localPosition = kv.Value.p; kv.Key.localRotation = kv.Value.r; } }
            private static string S(string side) => side == "le" ? "L" : "R";

            public Matrix4x4 PalmTarget(Matrix4x4 rec, Fk fk, string side) => rec * Basis(P, fk.Rel("j_gun", "j_wrist_" + side)) * PalmOff[side];

            // fotograma f de 'clip' en el rig; pins: cuanto queda cada mano pegada a su reposo en el arma
            public void Pose(CodClip clip, float f, Pins pins, string magBone = "j_mag1", CodClip parts = null, float partsF = 0f)
            {
                Reset();
                var fk = new Fk(Skel, clip, f);
                var rec = Matrix4x4.Translate(Push * (1f + ExtraPush)) * Rec0 * Basis(P, G0inv * fk.Rel("tag_view", "j_gun"));
                var w = rec * WeaponInRec;
                Weapon.SetPositionAndRotation(Pos(w), Rot(w));
                if (Mag != null && magBone != null && MagRest.ContainsKey(magBone))
                {
                    var m = rec * Basis(P, fk.Rel("j_gun", magBone)) * Basis(P, MagRest[magBone]).inverse * Rec0.inverse * MagSlot0;
                    Mag.SetPositionAndRotation(Pos(m), Rot(m));
                }
                var cam = CodCam0Inv * Rot(Basis(PC, fk.Rel("tag_view", "tag_camera")));
                Rig.CamAnim.rotation = Cam0 * cam * Quaternion.Inverse(Quaternion.Euler(90f, 0f, 0f));
                foreach (var side in new[] { "le", "ri" })
                {
                    string s = S(side);
                    float ik = pins.At(side == "le" ? pins.IkL : pins.IkR, f), fg = pins.At(side == "le" ? pins.FgL : pins.FgR, f);
                    var upper = Rig.Bones["Base Human" + s + "Upperarm"]; var fore = Rig.Bones["Base Human" + s + "Forearm1"]; var palm = Rig.Bones["Base Human" + s + "Palm"];
                    // palma animada: se hacen coincidir los nudillos (no la muneca): la mano de EFT es ~1,4 veces la de BO7 respecto al arma
                    System.Func<string, Vector3> cp = b => Pos(rec * Basis(P, fk.Rel("j_gun", b)));
                    var anim = HandFrameAt(cp("j_wrist_" + side), cp("j_index_" + side + "_1"), cp("j_pinky_" + side + "_1"), 1f) * KnuckleInv[side];
                    var pin = World(Weapon) * RestPalm[side];
                    var pos = Vector3.Lerp(Pos(anim), Pos(pin), ik); var rot = Quaternion.Slerp(Rot(anim), Rot(pin), ik);
                    var elbow = Pos(rec * Basis(P, fk.Rel("j_gun", "j_elbow_" + side))) + ElbowOff[fore];
                    TwoBone(upper, fore, palm, pos, elbow);
                    palm.rotation = rot;
                    ReachErr[side] = (palm.position - pos).magnitude;
                    foreach (var (cod, eft) in Fingers[side])
                    {
                        // falange de EFT = la de BO7 por una constante (ejes: EFT hueso -x y bisagra +z; BO7 hueso -x y bisagra -y); medido en HandProbe
                        eft.rotation = Rot(rec * Basis(P, fk.Rel("j_gun", cod))) * FingerC;
                        if (fg > 0f) eft.localRotation = Quaternion.Slerp(eft.localRotation, RestFinger[eft], fg);
                    }
                    var ikm = IkMarker[side]; var mk = World(palm) * MarkerOff[ikm];
                    ikm.SetPositionAndRotation(Pos(mk), Rot(mk));
                    Transform hm;
                    if (HandMarker.TryGetValue(side, out hm) && hm != null) { var r = pin * MarkerOff[ikm]; hm.SetPositionAndRotation(Pos(r), Rot(r)); }
                }
                foreach (var kv in MarkerOff)
                {
                    if (IkMarker.ContainsValue(kv.Key)) continue;
                    var m = World(Rig.Bones[Markers[kv.Key.name]]) * kv.Value; kv.Key.SetPositionAndRotation(Pos(m), Rot(m));
                }
                // partes moviles: la guia lleva el movimiento del hueso respecto a su reposo, en el espacio del arma
                var t = WeaponInRec.inverse;
                if (parts != null) fk = new Fk(Skel, parts, partsF);
                foreach (var fo in Followers)
                {
                    string bone = fo.Driver.name.Substring(4);
                    Matrix4x4 restInv;
                    if (!MoverRestInv.TryGetValue(bone, out restInv)) continue;
                    var d = t * Basis(P, fk.Rel("j_gun", bone) * restInv) * t.inverse;
                    fo.Driver.localPosition = Pos(d); fo.Driver.localRotation = Rot(d);
                }
                BuildBo7Guns.Follow(Followers);
            }
        }

        internal static Solver2 Build2(string gun)
        {
            var def = BuildBo7Guns.Gun(gun);
            var rig = EftLab.LoadRig(def.Bundle);
            var weapon = rig.Bones["weapon"];
            BuildBo7Guns.HideBase(weapon);
            BuildBo7Guns.Assemble(gun, weapon, false);
            foreach (var t in weapon.GetComponentsInChildren<Transform>(true)) if (BuildBo7Guns.Guns.Keys.Any(k => t.name.StartsWith(k + "_")) && t.parent != null && t.parent.name.StartsWith("mod_")) EftLab.Simplify(t.gameObject);
            var idleClip = BuildBo7Guns.Idle(rig);
            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) if (!rig.Bones.ContainsKey(t.name)) rig.Bones[t.name] = t;
            var s = new Solver2 { Def = def, Rig = rig, Skel = LoadSkel(Dir + "bo7_" + def.Cod + "_skel.json"), Idle = LoadClip(Dir + "sat_vm_" + def.Cod + "_idle.json"), Weapon = weapon, PC = CamBasis() };
            s.Rec = weapon.GetComponentsInChildren<Transform>(true).First(t => t.name == "sat_vm_" + def.Cod + "_rec");
            s.Mag = weapon.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "mod_magazine");
            s.Followers = BuildBo7Guns.Followers(weapon);
            s.P = FbxBasis(FbxScale(BuildBo7Guns.Part("sat_vm_" + def.Cod + "_rec"), s.Skel));

            var fk0 = new Fk(s.Skel, s.Idle, 0f); var rest = new Fk(s.Skel, new CodClip(), 0f);
            s.Rec0 = World(s.Rec); s.WeaponInRec = s.Rec0.inverse * World(weapon);
            s.G0inv = fk0.Rel("tag_view", "j_gun").inverse;
            if (s.Mag != null) { s.MagSlot0 = World(s.Mag); foreach (var b in new[] { "j_mag1", "j_mag2" }) if (s.Skel.Parent.ContainsKey(b)) s.MagRest[b] = fk0.Rel("j_gun", "j_mag1"); }
            foreach (var fo in s.Followers) { string b = fo.Driver.name.Substring(4); if (s.Skel.Parent.ContainsKey(b)) s.MoverRestInv[b] = rest.Rel("j_gun", b).inverse; else Debug.LogWarning("[BO7A] parte movil sin hueso: " + b); }
            s.Cam0 = rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f);
            s.CodCam0Inv = Quaternion.Inverse(Rot(Basis(s.PC, fk0.Rel("tag_view", "tag_camera"))));

            // marcadores (respecto a su hueso, en el reposo de EFT) y codos
            foreach (var kv in Markers)
            {
                Transform m, b;
                if (rig.Bones.TryGetValue(kv.Key, out m) && rig.Bones.TryGetValue(kv.Value, out b)) s.MarkerOff[m] = World(b).inverse * World(m);
            }
            System.Func<string, Vector3> cod = b => Pos(s.Rec0 * Basis(s.P, fk0.Rel("j_gun", b)));
            var eftPalm0 = new Dictionary<string, Matrix4x4>();
            foreach (var side in new[] { "le", "ri" })
            {
                string sd = side == "le" ? "L" : "R";
                var palm = rig.Bones["Base Human" + sd + "Palm"]; var fore = rig.Bones["Base Human" + sd + "Forearm1"];
                s.IkMarker[side] = rig.Bones["weapon_" + sd + "_IK_marker"];
                Transform hm; if (rig.Bones.TryGetValue("weapon_" + sd + "_hand_marker", out hm)) s.HandMarker[side] = hm;
                s.ElbowOff[fore] = fore.position - cod("j_elbow_" + side);
                // marcos anatomicos
                var wc = s.Rec0 * Basis(s.P, fk0.Rel("j_gun", "j_wrist_" + side));
                var ac = wc.inverse * HandFrame(cod("j_wrist_" + side), cod("j_index_" + side + "_1"), cod("j_pinky_" + side + "_1"));
                var ae = World(palm).inverse * HandFrame(palm.position, rig.Bones["Base Human" + sd + "Digit21"].position, rig.Bones["Base Human" + sd + "Digit51"].position);
                // el origen de la palma de EFT no cae en la muneca de BO7: desplazamiento medido en la MK35 (encajada pieza a pieza sobre la HK416),
                // en el marco anatomico de la mano derecha; en la izquierda (marco espejado) cambia el signo de z
                var shift = new Vector3(PalmShift.x, PalmShift.y, side == "le" ? -PalmShift.z : PalmShift.z);
                Debug.Log("[BO7A] " + gun + " mano " + sd + ": palma de EFT en el marco de BO7 " + ((Vector3)(wc * ac).inverse.MultiplyPoint3x4(palm.position)).ToString("F4"));
                s.PalmOff[side] = ac * Matrix4x4.Translate(shift) * ae.inverse;
                eftPalm0[side] = World(palm);
                s.KnuckleInv[side] = (World(palm).inverse * HandFrameAt(palm.position, rig.Bones["Base Human" + sd + "Digit21"].position, rig.Bones["Base Human" + sd + "Digit51"].position, 1f)).inverse;
                s.Fingers[side] = HandMap(side).Skip(1).Where(p => rig.Bones.ContainsKey(p.eft) && s.Skel.Parent.ContainsKey(p.cod)).Select(p => (p.cod, rig.Bones[p.eft])).ToList();
            }
            // reposo: las palmas de EFT en la pose de reposo de BO7 (los dedos conservan el agarre del reposo de EFT)
            foreach (var side in new[] { "le", "ri" })
            {
                string sd = side == "le" ? "L" : "R";
                var upper = rig.Bones["Base Human" + sd + "Upperarm"]; var fore = rig.Bones["Base Human" + sd + "Forearm1"]; var palm = rig.Bones["Base Human" + sd + "Palm"];
                // BO7 pega cada mano a su tag_ik_loc_* (del canon la izquierda, del receptor la derecha): esa es la pose de reposo, no la de la
                // animacion en bruto (en el Peacekeeper la izquierda en bruto queda 20 cm mas atras, junto al receptor)
                var target = s.PalmTarget(s.Rec0, fk0, side);
                string tag = "tag_ik_loc_" + side;
                if (s.Skel.Parent.ContainsKey(tag))
                {
                    var tw = s.Rec0 * Basis(s.P, rest.Rel("j_gun", tag)); var ww = s.Rec0 * Basis(s.P, fk0.Rel("j_gun", "j_wrist_" + side));
                    Debug.Log("[BO7A] " + gun + " mano " + sd + ": " + tag + " a " + ((Pos(tw) - Pos(ww)).magnitude * 1000f).ToString("F0") + " mm y " + Quaternion.Angle(Rot(tw), Rot(ww)).ToString("F0") + " grados de la muneca en bruto");
                    target = tw * s.PalmOff[side];
                }
                if (side == "le" && Mathf.Abs(def.GripRollL) > 0.01f)
                {
                    // agarre mas bajo que el de BO7 (alli la mano va al costado, a la altura del anima, y en EFT tapa la vista al apuntar):
                    // se gira la pose de reposo alrededor del eje del canon hacia abajo
                    var ww = World(weapon);
                    System.Func<float, Matrix4x4> roll = ang => ww * Matrix4x4.Rotate(Quaternion.AngleAxis(ang, Vector3.up)) * ww.inverse * target;
                    var a1 = roll(def.GripRollL); var a2 = roll(-def.GripRollL);
                    target = weapon.InverseTransformPoint(Pos(a1)).z < weapon.InverseTransformPoint(Pos(a2)).z ? a1 : a2;
                }
                TwoBone(upper, fore, palm, Pos(target), fore.position);
                palm.rotation = Rot(target);
                s.RestPalm[side] = World(weapon).inverse * World(palm);
                foreach (var (c, e) in s.Fingers[side]) { s.RestFinger[e] = e.localRotation; s.FingerOff[e] = Quaternion.Inverse(Rot(s.Rec0 * Basis(s.P, fk0.Rel("j_gun", c)))) * e.rotation; }
                var d = World(palm).inverse * eftPalm0[side];
                Debug.Log("[BO7A] " + gun + " mano " + sd + ": reposo de BO7 a " + (Pos(d).magnitude * 1000f).ToString("F0") + " mm y " + Quaternion.Angle(Quaternion.identity, Rot(d)).ToString("F0") + " grados del reposo de EFT; respecto al arma " + ((Vector3)s.RestPalm[side].GetColumn(3)).ToString("F3") + "; EFT - BO7 (ejes del arma) " + weapon.InverseTransformVector(Pos(eftPalm0[side]) - palm.position).ToString("F3"));
                var ikm = s.IkMarker[side]; var mk = World(palm) * s.MarkerOff[ikm]; ikm.SetPositionAndRotation(Pos(mk), Rot(mk));
                Transform hm; if (s.HandMarker.TryGetValue(side, out hm) && hm != null) hm.SetPositionAndRotation(Pos(mk), Rot(mk));
            }
            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) s.Base[t] = (t.localPosition, t.localRotation);
            s.Push = -weapon.up * def.ViewPush;
            Debug.Log("[BO7A] " + gun + ": reposo de EFT " + (idleClip != null ? idleClip.name : "ninguno") + ", partes moviles " + string.Join(",", s.MoverRestInv.Keys));
            return s;
        }

        // ---------------------------------------------------------------- agarres (GripPose) de las dos manos en los accesorios del arma
        internal static void AddGripPoses(BuildBo7Guns.GunDef def)
        {
            var s = Build2(def.Id);
            try
            {
                // primero se miden las dos manos (guardar el prefab rehace sus instancias: con las dos manos en la misma pieza, Hawker, la
                // segunda ya no la encontraria)
                var grips = new List<(string side, string prefab, Matrix4x4 target, Quaternion[] fingers)>();
                foreach (var side in new[] { "le", "ri" })
                {
                    string prefab = side == "le" ? def.GripL : def.GripR;
                    if (prefab == null) continue;
                    var inst = s.Weapon.GetComponentsInChildren<Transform>(true).First(t => t.name == prefab);
                    var palm = s.Rig.Bones["Base Human" + (side == "le" ? "L" : "R") + "Palm"];
                    grips.Add((side, prefab, inst.worldToLocalMatrix * World(palm) * s.MarkerOff[s.IkMarker[side]], palm.GetComponentsInChildren<Transform>().Select(t => t.localRotation).ToArray()));
                }
                foreach (var (side, prefab, target, fingers) in grips)
                {
                    string path = "Assets/TarkovaCola/Prefabs/" + prefab + ".prefab";
                    var root = PrefabUtility.LoadPrefabContents(path);
                    var go = new GameObject("grip_pose_" + (side == "le" ? "l" : "r"));
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = Pos(target); go.transform.localRotation = Rot(target);
                    var gp = go.AddComponent<GripPose>();
                    gp.Hand = side == "le" ? GripPose.EHand.Left : GripPose.EHand.Right; gp.GripType = GripPose.EGripType.Common; gp.Fingers = fingers;
                    var so = new SerializedObject(gp); so.FindProperty("_cached").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    Debug.Log("[BO7A] agarre " + side + " en " + prefab + " " + Pos(target).ToString("F3") + " (" + fingers.Length + " huesos)");
                }
            }
            finally { Object.DestroyImmediate(s.Rig.Root); EftBundles.UnloadAll(); }
        }

        // Donde queda cada arma respecto a la camara de primera persona en reposo (x derecha, y arriba, z adelante; metros):
        //   tools/u.sh Bo7Anim.ViewProbe BO7_GUN=pk,vk,mx,vs,hw,br
        public static void ViewProbe()
        {
            foreach (var gun in Env("BO7_GUN", "pk").Split(','))
            {
                var s = Build2(gun);
                try
                {
                    s.Pose(s.Idle, 0f, PinsOf(s.Idle));
                    var cam = Matrix4x4.TRS(s.Rig.CamAnim.position, s.Rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f), Vector3.one).inverse;
                    System.Func<Vector3, string> c = p => ((Vector3)cam.MultiplyPoint3x4(p)).ToString("F3");
                    var bo7 = s.Weapon.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
                    float near = bo7.Count > 0 ? bo7.Min(r => cam.MultiplyPoint3x4(r.bounds.center).z - r.bounds.extents.magnitude * 0.5f) : 0f;
                    var fk = new Fk(s.Skel, s.Idle, 0f);
                    Debug.Log("[AIM] " + gun + " BO7: tag_view->j_gun " + ((Vector3)fk.Rel("tag_view", "j_gun").GetColumn(3)).ToString("F2") + " tag_view->j_wrist_ri " + ((Vector3)fk.Rel("tag_view", "j_wrist_ri").GetColumn(3)).ToString("F2")
                        + " j_gun->tag_flash " + (s.Skel.Parent.ContainsKey("tag_flash") ? ((Vector3)new Fk(s.Skel, new CodClip(), 0f).Rel("j_gun", "tag_flash").GetColumn(3)).ToString("F2") : "-")
                        + " | EFT camara->receptor " + (s.Rec.position - s.Rig.CamAnim.position).magnitude.ToString("F3") + " camara->palma der " + (s.Rig.Bones["Base HumanRPalm"].position - s.Rig.CamAnim.position).magnitude.ToString("F3"));
                    // lo mas cerca que llega el arma (cajas de sus mallas) a la camara en cada animacion
                    var sb = new System.Text.StringBuilder();
                    var verts = new Dictionary<Mesh, Vector3[]>(); float cone = float.Parse(Env("VIEW_CONE", "30"));
                    foreach (var cn in Env("BO7_VIEWCLIPS", "idle,reload,reload_empty,inspect,raise_first,raise").Split(','))
                    {
                        string path = Dir + "sat_vm_" + s.Def.Cod + "_" + cn + ".json";
                        if (!File.Exists(path)) continue;
                        var cl = LoadClip(path); var pins = PinsOf(cl); float best = 9f; int bf = 0; float reach = 0f; string bn = ""; float bestH = 9f; int hf = 0; string hn = "";
                        for (int f = 0; f < cl.Frames; f += 2)
                        {
                            s.ExtraPush = cn == "inspect" ? LookX(s, f / (float)(cl.Frames - 1)) : 0f;
                            s.Pose(cl, f, pins);
                            if (pins.At(pins.IkL, f) > 0.5f) reach = Mathf.Max(reach, s.ReachErr["le"]);
                            if (pins.At(pins.IkR, f) > 0.5f) reach = Mathf.Max(reach, s.ReachErr["ri"]);
                            var cp = s.Rig.CamAnim.position; var cf = s.Rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f) * Vector3.forward;
                            foreach (var hb in new[] { "Base HumanLPalm", "Base HumanRPalm", "Base HumanLDigit11", "Base HumanLDigit21", "Base HumanLDigit41", "Base HumanRDigit21", "Base HumanLForearm2", "Base HumanRForearm2" })
                            {
                                Transform ht; if (!s.Rig.Bones.TryGetValue(hb, out ht)) continue;
                                var dv = ht.position - cp; float d = dv.magnitude - 0.03f;   // ~3 cm de mano alrededor del hueso
                                if (Vector3.Angle(dv, cf) > cone) continue;
                                if (d < bestH) { bestH = d; hf = f; hn = hb.Substring(10); }
                            }
                            foreach (var mf in s.Weapon.GetComponentsInChildren<MeshFilter>(true))
                            {
                                var r = mf.GetComponent<Renderer>();
                                if (r == null || !r.enabled || !mf.gameObject.activeInHierarchy || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                                Vector3[] vs;
                                if (!verts.TryGetValue(mf.sharedMesh, out vs)) verts[mf.sharedMesh] = vs = mf.sharedMesh.vertices.Where((v, i) => i % 7 == 0).ToArray();
                                var l2w = mf.transform.localToWorldMatrix;
                                foreach (var v in vs)
                                {
                                    var dv = l2w.MultiplyPoint3x4(v) - cp; float d = dv.magnitude;
                                    if (Vector3.Angle(dv, cf) > cone) continue;
                                    if (d < best) { best = d; bf = f; bn = (mf.transform.parent != null ? mf.transform.parent.name + "/" : "") + mf.name; }
                                }
                            }
                        }
                        sb.Append(" " + cn + "=" + best.ToString("F3") + "@" + bf + "[" + bn.Replace("sat_vm_", "").Split('/')[0] + "] mano " + bestH.ToString("F3") + "@" + hf + hn + (reach > 0.005f ? "(mano corta " + (reach * 100f).ToString("F1") + "cm)" : ""));
                    }
                    s.ExtraPush = 0f;
                    Debug.Log("[AIM] " + gun + " minimo camara->arma:" + sb);
                    s.Pose(s.Idle, 0f, PinsOf(s.Idle));
                    Debug.Log("[AIM] " + gun + " vista: arma " + c(s.Weapon.position) + ", receptor BO7 " + c(s.Rec.position) + ", palma der " + c(s.Rig.Bones["Base HumanRPalm"].position) + ", palma izq " + c(s.Rig.Bones["Base HumanLPalm"].position) + ", eje adelante del arma " + ((Vector3)cam.MultiplyVector(-s.Weapon.up)).ToString("F2"));
                }
                finally { Object.DestroyImmediate(s.Rig.Root); EftBundles.UnloadAll(); }
            }
        }

        // ---------------------------------------------------------------- hoja de un clip de BO7
        public static void Run2()
        {
            try
            {
                string gun = Env("BO7_GUN", "pk");
                var s = Build2(gun);
                foreach (var clipName in Env("BO7_CLIP", "reload").Split(','))
                {
                    var clip = LoadClip(Dir + "sat_vm_" + s.Def.Cod + "_" + clipName + ".json");
                    var pins = Env("BO7_PINS", "1") == "1" ? PinsOf(clip) : Pins.All(clip.Frames);
                    string magBone = Env("BO7_MAGBONE", "j_mag1");
                    var frames = EftLab.ParseFrames(Env("BO7_FRAMES", "0-end/4"), clip.Frames - 1);
                    System.Action<int> pose = f => { s.ExtraPush = clipName == "inspect" ? LookX(s, f / (float)(clip.Frames - 1)) : 0f; s.Pose(clip, f, pins, magBone); };
                    if (Env("BO7_AIM", "0") == "1")
                    {
                        // vista al apuntar: el ojo en el mod_aim_camera de la mira, mirando por el canon (LAB_FOV=50 se parece al juego)
                        var eye = new GameObject("ojo").transform; var real = s.Rig.CamAnim;
                        var aim = s.Weapon.GetComponentsInChildren<Transform>(true).First(t => t.name == "mod_aim_camera");
                        pose = f =>
                        {
                            s.Rig.CamAnim = real; s.Pose(clip, f, pins, magBone); s.Rig.CamAnim = eye;
                            eye.SetPositionAndRotation(aim.position, Quaternion.LookRotation(s.Weapon.rotation * Vector3.down, s.Weapon.rotation * Vector3.forward) * Quaternion.Inverse(Quaternion.Euler(90f, 0f, 0f)));
                        };
                    }
                    else if (Env("BO7_AIM", "0") == "2")
                    {
                        // como EFT (ProceduralWeaponAnimation): profundidad del ojo = Weapon_root_anim + DefaultAimPlane.Depth hacia delante;
                        // lado = mod_aim_camera; altura = la linea mod_align_rear -> mod_align_front a esa profundidad
                        var eye = new GameObject("ojo").transform; var real = s.Rig.CamAnim;
                        var all = s.Weapon.GetComponentsInChildren<Transform>(true);
                        var aim = all.First(t => t.name == "mod_aim_camera");
                        System.Func<string, Transform> deepest = n => all.Where(t => t.name == n).OrderByDescending(t => { int d = 0; for (var p = t; p != null; p = p.parent) d++; return d; }).FirstOrDefault();
                        var rear = deepest("mod_align_rear"); var front = deepest("mod_align_front");
                        var rootAnim = s.Rig.Root.GetComponentsInChildren<Transform>(true).First(t => t.name == "Weapon_root_anim");
                        float depth = float.Parse(Env("AIM_DEPTH", "0.08"), System.Globalization.CultureInfo.InvariantCulture);
                        bool logged = false;
                        pose = f =>
                        {
                            s.Rig.CamAnim = real; s.Pose(clip, f, pins, magBone); s.Rig.CamAnim = eye;
                            Vector3 fwd = s.Weapon.rotation * Vector3.down, up = s.Weapon.rotation * Vector3.forward;
                            var P = rootAnim.TransformPoint(new Vector3(0f, -depth, 0f));
                            var e = aim.position + fwd * Vector3.Dot(P - aim.position, fwd);
                            if (rear != null && front != null)
                            {
                                var lr = s.Weapon.InverseTransformPoint(rear.position); var lf = s.Weapon.InverseTransformPoint(front.position); var le = s.Weapon.InverseTransformPoint(e);
                                float k = (le.y - lr.y) / (lf.y - lr.y); le.z = lr.z + (lf.z - lr.z) * k; e = s.Weapon.TransformPoint(le);
                            }
                            eye.SetPositionAndRotation(e, Quaternion.LookRotation(fwd, up) * Quaternion.Inverse(Quaternion.Euler(90f, 0f, 0f)));
                            if (!logged)
                            {
                                logged = true;
                                Debug.Log("[AIM] " + gun + ": ojo EFT en (arma) " + s.Weapon.InverseTransformPoint(e).ToString("F3") + ", mod_aim_camera " + s.Weapon.InverseTransformPoint(aim.position).ToString("F3")
                                    + ", align_rear " + (rear ? s.Weapon.InverseTransformPoint(rear.position).ToString("F3") : "-") + ", align_front " + (front ? s.Weapon.InverseTransformPoint(front.position).ToString("F3") : "-")
                                    + ", root_anim " + s.Weapon.InverseTransformPoint(rootAnim.position).ToString("F3") + " ejes y " + s.Weapon.InverseTransformDirection(rootAnim.up).ToString("F2")
                                    + "; distancia ojo->mira trasera " + (rear ? Vector3.Dot(rear.position - e, fwd).ToString("F3") : "-"));
                            }
                        };
                    }
                    EftLab.RenderSheet(s.Rig, pose, frames, Env("BO7_OUT", "Preview/bo7c_" + gun + "_" + clipName + ".png"));
                    if (Env("BO7_AIM", "0") != "0") s.Rig.CamAnim = s.Rig.Bones["Camera_animated"];
                    Debug.Log("[BO7A] " + gun + " " + clipName + ": " + clip.Frames + " fotogramas");
                }
            }
            catch (System.Exception e) { Debug.LogError("[BO7A] " + e); }
            finally { EftBundles.UnloadAll(); }
        }

        // fraccion de Push que se suma al inspeccionar en el instante t (0..1); entra y sale suave en el primer y ultimo 15 %
        static float LookX(Solver2 s, float t) => s.Def.ViewPush > 0f ? s.Def.LookPush / s.Def.ViewPush * Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f - t) / 0.15f) : 0f;

        // ---------------------------------------------------------------- horneado
        internal static AnimationClip Bake2(Solver2 s, CodClip clip, Piece piece, List<Transform> rec, CodClip parts = null)
        {
            float secs = piece.Seconds * piece.Speed;
            int n = Mathf.Max(2, Mathf.RoundToInt(piece.Seconds * 30f) + 1);
            var curves = rec.ToDictionary(t => t, t => new AnimationCurve[7].Select(_ => new AnimationCurve()).ToArray());
            var pins = PinsOf(clip);
            var mag = s.Mag != null ? piece.Mag : null;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1), f = piece.FrameAt(t), time = t * secs;
                // inspeccion: el arma se aleja algo mas (entra y sale suave en el primer y ultimo 15 %)
                s.ExtraPush = piece.State == "LOOK" ? LookX(s, t) : 0f;
                if (parts != null) s.Pose(clip, 0f, pins, piece.MagBone ?? "j_mag1", parts, f); else s.Pose(clip, f, pins, piece.MagBone ?? "j_mag1");
                if (mag != null && (mag.Mode == MagMode.Seat || (mag.Mode == MagMode.SeatAfter && f >= mag.From)))
                {
                    var seat = World(s.Weapon) * (s.Rec0 * s.WeaponInRec).inverse * s.MagSlot0;
                    s.Mag.SetPositionAndRotation(Pos(seat), Rot(seat));
                }
                foreach (var kv in curves)
                {
                    var p = kv.Key.localPosition; var q = kv.Key.localRotation;
                    var v = new[] { p.x, p.y, p.z, q.x, q.y, q.z, q.w };
                    for (int c = 0; c < 7; c++) kv.Value[c].AddKey(new Keyframe(time, v[c]));
                }
            }
            var res = new AnimationClip { name = piece.Eft, frameRate = 30f };
            string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
            foreach (var kv in curves)
            {
                string path = AnimationUtility.CalculateTransformPath(kv.Key, s.Rig.Root.transform);
                for (int c = 0; c < 7; c++) res.SetCurve(path, typeof(Transform), props[c], kv.Value[c]);
            }
            res.EnsureQuaternionContinuity();
            var events = new List<AnimationEvent>();
            if (!piece.Silent)
                foreach (var note in clip.Notes.Where(x => x.Key.StartsWith("AudioOneShot@")))
                    foreach (int f in note.Value)
                    {
                        float t = piece.TimeOf(f);
                        if (t >= 0f) events.Add(new AnimationEvent { functionName = "Bo7Snd", stringParameter = note.Key.Substring(13), time = t * secs });
                    }
            AnimationUtility.SetAnimationEvents(res, events.OrderBy(e => e.time).ToArray());
            return res;
        }

        private static List<Transform> Recorded2(Solver2 s)
        {
            var res = s.Rig.Root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Base Human") || t.name == "Camera_animated" || t.name == "Weapon_root" || t.name == "Weapon_root_anim").ToList();
            res.Add(s.Weapon);
            foreach (Transform c in s.Weapon) res.Add(c);
            return res;
        }

        // tramo natural: de 'a' a 'b' (fotogramas de BO7) a su velocidad
        internal static Piece Nat(string eft, string clip, float a, float b, string state = null, float speed = 1f) =>
            new Piece { Eft = eft, Clip = clip, Anchors = new[] { (0f, a), (1f, b) }, State = state, Speed = speed };
        // varios tramos seguidos (a, b, duracion en fotogramas): b < a = hacia atras, a == b = parado
        internal static Piece Seq(string eft, string clip, string state, params (float a, float b, float dur)[] segs)
        {
            float total = segs.Sum(x => x.dur), t = 0f;
            var an = new List<(float, float)>();
            foreach (var g in segs) { an.Add((t / total, g.a)); t += g.dur; an.Add((t / total - 1e-4f, g.b)); }
            an[an.Count - 1] = (1f, an[an.Count - 1].Item2);
            return new Piece { Eft = eft, Clip = clip, Anchors = an.ToArray(), State = state, Frames = total };
        }
        private static Piece With(this Piece p, MagPlan mag = null, string magBone = null, bool silent = false, params (string ev, float f)[] ev)
        { p.Mag = mag ?? p.Mag; p.MagBone = magBone ?? p.MagBone; p.Silent = silent; if (ev.Length > 0) p.Ev = ev; return p; }

        private static Piece E(this Piece p, params (string ev, float f)[] ev) { p.Ev = ev; return p; }
        private static Piece Sp(this Piece p, float speed) { p.Speed = speed; return p; }
        private static readonly MagPlan Seated = new MagPlan { Mode = MagMode.Seat };
        private static Piece St(this Piece p) { p.Mag = Seated; return p; }

        // Fusil con cargador: nombres de los clips del arma base y fotogramas clave de BO7 (se miran con Run2 y tools/bo7_notes.py)
        internal sealed class Rifle
        {
            public string[] Idle, Charge, FmAuto, FmSemi;
            // armas con reten de cerrojo (AR, SCAR): tras la recarga en vacio se suelta el reten (estado RELOAD CATCH END) en vez de montar;
            // montar (RELOAD CHARGE END) sale entonces del primer sacado (raise_first), entre PullFrom y el final
            public string[] Catch, DrawFirsts;
            public float PullFrom = -1f;
            public string InBone;
            public string CatchState;   // estado de EFT del reten si no es el de la SCAR (MP9: RELOAD CATCH END IDLE)   // hueso del cargador NUEVO en RELOAD IN (MXR-17: j_mag2; j_mag1 es el viejo que sale)
            public string Fire, Start, Out, In, End, OutAll, Hit, Admin, DrawFirst, Draw, Holster, Look;
            public float FireSpeed = 10.83f, StartSpeed = 0.75f, EndSpeed = 0.73f;
            // recarga: la mano suelta en Off, el cargador sale en MagOut y deja de verse en Swap, el nuevo entra en MagIn y la mano vuelve desde Back
            public float Off, MagOut, Swap, MagIn, Back, ReloadEnd;
            // recarga en vacio: el cargador cae en Eject (clip reload_empty), el cerrojo se monta entre ChargeFrom y el final (suelta en Arm)
            public float Eject, ChargeFrom, Arm, EmptyEnd;
            public float FastOut, FastIn, FastEnd;                 // reload_fast01
            public float DrawArm, DrawChamber, DrawFirstEnd, DrawEnd, HolsterEnd, LookEnd, FireEnd, SemiOffEnd, SemiOnEnd;

            public Piece[] Jobs()
            {
                var l = new List<Piece>();
                foreach (var c in Idle) l.Add(Seq(c, "idle", "IDLE", (0f, 0f, 10f)).Sp(0.5f).St());
                l.Add(new Piece { Eft = Fire, Clip = "idle", Parts = "fire", Anchors = new[] { (0f, 0f), (1f, FireEnd) }, Speed = FireSpeed, Mag = Seated, Silent = true });
                l.Add(Nat(Start, "reload", 0f, Off, "RELOAD START", StartSpeed).St());
                l.Add(Nat(Out, "reload", Off, Swap, "RELOAD OUT").E(("MagOut", MagOut), ("MagHide", Swap - 0.5f)));
                { var pin = Nat(In, "reload", Swap, Back, "RELOAD IN").E(("MagShow", Swap + 0.5f), ("MagIn", MagIn)); pin.MagBone = InBone; l.Add(pin); }
                l.Add(Nat(End, "reload", Back, ReloadEnd, "RELOAD END", EndSpeed).St());
                bool first = true;
                if (Catch != null)
                {
                    // cada variante tiene su estado (RELOAD CATCH END / CATCH TO IDLE / "RELOAR" CATCH RACK; RELOAD CHARGE END / CHARGE TO IDLE)
                    foreach (var c in Catch)
                        l.Add(Nat(c, "reload_empty", ChargeFrom, EmptyEnd, CatchState ?? (c.Contains("rack") ? "RELOAR CATCH RACK" : c.Contains("to_idle") ? "RELOAD CATCH TO IDLE" : "RELOAD CATCH END")).St().E(("OffBoltCatch", Arm), ("AddAmmoInChamber", Arm + 1f)));
                    foreach (var c in Charge)
                        l.Add(Nat(c, "raise_first", PullFrom, DrawFirstEnd, c.Contains("idle") ? "RELOAD CHARGE TO IDLE" : "RELOAD CHARGE END").St().E(("Arm", DrawArm), ("AddAmmoInChamber", DrawChamber)));
                }
                else foreach (var c in Charge) { var pc = Nat(c, "reload_empty", ChargeFrom, EmptyEnd, first ? "RELOAD CHARGE END" : null).St(); if (first) pc.E(("Arm", Arm), ("AddAmmoInChamber", Arm + 1f)); l.Add(pc); first = false; }
                l.Add(Nat(OutAll, "reload_empty", 0f, Eject + 4f, "RELOAD OUT ALL").E(("MagOut", Eject), ("MagHide", Eject + 3f)));
                if (Hit != null) l.Add(Nat(Hit, "reload_fast01", 0f, FastEnd, "RELOAD HIT").E(("MagShow", 1f), ("MagOut", FastOut), ("MagIn", FastIn), ("MagHide", FastIn + 14f)));
                if (Admin != null) l.Add(Nat(Admin, "reload", 0f, ReloadEnd, "RELOAD ADMIN").E(("MagShow", 1f), ("MagOut", MagOut), ("MagIn", MagIn), ("MagHide", MagIn + 16f)));
                l.Add(Nat(DrawFirst, "raise_first", 0f, DrawFirstEnd, "OUT TO IDLE ARM").St().E(("Arm", DrawArm), ("AddAmmoInChamber", DrawChamber)));
                foreach (var c in DrawFirsts ?? new string[0]) l.Add(Nat(c, "raise_first", 0f, DrawFirstEnd).St());
                l.Add(Nat(Draw, "raise", 0f, DrawEnd, "OUT TO IDLE").St());
                l.Add(Nat(Holster, "drop", 0f, HolsterEnd, "IDLE TO OUT").St());
                l.Add(Nat(Look, "inspect", 0f, LookEnd, "LOOK"));
                foreach (var c in FmAuto) l.Add(Nat(c, "selectsemi_off", 0f, SemiOffEnd).St());
                foreach (var c in FmSemi) l.Add(Nat(c, "selectsemi_on", 0f, SemiOnEnd).St());
                return l.ToArray();
            }
        }

        // Estados y eventos: Preview/probe_<arma base>.txt (EftProbe2). Los fotogramas salen de las notas de BO7 (tools/bo7_notes.py).
        internal static readonly Dictionary<string, Piece[]> Jobs2 = new Dictionary<string, Piece[]>
        {
            // Peacekeeper Mk1 (ar_condor) sobre el AK-12 (clips rpk16_*)
            ["pk"] = new Rifle
            {
                Idle = new[] { "rpk16_idle_armed" }, Fire = "rpk16_fire", Start = "rpk16_hands_out_half_1", Out = "rpk16_mag_out", In = "rpk16_mag_in", End = "rpk16_hands_in_half_2",
                Charge = new[] { "rpk16_reload_charge" }, OutAll = "rpk16_mag_out_all", Hit = "rpk16_mag_hit", Admin = "rpk16_mag_admin",
                DrawFirst = "rpk16_out_to_idle_arm", Draw = "rpk16_out_to_idle", Holster = "rpk16_idle_to_out", Look = "rpk16_look",
                FmAuto = new[] { "ak12_selector_0_2", "ak12_selector_1_0" }, FmSemi = new[] { "ak12_selector_2_1" },
                Off = 8f, MagOut = 21f, Swap = 25f, MagIn = 42f, Back = 52f, ReloadEnd = 81f, Eject = 12f, ChargeFrom = 50f, Arm = 62f, EmptyEnd = 93f,
                FastOut = 22f, FastIn = 36f, FastEnd = 68f, DrawArm = 15f, DrawChamber = 21f, DrawFirstEnd = 54f, DrawEnd = 30f, HolsterEnd = 13f, LookEnd = 243f,
                FireEnd = 3f, SemiOffEnd = 18f, SemiOnEnd = 19f,
            }.Jobs(),
            // Voyak KT-3 (ar_heron) sobre el AK-74M: coge el cargador nuevo y con el saca el viejo (cambio fuera de vista en el 16), lo mete hacia el 38
            ["vk"] = new Rifle
            {
                Idle = new[] { "ak74m_idle_armed", "ak74m_launcher_0_idle_off" }, Fire = "ak74m_fire", Start = "ak74m_hands_out_half_1", Out = "ak74m_mag_out", In = "ak74m_mag_in", End = "ak74m_hands_in_half_2",
                Charge = new[] { "ak100_reload_charge", "ak74m_reload_charge" }, OutAll = "ak74m_mag_out_all", Hit = "ak74m_mag_hit", Admin = "ak74m_mag_admin",
                DrawFirst = "ak74m_out_to_idle_arm", Draw = "ak74m_out_to_idle", Holster = "ak74m_idle_to_out", Look = "ak74m_look",
                FmAuto = new[] { "ak74m_firemode_0" }, FmSemi = new[] { "ak74m_firemode_1" },
                Off = 5f, MagOut = 12f, Swap = 16f, MagIn = 38f, Back = 50f, ReloadEnd = 80f, Eject = 27f, ChargeFrom = 52f, Arm = 67f, EmptyEnd = 100f,
                FastOut = 18f, FastIn = 33f, FastEnd = 65f, DrawArm = 13f, DrawChamber = 16f, DrawFirstEnd = 49f, DrawEnd = 26f, HolsterEnd = 17f, LookEnd = 294f,
                FireEnd = 2f, SemiOffEnd = 22f, SemiOnEnd = 18f,
            }.Jobs(),
            // MXR-17 (ar_eagle) sobre la SCAR-L (clips scar_16_*; el cliente empareja "mag_out" con mag_out_0..7). Fotogramas medidos sobre los
            // huesos: el cargador nuevo (j_mag2) llega al arma en el 14-18, el viejo (j_mag1) sale en el 26 y el nuevo asienta en el 42; en vacio
            // el cargador cae en el 17 y el reten suelta el cerrojo (j_bolt_carrier) en el 63; al sacarla por primera vez monta en el 16-20.
            // SIN VER en el laboratorio (Unity sin licencia el 2026-11-12): repasar con Run2 antes de dar por buenos los tiempos.
            ["mx"] = new Rifle
            {
                Idle = new[] { "scar_16_idle_armed" }, Fire = "scar_16_fire", Start = "scar_16_hands_out_half_1", Out = "scar_16_mag_out", In = "scar_16_mag_in", End = "scar_16_hands_in_half_2",
                Catch = new[] { "scar_16_catch", "scar_16_catch_to_idle", "scar_16_catch_rack" }, Charge = new[] { "scar_16_charge", "scar_16_charge_to_idle" }, PullFrom = 10f, InBone = "j_mag2", OutAll = "scar_16_mag_out_all",
                DrawFirst = "scar_16_out_to_idle_arm", Draw = "scar_16_out_to_idle", Holster = "scar_16_idle_to_out", Look = "scar_16_look",
                FmAuto = new[] { "scar_16_firemode_0" }, FmSemi = new[] { "scar_16_firemode_1" },
                Off = 3f, MagOut = 14f, Swap = 18f, MagIn = 40f, Back = 56f, ReloadEnd = 79f, Eject = 17f, ChargeFrom = 48f, Arm = 63f, EmptyEnd = 95f,
                DrawArm = 17f, DrawChamber = 20f, DrawFirstEnd = 54f, DrawEnd = 33f, HolsterEnd = 16f, LookEnd = 240f,
                FireEnd = 2f, SemiOffEnd = 22f, SemiOnEnd = 18f,
            }.Jobs(),
            // VST (sm_bear) sobre la MP9 (clips mp9_*; el cliente empareja mp9_mag_out con mp9_mag_out_0..3). Medido con tools/bo7_timing.py:
            // recarga: el viejo (j_mag1) sale en el 20 y lo suelta en el 44-54; el nuevo (j_mag2, ya en la mano desde el 2) asienta en el 66.
            // En vacio el cerrojo esta atras desde el principio y se suelta en el 61 (reten, como la MP9); al sacarla por primera vez monta en el 10-12.
            ["vs"] = new Rifle
            {
                Idle = new[] { "mp9_idle_armed" }, Fire = "mp9_fire", FireSpeed = 15.83f, Start = "mp9_reload_start", Out = "mp9_mag_out", In = "mp9_mag_in", End = "mp9_reload_end",
                Catch = new[] { "mp9_catch_end" }, CatchState = "RELOAD CATCH END IDLE", Charge = new[] { "mp9_reload_charge", "mp9_reload_charge_idle" }, PullFrom = 4f,
                OutAll = "mp9_mag_out_all", InBone = "j_mag2",
                DrawFirst = "mp9_out_to_idle_arm", Draw = "mp9_out_to_idle", Holster = "mp9_idle_to_out", Look = "mp9_look",
                FmAuto = new[] { "mp9_firemode_0" }, FmSemi = new[] { "mp9_firemode_1" },
                Off = 4f, MagOut = 20f, Swap = 44f, MagIn = 66f, Back = 68f, ReloadEnd = 82f, Eject = 18f, ChargeFrom = 50f, Arm = 61f, EmptyEnd = 93f,
                DrawArm = 11f, DrawChamber = 13f, DrawFirstEnd = 47f, DrawEnd = 28f, HolsterEnd = 14f, LookEnd = 286f,
                FireEnd = 2f, SemiOffEnd = 22f, SemiOnEnd = 18f,
            }.Jobs(),
            // M10 Breacher (sh_corso) sobre la Mossberg 590A1 (clips m590_*). Medido con tools/bo7_timing.py: en rechamber la bomba
            // (tag_guard_attach/j_bolt) va atras en el 2-6 y vuelve en el 8-10; en reload_empty_chamber_start va atras en el 6-8, el cartucho
            // aparece en el 17 y la bomba cierra en el 40-42; en reload_loop el cartucho entra en el tubo hacia el 15; raise_first bombea en el 20-22.
            // FIRE de EFT incluye el bombeo: se espera 6 fotogramas (retroceso procedural de EFT) y luego rechamber entero.
            // Todo St(): el corso tiene j_mag1 (variante de cargador) y no debe mover el tubo (mod_magazine).
            ["br"] = new[]
            {
                Seq("m590_idle", "idle", "IDLE", (0f, 0f, 10f)).Sp(0.5f).St(),
                Seq("m590_fire", "rechamber", "FIRE", (0f, 0f, 6f), (0f, 19f, 20f)).St().E(("ShellEject", 5f), ("DelAmmoChamber", 5.5f), ("AddAmmoInChamber", 6.5f)),
                Nat("m590_rechamber", "rechamber", 0f, 19f, "RECHAMBER").St().E(("ShellEject", 5f), ("DelAmmoChamber", 5.5f), ("AddAmmoInChamber", 6.5f)),
                Nat("m590_hands_out", "reload_start", 0f, 27f, "RELOAD START", 1.2f).St(),
                Nat("m590_ammo_in_loop_fix", "reload_loop", 0f, 22f, "RELOAD IN", 1.2f).St().E(("AddAmmoInMag", 15f)),
                Nat("m590_ammo_in", "reload_empty_chamber_start", 0f, 45f, "RELOAD FIRST").St().E(("MagShow", 17f), ("AddAmmoInChamber", 41f)),
                Nat("m590_ammo_in", "reload_empty_chamber_start", 0f, 45f, "LOAD ONE").St().E(("MagShow", 17f), ("AddAmmoInChamber", 41f)),
                Nat("m590_hands_in_half_2", "reload_end", 0f, 23f, "RELOAD END").St(),
                Nat("m590_out_to_idle_arm", "raise_first", 0f, 42f, "OUT TO IDLE ARM").St().E(("AddAmmoInChamber", 21f)),
                Nat("m590_out_to_idle", "raise", 0f, -1f, "OUT TO IDLE").St(),
                Nat("m590_idle_to_out", "drop", 0f, -1f, "IDLE TO OUT").St(),
                Nat("m590_look", "inspect", 0f, -1f, "LOOK"),
            },
            // Hawker HX (sn_aden) sobre la AXMC (clips axmc_*). Medido con tools/bo7_timing.py: en rechamber el cerrojo (j_bolt) gira en el 6-9,
            // va atras en el 12-15 (sale la vaina), vuelve en el 18-21 y baja hacia el 30. Recarga (reload): el viejo (j_mag1) sale del arma en
            // el 24-27 y desaparece en el 60 (vuelve a su sitio oculto en el 63); el nuevo (j_mag2, en la mano desde el principio) asienta en el
            // 87-93. En vacio (reload_empty) el viejo cae en el 42-57. raise_first monta el cerrojo: atras en el 12-21, delante en el 24-27, baja en el 33.
            // axmc_fire_chambering lo comparten FIRE RELOAD, RECHAMBER, MISFIRE_REPAIR y RELOAD CHARGE END MAG: un clip, eventos por estado.
            // Sin tocar (siguen los de EFT): carga de balas sueltas (bolt_open_to_load, load_loop, bolt_close...), check, discharge, culata.
            ["hw"] = new[]
            {
                Seq("axmc_idle_armed", "idle", "IDLE", (0f, 0f, 10f)).Sp(0.5f).St(),
                Seq("axmc_idle_disarmed", "idle", null, (0f, 0f, 10f)).Sp(0.5f).St(),
                new Piece { Eft = "axmc_fire", Clip = "idle", Parts = "fire", Anchors = new[] { (0f, 0f), (1f, 5f) }, Speed = 2f, Mag = Seated, Silent = true },
                Nat("axmc_fire_chambering", "rechamber", 0f, 50f, "FIRE RELOAD").St().E(("ShellEject", 14f), ("DelAmmoChamber", 14.5f), ("AddAmmoInChamber", 20f), ("Arm", 29f)),
                Nat("axmc_fire_chambering", "rechamber", 0f, 50f, "RECHAMBER").St().E(("ShellEject", 14f), ("DelAmmoChamber", 14.5f), ("AddAmmoInChamber", 20f), ("Arm", 29f)),
                Nat("axmc_fire_chambering", "rechamber", 0f, 50f, "MISFIRE_REPAIR").St().E(("ShellEject", 14f), ("DelAmmoChamber", 14.5f), ("AddAmmoInChamber", 20f), ("Arm", 29f)),
                Nat("axmc_fire_chambering", "rechamber", 0f, 50f, "RELOAD CHARGE END MAG").St().E(("AddAmmoInChamber", 20f), ("Arm", 29f)),
                Nat("axmc_mag_out", "reload", 0f, 60f, "RELOAD OUT MAG").E(("MagOut", 26f), ("MagHide", 59.5f)),
                Nat("axmc_mag_in", "reload", 60f, 95f, "RELOAD IN MAG").With(magBone: "j_mag2", ev: new[] { ("MagShow", 60.5f), ("MagIn", 88f) }),
                Nat("axmc_hands_in_half_2", "reload", 95f, 107f, "RELOAD END MAG").St(),
                Nat("axmc_mag_out_all", "reload_empty", 0f, 57f, "RELOAD OUT ALL MAG").E(("MagOut", 43f), ("MagHide", 56f)),
                Nat("axmc_out_to_idle_arm", "raise_first", 0f, 63f, "OUT TO IDLE ARM").St().E(("AddAmmoInChamber", 26f), ("Arm", 33f)),
                Nat("axmc_out_to_idle", "raise", 0f, -1f, "OUT TO IDLE").St(),
                Nat("axmc_idle_to_out", "drop", 0f, -1f, "IDLE TO OUT").St(),
                Nat("axmc_look", "inspect", 0f, -1f, "LOOK"),
            },
        };

        public static void BakeRun2()
        {
            try
            {
                string gun = Env("BO7_GUN", "pk");
                var s = Build2(gun);
                var rec = Recorded2(s);
                Directory.CreateDirectory(Dir + "Baked");
                foreach (var old in Directory.GetFiles(Dir + "Baked", gun + "_*.anim")) AssetDatabase.DeleteAsset(old.Replace('\\', '/'));
                var clips = new Dictionary<string, CodClip>();
                var baked = new Dictionary<string, AnimationClip>();
                var events = new SortedDictionary<string, SortedDictionary<string, float>>();
                foreach (var piece in Jobs2[gun])
                {
                    if (!clips.ContainsKey(piece.Clip)) clips[piece.Clip] = LoadClip(Dir + "sat_vm_" + s.Def.Cod + "_" + piece.Clip + ".json");
                    var cc = clips[piece.Clip];
                    piece.Anchors = piece.Anchors.Select(a => (a.t, a.f < 0f ? cc.Frames - 1f : a.f)).ToArray();
                    if (piece.Parts != null && !clips.ContainsKey(piece.Parts)) clips[piece.Parts] = LoadClip(Dir + "sat_vm_" + s.Def.Cod + "_" + piece.Parts + ".json");
                    var c = Bake2(s, cc, piece, rec, piece.Parts != null ? clips[piece.Parts] : null);
                    c.name = gun + "_" + piece.Eft;
                    AssetDatabase.CreateAsset(c, Dir + "Baked/" + c.name + ".anim");
                    baked[piece.Eft] = c;
                    if (piece.State != null && piece.Ev != null)
                    {
                        if (!events.ContainsKey(piece.State)) events[piece.State] = new SortedDictionary<string, float>();
                        foreach (var (ev, f) in piece.Ev) events[piece.State][ev] = f <= -1f ? -f - 1f : Mathf.Clamp(piece.TimeOf(f) < 0f ? (f >= piece.Anchors[piece.Anchors.Length - 1].f ? 0.99f : 0f) : piece.TimeOf(f), 0f, 0.99f);
                    }
                    Debug.Log("[BO7A] horneado " + c.name + " <- " + piece.Clip + " " + string.Join(" ", piece.Anchors.Select(a => a.t.ToString("0.###") + ":" + a.f)) + " = " + c.length.ToString("F2") + " s");
                }
                AssetDatabase.SaveAssets();
                WriteEvents(gun, events);
                if (Env("BO7_DEBUG", "0") == "1")
                {
                    var pc = Jobs2[gun].First(p => p.Eft == "rpk16_mag_in"); var cl = clips[pc.Clip]; var pl = s.Rig.Bones["Base HumanLPalm"];
                    s.Pose(cl, pc.FrameAt(0.5f), PinsOf(cl)); var a1 = pl.position; var m1 = s.Mag.position; var l1 = pl.localRotation;
                    s.Reset(); baked[pc.Eft].SampleAnimation(s.Rig.Root, baked[pc.Eft].length * 0.5f);
                    Debug.Log("[BO7A] depuracion: palma pose " + a1.ToString("F3") + " clip " + pl.position.ToString("F3") + " | cargador pose " + m1.ToString("F3") + " clip " + s.Mag.position.ToString("F3") + " | giro " + l1.ToString("F3") + " / " + pl.localRotation.ToString("F3") + " ruta " + AnimationUtility.CalculateTransformPath(pl, s.Rig.Root.transform) + " raiz " + s.Rig.Root.name + " activo " + s.Rig.Root.activeInHierarchy);
                }
                var sheet = Env("BO7_SHEET", "").Split(',').Where(baked.ContainsKey).ToList();
                if (sheet.Count == 0) { BuildAnimBundles(); return; }
                float step = float.Parse(Env("BO7_STEP", "4")) / 30f;
                var seq = new List<(AnimationClip c, float t)>();
                foreach (var name in sheet) for (float t = 0f; t <= baked[name].length + 1e-4f; t += step * Jobs2[gun].First(p => p.Eft == name).Speed) seq.Add((baked[name], t));
                EftLab.RenderSheet(s.Rig, i => { s.Reset(); seq[i].c.SampleAnimation(s.Rig.Root, seq[i].t); BuildBo7Guns.Follow(s.Followers); }, Enumerable.Range(0, seq.Count).ToList(), Env("BO7_OUT", "Preview/bo7b_" + gun + ".png"));
                BuildAnimBundles();
            }
            catch (System.Exception e) { Debug.LogError("[BO7A] " + e); }
            finally { EftBundles.UnloadAll(); }
        }

        // bo7_events.json: { arma: { estado: { evento: tiempo normalizado } } }; cada arma reescribe solo su parte
        private const string EventsPath = Dir + "Baked/bo7_events.json";
        private static void WriteEvents(string gun, SortedDictionary<string, SortedDictionary<string, float>> ev)
        {
            var all = new SortedDictionary<string, string>();
            if (File.Exists(EventsPath))
                foreach (var kv in (Dictionary<string, object>)Json.Parse(File.ReadAllText(EventsPath)))
                    all[kv.Key] = "{" + string.Join(",", ((Dictionary<string, object>)kv.Value).Select(st => "\"" + st.Key + "\":{" + string.Join(",", ((Dictionary<string, object>)st.Value).Select(e => "\"" + e.Key + "\":" + System.Convert.ToDouble(e.Value).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture))) + "}")) + "}";
            all[gun] = "{" + string.Join(",", ev.Select(st => "\"" + st.Key + "\":{" + string.Join(",", st.Value.Select(e => "\"" + e.Key + "\":" + e.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture))) + "}")) + "}";
            File.WriteAllText(EventsPath, "{" + string.Join(",\n", all.Select(kv => "\"" + kv.Key + "\":" + kv.Value)) + "}");
            AssetDatabase.ImportAsset(EventsPath);
        }

        internal static void BuildAnimBundles()
        {
            var all = Directory.GetFiles(Dir + "Baked").Where(p => p.EndsWith(".anim") || p.EndsWith(".json")).Select(p => p.Replace('\\', '/')).ToArray();
            Directory.CreateDirectory("Build/anims");
            var sounds = Directory.Exists(SoundDir) ? Directory.GetFiles(SoundDir).Where(p => p.EndsWith(".wav") || p.EndsWith(".json")).Select(p => p.Replace('\\', '/')).ToArray() : new string[0];
            var builds = new List<AssetBundleBuild> { new AssetBundleBuild { assetBundleName = BundleName, assetNames = all } };
            if (sounds.Length > 0) builds.Add(new AssetBundleBuild { assetBundleName = SoundBundle, assetNames = sounds });
            BuildPipeline.BuildAssetBundles("Build/anims", builds.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            foreach (var b in builds) File.Copy("Build/anims/" + b.assetBundleName, "../Client/assets/" + b.assetBundleName, true);
            Debug.Log("[BO7A] " + BundleName + ": " + all.Length + " archivos, " + SoundBundle + ": " + sounds.Length + " -> Client/assets");
        }
    }
}
