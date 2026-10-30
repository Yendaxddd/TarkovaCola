using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;
using UnityEngine.AI;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Pruebas de TEDD (BO7). TEDD sale en algunas raids (Plugin.CfgTeddChance) en uno de sus puntos del mapa (Machines). Al
    //  hablarle (mantener F) da UNA prueba al azar por raid; todo lo que hace falta aparece seguro:
    //   - Objeto: aparece un objeto lejos (80-180 m); hay que llevarselo a TEDD.
    //   - Abatir: aparece un PMC del otro bando lejos (100-200 m), marcado en pantalla; basta con que muera.
    //   - Aguantar: 90 s a menos de 8 m de TEDD mientras llegan oleadas de infectados que van hacia el.
    //   - Dinero: aparece un fajo de rublos lejos; hay que pagarle esa cantidad a TEDD (vale tambien el dinero que ya lleves).
    //  Recompensa: una botella de perk al azar que TEDD lanza delante de el. Fallo: se acaba el tiempo.
    // ============================================================================================
    internal static class Trials
    {
        private enum Kind { Fetch, Kill, Hold, Money }
        private enum State { None, Waiting, Active, Over }

        private const float TimeLimit = 300f, HoldTimeLimit = 360f;
        private const float HoldNeed = 90f, HoldRadius = 8f, WaveEvery = 20f;
        private const int WaveSize = 3;
        private const string ZombieRole = "infectedAssault";
        private const string Roubles = "5449016a4bdc2d6f028b456f";
        // objetos que puede pedir (de taller y de valor medio)
        private static readonly string[] FetchItems =
        {
            "590a3efd86f77437d351a25b" /* analizador de gases */, "5c06779c86f77426e00dd782" /* cables */, "590c2e1186f77425357b6124" /* juego de herramientas */,
            "59e35cbb86f7741778269d83" /* manguera corrugada */, "5d1b392c86f77425243e98fe" /* bombilla */, "5c12620d86f7743f8b198b72" /* Tetriz */,
            "590c2b4386f77425357b6123" /* alicates */, "57347c5b245977448d35f6e1" /* tornillos */,
        };

        private static State _state;
        private static Kind _kind;
        private static Transform _tedd;
        private static float _deadline, _held, _nextWave;
        private static string _fetchTpl, _targetId;
        private static LootItem _loot;
        private static Player _target;
        private static int _money;
        private static bool _canUse, _outside;
        private static GUIStyle _style, _marker;

        internal static bool CanUse { get { return _canUse; } }

        internal static void OnRaidStart() { _spawnPoints = null; _state = State.None; _tedd = null; _loot = null; _target = null; _targetId = null; _canUse = false; }

        internal static void OnTeddSpawned(GameObject tedd)
        {
            _tedd = tedd.transform; _state = State.Waiting;
            Hud.Notify("TEDD", L.T("tedd.here"), null);
            // luz verde encima (su pantalla), para verlo de lejos
            var lg = new GameObject("TarkovaColaTeddLight");
            lg.transform.SetParent(_tedd, false);
            lg.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(0.4f, 1f, 0.55f); l.range = 6f; l.intensity = 1.6f; l.shadows = LightShadows.None;
            Dbg.Log("TEDD", "aparece en " + _tedd.position.ToString("F1"));
        }

        // ---------------------------------------------------------------- hablar con TEDD (Machines.TickUse)
        internal static string Prompt(Player me, string key)
        {
            _canUse = false;
            switch (_state)
            {
                case State.Waiting: _canUse = true; return L.F("tedd.start", key);
                case State.Over: return L.T("tedd.over");
                case State.Active:
                    if (_kind == Kind.Fetch && Carried(me, _fetchTpl) != null) { _canUse = true; return L.F("tedd.deliver", key, ItemName(_fetchTpl)); }
                    if (_kind == Kind.Money) { _canUse = true; return L.F("tedd.pay", key, _money.ToString("N0")); }
                    return L.F("tedd.busy", Desc());
            }
            return null;
        }

        internal static void Use(Player me)
        {
            if (_state == State.Waiting) { Start(me); return; }
            if (_state != State.Active) return;
            if (_kind == Kind.Fetch)
            {
                var it = Carried(me, _fetchTpl);
                if (it == null) return;
                var op = ItemManipulator.Remove(it, me.InventoryController, true);
                if (op.Failed) { Plugin.Log.LogWarning("TEDD: no se pudo entregar el objeto (" + op.Error + ")"); return; }
                me.InventoryController.TryRunNetworkTransaction(op);
                Complete(me);
            }
            else if (_kind == Kind.Money && Machines.Charge(me, _money, "TEDD", null)) Complete(me);
        }

        // ---------------------------------------------------------------- empezar
        private static void Start(Player me)
        {
            _kind = (Kind)Random.Range(0, 4);
            _state = State.Active;
            _deadline = Time.time + (_kind == Kind.Hold ? HoldTimeLimit : TimeLimit);
            var from = _tedd.position;
            Vector3 p;
            switch (_kind)
            {
                case Kind.Fetch:
                    _fetchTpl = FetchItems[Random.Range(0, FetchItems.Length)];
                    if (FarPoint(from, 80f, 180f, out p)) _loot = Drop(me, NewItem(_fetchTpl), p + Vector3.up * 0.4f, Vector3.zero);
                    break;
                case Kind.Money:
                    _money = Random.Range(3, 9) * 5000;
                    var stack = NewItem(Roubles);
                    if (stack != null) stack.StackObjectsCount = _money;
                    if (FarPoint(from, 80f, 180f, out p)) _loot = Drop(me, stack, p + Vector3.up * 0.4f, Vector3.zero);
                    break;
                case Kind.Kill:
                    string role = me.Profile.Side == EPlayerSide.Bear ? "pmcUSEC" : "pmcBEAR";
                    if (FarPoint(from, 100f, 200f, out p))
                        GuardSpawner.SpawnBots("TEDD", role, 1, p, 4f, b => { _target = b.GetPlayer; _targetId = b.ProfileId; Dbg.Log("TEDD", "objetivo en su sitio: " + b.Profile.Info.Nickname); });
                    break;
                case Kind.Hold:
                    _held = 0f; _nextWave = Time.time + 3f;
                    break;
            }
            Hud.Notify(L.T("tedd.title"), Desc(), null);
            Dbg.Log("TEDD", "prueba " + _kind + (_kind == Kind.Fetch ? " (" + _fetchTpl + ")" : _kind == Kind.Money ? " (" + _money + " rublos)" : ""));
        }

        private static string Desc()
        {
            switch (_kind)
            {
                case Kind.Fetch: return L.F("tedd.t.fetch", ItemName(_fetchTpl));
                case Kind.Kill: return L.T("tedd.t.kill");
                case Kind.Hold: return L.F("tedd.t.hold", Mathf.CeilToInt(HoldNeed - _held));
                default: return L.F("tedd.t.money", _money.ToString("N0"));
            }
        }

        // ---------------------------------------------------------------- cada frame
        internal static void Tick(Player me)
        {
            if (_state != State.Active || _tedd == null) return;
            if (Time.time > _deadline) { Fail(); return; }
            if (_kind != Kind.Hold) return;
            _outside = (me.Position - _tedd.position).sqrMagnitude > HoldRadius * HoldRadius;
            if (!_outside) _held += Time.deltaTime;
            if (_held >= HoldNeed) { Complete(me); return; }
            if (Time.time >= _nextWave)
            {
                _nextWave = Time.time + WaveEvery;
                Vector3 p;
                if (FarPoint(_tedd.position, 25f, 45f, out p))
                    GuardSpawner.SpawnBots("TEDD oleada", ZombieRole, WaveSize, p, 5f, b =>
                    {
                        b.BotsGroup.AddEnemy(me, EBotEnemyCause.addPlayer);
                        if (_tedd != null) b.GoToPoint(_tedd.position, false);
                    });
            }
        }

        internal static void OnDeath(Player victim)
        {
            if (_state == State.Active && _kind == Kind.Kill && victim != null && victim.ProfileId == _targetId) Complete(Plugin.Me);
        }

        private static void Complete(Player me)
        {
            _state = State.Over;
            var perk = Perks.All[Random.Range(0, Perks.All.Length)];
            if (me != null && _tedd != null) Drop(me, NewItem(perk.ItemTpl), _tedd.TransformPoint(new Vector3(0f, 1.2f, -0.6f)), _tedd.TransformDirection(new Vector3(0f, 1f, -2f)));
            Hud.Notify(L.T("tedd.title"), L.T("tedd.done") + "  ·  " + L.F("tedd.reward", perk.Name), perk.Id);
            Dbg.Log("TEDD", "prueba " + _kind + " superada: " + perk.Id);
        }

        private static void Fail()
        {
            _state = State.Over;
            Hud.Notify(L.T("tedd.title"), L.T("tedd.fail"), null);
            Dbg.Log("TEDD", "prueba " + _kind + " fallida (tiempo)");
        }

        // ---------------------------------------------------------------- utilidades
        private static Item NewItem(string tpl)
        {
            try { return Singleton<ItemFactory>.Instance.CreateItem(System.Guid.NewGuid().ToString("N").Substring(0, 24), tpl, null); }
            catch (System.Exception e) { Plugin.Log.LogWarning("TEDD: no se pudo crear " + tpl + ": " + e.Message); return null; }
        }

        private static LootItem Drop(Player me, Item item, Vector3 pos, Vector3 vel)
        {
            var world = Singleton<GameWorld>.Instance;
            if (item == null || world == null) return null;
            return world.ThrowItem(item, me, pos, Quaternion.identity, vel, Vector3.zero, true);
        }

        private static Item Carried(Player me, string tpl)
        {
            return me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(i => i != null && i.StringTemplateId == tpl);
        }

        private static string ItemName(string tpl)
        {
            try { return (tpl + " Name").Localized(); } catch { return tpl; }
        }

        // Un sitio dentro del mapa a una distancia entre min y max: un punto de aparicion de verdad del mapa (los de bots y jugadores) al que se
        // pueda llegar andando desde 'from'. La malla de navegacion de EFT tiene trozos fuera de la zona jugable, asi que un punto al azar
        // sobre ella podia caer fuera del mapa. En mapas pequenos (Factory) se acorta la distancia hasta encontrar alguno.
        private static List<Vector3> _spawnPoints;

        private static bool FarPoint(Vector3 from, float min, float max, out Vector3 p)
        {
            if (_spawnPoints == null || _spawnPoints.Count == 0)
                _spawnPoints = Object.FindObjectsOfType<EFT.Game.Spawning.SpawnPointMarker>().Select(s => s.Position).ToList();
            var path = new NavMeshPath();
            for (float f = 1f; f >= 0.2f; f -= 0.2f)
            {
                float lo = min * f, hi = max;
                foreach (var c in _spawnPoints.Where(s => { float d = Vector3.Distance(s, from); return d >= lo && d <= hi; }).OrderBy(_ => Random.value).Take(25))
                {
                    NavMeshHit hit;
                    if (!NavMesh.SamplePosition(c, out hit, 3f, -1)) continue;
                    if (NavMesh.CalculatePath(from, hit.position, -1, path) && path.status == NavMeshPathStatus.PathComplete) { p = hit.position; return true; }
                }
            }
            // sin puntos de aparicion cerca (p. ej. las oleadas junto a TEDD): un punto de la malla al que se llegue andando (asi esta dentro del mapa)
            for (int i = 0; i < 80; i++)
            {
                var dir = Random.insideUnitCircle.normalized * Random.Range(min * 0.5f, max);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(from + new Vector3(dir.x, 0f, dir.y), out hit, 6f, -1)
                    && NavMesh.CalculatePath(from, hit.position, -1, path) && path.status == NavMeshPathStatus.PathComplete) { p = hit.position; return true; }
            }
            Dbg.Log("TEDD", "no hay punto alcanzable entre " + min + " y " + max + " m (" + _spawnPoints.Count + " puntos de aparicion en el mapa)");
            p = from; return false;
        }

        // ---------------------------------------------------------------- pantalla: la prueba arriba y la marca del objetivo
        private const float FindRange = 100f;                                  // a esta distancia TEDD se marca en pantalla aunque no haya prueba

        internal static void DrawGui(float scale)
        {
            if (_tedd == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, richText = true };
                _marker = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true };
            }
            _marker.fontSize = Mathf.RoundToInt(16 * scale);
            // esperando a que lo encuentres: se marca cuando estas cerca
            if (_state == State.Waiting)
            {
                var me = Plugin.Me;
                if (me != null && Vector3.Distance(me.Position, _tedd.position) < FindRange) Marker(_tedd.position + Vector3.up * 2.6f, "TEDD", scale);
                return;
            }
            if (_state != State.Active) return;
            _style.fontSize = Mathf.RoundToInt(20 * scale);
            _marker.fontSize = Mathf.RoundToInt(16 * scale);
            int left = Mathf.Max(0, Mathf.CeilToInt(_deadline - Time.time));
            string text = "<b><color=#7dffa0>" + L.T("tedd.title") + "</color></b>  ·  " + Desc() + "  ·  " + (left / 60) + ":" + (left % 60).ToString("00");
            if (_kind == Kind.Hold && _outside) text += "\n<color=#ff8080>" + L.T("tedd.outside") + "</color>";
            Shadowed(new Rect(Screen.width / 2f - 600 * scale, 70 * scale, 1200 * scale, 70 * scale), text, _style);

            // la marca: el objetivo, el objeto o el dinero (si siguen en el suelo); si no, TEDD
            Vector3? where = null; string label = "TEDD";
            if (_kind == Kind.Kill && _target != null && _target.HealthController != null && _target.HealthController.IsAlive) { where = _target.Position + Vector3.up * 1.9f; label = L.T("tedd.target"); }
            else if ((_kind == Kind.Fetch || _kind == Kind.Money) && _loot != null && _loot.gameObject.activeInHierarchy) { where = _loot.transform.position + Vector3.up * 0.5f; label = L.T(_kind == Kind.Fetch ? "tedd.item" : "tedd.money"); }
            else if (_kind != Kind.Kill) where = _tedd.position + Vector3.up * 2.6f;
            if (where.HasValue) Marker(where.Value, label, scale);
        }

        private static void Marker(Vector3 pos, string label, float scale)
        {
            var cam = Powerup.Cam(); var me = Plugin.Me;
            if (cam == null || me == null) return;
            // en coordenadas de viewport (EFT renderiza la camara a otra resolucion: ver Powerup.DrawMarker)
            var vp = cam.WorldToViewportPoint(pos);
            bool behind = Vector3.Dot(cam.transform.forward, pos - cam.transform.position) < 0f;
            float x = vp.x * Screen.width, y = (1f - vp.y) * Screen.height;
            if (behind) { x = Screen.width - x; y = Screen.height - y; }
            x = Mathf.Clamp(x, 40 * scale, Screen.width - 40 * scale); y = Mathf.Clamp(y, 40 * scale, Screen.height - 40 * scale);
            int dist = Mathf.RoundToInt(Vector3.Distance(me.Position, pos));
            Shadowed(new Rect(x - 100 * scale, y - 20 * scale, 200 * scale, 40 * scale), "<b><color=#7dffa0>◆ " + label + "</color></b>\n" + dist + " m", _marker);
        }

        private static void Shadowed(Rect r, string text, GUIStyle st)
        {
            var c = st.normal.textColor;
            st.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", ""), st);
            st.normal.textColor = Color.white;
            GUI.Label(r, text, st);
            st.normal.textColor = c;
        }
    }
}
