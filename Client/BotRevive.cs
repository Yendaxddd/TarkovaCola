using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Quick Revive en bots: el golpe que le mataria (cabeza o torax a 0) tiene un 50% de dejarlo CAIDO en vez de muerto:
    //   - se tira al suelo, se queda a 1 PV y sin moverse ni disparar (su IA se congela);
    //   - mientras esta caido solo muere de un tiro a la cabeza (el resto del dano no le hace nada);
    //   - se levanta cuando acaba el combate (15 s sin dano a menos de 50 m) y tu estas lejos (>40 m) o no le ves:
    //     con toda la vida, sin Quick Revive (no se repite) y yendo solo a por ti (Death Perception).
    // (Antes se generaba un bot nuevo junto al cadaver: mas fragil. Asi el bot nunca llega a morir.)
    internal static class BotRevive
    {
        internal const float Chance = 0.5f, CalmSeconds = 15f, FarMeters = 40f, NearCombat = 50f;

        private class Down { public Player P; public float Since; }
        private static readonly Dictionary<string, Down> _down = new Dictionary<string, Down>();
        private static readonly HashSet<string> _rolled = new HashSet<string>();       // ya tuvo su oportunidad (salga o no)
        private static readonly HashSet<string> _risen = new HashSet<string>();        // se levanto: solo a por ti, sin Quick Revive
        private static float _lastCombat = -99f;
        private static Vector3 _lastCombatAt;

        internal static void Clear() { _down.Clear(); _rolled.Clear(); _risen.Clear(); }
        internal static bool IsRevived(string profileId) { return profileId != null && _risen.Contains(profileId); }
        internal static bool IsDowned(string profileId) { return profileId != null && _down.ContainsKey(profileId); }
        internal static bool IsDowned(BotOwner b) { var p = b != null ? b.GetPlayer : null; return p != null && _down.ContainsKey(p.ProfileId); }

        internal static void NoteCombat(Vector3 at) { _lastCombat = Time.time; _lastCombatAt = at; }

        // desde BotPerks.OnDamage (victima con Quick Revive). Devuelve el dano que de verdad recibe.
        internal static float OnDamage(Player p, EBodyPart part, float damage)
        {
            var hc = p.ActiveHealthController;
            if (hc == null) return damage;
            if (_down.ContainsKey(p.ProfileId))
            {
                if (part != EBodyPart.Head) return 0f;
                _down.Remove(p.ProfileId);                                   // tiro a la cabeza: rematado
                if (Plugin.Me != null && (Plugin.Me.Position - p.Position).sqrMagnitude < 150f * 150f)
                    Hud.Notify(L.T("perk.quickrevive").ToUpperInvariant(), L.T("notice.botfinished"), QuickRevive.Id);
                Dbg.Log("BOTS", p.Profile.Nickname + " (Quick Revive): rematado de un tiro a la cabeza");
                return Mathf.Max(damage, hc.GetBodyPartHealth(EBodyPart.Head).Current + 1f);
            }
            if (part != EBodyPart.Head && part != EBodyPart.Chest) return damage;
            float cur = hc.GetBodyPartHealth(part).Current;
            if (damage < cur || _rolled.Contains(p.ProfileId) || IsRevived(p.ProfileId)) return damage;
            _rolled.Add(p.ProfileId);
            if (UnityEngine.Random.value >= Chance) return damage;
            GoDown(p);
            return Mathf.Max(0f, cur - 1f);
        }

        private static void GoDown(Player p)
        {
            _down[p.ProfileId] = new Down { P = p, Since = Time.time };
            var bot = p.AIData != null ? p.AIData.BotOwner : null;
            try { if (bot != null) { bot.ShootData?.EndShoot(); bot.Mover?.Stop(); } } catch { }
            try { if (!p.IsInPronePose) p.ToggleProne(); } catch { }
            if (Plugin.Me != null && (Plugin.Me.Position - p.Position).sqrMagnitude < 150f * 150f)
                Hud.Notify(L.T("perk.quickrevive").ToUpperInvariant(), L.T("notice.botdowned"), QuickRevive.Id);
            Dbg.Log("BOTS", p.Profile.Nickname + " (Quick Revive): caido a 1 PV");
        }

        // 2 veces por segundo (BotPerks.Tick)
        internal static void Tick(Player me)
        {
            if (_down.Count == 0 || me == null) return;
            float now = Time.time;
            foreach (var d in _down.Values.ToList())
            {
                var p = d.P;
                var hc = p != null ? p.ActiveHealthController : null;
                if (hc == null || !hc.IsAlive) { _down.Remove(d.P != null ? d.P.ProfileId : ""); continue; }
                // se queda a 1 PV: que ni el sangrado lo mate
                foreach (var vital in new[] { EBodyPart.Head, EBodyPart.Chest })
                    if (hc.GetBodyPartHealth(vital).Current < 1f) hc.ChangeHealth(vital, 1f - hc.GetBodyPartHealth(vital).Current, DamageHelper.UndefinedDamage);
                try { if (!p.IsInPronePose) p.ToggleProne(); } catch { }
                bool calm = now - d.Since >= CalmSeconds && (now - _lastCombat >= CalmSeconds || (_lastCombatAt - p.Position).sqrMagnitude > NearCombat * NearCombat);
                if (!calm) continue;
                if ((me.Position - p.Position).sqrMagnitude < FarMeters * FarMeters && Sees(me, p)) continue;
                Rise(d);
            }
        }

        private static bool Sees(Player me, Player p)
        {
            var eye = me.Fireport != null ? me.Fireport.position : me.Position + Vector3.up * 1.6f;
            return !Physics.Linecast(eye, p.Position + Vector3.up * 0.4f, LayerMask.GetMask("HighPolyCollider", "LowPolyCollider", "Terrain"));
        }

        private static void Rise(Down d)
        {
            var p = d.P;
            _down.Remove(p.ProfileId);
            _risen.Add(p.ProfileId);
            var hc = p.ActiveHealthController;
            try { hc.RestoreFullHealth(); }
            catch
            {
                foreach (EBodyPart part in new[] { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.LeftArm, EBodyPart.RightArm, EBodyPart.LeftLeg, EBodyPart.RightLeg })
                {
                    var v = hc.GetBodyPartHealth(part);
                    hc.ChangeHealth(part, v.Maximum - v.Current, DamageHelper.MedKitUse);
                }
            }
            try { if (p.IsInPronePose) p.ToggleProne(); } catch { }
            BotPerks.ScanSoon(p.ProfileId);                                  // pasa a tener Death Perception y pierde Quick Revive
            var bot = p.AIData != null ? p.AIData.BotOwner : null;
            var me = Plugin.Me;
            try { if (me != null && bot != null && bot.BotsGroup != null && !bot.BotsGroup.IsEnemy(me)) bot.BotsGroup.AddEnemy(me, EBotEnemyCause.initial); } catch { }
            Hud.Notify(L.T("perk.quickrevive").ToUpperInvariant(), L.T("notice.botrevived"), QuickRevive.Id);
            Dbg.Log("BOTS", p.Profile.Nickname + " (Quick Revive): se levanta con toda la vida");
        }
    }

    // Caido: la IA no hace nada (ni moverse, ni apuntar, ni disparar, ni curarse)
    [HarmonyPatch(typeof(BotOwner), nameof(BotOwner.UpdateManual))]
    internal static class DownedBotUpdatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(BotOwner __instance) { return !BotRevive.IsDowned(__instance); }
    }

    [HarmonyPatch(typeof(BotOwner), nameof(BotOwner.FixedUpdate))]
    internal static class DownedBotFixedPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(BotOwner __instance) { return !BotRevive.IsDowned(__instance); }
    }
}
