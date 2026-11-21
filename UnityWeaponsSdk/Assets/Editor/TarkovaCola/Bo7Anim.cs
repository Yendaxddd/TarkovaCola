using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Animaciones de BO7 horneadas sobre el rig de primera persona de EFT (el de la HK416 o la MP5 con las piezas de BO7 montadas).
//   BO7_GUN=vmp|mk35  BO7_CLIP=reload (sat_vm_<arma>_<clip>.json en Assets/TarkovaCola/Anim)  BO7_FRAMES=0-end/5  BO7_OUT=<png>  BO7_BAKE=1 (guarda el .anim)
//   Unity.exe -batchmode -quit -projectPath UnityWeaponsSdk -executeMethod TarkovaCola.Bo7Anim.Run -logFile l.log      (sin -nographics)
// Metodo (todo calibrado con el reposo: en el fotograma de reposo de BO7 el rig queda exactamente en el reposo de EFT):
//  - Arma: el giro y desplazamiento de j_gun respecto a su reposo, en sus propios ejes, se aplica al receptor de BO7 montado en EFT.
//  - Cargador: lo que se mueve j_mag1 respecto al arma se aplica a la ranura mod_magazine.
//  - Manos: la muneca de BO7 respecto al arma marca donde va la palma de EFT respecto al receptor (IK de dos huesos desde el hombro de
//    EFT, con el codo de BO7 como polo); los dedos copian el giro de los de BO7 (respecto al arma).
//  - Camara: el giro de tag_camera respecto a tag_view, en Camera_animated.
//  - Marcadores de IK de EFT (weapon_*_marker, Bend_Goal_*): siguen a su hueso con el desplazamiento del reposo.
namespace TarkovaCola
{
    public static partial class Bo7Anim
    {
        private const string Dir = "Assets/TarkovaCola/Anim/";
        internal static string Env(string k, string d) { var v = System.Environment.GetEnvironmentVariable(k); return string.IsNullOrEmpty(v) ? d : v; }

        // ---------------------------------------------------------------- datos de BO7 (JSON de tools/cast2json.py y tools/bo7_skel.py)
        internal sealed class Curve { public int[] Frames; public float[][] Values; }
        internal sealed class CodClip
        {
            public int Frames;
            public Dictionary<string, Curve> Rq = new Dictionary<string, Curve>(), Tx = new Dictionary<string, Curve>(), Ty = new Dictionary<string, Curve>(), Tz = new Dictionary<string, Curve>();
            public Dictionary<string, int[]> Notes = new Dictionary<string, int[]>();
        }
        internal sealed class Skel { public Dictionary<string, string> Parent = new Dictionary<string, string>(); public Dictionary<string, Vector3> Lp = new Dictionary<string, Vector3>(); public Dictionary<string, Quaternion> Lr = new Dictionary<string, Quaternion>(); }

        internal static float F(object o) => System.Convert.ToSingle(o, System.Globalization.CultureInfo.InvariantCulture);

        internal static CodClip LoadClip(string path)
        {
            var j = (Dictionary<string, object>)Json.Parse(File.ReadAllText(path));
            var c = new CodClip { Frames = System.Convert.ToInt32(j["frames"]) };
            foreach (Dictionary<string, object> cv in (List<object>)j["curves"])
            {
                var k = new Curve
                {
                    Frames = ((List<object>)cv["frames"]).Select(System.Convert.ToInt32).ToArray(),
                    Values = ((List<object>)cv["values"]).Select(v => v is List<object> l ? l.Select(F).ToArray() : new[] { F(v) }).ToArray(),
                };
                string bone = (string)cv["bone"];
                switch ((string)cv["prop"]) { case "rq": c.Rq[bone] = k; break; case "tx": c.Tx[bone] = k; break; case "ty": c.Ty[bone] = k; break; case "tz": c.Tz[bone] = k; break; }
            }
            foreach (Dictionary<string, object> n in (List<object>)j["notes"]) c.Notes[(string)n["name"]] = ((List<object>)n["frames"]).Select(System.Convert.ToInt32).ToArray();
            return c;
        }

        internal static Skel LoadSkel(string path)
        {
            var s = new Skel();
            foreach (var kv in (Dictionary<string, object>)Json.Parse(File.ReadAllText(path)))
            {
                var b = (Dictionary<string, object>)kv.Value;
                var p = ((List<object>)b["lp"]).Select(F).ToArray(); var r = ((List<object>)b["lr"]).Select(F).ToArray();
                s.Parent[kv.Key] = b["parent"] as string; s.Lp[kv.Key] = new Vector3(p[0], p[1], p[2]); s.Lr[kv.Key] = new Quaternion(r[0], r[1], r[2], r[3]);
            }
            return s;
        }

