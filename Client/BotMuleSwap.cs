using System;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Mule Kick en bots: cuando se queda sin balas, en vez de recargar saca otra arma que aun tenga municion
    // (principal, secundaria, pistola o la 3.a arma de Mule Kick). Si todas estan vacias, recarga normal.
    [HarmonyPatch(typeof(BotReload), nameof(BotReload.TryReload))]
    internal static class BotMuleSwapPatch
    {
        private static readonly AccessTools.FieldRef<BotReload, BotOwner> Owner = AccessTools.FieldRefAccess<BotReload, BotOwner>("_owner");

        [HarmonyPrefix]
        private static bool Prefix(BotReload __instance, ref bool __result)
        {
            try
            {
                if (!BotPerks.Any || __instance.Reloading) return true;
                var bot = Owner(__instance);
                var p = bot != null ? bot.GetPlayer : null;
                if (p == null || !BotPerks.Has(p, MuleKick.Id)) return true;
                var sel = bot.WeaponManager != null ? bot.WeaponManager.Selector : null;
                if (sel == null || !sel.IsWeaponReady) return true;
                var inHands = p.HandsController != null ? p.HandsController.Item : null;
                var eq = p.Profile.Inventory.Equipment;
                var slots = new[] { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster }.ToList();
                if (MuleKick.SlotIndex >= 0) slots.Add(MuleKick.SlotEnum);
                foreach (var s in slots)
                {
                    Slot slot;
                    try { slot = eq.GetSlot(s); } catch { continue; }
                    var w = slot != null ? slot.ContainedItem as Weapon : null;
                    if (w == null || w == inHands || Loaded(w) <= 0) continue;
                    if (!sel.TryChangeToSlot(s, true)) return true;
                    Dbg.Log("BOTS", p.Profile.Nickname + " (Mule Kick): cambia a " + w.ShortName.Localized() + " en vez de recargar");
                    __result = false;
                    return false;
                }
            }
            catch (Exception e) { Dbg.Throttled("mulebot", 10f, "ERROR", "Mule Kick (bot, cambio): " + e.Message); }
            return true;
        }

        private static int Loaded(Weapon w)
        {
            var mag = w.GetCurrentMagazine();
            return (mag != null ? mag.Count : 0) + w.ChamberAmmoCount;
        }
    }
}
