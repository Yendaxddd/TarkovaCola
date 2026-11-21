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
    // Electric Cherry: velocidad al recargar.
    //   Base: al terminar una recarga de cargador, un impulso electrico a la velocidad de esprint durante 1 s; cuanto mas vacio
    //         estaba el cargador, mayor (vacio = +250%, medio = +125%, lleno = nada). Se va apagando con el tiempo.
    //   Mayores: Discharge (al recargar, descarga alrededor: aturde hasta 1.5 s a 2-5 m segun lo vacio), Overcharge (recargar con
    //            impulso lo acumula hasta +375% y renueva el tiempo), Lightning (con impulso: esprintar no gasta estamina y puedes
    //            recargar esprintando).
    //   Menores: Battery (2 s), Voltage (toda recarga da al menos +75%), Feedback (una baja lo devuelve al valor inicial),
    //            Steady Current (no se apaga: se mantiene y se corta al final). Especial: Extra Slot.
    //   Desventajas ligadas: Leak, Polarity, Backlash | Short Circuit, Visible Spark, Tingling, Low Ceiling.
    // ===================================================================================================================
    internal static class ElectricCherry
    {
        internal const string Id = "electriccherry";
        internal const string ItemTpl = "6a1c00000000000000000c0c";
        // +150% durante 1 s (11/21: nerf desde +250%; se probo +400% 4 s y era exagerado; ahora si se nota gracias a CherrySpeedLimitPatch); el resto en proporcion
        internal const float MaxBoost = 1.50f, CeilingBoost = 1.05f, OverMax = 2.25f, VoltageMin = 0.45f;
        internal const float Seconds = 1f, BatterySeconds = 2f, LeakSeconds = 0.5f;
        internal const float StunMax = 1.5f, StunRadiusMin = 2f, StunRadiusMax = 5f;
        internal const float BacklashSpeed = 0.85f, BacklashSeconds = 1.5f;
        internal const float SparkRadius = 20f, TingleSway = 1.15f;
        internal const float BotBoost = 1.20f, BotSeconds = 2f;

        private static float _start, _startValue, _until, _emptiness = -1f, _nextSpark, _runAcc, _boostAcc;
        private static bool _reloading, _wasBoosted, _sprintAtReload;
        private static float _lastReloadOp;

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power)
        {
            return L.F("notice.electriccherry.sub", Mathf.RoundToInt(MaxBoost * 100f * power), Seconds.ToString("0"));
        }

        internal static void Reset()
        {
            _start = 0f; _startValue = 0f; _until = 0f; _emptiness = -1f; _nextSpark = 0f; _runAcc = 0f; _boostAcc = 0f;
            _reloading = false; _wasBoosted = false; _sprintAtReload = false;
            _bots.Clear();
        }

        private static float Duration
        {
            get
            {
                if (Apex.Has(Id)) return Mathf.Max(Seconds, Perk.Has("ec_battery") ? BatterySeconds : Seconds);
                float d = Perk.Has("ec_battery") ? BatterySeconds : Seconds;
                if (Perk.Has("ec_d_leak")) d = Mathf.Min(d, LeakSeconds);
                return d;
            }
        }

        // impulso actual (0..3.75): +X a la velocidad de esprint
        internal static float Boost
        {
            get
            {
                if (!Active) return 0f;
                float now = Time.time;
                if (now >= _until) return 0f;
                if (Perk.Has("ec_steady")) return _startValue;
                float d = Mathf.Max(0.01f, _until - _start);
                return _startValue * Mathf.Clamp01((_until - now) / d);
            }
        }

        internal static bool Boosted { get { return Boost > 0.001f; } }

        // ---------------------------------------------------------------- empieza una recarga de cargador (OverpressurePatch)
        internal static void OnReloadStart(Player.FirearmController fc)
        {
            if (!Active) return;
            var w = fc.Item as Weapon;
            var cur = w != null ? w.GetCurrentMagazine() : null;
            int max = cur != null ? cur.MaxCount : 0;
            _emptiness = cur == null || max <= 0 ? 1f : 1f - Mathf.Clamp01(cur.Count / (float)max);
            _reloading = true; _lastReloadOp = Time.time;
            _sprintAtReload = fc._player.Physical != null && fc._player.Physical.Sprinting;
            PerkService.AddProgress("ec_voltage");
            if (_emptiness >= 0.99f) { PerkService.AddProgress("ec_discharge"); Apex.AddProgress(Id, 1f); }
            if (Perk.Has("ec_discharge") || Apex.Has(Id)) Discharge(fc._player, Apex.Has(Id) ? 1f : _emptiness);
        }

        // al terminar la recarga: el impulso
        private static void OnReloadEnd(Player me)
        {
            float e = _emptiness;
            _emptiness = -1f;
            if (e < 0f) return;
            float p = Perks.Power(Id);
            if (Perk.Has("ec_d_polarity") && e < 0.5f && !Apex.Has(Id)) { Dbg.Log("EFECTO", "Polaridad: el cargador no estaba medio vacio, sin impulso"); return; }
            float cap = Perk.Has("ec_d_ceiling") ? CeilingBoost : MaxBoost;
            float v = Apex.Has(Id) ? cap : cap * e;
            if (Perk.Has("ec_voltage")) v = Mathf.Max(v, VoltageMin);
            v *= p;
            if (v <= 0.001f) return;
            float now = Time.time;
            if (Perk.Has("ec_overcharge") && Boosted) v = Mathf.Min(OverMax * p, Boost + v);
            else v = Mathf.Max(v, Boost);
            _startValue = v; _start = now; _until = now + Duration;
            _wasBoosted = true;
            ElectricVfx.Flash(Mathf.Clamp01(v / MaxBoost));
            Dbg.Log("EFECTO", "Electric Cherry: impulso +" + Mathf.RoundToInt(v * 100f) + "% " + Duration.ToString("0.0") + " s (cargador " + Mathf.RoundToInt(e * 100f) + "% vacio)");
        }

        // Discharge: aturde a los enemigos cercanos segun lo vacio que estaba el cargador
        private static void Discharge(Player me, float e)
        {
            if (e <= 0.05f) return;
            float radius = Mathf.Lerp(StunRadiusMin, StunRadiusMax, e);
            float secs = StunMax * e;
            var world = Singleton<GameWorld>.Instance;
            if (world == null) return;
            int n = 0;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p == me || p.IsYourPlayer || Allies.Is(p) || (p.Position - me.Position).sqrMagnitude > radius * radius) continue;
                WidowsWine.Stun(p, secs);
                n++;
            }
            ElectricVfx.Burst(me.Position, radius);
            Dbg.Log("EFECTO", "Descarga: " + n + " enemigo(s) aturdidos " + secs.ToString("0.0") + " s a " + radius.ToString("0.0") + " m");
        }

        // ---------------------------------------------------------------- cada frame
        internal static void Tick(Player me)
        {
            if (!Active) return;
            var fc = me.HandsController as Player.FirearmController;
            bool reloading = fc != null && fc.IsInReloadOperation();
            if (reloading) _lastReloadOp = Time.time;
            if (reloading && !_reloading) OnReloadStart(fc);           // recargas sin cargador (tubo, tambor, cerrojo): no pasan por ReloadMag
            // entre una operacion y la siguiente (meter cartucho -> cerrar) puede haber un frame sin recarga: margen de 0.15 s
            if (_reloading && !reloading && Time.time - _lastReloadOp > 0.15f) { _reloading = false; OnReloadEnd(me); }
            if (!reloading && fc == null) _reloading = false;

            float dt = Time.deltaTime;
            bool boosted = Boosted;
            var phys = me.Physical;
            bool sprint = phys != null && phys.Sprinting;
            if (boosted)
            {
                // desafios: tiempo con impulso y metros esprintando con impulso
                _boostAcc += dt;
                if (_boostAcc >= 5f) { PerkService.AddProgress("ec_battery", (int)_boostAcc); _boostAcc -= (int)_boostAcc; }
                if (sprint)
                {
                    _runAcc += PerkFx.Speed * dt;
                    if (_runAcc >= 10f) { int m = (int)_runAcc; PerkService.AddProgress("ec_overcharge", m); PerkService.AddProgress("ec_steady", m); _runAcc -= m; }
                }
                // Lightning: esprintar no gasta estamina y puedes seguir esprintando mientras recargas
                if (Perk.Has("ec_lightning") && phys != null && phys.Stamina != null)
                {
                    if (sprint) phys.Stamina.Current = Mathf.Max(phys.Stamina.Current, _lastStamina);
                    if (reloading && _sprintAtReload && !sprint && me.MovementContext != null && me.MovementContext.CanSprint) me.EnableSprint(true);
                }
                // Visible Spark: los bots cercanos te oyen chisporrotear
                if (Perk.Has("ec_d_spark") && Time.time >= _nextSpark)
                {
                    _nextSpark = Time.time + 1f;
                    Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, SparkRadius, AISoundType.step);
                }
            }
            else if (_wasBoosted)
            {
                _wasBoosted = false;
                // Backlash: al acabar, mas lento un momento
                if (Perk.Has("ec_d_backlash") && me.MovementContext != null)
                {
                    var mc = me.MovementContext;
                    mc.ChangeSpeedLimit(BacklashSpeed * mc.MaxSpeed, Player.ESpeedLimit.Swamp, BacklashSeconds);
                    Dbg.Log("EFECTO", "Contragolpe: velocidad x" + BacklashSpeed + " " + BacklashSeconds + " s");
                }
            }
            _lastStamina = phys != null && phys.Stamina != null ? phys.Stamina.Current : 0f;
        }
        private static float _lastStamina;

        // ---------------------------------------------------------------- dano recibido / bajas
        internal static void OnDamageTaken()
        {
            if (Active && Boosted && Perk.Has("ec_d_short")) { _until = 0f; Dbg.Log("EFECTO", "Cortocircuito: el dano corta el impulso"); }
        }

        internal static void OnKill(Player me, bool boss)
        {
            if (!Active) return;
            if (boss) PerkService.AddProgress("ec_slot");
            if (!Boosted) return;
            PerkService.AddProgress("ec_lightning");
            PerkService.AddProgress("ec_feedback");
            if (Perk.Has("ec_feedback"))
            {
                float d = _until - _start;
                _start = Time.time; _until = _start + d;        // vuelve al valor inicial
                Dbg.Log("EFECTO", "Realimentacion: el impulso vuelve a +" + Mathf.RoundToInt(_startValue * 100f) + "%");
            }
        }

        internal static float SwayFactor() { return Active && Perk.Has("ec_d_tingle") && Boosted ? TingleSway : 1f; }

        // ---------------------------------------------------------------- bots con Electric Cherry: +20% de esprint 2 s tras recargar
        private static readonly Dictionary<MovementContext, float> _bots = new Dictionary<MovementContext, float>();
        internal static void BotReloading(Player bot)
        {
            if (bot.MovementContext != null) _bots[bot.MovementContext] = Time.time + BotSeconds;
        }
        internal static float BotMult(MovementContext mc)
        {
            float until;
            return _bots.Count > 0 && _bots.TryGetValue(mc, out until) && Time.time < until ? BotBoost : 1f;
        }

        internal static void DrawGui()
        {
            if (!Active) return;
            float b = Boost;
            if (b > 0.001f) FinalFx.Vignette(new Color(0.35f, 0.75f, 1f), Mathf.Clamp01(b / MaxBoost) * 0.45f * (0.85f + 0.15f * Mathf.Sin(Time.time * 40f)));
        }
    }

    // velocidad de esprint: impulso de Electric Cherry (tu) y bots con la perk. Se aplica al movimiento de cada fotograma (ApplyMotion):
    // cambiar SprintingSpeed no servia, el juego limita la velocidad fisica y el impulso no se notaba nada
    [HarmonyPatch(typeof(MovementState), "ApplyMotion")]
    internal static class CherrySprintPatch
    {
        [HarmonyPrefix]
        private static void Prefix(MovementState __instance, ref Vector3 motion)
        {
            var mc = __instance.MovementContext;
            if (mc == null || !mc.IsSprintEnabled) return;
            float k = ElectricCherry.BotMult(mc);
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(me.MovementContext, mc)) k = (1f + ElectricCherry.Boost) * Tombstone.SpeedMult;      // + espectro de Tombstone
            if (k != 1f) motion *= k;
        }
    }

    // el controlador del personaje recorta la velocidad a su SpeedLimit DESPUES de ApplyMotion: sin esto el impulso no se notaba nada.
    // Mientras dura el impulso se sube el limite en la misma proporcion (y se deja como estaba al acabar el movimiento).
    [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.DirectApplyMotion))]
    internal static class CherrySpeedLimitPatch
    {
        [HarmonyPrefix]
        private static void Prefix(MovementContext __instance, out float __state)
        {
            __state = float.NaN;
            var me = Plugin.Me;
            if (me == null || !ReferenceEquals(me.MovementContext, __instance) || !__instance.IsSprintEnabled) return;
            float k = (1f + ElectricCherry.Boost) * Tombstone.SpeedMult;
            if (k <= 1.001f) return;
            var cc = __instance.CharacterController;
            if (cc == null || cc.SpeedLimit <= 0f) return;
            __state = cc.SpeedLimit;
            cc.SpeedLimit = __state * k;
        }

        [HarmonyPostfix]
        private static void Postfix(MovementContext __instance, float __state)
        {
            if (float.IsNaN(__state)) return;
            var cc = __instance.CharacterController;
            if (cc != null) cc.SpeedLimit = __state;
        }
    }

    // ---------- efectos visuales electricos (chispas y destello) ----------
    internal static class ElectricVfx
    {
        private static float _flashAt = -99f, _flashPower;
        internal static void Flash(float power) { _flashAt = Time.time; _flashPower = power; }

        internal static void Burst(Vector3 pos, float radius)
        {
            try
            {
                var root = new GameObject("TarkovaCola_Cherry_VFX");
                root.transform.position = pos + Vector3.up * 1f;
                var light = root.AddComponent<Light>();
                light.type = LightType.Point; light.color = new Color(0.4f, 0.8f, 1f); light.range = radius * 2f; light.intensity = 6f;
                UnityEngine.Object.Destroy(root, 0.25f);
            }
            catch (Exception e) { Dbg.Throttled("cherryvfx", 10f, "ERROR", "Electric Cherry (efecto): " + e.Message); }
        }

        internal static void DrawGui()
        {
            float t = Time.time - _flashAt;
            if (t >= 0f && t < 0.35f) FinalFx.Vignette(new Color(0.6f, 0.9f, 1f), (1f - t / 0.35f) * 0.6f * _flashPower);
        }
    }
}
