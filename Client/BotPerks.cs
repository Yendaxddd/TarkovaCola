using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Perks en los demas: los enemigos que llevan una perk encima (en el inventario) tienen su efecto, y los companeros de
    //  PIT Fireteam las que les equipes en sus 4 casillas (PitPerks; las pierden si mueren). F12: "Efectos de perks en
    //  enemigos y companeros".
    //   Juggernog: reciben la mitad de dano | Speed Cola: disparan un 30% mas rapido | Stamin-Up: estamina infinita e inmunes al dolor
    //   Widow's Wine: sus disparos frenan a quien aciertan | PHD Flopper: inmunes a explosiones y caidas
    //   Bandolier: aparecen con municion extra | Vulture Aid: aparecen con un objeto valioso | Melee Macchiato: estamina infinita y
    //   golpes cuerpo a cuerpo x2 | Deadshot: saben donde estan sus enemigos aunque haya paredes (hasta 80 m)
    //  (Los Embotelladores de las misiones tienen sus propias reglas en QuestGuards.)
    // ============================================================================================
    internal static class BotPerks
    {
        internal const float JugTaken = 0.5f;
        internal const float FireRate = 1.3f;
        internal const float WebLeft = 0.6f, WebSeconds = 3f, WebCooldown = 4f;
        internal const float MeleeMult = 2f;
        internal const float DeadshotRange = 80f, DeadshotEvery = 2f;
        internal const int MaxFollowerPerks = 4;
        private const float Rescan = 15f;

        // objetos valiosos con los que puede aparecer un enemigo con Vulture Aid
        private static readonly string[] Valuables =
        {
            "5c0530ee86f774697952d952" /* LEDX */, "57347ca924597744596b4e71" /* tarjeta grafica */, "59faff1d86f7746c51718c9c" /* bitcoin */,
            "5bc9bc53d4351e00367fbcee" /* gallo de oro */, "5c94bbff86f7747ee735c08f" /* tarjeta de Labs */,
        };

        private class Info { public Player P; public HashSet<string> Perks = new HashSet<string>(); public float NextScan; public bool Bonus; public bool Follower; public bool Dead; }
        private static readonly Dictionary<string, Info> _byId = new Dictionary<string, Info>();
        private static readonly Dictionary<object, Info> _byInv = new Dictionary<object, Info>();
        private static readonly Dictionary<string, float> _nextWeb = new Dictionary<string, float>();
        private static float _next, _nextSight;
        private static string _raidKey;

        internal static bool Any { get { return _byId.Count > 0; } }

        internal static bool Has(Player p, string perk)
        {
            Info i;
            return p != null && _byId.TryGetValue(p.ProfileId, out i) && i.Perks.Contains(perk);
        }

        // ---------------------------------------------------------------- PIT Fireteam (opcional: se busca por reflexion)
        private static bool _pitLooked;
        private static MethodInfo _isFollower;
        internal static bool IsFollower(Player p)
        {
            if (!_pitLooked)
            {
                _pitLooked = true;
                try { _isFollower = AccessTools.Method(AccessTools.TypeByName("pitTeam.Modules.BossPlayers"), "IsFollowerProfileId"); } catch { }
                Plugin.Log.LogInfo("PIT Fireteam " + (_isFollower != null ? "encontrado: sus companeros pueden llevar perks" : "no esta instalado"));
            }
            if (_isFollower == null) return false;
            try { return (bool)_isFollower.Invoke(null, new object[] { p.ProfileId }); } catch { return false; }
        }

        // ---------------------------------------------------------------- 2 veces por segundo (Plugin.Update)
        internal static void Tick()
        {
            if (!Plugin.InRaid || ArenaCompat.InArena || !Plugin.CfgBotPerks.Value) { if (_byId.Count > 0) Clear(); return; }
            float now = Time.time;
            if (now < _next) return;
            _next = now + 0.5f;
            var me = Plugin.Me;
            var world = Singleton<GameWorld>.Instance;
            if (me == null || world == null || world.AllAlivePlayersList == null) return;
            string key = world.LocationId + "|" + me.ProfileId + "|" + world.GetHashCode();
            if (key != _raidKey) { _raidKey = key; Clear(); }

            // companeros muertos: pierden sus perks
            foreach (var i in _byId.Values)
                if (!i.Dead && i.Follower && i.P != null && i.P.HealthController != null && !i.P.HealthController.IsAlive) { i.Dead = true; PitPerks.OnDeath(i.P); }

            bool sight = now >= _nextSight;
            if (sight) _nextSight = now + DeadshotEvery;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p.IsYourPlayer || p.HealthController == null || !p.HealthController.IsAlive) continue;
                Info info;
                if (!_byId.TryGetValue(p.ProfileId, out info)) { info = new Info { P = p }; _byId[p.ProfileId] = info; }
                if (now >= info.NextScan) { info.NextScan = now + Rescan; Scan(info); }
                if (info.Perks.Count == 0) continue;
                try { Apply(info, sight); }
                catch (Exception e) { Dbg.Throttled("botperks", 10f, "ERROR", "perks en bots: " + e.Message); }
            }
        }

        private static void Clear() { _byId.Clear(); _byInv.Clear(); _nextWeb.Clear(); }

        // al cambiar sus casillas: se vuelve a mirar en el siguiente tick
        internal static void ScanSoon(string profileId)
        {
            Info i;
            if (profileId != null && _byId.TryGetValue(profileId, out i)) i.NextScan = 0f;
        }

        // Que perks lleva (los companeros: las de sus casillas)
        private static void Scan(Info info)
        {
            var p = info.P;
            info.Perks.Clear();
            var slots = PitPerks.Of(p.ProfileId);
            info.Follower = slots.Count > 0 || IsFollower(p);
            if (info.Follower) foreach (var id in slots.Take(MaxFollowerPerks)) info.Perks.Add(id);
            else try
            {
                foreach (var it in p.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment))
                {
                    var def = Perks.ByItem(it?.TemplateId.ToString());
                    if (def != null) info.Perks.Add(def.Id);
                }
            }
            catch { }
            if (info.Perks.Count > 0)
            {
                if (p.InventoryController != null) _byInv[p.InventoryController] = info;
                Dbg.Log("BOTPERKS", (info.Follower ? "companero " : "") + p.Profile?.Info?.Settings?.Role + " " + p.Profile?.Nickname + ": " + string.Join(", ", info.Perks.ToArray()));
            }
            // al aparecer: municion extra (Bandolier) y un objeto valioso (Vulture Aid). Solo enemigos: los companeros conservan su equipo entre raids.
            if (!info.Bonus && info.Perks.Count > 0 && !info.Follower)
            {
                info.Bonus = true;
                if (info.Perks.Contains(Bandolier.Id)) ExtraAmmo(p);
                if (info.Perks.Contains(VultureAid.Id)) NewPerkUtil.Give(p.Profile, Valuables[UnityEngine.Random.Range(0, Valuables.Length)]);
            }
        }

        private static void ExtraAmmo(Player p)
        {
            var w = p.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem as Weapon
                    ?? p.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).ContainedItem as Weapon;
            var top = w != null && w.GetCurrentMagazine() != null ? w.GetCurrentMagazine().Cartridges.Last : null;
            if (top == null) return;
            for (int i = 0; i < 2; i++) NewPerkUtil.Give(p.Profile, top.TemplateId.ToString(), 60);
        }

        private static void Apply(Info info, bool sight)
        {
            var p = info.P;
            var phys = p.Physical;
            // Stamin-Up y Melee Macchiato: estamina infinita; Stamin-Up ademas inmune al dolor
            if ((info.Perks.Contains(StaminUp.Id) || info.Perks.Contains(MeleeMacchiato.Id)) && phys != null && phys.Stamina != null)
                phys.Stamina.Current = phys.Stamina.TotalCapacity;
            if (info.Perks.Contains(StaminUp.Id) && p.ActiveHealthController != null && !p.ActiveHealthController.FindActiveEffects<ActiveHealthController.PainKiller>(EBodyPart.Head).Any())
                p.ActiveHealthController.AddEffect<ActiveHealthController.PainKiller>(EBodyPart.Head, 0f, 3600f, 0f);

            // Deadshot: sabe donde estan sus enemigos cercanos aunque no los vea
            var bot = p.AIData != null ? p.AIData.BotOwner : null;
            if (sight && info.Perks.Contains(Deadshot.Id) && bot != null && bot.BotsGroup != null)
            {
                var world = Singleton<GameWorld>.Instance;
                foreach (var t in world.AllAlivePlayersList)
                {
                    if (t == null || t == p || (t.Position - p.Position).sqrMagnitude > DeadshotRange * DeadshotRange || !bot.BotsGroup.IsEnemy(t)) continue;
                    bot.BotsGroup.SetEnemyPos(t, t.Position, t.WeaponRoot != null ? t.WeaponRoot.position : t.Position, EEnemyPartVisibleType.Sence);
                }
            }
        }

        // ---------------------------------------------------------------- dano (desde DamageHubPatch, para cualquier victima)
        internal static float OnDamage(Player victim, float damage, DamageInfo info)
        {
            // la victima
            Info v;
            if (_byId.TryGetValue(victim.ProfileId, out v) && v.Perks.Count > 0)
            {
                var t = info.DamageType;
                if (v.Perks.Contains(PhdFlopper.Id) && (t & (PerkFx.Blast | EDamageType.Fall)) != 0) return 0f;
                if (v.Perks.Contains(Juggernog.Id) && Juggernog.Reduces(t)) damage *= JugTaken;
            }
            // quien ataca
            var by = info.Player != null ? info.Player.iPlayer : null;
            Info a;
            if (by == null || !_byId.TryGetValue(by.ProfileId, out a) || a.Perks.Count == 0) return damage;
            if ((info.DamageType & EDamageType.Melee) != 0 && a.Perks.Contains(MeleeMacchiato.Id)) damage *= MeleeMult;
            if ((info.DamageType & (EDamageType.Bullet | EDamageType.Sniper)) != 0 && a.Perks.Contains(WidowsWine.Id)) Web(victim);
            return damage;
        }

        private static void Web(Player victim)
        {
            float until;
            if (_nextWeb.TryGetValue(victim.ProfileId, out until) && Time.time < until) return;
            _nextWeb[victim.ProfileId] = Time.time + WebCooldown;
            var mc = victim.MovementContext;
            if (mc != null) mc.ChangeSpeedLimit(WebLeft * mc.MaxSpeed, Player.ESpeedLimit.Swamp, WebSeconds);
            WebVfx.OnVictim(victim.Position, WebSeconds);
            if (victim.IsYourPlayer) Hud.Notify(L.T("perk.widowswine").ToUpperInvariant(), L.T("notice.botweb"), WidowsWine.Id);
        }

        // Speed Cola: cadencia de las armas de quien la lleva
        internal static float FireRateMult(Weapon w)
        {
            Info i;
            return w != null && w.Owner != null && _byInv.TryGetValue(w.Owner, out i) && i.Perks.Contains(Perks.SpeedCola.Id) ? FireRate : 1f;
        }
    }

    [HarmonyPatch]
    internal static class BotFireRatePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(Weapon), nameof(Weapon.FireRate));
            yield return AccessTools.PropertyGetter(typeof(Weapon), nameof(Weapon.SingleFireRate));
        }

        [HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref int __result)
        {
            if (!BotPerks.Any) return;
            float m = BotPerks.FireRateMult(__instance);
            if (m != 1f) __result = Mathf.RoundToInt(__result * m);
        }
    }
}
