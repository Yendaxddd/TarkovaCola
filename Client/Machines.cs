using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Maquinas de perks en los mapas (modelos de Black Ops 3, assets/machines.bundle).
    //   - Al empezar la raid se colocan las de machines.json (servidor) que son de este mapa.
    //   - Editor (F12 > Herramientas > Editor de maquinas): una maquina "fantasma" sigue tu mira; con teclas se gira, se cambia
    //     de perk, se coloca o se borra la mas cercana, y se guarda en el servidor al momento. Tecla de foto: captura de pantalla
    //     en BepInEx/plugins/TarkovaCola/photos (para revisar si queda bien).
    internal static class Machines
    {
        internal class Spot { public string map, perk; public float x, y, z, rot; }

        internal const string Power = "power";               // el interruptor de la luz del mapa (se coloca con el editor como una pieza mas)
        internal const string Wunderfizz = "wunderfizz";     // Der Wunderfizz: una perk al azar (la unica maquina de Bandolier, Vulture Aid y Melee Macchiato)
        // la caja misteriosa y TEDD se colocan en varios puntos, pero en cada raid sale solo una caja (en uno al azar) y TEDD solo a
        // veces (Plugin.CfgTeddChance). No necesitan la luz.
        internal const string Box = "mysterybox", Tedd = "tedd";
        private static readonly string[] Ids = { "speedcola", "staminup", "juggernog", "phdflopper", "widowswine", "deadshot", Wunderfizz, Box, Tedd, Power };
        private const float MaxDistance = 15f;
        internal const int Price = 30000, EternalPrice = 15000;     // rublos
        internal const int WunderPrice = 40000;
        private const float RollTime = 3f;                   // la ruleta de la Wunderfizz
        private static PerkDef _rollPerk;
        private static float _rollUntil;
        private const string Roubles = "5449016a4bdc2d6f028b456f";
        private const float UseRange = 2.2f, HoldTime = 1.2f;

        // energia: sin interruptor en el mapa (o con la opcion apagada) las maquinas funcionan desde el principio
        private static bool _powered;
        private static float _hold;
        private static Spot _target;
        private static float _cooldown;
        private static string _prompt;

        private static AssetBundle _bundle;
        private static bool _bundleFailed;
        private static readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
        private static List<Spot> _all = new List<Spot>();
        private static readonly List<KeyValuePair<Spot, GameObject>> _placed = new List<KeyValuePair<Spot, GameObject>>();
        private static string _map;

        // editor
        private static GameObject _ghost;
        private static int _ghostPerk;
        private static float _ghostRot;
        private static bool _ghostValid;
        private static Vector3 _ghostPos;
        private static string _status;
        private static float _statusUntil;

        private static readonly int RayMask = LayerMask.GetMask("HighPolyCollider", "LowPolyCollider", "Terrain", "Default");

        // ---------------------------------------------------------------- modelos
        private static GameObject Prefab(string perk)
        {
            GameObject p;
            if (_prefabs.TryGetValue(perk, out p) && p != null) return p;
            if (_bundle == null && !_bundleFailed)
            {
                string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", "machines.bundle"));
                _bundle = AssetBundle.LoadFromFile(path);
                if (_bundle == null) { _bundleFailed = true; Plugin.Log.LogWarning("Maquinas: no se pudo cargar " + path); }
            }
            if (_bundle == null) return null;
            p = _bundle.LoadAsset<GameObject>(perk == Power ? "power_switch" : "machine_" + perk);
            _prefabs[perk] = p;
            return p;
        }

        private static GameObject SwitchModel()
        {
            var root = new GameObject("power_switch");
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = new Vector3(0.45f, 0.7f, 0.18f); box.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            box.GetComponent<Renderer>().material.color = new Color(0.28f, 0.3f, 0.27f);
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "lever";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localScale = new Vector3(0.06f, 0.35f, 0.06f); handle.transform.localPosition = new Vector3(0f, 1.3f, -0.14f);
            handle.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            handle.GetComponent<Renderer>().material.color = new Color(0.75f, 0.1f, 0.08f);
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(0.06f, 0.5f, 0.06f); pole.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            pole.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f);
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(c);
            var col = root.AddComponent<BoxCollider>(); col.center = new Vector3(0f, 1.2f, 0f); col.size = new Vector3(0.5f, 1.6f, 0.3f);
            return root;
        }

        private static readonly Dictionary<string, Color> LightColor = new Dictionary<string, Color>
        {
            ["speedcola"] = new Color(0.3f, 1f, 0.4f), ["staminup"] = new Color(1f, 0.65f, 0.2f), ["juggernog"] = new Color(1f, 0.25f, 0.2f),
            ["phdflopper"] = new Color(0.75f, 0.4f, 1f), ["widowswine"] = new Color(1f, 0.3f, 0.45f), ["deadshot"] = new Color(1f, 0.85f, 0.3f),
            [Wunderfizz] = new Color(0.55f, 0.6f, 1f), [Box] = new Color(0.4f, 0.7f, 1f), [Tedd] = new Color(0.4f, 1f, 0.6f),
        };

        private static GameObject Spawn(Spot s, bool ghost)
        {
            GameObject go;
            if (s.perk == Power)
            {
                var sw = Prefab(Power);                                       // el de BO6 (si no esta en el bundle, uno simple hecho en codigo)
                go = sw != null ? UnityEngine.Object.Instantiate(sw) : SwitchModel();
                go.transform.SetPositionAndRotation(new Vector3(s.x, s.y, s.z), Quaternion.Euler(0f, s.rot, 0f));
            }
            else
            {
                var prefab = Prefab(s.perk);
                if (prefab == null) return null;
                go = UnityEngine.Object.Instantiate(prefab, new Vector3(s.x, s.y, s.z), Quaternion.Euler(0f, s.rot, 0f));
                Color c;
                if (!ghost && LightColor.TryGetValue(s.perk, out c))
                {
                    var lg = new GameObject("glow");
                    lg.transform.SetParent(go.transform, false);
                    lg.transform.localPosition = new Vector3(0f, 2.0f, -0.8f);       // delante y arriba (el frente mira a -Z)
                    var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = Color.Lerp(c, Color.white, 0.5f); l.range = 2.2f; l.intensity = 0.45f; l.shadows = LightShadows.None;
                }
            }
            go.name = (ghost ? "TarkovaCola_GhostMachine_" : "TarkovaCola_Machine_") + s.perk;
            if (ghost) foreach (var c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(c);
            return go;
        }

        // ---------------------------------------------------------------- raid
        internal static void OnRaidStart()
        {
            _placed.Clear();                       // (los objetos de la raid anterior ya no existen)
            _boxSpots = new List<Spot>();
            _ghost = null;
            _rollPerk = null;
            _map = null;
            MysteryBox.OnRaidStart();
            Trials.OnRaidStart();
        }

        // Factory de noche comparte las posiciones de Factory de dia
        private static string MapKey(string id) { return string.Equals(id, "factory4_night", StringComparison.OrdinalIgnoreCase) ? "factory4_day" : id; }

        // Tracker: el jugador ya existe
        internal static void OnPlayerReady(Player me)
        {
            try
            {
                var world = Singleton<GameWorld>.Instance;
                _map = world != null ? MapKey(world.LocationId) : null;
                var resp = JObject.Parse(RequestHandler.PostJson("/tarkovacola/machines/get", "{}"));
                _all = JsonConvert.DeserializeObject<List<Spot>>((string)resp["json"] ?? "[]") ?? new List<Spot>();
                int n = 0;
                bool hasSwitch = _all.Any(s => s.perk == Power && string.Equals(MapKey(s.map), _map, StringComparison.OrdinalIgnoreCase));
                _powered = !Plugin.CfgMachinesNeedPower.Value || !hasSwitch;
                if (!Plugin.CfgMachinesEnabled.Value && !Plugin.CfgMachineEditor.Value) { Dbg.Log("MAQUINAS", "desactivadas en F12"); return; }
                var here = _all.Where(s => string.Equals(MapKey(s.map), _map, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!Plugin.CfgMachineEditor.Value) here = PickRandom(here);
                foreach (var s in here)
                {
                    var go = Spawn(s, false);
                    if (go != null) { _placed.Add(new KeyValuePair<Spot, GameObject>(s, go)); n++; }
                }
                var tedd = _placed.FirstOrDefault(p => p.Key.perk == Tedd);
                if (tedd.Value != null) Trials.OnTeddSpawned(tedd.Value);
                SetPower(_powered);
                Dbg.Log("MAQUINAS", _map + ": " + n + " piezas colocadas (" + _all.Count + " en total), energia: " + (_powered ? "si" : "no (falta el interruptor)"));
            }
            catch (Exception e) { Plugin.Log.LogError("Maquinas (inicio): " + e); }
        }

        // de los puntos de la caja, uno al azar; de los de TEDD, uno al azar y solo con la probabilidad de F12
        private static List<Spot> PickRandom(List<Spot> spots)
        {
            var res = spots.Where(s => s.perk != Box && s.perk != Tedd).ToList();
            var boxes = _boxSpots = spots.Where(s => s.perk == Box).ToList();
            if (boxes.Count > 0) res.Add(boxes[UnityEngine.Random.Range(0, boxes.Count)]);
            var tedds = spots.Where(s => s.perk == Tedd).ToList();
            bool tedd = tedds.Count > 0 && UnityEngine.Random.value * 100f < Plugin.CfgTeddChance.Value;
            if (tedd) res.Add(tedds[UnityEngine.Random.Range(0, tedds.Count)]);
            Dbg.Log("MAQUINAS", "caja misteriosa: " + boxes.Count + " punto(s); TEDD: " + tedds.Count + " punto(s), " + (tedd ? "aparece" : "no aparece esta raid"));
            return res;
        }

        // la caja se va (el osito): aparece en otro de sus puntos de este mapa. Devuelve la caja nueva (null si no hay otro sitio)
        private static List<Spot> _boxSpots = new List<Spot>();
        internal static int BoxSpots { get { return _boxSpots.Count; } }

        internal static GameObject MoveBox(GameObject old)
        {
            int i = _placed.FindIndex(p => p.Value == old);
            if (i < 0) return null;
            var others = _boxSpots.Where(s => s != _placed[i].Key).ToList();
            if (others.Count == 0) return null;
            var spot = others[UnityEngine.Random.Range(0, others.Count)];
            var go = Spawn(spot, false);
            if (go == null) return null;
            UnityEngine.Object.Destroy(old);
            _placed[i] = new KeyValuePair<Spot, GameObject>(spot, go);
            Dbg.Log("MAQUINAS", "la caja se mueve a " + spot.x.ToString("0") + ", " + spot.y.ToString("0") + ", " + spot.z.ToString("0"));
            return go;
        }

        private static readonly Dictionary<Material, Color> _emission = new Dictionary<Material, Color>();

        private static void SetPower(bool on)
        {
            _powered = on;
            foreach (var kv in _placed)
            {
                if (kv.Value == null || kv.Key.perk == Power || kv.Key.perk == Box || kv.Key.perk == Tedd) continue;
                foreach (var l in kv.Value.GetComponentsInChildren<Light>(true)) l.enabled = on;
                // maquinas de BO6: dos modelos, apagada ("off") y encendida ("on")
                var onT = kv.Value.transform.Find("on"); var offT = kv.Value.transform.Find("off");
                if (onT != null && offT != null) { onT.gameObject.SetActive(on); offT.gameObject.SetActive(!on); continue; }
                foreach (var r in kv.Value.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.materials)
                    {
                        if (!m.HasProperty("_EmissionColor")) continue;
                        Color orig;
                        if (!_emission.TryGetValue(m, out orig)) _emission[m] = orig = m.GetColor("_EmissionColor");
                        m.SetColor("_EmissionColor", on ? orig : Color.black);
                    }
                if (kv.Key.perk == Power) continue;
            }
            foreach (var kv in _placed)
                if (kv.Key.perk == Power && kv.Value != null)
                {
                    var lever = kv.Value.transform.Find("lever");
                    if (lever != null && kv.Value.transform.Find("body") == null) lever.localRotation = Quaternion.Euler(on ? 35f : -35f, 0f, 0f);
                }
        }

        private static void Save()
        {
            try
            {
                var body = new JObject { ["json"] = JsonConvert.SerializeObject(_all) }.ToString(Formatting.None);
                RequestHandler.PostJsonAsync("/tarkovacola/machines/save", body).ContinueWith(t =>
                {
                    if (t.IsFaulted) Plugin.Log.LogWarning("Maquinas: no se pudo guardar: " + t.Exception?.GetBaseException().Message);
                });
            }
            catch (Exception e) { Plugin.Log.LogError("Maquinas (guardar): " + e); }
        }

        // ---------------------------------------------------------------- usar (mantener F)
        private static void TickUse(Player me, Camera cam)
        {
            _prompt = null;
            if (_rollPerk != null)
            {
                // la ruleta: van pasando nombres de perks y al final sale la elegida
                if (Time.time < _rollUntil)
                {
                    var all = Perks.All;
                    _prompt = "<b>DER WUNDERFIZZ</b>  ·  " + all[(int)(Time.time * 12f) % all.Length].Name;
                    return;
                }
                var def = _rollPerk; _rollPerk = null;
                Hud.Notify("DER WUNDERFIZZ", def.Name, def.Id);
                Dbg.Log("MAQUINAS", "Wunderfizz: sale " + def.Id);
                DrinkAnim.Play(def, () => { if (Plugin.InRaid) PerkGrant.Apply(def, Plugin.Me, false); });
                return;
            }
            if (_placed.Count == 0 || !Plugin.CfgMachinesEnabled.Value) { _hold = 0f; return; }
            Spot best = null; float bestD = UseRange;
            foreach (var kv in _placed)
            {
                if (kv.Value == null) continue;
                var p = kv.Value.transform.position;
                Vector3 to = p + Vector3.up * 1.1f - cam.transform.position;
                float d = new Vector2(p.x - me.Position.x, p.z - me.Position.z).magnitude;
                if (d > bestD || Vector3.Dot(cam.transform.forward, to.normalized) < 0.55f) continue;
                best = kv.Key; bestD = d;
            }
            if (best != _target) { _target = best; _hold = 0f; }
            if (best == null) return;

            string key = "F";
            if (best.perk == Power)
            {
                if (_powered) { _prompt = L.T("machine.power.on"); return; }
                _prompt = L.F("machine.power.hold", key);
            }
            else if (best.perk == Wunderfizz)
            {
                if (!_powered) { _prompt = L.F("machine.nopower", "Der Wunderfizz"); _hold = 0f; return; }
                if (WunderPool().Count == 0) { _prompt = L.T("machine.wunder.none"); _hold = 0f; return; }
                _prompt = L.F("machine.wunder", key, WunderPrice.ToString("N0"));
            }
            else if (best.perk == Box)
            {
                _prompt = MysteryBox.Prompt(key);
                if (MysteryBox.Busy) { _hold = 0f; return; }
            }
            else if (best.perk == Tedd)
            {
                _prompt = Trials.Prompt(me, key);
                if (!Trials.CanUse) { _hold = 0f; return; }
            }
            else
            {
                var def = Perks.Get(best.perk);
                if (!_powered) { _prompt = L.F("machine.nopower", def.Name); _hold = 0f; return; }
                string why = Reason(def, out int price);
                if (why != null) { _prompt = why; _hold = 0f; return; }
                _prompt = L.F(price == EternalPrice ? "machine.buy.eternal" : "machine.buy", key, def.Name, price.ToString("N0"));
            }
            if (Time.time < _cooldown) return;
            bool alt = best.perk == Box && Input.GetKey(Plugin.CfgBoxModKey.Value);           // caja: F = arma, la otra tecla = accesorio
            if (Input.GetKey(KeyCode.F) || alt) _hold += Time.deltaTime; else _hold = 0f;
            if (_hold < HoldTime) return;
            _hold = 0f; _cooldown = Time.time + 2f;
            if (best.perk == Box) { MysteryBox.Use(me, _placed.First(p => p.Key == best).Value, alt); return; }
            if (best.perk == Tedd) { Trials.Use(me); return; }
            if (best.perk == Power) { SetPower(true); Hud.Notify(L.T("machine.power.title"), L.T("machine.power.sub"), null); Dbg.Log("MAQUINAS", "luz encendida en " + _map); return; }
            if (best.perk == Wunderfizz) { Roll(me); return; }
            Buy(me, Perks.Get(best.perk));
        }

        // perks que puede dar la Wunderfizz: las que podrias comprar ahora mismo, sin volver eterna una que ya llevas
        private static List<PerkDef> WunderPool()
        {
            return Perks.All.Where(d => !Perks.IsActive(d.Id) && Reason(d, out int _) == null).ToList();
        }

        private static void Roll(Player me)
        {
            var pool = WunderPool();
            if (pool.Count == 0 || !Charge(me, WunderPrice, "Der Wunderfizz", null)) return;
            _rollPerk = pool[UnityEngine.Random.Range(0, pool.Count)];
            _rollUntil = Time.time + RollTime;
            _cooldown = _rollUntil + 1f;
            Dbg.Log("MAQUINAS", "Wunderfizz: " + WunderPrice + " rublos, ruleta entre " + pool.Count + " perks");
        }

        // null si se puede comprar (y el precio); si no, el motivo
        private static string Reason(PerkDef def, out int price)
        {
            price = Price;
            if (ClassicMode.Enabled) return ClassicMode.DrinkBlockReason(def);
            if (Eternal.Is(def.Id)) return L.F("eternal.block", def.Name);
            if (Perks.IsActive(def.Id)) price = EternalPrice;
            return null;
        }

        private static void Buy(Player me, PerkDef def)
        {
            if (Reason(def, out int price) != null) return;
            if (!Charge(me, price, def.Name, def.Id)) return;
            Dbg.Log("MAQUINAS", def.Id + " comprada por " + price + " rublos" + (price == EternalPrice ? " (eterna)" : ""));
            DrinkAnim.Play(def, () => { if (Plugin.InRaid) PerkGrant.Apply(def, Plugin.Me, false); });
        }

        // cobra en rublos de lo que llevas encima; false (y aviso) si no te llega
        internal static bool Charge(Player me, int price, string what, string icon)
        {
            var stacks = me.Profile.Inventory.GetPlayerItems(EFT.InventoryLogic.EPlayerItems.Equipment)
                .Where(i => i != null && i.StringTemplateId == Roubles).OrderBy(i => i.StackObjectsCount).ToList();
            long have = stacks.Sum(i => (long)i.StackObjectsCount);
            if (have < price) { Hud.Notify(what.ToUpperInvariant(), L.F("machine.poor", (price - have).ToString("N0")), icon); return false; }

            int left = price;
            var ctrl = me.InventoryController;
            foreach (var st in stacks)
            {
                if (left <= 0) break;
                int take = Math.Min(left, st.StackObjectsCount);
                if (take == st.StackObjectsCount)
                {
                    var op = EFT.InventoryLogic.ItemManipulator.Remove(st, ctrl, true);
                    if (op.Failed) { Plugin.Log.LogWarning("Maquinas: no se pudo cobrar (" + op.Error + ")"); return false; }
                    ctrl.TryRunNetworkTransaction(op);
                }
                else
                {
                    // partir un fajo "a ninguna parte" no se puede mandar como transaccion de red: se aplica aqui mismo
                    // (en SPT el perfil se guarda desde el cliente al acabar la raid)
                    var op = EFT.InventoryLogic.ItemManipulator.SplitToNowhere(st, take, ctrl, ctrl, false);
                    if (op.Failed) { Plugin.Log.LogWarning("Maquinas: no se pudo cobrar (" + op.Error + ")"); return false; }
                }
                left -= take;
            }
            return true;
        }

        // ---------------------------------------------------------------- editor (Plugin.Update)
        internal static void Tick()
        {
            var me0 = Plugin.Me; var cam0 = Powerup.Cam();
            if (Plugin.InRaid && !Plugin.CfgMachineEditor.Value && me0 != null && cam0 != null) TickUse(me0, cam0);
            if (Plugin.InRaid && me0 != null)
            {
                try { MysteryBox.Tick(me0); } catch (Exception e) { Dbg.Throttled("box", 10f, "ERROR", "caja misteriosa: " + e); }
                try { Trials.Tick(me0); } catch (Exception e) { Dbg.Throttled("tedd", 10f, "ERROR", "TEDD: " + e); }
            }
            bool on = Plugin.InRaid && Plugin.CfgMachineEditor.Value;
            if (!on) { if (_ghost != null) { UnityEngine.Object.Destroy(_ghost); _ghost = null; } return; }
            var me = Plugin.Me;
            var cam = Powerup.Cam();
            if (me == null || cam == null) return;

            if (Input.GetKeyDown(Plugin.CfgMachineNext.Value)) { _ghostPerk = (_ghostPerk + 1) % Ids.Length; Rebuild(); }
            if (Input.GetKeyDown(Plugin.CfgMachinePrev.Value)) { _ghostPerk = (_ghostPerk + Ids.Length - 1) % Ids.Length; Rebuild(); }
            if (Input.GetKey(Plugin.CfgMachineRotL.Value)) _ghostRot -= 90f * Time.deltaTime;
            if (Input.GetKey(Plugin.CfgMachineRotR.Value)) _ghostRot += 90f * Time.deltaTime;
            if (Input.GetKeyDown(Plugin.CfgMachineSnap.Value)) _ghostRot = Mathf.Round(_ghostRot / 45f) * 45f;

            // donde mira el jugador
            RaycastHit hit;
            _ghostValid = Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, MaxDistance, RayMask) && hit.normal.y > 0.6f;
            if (_ghostValid) _ghostPos = hit.point;
            if (_ghost == null) Rebuild();
            if (_ghost != null)
            {
                _ghost.SetActive(_ghostValid);
                // de frente al jugador + el giro elegido
                Vector3 toMe = me.Position - _ghostPos; toMe.y = 0f;
                float face = toMe.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-toMe).eulerAngles.y : 0f;
                _ghost.transform.SetPositionAndRotation(_ghostPos, Quaternion.Euler(0f, face + 180f + _ghostRot, 0f));
            }

            if (Input.GetKeyDown(Plugin.CfgMachinePlace.Value) && _ghostValid && _ghost != null) Place();
            if (Input.GetKeyDown(Plugin.CfgMachineDelete.Value)) DeleteNearest(me.Position);
            if (Input.GetKeyDown(Plugin.CfgMachinePhoto.Value)) Photo();
        }

        private static void Rebuild()
        {
            if (_ghost != null) UnityEngine.Object.Destroy(_ghost);
            _ghost = Spawn(new Spot { perk = Ids[_ghostPerk] }, true);
        }

        private static void Place()
        {
            var t = _ghost.transform;
            var s = new Spot { map = _map, perk = Ids[_ghostPerk], x = R(t.position.x), y = R(t.position.y), z = R(t.position.z), rot = R(t.eulerAngles.y) };
            _all.Add(s);
            var go = Spawn(s, false);
            if (go != null) _placed.Add(new KeyValuePair<Spot, GameObject>(s, go));
            Save();
            Status(L.F("machine.placed", Name(s.perk), _map));
            Dbg.Log("MAQUINAS", "colocada " + s.perk + " en " + _map + " (" + s.x + ", " + s.y + ", " + s.z + ") giro " + s.rot);
        }

        private static void DeleteNearest(Vector3 from)
        {
            if (_placed.Count == 0) return;
            var near = _placed.OrderBy(p => p.Value != null ? (p.Value.transform.position - from).sqrMagnitude : float.MaxValue).First();
            if (near.Value == null || (near.Value.transform.position - from).sqrMagnitude > 10f * 10f) { Status(L.T("machine.none")); return; }
            UnityEngine.Object.Destroy(near.Value);
            _placed.Remove(near);
            _all.Remove(near.Key);
            Save();
            Status(L.F("machine.deleted", Name(near.Key.perk)));
        }

        private static void Photo()
        {
            try
            {
                string dir = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "photos");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, (_map ?? "raid") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                ScreenCapture.CaptureScreenshot(file);
                Status(L.F("machine.photo", Path.GetFileName(file)));
                Plugin.Log.LogInfo("Maquinas: foto " + file);
            }
            catch (Exception e) { Plugin.Log.LogError("Maquinas (foto): " + e); }
        }

        private static string Name(string id)
        {
            return id == Power ? L.T("machine.switch") : id == Wunderfizz ? "Der Wunderfizz" : id == Box ? L.T("box.name") : id == Tedd ? "TEDD" : Perks.Get(id).Name;
        }

        private static float R(float v) { return (float)Math.Round(v, 2); }
        private static void Status(string s) { _status = s; _statusUntil = Time.realtimeSinceStartup + 4f; }

        // Hud.Draw: ayuda del editor y nombre de la maquina elegida
        internal static void DrawGui(float scale)
        {
            if (Plugin.InRaid) Trials.DrawGui(scale);
            if (!Plugin.CfgMachineEditor.Value)
            {
                if (_prompt == null) return;
                var ps = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * scale), alignment = TextAnchor.MiddleCenter, richText = true };
                var rr = new Rect(Screen.width / 2f - 450 * scale, Screen.height * 0.62f, 900 * scale, 40 * scale);
                var sh = new GUIStyle(ps); sh.normal.textColor = Color.black;
                GUI.Label(new Rect(rr.x + 2, rr.y + 2, rr.width, rr.height), _prompt, sh);
                ps.normal.textColor = Color.white;
                GUI.Label(rr, _prompt, ps);
                if (_hold > 0f)
                {
                    var bar = new Rect(Screen.width / 2f - 120 * scale, rr.yMax + 4 * scale, 240 * scale * Mathf.Clamp01(_hold / HoldTime), 6 * scale);
                    GUI.DrawTexture(bar, Texture2D.whiteTexture);
                }
                return;
            }
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * scale), richText = true };
            style.normal.textColor = Color.white;
            string perk = Name(Ids[_ghostPerk]);
            string help = "<b>" + L.T("machine.editor") + "</b>  ·  " + perk + (_ghostValid ? "" : "  <color=#ff8080>(" + L.T("machine.aim") + ")</color>") + "\n"
                + L.F("machine.keys", Plugin.CfgMachinePrev.Value, Plugin.CfgMachineNext.Value, Plugin.CfgMachineRotL.Value, Plugin.CfgMachineRotR.Value,
                    Plugin.CfgMachineSnap.Value, Plugin.CfgMachinePlace.Value, Plugin.CfgMachineDelete.Value, Plugin.CfgMachinePhoto.Value)
                + "\n" + L.F("machine.count", _placed.Count, _map);
            if (_status != null && Time.realtimeSinceStartup < _statusUntil) help += "\n<color=#ffd060>" + _status + "</color>";
            var r = new Rect(20 * scale, 140 * scale, 900 * scale, 140 * scale);
            var shadow = new GUIStyle(style); shadow.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), StripColor(help), shadow);
            GUI.Label(r, help, style);
        }

        private static string StripColor(string s) { return System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", ""); }
    }
}
