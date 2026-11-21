using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ===================================================================================================================
    // Double Tap Root Beer: ritmo.
    //   Base: cadencia x1.10; cada impacto seguido (en menos de 0.4 s) sube el ritmo +3% de cadencia, hasta +15%. Fallar
    //         (3 disparos seguidos sin acertar) o parar reinicia el ritmo. Solo las armas automaticas ganan ritmo.
    //   Mayores: Double Shot (al maximo, 15%: el impacto cuenta como dos balas, dano x2), Perforation (+8 de penetracion al maximo,
    //            solo contra armaduras de clase <= 4), Momentum (fallar no reinicia; solo parar 1 s).
    //   Menores: Light Trigger (las semiautomaticas tambien, con 0.8 s de margen), Cold Barrel (la cadencia extra no calienta),
    //            Feed (la cadencia extra no encasquilla), Metronome (ritmo hasta +25%). Especial: Extra Slot.
    //   Desventajas ligadas: Hungry Mag, Out of Step, Jam | Slow Start, Kick, Wear, Irregular.
    // ===================================================================================================================
    internal static class DoubleTap
    {
        internal const string Id = "doubletap";
        internal const string ItemTpl = "6a1c00000000000000000c0f";
        internal const float BaseRate = 0.10f;           // cadencia +10%
        internal const float Step = 0.03f;               // +3% por impacto seguido
        internal const float Cap = 0.15f, MetroCap = 0.25f;
        internal const float Window = 0.4f, TriggerWindow = 0.8f, MomentumStop = 1f;
        internal const int MissShots = 3;                // disparos seguidos sin impacto = fallo
        internal const float DoubleChance = 0.15f;
        internal const float PerfMax = 8f;               // Perforation: +8 de penetracion con el ritmo al maximo
        internal const int PerfMaxClass = 4;
        internal const float HungryChance = 0.05f;
        internal const float StepPenalty = 0.90f, StepSeconds = 1f;
        internal const float JamMult = 1.5f, SlowStart = 0.90f, KickMult = 1.10f, WearMult = 1.20f, IrregRange = 0.10f;
        internal const float BotRate = 1.25f;          // bots: cadencia +25%

        private static float _rhythm, _lastHit = -99f, _lastShot = -99f, _stepUntil, _irreg = 1f, _maxSince = -1f;
        private static int _shotsSinceHit, _burst, _hitFrame = -1, _shots, _feedStreak, _hungryPending;
        private static bool _semi, _metroDone, _doubleNow;

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power)
        {
            return L.F("notice.doubletap.sub", (1f + BaseRate * power).ToString("0.00"), Mathf.RoundToInt(Cap * 100f * power));
        }

        internal static void Reset()
        {
            _rhythm = 0f; _lastHit = -99f; _lastShot = -99f; _stepUntil = 0f; _irreg = 1f; _maxSince = -1f;
            _shotsSinceHit = 0; _burst = 0; _hitFrame = -1; _shots = 0; _feedStreak = 0; _hungryPending = 0;
            _semi = false; _metroDone = false; _doubleNow = false;
        }

        private static float MaxRhythm { get { return Perk.Has("db_metro") ? MetroCap : Cap; } }
        private static float HitWindow { get { return _semi && Perk.Has("db_trigger") ? TriggerWindow : Window; } }

        // ritmo actual (0..maximo); caduca si dejas de acertar (o de disparar, con Momentum)
        internal static float Rhythm
        {
            get
            {
                if (!Active) return 0f;
                if (Apex.Has(Id)) return MaxRhythm;                 // APEX: siempre al maximo
                if (_rhythm > 0f)
                {
                    float now = Time.time;
                    bool expired = Perk.Has("db_momentum") ? now - _lastShot > MomentumStop : now - _lastHit > HitWindow;
                    if (expired) Drop();
                }
                return _rhythm;
            }
        }

        internal static bool AtMax { get { return Active && Rhythm >= MaxRhythm - 0.001f; } }

        private static void Drop()
        {
            _rhythm = 0f; _maxSince = -1f; _metroDone = false;
        }

        // multiplicador de cadencia de tus armas
        internal static float RateMult
        {
            get
            {
                float p = Perks.Power(Id);
                if (p <= 0f) return 1f;
                float m = 1f + (BaseRate + Rhythm * _irreg) * p;
                if (Time.time < _stepUntil) m *= StepPenalty;                       // A destiempo
                if (_burst < 3 && Perk.Has("db_d_slow")) m *= SlowStart;            // Arranque lento
                return m;
            }
        }

        // solo la parte extra (para Cold Barrel y Feed)
        internal static float ExtraRate { get { return Mathf.Max(1f, RateMult); } }

        private static bool IsSemi(Weapon w)
        {
            if (w == null) return false;
            var m = w.SelectedFireMode;
            return m == Weapon.EFireMode.single || m == Weapon.EFireMode.semiauto || m == Weapon.EFireMode.doubleaction;
        }

        // ---------------------------------------------------------------- disparo propio (DeadshotShotPatch, fragmento 0)
        internal static void OnShot(Player me)
        {
            if (!Active) return;
            float now = Time.time;
            var w = me.HandsController != null ? me.HandsController.Item as Weapon : null;
            _semi = IsSemi(w);
            if (now - _lastShot > 0.35f) _burst = 0;
            _burst++;
            _lastShot = now;
            if (Perk.Has("db_d_irreg")) _irreg = 1f + UnityEngine.Random.Range(-IrregRange, IrregRange);
            else _irreg = 1f;

            // fallo: varios disparos seguidos sin acertar
            _shotsSinceHit++;
            if (_shotsSinceHit == MissShots)
            {
                if (Perk.Has("db_d_step")) { _stepUntil = now + StepSeconds; Dbg.Log("EFECTO", "A destiempo: cadencia -10% 1 s"); }
                if (_rhythm > 0f && !Perk.Has("db_momentum")) { Drop(); Dbg.Log("EFECTO", "Double Tap: fallo, el ritmo se reinicia"); }
            }

            // Cargador hambriento: al maximo, a veces se gasta una bala de mas (se quita en el Tick, fuera del disparo)
            if (AtMax && Perk.Has("db_d_hungry") && UnityEngine.Random.value < HungryChance) _hungryPending++;

            // Alimentacion (desafio): cargadores vaciados seguidos sin encasquillarse
            var mag = w != null ? w.GetCurrentMagazine() : null;
            if (mag != null && mag.Count == 0)
            {
                _feedStreak++;
                if (_feedStreak == 10) PerkService.AddProgress("db_feed", 10);
            }

            // Canon frio (desafio): balas disparadas, de 10 en 10
            if (++_shots % 10 == 0) PerkService.AddProgress("db_cold", 10);
        }

        // ---------------------------------------------------------------- impacto propio en un jugador (DamageHubPatch)
        internal static float OnDealt(Player me, Player victim, float damage, DamageInfo info)
        {
            if (!Active || (info.DamageType & (EDamageType.Bullet | EDamageType.Sniper)) == 0) return damage;
            float now = Time.time;
            if (Time.frameCount != _hitFrame)                 // los perdigones de un mismo disparo cuentan como un impacto
            {
                _hitFrame = Time.frameCount;
                _shotsSinceHit = 0;
                var w = me.HandsController != null ? me.HandsController.Item as Weapon : null;
                bool gains = !IsSemi(w) || Perk.Has("db_trigger");
                bool chain = now - _lastHit <= HitWindow;
                float before = Rhythm;
                if (gains && chain) _rhythm = Mathf.Min(MaxRhythm, _rhythm + Step);
                _lastHit = now;
                if (_rhythm >= MaxRhythm - 0.001f && before < MaxRhythm - 0.001f)
                {
                    _maxSince = now;
                    PerkService.AddProgress("db_double");
                    Dbg.Log("EFECTO", "Double Tap: ritmo al maximo (+" + Mathf.RoundToInt(MaxRhythm * 100f) + "%)");
                }
                _doubleNow = AtMax && Perk.Has("db_double") && UnityEngine.Random.value < DoubleChance;
                if (_doubleNow) Dbg.Log("EFECTO", "Double Shot: el impacto cuenta como dos balas");
            }
            return _doubleNow ? damage * 2f : damage;
        }

        // ---------------------------------------------------------------- bajas propias
        internal static void OnKill(Player me, Player victim, DamageInfo info, bool boss)
        {
            if (!Active) return;
            Apex.AddProgress(Id, 1f);
            if (boss) PerkService.AddProgress("db_slot");
            if ((info.DamageType & (EDamageType.Bullet | EDamageType.Sniper)) == 0) return;
            var w = me.HandsController != null ? me.HandsController.Item as Weapon : null;
            if (AtMax) PerkService.AddProgress("db_momentum");
            if (IsSemi(w)) PerkService.AddProgress("db_trigger");
            if (w != null && (w.WeapClass == "smg" || w.WeapClass == "pistol") && Deadshot.Armored(victim)) PerkService.AddProgress("db_perf");
        }

        // ---------------------------------------------------------------- cada frame
        internal static void Tick(Player me)
        {
            if (!Active) return;
            var fc = me.HandsController as Player.FirearmController;
            var w = fc != null ? fc.Item as Weapon : null;
            if (w != null && w.MalfState.State != Weapon.EMalfunctionState.None) _feedStreak = 0;

            // Metronomo (desafio): 3 s seguidos con el ritmo al maximo
            if (AtMax && _maxSince > 0f && !_metroDone && Time.time - _maxSince >= 3f) { _metroDone = true; PerkService.AddProgress("db_metro"); }

            if (_hungryPending > 0 && fc != null && !fc.IsInReloadOperation())
            {
                _hungryPending = 0;
                EatRound(me, w);
            }
        }

        private static void EatRound(Player me, Weapon w)
        {
            var mag = w != null ? w.GetCurrentMagazine() : null;
            var top = mag != null ? mag.Cartridges.Last : null;
            if (top == null) return;
            try
            {
                var ctrl = me.InventoryController;
                if (top.StackObjectsCount > 1)
                {
                    var s = ItemManipulator.SplitToNowhere(top, 1, ctrl, ctrl, true);
                    if (s.Succeeded) ctrl.TryRunNetworkTransaction(s);
                }
                else
                {
                    var r = ItemManipulator.Remove(top, ctrl, true);
                    if (r.Succeeded) ctrl.TryRunNetworkTransaction(r);
                }
                Dbg.Log("EFECTO", "Cargador hambriento: se gasta una bala de mas");
            }
            catch (Exception e) { Dbg.Throttled("dbhungry", 10f, "ERROR", "Cargador hambriento: " + e.Message); }
        }

        // Perforation: bono de penetracion ahora mismo (0 si no aplica)
        internal static float PerfBonus
        {
            get
            {
                if (!Active || !Perk.Has("db_perf")) return 0f;
                return PerfMax * (Rhythm / MaxRhythm) * Perks.Power(Id);
            }
        }

        internal static bool Mine(Weapon w)
        {
            var me = Plugin.Me;
            return me != null && w != null && ReferenceEquals(w.Owner, me.InventoryController);
        }
    }

    // ============================================================================================
    //  Ganchos de Double Tap
    // ============================================================================================

    // cadencia de tus armas
    [HarmonyPatch]
    internal static class DoubleTapRatePatch
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(Weapon), nameof(Weapon.FireRate));
            yield return AccessTools.PropertyGetter(typeof(Weapon), nameof(Weapon.SingleFireRate));
        }

        [HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref int __result)
        {
            if (!DoubleTap.Active || !DoubleTap.Mine(__instance)) return;
            float m = DoubleTap.RateMult;
            if (m != 1f) __result = Mathf.RoundToInt(__result * m);
        }
    }

    // Canon frio (la cadencia extra no calienta) y Desgaste (mas desgaste con ritmo)
    [HarmonyPatch(typeof(Weapon), "get_HeatFactorByShot")]
    internal static class DoubleTapHeatPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref float __result)
        {
            if (DoubleTap.Active && Perk.Has("db_cold") && DoubleTap.Mine(__instance)) __result /= DoubleTap.ExtraRate;
        }
    }

    [HarmonyPatch(typeof(Weapon), "get_DurabilityBurnRatio")]
    internal static class DoubleTapWearPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref float __result)
        {
            if (DoubleTap.Active && Perk.Has("db_d_wear") && DoubleTap.Rhythm > 0f && DoubleTap.Mine(__instance)) __result *= DoubleTap.WearMult;
        }
    }

    // Alimentacion (la cadencia extra no encasquilla) y Atasco (al maximo, x1.5)
    [HarmonyPatch(typeof(Player.FirearmController), "GetTotalMalfunctionChance")]
    internal static class DoubleTapJamPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player.FirearmController __instance, ref float __result)
        {
            if (!DoubleTap.Active || !Util.IsMe(__instance._player)) return;
            if (Perk.Has("db_feed")) __result /= DoubleTap.ExtraRate;
            if (Perk.Has("db_d_jam") && DoubleTap.AtMax) __result *= DoubleTap.JamMult;
        }
    }

    // Patada: al maximo, retroceso x1.1
    [HarmonyPatch(typeof(ShotEffector), nameof(ShotEffector.Process), new[] { typeof(float) })]
    internal static class DoubleTapKickPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ShotEffector __instance, ref float str)
        {
            if (!DoubleTap.Active || !Perk.Has("db_d_kick") || !DoubleTap.AtMax) return;
            var me = Plugin.Me;
            if (me != null && me.ProceduralWeaponAnimation != null && ReferenceEquals(me.ProceduralWeaponAnimation.Shootingg, __instance)) str *= DoubleTap.KickMult;
        }
    }

    // Perforation: mas penetracion contra armaduras de clase 4 o menos (al decidir si atraviesa y al calcular el dano)
    [HarmonyPatch(typeof(ArmorComponent), nameof(ArmorComponent.SetPenetrationStatus))]
    internal static class DoubleTapPenStatusPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ArmorComponent __instance, Shot shot, out float __state)
        {
            __state = 0f;
            if (shot == null || !DoubleTap.Active || __instance.ArmorClass > DoubleTap.PerfMaxClass) return;
            var me = Plugin.Me;
            if (me == null || shot.PlayerProfileID != me.ProfileId) return;
            __state = DoubleTap.PerfBonus;
            shot.PenetrationPower += __state;
        }

        [HarmonyPostfix]
        private static void Postfix(Shot shot, float __state)
        {
            if (__state > 0f && shot != null) shot.PenetrationPower -= __state;
        }
    }

    [HarmonyPatch(typeof(ArmorComponent), nameof(ArmorComponent.ApplyDamage))]
    internal static class DoubleTapPenDamagePatch
    {
        [HarmonyPrefix]
        // __state = (bono, penetracion antes): al salir se quita el bono en la misma proporcion en que la armadura redujo la penetracion,
        // para que la capa siguiente parta de la penetracion normal (y reciba su propio bono si es de clase <= 4)
        private static void Prefix(ArmorComponent __instance, ref DamageInfo damageInfo, out Vector2 __state)
        {
            __state = Vector2.zero;
            if (!DoubleTap.Active || __instance.ArmorClass > DoubleTap.PerfMaxClass) return;
            var me = Plugin.Me;
            var by = damageInfo.Player != null ? damageInfo.Player.iPlayer : null;
            if (me == null || by == null || by.ProfileId != me.ProfileId) return;
            float b = DoubleTap.PerfBonus;
            if (b <= 0f) return;
            __state = new Vector2(b, damageInfo.PenetrationPower);
            damageInfo.PenetrationPower += b;
        }

        [HarmonyPostfix]
        private static void Postfix(ref DamageInfo damageInfo, Vector2 __state)
        {
            if (__state.x <= 0f) return;
            float boosted = __state.y + __state.x;
            if (boosted > 0f) damageInfo.PenetrationPower *= __state.y / boosted;
        }
    }
}
