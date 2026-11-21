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
    // ===================================================================================================================
    // Quick Revive: regeneracion.
    //   Base: tras 8 s sin recibir golpes, una parte herida (no destruida) a la vez regenera 1 PV/s. Tambien esprintando.
    //         Cada PV sale de uno de tus botiquines (AI-2, IFAK, Salewa...): sin botiquin no hay regeneracion. Nunca el que tienes en la mano.
    //   Mayores: Revive (con un estimulante en el inventario, que se gasta: un golpe letal te deja Caido 10 s a 1 PV, con solo una
    //            pistola y tumbado; si matas a alguien te levantas con el 30% de vida y tus perks; si no, mueres. Una vez por raid),
    //            Transfusion (recupera las partes destruidas a 1 PV, sin darles mas vida), Combat Medic (en combate curas 0.3 PV/s).
    //   Menores: Patience (5 s), Priority (cabeza y torso x1.5), Clotting (quita sangrados leves mientras regenera),
    //            Reserve (tras un golpe, 3 s a 2 PV/s por parte). Especial: Extra Slot.
    //   Desventajas ligadas: Thin Blood, Expensive Metabolism, Startle | Lukewarm Regen, Scars, No Rush, Weak Mending.
    // ===================================================================================================================
    internal static class QuickRevive
    {
        internal const string Id = "quickrevive";
        internal const string ItemTpl = "6a1c00000000000000000c0a";
        internal const float Rate = 1f, Wait = 8f, PatienceWait = 5f, ThinExtra = 6f;
        internal const float MedicRate = 0.3f, PriorityMult = 1.5f, LukewarmMult = 0.75f, ScarsCap = 0.80f;
        internal const float ReserveSeconds = 3f, ReserveRate = 2f;            // por parte herida
        internal const int MinKitResource = 50;                                // botiquines (no vendas, ferulas ni kits de cirugia)
        internal const float MetabCost = 0.5f, WeakTaken = 1.10f;
        internal const float DownedSeconds = 10f, ReviveHealth = 0.30f;
        internal const int ApexKills = 7;
        internal const string TempPistol = "5448bd6b4bdc2dfc2f8b4569";          // PM
        internal const string TempAmmo = "5737218f245977612125ba51";            // 9x18 PM PST gzh
        internal const float BotRate = 1f, BotWait = 8f;

        private static readonly EBodyPart[] Parts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.LeftArm, EBodyPart.RightArm, EBodyPart.LeftLeg, EBodyPart.RightLeg };

        private static float _lastHit, _acc, _reserveUntil, _reserveReady, _healedAcc, _priorityAcc, _takenAcc, _next1Hz;
        private static bool _regenOn, _reviveUsed, _noStimShown, _noKitShown;
        private static MedKitComponent _kit;
        private static int _pmcKills;
        private static readonly HashSet<EBodyPart> _locked = new HashSet<EBodyPart>();      // Transfusion: recuperadas, ya no reciben PV
        private static readonly HashSet<EBodyPart> _wasDestroyed = new HashSet<EBodyPart>();
        private static readonly HashSet<object> _bleeds = new HashSet<object>();

        // --- Caido ---
        internal static bool Downed;
        internal static bool Bypass;                      // dejar pasar el golpe final al acabarse el tiempo
        private static float _downedUntil, _nextForce;
        private static EBodyPart _lethalPart;
        private static DamageInfo _lethalInfo;
        private static Item _pistol, _tempPistol;
        private static Item _primary;

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power) { return L.F("notice.quickrevive.sub", (Rate * power).ToString("0.#"), Wait.ToString("0")); }

        internal static void Reset()
        {
            _lastHit = Time.time; _acc = 0f; _reserveUntil = 0f; _reserveReady = 0f; _healedAcc = 0f; _priorityAcc = 0f; _takenAcc = 0f; _next1Hz = 0f;
            _regenOn = false; _reviveUsed = false; _noStimShown = false; _noKitShown = false; _kit = null; _pmcKills = 0;
            _locked.Clear(); _wasDestroyed.Clear(); _bleeds.Clear();
            Downed = false; Bypass = false; _downedUntil = 0f; _nextForce = 0f; _pistol = null; _tempPistol = null; _primary = null;
            _botHit.Clear();
        }

        private static float WaitTime
        {
            get
            {
                float w = Perk.Has("qr_patience") ? PatienceWait : Wait;
                if (Perk.Has("qr_d_thin")) w += ThinExtra;
                return w;
            }
        }

        internal static bool Regenerating { get { return Active && _regenOn; } }

        // ---------------------------------------------------------------- golpes recibidos (FinalPerks.OnTaken)
        internal static float OnTaken(Player me, ActiveHealthController hc, EBodyPart part, float damage, DamageInfo info)
        {
            if (!Active) return damage;
            if (Downed) return Bypass ? damage : 0f;                       // Caido: no recibes mas dano hasta levantarte o morir
            var t = info.DamageType;
            bool hit = (t & (PerkFx.Hits | EDamageType.Fall)) != 0;
            if (hit)
            {
                if (_regenOn && Perk.Has("qr_d_weak")) damage *= WeakTaken;       // Curacion debil
                float now = Time.time;
                _lastHit = now;
                if (_regenOn) _regenOn = false;
                if (Perk.Has("qr_reserve") && now >= _reserveReady) { _reserveUntil = now + ReserveSeconds; _reserveReady = now + WaitTime; }
                _takenAcc += damage;
                if (_takenAcc >= 25f) { PerkService.AddProgress("qr_medic", (int)_takenAcc); _takenAcc -= (int)_takenAcc; }
            }

            // Revive: un golpe letal te deja Caido
            if (hit && Perk.Has("qr_revive") && !_reviveUsed && Lethal(hc, part, damage))
            {
                var stim = FindStim(me);
                if (stim == null)
                {
                    if (!_noStimShown) { _noStimShown = true; Hud.Notify(Data.Name("qr_revive"), L.T("notice.qr_nostim"), Id); }
                    return damage;
                }
                float lim = Mathf.Min(hc.GetBodyPartHealth(part).Current, hc.GetBodyPartHealth(EBodyPart.Common).Current);
                damage = Mathf.Max(0f, lim - 1f);
                EnterDowned(me, stim, part, info);
            }
            return damage;
        }

        private static bool Lethal(ActiveHealthController hc, EBodyPart part, float damage)
        {
            float common = hc.GetBodyPartHealth(EBodyPart.Common).Current;
            if (damage >= common) return true;
            if ((part == EBodyPart.Head || part == EBodyPart.Chest) && damage >= hc.GetBodyPartHealth(part).Current) return true;
            // una extremidad destruida reparte el dano: si el torso o la cabeza no aguantan el reparto, tambien es letal
            if (part != EBodyPart.Head && part != EBodyPart.Chest && hc.IsBodyPartDestroyed(part))
                return damage * 0.7f >= Mathf.Min(hc.GetBodyPartHealth(EBodyPart.Chest).Current, hc.GetBodyPartHealth(EBodyPart.Head).Current) * 3f;
            return false;
        }

        private static Item FindStim(Player me)
        {
            return me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)
                .FirstOrDefault(i => i is Stimulator && Perks.ByItem(i.StringTemplateId) == null && i.StringTemplateId != Apex.CoreTpl);
        }

        // ---------------------------------------------------------------- Caido
        private static void EnterDowned(Player me, Item stim, EBodyPart part, DamageInfo info)
        {
            _reviveUsed = true;
            Downed = true;
            _downedUntil = Time.time + DownedSeconds;
            _lethalPart = part; _lethalInfo = info;
            try
            {
                var op = ItemManipulator.Remove(stim, me.InventoryController, false);
                if (op.Failed) Dbg.Log("EFECTO", "Revivir: no se pudo gastar el estimulante (" + op.Error + ")");
            }
            catch (Exception e) { Dbg.Throttled("qrstim", 10f, "ERROR", "Revivir (estimulante): " + e.Message); }

            var eq = me.Profile.Inventory.Equipment;
            _primary = me.HandsController != null ? me.HandsController.Item : null;
            var holster = eq.GetSlot(EquipmentSlot.Holster);
            _pistol = holster.ContainedItem as Weapon;
            if (_pistol == null) _pistol = _tempPistol = MakeTempPistol(holster);
            ForcePistol(me);
            if (!me.IsInPronePose) me.ToggleProne();
            Hud.Notify(Data.Name("qr_revive").ToUpperInvariant(), L.F("notice.qr_revive", DownedSeconds.ToString("0")), Id);
            Dbg.Log("EFECTO", "Revivir: golpe letal en " + part + " -> Caido " + DownedSeconds + " s (pistola " + (_pistol != null ? _pistol.ShortName.Localized() : "ninguna") + ")");
        }

        private static Item MakeTempPistol(Slot holster)
        {
            try
            {
                var w = Singleton<ItemFactory>.Instance.GetPresetItem(TempPistol) as Weapon;
                if (w == null) return null;
                var mag = w.GetAllItems().OfType<Magazine>().FirstOrDefault();
                if (mag != null && mag.Count < mag.MaxCount)
                {
                    var ammo = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), TempAmmo, null);
                    if (ammo != null) { ammo.StackObjectsCount = mag.MaxCount - mag.Count; mag.Cartridges.Add(ammo, false); }
                }
                var r = holster.AddWithoutRestrictions(w);
                if (r.Failed) { Dbg.Log("EFECTO", "Revivir: no cabe la pistola temporal: " + r.Error); return null; }
                Dbg.Log("EFECTO", "Revivir: pistola temporal (PM) en la funda");
                return w;
            }
            catch (Exception e) { Dbg.Throttled("qrpm", 10f, "ERROR", "Revivir (pistola): " + e.Message); return null; }
        }

        private static void ForcePistol(Player me)
        {
            if (_pistol == null || (me.HandsController != null && ReferenceEquals(me.HandsController.Item, _pistol))) return;
            try { me.TryProceed(_pistol, r => { }, false); }
            catch (Exception e) { Dbg.Throttled("qrhands", 10f, "ERROR", "Revivir (manos): " + e.Message); }
        }

        private static void TickDowned(Player me, ActiveHealthController hc)
        {
            float now = Time.time;
            if (now >= _downedUntil) { Die(me, hc); return; }
            var mc = me.MovementContext;
            if (mc != null) mc.ChangeSpeedLimit(0.4f * mc.MaxSpeed, Player.ESpeedLimit.Swamp, 0.3f);
            if (now >= _nextForce)
            {
                _nextForce = now + 0.5f;
                if (!me.IsInPronePose) try { me.ToggleProne(); } catch { }
                ForcePistol(me);
            }
        }

        private static void Die(Player me, ActiveHealthController hc)
        {
            Downed = false;
            Dbg.Log("EFECTO", "Revivir: se acabo el tiempo, mueres");
            RemoveTemp(me);
            try
            {
                Bypass = true;
                hc.ApplyDamage(_lethalPart, 9999f, _lethalInfo);
            }
            catch (Exception e) { Dbg.Throttled("qrdie", 10f, "ERROR", "Revivir (muerte): " + e.Message); }
            finally { Bypass = false; }
            if (hc.IsAlive) hc.Kill(_lethalInfo.DamageType);
        }

        private static void Revive(Player me)
        {
            Downed = false;
            var hc = me.ActiveHealthController;
            if (hc != null)
            {
                Rev.SelfHeal = true;
                try
                {
                    foreach (var p in Parts)
                    {
                        if (hc.IsBodyPartDestroyed(p)) continue;
                        var v = hc.GetBodyPartHealth(p);
                        float want = v.Maximum * ReviveHealth;
                        if (v.Current < want) hc.ChangeHealth(p, want - v.Current, DamageHelper.MedKitUse);
                    }
                }
                finally { Rev.SelfHeal = false; }
            }
            if (_primary != null && !ReferenceEquals(_primary, _tempPistol) && _primary.Parent != null)
                try { me.TryProceed(_primary, r => { }, false); } catch (Exception e) { Dbg.Throttled("qrback", 10f, "ERROR", "Revivir (volver al arma): " + e.Message); }
            _removeTempAt = Time.time + 2f;
            Hud.Notify(Data.Name("qr_revive").ToUpperInvariant(), L.T("notice.qr_up"), Id);
            Dbg.Log("EFECTO", "Revivir: baja estando Caido -> te levantas con el " + Mathf.RoundToInt(ReviveHealth * 100f) + "% de vida");
        }

        private static float _removeTempAt;
        private static void RemoveTemp(Player me)
        {
            if (_tempPistol == null) return;
            if (me.HandsController != null && ReferenceEquals(me.HandsController.Item, _tempPistol)) return;      // aun en la mano: se reintenta
            try
            {
                var slot = _tempPistol.Parent != null ? _tempPistol.Parent.Container as Slot : null;
                if (slot != null) slot.RemoveItemWithoutRestrictions();
                Dbg.Log("EFECTO", "Revivir: pistola temporal retirada");
            }
            catch (Exception e) { Dbg.Throttled("qrrm", 10f, "ERROR", "Revivir (quitar pistola): " + e.Message); }
            _tempPistol = null;
        }

        // ---------------------------------------------------------------- disparo propio (Sobresalto)
        internal static void OnShot()
        {
            if (Active && Perk.Has("qr_d_startle")) { _lastHit = Time.time; _regenOn = false; }
        }

        // ---------------------------------------------------------------- bajas
        internal static void OnKill(Player me, Player victim, bool boss)
        {
            if (!Active) return;
            if (Downed) Revive(me);
            if (boss) PerkService.AddProgress("qr_slot");
            if (_regenOn) PerkService.AddProgress("qr_reserve");
            if (Apex.Has(Id) && _reviveUsed && NewPerkUtil.IsPmc(victim) && ++_pmcKills >= ApexKills)
            {
                _pmcKills = 0; _reviveUsed = false; _noStimShown = false;
                Hud.Notify(Data.Name("qr_revive"), L.T("apex_quickrevive.n"), Id);
                Dbg.Log("APEX", "Quick Revive: Revivir recargado");
            }
        }

        // ---------------------------------------------------------------- cada frame
        internal static void Tick(Player me)
        {
            if (_tempPistol != null && !Downed && Time.time >= _removeTempAt) RemoveTemp(me);
            if (!Active) return;
            var hc = me.ActiveHealthController;
            if (hc == null || !hc.IsAlive) return;
            if (Downed) { TickDowned(me, hc); return; }

            float now = Time.time, dt = Time.deltaTime;
            if (now >= _next1Hz) { _next1Hz = now + 1f; OneHz(hc); }

            float p = Perks.Power(Id);
            bool sprint = me.Physical != null && me.Physical.Sprinting;
            float rate = 0f;
            bool waiting = now - _lastHit < WaitTime;
            if (!waiting)
            {
                if (!_regenOn) { _regenOn = true; PerkService.AddProgress("qr_patience"); }
                rate = Rate * p;
            }
            else
            {
                _regenOn = false;
                if (Perk.Has("qr_medic")) rate = MedicRate * p;
            }
            if (now < _reserveUntil) rate = Mathf.Max(rate, ReserveRate * p);
            if (Perk.Has("qr_d_lukewarm")) rate *= LukewarmMult;
            if (sprint && Perk.Has("qr_d_rush")) rate = 0f;
            if (rate <= 0f) return;

            if (_regenOn && Perk.Has("qr_clot")) StopLightBleeds(hc);
            _acc += rate * dt;
            if (_acc < 0.25f) return;
            float amount = _acc;
            _acc = 0f;
            HealStep(me, hc, amount);
        }

        private static void OneHz(ActiveHealthController hc)
        {
            // Transfusion (desafio): partes destruidas con la perk activa
            foreach (var part in Parts)
            {
                bool d = hc.IsBodyPartDestroyed(part);
                if (d && _wasDestroyed.Add(part)) PerkService.AddProgress("qr_transfuse");
                else if (!d) _wasDestroyed.Remove(part);
            }
            // Clotting (desafio): sangrados mientras regeneras
            if (_regenOn)
            {
                foreach (var e in hc.FindActiveEffects<ActiveHealthController.LightBleeding>(EBodyPart.Common))
                    if (_bleeds.Add(e)) PerkService.AddProgress("qr_clot");
                foreach (var e in hc.FindActiveEffects<ActiveHealthController.HeavyBleeding>(EBodyPart.Common))
                    if (_bleeds.Add(e)) PerkService.AddProgress("qr_clot");
            }
        }

        private static void StopLightBleeds(ActiveHealthController hc)
        {
            foreach (var e in hc.FindActiveEffects<ActiveHealthController.LightBleeding>(EBodyPart.Common).ToList())
            {
                _bleeds.Add(e);
                e.ForceResidue();
                Dbg.Throttled("qrclot", 3f, "EFECTO", "Coagulacion: sangrado leve cortado");
            }
        }

        // botiquin del que sale la vida: el mas gastado de los que llevas encima, sin contar el que tengas en la mano
        private static MedKitComponent Kit(Player me)
        {
            var hands = me.HandsController != null ? me.HandsController.Item : null;
            if (_kit != null && _kit.HpResource >= 0.01f && _kit.Item.Parent != null && !ReferenceEquals(_kit.Item, hands)) return _kit;
            _kit = null;
            foreach (var i in me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment))
            {
                if (ReferenceEquals(i, hands)) continue;
                MedKitComponent mk;
                if (!i.TryGetItemComponent(out mk) || mk.MaxHpResource < MinKitResource || mk.HpResource < 0.01f) continue;
                if (_kit == null || mk.HpResource < _kit.HpResource) _kit = mk;
            }
            return _kit;
        }

        private static void Spend(Player me, MedKitComponent kit, float hp)
        {
            kit.HpResource = Mathf.Max(0f, kit.HpResource - hp);
            kit.Item.RaiseRefreshEvent();
            if (kit.HpResource >= 0.01f) return;
            Dbg.Log("EFECTO", "Quick Revive: botiquin gastado (" + kit.Item.ShortName.Localized() + ")");
            try { var op = ItemManipulator.Remove(kit.Item, me.InventoryController, false); if (op.Failed) Dbg.Log("EFECTO", "Quick Revive: no se pudo quitar el botiquin vacio (" + op.Error + ")"); }
            catch (Exception e) { Dbg.Throttled("qrkit", 10f, "ERROR", "Quick Revive (botiquin vacio): " + e.Message); }
            _kit = null;
        }

        private static void HealStep(Player me, ActiveHealthController hc, float amount)
        {
            // Transfusion: una parte destruida vuelve a 1 PV (y ya no recibe mas vida de la regeneracion)
            if (Perk.Has("qr_transfuse"))
                foreach (var part in Parts)
                {
                    if (part == EBodyPart.Head || part == EBodyPart.Chest || !hc.IsBodyPartDestroyed(part)) continue;
                    if (hc.RestoreBodyPart(part, 1f))
                    {
                        _locked.Add(part);
                        Dbg.Log("EFECTO", "Transfusion: " + part + " recuperada a 1 PV");
                        return;
                    }
                }

            // una parte herida a la vez recibe 'amount' (x1.5 cabeza y torax con Prioridad), pagado del botiquin
            bool prio = Perk.Has("qr_priority");
            float cap = Perk.Has("qr_d_scars") ? ScarsCap : 1f;
            float total = 0f, vitalHeal = 0f;
            Rev.SelfHeal = true;
            try
            {
                foreach (var part in Parts)
                {
                    if (hc.IsBodyPartDestroyed(part) || _locked.Contains(part)) continue;
                    var v = hc.GetBodyPartHealth(part);
                    float missing = v.Maximum * cap - v.Current;
                    if (missing <= 0.05f) continue;
                    var kit = Kit(me);
                    if (kit == null)
                    {
                        if (!_noKitShown) { _noKitShown = true; Hud.Notify(Data.Name(Id), L.T("notice.qr_nokit"), Id); }
                        break;
                    }
                    bool vital = part == EBodyPart.Head || part == EBodyPart.Chest;
                    float heal = Mathf.Min(amount * (prio && vital ? PriorityMult : 1f), missing, kit.HpResource);
                    // tipo "Undefined" y no "Medicine": otros mods (ImprovedSelfcare/HealingAutoCancel) leen las curas de medicina y
                    // cancelaban el botiquin que tienes en la mano al llenarse la parte que cura Quick Revive (y fallaban sin meds en la mano)
                    try { hc.ChangeHealth(part, heal, DamageHelper.UndefinedDamage); }
                    finally { Spend(me, kit, heal); }
                    total += heal;
                    if (vital) vitalHeal += heal;
                    break;   // una sola parte a la vez (cabeza, torax, estomago, brazos, piernas); al llenarse pasa a la siguiente
                }
            }
            finally { Rev.SelfHeal = false; }
            if (total <= 0f) return;
            _noKitShown = false;
            if (Perk.Has("qr_d_metab")) { hc.ChangeEnergy(-MetabCost * total); hc.ChangeHydration(-MetabCost * total); }

            // desafios (agrupados: guardar en cada llamada cuesta)
            _healedAcc += total;
            if (_healedAcc >= 10f) { int n = (int)_healedAcc; PerkService.AddProgress("qr_revive", n); Apex.AddProgress(Id, n); _healedAcc -= n; }
            if (vitalHeal > 0f)
            {
                _priorityAcc += vitalHeal;
                if (_priorityAcc >= 10f) { int n = (int)_priorityAcc; PerkService.AddProgress("qr_priority", n); _priorityAcc -= n; }
            }
        }

        // ---------------------------------------------------------------- bots con Quick Revive: 1 PV/s tras 8 s sin golpes
        private static readonly Dictionary<string, float> _botHit = new Dictionary<string, float>();
        internal static void BotDamaged(Player bot) { _botHit[bot.ProfileId] = Time.time; }

        internal static void BotTick(Player bot, float step)
        {
            float last;
            if (BotRevive.IsDowned(bot.ProfileId)) return;                   // caido: no se cura solo (se levanta entero luego)
            if (_botHit.TryGetValue(bot.ProfileId, out last) && Time.time - last < BotWait) return;
            var hc = bot.ActiveHealthController;
            if (hc == null || !hc.IsAlive) return;
            EBodyPart best = EBodyPart.Common;
            float bestMissing = 0.05f;
            foreach (var part in Parts)
            {
                if (hc.IsBodyPartDestroyed(part)) continue;
                var v = hc.GetBodyPartHealth(part);
                if (v.Maximum - v.Current > bestMissing) { bestMissing = v.Maximum - v.Current; best = part; }
            }
            if (best != EBodyPart.Common) hc.ChangeHealth(best, Mathf.Min(BotRate * step, bestMissing), DamageHelper.MedKitUse);
        }
    }

    // ---------- Caido: no puedes levantarte (tumbado obligatorio) ----------
    [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.IsInPronePose), MethodType.Setter)]
    internal static class DownedPronePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(MovementContext __instance, bool value)
        {
            if (value || !QuickRevive.Downed) return true;
            var me = Plugin.Me;
            return me == null || !ReferenceEquals(me.MovementContext, __instance);
        }
    }
}
