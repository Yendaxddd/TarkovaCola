using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Animacion de beber de Black Ops 7 (t10_vm_gesture_zm_perk_can_drink) sobre el brazo izquierdo del jugador, al beber una
    //  perk (no con los inyectores). Es lo mismo que el laboratorio de Unity (UnityBundle/Assets/Editor/EftCod.cs):
    //   - assets/cod_viewhands.json: el esqueleto de los brazos de CoD (padre, posicion y giro de reposo en cm).
    //   - assets/cod_drink.json: la animacion (giros por hueso; tag_accessory_left, donde va la lata, tambien con traslacion).
    //  Cinematica directa desde tag_view (la camara); la muneca de EFT va donde la de CoD respecto a la camara (IK de dos huesos),
    //  la palma se alinea con la de CoD por los nudillos y cada falange apunta como la de CoD. Se aplica despues de que EFT coloca
    //  los brazos (ProceduralWeaponAnimation.LateTransformations), mezclada con la pose de EFT al entrar y al salir.
    //  Tiempo: subir y abrir (fotogramas 0-55) y bajar (75-100) a su velocidad; el trago (55-75) se estira hasta lo que dura beber.
    // ============================================================================================
    internal static class CodDrink
    {
        private const float Fps = 30f;
        private const int DrinkFrom = 55, DrinkTo = 75, Last = 100;
        private const int InStart = 1, InEnd = 11, OutStart = 87, OutEnd = 99;      // notas ik_out / ik_in de la animacion
        private const int Show = 5, Hide = 90;
        private const string CanTag = "tag_accessory_left";
        private const float CanHeight = 0.13f;                                         // el modelo de la lata en la mano (BuildBo6.CanHeight)

        private static readonly string[][] Fingers =
        {
            new[] { "j_thumb_le_1", "j_thumb_le_2", "j_thumb_le_3" }, new[] { "j_index_le_1", "j_index_le_2", "j_index_le_3" },
            new[] { "j_mid_le_1", "j_mid_le_2", "j_mid_le_3" }, new[] { "j_ring_le_1", "j_ring_le_2", "j_ring_le_3" },
            new[] { "j_pinky_le_1", "j_pinky_le_2", "j_pinky_le_3" },
        };
        private static readonly string[] EftFingers = { "1", "2", "3", "4", "5" };        // Base HumanLDigit<dedo><falange>

        // ---------------------------------------------------------------- datos
        private class Curve { public int[] F; public float[][] V; }
        private static bool _loaded, _failed;
        private static readonly Dictionary<string, string> _parent = new Dictionary<string, string>();
        private static readonly Dictionary<string, Vector3> _lp = new Dictionary<string, Vector3>();
        private static readonly Dictionary<string, Quaternion> _lr = new Dictionary<string, Quaternion>();
        private static readonly Dictionary<string, Curve> _rq = new Dictionary<string, Curve>(), _tx = new Dictionary<string, Curve>(), _ty = new Dictionary<string, Curve>(), _tz = new Dictionary<string, Curve>();

        private static bool Load()
        {
            if (_loaded || _failed) return _loaded;
            try
            {
                string dir = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "assets");
                foreach (var kv in JObject.Parse(File.ReadAllText(Path.Combine(dir, "cod_viewhands.json"))))
                {
                    var b = (JObject)kv.Value;
                    _parent[kv.Key] = b["parent"].Type == JTokenType.Null ? null : (string)b["parent"];
                    var p = b["lp"].Select(x => (float)x).ToArray(); var r = b["lr"].Select(x => (float)x).ToArray();
                    _lp[kv.Key] = new Vector3(p[0], p[1], p[2]); _lr[kv.Key] = new Quaternion(r[0], r[1], r[2], r[3]);
                }
                foreach (JObject c in JObject.Parse(File.ReadAllText(Path.Combine(dir, "cod_drink.json")))["curves"])
                {
                    var cv = new Curve
                    {
                        F = c["frames"].Select(x => (int)x).ToArray(),
                        V = c["values"].Select(v => v.Type == JTokenType.Array ? v.Select(x => (float)x).ToArray() : new[] { (float)v }).ToArray(),
                    };
                    string bone = (string)c["bone"], prop = (string)c["prop"];
                    var d = prop == "rq" ? _rq : prop == "tx" ? _tx : prop == "ty" ? _ty : prop == "tz" ? _tz : null;
                    if (d != null) d[bone] = cv;
                }
                _loaded = true;
                Dbg.Log("BEBER", "animacion de CoD cargada: " + _lp.Count + " huesos, " + _rq.Count + " curvas de giro");
            }
            catch (System.Exception e) { _failed = true; Plugin.Log.LogWarning("Animacion de beber de CoD: no se pudo cargar (" + e.Message + "); se usa la de EFT"); }
            return _loaded;
        }

        private static float[] Sample(Curve c, float f)
        {
            int i = 0;
            while (i + 1 < c.F.Length && c.F[i + 1] <= f) i++;
            if (i + 1 >= c.F.Length || f <= c.F[0]) return c.V[Mathf.Min(i, c.V.Length - 1)];
            float t = (f - c.F[i]) / (c.F[i + 1] - c.F[i]);
            var a = c.V[i]; var b = c.V[i + 1];
            if (a.Length == 4) { var q = Quaternion.Slerp(new Quaternion(a[0], a[1], a[2], a[3]), new Quaternion(b[0], b[1], b[2], b[3]), t); return new[] { q.x, q.y, q.z, q.w }; }
            return new[] { Mathf.Lerp(a[0], b[0], t) };
        }

        // ---------------------------------------------------------------- pose de CoD en un fotograma (ejes de camara, m)
        private sealed class Pose
        {
            private readonly float _f;
            private readonly Dictionary<string, KeyValuePair<Vector3, Quaternion>> _w = new Dictionary<string, KeyValuePair<Vector3, Quaternion>>();
            private readonly Vector3 _vp; private readonly Quaternion _vqInv;

            public Pose(float f) { _f = f; var v = World("tag_view"); _vp = v.Key; _vqInv = Quaternion.Inverse(v.Value); }

            private KeyValuePair<Vector3, Quaternion> World(string b)
            {
                KeyValuePair<Vector3, Quaternion> r;
                if (_w.TryGetValue(b, out r)) return r;
                Curve c;
                Quaternion lr = _lr[b];
                if (_rq.TryGetValue(b, out c)) { var v = Sample(c, _f); lr = new Quaternion(v[0], v[1], v[2], v[3]); }
                var lp = _lp[b];
                if (_tx.ContainsKey(b)) lp += new Vector3(Sample(_tx[b], _f)[0], Sample(_ty[b], _f)[0], Sample(_tz[b], _f)[0]);     // "relative": sobre el reposo
                string par = _parent[b];
                if (par == null) r = new KeyValuePair<Vector3, Quaternion>(lp, lr);
                else { var pw = World(par); r = new KeyValuePair<Vector3, Quaternion>(pw.Key + pw.Value * lp, pw.Value * lr); }
                _w[b] = r; return r;
            }

            // CoD (X adelante, Y izquierda, Z arriba) -> camara (x derecha, y arriba, z adelante)
            private static Vector3 Cam(Vector3 v) { return new Vector3(-v.y, v.z, v.x); }
            public Vector3 Pos(string b) { return Cam(_vqInv * (World(b).Key - _vp)) / 100f; }
            public Vector3 Axis(string b, Vector3 local) { return Cam(_vqInv * (World(b).Value * local)); }
        }

        // ---------------------------------------------------------------- estado
        private static int _ctrlId;
        private static float _start, _drinkLen;
        private static PerkDef _perk;
        private static Transform _upper, _fore, _palm;
        private static readonly List<KeyValuePair<string, Transform>> _fingers = new List<KeyValuePair<string, Transform>>();
        private static Vector3 _eftA, _eftB;
        private static GameObject _can;
        private static bool _hidEft;

        internal static bool Active { get { return _ctrlId != 0; } }

        // fotograma de CoD para el tiempo t desde que se saca la lata
        private static float Frame(float t)
        {
            float a = DrinkFrom / Fps;
            if (t < a) return t * Fps;
            if (t < a + _drinkLen) return DrinkFrom + (t - a) / _drinkLen * (DrinkTo - DrinkFrom);
            return Mathf.Min(Last, DrinkTo + (t - a - _drinkLen) * Fps);
        }

        private static float Weight(float f)
        {
            if (f < InEnd) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(InStart, InEnd, f));
            if (f > OutStart) return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(OutStart, OutEnd, f));
            return 1f;
        }

        // ---------------------------------------------------------------- cada frame, despues de colocar los brazos (MeleeLatePatch)
        internal static void LateTransform(Player me)
        {
            var meds = me.HandsController as Player.MedsController;
            var item = meds != null ? meds.Item : null;
            var perk = item != null ? Perks.ByItem(item.StringTemplateId) : null;
            if (perk == null || HandModel.IsInjector(item) || !Plugin.CfgCodDrink.Value || !Load()) { Stop(); return; }
            int id = meds.GetInstanceID();
            if (id != _ctrlId && !Begin(me, meds, item, perk)) return;

            float f = Frame(Time.time - _start);
            if (f >= Last) { Stop(); return; }
            float w = Weight(f);
            var cam = Powerup.Cam();
            if (cam == null || _upper == null) return;
            Apply(new Pose(f), cam.transform, w);
            PlaceCan(new Pose(f), cam.transform, f, w);
        }

        private static bool Begin(Player me, Player.MedsController meds, Item item, PerkDef perk)
        {
            Stop();
            var bones = me.PlayerBones;
            if (bones == null || bones.LeftPalm == null) return false;
            _palm = bones.LeftPalm; _fore = _palm.parent; _upper = _fore != null ? _fore.parent : null;
            // entre el antebrazo y la palma puede haber huesos de giro: se busca hacia arriba por nombre
            for (var t = _palm.parent; t != null; t = t.parent)
            {
                if (t.name == "Base HumanLForearm1") _fore = t;
                if (t.name == "Base HumanLUpperarm") { _upper = t; break; }
            }
            if (_upper == null || _fore == null) { Plugin.Log.LogWarning("Animacion de beber de CoD: no se encontro el brazo izquierdo"); return false; }
            _fingers.Clear();
            var all = _palm.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < Fingers.Length; i++)
                for (int k = 0; k < 3; k++)
                {
                    var t = all.FirstOrDefault(x => x.name == "Base HumanLDigit" + EftFingers[i] + (k + 1));
                    if (t != null) _fingers.Add(new KeyValuePair<string, Transform>(Fingers[i][k], t));
                }
            var d31 = all.FirstOrDefault(x => x.name == "Base HumanLDigit31"); var d21 = all.FirstOrDefault(x => x.name == "Base HumanLDigit21"); var d51 = all.FirstOrDefault(x => x.name == "Base HumanLDigit51");
            if (d31 == null || d21 == null || d51 == null) { Plugin.Log.LogWarning("Animacion de beber de CoD: faltan los dedos de la mano izquierda"); return false; }
            _eftA = _palm.InverseTransformDirection(d31.position - _palm.position);
            _eftB = _palm.InverseTransformDirection(d21.position - d51.position);

            // el trago dura lo que dura beber en EFT (menos subir y bajar la lata)
            float use = item.Template is FoodDrinkTemplate fd ? fd.UseTime : item.Template is MedsTemplate md ? md.UseTime : 0f;
            if (use <= 0f) use = 4f;
            _drinkLen = Mathf.Max((DrinkTo - DrinkFrom) / Fps, use - (DrinkFrom + Last - DrinkTo) / Fps);
            _ctrlId = meds.GetInstanceID(); _start = Time.time; _perk = perk;

            var prefab = HandModel.Load(perk);
            if (prefab != null)
            {
                _can = Object.Instantiate(prefab);
                _can.name = "TarkovaColaCodCan";
                foreach (var c in _can.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
                int layer = HandModel.Instance != null ? HandModel.Instance.layer : _palm.gameObject.layer;
                foreach (var t in _can.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
                _can.SetActive(false);
            }
            Dbg.Log("BEBER", "animacion de CoD: " + perk.Id + ", beber " + use.ToString("0.0") + " s -> trago " + _drinkLen.ToString("0.00") + " s, " + _fingers.Count + " falanges");
            return true;
        }

        private static void Stop()
        {
            if (_ctrlId == 0) return;
            _ctrlId = 0;
            if (_can != null) Object.Destroy(_can);
            _can = null;
            if (_hidEft) { HandModel.SetHidden(false); _hidEft = false; }
        }

        // brazo, palma y dedos con la pose de CoD, mezclados con la de EFT por w (giros locales de padre a hijo)
        private static void Apply(Pose p, Transform cam, float w)
        {
            var bonesOrder = new List<Transform> { _upper, _fore, _palm };
            bonesOrder.AddRange(_fingers.Select(x => x.Value));
            var orig = bonesOrder.Select(t => t.localRotation).ToArray();

            var rc = cam.rotation; var cp = cam.position;
            TwoBone(_upper, _fore, _palm, cp + rc * p.Pos("j_wrist_le"), cp + rc * p.Pos("j_elbow_le"));
            var codA = p.Pos("j_mid_le_1") - p.Pos("j_wrist_le");
            var codB = p.Pos("j_index_le_1") - p.Pos("j_pinky_le_1");
            _palm.rotation = rc * Frame(codA, codB) * Quaternion.Inverse(Frame(_eftA, _eftB));
            foreach (var kv in _fingers) Aim(kv.Value, rc * p.Axis(kv.Key, Vector3.right));

            if (w >= 0.999f) return;
            var target = bonesOrder.Select(t => t.localRotation).ToArray();
            for (int i = 0; i < bonesOrder.Count; i++) bonesOrder[i].localRotation = Quaternion.Slerp(orig[i], target[i], w);
        }

        // la lata de CoD: su base en tag_accessory_left, su eje en el +Z del tag. La de EFT se esconde mientras.
        private static void PlaceCan(Pose p, Transform cam, float f, float w)
        {
            if (_can == null) return;
            bool show = f >= Show && f < Hide && w > 0.5f;
            if (show != _hidEft) { HandModel.SetHidden(show); _hidEft = show; }
            _can.SetActive(show);
            if (!show) return;
            var rc = cam.rotation;
            var up = rc * p.Axis(CanTag, Vector3.forward);
            var front = rc * p.Axis(CanTag, Vector3.right);
            _can.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(front, up), up);
            _can.transform.position = cam.position + rc * p.Pos(CanTag) + up * (CanHeight * 0.5f);
        }

        // ---------------------------------------------------------------- utilidades (como EftCod)
        private static Quaternion Frame(Vector3 a, Vector3 b) { return Quaternion.LookRotation(a, Vector3.ProjectOnPlane(b, a)); }

        private static void Aim(Transform t, Vector3 dir) { t.rotation = Quaternion.FromToRotation(t.rotation * Vector3.left, dir) * t.rotation; }

        // IK de dos huesos (los huesos de EFT apuntan a su hijo por -X local)
        private static void TwoBone(Transform upper, Transform fore, Transform end, Vector3 target, Vector3 pole)
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
    }
}
