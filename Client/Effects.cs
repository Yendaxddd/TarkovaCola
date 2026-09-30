using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Ganchos (Harmony) de Speed Cola: efectos de la perk, mejoras, desventajas y contadores de desafios.
    //  Regla de rendimiento: todo gancho que se ejecuta muy a menudo sale en la primera linea si no le toca actuar.
    // ============================================================================================

    internal static class Util
    {
        internal static bool IsMe(Player p) { return p != null && p.IsYourPlayer; }
    }

    // ---------- Speed Cola base + Sugar Rush: velocidad de recarga ----------
    // El juego calcula los buffs del arma del jugador aqui; multiplicamos la velocidad de recarga.
    [HarmonyPatch(typeof(SkillManager), nameof(SkillManager.GetWeaponInfo))]
    internal static class ReloadSpeedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SkillManager __instance, SkillManager.WeaponBuffsInfo __result)
        {
            if (!Plugin.SpeedColaActive || __result == null) return;
            var me = Plugin.Me;
            if (me == null || me.Skills != __instance) return;
            __result.ReloadSpeed *= Perk.ReloadMult;
            Dbg.Throttled("reloadspeed", 10f, "RELOAD", "velocidad de recarga x" + __result.ReloadSpeed.ToString("0.00") + " (perk x" + Perk.ReloadMult.ToString("0.0") + ")");
        }
    }

    // ---------- Velocidades de animacion: brazo danado (Speed Cola) y Quick Hands ----------
    // Con un brazo danado el juego llama SetSpeedParameters() con valores por defecto (1x) y anula el buff de recarga:
    // si la perk esta activa, nunca dejamos que baje del multiplicador de la perk. Quick Hands acelera sacar/cambiar de arma.
    [HarmonyPatch(typeof(FirearmsAnimator), nameof(FirearmsAnimator.SetSpeedParameters))]
    internal static class FirearmsSpeedPatch
    {
        [HarmonyPrefix]
        private static void Prefix(FirearmsAnimator __instance, ref float reload, ref float draw)
        {
            if (!Plugin.SpeedColaActive) return;
            var me = Plugin.Me;
            if (me == null || me.HandsController == null || me.HandsController.FirearmsAnimator != __instance) return;
            float rm = Perk.ReloadMult;
            if (Plugin.CfgIgnoreArmPenalty.Value && reload < rm) reload = rm;
            draw *= Perk.DrawMult;
        }
    }

    // El cuerpo (PlayerAnimator) lleva su propia velocidad de sacar arma: mismo tratamiento.
    [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.SetSpeedParameters))]
    internal static class BodySpeedPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref float reload, ref float draw)
        {
            if (!Plugin.SpeedColaActive) return;
            float rm = Perk.ReloadMult;
            if (Plugin.CfgIgnoreArmPenalty.Value && reload < rm) reload = rm;
            draw *= Perk.DrawMult;
        }
    }

    // ---------- Desafio Bolt Runner: desencasquillar ----------
    [HarmonyPatch(typeof(Player.FirearmController.RepairMalfunction), "OnMalfunctionOffEvent")]
    internal static class UnjamPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player.FirearmController.RepairMalfunction __instance)
        {
            var p = __instance.Controller != null ? __instance.Controller._player : null;
            if (!Util.IsMe(p)) return;
            Dbg.Log("DESAFIO", "arma desencasquillada (cola activa=" + Plugin.SpeedColaActive + ")");
            if (Plugin.SpeedColaActive) PerkService.AddProgress("a_bolt");
        }
    }

    // ---------- Desafio Chamber Check: revisar el cargador del arma en raid ----------
    [HarmonyPatch(typeof(Player.FirearmController), "CheckAmmo")]
    internal static class CheckAmmoPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player.FirearmController __instance, bool __result)
        {
            if (!__result || !Util.IsMe(__instance._player)) return;
            Dbg.Log("DESAFIO", "cargador del arma revisado");
            PerkService.AddProgress("a_check");
        }
    }

    // ---------- Revisar cargadores en el inventario: Chamber Check (+75%) y desafio Round Counter ----------
    [HarmonyPatch(typeof(Player.PlayerInventoryController), "InventoryCheckMagazine")]
    internal static class InventoryCheckPatch
    {
        [HarmonyPrefix]
        private static void Prefix(out float __state)
        {
            var cfg = Singleton<GlobalConfiguration>.Instance;
            __state = cfg.BaseCheckTime;
            if (Perk.Has("a_check"))
            {
                cfg.BaseCheckTime = __state / Perk.CheckSpeed;
                Dbg.Log("EFECTO", "Chamber Check: revisar cargador x" + Perk.CheckSpeed.ToString("0.00") + " mas rapido");
            }
            if (Plugin.InRaid)
            {
                Dbg.Log("DESAFIO", "cargador revisado en el inventario");
                PerkService.AddProgress("a_count");
            }
        }

        [HarmonyPostfix]
        private static void Postfix(float __state)
        {
            Singleton<GlobalConfiguration>.Instance.BaseCheckTime = __state;   // se restaura: solo cambia para esta revision
        }
    }

    // ---------- Cargar municion en un cargador: Mag Juggler (+50%, arma principal) y desafio ----------
    [HarmonyPatch(typeof(Player.PlayerInventoryController), "LoadMagazine")]
    internal static class LoadMagazinePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Magazine magazine, int loadCount, out float __state)
        {
            var cfg = Singleton<GlobalConfiguration>.Instance;
            __state = cfg.BaseLoadTime;
            if (loadCount <= 0 || !Plugin.InRaid) return;

            Dbg.Log("DESAFIO", "cargador cargado con municion (" + loadCount + " balas, cola activa=" + Plugin.SpeedColaActive + ")");
            if (Plugin.SpeedColaActive) PerkService.AddProgress("a_mags");     // este desafio pide Speed Cola activa

            if (Perk.Has("a_mags") && IsPrimaryMagazine(magazine))
            {
                cfg.BaseLoadTime = __state / Perk.MagJugglerSpeed;
                Dbg.Log("EFECTO", "Mag Juggler: carga x" + Perk.MagJugglerSpeed.ToString("0.0") + " mas rapida");
            }
        }

        [HarmonyPostfix]
        private static void Postfix(float __state)
        {
            Singleton<GlobalConfiguration>.Instance.BaseLoadTime = __state;
        }

        // ¿El cargador encaja en alguna de las armas principales equipadas?
        private static bool IsPrimaryMagazine(Magazine mag)
        {
            var me = Plugin.Me;
            if (me == null || mag == null) return false;
            foreach (var slotId in new[] { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon })
            {
                var w = me.Equipment.GetSlot(slotId).ContainedItem as Weapon;
                var ms = w != null ? w.GetMagazineSlot() : null;
                if (ms != null && ms.CheckCompatibility(mag)) return true;
            }
            return false;
        }
    }

    // ---------- Round Counter: los cargadores muestran su municion exacta ----------
    [HarmonyPatch(typeof(Magazine), "GetAmmoCountByLevel")]
    internal static class RoundCounterPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref int skill, ref bool @checked)
        {
            if (!Perk.Has("a_count")) return;
            skill = 2;          // nivel maximo de comprobacion = cifra exacta
            @checked = true;
        }
    }

    // ---------- Steady Fingers: los brazos reciben menos dano ----------
    [HarmonyPatch(typeof(ActiveHealthController), "ApplyDamage")]
    internal static class SteadyFingersPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ActiveHealthController __instance, EBodyPart bodyPart, ref float damage)
        {
            if (!Perk.Has("a_steady")) return;
            if (bodyPart != EBodyPart.LeftArm && bodyPart != EBodyPart.RightArm) return;
            if (!Util.IsMe(__instance.Player)) return;
            float before = damage;
            damage *= Perk.SteadyDamage;
            Dbg.Log("EFECTO", "Steady Fingers: dano en " + bodyPart + " " + before.ToString("0.0") + " -> " + damage.ToString("0.0"));
        }
    }

    // ---------- Jittery Hands: balanceo del arma +25% si un brazo no esta sano ----------
    [HarmonyPatch(typeof(BreathEffector), "Process")]
    internal static class JitterPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BreathEffector __instance, out float __state)
        {
            __state = __instance.Intensity;
            if (Perk.JitterOn && ReferenceEquals(__instance, Perk.MyBreath)) __instance.Intensity = __state * Perk.JitterSway;
        }

        [HarmonyPostfix]
        private static void Postfix(BreathEffector __instance, float __state)
        {
            __instance.Intensity = __state;
        }
    }

    // ---------- Sticky Mags: mas probabilidad de encasquillamiento ----------
    [HarmonyPatch(typeof(Player.FirearmController), "GetTotalMalfunctionChance")]
    internal static class StickyMagsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player.FirearmController __instance, ref float __result)
        {
            if (!Perk.Has("d_jam") || !Util.IsMe(__instance._player)) return;
            __result *= Perk.StickyMalf;
        }
    }

    // ---------- Overpressure: cada 4.o cargador pierde todas las balas al recargar (vuelven al inventario) ----------
    [HarmonyPatch]
    internal static class OverpressurePatch
    {
        private static int _lastFrame = -1;

        // ReloadMag existe en el controlador base y en sus versiones cliente: se parchean todas (una vez por frame).
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            return typeof(Player.FirearmController).Assembly.GetTypes()
                .Where(t => typeof(Player.FirearmController).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(flags))
                .Where(m => m.Name == "ReloadMag" && m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType == typeof(Magazine)
                            && !m.IsAbstract);
        }

        [HarmonyPrefix]
        private static void Prefix(Player.FirearmController __instance, Magazine magazine)
        {
            if (Time.frameCount == _lastFrame) return;      // el override llama a la base: contar una sola vez
            _lastFrame = Time.frameCount;
            if (!Util.IsMe(__instance._player)) return;

            Perk.ReloadCount++;
            Dbg.Log("RECARGA", "cambio de cargador #" + Perk.ReloadCount + " (" + magazine.Count + " balas)");
            if (!Perk.Has("d_over") || Perk.ReloadCount % 4 != 0) return;

            Dbg.Log("EFECTO", "Overpressure: el cargador pierde sus " + magazine.Count + " balas");
            Hud.Notify(Data.Name("d_over"), L.T("notice.over.sub"));
            EmptyMagazine(__instance._player, magazine);
        }

        // Devuelve las balas del cargador al inventario (rapido, sin animacion). Si no caben, se quedan en el cargador.
        private static async void EmptyMagazine(Player me, Magazine mag)
        {
            try
            {
                int guard = 0;
                while (mag.Count > 0 && guard++ < 30)
                {
                    var r = await me.InventoryController.UnloadAmmoInstantly(mag, false);
                    if (r.Failed) { Dbg.Log("EFECTO", "Overpressure: no se pudieron devolver todas las balas (" + r.Error + ")"); break; }
                }
            }
            catch (Exception e) { Plugin.Log.LogError("Overpressure: " + e); }
        }
    }

    // ---------- Sweet Tooth: comer/beber otra cosa tarda el doble ----------
    [HarmonyPatch(typeof(FirearmsAnimator), nameof(FirearmsAnimator.SetUseTimeMultiplier))]
    internal static class SweetToothPatch
    {
        [HarmonyPrefix]
        private static void Prefix(FirearmsAnimator __instance, ref float __0)
        {
            if (!Perk.Has("d_sweet")) return;
            var me = Plugin.Me;
            if (me == null || me.HandsController == null || me.HandsController.FirearmsAnimator != __instance) return;
            var item = (me.HandsController as Player.ItemHandsController)?.Item as FoodDrink;
            if (item == null || item.TemplateId == Plugin.SpeedColaId) return;
            __0 /= Perk.SweetToothTime;
            Dbg.Log("EFECTO", "Sweet Tooth: " + item.ShortName + " tarda x" + Perk.SweetToothTime.ToString("0") + " mas");
        }
    }
}
