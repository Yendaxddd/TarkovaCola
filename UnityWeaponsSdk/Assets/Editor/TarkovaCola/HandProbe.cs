using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// DIAGNOSTICO (no forma parte del mod): vuelca las dos manos, la de EFT y la de BO7, hueso a hueso, para medir la traduccion de dedos.
//   BO7_GUN=pk BO7_CLIP=reload BO7_FRAMES=0,14,18,36,40,44,50  -> Preview/handprobe_<gun>_<clip>.json
namespace TarkovaCola
{
    public static partial class Bo7Anim
    {
        private static string M(Matrix4x4 m) { var v = new float[16]; for (int i = 0; i < 16; i++) v[i] = m[i / 4, i % 4]; return "[" + string.Join(",", v.Select(x => x.ToString("R", CultureInfo.InvariantCulture))) + "]"; }

        public static void HandProbe()
        {
            try
            {
                string gun = Env("BO7_GUN", "pk"), clipName = Env("BO7_CLIP", "reload");
                var s = Build2(gun);
                var clip = LoadClip(Dir + "sat_vm_" + s.Def.Cod + "_" + clipName + ".json");
                var pins = PinsOf(clip);
                var sb = new StringBuilder("{\n");
                var hand = new List<Transform>();
                foreach (var sd in new[] { "L", "R" }) { hand.Add(s.Rig.Bones["Base Human" + sd + "Forearm1"]); hand.AddRange(s.Rig.Bones["Base Human" + sd + "Palm"].GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Base Human"))); }
                var codBones = s.Skel.Parent.Keys.Where(b => b.StartsWith("j_wrist_") || b.StartsWith("j_meta") || Fingers.Any(f => b.StartsWith("j_" + f + "_")) || b == "j_gun" || b.StartsWith("j_mag") || b.StartsWith("j_elbow")).ToList();

                // pose de enlace de la piel de las manos de EFT
                sb.Append("\"bind\": {");
                var names = MiniJsonNames();
                var sk = s.Rig.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.bones.Contains(s.Rig.Bones["Base HumanLPalm"]));
                var parts = new List<string>();
                if (sk != null)
                    for (int i = 0; i < sk.bones.Length; i++)
                        if (sk.bones[i] != null && hand.Contains(sk.bones[i])) parts.Add("\"" + sk.bones[i].name + "\": " + M(sk.sharedMesh.bindposes[i].inverse));
                sb.Append(string.Join(",\n", parts)).Append("},\n");

                // reposo del rig (Build2): padres y locales de EFT
                s.Reset();
                sb.Append("\"eftRest\": {").Append(string.Join(",\n", hand.Select(t => "\"" + t.name + "\": {\"parent\": \"" + t.parent.name + "\", \"local\": " + M(Matrix4x4.TRS(t.localPosition, t.localRotation, Vector3.one)) + ", \"world\": " + M(World(t)) + "}"))).Append("},\n");
                sb.Append("\"eftIdleFinger\": {").Append(string.Join(",\n", s.RestFinger.Select(kv => "\"" + kv.Key.name + "\": " + M(Matrix4x4.Rotate(kv.Value))))).Append("},\n");
                sb.Append("\"codParent\": {").Append(string.Join(",", codBones.Select(b => "\"" + b + "\": \"" + s.Skel.Parent[b] + "\""))).Append("},\n");
                var rest = new Fk(s.Skel, new CodClip(), 0f);
                sb.Append("\"codBind\": {").Append(string.Join(",\n", codBones.Select(b => "\"" + b + "\": " + M(s.Rec0 * Basis(s.P, rest.Rel("j_gun", b)))))).Append("},\n");
                var fki = new Fk(s.Skel, s.Idle, 0f);
                sb.Append("\"codIdle\": {").Append(string.Join(",\n", codBones.Select(b => "\"" + b + "\": " + M(s.Rec0 * Basis(s.P, fki.Rel("j_gun", b)))))).Append("},\n");
                {
                    // caja del cargador en su espacio local
                    var b = new Bounds(); bool first = true; var inv = s.Mag.worldToLocalMatrix;
                    foreach (var mf in s.Mag.GetComponentsInChildren<MeshFilter>())
                    {
                        if (mf.sharedMesh == null) continue;
                        var mb = mf.sharedMesh.bounds; var l2 = inv * mf.transform.localToWorldMatrix;
                        for (int i = 0; i < 8; i++)
                        {
                            var c = l2.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) * 2 - 1, (i >> 1 & 1) * 2 - 1, (i >> 2 & 1) * 2 - 1)));
                            if (first) { b = new Bounds(c, Vector3.zero); first = false; } else b.Encapsulate(c);
                        }
                    }
                    sb.Append("\"magBox\": [" + string.Join(",", new[] { b.min.x, b.min.y, b.min.z, b.max.x, b.max.y, b.max.z }.Select(x => x.ToString("R", CultureInfo.InvariantCulture))) + "],");
                }
                sb.Append("\"weapon\": ").Append(M(World(s.Weapon))).Append(",\n\"frames\": {\n");
                var frames = Env("BO7_FRAMES", "0,14,18,36,40,44,50").Split(',').Select(int.Parse).ToList();
                var fr = new List<string>();
                foreach (int f in frames)
                {
                    s.Pose(clip, f, pins);
                    if (Env("BO7_V3", "0") == "1") Fix3(s, clip, f, pins);
                    var fk = new Fk(s.Skel, clip, f);
                    var rec = s.Rec0 * Basis(s.P, s.G0inv * fk.Rel("tag_view", "j_gun"));
                    fr.Add("\"" + f + "\": {\"pins\": [" + string.Join(",", new[] { pins.IkL, pins.IkR, pins.FgL, pins.FgR }.Select(a => pins.At(a, f).ToString("R", CultureInfo.InvariantCulture))) + "],\n \"eft\": {"
                           + string.Join(",", hand.Select(t => "\"" + t.name + "\": " + M(World(t)))) + "},\n \"cod\": {"
                           + string.Join(",", codBones.Select(b => "\"" + b + "\": " + M(rec * Basis(s.P, fk.Rel("j_gun", b))))) + "}, \"mag\": " + M(World(s.Mag)) + ", \"weapon\": " + M(World(s.Weapon)) + "}");
                }
                sb.Append(string.Join(",\n", fr)).Append("}\n}\n");
                string path = "Preview/handprobe_" + gun + "_" + clipName + (Env("BO7_V3", "0") == "1" ? "_v3" : "") + ".json";
                File.WriteAllText(path, sb.ToString());
                Debug.Log("[BO7A] handprobe -> " + path + " (" + hand.Count + " huesos EFT, " + codBones.Count + " BO7, piel " + (sk != null ? sk.name : "no") + ")");
            }
            catch (System.Exception e) { Debug.LogError("[BO7A] " + e); }
            finally { EftBundles.UnloadAll(); }
        }
        private static string[] MiniJsonNames() => null;

        // PROTOTIPO de traduccion nueva (solo laboratorio): falanges = rotacion de BO7 por una constante (ejes: EFT hueso -x, bisagra +z;
        // BO7 convertido hueso -x, bisagra -y), y palma anclada en un punto entre la muneca (0) y los nudillos (1), no en la muneca.
        private static readonly Quaternion FingerC = Quaternion.AngleAxis(90f, Vector3.right);
        private static Matrix4x4 HandFrameAt(Vector3 wrist, Vector3 index, Vector3 pinky, float u)
        {
            var m = HandFrame(wrist, index, pinky);
            var o = Vector3.Lerp(wrist, (index + pinky) * 0.5f, u);
            m.SetColumn(3, new Vector4(o.x, o.y, o.z, 1f));
            return m;
        }
        internal static void Fix3(Solver2 s, CodClip clip, float f, Pins pins)
        {
            float u = float.Parse(Env("BO7_ANCHOR", "1"), CultureInfo.InvariantCulture);
            bool keepPalm = Env("BO7_KEEPPALM", "0") == "1";
            var fk = new Fk(s.Skel, clip, f);
            var rec = s.Rec0 * Basis(s.P, s.G0inv * fk.Rel("tag_view", "j_gun"));
            System.Func<string, Matrix4x4> cw = b => rec * Basis(s.P, fk.Rel("j_gun", b));
            foreach (var side in new[] { "le", "ri" })
            {
                string sd = side == "le" ? "L" : "R";
                float ik = pins.At(side == "le" ? pins.IkL : pins.IkR, f), fg = pins.At(side == "le" ? pins.FgL : pins.FgR, f);
                var upper = s.Rig.Bones["Base Human" + sd + "Upperarm"]; var fore = s.Rig.Bones["Base Human" + sd + "Forearm1"]; var palm = s.Rig.Bones["Base Human" + sd + "Palm"];
                if (!keepPalm)
                {
                    var fc = HandFrameAt(Pos(cw("j_wrist_" + side)), Pos(cw("j_index_" + side + "_1")), Pos(cw("j_pinky_" + side + "_1")), u);
                    var ae = World(palm).inverse * HandFrameAt(palm.position, s.Rig.Bones["Base Human" + sd + "Digit21"].position, s.Rig.Bones["Base Human" + sd + "Digit51"].position, u);
                    var anim = fc * ae.inverse; var pin = World(s.Weapon) * s.RestPalm[side];
                    var pos = Vector3.Lerp(Pos(anim), Pos(pin), ik); var rot = Quaternion.Slerp(Rot(anim), Rot(pin), ik);
                    var elbow = Pos(cw("j_elbow_" + side)) + s.ElbowOff[fore];
                    TwoBone(upper, fore, palm, pos, elbow);
                    palm.rotation = rot;
                }
                foreach (var (cod, eft) in s.Fingers[side])
                {
                    eft.rotation = Rot(cw(cod)) * FingerC;
                    if (fg > 0f) eft.localRotation = Quaternion.Slerp(eft.localRotation, s.RestFinger[eft], fg);
                }
            }
        }

        public static void Run3()
        {
            try
            {
                string gun = Env("BO7_GUN", "pk");
                var s = Build2(gun);
                foreach (var clipName in Env("BO7_CLIP", "reload").Split(','))
                {
                    var clip = LoadClip(Dir + "sat_vm_" + s.Def.Cod + "_" + clipName + ".json");
                    var pins = Env("BO7_PINS", "1") == "1" ? PinsOf(clip) : Pins.All(clip.Frames);
                    var frames = EftLab.ParseFrames(Env("BO7_FRAMES", "0-end/4"), clip.Frames - 1);
                    if (Env("BO7_PINS", "1") == "none") { var z = new float[Mathf.Max(clip.Frames, 1)]; pins = new Pins { IkL = z, IkR = z, FgL = z, FgR = z }; }
                    bool v3 = Env("BO7_V3", "1") == "1";
                    string eyeSpec = Env("BO7_EYE", "");
                    var real = s.Rig.CamAnim; var eye = new GameObject("ojo").transform;
                    var ghost = new List<GameObject>();
                    System.Action<int> pose = f =>
                    {
                        s.Rig.CamAnim = real;
                        s.Pose(clip, f, pins); if (v3) Fix3(s, clip, f, pins);
                        foreach (var g in ghost) Object.DestroyImmediate(g); ghost.Clear();
                        if (Env("BO7_GHOST", "0") == "1")
                        {
                            // articulaciones de la mano izquierda de BO7 (bolitas rojas) para comparar con la de EFT
                            var fk = new Fk(s.Skel, clip, f);
                            var rec = s.Rec0 * Basis(s.P, s.G0inv * fk.Rel("tag_view", "j_gun"));
                            foreach (var b in s.Skel.Parent.Keys.Where(b => b.EndsWith("_le") || b.Contains("_le_")).Where(b => b.StartsWith("j_wrist") || Fingers.Any(x => b.StartsWith("j_" + x + "_"))))
                            {
                                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.transform.position = Pos(rec * Basis(s.P, fk.Rel("j_gun", b))); g.transform.localScale = Vector3.one * 0.008f;
                                var m = new Material(Shader.Find("Standard")); m.color = Color.red; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.red); g.GetComponent<Renderer>().sharedMaterial = m;
                                ghost.Add(g);
                            }
                        }
                        if (eyeSpec != "")
                        {
                            var o = eyeSpec.Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                            var camRot = real.rotation * Quaternion.Euler(90f, 0f, 0f);
                            var target = (s.Rig.Bones["Base HumanLPalm"].position + s.Rig.Bones["Base HumanLDigit32"].position) * 0.5f;
                            var ep = target + camRot * new Vector3(o[0], o[1], o[2]);
                            eye.SetPositionAndRotation(ep, Quaternion.LookRotation(target - ep, camRot * Vector3.up) * Quaternion.Inverse(Quaternion.Euler(90f, 0f, 0f)));
                            s.Rig.CamAnim = eye;
                        }
                    };
                    EftLab.RenderSheet(s.Rig, pose, frames, Env("BO7_OUT", "Preview/bo7d_" + gun + "_" + clipName + ".png"));
                    s.Rig.CamAnim = real;
                    Debug.Log("[BO7A] v3 " + gun + " " + clipName + ": " + clip.Frames + " fotogramas");
                }
            }
            catch (System.Exception e) { Debug.LogError("[BO7A] " + e); }
            finally { EftBundles.UnloadAll(); }
        }
    }
}
