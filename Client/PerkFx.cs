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
    //  Piezas compartidas por varias perks (Juggernog, PHD Flopper, Widow's Wine y las mejoras nuevas de Stamin-Up).
    //  Cada efecto vive en su perk; aqui estan los ganchos que varias perks necesitan a la vez, para que haya UN solo punto que
    //  toque cada cosa del juego (dano recibido, peso, capacidad de estamina, balanceo, velocidad de animaciones, ruido de pasos...).
    //  Regla de rendimiento: todo gancho muy frecuente sale en la primera linea si no hay perks activas.
    // ============================================================================================
    internal static class PerkFx
    {
        // --- tipos de dano ---
        internal const EDamageType Blast = EDamageType.Explosion | EDamageType.GrenadeFragment | EDamageType.Landmine
            | EDamageType.Artillery | EDamageType.ThermobaricExplosion;
        internal const EDamageType Hits = Blast | EDamageType.Bullet | EDamageType.Sniper | EDamageType.Melee | EDamageType.Blunt | EDamageType.Btr;
        internal const EDamageType Bleeds = EDamageType.LightBleeding | EDamageType.HeavyBleeding;
        internal const EDamageType Hunger = EDamageType.Existence | EDamageType.Dehydration;

        // --- balance ---
        internal const float LungsTime = 1.6f;            // Iron Lungs: +60% de aire
        internal const float LungsRest = 1.5f;            //   y se recupera un 50% mas rapido
        internal const float ChestLow = 0.30f;            // Tight Chest: por debajo del 30% de estamina...
        internal const float ChestSway = 1.10f;           //   ...el balanceo sube un 10%
        internal const float HeavyHandsSway = 1.20f;      // Heavy Hands
        internal const float ConcussSway = 1.40f;         // Concussed
        internal const float ConcussSeconds = 8f;
        internal const float ConcussRadius = 15f;
        internal const float PlatesWeight = 1.10f;        // Heavy Plates
        internal const float PocketsWeight = 1.10f;       // Heavy Pockets
        internal const float FeetWeight = 0.90f;          // Light Feet
        internal const float LazyLegs = 0.85f;            // Lazy Legs: -15% de estamina de piernas
        internal const float DeadWeightDrain = 0.20f;     // Dead Weight: correr gasta un 20% mas
        internal const float BitterDrain = 0.15f;         // Bitter Wine: un 15% mas
        internal const float SluggishDraw = 0.80f;        // Sluggish Reflexes / Greasy Fingers: sacar arma un 20% mas lento
        internal const float GreasyDraw = 0.80f;
        internal const float Metabolism = 0.20f;          // Slow Metabolism / Hungry Spider / Dry Throat: +20%
        // Gasto base de energia / hidratacion por segundo (el metabolismo normal del juego, aprox.). Los extras de las desventajas se calculan
        // sobre esto y NO sobre el ritmo actual del juego, que incluye la fatiga al correr y otros efectos: si no, cuantas mas perks, mas se
        // multiplicaba el gasto mientras sprintabas.
        internal const float BaseEnergyRate = 1f / 60f, BaseHydrationRate = 1f / 45f;
        internal const float ChewTime = 1.30f;            // Slow Chewer: comer y beber tarda un 30% mas
        internal const float PatchSpeed = 2.0f;           // Quick Patch: animaciones de curacion x2
        internal const float PitStopBonus = 1.0f;         // Pit Stop: quieto, la estamina se recupera el doble (+100%)
        internal const float ClankRadius = 12f;           // Clanky Plates
        internal const float StompRadius = 15f;           // Hard Stomp
        internal const float StepInterval = 0.5f;
        internal const float QuickSpinnerAnim = 1.4f;     // Quick Spinner: animaciones de granada +40%
        internal const float AnimSpeed = 1.75f;           // Sprint Shooter

        // --- estado dinamico (por raid) ---
        internal static float LastSprint = -100f;          // ultimo momento en que el jugador esprintaba (desafio de Quick Recovery)
        internal static float SwayMult = 1f;              // balanceo total del arma (lo lee JitterPatch)
        internal static float Speed;                      // velocidad del jugador (m/s, suavizada): "se esta moviendo"
        private static bool _wasBreathD;
        internal static float ConcussUntil;
        private static bool _hasPos;
        private static Vector3 _lastPos;
        private static float _travel, _still, _prevStamina = -1f, _nextStep, _next1Hz;
        private static bool _wasBreath;
        private static IAnimator _sped;

        internal static void OnRaidStart()
        {
            SwayMult = 1f; ConcussUntil = 0f;
            _hasPos = false; _travel = 0f; _still = 0f; _prevStamina = -1f; _nextStep = 0f; _next1Hz = 0f; _wasBreath = false;
            if (_sped != null) { _sped.speed = 1f; _sped = null; }
            Juggernog.OnRaidStart();
            PhdFlopper.OnRaidStart();
            WidowsWine.OnRaidStart();
            Deadshot.OnRaidStart();
            WebVfx.OnRaidStart();
            Speed = 0f; _wasBreathD = false;
        }

        internal static bool IsBoss(Player victim)
        {
            return victim != null && victim.Profile != null && victim.Profile.Info != null && victim.Profile.Info.Settings != null
                   && victim.Profile.Info.Settings.Role.ToString().StartsWith("boss", StringComparison.OrdinalIgnoreCase);
        }

        // salud "en rojo": cualquier parte vital o la salud total por debajo del 30%
        internal static bool NearDeath(IHealthController hc, float low = 0.30f)
        {
            return hc.GetBodyPartHealth(EBodyPart.Common).Normalized <= low
                || hc.GetBodyPartHealth(EBodyPart.Head).Normalized <= low
                || hc.GetBodyPartHealth(EBodyPart.Chest).Normalized <= low;
        }

        // ---------------------------------------------------------------- al beber una perk
        // Recalcula lo que el juego guarda en cache y que depende de las perks activas: capacidad de estamina y de aire, y peso.
        internal static void RefreshPhysical(Player me)
        {
            var phys = me.Physical;
            if (phys == null || phys.Stamina == null) return;
            float before = phys.Stamina.TotalCapacity;
            phys.Stamina.TotalCapacity.SetDirty();
            float after = phys.Stamina.TotalCapacity;
            if (before > 0f) phys.Stamina.Current = Mathf.Min(after, phys.Stamina.Current * after / before);
            if (phys.Oxygen != null)
            {
                float ob = phys.Oxygen.TotalCapacity;
                phys.Oxygen.TotalCapacity.SetDirty();
                phys.Oxygen.SelfRestoration.SetDirty();
                float oa = phys.Oxygen.TotalCapacity;
                if (ob > 0f) phys.Oxygen.Current = Mathf.Min(oa, phys.Oxygen.Current * oa / ob);
            }
            Dbg.Log("EFECTO", "capacidad de piernas " + before.ToString("0") + " -> " + after.ToString("0"));
            (phys._playerBridge as PhysicalBase.PlayerBridge)?.CallOnTotalWeightUpdated();      // el peso lo recalcula el juego al avisarle
        }

        // ---------------------------------------------------------------- cada frame (solo con alguna perk activa)
        internal static void Tick(Player me)
        {
            var hc = me.HealthController;
            if (!Perks.AnyActive || hc == null || !hc.IsAlive) { SwayMult = 1f; ResetAnim(); Deadshot.TickDeadAim(me); return; }
            var phys = me.Physical;
            if (phys == null || phys.Stamina == null) return;
            float dt = Time.deltaTime, now = Time.time;
            var stamina = phys.Stamina;
            float cap = stamina.TotalCapacity;
            bool sprinting = phys.Sprinting;
            if (sprinting) LastSprint = now;
            if (me.ProceduralWeaponAnimation != null) Perk.MyBreath = me.ProceduralWeaponAnimation.Breath;     // lo usa JitterPatch

            // ---- movimiento: distancia recorrida y tiempo quieto (desafios de PHD Flopper) ----
            var pos = me.Position;
            float moved = 0f;
            if (_hasPos)
            {
                moved = new Vector2(pos.x - _lastPos.x, pos.z - _lastPos.z).magnitude;
                if (moved >= 5f) moved = 0f;                  // ignora teletransportes
            }
            _lastPos = pos; _hasPos = true;
            float speed = dt > 0f ? moved / dt : 0f;
            Speed = Mathf.Lerp(Speed, speed, Mathf.Clamp01(dt * 5f));
            bool still = speed < 0.15f;
            ClassicChallenge.OnMove(moved, sprinting, dt);                // desafio de raid (modo Classic): correr, recorrer, sobrevivir
            if (sprinting) { Synergy.OnRun(moved); AchTrack.OnRun(moved); }                          // desafio de la sinergia Speed Cola + Stamin-Up

            if (Perks.IsActive(PhdFlopper.Id))
            {
                _travel += moved;
                while (_travel >= 100f)
                {
                    _travel -= 100f;
                    PerkService.AddProgress("pf_terrain", 100);
                }
                if (still)
                {
                    _still += dt;
                    while (_still >= 10f) { _still -= 10f; PerkService.AddProgress("pf_stop", 10); }
                }
            }

            // ---- Pit Stop: quieto, la estamina de piernas se recupera el doble ----
            if (still && !sprinting && stamina.Current < cap && now > stamina.DisableRestoration && Perk.Has("pf_stop"))
            {
                float rest = (float)stamina.SelfRestoration * dt * stamina.Multiplier;
                stamina.Current = Mathf.Min(cap, stamina.Current + rest * PitStopBonus);
            }

            // ---- Dead Weight / Bitter Wine: correr gasta mas estamina (un extra sobre lo que el juego ya gasto este frame) ----
            float extra = (Perk.Has("dj_dead") ? DeadWeightDrain : 0f) + (Perk.Has("dw_bitter") ? BitterDrain : 0f) + (Perk.Has("dd_d_breath") ? Deadshot.BreathingDrain : 0f);
            if (extra > 0f && sprinting && _prevStamina >= 0f)
            {
                float drop = _prevStamina - stamina.Current;
                if (drop > 0f) stamina.Current = Mathf.Max(0f, stamina.Current - drop * extra);
            }
            float save = Synergy.StaminaSave;                                // sinergia Speed Cola + Stamin-Up: correr gasta algo menos
            if (save > 0f && sprinting && _prevStamina >= 0f)
            {
                float spent = _prevStamina - stamina.Current;
                if (spent > 0f) stamina.Current = Mathf.Min(cap, stamina.Current + spent * save);
            }
            _prevStamina = stamina.Current;

            // ---- Iron Lungs (desafio): aguantar la respiracion ----
            if (Perks.IsActive(StaminUp.Id))
            {
                bool hb = phys.HoldingBreath;
                if (hb && !_wasBreath) PerkService.AddProgress("st_lungs");
                _wasBreath = hb;
            }

            // ---- Steady Pulse (desafio de Deadshot): aguantar la respiracion apuntando ----
            if (Deadshot.Active)
            {
                bool hb = phys.HoldingBreath;
                if (hb && !_wasBreathD && Deadshot.Aiming(me)) { PerkService.AddProgress("dd_steady"); Synergy.OnBreath(); }
                _wasBreathD = hb;
            }

            // ---- balanceo del arma: todos los factores en un solo numero ----
            float sway = 1f;
            if (Perk.JitterOn || StaminUp.JellyOn) sway *= Perk.JitterSway;
            if (cap > 0f && stamina.Current < cap * ChestLow && Perk.Has("dt_chest")) sway *= ChestSway;
            if (Perk.Has("dj_hands")) sway *= HeavyHandsSway;
            if (now < ConcussUntil) sway *= ConcussSway;
            if (Deadshot.Active) sway *= Deadshot.SwayFactor(me, Deadshot.Aiming(me));      // Deadshot: menos balanceo (Stillness, Shaky Hands)
            sway *= Synergy.SwayFactor;                                                       // sinergia PHD Flopper + Deadshot
            SwayMult = sway;
            Deadshot.TickDeadAim(me);                                                         // Dead Aim: arma fija en el centro
            Deadshot.TickStillness(me, Time.deltaTime);                                        // Stillness: recuperas agachado o tumbado

            // ---- ruido de pasos: Clanky Plates (12 m) y Hard Stomp (15 m) mientras te mueves ----
            if (speed > 0.8f && now >= _nextStep)
            {
                // varias desventajas de pasos se acumulan: la mas fuerte y +5 m por cada una de las demas
                float r = 0f; int n = 0;
                if (Perk.Has("dj_clank")) { r = Mathf.Max(r, ClankRadius); n++; }
                if (Perk.Has("dp_stomp")) { r = Mathf.Max(r, StompRadius); n++; }
                if (Perk.Has("dd_d_squeak")) { r = Mathf.Max(r, Deadshot.SqueakRadius); n++; }
                if (n > 1) r += (n - 1) * 5f;
                if (r > 0f)
                {
                    _nextStep = now + StepInterval;
                    Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, r, AISoundType.step);
                }
            }

            // ---- 1 Hz ----
            if (now >= _next1Hz)
            {
                float step = now - _next1Hz + 1f;
                _next1Hz = now + 1f;
                OneHz(me, hc, step);
            }

            UpdateAnim(me);
            WidowsWine.Update(me);
        }

        private static void OneHz(Player me, IHealthController hc, float step)
        {
            // hambre y sed extra: Slow Metabolism (ambas), Hungry Spider (energia), Dry Throat (hidratacion)
            bool m = Perk.Has("dj_metab");
            float e = (m ? Metabolism : 0f) + (Perk.Has("dw_hungry") ? Metabolism : 0f) + (Perk.Has("dd_d_sugar") ? Metabolism : 0f) + (Perk.Has("dd_d_strain") ? Deadshot.StrainEnergy : 0f);
            float h = (m ? Metabolism : 0f) + (Perk.Has("dw_dry") ? Metabolism : 0f) + (Perk.Has("dd_d_lime") ? Metabolism : 0f);
            if (e > 0f && hc.EnergyRate < 0f) me.ActiveHealthController.ChangeEnergy(-BaseEnergyRate * e * step);
            if (h > 0f && hc.HydrationRate < 0f) me.ActiveHealthController.ChangeHydration(-BaseHydrationRate * h * step);

            // salud en rojo con Juggernog: logro de extraer con vida en rojo, y vigilancia de curaciones para los desafios
            if (Perks.IsActive(Juggernog.Id))
            {
                bool red = NearDeath(hc);
                Juggernog.SetRed(red);
                Juggernog.PollHealing(hc);                       // desafios de Heavy Bones y Thick Skin
            }
        }

        // ---------------------------------------------------------------- velocidad de las animaciones de las manos
        // Una sola funcion decide la velocidad del animador del item en mano; si no toca acelerar/frenar nada, lo deja en x1.
        //   arma: Sprint Shooter (menos en recargas, que son cosa de Speed Cola) | granada: Quick Spinner
        //   cura: Quick Patch | comida: Slow Chewer (las perks no sufren Slow Chewer)
        private static void UpdateAnim(Player me)
        {
            var hc = me.HandsController;
            var animator = hc != null && hc.FirearmsAnimator != null ? hc.FirearmsAnimator.Animator : null;
            float want = 1f;
            if (animator != null)
            {
                if (hc is Player.FirearmController)
                {
                    if (Perk.Has("st_shooter") && !((Player.FirearmController)hc).IsInReloadOperation()) want = AnimSpeed;
                }
                else if (hc is Player.BaseGrenadeHandsController)
                {
                    if (Perk.Has("ww_quick")) want = QuickSpinnerAnim;
                    want *= Synergy.PinMult;
                }
                else
                {
                    var item = (hc as Player.ItemHandsController)?.Item;
                    if (item is Meds && !(item is Stimulator)) { if (Perk.Has("jg_patch")) want = PatchSpeed; want *= Synergy.MedicMult; }
                    else if (item is FoodDrink && Perks.ByItem(item.StringTemplateId) == null) { if (Perk.Has("dj_chew")) want = 1f / ChewTime; }
                }
            }

            if (animator != null && want != 1f)
            {
                if (!ReferenceEquals(_sped, animator) && _sped != null) _sped.speed = 1f;
                animator.speed = want;
                _sped = animator;
            }
            else ResetAnim();
        }

        private static void ResetAnim()
        {
            if (_sped != null) { _sped.speed = 1f; _sped = null; }
        }

        // ---------------------------------------------------------------- bajas del jugador (evento estatico del juego)
        internal static void OnKill(Player victim, IPlayer aggressor, DamageInfo info, EBodyPart part)
        {
            try
            {
                var me = Plugin.Me;
                if (me != null && victim == me && ArenaCompat.InArena) { ArenaCompat.OnDeath(); return; }      // Arena: no cuenta para el perfil
                if (me != null && victim == me) { ClassicMode.OnDeath(); Eternal.OnDeath("muerte"); return; }      // (morir de verdad si cuenta)       // el operador murio: Classic pierde sus perks y vuelven las botellas eternas
                StaminUp.OnPlayerDead(victim, aggressor, info, part);
                if (me == null || victim == null || aggressor == null || aggressor.ProfileId != me.ProfileId) return;
                ClassicChallenge.OnKill(me, victim, info, part);                          // desafio de la raid (modo Classic)
                AchTrack.OnKillAny(me, victim, part);                                     // logros de bajas
                Powerup.OnKill(me, victim);                                               // puede caer una perk aleatoria
                if (Perks.IsActive(StaminUp.Id) && Time.time - LastSprint <= 3f) PerkService.AddProgress("st_recover");      // Quick Recovery: baja justo despues de esprintar
                if (!Perks.AnyActive) return;

                bool boss = IsBoss(victim);
                bool grenade = (info.DamageType & (EDamageType.Explosion | EDamageType.GrenadeFragment)) != 0;
                bool jug = Perks.IsActive(Juggernog.Id), phd = Perks.IsActive(PhdFlopper.Id), wid = Perks.IsActive(WidowsWine.Id);
                Dbg.Log("BAJA", "baja de " + victim.Profile.Info.Settings.Role + ": jefe=" + boss + " granada=" + grenade + " tipo=" + info.DamageType);

                if (boss)
                {
                    if (jug) PerkService.AddProgress("jg_slot");
                    if (phd) PerkService.AddProgress("pf_slot");
                    if (wid) PerkService.AddProgress("ww_slot");
                }
                if (grenade)
                {
                    if (phd) { PerkService.AddProgress("pf_shock"); PhdFlopper.OnGrenadeKill(); }
                    if (wid) WidowsWine.OnGrenadeKill();
                }
                bool webbed = wid && WidowsWine.IsWebbed(victim);
                if (webbed) PerkService.AddProgress("ww_venom");
                Synergy.OnKill(me, victim, webbed, Deadshot.Armored(victim));
                if (Deadshot.Active) Deadshot.OnKill(me, victim, info, part);
            }
            catch (Exception e) { Plugin.Log.LogError("baja (perks): " + e); }
        }

        // ---------------------------------------------------------------- fin de raid (con el resultado de la raid)
        internal static void OnRaidEnd(ExitStatus status)
        {
            try
            {
                bool alive = status == ExitStatus.Survived || status == ExitStatus.Runner;
                Dbg.Log("RAID", "fin de raid: " + status + " (con vida=" + alive + ")");
                if (status == ExitStatus.Left && Plugin.CfgLeftKeeps.Value)
                {
                    // abandonar / desconectarse: como si la raid no hubiera pasado (mods que deshacen la raid): no se pierde nada
                    Plugin.Log.LogInfo("Fin de raid por abandono/desconexion: se conservan las perks de Clasico y las Eternas");
                    return;
                }
                if (ArenaCompat.InArena) { ArenaCompat.OnRaidEnd(); return; }       // partida de Arena: nada de esto cuenta para el perfil
                Juggernog.OnRaidEnd(alive);
                AchTrack.OnRaidEnd(alive);
                ClassicMode.OnRaidEnd(status);
                Eternal.OnRaidEnd(status);
            }
            catch (Exception e) { Plugin.Log.LogError("fin de raid (perks): " + e); }
        }
    }

    // ============================================================================================
    //  Ganchos compartidos
    // ============================================================================================

    // ---------- estamina de piernas: Stamin-Up (x1.5) y Lazy Legs (-15%) ----------
    [HarmonyPatch(typeof(Physical), nameof(Physical.GetStaminaCapacityFunc))]
    internal static class StaminaCapacityPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Physical __instance, ref float __result)
        {
            if (QuestGuards.Any && QuestGuards.HasObj(__instance, StaminUp.Id)) { __result *= QuestGuards.StaminaMult; return; }     // Embotellador con Stamin-Up
            if (!Perks.AnyActive) return;
            var me = Plugin.Me;
            if (me == null || !ReferenceEquals(me.Physical, __instance)) return;
            float m = 1f;
            if (StaminUp.Active) m *= 1f + (StaminUp.CapacityMult - 1f) * Perks.Power(StaminUp.Id);     // como modificador (Classic) solo una parte
            if (Perk.Has("dp_lazy")) m *= PerkFx.LazyLegs;
            if (m != 1f) __result *= m;
        }
    }

    // ---------- Iron Lungs: aguantas la respiracion mas y recuperas el aire antes ----------
    [HarmonyPatch(typeof(Physical), nameof(Physical.GetOxygenCapacityFunc))]
    internal static class OxygenCapacityPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Physical __instance, ref float __result)
        {
            if (!Perks.AnyActive) return;
            var me = Plugin.Me;
            if (me == null || !ReferenceEquals(me.Physical, __instance)) return;
            if (Perk.Has("st_lungs")) __result *= PerkFx.LungsTime;
            if (Perk.Has("dd_steady")) __result *= Deadshot.PulseBreath;     // Steady Pulse (Deadshot)
            __result *= Synergy.OxygenMult;                                  // sinergia Stamin-Up + Deadshot
        }
    }

    [HarmonyPatch(typeof(Physical), nameof(Physical.GetOxygenRestorationFunc))]
    internal static class OxygenRestorationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Physical __instance, ref float __result)
        {
            if (!Perks.IsActive(StaminUp.Id)) return;
            var me = Plugin.Me;
            if (me == null || !ReferenceEquals(me.Physical, __instance) || !Perk.Has("st_lungs")) return;
            __result *= PerkFx.LungsRest;
        }
    }

    // ---------- peso transportado: Light Feet (-10%), Heavy Plates (+10%), Heavy Pockets (+10%) ----------
    [HarmonyPatch(typeof(PhysicalBase.PlayerBridge), "get_TotalWeight")]
    internal static class WeightPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PhysicalBase.PlayerBridge __instance, ref float __result)
        {
            if (!Perks.AnyActive || !Util.IsMe(__instance._iPlayer)) return;
            float m = 1f;
            if (Perk.Has("st_feet")) m *= PerkFx.FeetWeight;
            if (Perk.Has("dj_plates")) m *= PerkFx.PlatesWeight;
            if (Perk.Has("dp_pockets")) m *= PerkFx.PocketsWeight;
            m *= Synergy.WeightMult;                                     // sinergia Stamin-Up + Juggernog
            if (m != 1f) __result *= m;
        }
    }

    // ---------- Un unico gancho para el dano que recibe y que causa el jugador ----------
    // Prioridad ultima: corre despues de los demas ganchos que ya tocan el dano (Steady Fingers...).
    // Recibido: PHD Flopper (inmunidad) -> Juggernog (reduccion y mejoras) -> Widow's Wine (seguimiento de Web Burst).
    // Causado: PHD Flopper (Blast Absorber).
    [HarmonyPatch(typeof(ActiveHealthController), "ApplyDamage")]
    internal static class DamageHubPatch
    {
        [HarmonyPriority(Priority.Last)]
        [HarmonyPrefix]
        private static void Prefix(ActiveHealthController __instance, EBodyPart bodyPart, ref float damage, DamageInfo damageInfo)
        {
            if (damage <= 0f || (!Perks.AnyActive && !QuestGuards.Any)) return;
            try
            {
                var victim = __instance.Player;
                if (victim == null) return;
                if (Util.IsMe(victim))
                {
                    if (QuestGuards.Any) QuestGuards.OnHitMe(victim, damageInfo);         // un Embotellador con Widow's Wine te frena
                    if (!Perks.AnyActive) return;
                    if (Perks.IsActive(PhdFlopper.Id)) damage = PhdFlopper.OnDamage(bodyPart, damage, damageInfo);
                    if (damage <= 0f) return;
                    if (Perks.IsActive(Juggernog.Id)) damage = Juggernog.OnDamage(__instance, bodyPart, damage, damageInfo);
                    if (damage <= 0f) return;
                    if (Synergy.AnyEquipped) damage *= Synergy.DamageMult;      // sinergia Juggernog + Widow's Wine
                    ClassicChallenge.OnDamageTaken(damageInfo);                 // desafio "ghost": solo cuenta el dano que de verdad recibes
                    if (Perks.IsActive(WidowsWine.Id)) WidowsWine.OnDamageTaken(damage);
                }
                else
                {
                    // los Embotelladores (guardias de las misiones) llevan perks de verdad
                    if (QuestGuards.Any && QuestGuards.IsGuard(victim))
                    {
                        var t = damageInfo.DamageType;
                        if (QuestGuards.Has(victim, PhdFlopper.Id) && (t & (PerkFx.Blast | EDamageType.Fall)) != 0) { damage = 0f; return; }
                        if (QuestGuards.Has(victim, Juggernog.Id) && Juggernog.Reduces(t)) damage *= QuestGuards.JugMult;
                    }
                    if (!Perks.AnyActive) return;
                    var me = Plugin.Me;
                    var by = damageInfo.Player != null ? damageInfo.Player.iPlayer : null;
                    if (me != null && by != null && by.ProfileId == me.ProfileId)
                    {
                        if (Synergy.AnyEquipped && Perks.IsActive(WidowsWine.Id) && WidowsWine.IsWebbed(victim)) damage *= Synergy.OutgoingMult;   // Widow's Wine + Deadshot
                        if (Perks.IsActive(PhdFlopper.Id)) PhdFlopper.OnDealt(damage, damageInfo);
                    }
                }
            }
            catch (Exception e) { Dbg.Throttled("dmghub", 10f, "ERROR", "dano (perks): " + e); }
        }

        [HarmonyPostfix]
        private static void Postfix(ActiveHealthController __instance, EBodyPart bodyPart, float damage, DamageInfo damageInfo)
        {
            if (damage <= 0f || !Perks.IsActive(Juggernog.Id) || !Util.IsMe(__instance.Player)) return;
            try { Juggernog.AfterDamage(__instance, bodyPart, damage, damageInfo); }
            catch (Exception e) { Dbg.Throttled("dmgpost", 10f, "ERROR", "dano (Juggernog): " + e); }
        }
    }

    // ---------- Heavy Bones: menos fracturas ----------
    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.DoFracture))]
    internal static class FracturePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ActiveHealthController __instance)
        {
            if (!Perks.IsActive(Juggernog.Id) || !Util.IsMe(__instance.Player)) return true;
            if (Perk.Has("jg_bones") && UnityEngine.Random.value < Juggernog.BonesFractureBlock)
            {
                Dbg.Log("EFECTO", "Heavy Bones: fractura evitada");
                return false;
            }
            return true;
        }
    }

    // ---------- Ear Plugs: inmune a la contusion (y cuenta las contusiones para su desafio) ----------
    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.DoContusion))]
    internal static class ContusionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ActiveHealthController __instance)
        {
            if (!Perks.IsActive(PhdFlopper.Id) || !Util.IsMe(__instance.Player)) return true;
            PerkService.AddProgress("pf_ears");
            if (Perk.Has("pf_ears")) { Dbg.Log("EFECTO", "Ear Plugs: contusion evitada"); return false; }
            return true;
        }
    }

    // ---------- limitadores de velocidad: Soft Soles (caidas) y Terrain Walker (alambre y barro) ----------
    // Hay dos versiones del metodo (con y sin duracion): se parchean las dos.
    [HarmonyPatch]
    internal static class SpeedLimitPatch
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(MovementContext).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == "ChangeSpeedLimit");
        }

        [HarmonyPrefix]
        private static bool Prefix(MovementContext __instance, Player.ESpeedLimit cause)
        {
            if (!Perks.IsActive(PhdFlopper.Id)) return true;
            if (cause != Player.ESpeedLimit.Fall && cause != Player.ESpeedLimit.BarbedWire && cause != Player.ESpeedLimit.Swamp) return true;
            var me = Plugin.Me;
            if (me == null || !ReferenceEquals(me.MovementContext, __instance)) return true;
            if (cause == Player.ESpeedLimit.Fall) return !Perk.Has("pf_soles");
            return !Perk.Has("pf_terrain");
        }
    }

    // ---------- Aterrizajes con PHD Flopper: Stagger y logros de caidas ----------
    // El juego llama a HandleFall en cada frame con el jugador en el suelo (height = lo caido desde el punto mas alto, casi siempre 0):
    // la primera linea descarta todo lo que no sea una caida de verdad.
    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.HandleFall))]
    internal static class FallPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ActiveHealthController __instance, float height)
        {
            if (!Perks.IsActive(PhdFlopper.Id) || !Util.IsMe(__instance.Player)) return;
            PhdFlopper.InFall = true;                                     // dentro de HandleFall: el dano "Fall" que se aplique es una caida de verdad
            if (height >= PhdFlopper.MinFall) PhdFlopper.OnFall(__instance.Player, height);
        }

        [HarmonyFinalizer]
        private static void Finalizer() { PhdFlopper.InFall = false; }
    }

    // ---------- armadura: Bone Plating, Safe Fuse, Brittle Plates y el desafio de Bone Plating ----------
    internal static class ArmorHook
    {
        internal static bool InExplosion;     // estamos dentro del dano de explosion a la armadura
        private static readonly FieldInfo MyList = AccessTools.Field(typeof(Player), "_preAllocatedArmorComponents");
        private static bool _warned;

        // La lista de armaduras que recibe ApplyDurabilityDamage es la lista precalculada de la victima: si es la del jugador, es suya.
        internal static bool IsMine(List<ArmorComponent> list)
        {
            var me = Plugin.Me;
            if (me == null || list == null) return false;
            if (MyList == null)
            {
                if (!_warned) { _warned = true; Plugin.Log.LogWarning("No se encontro la lista de armaduras del jugador: las mejoras de armadura no actuaran"); }
                return false;
            }
            return ReferenceEquals(MyList.GetValue(me), list);
        }
    }

    [HarmonyPatch(typeof(ArmorComponent), nameof(ArmorComponent.ApplyDurabilityDamage))]
    internal static class ArmorDurabilityPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref float armorDamage, List<ArmorComponent> armorComponents)
        {
            bool jug = Perks.IsActive(Juggernog.Id), wid = Perks.IsActive(WidowsWine.Id);
            if (!jug && !wid) return;
            if (!ArmorHook.IsMine(armorComponents)) return;
            float before = armorDamage;
            if (jug)
            {
                PerkService.AddProgress("jg_plates");
                if (Perk.Has("jg_plates")) armorDamage *= Juggernog.PlatesDurability;
            }
            if (wid)
            {
                if (ArmorHook.InExplosion) { if (Perk.Has("ww_fuse")) armorDamage = 0f; }
                else if (Perk.Has("dw_brittle")) armorDamage *= WidowsWine.BrittleDamage;
            }
            if (armorDamage != before) Dbg.Throttled("armor", 5f, "EFECTO", "armadura: desgaste " + before.ToString("0.0") + " -> " + armorDamage.ToString("0.0"));
        }
    }

    [HarmonyPatch(typeof(ArmorComponent), nameof(ArmorComponent.ApplyExplosionDurabilityDamage))]
    internal static class ArmorExplosionPatch
    {
        [HarmonyPrefix]
        private static void Prefix() { ArmorHook.InExplosion = true; }

        [HarmonyFinalizer]
        private static void Finalizer() { ArmorHook.InExplosion = false; }
    }

    // ---------- granadas: lanzamiento ----------
    // Grenade.Init(...) lo llaman tambien los tipos derivados (humo...) a traves de la base.
    [HarmonyPatch(typeof(Grenade), "Init")]
    internal static class GrenadeInitPatch
    {
        // Shockwave: la mecha dura la mitad. Se logra dando por "gastada" la mitad del tiempo que quedaba.
        [HarmonyPrefix]
        private static void Prefix(Grenade __instance, string profileId, ThrowWeap throwWeap, ref float timeSpent)
        {
            try
            {
                if (!Perks.IsActive(PhdFlopper.Id) || __instance is SmokeGrenade || !Perk.Has("pf_shock")) return;
                var me = Plugin.Me;
                if (me == null || me.ProfileId != profileId || throwWeap == null) return;
                float rest = throwWeap.GetExplDelay - timeSpent;
                if (rest <= 0f) return;
                timeSpent += rest * (1f - PhdFlopper.ShockFuse);
                Dbg.Log("EFECTO", "Shockwave: mecha " + rest.ToString("0.0") + " s -> " + (rest * PhdFlopper.ShockFuse).ToString("0.0") + " s");
            }
            catch (Exception e) { Plugin.Log.LogError("granada (mecha): " + e); }
        }

        [HarmonyPostfix]
        private static void Postfix(Grenade __instance, string profileId, ThrowWeap throwWeap)
        {
            Creatura.OnThrown(__instance, profileId, throwWeap);          // le creatura: modelo y sonido
            try
            {
                var me = Plugin.Me;
                if (me == null || me.ProfileId != profileId || !Perks.AnyActive) return;
                if (Perks.IsActive(WidowsWine.Id)) WidowsWine.OnThrown(__instance);
                if (Perks.IsActive(PhdFlopper.Id) && Perk.Has("dp_throw") && me.Physical != null && me.Physical.Stamina != null)
                {
                    me.Physical.Stamina.Current = Mathf.Max(0f, me.Physical.Stamina.Current - PhdFlopper.ThrowCost);
                    Dbg.Log("EFECTO", "Heavy Throw: -" + PhdFlopper.ThrowCost + " de estamina");
                }
            }
            catch (Exception e) { Plugin.Log.LogError("granada (lanzamiento): " + e); }
        }
    }

    // Sticky Floor: la granada sale con mas fuerza (el alcance crece con el cuadrado de la velocidad: x1.16 de fuerza = +35% de alcance)
    [HarmonyPatch(typeof(Grenade), nameof(Grenade.SetThrowForce))]
    internal static class ThrowForcePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Grenade __instance, ref Vector3 force)
        {
            if (!Perks.IsActive(WidowsWine.Id) || !Perk.Has("ww_sticky")) return;
            var me = Plugin.Me;
            if (me == null || me.ProfileId != __instance.ProfileId) return;
            force *= WidowsWine.ThrowForce;
        }
    }

    // ---------- granadas: explosion ----------
    // Grenade.Explosion(Grenade, IExplosiveItem, Vector3 posicion, string perfilQueLaLanzo, ...) (estatico)
    [HarmonyPatch]
    internal static class GrenadeExplosionPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return typeof(Grenade).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == "Explosion" && m.GetParameters().Length >= 4);
        }

        // Shockwave: durante la explosion, el radio de la plantilla de la granada se duplica (se restaura al terminar)
        [HarmonyPrefix]
        private static void Prefix(IExplosiveItem grenadeItem, string playerProfileIDWhoThrew, out float[] __state)
        {
            __state = null;
            try
            {
                if (!Perks.IsActive(PhdFlopper.Id) || !Perk.Has("pf_shock")) return;
                var me = Plugin.Me;
                if (me == null || me.ProfileId != playerProfileIDWhoThrew) return;
                var tpl = (grenadeItem as ThrowWeap)?.Template as ThrowWeapTemplate;
                if (tpl == null || tpl.ThrowType == ThrowWeapType.smoke_grenade) return;
                __state = new[] { tpl.MinExplosionDistance, tpl.MaxExplosionDistance };
                tpl.MinExplosionDistance *= PhdFlopper.ShockRadius;
                tpl.MaxExplosionDistance *= PhdFlopper.ShockRadius;
                Dbg.Log("EFECTO", "Shockwave: radio de la explosion " + __state[1].ToString("0.0") + " m -> " + tpl.MaxExplosionDistance.ToString("0.0") + " m");
            }
            catch (Exception e) { Plugin.Log.LogError("granada (radio): " + e); }
        }

        [HarmonyFinalizer]
        private static void Finalizer(IExplosiveItem grenadeItem, float[] __state)
        {
            if (__state == null) return;
            var tpl = (grenadeItem as ThrowWeap)?.Template as ThrowWeapTemplate;
            if (tpl == null) return;
            tpl.MinExplosionDistance = __state[0];
            tpl.MaxExplosionDistance = __state[1];
        }

        [HarmonyPostfix]
        private static void Postfix(Grenade grenade, IExplosiveItem grenadeItem, Vector3 grenadePosition, string playerProfileIDWhoThrew)
        {
            try
            {
                if (!Perks.AnyActive || grenade is SmokeGrenade) return;
                var me = Plugin.Me;
                if (me == null) return;
                PhdFlopper.OnExplosionNear(grenadePosition);                         // Concussed: cualquier explosion cercana
                if (me.ProfileId == playerProfileIDWhoThrew && Perks.IsActive(WidowsWine.Id)) WidowsWine.OnExploded(grenade, grenadeItem as Item, grenadePosition);
            }
            catch (Exception e) { Plugin.Log.LogError("granada (explosion): " + e); }
        }
    }
}
