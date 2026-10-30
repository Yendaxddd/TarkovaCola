using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TarkovaCola
{
    // Lleva una animacion de CoD (Cast -> JSON, brazo izquierdo) al esqueleto de primera persona de EFT.
    // Metodo "delta": cada hueso de EFT parte de una pose base de EFT (un fotograma de un clip vanilla) y recibe el giro que hace
    // su hueso de CoD desde el fotograma 0 de la animacion de CoD, convertido de ejes (CoD y EFT orientan los huesos distinto).
    // La conversion (espejo de un eje + giro de 180) se elige sola: la que mejor lleva la mano a la boca en los fotogramas de beber.
    //   COD_ANIM=Assets/Anim/t10_vm_gesture_zm_perk_can_drink.json  LAB_FRAMES=0-100/5  LAB_OUT=<png>  [COD_COMBO=xY]
    //   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.EftCod.Run
    public static class EftCod
    {
        private static string Env(string k, string d) { var v = System.Environment.GetEnvironmentVariable(k); return string.IsNullOrEmpty(v) ? d : v; }

        internal static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            ["j_shoulder_le"] = "Base HumanLUpperarm", ["j_elbow_le"] = "Base HumanLForearm1", ["j_wrist_le"] = "Base HumanLPalm",
            ["j_thumb_le_1"] = "Base HumanLDigit11", ["j_thumb_le_2"] = "Base HumanLDigit12", ["j_thumb_le_3"] = "Base HumanLDigit13",
            ["j_index_le_1"] = "Base HumanLDigit21", ["j_index_le_2"] = "Base HumanLDigit22", ["j_index_le_3"] = "Base HumanLDigit23",
            ["j_mid_le_1"] = "Base HumanLDigit31", ["j_mid_le_2"] = "Base HumanLDigit32", ["j_mid_le_3"] = "Base HumanLDigit33",
            ["j_ring_le_1"] = "Base HumanLDigit41", ["j_ring_le_2"] = "Base HumanLDigit42", ["j_ring_le_3"] = "Base HumanLDigit43",
            ["j_pinky_le_1"] = "Base HumanLDigit51", ["j_pinky_le_2"] = "Base HumanLDigit52", ["j_pinky_le_3"] = "Base HumanLDigit53",
        };

        // combo: letra 1 = eje espejado (n, x, y, z), letra 2 = giro de 180 (I, X, Y, Z)
        internal static Quaternion Convert(Quaternion q, string combo)
        {
            switch (combo[0]) { case 'x': q = new Quaternion(q.x, -q.y, -q.z, q.w); break; case 'y': q = new Quaternion(-q.x, q.y, -q.z, q.w); break; case 'z': q = new Quaternion(-q.x, -q.y, q.z, q.w); break; }
            Quaternion c = combo[1] == 'X' ? Quaternion.Euler(180, 0, 0) : combo[1] == 'Y' ? Quaternion.Euler(0, 180, 0) : combo[1] == 'Z' ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
            return c * q * Quaternion.Inverse(c);
        }

        internal static Quaternion Rot(AnimViewer.Curve c, float f) { var v = AnimViewer.Sample(c, f); return new Quaternion(v[0], v[1], v[2], v[3]); }

        internal sealed class Retarget
        {
            public EftLab.Rig Rig;
            public AnimViewer.Clip Cod;
            public Dictionary<Transform, Quaternion> Base = new Dictionary<Transform, Quaternion>();
            public List<KeyValuePair<Transform, AnimViewer.Curve>> Pairs = new List<KeyValuePair<Transform, AnimViewer.Curve>>();
            public string Combo = "nI";

            public void Pose(float f)
            {
                foreach (var kv in Base) kv.Key.localRotation = kv.Value;
                foreach (var p in Pairs)
                {
                    var d = Quaternion.Inverse(Rot(p.Value, 0f)) * Rot(p.Value, f);
                    p.Key.localRotation = Base[p.Key] * Convert(d, Combo);
                }
            }
        }

        internal static Retarget Build(EftLab.Rig rig, string codJson, AnimationClip baseClip, float baseTime)
        {
            var r = new Retarget { Rig = rig, Cod = AnimViewer.Load(codJson) };
            baseClip.SampleAnimation(rig.Root, baseTime);
            foreach (var t in rig.Bones.Values) r.Base[t] = t.localRotation;
            foreach (var c in r.Cod.curves.Where(c => c.prop == "rq"))
            {
                string eft;
                if (Map.TryGetValue(c.bone, out eft) && rig.Bones.ContainsKey(eft)) r.Pairs.Add(new KeyValuePair<Transform, AnimViewer.Curve>(rig.Bones[eft], c));
            }
            return r;
        }

        internal static Quaternion CamRot(EftLab.Rig rig) { return rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f); }

        // ---------------------------------------------------------------- metodo IK (el bueno: tools/cod_fk.py hace lo mismo en Python)
        // CoD: cinematica directa del brazo (columna y clavicula de la animacion; huesos hacia su hijo por +X; largos supuestos) ->
        // codo y muneca respecto al hombro en ejes de camara. EFT: IK de dos huesos desde su hombro real hacia esos puntos, y la palma
        // con la orientacion de la muneca de CoD.
        private const float CodUpper = 0.29f, CodFore = 0.26f;
        private static readonly string[] Chain = { "j_spinelower", "j_spineupper", "j_spine4", "j_clavicle_le" };

        // CoD (X adelante, Y izquierda, Z arriba) -> camara de Unity (x derecha, y arriba, z adelante)
        private static Vector3 Cam(Vector3 v) { return new Vector3(-v.y, v.z, v.x); }

        internal static void CodArm(AnimViewer.Clip cod, float f, out Vector3 elbow, out Vector3 wrist, out Vector3[] wristAxes)
        {
            Quaternion q = Quaternion.identity;
            foreach (var b in Chain) { var c = cod.curves.FirstOrDefault(x => x.bone == b && x.prop == "rq"); if (c != null) q = q * Rot(c, f); }
            var qs = q * Rot(cod.curves.First(x => x.bone == "j_shoulder_le" && x.prop == "rq"), f);
            var e = qs * new Vector3(CodUpper, 0f, 0f);
            var qe = qs * Rot(cod.curves.First(x => x.bone == "j_elbow_le" && x.prop == "rq"), f);
            var w = e + qe * new Vector3(CodFore, 0f, 0f);
            var qw = qe * Rot(cod.curves.First(x => x.bone == "j_wrist_le" && x.prop == "rq"), f);
            elbow = Cam(e); wrist = Cam(w);
            wristAxes = new[] { Cam(qw * Vector3.right), Cam(qw * Vector3.up), Cam(qw * Vector3.forward) };
        }

        // ---------------------------------------------------------------- esqueleto real de los brazos de CoD (viewhands_skel.json,
        // sacado del .cast de los viewhands: padre, posicion y rotacion locales de reposo en cm). Cinematica directa desde tag_view (la
        // camara) con las rotaciones de la animacion; lo que la animacion no mueve se queda en reposo.
        internal sealed class CodSkel
        {
            public readonly Dictionary<string, string> Parent = new Dictionary<string, string>();
            public readonly Dictionary<string, Vector3> Lp = new Dictionary<string, Vector3>();
            public readonly Dictionary<string, Quaternion> Lr = new Dictionary<string, Quaternion>();

            public static CodSkel Load(string path)
            {
                var s = new CodSkel();
                var j = MiniJson.Parse(System.IO.File.ReadAllText(path)) as Dictionary<string, object>;
                foreach (var kv in j)
                {
                    var b = (Dictionary<string, object>)kv.Value;
                    var p = ((List<object>)b["lp"]).Select(System.Convert.ToSingle).ToArray();
                    var r = ((List<object>)b["lr"]).Select(System.Convert.ToSingle).ToArray();
                    s.Parent[kv.Key] = b["parent"] as string;
                    s.Lp[kv.Key] = new Vector3(p[0], p[1], p[2]);
                    s.Lr[kv.Key] = new Quaternion(r[0], r[1], r[2], r[3]);
                }
                return s;
            }
        }

        internal sealed class CodPose
        {
            private readonly CodSkel _s; private readonly Dictionary<string, AnimViewer.Curve> _rq, _tx, _ty, _tz; private readonly float _f;
            private readonly Dictionary<string, KeyValuePair<Vector3, Quaternion>> _w = new Dictionary<string, KeyValuePair<Vector3, Quaternion>>();
            private Vector3 _vp; private Quaternion _vqInv;

            public CodPose(CodSkel s, AnimViewer.Clip cod, float f)
            {
                _s = s; _f = f;
                System.Func<string, Dictionary<string, AnimViewer.Curve>> by = prop => cod.curves.Where(c => c.prop == prop).GroupBy(c => c.bone).ToDictionary(g => g.Key, g => g.First());
                _rq = by("rq"); _tx = by("tx"); _ty = by("ty"); _tz = by("tz");
                var v = World("tag_view"); _vp = v.Key; _vqInv = Quaternion.Inverse(v.Value);
            }

            private KeyValuePair<Vector3, Quaternion> World(string b)
            {
                KeyValuePair<Vector3, Quaternion> r;
                if (_w.TryGetValue(b, out r)) return r;
                AnimViewer.Curve c;
                var lr = _rq.TryGetValue(b, out c) ? Rot(c, _f) : _s.Lr[b];
                string par = _s.Parent[b];
                // traslaciones "relative": se suman a la posicion de reposo (asi tag_accessory_left, donde va la lata, sigue a la mano)
                var lp = _s.Lp[b];
                if (_tx.ContainsKey(b)) lp += new Vector3(AnimViewer.Sample(_tx[b], _f)[0], AnimViewer.Sample(_ty[b], _f)[0], AnimViewer.Sample(_tz[b], _f)[0]);
                if (par == null) r = new KeyValuePair<Vector3, Quaternion>(lp, lr);
                else { var pw = World(par); r = new KeyValuePair<Vector3, Quaternion>(pw.Key + pw.Value * lp, pw.Value * lr); }
                _w[b] = r; return r;
            }

            // posicion en ejes de camara (m) respecto a tag_view
            public Vector3 Pos(string b) { return Cam(_vqInv * (World(b).Key - _vp)) / 100f; }
            // direccion de un eje local del hueso, en ejes de camara
            public Vector3 Axis(string b, Vector3 local) { return Cam(_vqInv * (World(b).Value * local)); }
        }

        // los dedos: cada falange de EFT apunta (-X local) hacia donde apunta la de CoD (+X local, lado izquierdo)
        internal static readonly string[][] Fingers =
        {
            new[] { "j_thumb_le_1", "j_thumb_le_2", "j_thumb_le_3" }, new[] { "j_index_le_1", "j_index_le_2", "j_index_le_3" },
            new[] { "j_mid_le_1", "j_mid_le_2", "j_mid_le_3" }, new[] { "j_ring_le_1", "j_ring_le_2", "j_ring_le_3" },
            new[] { "j_pinky_le_1", "j_pinky_le_2", "j_pinky_le_3" },
        };

        internal const string CanTag = "tag_accessory_left";

        internal static void Aim(Transform t, Vector3 dir)
        {
            t.rotation = Quaternion.FromToRotation(t.rotation * Vector3.left, dir) * t.rotation;
        }

        // la palma, medida en las dos manos: "a" = de la muneca al nudillo del dedo corazon, "b" = del nudillo del menique al del
        // indice. Se gira la palma de EFT para que sus a y b (en su espacio local) caigan sobre los de CoD.
        internal static Quaternion Frame(Vector3 a, Vector3 b) { return Quaternion.LookRotation(a, Vector3.ProjectOnPlane(b, a)); }

        internal static Quaternion PalmMatch(Vector3 eftA, Vector3 eftB, Vector3 codA, Vector3 codB)
        {
            return Frame(codA, codB) * Quaternion.Inverse(Frame(eftA, eftB));
        }

        // IK de dos huesos: el hueso apunta a su hijo por -X local (convencion de EFT); se gira con FromTo para no tocar el resto
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

        // palm: sus ejes en EFT. -X hacia los dedos = eje X de la muneca de CoD; el eje Y de EFT = el eje de CoD que diga 'palmAxis' (+y,-y,+z,-z)
        internal static Quaternion PalmRot(Vector3[] ax, string palmAxis)
        {
            Vector3 x = -ax[0];
            Vector3 y = palmAxis[1] == 'y' ? ax[1] : ax[2]; if (palmAxis[0] == '-') y = -y;
            Vector3 z = Vector3.Cross(x, y);
            return Quaternion.LookRotation(z, y);
        }

        public static void Run()
        {
            try
            {
                var rig = EftLab.LoadRig(Env("LAB_WEAPON", "assets/content/weapons/usable_items/item_drink_cans/item_tarcola_container.bundle"));
                var baseClip = EftLab.FindClip(rig, Env("COD_BASECLIP", "item_drink_cans_out_to_use"));
                var r = Build(rig, Env("COD_ANIM", "Assets/Anim/t10_vm_gesture_zm_perk_can_drink.json"), baseClip, float.Parse(Env("COD_BASEFRAME", "0")) / 30f);
                Debug.Log("[COD] huesos enlazados: " + r.Pairs.Count + " de " + r.Cod.curves.Count + " curvas");
                var palm = rig.Bones["Base HumanLPalm"];
                foreach (var rend in rig.Root.GetComponentsInChildren<Renderer>(true)) if (rend.name.Contains("tarcola")) rend.enabled = false;

                if (Env("COD_MODE", "ik") == "ik")
                {
                    var upper = rig.Bones["Base HumanLUpperarm"]; var fore = rig.Bones["Base HumanLForearm1"];
                    string palmAxis = Env("COD_PALM", "+y");
                    float scale = (Vector3.Distance(upper.position, fore.position) + Vector3.Distance(fore.position, palm.position)) / (CodUpper + CodFore);
                    r.Combo = Env("COD_COMBO", "nI");
                    var fingers = r.Pairs.Where(p => p.Key.name.Contains("Digit")).ToList();
                    // el hombro de los brazos de CoD no esta donde el de EFT: desplazamiento en ejes de camara (m)
                    var off = Env("COD_OFFSET", "0,0,0.08").Split(',').Select(float.Parse).ToArray();
                    var offset = new Vector3(off[0], off[1], off[2]);
                    // la lata en la mano: en la palma de EFT, -X va a los dedos, -Y es la cara de la palma (el pulgar esta en +Z):
                    // la lata va hacia los dedos y hacia la palma, con su eje a lo ancho de la mano (pulgar arriba)
                    var canPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/bo6_can_" + Env("COD_CAN", "speedcola") + ".prefab");
                    GameObject can = null;
                    if (canPrefab != null)
                    {
                        can = Object.Instantiate(canPrefab, palm, false);
                        var cp = Env("COD_CANPOS", "-0.07,-0.045,0.01").Split(',').Select(float.Parse).ToArray();
                        can.transform.localPosition = new Vector3(cp[0], cp[1], cp[2]);
                        can.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                    }
                    float canYaw = float.Parse(Env("COD_CANYAW", "0"));
                    string skelPath = Env("COD_SKEL", "Assets/Anim/viewhands_skel.json");
                    var skel = System.IO.File.Exists(skelPath) ? CodSkel.Load(skelPath) : null;
                    // a y b de la palma de EFT en su espacio local (pose base: los nudillos no se mueven respecto a la palma)
                    var eftA = palm.InverseTransformDirection(rig.Bones["Base HumanLDigit31"].position - palm.position);
                    var eftB = palm.InverseTransformDirection(rig.Bones["Base HumanLDigit21"].position - rig.Bones["Base HumanLDigit51"].position);
                    Debug.Log("[COD] palma de EFT (local): a " + eftA.normalized.ToString("F2") + " b " + eftB.normalized.ToString("F2"));
                    // con el esqueleto: la muneca va donde la de CoD respecto a la camara (lo que importa es llevar la lata a la boca);
                    // COD_OFFSET corrige en ejes de camara (m). Sin el: el metodo viejo desde el hombro con largos supuestos.
                    // COD_SHOW=1: dibuja encima el esqueleto de CoD (brazo negro, pulgar rojo, indice verde, corazon azul,
                    // anular magenta, menique cian) para comparar
                    var sticks = new List<KeyValuePair<string[], Transform>>();
                    if (skel != null && Env("COD_SHOW", "0") == "1")
                    {
                        var chains = new List<string[]> { new[] { "j_shoulder_le", "j_elbow_le", "j_wrist_le" } };
                        foreach (var fc in Fingers) chains.Add(new[] { "j_wrist_le" }.Concat(fc).ToArray());
                        var colors = new[] { Color.black, Color.red, Color.green, Color.blue, Color.magenta, Color.cyan };
                        for (int i = 0; i < chains.Count; i++)
                            for (int k = 0; k + 1 < chains[i].Length; k++)
                            {
                                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                var mat = new Material(Shader.Find("Unlit/Color")); mat.color = colors[i];
                                go.GetComponent<Renderer>().sharedMaterial = mat;
                                sticks.Add(new KeyValuePair<string[], Transform>(new[] { chains[i][k], chains[i][k + 1] }, go.transform));
                            }
                    }
                    System.Action<int> ik = f =>
                    {
                        foreach (var kv in r.Base) kv.Key.localRotation = kv.Value;
                        var rc = CamRot(rig);
                        if (skel != null)
                        {
                            var cp = new CodPose(skel, r.Cod, f);
                            var cam = rig.CamAnim.position;
                            foreach (var st in sticks)
                            {
                                Vector3 a = cam + rc * cp.Pos(st.Key[0]), b = cam + rc * cp.Pos(st.Key[1]);
                                st.Value.position = (a + b) * 0.5f;
                                st.Value.rotation = Quaternion.LookRotation(b - a);
                                st.Value.localScale = new Vector3(0.004f, 0.004f, Vector3.Distance(a, b));
                            }
                            TwoBone(upper, fore, palm, cam + rc * (cp.Pos("j_wrist_le") + offset), cam + rc * (cp.Pos("j_elbow_le") + offset));
                            var codA = cp.Pos("j_mid_le_1") - cp.Pos("j_wrist_le");
                            var codB = cp.Pos("j_index_le_1") - cp.Pos("j_pinky_le_1");
                            palm.rotation = rc * PalmMatch(eftA, eftB, codA, codB);
                            for (int i = 0; i < Fingers.Length; i++)
                                foreach (var b in Fingers[i]) Aim(rig.Bones[Map[b]], rc * cp.Axis(b, Vector3.right));
                            // la lata, donde la pone CoD: su base en tag_accessory_left, el eje en su +Z (el prefab esta centrado, eje +Y)
                            if (can != null)
                            {
                                int[] show, hide;
                                can.SetActive((!r.Cod.notes.TryGetValue("show", out show) || f >= show[0]) && (!r.Cod.notes.TryGetValue("hide", out hide) || f < hide[0]));
                                var up = rc * cp.Axis(CanTag, Vector3.forward);
                                var front = rc * cp.Axis(CanTag, Vector3.right);
                                can.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(front, up), up) * Quaternion.Euler(0f, canYaw, 0f);
                                can.transform.position = cam + rc * cp.Pos(CanTag) + up * (BuildBo6.CanHeight * 0.5f);
                            }
                            return;
                        }
                        Vector3 e, w; Vector3[] ax;
                        CodArm(r.Cod, f, out e, out w, out ax);
                        var s = upper.position;
                        TwoBone(upper, fore, palm, s + rc * (w * scale + offset), s + rc * (e * scale + offset));
                        palm.rotation = rc * PalmRot(ax, palmAxis);
                        foreach (var p in fingers) p.Key.localRotation = r.Base[p.Key] * Convert(Quaternion.Inverse(Rot(p.Value, 0f)) * Rot(p.Value, f), r.Combo);
                    };
                    if (skel != null)
                    {
                        var cp0 = new CodPose(skel, r.Cod, 0f);
                        Debug.Log("[COD] esqueleto real: hombro L de CoD (camara) " + cp0.Pos("j_shoulder_le").ToString("F3"));
                    }
                    Debug.Log("[COD] modo IK, escala " + scale.ToString("F2") + ", palma " + palmAxis);
                    foreach (int f in new[] { 5, 30, 65 })
                    {
                        ik(f);
                        var inv = Quaternion.Inverse(CamRot(rig));
                        System.Func<Transform, string> c = t => (inv * (t.position - rig.CamAnim.position)).ToString("F2");
                        Debug.Log("[COD] f" + f + " (camara) hombro L " + c(upper) + " hombro R " + c(rig.Bones["Base HumanRUpperarm"]) + " codo " + c(fore) + " palma " + c(palm));
                    }
                    EftLab.RenderSheet(rig, ik, EftLab.ParseFrames(Env("LAB_FRAMES", "0-100/5"), r.Cod.frames - 1), Env("LAB_OUT", "cod.png"));
                    return;
                }

                // buscar la conversion: distancia minima de la palma a la boca entre los fotogramas 35 y 80 (y que al principio este lejos)
                var scores = new List<KeyValuePair<string, float>>();
                foreach (char m in "nxyz") foreach (char c in "IXYZ")
                {
                    r.Combo = "" + m + c;
                    float best = 9f;
                    for (int f = 35; f <= 80; f += 3)
                    {
                        r.Pose(f);
                        var cr = CamRot(rig);
                        var mouth = rig.CamAnim.position + cr * new Vector3(0f, -0.09f, 0.08f);
                        best = Mathf.Min(best, Vector3.Distance(palm.position, mouth));
                    }
                    scores.Add(new KeyValuePair<string, float>(r.Combo, best));
                }
                foreach (var s in scores.OrderBy(s => s.Value)) Debug.Log("[COD] combinacion " + s.Key + ": palma a " + (s.Value * 100f).ToString("0") + " cm de la boca");
                r.Combo = Env("COD_COMBO", scores.OrderBy(s => s.Value).First().Key);
                Debug.Log("[COD] se usa " + r.Combo);

                // ocultar la lata de EFT
                foreach (var rend in rig.Root.GetComponentsInChildren<Renderer>(true)) if (rend.name.Contains("tarcola")) rend.enabled = false;
                EftLab.RenderSheet(rig, f => r.Pose(f), EftLab.ParseFrames(Env("LAB_FRAMES", "0-100/5"), r.Cod.frames - 1), Env("LAB_OUT", "cod.png"));
            }
            catch (System.Exception e) { Debug.LogError("[COD] " + e); }
            finally { EftBundles.UnloadAll(); }
        }
    }
}
