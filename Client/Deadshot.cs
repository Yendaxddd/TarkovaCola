using System;
using EFT;
using EFT.Animations;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Deadshot Daiquiri: punteria.
    //   Base: apuntas un 15% mas rapido y el balanceo del arma baja un 25%.
    //   Mayores: Dead Aim (el arma no se balancea al moverte), Incredible (mas penetracion de armadura), To the Skies (retroceso -50%).
    //   Menores: Quick Scope, Steady Pulse, Headhunter, Stillness. Especial: Extra Slot.
    //   Desventajas ligadas: Tunnel Vision, Heavy Breathing, Shaky Hands | Eye Strain, Bitter Lime, Sugar Rim, Squeaky Boots.
    // El balanceo, el aire, el ruido de pasos, el hambre/sed y la estamina se aplican en los ganchos compartidos de PerkFx.
    internal static class Deadshot
    {
        internal const string Id = "deadshot";
        internal const string ItemTpl = "6a1c00000000000000000c06";

        // --- balance ---
        internal const float BaseAim = 0.15f;           // base: apuntar +15% rapido
        internal const float BaseSway = 0.25f;          // base: balanceo -25%
        internal const float QuickAim = 0.20f;          // Quick Scope: +20% mas
        internal const float StrainAim = 0.10f;         // Eye Strain: -10%
        internal const float StillSway = 0.75f;         // Stillness: agachado o tumbado, balanceo -25% mas
        internal const float PulseBreath = 1.4f;        // Steady Pulse: aguantas la respiracion un 40% mas
        internal const float HeadStamina = 0.20f;       // Headhunter: matar de un tiro a la cabeza devuelve el 20% de la estamina
        internal const float Penetration = 1.30f;       // Incredible: +30% de penetracion
        internal const float RecoilCut = 0.5f;          // To the Skies: retroceso x0.5
        internal const float TunnelDraw = 0.85f;        // Tunnel Vision: sacar el arma -15%
        internal const float BreathingDrain = 0.15f;    // Heavy Breathing: correr gasta un 15% mas
        internal const float ShakySway = 1.20f;         // Shaky Hands: balanceo +20% sin apuntar
        internal const float SqueakRadius = 12f;        // Squeaky Boots
        internal const float MovingSpeed = 0.8f;        // "te estas moviendo" (m/s) para el desafio de Dead Aim

        internal static bool Active { get { return Perks.IsActive(Id); } }

        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power)
        {
            return L.F("notice.deadshot.sub", Mathf.RoundToInt(BaseAim * 100f * power), Mathf.RoundToInt(BaseSway * 100f * power));
        }

        // perk activa sin beberla (eterna o de Classic, desde el principio de la raid): lo mismo que al beber, sin el logro
        internal static void Reapply(Player me)
        {
            RefreshWeapon(me);
            if (Perk.Has("dd_aim")) me.ProceduralWeaponAnimation.WalkEffectorEnabled = false;
        }

        internal static void OnDrink(ActiveHealthController health)
        {
            Achievements.Unlock("dd_fresh");
            RefreshWeapon(health.Player);
            if (Perk.Has("dd_aim")) health.Player.ProceduralWeaponAnimation.WalkEffectorEnabled = false;
        }

        // ---------------------------------------------------------------- bonos del arma (desde el gancho de GetWeaponInfo)
        internal static float AimBonus
        {
            get
            {
                float p = Perks.Power(Id);
                if (p <= 0f) return 0f;
                float b = BaseAim * p;
                if (Perk.Has("dd_quick")) b += QuickAim;
                b += Synergy.AimBonus;                      // sinergia Speed Cola + Deadshot
                return b;
            }
        }

        // Retroceso a la mitad: el juego calcula la fuerza como (1 + mods) * (1 - supX - supY * 0.1). Se sube supX lo justo para que el
        // resultado quede en la mitad.
        internal static Vector2 Recoil(Vector2 sup)
        {
            if (!Perk.Has("dd_sky")) return sup;
            float rest = 1f - sup.x - sup.y * 0.1f;
            return new Vector2(sup.x + rest * (1f - RecoilCut), sup.y);
        }

        // Aplica los bonos al arma que llevas en la mano (al beber, al empezar la raid con la perk activa...)
        internal static void RefreshWeapon(Player me)
        {
            try
            {
                var fc = me != null ? me.HandsController as Player.FirearmController : null;
                if (fc == null) return;
                fc.SyncWithCharacterSkills();
                var info = me.Skills.GetWeaponInfo(fc.Item);
                if (fc._buffInfo != null) fc._buffInfo.AimSpeed = info.AimSpeed;      // (SyncWithCharacterSkills no copia la velocidad de apuntar)
                var pwa = me.ProceduralWeaponAnimation;
                if (pwa != null)
                {
                    pwa.UpdateWeaponVariables();
                    if (pwa.Shootingg != null) pwa.Shootingg.OnWeaponParametersChanged();
                }
            }
            catch (Exception e) { Dbg.Log("ERROR", "Deadshot (arma): " + e.Message); }
        }

        // ---------------------------------------------------------------- Dead Aim: el arma se queda fija en el centro
        // Se quitan de la animacion del arma la respiracion (balanceo al apuntar y en reposo), el andar, la inercia al girar y el
        // temblor de manos. El retroceso (Shooting) y el resto se mantienen. Al dejar de tenerla se devuelven solo los que se quitaron.
        private const EFT.Animations.EProceduralAnimationMask NoSway = EFT.Animations.EProceduralAnimationMask.Breathing
            | EFT.Animations.EProceduralAnimationMask.Walking | EFT.Animations.EProceduralAnimationMask.MotionReaction
            | EFT.Animations.EProceduralAnimationMask.HandShake;
        private static EFT.Animations.ProceduralWeaponAnimation _deadAimPwa;
        private static EFT.Animations.EProceduralAnimationMask _deadAimRemoved;

        // Stillness: agachado o tumbado (sin esprintar) recuperas estamina y aguante de brazos mas rapido
        internal const float StillStamina = 2.0f, StillArms = 1.5f;      // puntos por segundo extra
        internal const float TriggerCost = 0.6f;
        internal const float StrainEnergy = 0.15f;                         // Eye Strain: la energia baja un 15% mas rapido                           // Heavy Trigger: aguante de brazos por disparo

        internal static void TickStillness(Player me, float dt)
        {
            if (!Perk.Has("dd_still") || me == null || me.Physical == null || me.Physical.Sprinting || !Low(me)) return;
            var st = me.Physical.Stamina; var hs = me.Physical.HandsStamina;
            if (st != null) st.Current = Mathf.Min(st.TotalCapacity, st.Current + StillStamina * dt);
            if (hs != null) hs.Current = Mathf.Min(hs.TotalCapacity, hs.Current + StillArms * dt);
        }

        internal static void TickDeadAim(Player me)
        {
            var pwa = me != null ? me.ProceduralWeaponAnimation : null;
            bool on = pwa != null && Perk.Has("dd_aim");
            if (_deadAimPwa != null && (!on || !ReferenceEquals(_deadAimPwa, pwa)))
            {
                try { _deadAimPwa.Mask |= _deadAimRemoved; } catch { }        // se devuelve lo quitado
                _deadAimPwa = null; _deadAimRemoved = 0;
            }
            if (!on) return;
            var take = pwa.Mask & NoSway;
            if (take == 0) return;
            pwa.Mask &= ~take;
            _deadAimRemoved |= take;
            _deadAimPwa = pwa;
        }

        // ---------------------------------------------------------------- balanceo (desde PerkFx.Tick)
        internal static float SwayFactor(Player me, bool aiming)
        {
            float p = Perks.Power(Id);
            if (p <= 0f) return 1f;
            float m = 1f - BaseSway * p;
            if (Perk.Has("dd_aim")) m = 0f;                             // Dead Aim: sin balanceo
            return m;
        }

        internal static bool Low(Player me)
        {
            var mc = me.MovementContext;
            return me.IsInPronePose || (mc != null && mc.PoseLevel < 0.5f);
        }

        internal static bool Aiming(Player me)
        {
            var pwa = me.ProceduralWeaponAnimation;
            return pwa != null && pwa.IsAiming;
        }

        // ---------------------------------------------------------------- bajas (desde PerkFx.OnKill, con la perk activa)
        internal static void OnKill(Player me, Player victim, DamageInfo info, EBodyPart part)
        {
            if (PerkFx.Speed >= MovingSpeed) PerkService.AddProgress("dd_aim");
            if (Armored(victim)) PerkService.AddProgress("dd_pen");
            if (Aiming(me)) PerkService.AddProgress("dd_quick");
            if (Low(me)) PerkService.AddProgress("dd_still");
            if (part == EBodyPart.Head)
            {
                PerkService.AddProgress("dd_head");
                PerkService.AddStat("dd_heads", 1);
                if (Perk.Has("dd_head") && me.Physical != null && me.Physical.Stamina != null)
                {
                    var st = me.Physical.Stamina;
                    st.Current = Mathf.Min(st.TotalCapacity, st.Current + st.TotalCapacity * HeadStamina);
                    Dbg.Log("EFECTO", "Headhunter: +" + Mathf.RoundToInt(HeadStamina * 100f) + "% de estamina");
                }
            }
            if (PerkFx.IsBoss(victim)) PerkService.AddProgress("dd_slot");
            float dist = Vector3.Distance(me.Position, victim.Position);
            if (dist >= 100f) PerkService.AddStat("dd_far", 1);
        }

        internal static bool Armored(Player victim)
        {
            try
            {
                var eq = victim.Equipment;
                if (eq == null) return false;
                return eq.GetSlot(EquipmentSlot.ArmorVest).ContainedItem != null || eq.GetSlot(EquipmentSlot.Headwear).ContainedItem != null;
            }
            catch { return false; }
        }

        // Disparos (para el desafio de To the Skies) y penetracion de Incredible
        internal static int ShotsThisRaid;
        internal static void OnRaidStart() { ShotsThisRaid = 0; }
    }

    // ---------- Incredible: +penetracion de tus balas | contador de disparos ----------
    [HarmonyPatch(typeof(BallisticsCalculator), nameof(BallisticsCalculator.CreateShot))]
    internal static class DeadshotShotPatch
    {
        [HarmonyPostfix]
        private static void Postfix(string player, int fragmentIndex, Shot __result)
        {
            if (__result == null) return;
            if (QuestGuards.Any && QuestGuards.Has(player, Deadshot.Id)) { __result.PenetrationPower *= QuestGuards.Penetration; return; }   // Embotellador con Deadshot
            var me = Plugin.Me;
            if (fragmentIndex == 0 && Bandolier.Active && me != null && player == me.ProfileId)
                try { Bandolier.OnShot(me); } catch (Exception e) { Dbg.Throttled("bbshot", 10f, "ERROR", "Bandolier (disparo): " + e.Message); }
            if (!Deadshot.Active) return;
            if (me == null || player != me.ProfileId) return;
            if (Apex.Has(Deadshot.Id)) __result.PenetrationPower += Apex.PenBonus;      // APEX: +1 clase de penetracion, sea cual sea la municion
            if (Perk.Has("dd_pen")) __result.PenetrationPower *= Deadshot.Penetration;
            if (fragmentIndex == 0)
            {
                Deadshot.ShotsThisRaid++;
                if (Perk.Has("dd_d_shaky") && me.Physical != null && me.Physical.HandsStamina != null)          // Heavy Trigger: cada disparo cansa los brazos
                    me.Physical.HandsStamina.Current = Mathf.Max(0f, me.Physical.HandsStamina.Current - Deadshot.TriggerCost);
                if (Deadshot.ShotsThisRaid % 10 == 0) { PerkService.AddProgress("dd_sky", 10); PerkService.AddStat("dd_shots", 10); }
            }
        }
    }

    // ---------- Dead Aim: sin balanceo del arma al moverte (el juego activa el "Walk" de la animacion al andar) ----------
    [HarmonyPatch(typeof(ProceduralWeaponAnimation), "set_WalkEffectorEnabled")]
    internal static class DeadAimPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ProceduralWeaponAnimation __instance, ref bool value)
        {
            if (!value || !Perk.Has("dd_aim")) return;
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(me.ProceduralWeaponAnimation, __instance)) value = false;
        }
    }
}
