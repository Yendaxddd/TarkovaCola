using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.HealthSystem;
using EFT.Ballistics;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Los Embotelladores: los guardias de las misiones de la historia (y el Barman, el jefe final).
    //  VisitAPI los hace aparecer en el punto de cada mision mientras esta en marcha (spawns/TarkovaCola.json del pack). Justo antes de
    //  que aparezcan, aqui:
    //   - se decide que perks lleva cada uno ("tc.perks" del punto: una lista, "random" = una al azar cada uno, "all" = todas;
    //     en modo Classic el jefe lleva 4 al azar),
    //   - se le mete en el inventario una botella de cada perk que lleva (botin) y, a uno, el objeto de la mision,
    //   - y las perks FUNCIONAN en ellos: Speed Cola recarga mas rapido, Stamin-Up mas estamina, Juggernog -33% de dano,
    //     PHD Flopper inmune a explosiones y caidas, Widow's Wine te frena al darte, Deadshot +30% de penetracion.
    //  Sobre la cabeza de cada uno se ven los iconos de sus perks (solo si lo tienes a la vista y a menos de 80 m).
    //  Se reconocen porque VisitAPI les pasa su propia funcion de "mantener posicion" y porque aparecen en un punto de nuestro pack.
    // ============================================================================================
    internal static class QuestGuards
    {
        private static JArray _posts;                                             // puntos del pack (con "tc")
        private static readonly Dictionary<string, string[]> _perks = new Dictionary<string, string[]>();    // perfil -> perks
        private static readonly Dictionary<string, Player> _players = new Dictionary<string, Player>();
        private static readonly Dictionary<object, string[]> _byObj = new Dictionary<object, string[]>();     // SkillManager / Physical -> perks
        private static readonly HashSet<BotCreationData> _done = new HashSet<BotCreationData>();
        private static readonly HashSet<string> _bosses = new HashSet<string>();
        private static float _nextWeb;

        internal const float JugMult = 0.67f, ReloadMult = 1.5f, StaminaMult = 1.5f, Penetration = 1.3f;
        internal const float WebSlow = 0.6f, WebSeconds = 3f, IconRange = 80f;

        internal static bool Any { get { return _perks.Count > 0; } }
        internal static bool IsGuard(Player p) { return p != null && _perks.ContainsKey(p.ProfileId); }
        internal static bool IsBoss(Player p) { return p != null && _bosses.Contains(p.ProfileId); }

        internal static bool Has(string profileId, string perk)
        {
            string[] list;
            return profileId != null && _perks.TryGetValue(profileId, out list) && Array.IndexOf(list, perk) >= 0;
        }
        internal static bool Has(Player p, string perk) { return p != null && Has(p.ProfileId, perk); }
        internal static bool HasObj(object o, string perk)
        {
            string[] list;
            return o != null && _byObj.TryGetValue(o, out list) && Array.IndexOf(list, perk) >= 0;
        }

        internal static void OnRaidStart()
        {
            _perks.Clear(); _players.Clear(); _byObj.Clear(); _done.Clear(); _bosses.Clear(); _posts = null; _nextWeb = 0f;
            try
            {
                var resp = JObject.Parse(RequestHandler.PostJson("/tarkovacola/quest/posts", "{}"));
                var json = (string)resp["json"];
                if (!string.IsNullOrEmpty(json)) _posts = JArray.Parse(json);
                Dbg.Log("MISION", _posts != null ? _posts.Count + " punto(s) de guardias en el pack" : "no hay misiones de guardias");
            }
            catch (Exception e) { Dbg.Log("MISION", "no se pudieron leer los puntos de los guardias: " + e.Message); }
        }

        // ---------------------------------------------------------------- aparicion
        // Antes de activar un bot de VisitAPI: si es de nuestro pack, se equipa a todo su grupo
        internal static void OnSpawn(BotCreationData data, Action<BotOwner> callback)
        {
            if (_posts == null || data == null || callback == null || !Singleton<GameWorld>.Instantiated) return;
            if (callback.Method.DeclaringType == null || callback.Method.DeclaringType.Name != "StorySpawns") return;     // no es una aparicion de VisitAPI
            if (!_done.Add(data)) return;                                                                                // el grupo ya se equipo
            var post = Match(data);
            if (post == null) return;                                                                                    // otro pack: no tocar

            var tc = post["tc"] as JObject;
            var profiles = data.Profiles.ToList();
            if (profiles.Count == 0) return;
            bool boss = tc != null && (bool?)tc["boss"] == true;
            string item = tc != null && tc["item"] != null && tc["item"].Type == JTokenType.String ? (string)tc["item"] : null;
            int carrier = boss ? 0 : UnityEngine.Random.Range(0, profiles.Count);
            for (int i = 0; i < profiles.Count; i++)
            {
                var p = profiles[i];
                var perks = PerksFor(tc != null ? tc["perks"] : null);
                _perks[p.Id] = perks;
                if (boss) _bosses.Add(p.Id);
                int given = 0;
                foreach (var id in perks) if (Give(p, Perks.Get(id).ItemTpl)) given++;
                bool carry = item != null && i == carrier && Give(p, item);
                Dbg.Log("MISION", (boss ? "JEFE " : "guardia ") + (i + 1) + "/" + profiles.Count + " (" + p.Info.Nickname + "): " + string.Join("+", perks) +
                                  " (" + given + " botella(s))" + (item != null && i == carrier ? ", objeto de mision " + (carry ? "si" : "NO") : ""));
            }
        }

        // el punto del pack al que pertenece este grupo (mismo mapa y al lado de donde van a aparecer)
        private static JObject Match(BotCreationData data)
        {
            string map = Singleton<GameWorld>.Instance.LocationId;
            JObject best = null; float bestD = float.MaxValue;
            foreach (var t in _posts.OfType<JObject>())
            {
                if (!string.Equals((string)t["map"], map, StringComparison.OrdinalIgnoreCase)) continue;
                var at = t["at"];
                if (at == null) continue;
                var c = new Vector3((float)at["x"], (float)at["y"], (float)at["z"]);
                float r = (float?)t["radius"] ?? 6f;
                foreach (var p in data._positions)
                {
                    float d = Vector3.Distance(p.position, c);
                    if (d <= r + 12f && d < bestD) { bestD = d; best = t; }
                }
            }
            // el jefe y su escolta comparten punto: el jefe es el grupo de 1
            if (best != null && data.Profiles.Count == 1)
            {
                var b = _posts.OfType<JObject>().FirstOrDefault(t => (string)t["quest"] == (string)best["quest"] && t["tc"] is JObject && (bool?)t["tc"]["boss"] == true);
                if (b != null) best = b;
            }
            else if (best != null && best["tc"] is JObject && (bool?)best["tc"]["boss"] == true && data.Profiles.Count > 1)
            {
                var g = _posts.OfType<JObject>().FirstOrDefault(t => (string)t["quest"] == (string)best["quest"] && !(t["tc"] is JObject && (bool?)t["tc"]["boss"] == true));
                if (g != null) best = g;
            }
            return best;
        }

        private static string[] PerksFor(JToken spec)
        {
            var all = Perks.All.Select(p => p.Id).ToList();
            if (spec is JArray arr) return arr.Select(x => (string)x).Where(x => Perks.Get(x) != null).ToArray();
            string s = spec != null && spec.Type == JTokenType.String ? (string)spec : "random";
            if (s == "all")
            {
                if (!ClassicMode.Enabled) return all.ToArray();
                return all.OrderBy(_ => UnityEngine.Random.value).Take(ClassicMode.MaxMain).ToArray();       // en Classic: 4 al azar
            }
            return new[] { all[UnityEngine.Random.Range(0, all.Count)] };
        }

        // Mete un objeto nuevo en el inventario del bot (mochila, chaleco o bolsillos, donde quepa)
        private static bool Give(Profile profile, string tpl)
        {
            try
            {
                var item = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), tpl, null);
                if (item == null) return false;
                var equipment = profile.Inventory.Equipment;
                foreach (var slot in new[] { EquipmentSlot.Backpack, EquipmentSlot.TacticalVest, EquipmentSlot.Pockets })
                {
                    var container = equipment.GetSlot(slot).ContainedItem as CompoundItem;
                    if (container == null) continue;
                    foreach (var grid in container.Grids)
                    {
                        var address = grid.FindLocationForItem(item);
                        if (address == null) continue;
                        if (grid.AddItemWithoutRestrictions(item, address.LocationInGrid).Succeeded) return true;
                    }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("Mision: no se pudo meter " + tpl + " a un guardia: " + e.Message); }
            return false;
        }

        // El bot ya existe: se apuntan su jugador, sus habilidades y su fisico (para aplicarle las perks)
        internal static void OnActivated(BotOwner bot)
        {
            var p = bot != null ? bot.GetPlayer : null;
            string[] perks;
            if (p == null || !_perks.TryGetValue(p.ProfileId, out perks)) return;
            _players[p.ProfileId] = p;
            if (p.Skills != null) _byObj[p.Skills] = perks;
            if (p.Physical != null) _byObj[p.Physical] = perks;
            if (Array.IndexOf(perks, "speedcola") >= 0 || Array.IndexOf(perks, "deadshot") >= 0)
                try { (p.HandsController as Player.FirearmController)?.SyncWithCharacterSkills(); } catch { }
            if (Array.IndexOf(perks, "staminup") >= 0 && p.Physical != null && p.Physical.Stamina != null)
                try { p.Physical.Stamina.TotalCapacity.SetDirty(); } catch { }
        }

        // ---------------------------------------------------------------- efectos en combate
        // Un guardia con Widow's Wine te ha dado: te quedas pegado un momento
        internal static void OnHitMe(Player me, DamageInfo info)
        {
            var by = info.Player != null ? info.Player.iPlayer : null;
            if (by == null || !Has(by.ProfileId, "widowswine") || Time.time < _nextWeb) return;
            _nextWeb = Time.time + WebSeconds + 1f;
            var mc = me.MovementContext;
            if (mc != null) mc.ChangeSpeedLimit(WebSlow * mc.MaxSpeed, Player.ESpeedLimit.Swamp, WebSeconds);
            WebVfx.OnVictim(me.Position, WebSeconds);
            Dbg.Log("MISION", "un Embotellador con Widow's Wine te ha atrapado");
        }

        // ---------------------------------------------------------------- iconos sobre la cabeza
        private static readonly RaycastHit[] _hits = new RaycastHit[1];

        internal static void DrawIcons(float scale)
        {
            if (_players.Count == 0) return;
            var me = Plugin.Me;
            var cam = Powerup.Cam();
            if (me == null || cam == null) return;
            var camPos = cam.transform.position;
            foreach (var kv in _players)
            {
                var p = kv.Value;
                if (p == null || p.HealthController == null || !p.HealthController.IsAlive) continue;
                var head = p.Position + Vector3.up * (p.MovementContext != null ? 1.4f + 0.6f * Mathf.Clamp01(p.MovementContext.PoseLevel) : 2f);
                float dist = Vector3.Distance(camPos, head);
                if (dist > IconRange) continue;
                if (Vector3.Dot(cam.transform.forward, head - camPos) <= 0f) continue;
                if (Physics.Linecast(camPos, head, LayersMaskController.HighPolyWithTerrainMask)) continue;      // solo a la vista
                var vp = cam.WorldToViewportPoint(head + Vector3.up * 0.25f);
                if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) continue;
                string[] perks;
                if (!_perks.TryGetValue(kv.Key, out perks) || perks.Length == 0) continue;
                float size = Mathf.Lerp(26f, 15f, dist / IconRange) * scale * (IsBoss(p) ? 1.2f : 1f);
                float total = perks.Length * size + (perks.Length - 1) * 2f * scale;
                float x = vp.x * Screen.width - total / 2f, y = (1f - vp.y) * Screen.height - size;
                float alpha = Mathf.Clamp01(1.2f - dist / IconRange);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                for (int i = 0; i < perks.Length; i++)
                {
                    var tex = Hud.IconTex(perks[i]);
                    if (tex != null) GUI.DrawTexture(new Rect(x + i * (size + 2f * scale), y, size, size), tex, ScaleMode.ScaleToFit, true);
                }
            }
            GUI.color = Color.white;
        }
    }

    [HarmonyPatch(typeof(BotSpawner), "method_10")]
    internal static class GuardSpawnPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BotCreationData data, Action<BotOwner> callback)
        {
            try { QuestGuards.OnSpawn(data, callback); }
            catch (Exception e) { Plugin.Log.LogError("Mision (guardias): " + e); }
        }
    }

    [HarmonyPatch(typeof(BotSpawner), nameof(BotSpawner.ActivateBotCallback))]
    internal static class GuardActivatedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(BotOwner bot)
        {
            if (!QuestGuards.Any) return;
            try { QuestGuards.OnActivated(bot); }
            catch (Exception e) { Plugin.Log.LogError("Mision (guardia activado): " + e); }
        }
    }
}
