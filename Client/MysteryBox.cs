using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Caja misteriosa (modelo de BO7). Sale en UNO de sus puntos del mapa (Machines.PickRandom). Por 1.000 rublos:
    //   mantener F = un arma al azar (montada, con su preset de fabrica) | mantener la otra tecla (F12) = un accesorio al azar.
    //  Como en Call of Duty: se abre la tapa, de dentro sube un modelo que va cambiando (cada vez mas despacio) y al final queda
    //  flotando el que te toca. Es un objeto de verdad del suelo: se coge mirandolo con la F de EFT. Si no lo coges a tiempo, se
    //  hunde en la caja y la tapa se cierra. Tras unos usos en el mismo sitio puede salir el osito: devuelve el dinero, la caja sube dando
    //  tumbos (animacion de BO6), sale disparada al cielo y cae en otro de sus puntos (Machines.MoveBox).
    // ============================================================================================
    internal static class MysteryBox
    {
        internal const int Price = 1000;
        private const float SpinTime = 4.5f, OfferTime = 10f, SinkTime = 0.8f, LoadTimeout = 8f;
        private const int Decoys = 7;                                          // modelos que van pasando ademas del premio
        private const float LowY = 0.75f, HighY = 1.45f;                       // altura del modelo sobre el suelo de la caja (m)
        private const string MasterMod = "55802f4a4bdc2ddb688b4569";          // nodo padre de los accesorios de arma
        private const string Roubles = "5449016a4bdc2d6f028b456f";

        // la tapa: grados por tiempo, sacados de las animaciones de BO6 (t10_zm_mystery_box_open/close_fxanim, 30 fps)
        private static readonly float[] OpenT = { 0f, 0.133f, 0.3f, 0.433f, 0.667f }, OpenA = { 0f, 35f, 97f, 98f, 90f };
        private static readonly float[] CloseT = { 0f, 0.133f, 0.267f, 0.467f, 0.667f }, CloseA = { 90f, 71f, 26f, 3f, 0f };
        // t10_zm_mystery_box_leaving_fxanim (BO6/BO7), cada 2 fotogramas a 30 fps: tapa (grados), giro de la caja (grados), lado y altura (m)
        private static readonly float[] LeaveLid = { 96.611f, 96.297f, 95.432f, 94.101f, 92.626f, 91.006f, 89.386f, 87.767f, 86.607f, 85.723f, 85.423f, 85.749f, 86.673f, 87.876f, 89.349f, 90.822f, 92.295f, 93.361f, 93.914f, 94.468f, 94.912f, 95.247f, 95.582f, 95.918f, 96.071f, 96.224f, 96.376f, 96.529f, 96.556f, 96.582f, 96.609f, 96.059f, 94.618f, 92.492f, 90.277f, 87.972f, 85.932f, 84.414f, 83.946f, 84.082f, 84.729f, 85.617f, 86.964f, 88.477f, 90.153f, 91.83f, 93.507f, 95.156f, 96.805f, 98.321f, 99.703f, 100.729f, 101.511f, 102.048f, 102.085f, 101.652f, 100.693f, 99.14f, 96.953f, 94.071f, 90.433f, 83.483f, 64.03f, 42.921f, 24.824f, 12.555f, -0.077f, -9.702f, -11.086f, 0.689f, 7.608f, 9.934f, 5.898f, 1.402f, 1.046f, 4.716f, 8.145f, 6.253f, 0.748f, 1.861f, 3.774f, 1.186f, 3.288f, 5.493f, 1.242f, 2.37f, 6.179f, 4.754f, 0.72f, 1.598f, 4.166f, 7.454f, 9.415f, 6.239f, 1.563f, 1.576f, 3.781f, 4.912f, 7.164f, 9.537f, 10.081f, 6.666f, 1.864f, 0.581f, 2.014f, 1.207f, -0.015f, -0.044f, -0.074f, -0.104f, -0.133f, -0.163f, -0.193f, -0.222f, -0.252f, -0.282f, -0.311f, -0.341f, -0.371f, -0.4f, -0.43f, -0.344f, -0.258f, -0.172f, -0.086f, 0f };
        private static readonly float[] LeaveSpin = { 0f, 1.109f, 2.218f, 3.327f, 4.436f, 5.431f, 6.312f, 6.863f, 7.317f, 7.672f, 8.026f, 8.221f, 8.415f, 8.514f, 8.519f, 8.523f, 8.419f, 8.207f, 7.995f, 7.678f, 7.256f, 6.834f, 6.225f, 5.615f, 4.673f, 3.567f, 2.299f, 0.902f, -0.623f, -2.148f, -3.673f, -5.198f, -6.723f, -8.116f, -9.377f, -10.475f, -11.41f, -11.963f, -12.311f, -12.453f, -12.423f, -12.221f, -11.869f, -11.367f, -10.865f, -10.109f, -9.353f, -8.597f, -7.724f, -6.852f, -5.979f, -5.106f, -4.233f, -3.36f, -2.583f, -1.902f, -1.221f, -0.808f, -0.395f, -0.126f, 0f, 5.3f, 20.757f, 45.669f, 79.342f, 121.098f, 170.235f, 226.08f, 287.917f, 355.078f, 426.855f, 502.584f, 581.553f, 663.064f, 746.449f, 831.065f, 916.022f, 1000.885f, 1086.147f, 1178.971f, 1279.55f, 1386.735f, 1499.386f, 1616.352f, 1736.48f, 1858.637f, 1981.658f, 2104.4f, 2225.728f, 2344.491f, 2459.53f, 2569.703f, 2673.871f, 2770.877f, 2859.58f, 2938.827f, 3009.075f, 3071.117f, 3124.343f, 3168.16f, 3201.932f, 3225.049f, 3236.898f, 3238.412f, 3219.364f, 3164.572f, 3077.565f, 2961.885f, 2821.052f, 2658.605f, 2478.055f, 2282.961f, 2076.844f, 1863.231f, 1645.659f, 1427.656f, 1212.762f, 1004.485f, 806.377f, 621.962f, 454.781f, 308.354f, 186.217f, 91.899f, 28.927f, 0.843f };
        private static readonly float[] LeaveSide = { 0f, 0.002f, 0.008f, 0.016f, 0.028f, 0.041f, 0.056f, 0.072f, 0.089f, 0.105f, 0.121f, 0.136f, 0.149f, 0.161f, 0.17f, 0.175f, 0.177f, 0.177f, 0.176f, 0.174f, 0.172f, 0.17f, 0.167f, 0.164f, 0.16f, 0.157f, 0.154f, 0.15f, 0.147f, 0.144f, 0.142f, 0.14f, 0.138f, 0.137f, 0.137f, 0.137f, 0.139f, 0.141f, 0.144f, 0.148f, 0.153f, 0.157f, 0.163f, 0.169f, 0.176f, 0.183f, 0.19f, 0.197f, 0.205f, 0.212f, 0.219f, 0.227f, 0.234f, 0.241f, 0.248f, 0.254f, 0.26f, 0.266f, 0.27f, 0.275f, 0.279f, 0.282f, 0.284f, 0.286f, 0.288f, 0.289f, 0.29f, 0.291f, 0.29f, 0.29f, 0.29f, 0.29f, 0.289f, 0.288f, 0.287f, 0.286f, 0.285f, 0.284f, 0.283f, 0.281f, 0.28f, 0.279f, 0.279f, 0.278f, 0.277f, 0.277f, 0.277f, 0.275f, 0.273f, 0.269f, 0.265f, 0.259f, 0.254f, 0.248f, 0.241f, 0.235f, 0.23f, 0.224f, 0.219f, 0.215f, 0.212f, 0.21f, 0.209f, 0.209f, 0.209f, 0.21f, 0.211f, 0.212f, 0.213f, 0.215f, 0.216f, 0.218f, 0.22f, 0.222f, 0.225f, 0.227f, 0.23f, 0.233f, 0.236f, 0.239f, 0.242f, 0.245f, 0.249f, 0.252f, 0.256f, 0.26f };
        private static readonly float[] LeaveUp = { 0f, 0.003f, 0.013f, 0.028f, 0.048f, 0.073f, 0.102f, 0.134f, 0.17f, 0.209f, 0.249f, 0.293f, 0.337f, 0.381f, 0.427f, 0.472f, 0.517f, 0.56f, 0.602f, 0.643f, 0.681f, 0.716f, 0.747f, 0.775f, 0.8f, 0.826f, 0.849f, 0.873f, 0.894f, 0.916f, 0.936f, 0.956f, 0.976f, 0.995f, 1.014f, 1.033f, 1.052f, 1.071f, 1.091f, 1.11f, 1.13f, 1.15f, 1.171f, 1.192f, 1.215f, 1.238f, 1.265f, 1.292f, 1.319f, 1.345f, 1.372f, 1.399f, 1.426f, 1.452f, 1.479f, 1.506f, 1.533f, 1.56f, 1.586f, 1.613f, 1.64f, 1.638f, 1.633f, 1.624f, 1.614f, 1.602f, 1.587f, 1.572f, 1.556f, 1.54f, 1.523f, 1.508f, 1.493f, 1.48f, 1.468f, 1.459f, 1.452f, 1.448f, 1.45f, 1.468f, 1.503f, 1.55f, 1.608f, 1.673f, 1.743f, 1.815f, 1.887f, 1.955f, 2.018f, 2.071f, 2.113f, 2.141f, 2.152f, 2.143f, 2.112f, 2.055f, 1.895f, 1.735f, 1.576f, 1.416f, 1.256f, 1.096f, 0.936f, 0.844f, 0.837f, 1.013f, 1.19f, 1.814f, 3.441f, 5.755f, 8.438f, 11.173f, 13.644f, 15.68f, 17.449f, 18.933f, 20.158f, 21.145f, 21.919f, 22.504f, 22.923f, 23.2f, 23.358f, 23.421f, 23.412f, 23.356f };

        // el osito: tras unos usos en el mismo sitio puede salir el oso de peluche de EFT; te devuelve el dinero y la caja se va a otro punto
        private const string TeddyTpl = "62a08f4c4f842e1bd12d9d62";            // BEAR Buddy plush toy
        private const int SafeUses = 4;                                        // los primeros usos en un sitio nunca dan el osito
        private const float TeddyTime = 2.5f, LeaveTime = 250f / 30f, DropTime = 1.1f, DropHeight = 14f;
        // sonidos (en assets/, .wav); si falta alguno simplemente no suena
        private const string SndOpen = "box_open.wav", SndMusic = "box_music.wav", SndClose = "box_close.wav",
                             SndTeddy = "box_teddy.wav", SndLeave = "box_leave.wav", SndLand = "box_land.wav";

        private enum State { Idle, Loading, Spinning, Offer, Sinking, Closing, Teddy, Leaving, Arriving }
        private static State _state;
        private static float _t0, _openedAt;
        private static Transform _lid, _box;
        private static bool _mod, _teddy, _landed;
        private static int _uses;                                              // usos desde que la caja llego a este sitio
        private static Vector3 _basePos, _pivot;
        private static Quaternion _baseRot;
        private static Light _inner;                                           // la luz de dentro mientras esta abierta
        private static Item _prize;
        private static readonly List<Item> _show = new List<Item>();           // lo que pasa por la caja (el premio es el ultimo)
        private static readonly List<GameObject> _models = new List<GameObject>();
        private static GameObject _float;                                      // contenedor que sube y gira
        private static int _shown = -1;
        private static LootItem _loot;
        private static List<string> _weapons, _mods;
        private static int _run;

        internal static bool Busy { get { return _state != State.Idle; } }

        internal static void OnRaidStart() { _state = State.Idle; _lid = null; _box = null; _loot = null; _float = null; _inner = null; _models.Clear(); _show.Clear(); _uses = 0; _run++; }

        internal static string Prompt(string key)
        {
            if (_state == State.Loading || _state == State.Spinning || _state == State.Teddy) return "<b>" + L.T("box.name").ToUpperInvariant() + "</b>";
            if (_state == State.Offer && _prize != null) return L.F("box.take", _prize.ShortName.Localized());
            if (_state != State.Idle) return null;
            return L.F("box.use", key, Plugin.CfgBoxModKey.Value, Price.ToString("N0"));
        }

        private static string Name(string tpl) { try { return (tpl + " Name").Localized(); } catch { return tpl; } }

        internal static void Use(Player me, GameObject box, bool mod)
        {
            if (Busy || box == null) return;
            BuildPools();
            var pool = mod ? _mods : _weapons;
            if (pool.Count == 0) { Plugin.Log.LogWarning("Caja misteriosa: no hay " + (mod ? "accesorios" : "armas") + " para elegir"); return; }
            if (!Machines.Charge(me, Price, L.T("box.name"), null)) return;
            _box = box.transform; _lid = _box.Find("lid"); _mod = mod;

            // el osito: nunca en los primeros usos ni si la caja no tiene otro sitio al que irse; luego cada vez mas probable (hasta 50%)
            _uses++;
            _teddy = Machines.BoxSpots > 1 && _uses > SafeUses && Random.value < Mathf.Min(0.5f, (_uses - SafeUses) * 0.12f);

            _show.Clear();
            var f = Singleton<ItemFactory>.Instance;
            for (int i = 0; i <= Decoys; i++)
            {
                string tpl = pool[Random.Range(0, pool.Count)];
                var it = mod ? f.CreateItem(NewId(), tpl, null) : f.GetPresetItem(tpl);
                if (it != null) _show.Add(it);
            }
            // armas de BO7 (Bo7Guns): premio especial, un 15% de las veces que se pide arma
            if (!mod && !_teddy && Random.value < 0.15f)
            {
                var bo7 = f.GetPresetItem(Random.value < 0.5f ? Bo7Guns.Mk35 : Bo7Guns.Vmp);
                if (bo7 != null) { if (_show.Count > Decoys) _show.RemoveAt(_show.Count - 1); _show.Add(bo7); }
            }
            if (_teddy) { var bear = f.CreateItem(NewId(), TeddyTpl, null); if (bear != null) { if (_show.Count > Decoys) _show.RemoveAt(_show.Count - 1); _show.Add(bear); } else _teddy = false; }
            if (_show.Count == 0) { _state = State.Idle; return; }
            _prize = _teddy ? null : _show[_show.Count - 1];
            _state = State.Loading; _t0 = _openedAt = Time.time;
            Jingle.At(SndOpen, Vector3.zero, 3f, _box);
            Glow(true);
            Plugin.Instance.StartCoroutine(Load(++_run));
            Dbg.Log("CAJA", (mod ? "accesorio" : "arma") + " por " + Price + " rublos (uso " + _uses + " aqui): " + (_teddy ? "OSITO, la caja se va" : "sale " + _prize.TemplateId + " (" + Name(_prize.TemplateId.ToString()) + ")") + ", entre " + pool.Count);
        }

        private static string NewId() { return System.Guid.NewGuid().ToString("N").Substring(0, 24); }

        // luz azulada dentro de la caja, que se enciende con la tapa abierta
        private static void Glow(bool on)
        {
            if (!on) { if (_inner != null) Object.Destroy(_inner.gameObject); _inner = null; return; }
            if (_inner != null || _box == null) return;
            var go = new GameObject("TarkovaColaBoxLight");
            go.transform.SetParent(_box, false);
            go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            _inner = go.AddComponent<Light>();
            _inner.type = LightType.Point; _inner.color = new Color(0.55f, 0.8f, 1f); _inner.range = 3.5f; _inner.intensity = 0f; _inner.shadows = LightShadows.None;
        }

        // modelos de lo que va a pasar por la caja (si no estan cargados en este mapa, se cargan ahora)
        private static IEnumerator Load(int run)
        {
            System.Threading.Tasks.Task task = null;
            try
            {
                var keys = _show.SelectMany(i => i.GetAllItems()).Select(i => i.Template.Prefab).Where(k => k != null && !string.IsNullOrEmpty(k.path))
                                .GroupBy(k => k.path).Select(g => g.First()).ToArray();
                task = Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(ObjectsFactory.PoolsCategory.Raid, ObjectsFactory.AssemblyType.Local,
                    keys, Diz.Jobs.JobYieldPriority.General, null, ObjectsFactory.DefaultCancellationToken);
            }
            catch (System.Exception e) { Plugin.Log.LogWarning("Caja misteriosa: no se pudieron pedir los modelos: " + e.Message); }
            float t = Time.time;
            while (task != null && !task.IsCompleted && Time.time - t < LoadTimeout) yield return null;
            if (run != _run || _box == null) yield break;
            if (task != null && task.IsFaulted) Plugin.Log.LogWarning("Caja misteriosa: error cargando modelos: " + task.Exception?.GetBaseException().Message);
            BuildModels();
            _state = State.Spinning; _t0 = Time.time;
            Jingle.At(SndMusic, Vector3.zero, SpinTime + 0.6f, _box);
        }

        private static void BuildModels()
        {
            _float = new GameObject("TarkovaColaBoxFloat");
            _float.transform.SetParent(_box, false);
            _float.transform.localPosition = new Vector3(0f, LowY, 0f);
            _models.Clear();
            var cam = Player.GetVisibleToCamera(Plugin.Me);
            foreach (var it in _show)
            {
                GameObject go = null;
                try { go = Singleton<ObjectsFactory>.Instance.CreateLootPrefab(it, cam); } catch (System.Exception e) { Dbg.Throttled("boxmodel", 5f, "CAJA", "sin modelo para " + it.TemplateId + ": " + e.Message); }
                if (go != null)
                {
                    foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                    foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
                    go.transform.SetParent(_float.transform, false);
                    go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
                    if (_teddy && it == _show[_show.Count - 1]) go.transform.localScale = Vector3.one * 3f;   // el peluche es pequeno
                    // centrado: el centro de sus mallas en el contenedor (activo: un objeto inactivo da cajas vacias en el origen del mundo
                    // y el modelo acababa lejisimos de la caja)
                    go.SetActive(true);
                    var rs = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.bounds.size.sqrMagnitude > 0f).ToArray();
                    if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); go.transform.position -= b.center - _float.transform.position; }
                    go.SetActive(false);
                }
                _models.Add(go);
            }
            _shown = -1;
        }

        private static void ShowModel(int i)
        {
            if (i == _shown) return;
            for (int k = 0; k < _models.Count; k++) if (_models[k] != null) _models[k].SetActive(k == i);
            _shown = i;
        }

        internal static void Tick(Player me)
        {
            if (_state == State.Idle) return;
            if (_box == null) { Cleanup(); _state = State.Idle; return; }
            float t = Time.time - _t0;
            if (_state < State.Closing || _state == State.Teddy)
            {
                float lid = Curve(OpenT, OpenA, Time.time - _openedAt);
                SetLid(lid);
                if (_inner != null) _inner.intensity = Mathf.Clamp01(lid / 90f) * 1.6f;
            }
            switch (_state)
            {
                case State.Loading:
                    return;
                case State.Spinning:
                {
                    float k = Mathf.Clamp01(t / SpinTime);
                    _float.transform.localPosition = new Vector3(0f, Mathf.Lerp(LowY, HighY, Mathf.SmoothStep(0f, 1f, k)), 0f);
                    _float.transform.localRotation = Quaternion.Euler(0f, t * 140f * (1f - k * 0.8f), 0f);
                    // cambia de modelo cada vez mas despacio (de 0.08 s a 0.45 s); el ultimo es el premio
                    int n = _models.Count - 1;
                    int idx = Mathf.Min(n, Mathf.FloorToInt(Mathf.Pow(k, 0.6f) * n));
                    if (k < 1f && idx == n) idx = n - 1;
                    ShowModel(k >= 1f ? n : idx);
                    if (t < SpinTime) return;
                    if (_teddy) Teddy(me); else Offer(me);
                    return;
                }
                case State.Teddy:
                    // el osito flota un momento balanceandose; luego la caja se va
                    if (_float != null) _float.transform.localRotation = Quaternion.Euler(0f, 180f + Mathf.Sin(t * 5f) * 25f, Mathf.Sin(t * 3.3f) * 10f);
                    if (t >= TeddyTime) Leave();
                    return;
                case State.Leaving:
                {
                    // t10_zm_mystery_box_leaving_fxanim: la tapa tiembla, la caja sube dando tumbos y sale disparada hacia el cielo
                    float fr = Mathf.Min(t, LeaveTime) * 15f;
                    float spin = Sample(LeaveSpin, fr);
                    var rot = _baseRot * Quaternion.Euler(spin, 0f, 0f);
                    var off = new Vector3(0f, Sample(LeaveUp, fr), -Sample(LeaveSide, fr));
                    _box.SetPositionAndRotation(_basePos + _baseRot * off + _baseRot * _pivot - rot * _pivot, rot);
                    SetLid(Sample(LeaveLid, fr));
                    if (_inner != null) _inner.intensity = Mathf.Clamp01(1f - t / 4f) * 1.6f;
                    if (t >= LeaveTime) Arrive();
                    return;
                }
                case State.Arriving:
                {
                    // cae desde el cielo sobre su nuevo sitio y rebota un poco
                    float k = Mathf.Clamp01(t / DropTime);
                    float h = k < 1f ? DropHeight * (1f - k * k) : Mathf.Abs(Mathf.Sin((t - DropTime) * 14f)) * 0.12f * Mathf.Max(0f, 1f - (t - DropTime) / 0.5f);
                    _box.position = _basePos + Vector3.up * h;
                    if (k >= 1f && !_landed) { _landed = true; Jingle.At(SndLand, _basePos, 4f); }
                    if (t >= DropTime + 0.5f) { _box.position = _basePos; _state = State.Idle; }
                    return;
                }
                case State.Offer:
                    if (_loot == null || !_loot.gameObject.activeInHierarchy) { Dbg.Log("CAJA", "premio recogido"); Close(); return; }   // lo cogiste
                    if (t >= OfferTime) { _state = State.Sinking; _t0 = Time.time; }
                    return;
                case State.Sinking:
                    if (_loot != null && _loot.gameObject.activeInHierarchy)
                        _loot.transform.position = _box.TransformPoint(new Vector3(0f, Mathf.Lerp(HighY, LowY - 0.3f, t / SinkTime), 0f));
                    if (t < SinkTime) return;
                    if (_loot != null && _loot.gameObject.activeInHierarchy) { try { Singleton<GameWorld>.Instance.DestroyLoot(_loot); } catch { Object.Destroy(_loot.gameObject); } }
                    Dbg.Log("CAJA", "premio no recogido: vuelve a la caja");
                    Close();
                    return;
                case State.Closing:
                    SetLid(Curve(CloseT, CloseA, t));
                    if (_inner != null) _inner.intensity = Mathf.Clamp01(1f - t / CloseT[CloseT.Length - 1]) * 1.6f;
                    if (t >= CloseT[CloseT.Length - 1]) { Glow(false); _state = State.Idle; }
                    return;
            }
        }

        // el osito: te devuelve el dinero (al inventario, o al suelo si no cabe)
        private static void Teddy(Player me)
        {
            _state = State.Teddy; _t0 = Time.time;
            ShowModel(_models.Count - 1);
            Jingle.At(SndTeddy, Vector3.zero, 4f, _box);
            if (!NewPerkUtil.Give(me.Profile, Roubles, Price))
            {
                var cash = Singleton<ItemFactory>.Instance.CreateItem(NewId(), Roubles, null);
                if (cash != null) { cash.StackObjectsCount = Price; Singleton<GameWorld>.Instance.ThrowItem(cash, me, _box.TransformPoint(new Vector3(0f, 1.2f, -0.6f)), _box.rotation, _box.TransformDirection(new Vector3(0f, 1f, -2f)), Vector3.zero, true); }
            }
            Hud.Notify(L.T("box.name").ToUpperInvariant(), L.F("box.teddy", Price.ToString("N0")), null);
        }

        private static void Leave()
        {
            Cleanup();
            _state = State.Leaving; _t0 = Time.time;
            _basePos = _box.position; _baseRot = _box.rotation;
            // gira sobre el centro de sus mallas (no sobre la base)
            _pivot = new Vector3(0f, 0.4f, 0f);
            var rs = _box.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); _pivot = Quaternion.Inverse(_baseRot) * (b.center - _basePos); }
            Jingle.At(SndLeave, _basePos, LeaveTime + 0.5f);
            Dbg.Log("CAJA", "osito: la caja se va");
        }

        private static void Arrive()
        {
            Glow(false);
            var old = _box.gameObject;
            var nu = Machines.MoveBox(old);
            if (nu == null)
            {
                // sin otro sitio: vuelve a caer donde estaba
                _box.SetPositionAndRotation(_basePos, _baseRot); SetLid(0f);
                nu = old;
            }
            _box = nu.transform; _lid = _box.Find("lid"); SetLid(0f);
            _basePos = _box.position; _uses = 0; _landed = false;
            _box.position = _basePos + Vector3.up * DropHeight;
            _state = State.Arriving; _t0 = Time.time;
            Hud.Notify(L.T("box.name").ToUpperInvariant(), L.T("box.moved"), null);
        }

        // muestra de una curva tomada cada 2 fotogramas, en fotogramas/2 (fraccional)
        private static float Sample(float[] c, float i)
        {
            int a = Mathf.Clamp((int)i, 0, c.Length - 1), b = Mathf.Min(a + 1, c.Length - 1);
            return Mathf.Lerp(c[a], c[b], i - a);
        }

        // el premio de verdad: un objeto del suelo quieto (sin fisica) flotando en la caja, que se coge con la F de EFT
        private static void Offer(Player me)
        {
            var world = Singleton<GameWorld>.Instance;
            var pos = _float.transform.position;
            Cleanup();
            _loot = world != null && _prize != null ? world.SetupItem(_prize, me, pos, _box.rotation) : null;
            if (_loot == null)
            {
                // si no se pudo dejar flotando, sale despedido hacia el frente como antes
                if (world != null && _prize != null) world.ThrowItem(_prize, me, pos, _box.rotation, _box.TransformDirection(new Vector3(0f, 1.2f, -2.2f)), Vector3.zero, true);
                Close(); return;
            }
            _state = State.Offer; _t0 = Time.time;
            Hud.Notify(L.T("box.name").ToUpperInvariant(), _prize.ShortName.Localized() + " · " + _prize.Name.Localized(), null);
            Dbg.Log("CAJA", "premio flotando: " + _prize.Name.Localized());
        }

        private static void Close() { Cleanup(); _loot = null; _state = State.Closing; _t0 = Time.time; Jingle.At(SndClose, Vector3.zero, 3f, _box); }

        private static void Cleanup()
        {
            if (_float != null) Object.Destroy(_float);
            _float = null; _models.Clear(); _shown = -1;
        }

        private static void SetLid(float deg) { if (_lid != null) _lid.localRotation = Quaternion.Euler(deg, 0f, 0f); }

        private static float Curve(float[] ts, float[] vs, float t)
        {
            if (t <= ts[0]) return vs[0];
            for (int i = 0; i + 1 < ts.Length; i++)
                if (t < ts[i + 1]) return Mathf.Lerp(vs[i], vs[i + 1], (t - ts[i]) / (ts[i + 1] - ts[i]));
            return vs[vs.Length - 1];
        }

        // armas con preset (montadas) y accesorios de arma; sin objetos de mision ni los que no se venden en el mercadillo
        private static void BuildPools()
        {
            if (_weapons != null) return;
            _weapons = new List<string>(); _mods = new List<string>();
            var f = Singleton<ItemFactory>.Instance;
            foreach (var kv in f.ItemTemplates)
            {
                var t = kv.Value;
                if (t == null || t.QuestItem || !t.CanSellOnRagfair) continue;
                if (t is WeaponTemplate) { if (f._itemPresets != null && f._itemPresets.ContainsKey(kv.Key)) _weapons.Add(kv.Key.ToString()); }
                else if (t is ModTemplate && Under(f, t, MasterMod)) _mods.Add(kv.Key.ToString());
            }
            Dbg.Log("CAJA", _weapons.Count + " armas y " + _mods.Count + " accesorios posibles");
        }

        private static bool Under(ItemFactory f, ItemTemplate t, string node)
        {
            for (int i = 0; i < 12 && t != null && t.ParentId.HasValue; i++)
            {
                if (t.ParentId.Value.ToString() == node) return true;
                ItemTemplate p;
                t = f.ItemTemplates.TryGetValue(t.ParentId.Value, out p) ? p : null;
            }
            return false;
        }
    }
}
