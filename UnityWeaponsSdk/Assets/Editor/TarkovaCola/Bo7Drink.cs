using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Beber perks con la animacion de BO7 (t10_vm_gesture_zm_perk_can_drink), horneada sobre el rig de la TarCola de EFT.
//   DRINK_FRAMES=0-end/4                -executeMethod TarkovaCola.Bo7Anim.DrinkRun    hoja de la animacion de BO7 sobre el rig (sin hornear)
//   [DRINK_SHEET=1]                     -executeMethod TarkovaCola.Bo7Anim.DrinkBake   hornea drink_<clip de EFT> y construye bo7_anims.bundle
// Es la cuenta de Client/CodDrink.cs (probada en el laboratorio) pasada a clips: el brazo izquierdo sigue al de BO7 (palma por los
// nudillos, codo como polo, falanges como en las armas), la lata de EFT (hueso "weapon") va donde la lata de BO7 (tag_accessory_left:
// base de la lata, +Z hacia arriba) y el brazo derecho se queda fuera de la vista (primer fotograma de sacar la lata de EFT).
// El cliente (Client/Bo7Drink.cs) cambia los clips del controlador de la lata solo al beber una perk.
namespace TarkovaCola
{
    public static partial class Bo7Anim
    {
        private const string DrinkBundle = "assets/content/weapons/usable_items/item_drink_cans/item_tarcola_container.bundle";
        private const string DrinkClip = "t10_vm_gesture_zm_perk_can_drink";
        private const float OpenFrame = 22f;                                                         // la anilla de BO7 (j_can_tab) termina de abrir
        private const int DrinkInStart = 1, DrinkInEnd = 11, DrinkOutStart = 87, DrinkOutEnd = 99;   // notas ik_out / ik_in de la mano izquierda

        internal sealed class DrinkSolver
        {
            public EftLab.Rig Rig; public Skel Skel; public CodClip Clip;
            public Transform Upper, Fore, Palm, Weapon;
            public List<(string cod, Transform eft)> Fingers;
            public Dictionary<Transform, (Vector3 p, Quaternion r)> Base = new Dictionary<Transform, (Vector3, Quaternion)>();
            public Dictionary<Transform, Matrix4x4> MarkerOff = new Dictionary<Transform, Matrix4x4>();
            public Matrix4x4 CamW, S, FingerConv, KnuckleInv;
            public Vector3 CanBase, CanUp, CanFront;   // lata de EFT en el espacio del hueso weapon

            public void Reset() { foreach (var kv in Base) { kv.Key.localPosition = kv.Value.p; kv.Key.localRotation = kv.Value.r; } }

            public Vector3 Push;                       // camara (x der, y arriba, z adelante): la mano de EFT es ~1,4 veces la de BO7 y tapaba la vista al beber
            public Dictionary<Transform, (Vector3 p, Quaternion r)> Open = new Dictionary<Transform, (Vector3, Quaternion)>();   // lata abierta (tc_*)
            public Matrix4x4 W(Fk fk, string b) => CamW * Matrix4x4.Translate(Push) * Basis(S, fk.Rel("tag_view", b));

            public static float Weight(float f)
            {
                if (f < DrinkInEnd) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(DrinkInStart, DrinkInEnd, f));
                if (f > DrinkOutStart) return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(DrinkOutStart, DrinkOutEnd, f));
                return 1f;
            }

            private Matrix4x4 AnimPalm(Fk fk)
            {
                System.Func<string, Vector3> cp = b => Pos(W(fk, b));
                return HandFrameAt(cp("j_wrist_le"), cp("j_index_le_1"), cp("j_pinky_le_1"), 1f) * KnuckleInv;
            }

