using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Stamin-Up: mas estamina de piernas y mejoras para correr / rushear.
    //   Base: la estamina de las PIERNAS (la barra de correr) sube x1.5 durante el resto de la raid.
    //   Mejoras: Rush Hour, Second Wind, Sprint Shooter, Light Feet, Quick Recovery, Soft Landing, Extra Slot.
    //   Desventajas ligadas: Loud Boots, Jelly Legs, Burnout, Hungry Legs, Cramps, Sweaty.
    // Lo que se consulta cada frame son campos simples; el calculo pesado solo ocurre mientras la perk esta activa.
    internal static class StaminUp
    {
        internal const string Id = "staminup";
        internal const string ItemTpl = "6a1c00000000000000000c02";

        // --- balance ---
        internal const float CapacityMult = 1.5f;       // base: estamina de piernas x1.5
        internal const float KillWindow = 180f;          // Rush Hour (desafio): 3 bajas en menos de 3 min
        internal const float LowHealth = 0.30f;          // Second Wind: salud "en rojo" (parte vital o total por debajo del 30%)
        internal const float WindSeconds = 5f;           //   duracion de la estamina infinita y la inmunidad al dolor (sin cooldown: se reactiva cada vez que la salud vuelve a estar en rojo)
        internal const float WeightMult = 0.90f;         // Light Feet: -10% de peso
        internal const float RecoveryBonus = 0.25f;      // Quick Recovery: +25% de recuperacion
        internal const float LandingMult = 0.70f;        // Soft Landing: -30% de dano por caida
        internal const float AnimSpeed = 1.75f;          // Sprint Shooter: las animaciones del arma van siempre x1.75
        internal const float BootsRadius = 25f;          // Loud Boots: ruido de pasos al correr (m)
        internal const float BootsInterval = 0.5f;
        internal const float BurnoutLow = 0.30f;         // Burnout: por debajo del 30% de estamina se recupera a la mitad
        internal const float BurnoutMult = 0.5f;
        internal const float JellyAfter = 5f;            // Jelly Legs: tras correr 5 s seguidos, balanceo +25% durante 6 s
        internal const float JellySeconds = 6f;
        internal const float HungryExtra = 0.5f;         // Hungry Legs / Sweaty: +50% de gasto de energia / hidratacion mientras corres
        internal const float CrampsSeconds = 2f;         // Cramps: 2 s mas sin recuperar estamina al dejar de correr
        internal const float BelowMeters = 2f;           // Soft Landing (desafio): el enemigo estaba >=2 m por debajo
        internal const float RushBSeconds = 180f;        // logro Rush B: 2 bajas en los 3 primeros minutos

        // IDs de los bebidas energeticas del desafio de Sprint Shooter: Max Energy, Hot Rod, RatCola
        internal static readonly string[] EnergyDrinks = { "5751435d24597720a27126d1", "5751496424597720a27126da", "60b0f93284c20f0feb453da7" };

        // --- estado dinamico (por raid) ---
        internal static bool JellyOn;                    // balanceo extra activo (lo lee JitterPatch)
        private static float _raidStart, _windUntil, _sprintStart, _jellyUntil, _nextBoots, _nextDrain;
        private static bool _wasSprinting, _exhaustArmed = true;
        private static IAnimator _spedAnimator;                // animador al que se le acelero la velocidad (para devolverla a 1)
        private static int _kills, _doubleKills;
        private static float _distance;
        private static Vector3 _lastPos;
        private static bool _hasPos;
        private static readonly List<float> _killTimes = new List<float>();
        private static readonly List<float> _earlyKills = new List<float>();

        internal static void OnRaidStart()
        {
            _raidStart = Time.time;
            JellyOn = false;
            _windUntil = 0f; _sprintStart = 0f; _jellyUntil = 0f; _nextBoots = 0f; _nextDrain = 0f;
            _wasSprinting = false; _exhaustArmed = true; _spedAnimator = null;
            _kills = 0; _doubleKills = 0; _distance = 0f; _hasPos = false;
            _killTimes.Clear(); _earlyKills.Clear();
        }

        internal static bool Active { get { return Perks.IsActive(Id); } }

        // ---------------------------------------------------------------- al beberla
        internal static string Notice() { return L.F("notice.stamin.sub", CapacityMult.ToString("0.0")); }

        internal static void OnDrink(ActiveHealthController health)
        {
            var me = health.Player;
            Dbg.Log("BEBIDA", "Stamin-Up: estamina de piernas x" + CapacityMult.ToString("0.0"));
            Achievements.Unlock("st_fresh");
            ApplyCapacity(me);
            _lastPos = me.Position; _hasPos = true;
        }

        // La capacidad se multiplica en el calculo del juego (CapacityPatch); aqui se fuerza que lo recalcule y se conserva la proporcion llena.
        private static void ApplyCapacity(Player me)
        {
            var phys = me.Physical;
            if (phys == null || phys.Stamina == null) return;
            float before = phys.Stamina.TotalCapacity;
            phys.Stamina.TotalCapacity.SetDirty();
            float after = phys.Stamina.TotalCapacity;
            if (before > 0f) phys.Stamina.Current = Mathf.Min(after, phys.Stamina.Current * after / before);
            Dbg.Log("EFECTO", "Stamin-Up: capacidad de piernas " + before.ToString("0") + " -> " + after.ToString("0"));
            // Light Feet cambia el peso: el juego lo recalcula al avisarle
            (phys._playerBridge as PhysicalBase.PlayerBridge)?.CallOnTotalWeightUpdated();
        }

        // ---------------------------------------------------------------- cada frame (solo si la perk esta activa)
        internal static void Tick(Player me)
        {
            if (!Active) { ResetFlags(me); return; }
            var phys = me.Physical;
            if (phys == null || phys.Stamina == null) return;
            float dt = Time.deltaTime, now = Time.time;
            var stamina = phys.Stamina;
            bool sprinting = phys.Sprinting;
            if (me.ProceduralWeaponAnimation != null) Perk.MyBreath = me.ProceduralWeaponAnimation.Breath;   // lo usa JitterPatch (Jelly Legs)
            float cap = stamina.TotalCapacity;

            // ---- distancia corriendo (desafios Light Feet / Quick Recovery y logro Marathon man) ----
            var pos = me.Position;
            if (_hasPos && sprinting)
            {
                var d = new Vector2(pos.x - _lastPos.x, pos.z - _lastPos.z).magnitude;
                if (d < 5f) _distance += d;                // ignora teletransportes
            }
            _lastPos = pos; _hasPos = true;
            while (_distance >= 100f)
            {
                _distance -= 100f;
                PerkService.AddProgress("st_feet", 100);
                PerkService.AddProgress("st_recover", 100);
                PerkService.AddStat("staminup_m", 100);
                if (PerkService.Stat("staminup_m") >= 20000) Achievements.Unlock("st_marathon");
            }

            // ---- inicio / fin de carrera ----
            if (sprinting && !_wasSprinting) _sprintStart = now;
            if (!sprinting && _wasSprinting)
            {
                // Jelly Legs: si se corrio 5 s seguidos, el arma se balancea mas un rato
                if (Perk.Has("dt_jelly") && now - _sprintStart >= JellyAfter) _jellyUntil = now + JellySeconds;
                // Cramps: unos segundos mas sin recuperar estamina
                if (Perk.Has("dt_cramps")) stamina.DisableRestoration = Mathf.Max(stamina.DisableRestoration, now + CrampsSeconds);
            }
            _wasSprinting = sprinting;
            JellyOn = now < _jellyUntil;

            // ---- recuperacion: Quick Recovery (+) y Burnout (-). Mismo calculo que el juego, solo cuando se esta recuperando ----
            if (!sprinting && stamina.Current < cap && now > stamina.DisableRestoration)
            {
                float rest = (float)stamina.SelfRestoration * dt * stamina.Multiplier;
                if (Perk.Has("st_recover")) stamina.Current = Mathf.Min(cap, stamina.Current + rest * RecoveryBonus);
                if (Perk.Has("dt_burn") && stamina.Current < cap * BurnoutLow) stamina.Current = Mathf.Max(0f, stamina.Current - rest * (1f - BurnoutMult));
            }

            // ---- agotarse (desafio de Second Wind) ----
            if (stamina.Current <= 0.5f && _exhaustArmed) { _exhaustArmed = false; Dbg.Log("DESAFIO", "estamina agotada"); PerkService.AddProgress("st_wind"); }
            else if (stamina.Current > cap * 0.3f) _exhaustArmed = true;

            // ---- Second Wind: cada vez que la salud esta en rojo, 5 s de estamina infinita e inmunidad al dolor (sin cooldown) ----
            if (now >= _windUntil && Perk.Has("st_wind") && me.HealthController.IsAlive && IsNearDeath(me.HealthController))
            {
                _windUntil = now + WindSeconds;
                me.ActiveHealthController.AddEffect<ActiveHealthController.PainKiller>(EBodyPart.Head, 0f, WindSeconds, 0f);
                Hud.Notify(Data.Name("st_wind"), L.T("notice.wind.sub"), Id);
                Dbg.Log("EFECTO", "Second Wind: salud en rojo, " + WindSeconds + " s de estamina infinita e inmunidad al dolor");
            }
            if (now < _windUntil)
            {
                stamina.Current = cap;
                if (phys.HandsStamina != null) phys.HandsStamina.Current = phys.HandsStamina.TotalCapacity;
                if (phys.Oxygen != null) phys.Oxygen.Current = phys.Oxygen.TotalCapacity;
            }

            // ---- Loud Boots / Hungry Legs / Sweaty: solo mientras se corre ----
            if (sprinting)
            {
                if (Perk.Has("dt_boots") && now >= _nextBoots)
                {
                    _nextBoots = now + BootsInterval;
                    Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, BootsRadius, AISoundType.step);
                }
                if (now >= _nextDrain && (Perk.Has("dt_hungry") || Perk.Has("dt_sweaty")))
                {
                    float step = now - _nextDrain + 1f;
                    _nextDrain = now + 1f;
                    var hc = me.HealthController;
                    if (Perk.Has("dt_hungry") && hc.EnergyRate < 0f) me.ActiveHealthController.ChangeEnergy(hc.EnergyRate * HungryExtra * step);
                    if (Perk.Has("dt_sweaty") && hc.HydrationRate < 0f) me.ActiveHealthController.ChangeHydration(hc.HydrationRate * HungryExtra * step);
                }
            }
            else _nextDrain = now + 1f;

            // ---- Sprint Shooter: animaciones aceleradas un instante tras dejar de correr ----
            UpdateAnimSpeed(me);
        }

        private static bool IsNearDeath(IHealthController hc)
        {
            return hc.GetBodyPartHealth(EBodyPart.Common).Normalized <= LowHealth
                || hc.GetBodyPartHealth(EBodyPart.Head).Normalized <= LowHealth
                || hc.GetBodyPartHealth(EBodyPart.Chest).Normalized <= LowHealth;
        }

        // Sprint Shooter: mientras este equipada, las animaciones del arma van siempre aceleradas. Al cambiar de arma el animador
        // es otro (velocidad 1), asi que se reaplica cada frame; al desequiparla o acabar la raid se devuelve a 1.
        private static void UpdateAnimSpeed(Player me)
        {
            var animator = me.HandsController != null && me.HandsController.FirearmsAnimator != null ? me.HandsController.FirearmsAnimator.Animator : null;
            if (animator != null && Perk.Has("st_shooter"))
            {
                if (!ReferenceEquals(_spedAnimator, animator) && _spedAnimator != null) _spedAnimator.speed = 1f;
                animator.speed = AnimSpeed;
                _spedAnimator = animator;
            }
            else if (_spedAnimator != null)
            {
                _spedAnimator.speed = 1f;
                _spedAnimator = null;
            }
        }

        // Si la perk deja de estar activa (nueva raid) no debe quedar nada tocado.
        private static void ResetFlags(Player me)
        {
            JellyOn = false;
            if (_spedAnimator != null) { _spedAnimator.speed = 1f; _spedAnimator = null; }
        }

        // ---------------------------------------------------------------- bajas del jugador (evento estatico del juego)
        internal static void OnPlayerDead(Player victim, IPlayer aggressor, DamageInfo info, EBodyPart part)
        {
            try
            {
                var me = Plugin.Me;
                if (me == null || victim == null || aggressor == null || victim == me) return;
                if (aggressor.ProfileId != me.ProfileId) return;                           // solo bajas del jugador
                if (!Active) return;

                float now = Time.time;
                _kills++;
                bool boss = victim.Profile != null && victim.Profile.Info != null && victim.Profile.Info.Settings != null
                            && victim.Profile.Info.Settings.Role.ToString().StartsWith("boss", StringComparison.OrdinalIgnoreCase);
                bool below = me.Position.y - victim.Position.y >= BelowMeters;
                Dbg.Log("BAJA", "Stamin-Up: baja #" + _kills + " boss=" + boss + " porDebajo=" + below);

                // Rush Hour: toda la estamina de piernas
                if (Perk.Has("st_rush") && me.Physical != null && me.Physical.Stamina != null)
                {
                    me.Physical.Stamina.Current = me.Physical.Stamina.TotalCapacity;
                    Dbg.Log("EFECTO", "Rush Hour: estamina de piernas completa");
                }

                // desafios
                _killTimes.Add(now);
                _killTimes.RemoveAll(t => now - t > KillWindow);
                if (_killTimes.Count >= 3) { PerkService.AddProgress("st_rush"); _killTimes.Clear(); }
                if (below) PerkService.AddProgress("st_land");
                if (boss) PerkService.AddProgress("st_slot");

                // logros
                _earlyKills.Add(now);
                if (now - _raidStart <= RushBSeconds && _earlyKills.Count(t => t - _raidStart <= RushBSeconds) >= 2) Achievements.Unlock("st_rushb");
                if (Plugin.SpeedColaActive && ++_doubleKills >= 5) Achievements.Unlock("st_double");
            }
            catch (Exception e) { Plugin.Log.LogError("Stamin-Up (baja): " + e); }
        }

        // Cantidad de bebidas energeticas en el inventario (desafio de Sprint Shooter): si sube durante la raid, se han encontrado.
        internal static int CountDrinks(Player me)
        {
            int n = 0;
            foreach (var it in me.Profile.Inventory.GetPlayerItems(EFT.InventoryLogic.EPlayerItems.Equipment))
                if (EnergyDrinks.Contains(it.StringTemplateId)) n += it.StackObjectsCount;
            return n;
        }
    }

    // ---------- base: la estamina de piernas se multiplica en el calculo del juego ----------
    [HarmonyPatch(typeof(Physical), nameof(Physical.GetStaminaCapacityFunc))]
    internal static class StaminUpCapacityPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Physical __instance, ref float __result)
        {
            if (!StaminUp.Active) return;
            var me = Plugin.Me;
            if (me == null || !ReferenceEquals(me.Physical, __instance)) return;
            __result *= StaminUp.CapacityMult;
        }
    }

    // ---------- Light Feet: -10% de peso transportado ----------
    [HarmonyPatch(typeof(PhysicalBase.PlayerBridge), "get_TotalWeight")]
    internal static class StaminUpWeightPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PhysicalBase.PlayerBridge __instance, ref float __result)
        {
            if (!Perk.Has("st_feet")) return;
            if (!Util.IsMe(__instance._iPlayer)) return;
            __result *= StaminUp.WeightMult;
        }
    }

    // ---------- Soft Landing: -30% de dano por caida ----------
    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.HandleFall))]
    internal static class StaminUpFallPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ActiveHealthController __instance, out float __state)
        {
            __state = -1f;
            if (!Perk.Has("st_land") || !Util.IsMe(__instance.Player) || __instance.Player.Physical == null) return;
            __state = __instance.Player.Physical.FallDamageMultiplier;
            __instance.Player.Physical.FallDamageMultiplier = __state * StaminUp.LandingMult;
            Dbg.Log("EFECTO", "Soft Landing: dano por caida x" + (__state * StaminUp.LandingMult).ToString("0.00"));
        }

        [HarmonyPostfix]
        private static void Postfix(ActiveHealthController __instance, float __state)
        {
            if (__state >= 0f && __instance.Player != null && __instance.Player.Physical != null) __instance.Player.Physical.FallDamageMultiplier = __state;
        }
    }
}