        internal static float[] Sample(Curve c, float f)
        {
            int i = 0;
            while (i + 1 < c.Frames.Length && c.Frames[i + 1] <= f) i++;
            if (i + 1 >= c.Frames.Length || f <= c.Frames[0]) return c.Values[Mathf.Min(i, c.Values.Length - 1)];
            float t = (f - c.Frames[i]) / (c.Frames[i + 1] - c.Frames[i]);
            var a = c.Values[i]; var b = c.Values[i + 1];
            if (a.Length == 4) { var q = Quaternion.Slerp(new Quaternion(a[0], a[1], a[2], a[3]), new Quaternion(b[0], b[1], b[2], b[3]), t); return new[] { q.x, q.y, q.z, q.w }; }
            return new[] { Mathf.Lerp(a[0], b[0], t) };
        }

        // cinematica directa en el espacio de BO7 (cm, X adelante, Y izquierda, Z arriba); traslaciones "relative" sobre el reposo
        internal sealed class Fk
        {
            internal readonly Skel _s; private readonly CodClip _c; private readonly float _f;
            internal readonly Dictionary<string, Matrix4x4> _w = new Dictionary<string, Matrix4x4>();
            public Fk(Skel s, CodClip c, float f) { _s = s; _c = c; _f = f; }
            public Matrix4x4 World(string b)
            {
                Matrix4x4 m;
                if (_w.TryGetValue(b, out m)) return m;
                Curve cv;
                var lr = _c.Rq.TryGetValue(b, out cv) ? Q(Sample(cv, _f)) : _s.Lr[b];
                var lp = _s.Lp[b];
                if (_c.Tx.ContainsKey(b)) lp += new Vector3(Sample(_c.Tx[b], _f)[0], Sample(_c.Ty[b], _f)[0], Sample(_c.Tz[b], _f)[0]);
                m = Matrix4x4.TRS(lp, Norm(lr), Vector3.one);
                string par = _s.Parent[b];
                if (par != null) m = World(par) * m;
                _w[b] = m; return m;
            }
            // b respecto a 'space'
            public Matrix4x4 Rel(string space, string b) => World(space).inverse * World(b);
        }
        internal static Quaternion Q(float[] v) => new Quaternion(v[0], v[1], v[2], v[3]);
        internal static Quaternion Norm(Quaternion q) { float n = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w); return n < 1e-6f ? Quaternion.identity : new Quaternion(q.x / n, q.y / n, q.z / n, q.w / n); }

