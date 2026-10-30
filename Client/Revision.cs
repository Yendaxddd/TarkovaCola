using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.Ballistics;
using EFT.CameraControl;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Revision de mejoras y desventajas de la 3.0 (REVISION_AUMENTOS.md): las desventajas que eran "impuestos" genericos
    //  (hambre, sed, correr, sacar el arma) pasan a castigar en el mismo terreno en que ayuda su perk. Los ids no cambian
    //  (nadie pierde lo que tenia equipado); solo cambia el efecto.
    //   Speed Cola   a_bolt  Recarga en caliente | d_crash Bajon | d_dry A medias
    //   Stamin-Up    dt_hungry Inquieto | dt_sweaty Inercia
    //   Juggernog    dj_dead Blanco grande | dj_sluggish Cura lenta | dj_metab Torpe
    //   PHD Flopper  pf_drop Caida explosiva (mayor nueva) | dp_powder Adicto a la polvora (nueva) | dp_lazy Metralla | dp_pockets Panico
    //   Widow's Wine ww_fuse Nido | dw_bitter Sin red | dw_greasy Seda pegajosa | dw_hungry Huevos fragiles | dw_dry Telarana enredosa
    //   Deadshot     dd_d_tunnel Vision de tunel | dd_d_breath Todo o nada | dd_d_strain Pulso nervioso | dd_d_lime Ojo seco | dd_d_sugar Tiro limpio
    //   Bandolier    bb_disc Cinturon de balas
    //   Vulture Aid  va_d_greedy Codicia | va_d_hunch Carronero marcado | va_d_dry Mochila llena | va_d_grip Ruido de bolsas
    //   Macchiato    mm_d_crash Sed de sangre | mm_d_bitter Impulsivo | mm_d_sugar Golpe en falso
    // ============================================================================================
    internal static class Rev
    {
        // --- balance ---
        internal const float HotRate = 1.15f, HotSeconds = 4f;          // Recarga en caliente
        internal const float CrashAfter = 120f;                         // Bajon
        internal const float RestlessAfter = 8f;                        // Inquieto
        internal const float InertiaTime = 0.4f;                        // Inercia
        internal const float BigTarget = 1.15f;                         // Blanco grande
        internal const float SlowHeal = 0.75f;                          // Cura lenta
        internal const float ShrapnelRadius = 10f, ShrapnelChance = 0.30f;   // Metralla
        internal const float PanicRadius = 10f, PanicCost = 20f;        // Panico
        internal const float PowderSway = 1.15f;                        // Adicto a la polvora
        internal const float NestSeconds = 20f, NestRadius = 1.6f, NestHold = 4f;   // Nido
        internal const float NoNetTaken = 1.10f;                        // Sin red
        internal const float StickySilkAnim = 0.75f;                    // Seda pegajosa (lanzar tarda mas)
        internal const float FragileChance = 0.25f;                     // Huevos fragiles
        internal const float WebSelfLeft = 0.20f;                       // Telarana enredosa (Enredado deja el 60%)
        internal const float TunnelFov = 0.90f;                         // Vision de tunel
        internal const float AllOrNothing = 0.90f;                      // Todo o nada
        internal const float NervousSpread = 1.20f;                     // Pulso nervioso
        internal const float DryEyeAfter = 6f, DryEyeSway = 1.5f;       // Ojo seco
        internal const float CleanWindow = 0.5f, CleanRecoil = 1.30f;   // Tiro limpio
        internal const int BeltMags = 6; internal const float BeltDamage = 1.10f;   // Cinturon de balas
        internal const float Greed = 0.80f;                             // Codicia: registrar un 25% mas lento
        internal const float MarkedSeconds = 10f;                       // Carronero marcado
        internal const float FullBag = 0.80f, FullBagSpeed = 0.95f;     // Mochila llena
        internal const float RustleRadius = 8f;                         // Ruido de bolsas
        internal const float BloodAfter = 90f, BloodDamage = 0.85f;     // Sed de sangre
        internal const float ImpulseDelay = 0.3f, DrawGuess = 0.6f;     // Impulsivo
        internal const float MissCost = 10f;                            // Golpe en falso

        // --- estado (por raid) ---
        internal static float HotUntil;
        internal static bool NoReloadBonus;
        internal static int FullMags, Grenades;
        internal static bool InvOpen;
        internal static bool InFallBlast, SelfHeal;
        private static bool _hotPending, _half, _crashOn;
        private static float _reloadAt, _stillFor, _prevStamina = -1f, _aimFor, _next1Hz, _markedUntil, _nextMark, _nextRustle;
        private static float _impulseUntil, _swingAt, _lastMeleeHit, _lastFallBlast, _nextNest;
        private static bool _swingHit, _wasKnife, _eggPending;
        private struct Nest { public Vector3 Pos; public float Until; }
        private static readonly List<Nest> _nests = new List<Nest>();
        private static int _nestKey = -1000;

        internal static void OnRaidStart()
        {
            HotUntil = 0f; NoReloadBonus = false; FullMags = 0; Grenades = 1; InvOpen = false; InFallBlast = false; SelfHeal = false;
            _hotPending = _half = _crashOn = false; _reloadAt = 0f; _stillFor = 0f; _prevStamina = -1f; _aimFor = 0f; _next1Hz = 0f;
            _markedUntil = 0f; _nextMark = 0f; _nextRustle = 0f; _impulseUntil = 0f; _swingAt = 0f; _lastMeleeHit = Time.time; _lastFallBlast = -10f;
            _swingHit = _wasKnife = _eggPending = false; _nests.Clear(); _nextNest = 0f;
        }

        // ---------------------------------------------------------------- cada frame (desde PerkFx.Tick, con alguna perk activa)
        internal static void Tick(Player me, bool still)
        {
            float now = Time.time, dt = Time.deltaTime;
            var fc = me.HandsController as Player.FirearmController;
            var st = me.Physical != null ? me.Physical.Stamina : null;

            // ---- Speed Cola: Recarga en caliente (al terminar la recarga) y A medias / Bajon (sin bonus de recarga) ----
            if ((_hotPending || _half) && now - _reloadAt > 0.3f && (fc == null || !fc.IsInReloadOperation()))
            {
                if (_hotPending) { HotUntil = now + HotSeconds; Dbg.Log("EFECTO", "Recarga en caliente: cadencia +15% durante " + HotSeconds + " s"); }
                _hotPending = false;
                if (_half) { _half = false; UpdateReloadBonus(me); }
            }
            bool crash = Perk.Has("d_crash") && now - Perk.LastKillOrDrink > CrashAfter;
            if (crash != _crashOn)
            {
                _crashOn = crash;
                UpdateReloadBonus(me);
                if (crash) Hud.Notify(Data.Name("d_crash"), L.T("notice.d_crash"), Perks.SpeedCola.Id);
            }

            // ---- Stamin-Up: Inquieto (quieto mas de 8 s, la estamina no se recupera) ----
            _stillFor = still ? _stillFor + dt : 0f;
            if (st != null)
            {
                if (_stillFor > RestlessAfter && _prevStamina >= 0f && st.Current > _prevStamina && Perk.Has("dt_hungry")) st.Current = _prevStamina;
                _prevStamina = st.Current;
            }

            // ---- Deadshot: Ojo seco (tiempo apuntando seguido) ----
            _aimFor = Deadshot.Aiming(me) ? _aimFor + dt : 0f;

            // ---- Melee Macchiato: Golpe en falso y el reloj de Sed de sangre ----
            if (!Perk.Has("mm_d_crash")) _lastMeleeHit = now;           // la cuenta empieza cuando la desventaja esta activa
            if (_swingAt > 0f && now - _swingAt > 0.8f)
            {
                if (!_swingHit && Perk.Has("mm_d_sugar") && st != null) { st.Current = Mathf.Max(0f, st.Current - MissCost); Dbg.Log("EFECTO", "Golpe en falso: -" + MissCost + " de estamina"); }
                _swingAt = 0f;
            }

            // ---- Widow's Wine: Huevos fragiles (pendiente desde el dano) y Nido ----
            if (_eggPending) { _eggPending = false; BreakGrenade(me); }
            if (_nests.Count > 0 && now >= _nextNest) { _nextNest = now + 0.2f; TickNests(me, now); }

            // ---- Vulture Aid: Carronero marcado ----
            if (now < _markedUntil && now >= _nextMark)
            {
                _nextMark = now + 2f;
                Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, 40f, AISoundType.step);
            }

            if (now >= _next1Hz) { _next1Hz = now + 1f; OneHz(me); }
        }

        private static void OneHz(Player me)
        {
            // municion y granadas que llevas (Cinturon de balas, Sin red, Adicto a la polvora)
            int mags = 0, grenades = 0;
            foreach (var it in me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment))
            {
                if (it is Magazine m && m.MaxCount > 0 && m.Count >= m.MaxCount && !(m.Parent?.Container?.ParentItem is Weapon)) mags++;
                else if (it is ThrowWeap tw && tw.ThrowType != ThrowWeapType.smoke_grenade && tw.ThrowType != ThrowWeapType.sonar) grenades++;
            }
            FullMags = mags; Grenades = grenades;

            // Mochila llena: mas del 80% de la mochila ocupada, un 5% mas lento
            if (Perk.Has("va_d_dry") && BackpackFill(me) > FullBag && me.MovementContext != null)
                me.MovementContext.ChangeSpeedLimit(FullBagSpeed * me.MovementContext.MaxSpeed, Player.ESpeedLimit.Swamp, 1.2f);
        }

        private static float BackpackFill(Player me)
        {
            var bag = me.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem as CompoundItem;
            if (bag == null) return 0f;
            int used = 0, total = 0;
            foreach (var grid in bag.Grids)
            {
                total += grid.GridWidth * grid.GridHeight;
                foreach (var it in grid.Items) { var s = it.CalculateCellSize(); used += s.X * s.Y; }
            }
            return total > 0 ? used / (float)total : 0f;
        }

        // ---------------------------------------------------------------- balanceo (desde PerkFx.Tick)
        internal static float SwayFactor(Player me)
        {
            float m = 1f;
            if (Grenades == 0 && Perk.Has("dp_powder")) m *= PowderSway;
            if (_aimFor > DryEyeAfter && Perk.Has("dd_d_lime")) m *= DryEyeSway;
            return m;
        }

        // ---------------------------------------------------------------- Speed Cola
        internal static void OnReloadStart(Player.FirearmController fc)
        {
            var w = fc.Item as Weapon;
            var cur = w != null ? w.GetCurrentMagazine() : null;
            bool empty = cur == null || cur.Count == 0;
            _reloadAt = Time.time;
            if (empty && Plugin.SpeedColaActive) PerkService.AddProgress("a_bolt");      // desafio: recargar cargadores vacios
            if (empty && Perk.Has("a_bolt")) _hotPending = true;
            if (!empty && Perk.Has("d_dry"))
            {
                _half = true;
                UpdateReloadBonus(fc._player);
                Dbg.Log("EFECTO", "A medias: recarga con balas en el cargador, sin bonus de recarga");
            }
        }

        private static void UpdateReloadBonus(Player me)
        {
            NoReloadBonus = _half || _crashOn;
            var fc = me != null ? me.HandsController as Player.FirearmController : null;
            if (fc != null) try { fc.SyncWithCharacterSkills(); } catch (Exception e) { Dbg.Throttled("revsync", 10f, "ERROR", "recarga: " + e.Message); }
        }

        // ---------------------------------------------------------------- manos (desde Plugin.OnHandsChanged)
        internal static void OnHandsChanged(object hc)
        {
            if (hc is Player.FirearmController && _wasKnife && Perk.Has("mm_d_bitter"))
                _impulseUntil = Time.time + DrawGuess + ImpulseDelay;
            _wasKnife = hc is Player.KnifeController;
        }

        internal static bool Impulsive { get { return Time.time < _impulseUntil; } }

        // ---------------------------------------------------------------- Melee Macchiato
        internal static void OnSwing() { _swingAt = Time.time; _swingHit = false; }
        internal static void OnMeleeHit() { _swingHit = true; _lastMeleeHit = Time.time; }

        // ---------------------------------------------------------------- dano
        internal static float OnDamageTaken(Player me, EBodyPart part, float damage, DamageInfo info)
        {
            bool bullet = (info.DamageType & (EDamageType.Bullet | EDamageType.Sniper)) != 0;
            if (Grenades == 0 && Perk.Has("dw_bitter")) damage *= NoNetTaken;
            if (bullet && (part == EBodyPart.Chest || part == EBodyPart.Stomach) && Grenades > 0 && Perk.Has("dw_hungry") && UnityEngine.Random.value < FragileChance)
                _eggPending = true;
            return damage;
        }

        internal static float OnDamageDealt(Player me, EBodyPart part, float damage, DamageInfo info)
        {
            bool bullet = (info.DamageType & (EDamageType.Bullet | EDamageType.Sniper)) != 0;
            if (!bullet) return damage;
            if (part != EBodyPart.Head && Perk.Has("dd_d_breath")) damage *= AllOrNothing;
            if (FullMags >= BeltMags && Perk.Has("bb_disc")) damage *= BeltDamage;
            if (Time.time - _lastMeleeHit > BloodAfter && Perk.Has("mm_d_crash")) damage *= BloodDamage;
            return damage;
        }

        // ---------------------------------------------------------------- explosiones (desde GrenadeExplosionPatch)
        internal static void OnExplosion(Player me, Vector3 pos)
        {
            if (InFallBlast) return;
            float d = Vector3.Distance(me.Position, pos);
            if (d <= PanicRadius && Perk.Has("dp_pockets") && me.Physical != null && me.Physical.Stamina != null)
            {
                me.Physical.Stamina.Current = Mathf.Max(0f, me.Physical.Stamina.Current - PanicCost);
                Dbg.Log("EFECTO", "Panico: explosion a " + d.ToString("0.0") + " m, -" + PanicCost + " de estamina");
            }
            if (d <= ShrapnelRadius && Perk.Has("dp_lazy") && UnityEngine.Random.value < ShrapnelChance)
            {
                var parts = new[] { EBodyPart.LeftArm, EBodyPart.RightArm, EBodyPart.LeftLeg, EBodyPart.RightLeg, EBodyPart.Stomach };
                var p = parts[UnityEngine.Random.Range(0, parts.Length)];
                try { me.ActiveHealthController.DoBleed(false, p); Dbg.Log("EFECTO", "Metralla: sangrado leve en " + p); }
                catch (Exception e) { Dbg.Throttled("shrapnel", 10f, "ERROR", "Metralla: " + e.Message); }
            }
        }

        // Caida explosiva: al caer de 3 m o mas, una explosion pequena alrededor (PHD Flopper te hace inmune a ella)
        internal static void FallBlast(Player me)
        {
            if (Time.time - _lastFallBlast < 1f) return;
            _lastFallBlast = Time.time;
            try
            {
                var w = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), "5448be9a4bdc2dfd2f8b456a", null) as ThrowWeap;   // RGD-5
                if (w == null) return;
                InFallBlast = true;
                Creatura.Bomblet(w, me.Position + Vector3.up * 0.1f, me.ProfileId, w);
                Dbg.Log("EFECTO", "Caida explosiva");
            }
            catch (Exception e) { Plugin.Log.LogError("Caida explosiva: " + e); }
            finally { InFallBlast = false; }
        }

        // ---------------------------------------------------------------- Widow's Wine
        internal static void LayNest(Vector3 pos)
        {
            _nests.Add(new Nest { Pos = pos, Until = Time.time + NestSeconds });
            WebVfx.Burst(_nestKey--, pos, NestRadius, NestSeconds);
            Dbg.Log("EFECTO", "Nido: trampa en " + pos.ToString("F1") + " durante " + NestSeconds + " s");
        }

        private static void TickNests(Player me, float now)
        {
            var world = Singleton<GameWorld>.Instance;
            if (world == null) return;
            for (int i = _nests.Count - 1; i >= 0; i--)
            {
                var n = _nests[i];
                if (now > n.Until) { _nests.RemoveAt(i); continue; }
                foreach (var p in world.AllAlivePlayersList)
                {
                    if (p == null || p == me || p.MovementContext == null || (p.Position - n.Pos).sqrMagnitude > NestRadius * NestRadius) continue;
                    p.MovementContext.ChangeSpeedLimit(0.05f * p.MovementContext.MaxSpeed, Player.ESpeedLimit.Shot, NestHold);
                    WebVfx.OnVictim(p.Position, NestHold);
                    Hud.Notify(Data.Name("ww_fuse"), L.T("notice.ww_fuse"), WidowsWine.Id);
                    Dbg.Log("EFECTO", "Nido: enemigo atrapado");
                    _nests.RemoveAt(i);
                    break;
                }
            }
        }

        private static void BreakGrenade(Player me)
        {
            try
            {
                var inHand = (me.HandsController as Player.ItemHandsController)?.Item;
                var g = me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).OfType<ThrowWeap>()
                    .FirstOrDefault(t => !ReferenceEquals(t, inHand) && t.ThrowType != ThrowWeapType.smoke_grenade && t.ThrowType != ThrowWeapType.sonar);
                if (g == null) return;
                var op = ItemManipulator.Remove(g, me.InventoryController, true);
                if (op.Failed) return;
                me.InventoryController.TryRunNetworkTransaction(op);
                Hud.Notify(Data.Name("dw_hungry"), L.F("notice.dw_hungry", g.ShortName.Localized()), WidowsWine.Id);
                Dbg.Log("EFECTO", "Huevos fragiles: se rompe " + g.ShortName);
            }
            catch (Exception e) { Dbg.Throttled("eggs", 10f, "ERROR", "Huevos fragiles: " + e.Message); }
        }

        // ---------------------------------------------------------------- Vulture Aid
        internal static void OnLootedPmc()
        {
            if (!Perk.Has("va_d_hunch")) return;
            var me = Plugin.Me;
            if (me == null) return;
            _markedUntil = Time.time + MarkedSeconds; _nextMark = Time.time + 2f;
            Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, 60f, AISoundType.gun);
            Hud.Notify(Data.Name("va_d_hunch"), L.T("notice.va_d_hunch"), VultureAid.Id);
            Dbg.Log("EFECTO", "Carronero marcado: los bots cercanos saben donde estas");
        }

        internal static void OnInventoryMove(Player me)
        {
            if (!InvOpen || !Perk.Has("va_d_grip") || Time.time < _nextRustle) return;
            _nextRustle = Time.time + 1.5f;
            Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, RustleRadius, AISoundType.step);
        }
    }

    // ============================================================================================
    //  Ganchos de la revision
    // ============================================================================================

    // Recarga en caliente: +15% de cadencia unos segundos (solo tus armas)
    [HarmonyPatch]
    internal static class HotReloadPatch
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(Weapon), nameof(Weapon.FireRate));
            yield return AccessTools.PropertyGetter(typeof(Weapon), nameof(Weapon.SingleFireRate));
        }

        [HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref int __result)
        {
            if (Time.time >= Rev.HotUntil) return;
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(__instance.Owner, me.InventoryController)) __result = Mathf.RoundToInt(__result * Rev.HotRate);
        }
    }

    // Inercia (no apuntar justo despues de correr) e Impulsivo (no apuntar ni disparar justo despues de guardar el cuchillo)
    [HarmonyPatch(typeof(Player.FirearmController), nameof(Player.FirearmController.SetAim), new[] { typeof(bool) })]
    internal static class InertiaAimPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player.FirearmController __instance, ref bool value)
        {
            if (!value || !Perks.AnyActive || !Util.IsMe(__instance._player)) return;
            if ((Time.time - PerkFx.LastSprint < Rev.InertiaTime && Perk.Has("dt_sweaty")) || Rev.Impulsive) value = false;
        }
    }

    [HarmonyPatch(typeof(Player.FirearmController), nameof(Player.FirearmController.SetTriggerPressed))]
    internal static class ImpulsiveTriggerPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player.FirearmController __instance, ref bool pressed)
        {
            if (pressed && Rev.Impulsive && Util.IsMe(__instance._player)) pressed = false;
        }
    }

    // Blanco grande: los bots te ven desde un 15% mas lejos (se aplica cuando el bot recalcula su distancia de vision)
    [HarmonyPatch(typeof(LookSensor), nameof(LookSensor.CalcVisibleDistance))]
    internal static class BigTargetPatch
    {
        [HarmonyPrefix]
        private static void Prefix(LookSensor __instance, out bool __state) { __state = __instance._nextUpdateVisibleDist < Time.time; }

        [HarmonyPostfix]
        private static void Postfix(LookSensor __instance, bool __state)
        {
            if (!__state || !Perk.Has("dj_dead")) return;
            __instance.VisibleDist *= Rev.BigTarget;
            __instance.ClearVisibleDist *= Rev.BigTarget;
        }
    }

    // Cura lenta: los botiquines curan un 25% menos por uso
    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.ChangeHealth))]
    internal static class SlowHealPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ActiveHealthController __instance, ref float value, DamageInfo damageInfo)
        {
            if (value <= 0f || Rev.SelfHeal || damageInfo.DamageType != EDamageType.Medicine || !Perk.Has("dj_sluggish") || !Util.IsMe(__instance.Player)) return;
            value *= Rev.SlowHeal;
        }
    }

    // Torpe: saltar gasta el doble de estamina
    [HarmonyPatch(typeof(Physical), nameof(Physical.OnJump))]
    internal static class ClumsyJumpPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Physical __instance, out float __state)
        {
            __state = -1f;
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(me.Physical, __instance) && __instance.Stamina != null && Perk.Has("dj_metab")) __state = __instance.Stamina.Current;
        }

        [HarmonyPostfix]
        private static void Postfix(Physical __instance, float __state)
        {
            if (__state < 0f) return;
            float spent = __state - __instance.Stamina.Current;
            if (spent > 0f) __instance.Stamina.Current = Mathf.Max(0f, __instance.Stamina.Current - spent);
        }
    }

    // Vision de tunel: al apuntar, el campo de vision se estrecha un 10% (el juego pide el FOV de apuntar sin aplicarlo a la camara del ojo)
    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.SetFov))]
    internal static class TunnelFovPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref float x, bool applyFovOnCamera)
        {
            if (!applyFovOnCamera && Perk.Has("dd_d_tunnel")) x *= Rev.TunnelFov;
        }
    }

    // Pulso nervioso: desde la cadera, un 20% mas de dispersion
    [HarmonyPatch(typeof(Weapon), nameof(Weapon.GetTotalCenterOfImpact))]
    internal static class NervousSpreadPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref float __result)
        {
            if (!Perk.Has("dd_d_strain")) return;
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(__instance.Owner, me.InventoryController) && !Deadshot.Aiming(me)) __result *= Rev.NervousSpread;
        }
    }

    // Tiro limpio: disparar menos de 0.5 s despues de correr da un 30% mas de retroceso
    [HarmonyPatch(typeof(ShotEffector), nameof(ShotEffector.Process))]
    internal static class CleanShotPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ShotEffector __instance, ref float str)
        {
            if (Time.time - PerkFx.LastSprint >= Rev.CleanWindow || !Perk.Has("dd_d_sugar")) return;
            var me = Plugin.Me;
            if (me != null && me.ProceduralWeaponAnimation != null && ReferenceEquals(me.ProceduralWeaponAnimation.Shootingg, __instance)) str *= Rev.CleanRecoil;
        }
    }

    // Codicia: registrar cuerpos y contenedores un 25% mas lento
    [HarmonyPatch(typeof(SkillManager), "get_AttentionLootSpeedValue")]
    internal static class GreedSearchPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SkillManager __instance, ref float __result)
        {
            if (!Perk.Has("va_d_greedy")) return;
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(me.Skills, __instance)) __result = (1f + __result) * Rev.Greed - 1f;
        }
    }

    // Ruido de bolsas: saber si tienes el inventario abierto, y cada movimiento de objetos con el abierto
    [HarmonyPatch(typeof(Player), nameof(Player.SetInventoryOpened))]
    internal static class InventoryOpenPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool opened) { if (__instance.IsYourPlayer) Rev.InvOpen = opened; }
    }

    [HarmonyPatch(typeof(Player.PlayerInventoryController), nameof(Player.PlayerInventoryController.TryRunNetworkTransaction))]
    internal static class InventoryRustlePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player.PlayerInventoryController __instance)
        {
            if (!Rev.InvOpen || !Perks.AnyActive) return;
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(me.InventoryController, __instance)) Rev.OnInventoryMove(me);
        }
    }
}
