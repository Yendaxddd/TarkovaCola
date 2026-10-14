using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Los guardias de la mision de Skier ("Carga fria").
    //  VisitAPI hace aparecer 4 PMC en el punto de la mision (spawns/TarkovaCola_Skier1.json del pack) mientras la mision esta en marcha.
    //  Aqui, justo antes de que aparezcan, se les mete en el inventario una PHD Flopper a cada uno y, a uno elegido al azar, el sobre.
    //  Como llevan una PHD Flopper "equipada", las explosiones no les hacen dano (como a ti con la perk).
    //  Los guardias se reconocen porque VisitAPI les pasa su propia funcion de "mantener posicion" y porque aparecen en el punto de la mision.
    // ============================================================================================
    internal static class QuestGuards
    {
        internal const string EnvelopeTpl = "6a1c00000000000000000f01";

        private static JObject _post;                                         // {map, at:{x,y,z}, radius} del servidor
        private static readonly HashSet<string> _guards = new HashSet<string>();
        private static readonly HashSet<BotCreationData> _done = new HashSet<BotCreationData>();

        internal static bool Any { get { return _guards.Count > 0; } }
        internal static bool IsGuard(Player p) { return p != null && _guards.Contains(p.ProfileId); }

        internal static void OnRaidStart()
        {
            _guards.Clear(); _done.Clear(); _post = null;
            try
            {
                var resp = JObject.Parse(RequestHandler.PostJson("/tarkovacola/quest/post", "{}"));
                var json = (string)resp["json"];
                if (!string.IsNullOrEmpty(json)) _post = JObject.Parse(json);
                Dbg.Log("MISION", _post != null ? "punto de los guardias: " + _post.ToString(Newtonsoft.Json.Formatting.None) : "no hay mision de guardias");
            }
            catch (Exception e) { Dbg.Log("MISION", "no se pudo leer el punto de los guardias: " + e.Message); }
        }

        // Antes de activar un bot de VisitAPI: si es de nuestra mision, se equipa a todos los de su grupo
        internal static void OnSpawn(BotCreationData data, Action<BotOwner> callback)
        {
            if (_post == null || data == null || callback == null || !Singleton<GameWorld>.Instantiated) return;
            if (callback.Method.DeclaringType == null || callback.Method.DeclaringType.Name != "StorySpawns") return;     // no es una aparicion de VisitAPI
            if (!_done.Add(data)) return;                                                                                // el grupo ya se equipo
            if (!string.Equals(Singleton<GameWorld>.Instance.LocationId, (string)_post["map"], StringComparison.OrdinalIgnoreCase)) return;
            var at = _post["at"];
            if (at == null) return;
            var center = new Vector3((float)at["x"], (float)at["y"], (float)at["z"]);
            float radius = (float?)_post["radius"] ?? 6f;
            if (!data._positions.Any(p => Vector3.Distance(p.position, center) <= radius + 12f)) return;                // otro pack: no tocar

            var profiles = data.Profiles.ToList();
            if (profiles.Count == 0) return;
            int carrier = UnityEngine.Random.Range(0, profiles.Count);
            var phd = Perks.PhdFlopper;
            for (int i = 0; i < profiles.Count; i++)
            {
                _guards.Add(profiles[i].Id);
                bool a = Give(profiles[i], phd.ItemTpl);
                bool b = i == carrier && Give(profiles[i], EnvelopeTpl);
                Dbg.Log("MISION", "guardia " + (i + 1) + "/" + profiles.Count + " (" + profiles[i].Info.Nickname + "): PHD Flopper " + (a ? "si" : "NO") + (i == carrier ? ", sobre " + (b ? "si" : "NO") : ""));
            }
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
}