        // cambio de base (con espejo: BO7 es de mano derecha, Unity de mano izquierda): T' = P T P^-1
        internal static Matrix4x4 Basis(Matrix4x4 p, Matrix4x4 t) => p * t * p.inverse;
        // BO7 -> espacio del FBX en Unity (canon -X, arriba +Y, derecha +Z), escalado a las unidades del FBX
        internal static Matrix4x4 FbxBasis(float k)
        {
            var m = Matrix4x4.zero;
            m[0, 0] = -k; m[1, 2] = k; m[2, 1] = -k; m[3, 3] = 1f;
            return m;
        }
        // BO7 -> ejes de camara de Unity (x derecha, y arriba, z adelante)
        internal static Matrix4x4 CamBasis()
        {
            var m = Matrix4x4.zero;
            m[0, 1] = -1f; m[1, 2] = 1f; m[2, 0] = 1f; m[3, 3] = 1f;
            return m;
        }
        internal static Vector3 Pos(Matrix4x4 m) => m.GetColumn(3);
        internal static Quaternion Rot(Matrix4x4 m) => Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
        internal static Matrix4x4 World(Transform t) => Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);

        // ---------------------------------------------------------------- EFT
        internal static readonly string[] Fingers = { "thumb", "index", "mid", "ring", "pinky" };
        internal static IEnumerable<(string cod, string eft)> HandMap(string side)
        {
            string s = side == "le" ? "L" : "R";
            yield return ("j_wrist_" + side, "Base Human" + s + "Palm");
            for (int f = 0; f < 5; f++) for (int j = 1; j <= 3; j++) yield return ("j_" + Fingers[f] + "_" + side + "_" + j, "Base Human" + s + "Digit" + (f + 1) + j);
        }

        // marcador de EFT -> hueso al que sigue. Los weapon_*_hand_marker NO: son el punto de agarre del arma; Player.IkProcess pega la mano
        // al guardamanos/empunadura mientras el *_IK_marker (donde va la mano) este a menos de 10 cm de el (asi se suelta en las recargas)
        internal static readonly Dictionary<string, string> Markers = new Dictionary<string, string>
        {
            ["weapon_L_IK_marker"] = "Base HumanLPalm", ["weapon_R_IK_marker"] = "Base HumanRPalm",
            ["Bend_Goal_Left"] = "Base HumanLForearm1", ["Bend_Goal_Right"] = "Base HumanRForearm1",
            ["weapon_LCollarbone_marker"] = "Base HumanLCollarbone", ["weapon_RCollarbone_marker"] = "Base HumanRCollarbone",
        };

        internal sealed class Solver
        {
            public EftLab.Rig Rig; public Skel Skel; public CodClip Idle, Anim;
            public Transform Rec, Weapon, Mag;
            public Matrix4x4 P, PC;
            public Dictionary<Transform, (Vector3 p, Quaternion r)> Base = new Dictionary<Transform, (Vector3, Quaternion)>();
            // calibracion en el reposo
            public Matrix4x4 Rec0, WeaponInRec, MagSlot0;
            public Matrix4x4 G0inv, Mag0;                                         // BO7
            public Dictionary<Transform, Matrix4x4> Off = new Dictionary<Transform, Matrix4x4>();
            public Dictionary<Transform, Vector3> ElbowOff = new Dictionary<Transform, Vector3>();
            public Dictionary<Transform, Matrix4x4> MarkerOff = new Dictionary<Transform, Matrix4x4>();
            public Quaternion Cam0, CodCam0Inv;
            public List<(string cod, Transform eft)> Hands = new List<(string, Transform)>();
            public string MagBone = "j_mag1";

            public void Reset() { foreach (var kv in Base) { kv.Key.localPosition = kv.Value.p; kv.Key.localRotation = kv.Value.r; } }

            // fotograma f de 'clip' en el rig
            public void Pose(CodClip clip, float f)
            {
                Reset();
                var fk = new Fk(Skel, clip, f);
                var g = fk.Rel("tag_view", "j_gun");
                var rec = Rec0 * Basis(P, G0inv * g);
                var w = rec * WeaponInRec;
                Weapon.SetPositionAndRotation(Pos(w), Rot(w));
                if (Mag != null)
                {
                    var m = rec * Basis(P, fk.Rel("j_gun", MagBone)) * Basis(P, Mag0).inverse * Rec0.inverse * MagSlot0;
                    Mag.SetPositionAndRotation(Pos(m), Rot(m));
                }
                // camara
                var cam = CodCam0Inv * Rot(Basis(PC, fk.Rel("tag_view", "tag_camera")));
                Rig.CamAnim.rotation = Cam0 * cam * Quaternion.Inverse(Quaternion.Euler(90f, 0f, 0f));
                // brazos: IK hacia la palma y luego giros de palma y dedos
                foreach (var side in new[] { "le", "ri" })
                {
                    string s = side == "le" ? "L" : "R";
                    var upper = Rig.Bones["Base Human" + s + "Upperarm"]; var fore = Rig.Bones["Base Human" + s + "Forearm1"]; var palm = Rig.Bones["Base Human" + s + "Palm"];
                    var pt = rec * Basis(P, fk.Rel("j_gun", "j_wrist_" + side)) * Off[palm];
                    var elbow = Pos(rec * Basis(P, fk.Rel("j_gun", "j_elbow_" + side))) + ElbowOff[fore];
                    TwoBone(upper, fore, palm, Pos(pt), elbow);
                }
                foreach (var (cod, eft) in Hands) eft.rotation = Rot(rec * Basis(P, fk.Rel("j_gun", cod)) * Off[eft]);
                foreach (var kv in MarkerOff) { var m = World(Rig.Bones[Markers[kv.Key.name]]) * kv.Value; kv.Key.SetPositionAndRotation(Pos(m), Rot(m)); }
            }
        }

        // ---------------------------------------------------------------- horneado
        // Un tramo de una animacion de BO7 para un estado de EFT: 'anchors' son pares (tiempo normalizado del estado, fotograma de BO7);
        // entre anclas el tiempo es lineal. Asi los eventos de EFT (StaticData, en tiempo normalizado) caen donde toca en BO7.
        internal sealed class Piece
        {
            public string Eft, Clip; public (float t, float f)[] Anchors; public MagPlan Mag;
            public string Parts;   // clip del que salen las partes moviles (si no es Clip)
            public float Speed = 1f; public string State, MagBone; public (string ev, float f)[] Ev; public bool Silent;   // v2: velocidad del estado, estado y eventos de EFT (fotograma de BO7)
            public float FrameAt(float t)
            {
                for (int i = 0; i + 1 < Anchors.Length; i++)
                    if (t <= Anchors[i + 1].t) return Mathf.Lerp(Anchors[i].f, Anchors[i + 1].f, Mathf.InverseLerp(Anchors[i].t, Anchors[i + 1].t, t));
                return Anchors[Anchors.Length - 1].f;
            }
            public float Frames;   // duracion en fotogramas de BO7 (0 = la suma de los tramos)
            public float Seconds { get { float d = Frames; if (d <= 0f) for (int i = 0; i + 1 < Anchors.Length; i++) d += Mathf.Abs(Anchors[i + 1].f - Anchors[i].f); return Mathf.Max(d, 1f) / 30f; } }
            // tiempo normalizado en el que suena el fotograma f de BO7 (inverso de FrameAt); -1 si cae fuera del tramo
            public float TimeOf(float f)
            {
                for (int i = 0; i + 1 < Anchors.Length; i++)
                    if (f >= Anchors[i].f && f < Anchors[i + 1].f) return Mathf.Lerp(Anchors[i].t, Anchors[i + 1].t, Mathf.InverseLerp(Anchors[i].f, Anchors[i + 1].f, f));
                return -1f;
            }
        }

        // El cargador: BO7 lo esconde y lo cambia con trucos (j_mag1 hace de cargador viejo y de nuevo, y "vuela" lejos cuando no se ve).
        //  Literal: tal cual | Seat: siempre en el arma | SeatAfter: tal cual hasta 'From', despues en el arma |
        //  Held: pegado a la mano izquierda desde 'From' y metido en el arma entre 'InA' e 'InB'
        internal enum MagMode { Literal, Seat, SeatAfter, Held }
        internal sealed class MagPlan { public MagMode Mode; public float From, InA, InB; }

        // lo que se graba: brazos, camara, raiz del arma, el arma y sus hijos directos (ranuras y marcadores)
        internal static List<Transform> Recorded(EftLab.Rig rig, Transform weapon)
        {
            var res = rig.Root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Base Human") || t.name == "Camera_animated" || t.name == "Weapon_root" || t.name == "Weapon_root_anim").ToList();
            res.Add(weapon);
            foreach (Transform c in weapon) res.Add(c);
            return res;
        }

        internal static AnimationClip Bake(Solver s, CodClip clip, Piece piece, List<Transform> rec)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt(piece.Seconds * 30f) + 1);
            var curves = rec.ToDictionary(t => t, t => new AnimationCurve[7].Select(_ => new AnimationCurve()).ToArray());
            var palm = s.Rig.Bones["Base HumanLPalm"];
            var mag = s.Mag != null ? piece.Mag : null;
            Matrix4x4 held = Matrix4x4.identity;
            if (mag != null && mag.Mode == MagMode.Held) { s.Pose(clip, mag.From); held = World(palm).inverse * World(s.Mag); }
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1), f = piece.FrameAt(t), time = t * piece.Seconds;
                s.Pose(clip, f);
                if (mag != null && mag.Mode != MagMode.Literal)
                {
                    var seat = World(s.Weapon) * (s.Rec0 * s.WeaponInRec).inverse * s.MagSlot0;
                    if (mag.Mode == MagMode.Seat || (mag.Mode == MagMode.SeatAfter && f >= mag.From)) s.Mag.SetPositionAndRotation(Pos(seat), Rot(seat));
                    else if (mag.Mode == MagMode.Held && f > mag.From)
                    {
                        var h = World(palm) * held;
                        float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(mag.InA, mag.InB, f));
                        s.Mag.SetPositionAndRotation(Vector3.Lerp(Pos(h), Pos(seat), w), Quaternion.Slerp(Rot(h), Rot(seat), w));
                    }
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
            // sonidos de BO7 (notas "AudioOneShot@alias"): eventos de Unity que recibe Bo7SoundRelay en el cliente (Bo7Sounds.cs)
            var events = new List<AnimationEvent>();
            foreach (var note in clip.Notes.Where(n => n.Key.StartsWith("AudioOneShot@")))
                foreach (int f in note.Value)
                {
                    float t = piece.TimeOf(f);
                    if (t >= 0f) events.Add(new AnimationEvent { functionName = "Bo7Snd", stringParameter = note.Key.Substring(13), time = t * piece.Seconds });
                }
            AnimationUtility.SetAnimationEvents(res, events.OrderBy(e => e.time).ToArray());
            return res;
        }

        // ---------------------------------------------------------------- que clip de BO7 va en cada clip de EFT
        // El nombre del clip horneado es <arma>_<clip de EFT sin el _N final>: el cliente lo pone en ese clip y en sus variantes _0.._6
        // (los arboles de mezcla de la recarga). Anclas (t, f): t tiempo normalizado del estado de EFT, f fotograma de BO7 (-1 = el ultimo);
        // en t caen los eventos de EFT (StaticData): asi suenan y pasan las cosas cuando se ven.
        // RELOAD START/END y RELOAD START EMPTY se quedan con los clips de EFT (los comparten los gestos y las acciones de la mano izquierda).
        internal static readonly MagPlan Seat = new MagPlan { Mode = MagMode.Seat };
        internal static readonly Dictionary<string, Piece[]> Jobs = new Dictionary<string, Piece[]>
        {
            // VMP sobre la MP5. Recarga: saca el cargador (12-34), la mano sube a la boca (36-44) y BO7 se inventa el cargador en el 58.
            // MP5: mag_out MagOut 0,462 MagHide 0,85 | mag_in MagShow 0,2 MagIn 0,7 | reload_charge OffBoltCatch 0,415 AddAmmo 0,539
            ["vmp"] = new[]
            {
                new Piece { Eft = "mp5_mag_out", Clip = "reload", Anchors = new[] { (0f, 2f), (0.462f, 20f), (0.85f, 31f), (1f, 34f) } },
                new Piece { Eft = "mp5_mag_in", Clip = "reload", Anchors = new[] { (0f, 34f), (0.2f, 35f), (0.7f, 44f), (1f, 48f) }, Mag = new MagPlan { Mode = MagMode.Held, From = 34f, InA = 37f, InB = 44f } },
                new Piece { Eft = "mp5_reload_charge", Clip = "reload_empty", Anchors = new[] { (0f, 44f), (0.415f, 54f), (0.539f, 57f), (1f, 75f) }, Mag = Seat },
                new Piece { Eft = "mp5_look", Clip = "inspect", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "mp5_out_to_idle", Clip = "raise", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "mp5_out_to_idle_arm", Clip = "raise_first", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "mp5_idle_to_out", Clip = "drop", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                // selector (EFireMode: 0 automatico, 1 tiro a tiro, 3 rafaga): a tiro a tiro BO7 "selectsemi_on", a lo demas "selectsemi_off"
                new Piece { Eft = "mp5_firemode_3_to_0", Clip = "selectsemi_off", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "mp5_firemode_0_to_1", Clip = "selectsemi_on", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "mp5_firemode_1_to_3", Clip = "selectsemi_off", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
            },
            // MK35 sobre la HK416 (clips de la M4A1). Recarga: coge un cargador nuevo, con el saca el viejo (sale en el 19), mete el nuevo
            // (asentado en el 40) y tira el viejo (BO7 lo hace con j_mag1 desde el 45). Vacio: suelta el cerrojo hacia el 61.
            // M4A1: mag_out MagOut 0,462 MagHide 0,85 | mag_in MagShow 0,049 MagIn 0,7 | reload_charge OffBoltCatch 0,205 AddAmmo 0,539
            ["mk35"] = new[]
            {
                new Piece { Eft = "m4a1_mag_out", Clip = "reload", Anchors = new[] { (0f, 0f), (0.462f, 19f), (1f, 25f) } },
                new Piece { Eft = "m4a1_mag_in", Clip = "reload", Anchors = new[] { (0f, 25f), (0.049f, 26f), (0.7f, 40f), (1f, 46f) }, Mag = new MagPlan { Mode = MagMode.SeatAfter, From = 41f } },
                new Piece { Eft = "m4a1_reload_charge", Clip = "reload_empty", Anchors = new[] { (0f, 55f), (0.205f, 61f), (0.539f, 64f), (1f, 75f) }, Mag = Seat },
                new Piece { Eft = "m4a1_look", Clip = "inspect", Anchors = new[] { (0f, 0f), (1f, -1f) } },
                new Piece { Eft = "m4a1_out_to_idle", Clip = "raise", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "m4a1_out_to_idle_arm", Clip = "raise_first", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "m4a1_idle_to_out", Clip = "drop", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "m4a1_firemode_0", Clip = "selectsemi_off", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
                new Piece { Eft = "m4a1_firemode_1", Clip = "selectsemi_on", Anchors = new[] { (0f, 0f), (1f, -1f) }, Mag = Seat },
            },
        };
        private const string BundleName = "bo7_anims.bundle", SoundBundle = "bo7_sounds.bundle", SoundDir = "Assets/TarkovaCola/Sounds";

        //   BO7_GUN=vmp|mk35  BO7_SHEET=mp5_mag_out,mp5_mag_in (clips de la hoja de comprobacion; vacio = ninguno)  BO7_STEP=4
        public static void BakeRun()
        {
            try
            {
                string gun = Env("BO7_GUN", "vmp"), cod = gun == "mk35" ? "ar_finch" : "sm_civet";
                var s = Build(gun);
                var rec = Recorded(s.Rig, s.Weapon);
                if (Directory.Exists(Dir + "Baked")) foreach (var old in Directory.GetFiles(Dir + "Baked", gun + "_*.anim")) AssetDatabase.DeleteAsset(old.Replace('\\', '/'));
                Directory.CreateDirectory(Dir + "Baked");
                var clips = new Dictionary<string, CodClip>();
                var baked = new Dictionary<string, AnimationClip>();
                foreach (var piece in Jobs[gun])
                {
                    if (!clips.ContainsKey(piece.Clip)) clips[piece.Clip] = LoadClip(Dir + "sat_vm_" + cod + "_" + piece.Clip + ".json");
                    var cc = clips[piece.Clip];
                    piece.Anchors = piece.Anchors.Select(a => (a.t, a.f < 0f ? cc.Frames - 1f : a.f)).ToArray();
                    var c = Bake(s, cc, piece, rec);
                    c.name = gun + "_" + piece.Eft;
                    string path = Dir + "Baked/" + c.name + ".anim";
                    AssetDatabase.CreateAsset(c, path);
                    baked[piece.Eft] = c;
                    Debug.Log("[BO7A] horneado " + c.name + " <- " + piece.Clip + " " + string.Join(" ", piece.Anchors.Select(a => a.t.ToString("0.###") + ":" + a.f)) + " = " + c.length.ToString("F2") + " s");
                }
                AssetDatabase.SaveAssets();
                // bundle para el cliente (Client/assets): todos los .anim horneados (de las dos armas)
                var all = Directory.GetFiles(Dir + "Baked", "*.anim").Select(p => p.Replace('\\', '/')).ToArray();
                Directory.CreateDirectory("Build/anims");
                // y el de los sonidos (tools/bo7_sounds.py los deja en Assets/TarkovaCola/Sounds con su indice bo7_sounds.json)
                var sounds = Directory.Exists(SoundDir) ? Directory.GetFiles(SoundDir).Where(p => p.EndsWith(".wav") || p.EndsWith(".json")).Select(p => p.Replace('\\', '/')).ToArray() : new string[0];
                var builds = new List<AssetBundleBuild> { new AssetBundleBuild { assetBundleName = BundleName, assetNames = all } };
                if (sounds.Length > 0) builds.Add(new AssetBundleBuild { assetBundleName = SoundBundle, assetNames = sounds });
                BuildPipeline.BuildAssetBundles("Build/anims", builds.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
                foreach (var b in builds) File.Copy("Build/anims/" + b.assetBundleName, "../Client/assets/" + b.assetBundleName, true);
                Debug.Log("[BO7A] " + BundleName + ": " + all.Length + " clips, " + SoundBundle + ": " + sounds.Length + " archivos -> Client/assets");
                // hoja de comprobacion: los clips horneados reproducidos sobre el rig (lo que vera el juego)
                var sheet = Env("BO7_SHEET", "").Split(',').Where(baked.ContainsKey).ToList();
                if (sheet.Count == 0) return;
                float step = float.Parse(Env("BO7_STEP", "4")) / 30f;
                var seq = new List<(AnimationClip c, float t)>();
                foreach (var name in sheet) for (float t = 0f; t <= baked[name].length + 1e-4f; t += step) seq.Add((baked[name], t));
                if (Env("BO7_MAGRED", "0") == "1" && s.Mag != null)
                {
                    var red = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
                    foreach (var r in s.Mag.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => red).ToArray();
                }
                EftLab.RenderSheet(s.Rig, i => { s.Reset(); seq[i].c.SampleAnimation(s.Rig.Root, seq[i].t); }, Enumerable.Range(0, seq.Count).ToList(), Env("BO7_OUT", "Preview/bo7b_" + gun + ".png"));
            }
            catch (System.Exception e) { Debug.LogError("[BO7A] " + e); }
            finally { EftBundles.UnloadAll(); }
        }

        internal static Solver Build(string gun)
        {
            string cod = gun == "mk35" ? "ar_finch" : "sm_civet";
            var rig = EftLab.LoadRig(gun == "mk35" ? "assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle" : "assets/content/weapons/mp5/weapon_hk_mp5_navy3_9x19_container.bundle");
            var weapon = rig.Bones["weapon"];
            BuildBo7Guns.HideBase(weapon);
            BuildBo7Guns.Assemble(gun, weapon, false);
            var idleClip = BuildBo7Guns.Idle(rig);
            Debug.Log("[BO7A] reposo de EFT: " + (idleClip != null ? idleClip.name : "ninguno"));
            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) if (!rig.Bones.ContainsKey(t.name)) rig.Bones[t.name] = t;
            var s = new Solver { Rig = rig, Skel = LoadSkel(Dir + "bo7_" + cod + "_skel.json"), Idle = LoadClip(Dir + "sat_vm_" + cod + "_idle.json"), Weapon = weapon, PC = CamBasis() };
            s.Rec = weapon.GetComponentsInChildren<Transform>(true).First(t => t.name == "sat_vm_" + cod + "_rec");
            s.Mag = weapon.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "mod_magazine");

            // escala del FBX: distancia entre dos tags en el FBX (unidades de Unity) / en el esqueleto de BO7 (cm)
            var part = BuildBo7Guns.Part("sat_vm_" + cod + "_rec");
            var rest = new Fk(s.Skel, new CodClip(), 0f);
            System.Func<string, Vector3> codTag = n => Pos(rest.Rel("j_gun", n));
            float k = (part.Tag("tag_mag_attach") - part.Tag("tag_stock_attach")).magnitude / (codTag("tag_mag_attach") - codTag("tag_stock_attach")).magnitude;
            s.P = FbxBasis(k);
            Debug.Log("[BO7A] escala FBX " + k.ToString("F4") + "; tag_mag_attach FBX " + part.Tag("tag_mag_attach").ToString("F3") + " vs BO7 convertido " + Pos(s.P * Matrix4x4.Translate(codTag("tag_mag_attach"))).ToString("F3"));

            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) s.Base[t] = (t.localPosition, t.localRotation);
            // calibracion
            var fk0 = new Fk(s.Skel, s.Idle, 0f);
            s.Rec0 = World(s.Rec); s.WeaponInRec = s.Rec0.inverse * World(weapon);
            s.G0inv = fk0.Rel("tag_view", "j_gun").inverse;
            if (s.Mag != null) { s.MagSlot0 = World(s.Mag); s.Mag0 = fk0.Rel("j_gun", s.MagBone); }
            foreach (var side in new[] { "le", "ri" })
            {
                string sd = side == "le" ? "L" : "R";
                foreach (var (c, e) in HandMap(side))
                {
                    if (!rig.Bones.ContainsKey(e) || !s.Skel.Parent.ContainsKey(c)) { Debug.LogWarning("[BO7A] sin " + c + " / " + e); continue; }
                    var t = rig.Bones[e];
                    s.Off[t] = (s.Rec0 * Basis(s.P, fk0.Rel("j_gun", c))).inverse * World(t);
                    s.Hands.Add((c, t));                                                 // la palma primero: los dedos cuelgan de ella
                }
                var fore = rig.Bones["Base Human" + sd + "Forearm1"];
                s.ElbowOff[fore] = fore.position - Pos(s.Rec0 * Basis(s.P, fk0.Rel("j_gun", "j_elbow_" + side)));
            }
            foreach (var kv in Markers)
            {
                Transform m, b;
                if (rig.Bones.TryGetValue(kv.Key, out m) && rig.Bones.TryGetValue(kv.Value, out b)) s.MarkerOff[m] = World(b).inverse * World(m);
            }
            s.Cam0 = rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f);
            s.CodCam0Inv = Quaternion.Inverse(Rot(Basis(s.PC, fk0.Rel("tag_view", "tag_camera"))));
            return s;
        }

        // IK de dos huesos (como EftCod): giros FromTo sobre las posiciones, sin tocar el resto
        internal static void TwoBone(Transform upper, Transform fore, Transform end, Vector3 target, Vector3 pole)
        {
            Vector3 s = upper.position;
            float a = Vector3.Distance(upper.position, fore.position), b = Vector3.Distance(fore.position, end.position);
            Vector3 d = target - s; float len = Mathf.Clamp(d.magnitude, 0.01f, a + b - 0.001f); d = d.normalized * len;
            float cosA = Mathf.Clamp((a * a + len * len - b * b) / (2f * a * len), -1f, 1f);
            Vector3 n = Vector3.ProjectOnPlane(pole - s, d).normalized;
            if (n.sqrMagnitude < 1e-6f) n = Vector3.down;
            Vector3 elbow = s + d.normalized * (a * cosA) + n * (a * Mathf.Sqrt(1f - cosA * cosA));
            upper.rotation = Quaternion.FromToRotation(fore.position - s, elbow - s) * upper.rotation;
            fore.rotation = Quaternion.FromToRotation(end.position - fore.position, s + d - fore.position) * fore.rotation;
        }

        public static void Run()
        {
            try
            {
                string gun = Env("BO7_GUN", "vmp"), cod = gun == "mk35" ? "ar_finch" : "sm_civet", clipName = Env("BO7_CLIP", "reload");
                if (Env("BO7_DIAG", "0") == "1") { Diag(gun); return; }
                var s = Build(gun);
                var clip = LoadClip(Dir + "sat_vm_" + cod + "_" + clipName + ".json");
                // comprobacion: el reposo de BO7 debe dejar el rig en el reposo de EFT
                var palm = s.Rig.Bones["Base HumanLPalm"]; var p0 = palm.position;
                s.Pose(s.Idle, 0f);
                Debug.Log("[BO7A] error del reposo (palma izq.): " + ((palm.position - p0).magnitude * 1000f).ToString("F1") + " mm");
                // comprobacion: direccion del canon y posicion de la muneca izquierda respecto a la camara, en BO7 y en EFT (ejes x der, y arriba, z adelante)
                foreach (int f in new[] { 0, 15, 30, 45, 60 })
                {
                    s.Pose(clip, f);
                    var fk = new Fk(s.Skel, clip, f);
                    var g = fk.Rel("tag_view", "j_gun");
                    var codDir = (Vector3)(s.PC * g.GetColumn(0));
                    var codUp = (Vector3)(s.PC * g.GetColumn(2));
                    var codWr = Pos(s.PC * fk.Rel("tag_view", "j_wrist_le")) / 100f;
                    var cr = Quaternion.Inverse(s.Rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f));
                    var eftDir = cr * (s.Weapon.rotation * Vector3.down);
                    var eftUp = cr * (s.Weapon.rotation * Vector3.forward);
                    var eftWr = cr * (s.Rig.Bones["Base HumanLPalm"].position - s.Rig.CamAnim.position);
                    Debug.Log("[BO7A] f" + f + " canon BO7 " + codDir.normalized.ToString("F2") + " EFT " + eftDir.ToString("F2") + " | arriba BO7 " + codUp.normalized.ToString("F2") + " EFT " + eftUp.ToString("F2") + " | muneca izq BO7 " + codWr.ToString("F2") + " EFT " + eftWr.ToString("F2"));
                }
                if (Env("BO7_MAGRED", "0") == "1" && s.Mag != null)
                {
                    var red = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
                    foreach (var r in s.Mag.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => red).ToArray();
                }
                var frames = EftLab.ParseFrames(Env("BO7_FRAMES", "0-end/6"), clip.Frames - 1);
                EftLab.RenderSheet(s.Rig, f => s.Pose(clip, f), frames, Env("BO7_OUT", "Preview/bo7a_" + gun + "_" + clipName + ".png"));
                Debug.Log("[BO7A] " + gun + " " + clipName + ": " + clip.Frames + " fotogramas; notas " + string.Join(", ", clip.Notes.Select(n => n.Key + "@" + n.Value[0])));
            }
            catch (System.Exception e) { Debug.LogError("[BO7A] " + e); }
            finally { EftBundles.UnloadAll(); }
        }

        // Diagnostico: en los clips de EFT, cuanto se mueven los marcadores respecto al hueso que se supone que siguen
        internal static void Diag(string gun)
        {
            var rig = EftLab.LoadRig(gun == "mk35" ? "assets/content/weapons/hk416/weapon_hk_416a5_556x45_container.bundle" : "assets/content/weapons/mp5/weapon_hk_mp5_navy3_9x19_container.bundle");
            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) if (!rig.Bones.ContainsKey(t.name)) rig.Bones[t.name] = t;
            foreach (var name in Env("BO7_DIAG_CLIPS", "mp5_mag_out_0,mp5_mag_in_0,mp5_look").Split(','))
            {
                var clip = rig.Clips.FirstOrDefault(c => c.name == name);
                if (clip == null) { Debug.LogWarning("[BO7A] no hay " + name); continue; }
                foreach (var kv in Markers)
                {
                    Transform m, b;
                    if (!rig.Bones.TryGetValue(kv.Key, out m) || !rig.Bones.TryGetValue(kv.Value, out b)) continue;
                    float maxP = 0f, maxR = 0f; Matrix4x4 r0 = Matrix4x4.identity; float maxW = 0f; Vector3 w0 = Vector3.zero;
                    for (int f = 0; f <= Mathf.RoundToInt(clip.length * 30f); f += 2)
                    {
                        clip.SampleAnimation(rig.Root, f / 30f);
                        var rel = World(b).inverse * World(m);
                        var wl = rig.Bones["weapon"].InverseTransformPoint(m.position);
                        if (f == 0) { r0 = rel; w0 = wl; continue; }
                        maxP = Mathf.Max(maxP, (Pos(rel) - Pos(r0)).magnitude); maxR = Mathf.Max(maxR, Quaternion.Angle(Rot(rel), Rot(r0)));
                        maxW = Mathf.Max(maxW, (wl - w0).magnitude);
                    }
                    Debug.Log("[BO7A] " + name + " " + kv.Key + ": respecto a " + kv.Value + " se mueve " + (maxP * 1000f).ToString("F0") + " mm / " + maxR.ToString("F0") + " grados; respecto al arma " + (maxW * 1000f).ToString("F0") + " mm");
                }
            }
        }

        // Lector JSON minimo (Unity no trae uno con diccionarios)
        internal static class Json
        {
            public static object Parse(string s) { int i = 0; return Val(s, ref i); }
            private static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
            private static object Val(string s, ref int i)
            {
                Ws(s, ref i);
                char c = s[i];
                if (c == '{')
                {
                    var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                    if (s[i] == '}') { i++; return d; }
                    while (true)
                    {
                        Ws(s, ref i); var k = (string)Val(s, ref i); Ws(s, ref i); i++;
                        d[k] = Val(s, ref i); Ws(s, ref i);
                        if (s[i] == ',') { i++; continue; }
                        i++; return d;
                    }
                }
                if (c == '[')
                {
                    var l = new List<object>(); i++; Ws(s, ref i);
                    if (s[i] == ']') { i++; return l; }
                    while (true)
                    {
                        l.Add(Val(s, ref i)); Ws(s, ref i);
                        if (s[i] == ',') { i++; continue; }
                        i++; return l;
                    }
                }
                if (c == '"') { int e = i + 1; var sb = new System.Text.StringBuilder(); while (s[e] != '"') { if (s[e] == '\\') e++; sb.Append(s[e]); e++; } i = e + 1; return sb.ToString(); }
                if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
                if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
                if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
                int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                return double.Parse(s.Substring(st, i - st), System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
