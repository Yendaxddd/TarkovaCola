using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Compatibilidad con mods de bots y companeros (todo por reflexion: sin ellos no hace nada):
    //  - Aliados: companeros de PIT Fireteam y cualquier bot cuyo grupo no te tiene por enemigo (scavs cuando vas de scav, amigos de
    //    otros mods). Las perks no los aturden, ni los marcan, ni les hacen dano con sus explosiones (plancha de PHD, APEX).
    //  - K9 Companion: el perro no recibe dano de las explosiones de las perks.
    //  - SAIN y la IA normal: un bot aturdido (Widow's Wine, Electric Cherry, Tombstone) no dispara mientras dura el aturdimiento
    //    (SAIN seguia disparando aunque el movimiento ya estuviera frenado).
    //  - QuestingBots: los guardias y jefes de las misiones de Tarkova-Cola se quedan en su puesto (su capa de misiones se los llevaba).
    internal static class Allies
    {
        private static readonly HashSet<string> _blasts = new HashSet<string>();

        // una explosion creada por una perk (Apex.Blast): su "arma" se apunta para reconocer su dano
        internal static void MarkBlast(Item w) { if (w != null) { if (_blasts.Count > 200) _blasts.Clear(); _blasts.Add(w.Id.ToString()); } }

        internal static bool IsPerkBlast(DamageInfo d)
        {
            try { return d.Weapon != null && _blasts.Contains(d.Weapon.Id.ToString()); } catch { return false; }
        }

        internal static bool Is(Player p)
        {
            if (p == null || p.IsYourPlayer) return false;
            if (BotPerks.IsFollower(p)) return true;
            try
            {
                var me = Plugin.Me;
                var bot = p.AIData != null ? p.AIData.BotOwner : null;
                if (me == null || bot == null || bot.BotsGroup == null) return false;
                // neutral por configuracion y que aun no te ha tomado por enemigo (si te ataca, deja de ser aliado)
                return !bot.BotsGroup.IsPlayerEnemy(me) && !bot.BotsGroup.IsEnemy(me);
            }
            catch { return false; }
        }
    }

    internal static class ModCompat
    {
        private static bool _done;

        // una vez, al empezar la primera raid (los demas mods ya estan cargados)
        internal static void Init()
        {
            if (_done) return;
            _done = true;
            var h = new Harmony("com.tarkovacola.client.modcompat");
            K9(h);
            QuestingBots(h);
        }

        // ---------------------------------------------------------------- K9 Companion
        private static void K9(Harmony h)
        {
            try
            {
                var t = AccessTools.TypeByName("K9Companion.DogHitbox");
                var m = t != null ? AccessTools.Method(t, "ApplyHit") : null;
                if (m == null) return;
                h.Patch(m, prefix: new HarmonyMethod(typeof(ModCompat), nameof(DogHitPrefix)));
                Plugin.Log.LogInfo("K9 Companion detectado: el perro no recibe dano de las explosiones de las perks");
            }
            catch (Exception e) { Plugin.Log.LogWarning("K9 Companion: " + e.Message); }
        }

        private static void DogHitPrefix(ref DamageInfo damageInfo)
        {
            if (Allies.IsPerkBlast(damageInfo)) damageInfo.Damage = 0f;
        }

        // ---------------------------------------------------------------- QuestingBots
        private static FieldInfo _qbBotField;
        private static PropertyInfo _qbBotProp;

        private static void QuestingBots(Harmony h)
        {
            try
            {
                var t = AccessTools.TypeByName("QuestingBots.BotLogic.Objective.BotObjectiveLayer");
                var m = t != null ? AccessTools.Method(t, "IsActive") : null;
                if (m == null) return;
                _qbBotProp = AccessTools.Property(t, "BotOwner");
                if (_qbBotProp == null) _qbBotField = AccessTools.Field(t, "BotOwner");
                if (_qbBotProp == null && _qbBotField == null) { Plugin.Log.LogWarning("QuestingBots: version no compatible (los guardias pueden irse de su puesto)"); return; }
                h.Patch(m, postfix: new HarmonyMethod(typeof(ModCompat), nameof(QbLayerPostfix)));
                Plugin.Log.LogInfo("QuestingBots detectado: los guardias de Tarkova-Cola no hacen sus misiones");
            }
            catch (Exception e) { Plugin.Log.LogWarning("QuestingBots: " + e.Message); }
        }

        private static void QbLayerPostfix(object __instance, ref bool __result)
        {
            if (!__result || !QuestGuards.Any) return;
            try
            {
                var bot = (_qbBotProp != null ? _qbBotProp.GetValue(__instance, null) : _qbBotField.GetValue(__instance)) as BotOwner;
                var p = bot != null ? bot.GetPlayer : null;
                if (p != null && (QuestGuards.IsGuard(p) || QuestGuards.IsBoss(p))) __result = false;
            }
            catch { }
        }
    }

    // ---------- un bot aturdido no dispara ----------
    [HarmonyPatch(typeof(Player.FirearmController), nameof(Player.FirearmController.SetTriggerPressed))]
    internal static class StunTriggerPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player.FirearmController __instance, ref bool pressed)
        {
            if (!pressed || !WidowsWine.AnyStunned) return;
            var p = __instance._player;
            if (p == null || !p.IsAI || !WidowsWine.IsStunned(p)) return;
            pressed = false;
        }
    }
}
