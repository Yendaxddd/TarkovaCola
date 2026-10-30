using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Re-roll (mejora mayor de Vulture Aid): mirando un contenedor de cerca, una tecla vuelve a tirar su botin.
    // 10 puntos por raid. El servidor decide el coste (normal 1, Scav muerto 2, caja fuerte 5, aereo 7; +0.5 por cada repeticion en el
    // mismo contenedor), la probabilidad de fallo (15/25/35/40%; al fallar se queda el botin original) y genera el botin nuevo con la
    // tabla de botin del mapa. Los cuerpos de PMC y jefes no son contenedores, asi que no se pueden re-tirar.
    internal static class Reroll
    {
        internal const float Points = 10f;
        private const float Reach = 2.5f;

        private static float _left = Points;
        private static LootableContainer _target;
        private static readonly Dictionary<string, int> _repeats = new Dictionary<string, int>();
        private static bool _busy;
        private static JObject _pending;          // respuesta del servidor (se aplica en el hilo principal)
        private static LootableContainer _pendingFor;

        internal static void Reset() { _left = Points; _target = null; _repeats.Clear(); _busy = false; _pending = null; _pendingFor = null; }

        internal static void Tick(Player me)
        {
            if (_pending != null) { var p = _pending; _pending = null; Apply(p, _pendingFor); }
            _target = null;
            if (!Perk.Has("va_reroll")) return;
            var cam = Powerup.Cam();
            if (cam == null) return;
            RaycastHit hit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, Reach, ~0, QueryTriggerInteraction.Collide))
                _target = hit.collider.GetComponentInParent<LootableContainer>();
            if (_target == null || _busy || !Input.GetKeyDown(Plugin.CfgRerollKey.Value)) return;
            Request(_target);
        }

        private static void Request(LootableContainer c)
        {
            var root = c.ItemOwner != null ? c.ItemOwner.RootItem : null;
            if (root == null) return;
            int rep;
            _repeats.TryGetValue(c.Id, out rep);
            var world = Singleton<GameWorld>.Instance;
            var body = new JObject
            {
                ["tpl"] = root.TemplateId.ToString(), ["map"] = world != null ? world.LocationId : "", ["repeat"] = rep, ["points"] = _left,
            }.ToString(Formatting.None);
            _busy = true;
            RequestHandler.PostJsonAsync("/tarkovacola/reroll", body).ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted) { Plugin.Log.LogWarning("Re-roll: " + t.Exception?.GetBaseException().Message); return; }
                    var outer = JObject.Parse(t.Result);
                    _pendingFor = c;
                    _pending = JObject.Parse((string)outer["json"]);
                }
                catch (Exception e) { Plugin.Log.LogWarning("Re-roll (respuesta): " + e.Message); }
                finally { _busy = false; }
            });
        }

        private static void Apply(JObject r, LootableContainer c)
        {
            string status = (string)r["status"];
            float cost = (float?)r["cost"] ?? 0f;
            if (status == "points") { Hud.Notify(L.T("va_reroll.n"), L.F("notice.reroll_points", cost.ToString("0.#"), _left.ToString("0.#"))); return; }
            if (status != "ok" && status != "fail") { Hud.Notify(L.T("va_reroll.n"), L.T("notice.reroll_no")); return; }
            _left -= cost;
            if (c != null) { int rep; _repeats.TryGetValue(c.Id, out rep); _repeats[c.Id] = rep + 1; }
            if (status == "fail") { Hud.Notify(L.T("va_reroll.n"), L.F("notice.reroll_fail", _left.ToString("0.#"))); return; }

            var root = c != null && c.ItemOwner != null ? c.ItemOwner.RootItem as CompoundItem : null;
            if (root == null) return;
            foreach (var grid in root.Grids)
                foreach (var it in grid.Items.ToList()) grid.RemoveWithoutRestrictions(it);
            int n = 0;
            foreach (var e in (JArray)r["items"])
                if (VultureAid.PutInto(root, (string)e["tpl"], (int?)e["count"] ?? 1)) n++;
            Hud.Notify(L.T("va_reroll.n"), L.F("notice.reroll_ok", n, _left.ToString("0.#")));
            Dbg.Log("EFECTO", "Re-roll: " + n + " objetos nuevos (coste " + cost + ", quedan " + _left + ")");
        }

        private static GUIStyle _style;
        internal static void DrawGui()
        {
            if (_target == null || !Perk.Has("va_reroll")) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold };
            var r = new Rect(Screen.width / 2f - 250f, Screen.height * 0.58f, 500f, 24f);
            string txt = _busy ? "..." : L.F("prompt.reroll", Plugin.CfgRerollKey.Value, _left.ToString("0.#"));
            GUI.color = Color.black; GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), txt, _style);
            GUI.color = new Color(0.9f, 1f, 0.45f); GUI.Label(r, txt, _style);
            GUI.color = Color.white;
        }
    }
}