            public void Pose(float f)
            {
                Reset();
                var fk = new Fk(Skel, Clip, f);
                float w = Weight(f);
                var chain = new List<Transform> { Upper, Fore, Palm }; chain.AddRange(Fingers.Select(x => x.eft));
                var orig = chain.Select(t => t.localRotation).ToArray();
                // palma: nudillos de EFT donde los de BO7 (como en las armas)
                var anim = AnimPalm(fk);
                TwoBone(Upper, Fore, Palm, Pos(anim), Pos(W(fk, "j_elbow_le")));
                Palm.rotation = Rot(anim);
                foreach (var (cod, eft) in Fingers) eft.rotation = Rot(W(fk, cod) * FingerConv) * FingerC;
                if (w < 0.999f)
                {
                    var target = chain.Select(t => t.localRotation).ToArray();
                    for (int i = 0; i < chain.Count; i++) chain[i].localRotation = Quaternion.Slerp(orig[i], target[i], w);
                }
                // lata: pegada a la palma (tambien mientras el brazo entra o sale mezclado)
                var animN = Matrix4x4.TRS(Pos(anim), Rot(anim), Vector3.one);
                var t2 = World(Palm) * animN.inverse * W(fk, "tag_accessory_left");
                Vector3 up = t2.MultiplyVector(Vector3.up).normalized, front = Vector3.ProjectOnPlane(t2.MultiplyVector(Vector3.forward), up).normalized;
                Weapon.rotation = Quaternion.LookRotation(front, up) * Quaternion.Inverse(Quaternion.LookRotation(CanFront, CanUp));
                Weapon.position = Pos(t2) - Weapon.TransformVector(CanBase);
                if (f >= OpenFrame) foreach (var kv in Open) { kv.Key.localPosition = kv.Value.p; kv.Key.localRotation = kv.Value.r; }
                foreach (var kv in MarkerOff) { var bone = Rig.Bones[Markers[kv.Key.name]]; var m = World(bone) * kv.Value; kv.Key.SetPositionAndRotation(Pos(m), Rot(m)); }
            }
        }

        internal static DrinkSolver BuildDrink()
        {
            var rig = EftLab.LoadRig(DrinkBundle);
            var s = new DrinkSolver { Rig = rig, Skel = LoadSkel("../UnityBundle/Assets/Anim/viewhands_skel.json"), Clip = LoadClip(Dir + DrinkClip + ".json") };
            s.Upper = rig.Bones["Base HumanLUpperarm"]; s.Fore = rig.Bones["Base HumanLForearm1"]; s.Palm = rig.Bones["Base HumanLPalm"]; s.Weapon = rig.Bones["weapon"];
            s.Fingers = HandMap("le").Skip(1).Where(p => rig.Bones.ContainsKey(p.eft) && s.Skel.Parent.ContainsKey(p.cod)).Select(p => (p.cod, rig.Bones[p.eft])).ToList();

            // lata de EFT: eje (el lado mas largo de la malla) y extremo de arriba (el que queda mas cerca de la camara al beber)
            var use = EftLab.FindClip(rig, "item_drink_cans_use");
            use.SampleAnimation(rig.Root, use.length * 0.5f);
            var mf = s.Weapon.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null && !m.name.Contains("LOD1") && !m.name.Contains("LOD2")).OrderByDescending(m => m.sharedMesh.bounds.size.sqrMagnitude).First();
            var b = mf.sharedMesh.bounds; int ax = b.size.x > b.size.y ? (b.size.x > b.size.z ? 0 : 2) : (b.size.y > b.size.z ? 1 : 2);
            var axis = Vector3.zero; axis[ax] = 1f; var perp = Vector3.zero; perp[(ax + 1) % 3] = 1f;
            Vector3 endA = b.center + axis * b.extents[ax], endB = b.center - axis * b.extents[ax];
            var camPos = rig.CamAnim.position;
            bool aTop = (mf.transform.TransformPoint(endA) - camPos).magnitude < (mf.transform.TransformPoint(endB) - camPos).magnitude;
            Vector3 top = aTop ? endA : endB, bottom = aTop ? endB : endA;
            s.CanBase = s.Weapon.InverseTransformPoint(mf.transform.TransformPoint(bottom));
            s.CanUp = s.Weapon.InverseTransformDirection(mf.transform.TransformDirection(top - bottom)).normalized;
            s.CanFront = Vector3.ProjectOnPlane(s.Weapon.InverseTransformDirection(mf.transform.TransformDirection(perp)), s.CanUp).normalized;
            Debug.Log("[DRINK] lata " + mf.name + ": alto " + (b.extents[ax] * 2f * mf.transform.lossyScale[ax]).ToString("F3") + " m, base " + s.CanBase.ToString("F3") + ", eje " + s.CanUp.ToString("F2"));

            // lata abierta: final de sacarla en EFT
            var o2u = EftLab.FindClip(rig, "item_drink_cans_out_to_use");
            o2u.SampleAnimation(rig.Root, o2u.length);
            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("tc_")) s.Open[t] = (t.localPosition, t.localRotation);
            // reposo: primer fotograma de sacar la lata (brazos fuera de la vista)
            var outToUse = EftLab.FindClip(rig, "item_drink_cans_out_to_use");
            outToUse.SampleAnimation(rig.Root, 0f);
            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) s.Base[t] = (t.localPosition, t.localRotation);
            foreach (var kv in Markers) { Transform m, bn; if (rig.Bones.TryGetValue(kv.Key, out m) && rig.Bones.TryGetValue(kv.Value, out bn)) s.MarkerOff[m] = World(bn).inverse * World(m); }
            s.KnuckleInv = (World(s.Palm).inverse * HandFrameAt(s.Palm.position, rig.Bones["Base HumanLDigit21"].position, rig.Bones["Base HumanLDigit51"].position, 1f)).inverse;
            s.CamW = Matrix4x4.TRS(rig.CamAnim.position, rig.CamAnim.rotation * Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            var pv = Env("DRINK_PUSH", "0,-0.04,0.03").Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            s.Push = s.CamW.MultiplyVector(new Vector3(pv[0], pv[1], pv[2]));
            s.S = Matrix4x4.Scale(Vector3.one * 0.01f) * CamBasis();
            // falanges: la constante FingerC esta medida con la base de las armas (FbxBasis); aqui se convierte desde la de la camara
            s.FingerConv = CamBasis() * FbxBasis(1f).inverse;
            return s;
        }

        public static void DrinkRun()
        {
            try
            {
                var s = BuildDrink();
                var frames = EftLab.ParseFrames(Env("DRINK_FRAMES", "0-end/4"), s.Clip.Frames - 1);
                EftLab.RenderSheet(s.Rig, f => s.Pose(f), frames, Env("DRINK_OUT", "Preview/drink_bo7.png"));
            }
            catch (System.Exception e) { Debug.LogError("[DRINK] " + e); }
            finally { EftBundles.UnloadAll(); }
        }

        // clips de EFT <- tramos de BO7: subir y abrir (0-60), trago en bucle (60-68-60) y bajar (60-100), a su velocidad
        private static Piece[] DrinkJobs() => new[]
        {
            Nat("item_drink_cans_out_to_use", DrinkClip, 0f, 60f),
            Seq("item_drink_cans_use", DrinkClip, null, (60f, 68f, 10f), (68f, 60f, 10f)),
            Nat("item_drink_cans_use_to_out", DrinkClip, 60f, 100f),
        };

        public static void DrinkBake()
        {
            try
            {
                var s = BuildDrink();
                var rec = s.Rig.Root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Base Human") || t.name == "Camera_animated" || t.name == "weapon"
                    || t.name.StartsWith("tc_") || Markers.ContainsKey(t.name)).ToList();
                Directory.CreateDirectory(Dir + "Baked");
                var baked = new List<AnimationClip>();
                string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
                foreach (var piece in DrinkJobs())
                {
                    int n = Mathf.Max(2, Mathf.RoundToInt(piece.Seconds * 30f) + 1);
                    var curves = rec.ToDictionary(t => t, t => new AnimationCurve[7].Select(_ => new AnimationCurve()).ToArray());
                    for (int i = 0; i < n; i++)
                    {
                        float u = i / (float)(n - 1);
                        s.Pose(piece.FrameAt(u));
                        foreach (var kv in curves)
                        {
                            var p = kv.Key.localPosition; var q = kv.Key.localRotation; var v = new[] { p.x, p.y, p.z, q.x, q.y, q.z, q.w };
                            for (int c = 0; c < 7; c++) kv.Value[c].AddKey(new Keyframe(u * piece.Seconds, v[c]));
                        }
                    }
                    var clip = new AnimationClip { name = "drink_" + piece.Eft, frameRate = 30f };
                    foreach (var kv in curves) { string path = AnimationUtility.CalculateTransformPath(kv.Key, s.Rig.Root.transform); for (int c = 0; c < 7; c++) clip.SetCurve(path, typeof(Transform), props[c], kv.Value[c]); }
                    clip.EnsureQuaternionContinuity();
                    if (piece.Eft.EndsWith("_use")) { var st = AnimationUtility.GetAnimationClipSettings(clip); st.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, st); }
                    string asset = Dir + "Baked/" + clip.name + ".anim";
                    AssetDatabase.DeleteAsset(asset);
                    AssetDatabase.CreateAsset(clip, asset);
                    baked.Add(clip);
                    Debug.Log("[DRINK] horneado " + clip.name + " = " + clip.length.ToString("F2") + " s");
                }
                AssetDatabase.SaveAssets();
                if (Env("DRINK_SHEET", "1") == "1")
                {
                    var seq = new List<(AnimationClip c, float t)>();
                    foreach (var c in baked) for (float t = 0f; t <= c.length + 1e-4f; t += 4f / 30f) seq.Add((c, t));
                    EftLab.RenderSheet(s.Rig, i => { s.Reset(); seq[i].c.SampleAnimation(s.Rig.Root, seq[i].t); }, Enumerable.Range(0, seq.Count).ToList(), Env("DRINK_OUT", "Preview/drink_baked.png"));
                }
                BuildAnimBundles();
            }
            catch (System.Exception e) { Debug.LogError("[DRINK] " + e); }
            finally { EftBundles.UnloadAll(); }
        }
    }
}
