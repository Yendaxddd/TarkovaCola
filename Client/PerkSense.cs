using System.Collections.Generic;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Aviso cuando te acercas a un enemigo que lleva perks encima (en el inventario) o a un Embotellador de las misiones.
    // Se revisa una vez por segundo; cada enemigo avisa una sola vez por raid. Su inventario se mira solo la primera vez que entra en rango.
    internal static class PerkSense
    {
        private static float _next;
        private static string _raidKey;
        private static readonly Dictionary<string, string> _carried = new Dictionary<string, string>();     // perfil -> perk que lleva ("" = ninguna)
        private static readonly HashSet<string> _warned = new HashSet<string>();

        internal static void Tick()
        {
            if (!Plugin.InRaid || !Plugin.CfgSense.Value) return;
            float now = Time.time;
            if (now < _next) return;
            _next = now + 1f;

            var me = Plugin.Me;
            var world = Singleton<GameWorld>.Instance;
            if (me == null || world == null || world.AllAlivePlayersList == null) return;
            string key = world.LocationId + "|" + me.ProfileId + "|" + world.GetHashCode();
            if (key != _raidKey) { _raidKey = key; _carried.Clear(); _warned.Clear(); }

            float r = Plugin.CfgSenseRadius.Value, r2 = r * r;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p == me || p.IsYourPlayer || p.HealthController == null || !p.HealthController.IsAlive) continue;
                if (_warned.Contains(p.ProfileId)) continue;
                if ((p.Position - me.Position).sqrMagnitude > r2) continue;

                string perk;
                if (!_carried.TryGetValue(p.ProfileId, out perk)) _carried[p.ProfileId] = perk = Find(p);
                if (perk == null || (perk.Length == 0 && !QuestGuards.IsGuard(p))) continue;

                _warned.Add(p.ProfileId);
                var def = perk.Length > 0 ? Perks.Get(perk) : null;
                Hud.Enqueue(L.T("sense.title"), def != null ? L.F("sense.sub", def.Name) : L.T("sense.guard"), def != null ? def.Id : null);
                Dbg.Log("SENTIDO", p.Profile?.Info?.Settings?.Role + " a " + Mathf.RoundToInt(Vector3.Distance(p.Position, me.Position)) + " m lleva " + (def != null ? def.Id : "perks de guardia"));
            }
        }

        // Primera perk que lleva en el equipo (mochila, chaleco, bolsillos...), o "" si ninguna
        private static string Find(Player p)
        {
            try
            {
                var inv = p.Profile?.Inventory;
                if (inv == null) return "";
                foreach (var it in inv.GetPlayerItems(EFT.InventoryLogic.EPlayerItems.Equipment))
                {
                    var def = Perks.ByItem(it?.TemplateId.ToString());
                    if (def != null) return def.Id;
                }
            }
            catch { }
            return "";
        }
    }
}
