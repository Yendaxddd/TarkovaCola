using System;
using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Compatibilidad con Sage's Advanced Movement (SAM, com.sage.advancedmovement), con PHD Flopper activa:
    //  - la plancha (dive) explota al aterrizar, como el "dolphin dive" de CoD (una granada entera donde caes; PHD te hace inmune)
    //  - el deslizamiento va el doble de rapido y llega un 50% mas lejos, sin la cola lenta del final (sin inercia), y agacharte otra vez
    //    lo corta al instante (saltar ya salia al momento y, con esprint pulsado, sales esprintando)
    // Por reflexion: MoveController es interno de SAM (InDive = hay plancha; Diving = aun en el aire, false al aterrizar).
    internal static class SamCompat
    {
        private const float Cooldown = 3f;
        private static bool _looked, _ok;
        private static PropertyInfo _inDive, _diving;
        private static bool _wasDiving;
        private static float _ready;
        private const float SlideDistance = 1.5f, SlideSpeed = 2f;
        private static object _phdSlide;                  // el deslizamiento en curso empezo con PHD (SlideState)
        private static MethodInfo _beginCancel;
        private static float _slideEndAt = -99f;          // cuando acabo el ultimo deslizamiento con PHD

        private static bool Lookup()
        {
            if (_looked) return _ok;
            _looked = true;
            try
            {
                var mc = AccessTools.TypeByName("AdvancedMovement.Moves.MoveController");
                if (mc == null) return false;
                _inDive = AccessTools.Property(mc, "InDive");
                _diving = AccessTools.Property(mc, "Diving");
                _ok = _inDive != null && _diving != null;
                if (_ok) PatchSlide();
                Plugin.Log.LogInfo(_ok ? "Advanced Movement (SAM) detectado: la plancha con PHD Flopper explota al aterrizar" : "Advanced Movement (SAM): version no compatible");
            }
            catch (Exception e) { _ok = false; Plugin.Log.LogWarning("Advanced Movement (SAM): " + e.Message); }
            return _ok;
        }

        internal static void OnRaidStart() { _wasDiving = false; _ready = 0f; _phdSlide = null; _slideEndAt = -99f; }

        // ---------------------------------------------------------------- deslizamiento con PHD
        private static void PatchSlide()
        {
            try
            {
                var slide = AccessTools.TypeByName("AdvancedMovement.States.SlideState");
                var ctor = slide != null ? AccessTools.GetDeclaredConstructors(slide).FirstOrDefault() : null;
                var pose = slide != null ? AccessTools.Method(slide, "ChangePose") : null;
                _beginCancel = slide != null ? AccessTools.Method(slide, "BeginCancel") : null;
                if (ctor == null || pose == null || _beginCancel == null) { Plugin.Log.LogWarning("Advanced Movement (SAM): deslizamiento no encontrado (sin cambios con PHD)"); return; }
                var h = new Harmony("com.tarkovacola.client.sam");
                h.Patch(ctor, prefix: new HarmonyMethod(typeof(SamCompat), nameof(SlidePrefix)), postfix: new HarmonyMethod(typeof(SamCompat), nameof(SlidePostfix)));
                h.Patch(pose, prefix: new HarmonyMethod(typeof(SamCompat), nameof(PosePrefix)));
                // al acabar: se apagan los efectos; y si sales esprintando, sales ya a toda velocidad (EFT reinicia el esprint desde abajo)
                var exit = AccessTools.Method(slide.BaseType, "Exit");
                if (exit != null) h.Patch(exit, postfix: new HarmonyMethod(typeof(SamCompat), nameof(ExitPostfix)));
                var sprint = AccessTools.Method(typeof(SprintPlayerState), "Enter");
                if (sprint != null) h.Patch(sprint, postfix: new HarmonyMethod(typeof(SamCompat), nameof(SprintEnterPostfix)));
            }
            catch (Exception e) { Plugin.Log.LogWarning("Advanced Movement (SAM): no se pudo enganchar el deslizamiento: " + e.Message); }
        }

        private static bool Mine(MovementContext ctx)
        {
            var me = Plugin.Me;
            return me != null && ReferenceEquals(me.MovementContext, ctx) && Perks.IsActive(PhdFlopper.Id);
        }

        // antes de crear el deslizamiento: mas lejos, mas rapido y sin la cola lenta (los ajustes de SAM no se tocan: es una copia por deslizamiento)
        private static void SlidePrefix(MovementContext ctx, object settings, out bool __state)
        {
            __state = false;
            try
            {
                if (settings == null || !Mine(ctx)) return;
                var t = Traverse.Create(settings);
                float dist = t.Field("DistanceScale").GetValue<float>(), dur = t.Field("DurationScale").GetValue<float>();
                t.Field("DistanceScale").SetValue(dist * SlideDistance);
                t.Field("DurationScale").SetValue(dur * SlideDistance / SlideSpeed);     // velocidad = distancia / duracion = x2
                t.Field("UseSlowDownGlide").SetValue(false);
                __state = true;
            }
            catch (Exception e) { Dbg.Throttled("samslide", 30f, "ERROR", "SAM (deslizamiento): " + e.Message); }
        }

        private static void SlidePostfix(object __instance, bool __state)
        {
            if (!__state) return;
            _phdSlide = __instance;
            try
            {
                var me = Plugin.Me;
                float secs = Traverse.Create(__instance).Field("EndTime").GetValue<float>();
                if (me != null) PhdVfx.SlideStart(me.Transform.Original, secs);
            }
            catch (Exception e) { Dbg.Throttled("samslidefx", 30f, "ERROR", "SAM (efectos del deslizamiento): " + e.Message); }
            Dbg.Log("EFECTO", "PHD + SAM: deslizamiento x" + SlideSpeed + " de velocidad y x" + SlideDistance + " de distancia");
        }

        // agacharse otra vez durante un deslizamiento con PHD: se corta al instante (sin los 0.3 s de frenada)
        private static bool PosePrefix(object __instance, float poseDelta)
        {
            if (poseDelta == 0f || !ReferenceEquals(__instance, _phdSlide)) return true;
            try { _beginCancel.Invoke(__instance, new object[] { 0f, 2.5f }); return false; }
            catch { return true; }
        }

        private static void ExitPostfix(object __instance)
        {
            if (!ReferenceEquals(__instance, _phdSlide)) return;
            _phdSlide = null;
            _slideEndAt = Time.time;
            PhdVfx.SlideStop();
        }

        // esprint justo despues de un deslizamiento con PHD: empieza a la velocidad maxima, sin acelerar desde cero
        private static void SprintEnterPostfix(SprintPlayerState __instance)
        {
            try
            {
                if (Time.time - _slideEndAt > 0.6f) return;
                var me = Plugin.Me;
                var ctx = __instance.MovementContext;
                if (me == null || me.Physical == null || !ReferenceEquals(me.MovementContext, ctx)) return;
                _slideEndAt = -99f;
                float max = (me.Physical.SprintSpeed * ctx.SprintingSpeed + 1f) * ctx.StateSprintSpeedLimit;
                if (max > ctx.SprintSpeed) ctx.SprintSpeed = max;
                Dbg.Log("EFECTO", "PHD + SAM: sales del deslizamiento esprintando a tope (x" + max.ToString("0.00") + ")");
            }
            catch (Exception e) { Dbg.Throttled("samsprint", 30f, "ERROR", "SAM (esprint): " + e.Message); }
        }

        // cada frame en raid (Plugin)
        internal static void Tick(Player me)
        {
            if (me == null || !Lookup()) return;
            try
            {
                bool diving = (bool)_diving.GetValue(null, null);
                bool landed = _wasDiving && !diving && (bool)_inDive.GetValue(null, null);     // del aire al aterrizaje (no si se cancelo)
                _wasDiving = diving;
                if (!landed || !Perks.IsActive(PhdFlopper.Id) || Time.time < _ready) return;
                _ready = Time.time + Cooldown;
                Apex.Blast(me, me.Position + Vector3.up * 0.2f);
                PerkService.AddStat("phd_dives", 1);
                Dbg.Log("EFECTO", "PHD + SAM: plancha explosiva");
            }
            catch (Exception e) { Dbg.Throttled("samcompat", 30f, "ERROR", "SAM: " + e.GetBaseException().Message); }
        }
    }
}
