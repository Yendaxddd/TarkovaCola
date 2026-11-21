using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ===================================================================================================================
    // Tombstone Soda: Espectro (como el "Ghost" de Cold War).
    //   Base: cuando un golpe te deja por debajo del 25% de tu vida total, dejas una lapida senuelo y te vuelves espectro 4 s: los bots
    //         te pierden (no te ven ni te disparan) y van a por la lapida; te mueves un 30% mas rapido pero no puedes disparar.
    //         Como mucho cada 3 minutos. No revive (eso es Quick Revive): sirve para salir de un tiroteo.
    //   Mayores: Offering (si mueres, lo de los bolsillos y el chaleco vuelve por correo, una vez por raid), Poltergeist (la lapida
    //            estalla al acabar: aturde 2 s a 5 m), Haunting (puedes disparar: el primer disparo x2 y acaba el espectro).
    //   Menores: Long Haunt (6 s), Restless (cada 2 min), Ectoplasm (corta sangrados y cura 15 PV), Cold Trail (los bots tardan 3 s
    //            mas en encontrarte). Especial: Extra Slot.
    //   Desventajas ligadas: Short Haunt, Heavy Soul, Marked | Cold, Wail, Slow Ghost, Fragile Tombstone.
    // ===================================================================================================================
    internal static class Tombstone
    {
        internal const string Id = "tombstone";
        internal const string ItemTpl = "6a1c00000000000000000c0e";
        internal const float Threshold = 0.25f;
        internal const float Seconds = 4f, LongSeconds = 6f, ShortSeconds = 2f, ApexSeconds = 6f;
        internal const float Cooldown = 180f, RestlessCooldown = 120f, HeavyCooldown = 300f;
        internal const float Speed = 1.30f;
        internal const float PolterRadius = 5f, PolterStun = 2f;
        internal const float EctoHeal = 15f, TrailSeconds = 3f;
        internal const float WailRadius = 40f, FragileSeconds = 1.5f;
        internal const float BotHeal = 0.30f;

        internal static bool Specter;                    // ahora mismo eres espectro
        private static float _until, _hiddenUntil, _readyAt, _decoyUntil, _endedAt = -99f, _nextLure, _totalAcc, _escapeCheck;
        private static bool _hauntShot, _offeringUsed, _readyShown = true, _escapeHit;
        private static int _hauntFrame = -1;
        private static Vector3 _tomb;
        private static GameObject _tombGo;
        private static readonly HashSet<string> _botUsed = new HashSet<string>();

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power) { return L.F("notice.tombstone.sub", (Seconds * Mathf.Max(0.5f, power)).ToString("0.#")); }

        internal static void Reset()
        {
            Specter = false; _until = 0f; _hiddenUntil = 0f; _readyAt = 0f; _decoyUntil = 0f; _endedAt = -99f; _nextLure = 0f; _totalAcc = 0f; _escapeCheck = 0f;
            _hauntShot = false; _offeringUsed = false; _readyShown = true; _escapeHit = false; _hauntFrame = -1;
            DestroyTomb();
            _botUsed.Clear();
        }

        // los bots no te ven (espectro y, con Cold Trail, 3 s despues)
        internal static bool Hidden { get { return Time.time < _hiddenUntil; } }
        internal static bool CanShoot { get { return !Specter || Perk.Has("ts_haunt"); } }

        private static float Duration
        {
            get
            {
                if (Apex.Has(Id)) return ApexSeconds;
                float d = Perk.Has("ts_long") ? LongSeconds : Seconds;
                if (Perk.Has("ts_d_short")) d = ShortSeconds;
                return d * Mathf.Max(0.5f, Perks.Power(Id));
            }
        }

        private static float CooldownTime
        {
            get
            {
                float c = Perk.Has("ts_restless") ? RestlessCooldown : Cooldown;
                if (Perk.Has("ts_d_heavy")) c = HeavyCooldown;
                return c;
            }
        }

        internal static float SpeedMult { get { return Specter && Active && !Perk.Has("ts_d_slow") ? Speed : 1f; } }

        // ---------------------------------------------------------------- golpe recibido (FinalPerks.OnTaken, despues de Quick Revive)
        internal static void OnTaken(Player me, ActiveHealthController hc, float damage, DamageInfo info)
        {
            if (!Active || damage <= 0f) return;
            if (Time.time < _escapeCheck) _escapeHit = true;
            if (Specter || Time.time < _readyAt || (info.DamageType & PerkFx.Hits) == 0) return;
            var common = hc.GetBodyPartHealth(EBodyPart.Common);
            if (common.Maximum <= 0f || damage >= common.Current) return;           // un golpe letal no lo salva Tombstone
            if ((common.Current - damage) / common.Maximum >= Threshold) return;
            Enter(me, hc);
        }

        private static void Enter(Player me, ActiveHealthController hc)
        {
            float now = Time.time;
            Specter = true;
            _until = now + Duration;
            _hiddenUntil = _until;
            _readyAt = now + CooldownTime;
            _readyShown = false;
            _hauntShot = false;
            _tomb = me.Position;
            _decoyUntil = now + (Perk.Has("ts_d_fragile") ? FragileSeconds : Duration);
            _nextLure = 0f;
            MakeTomb(_tomb);
            PerkService.AddProgress("ts_offering");
            PerkService.AddProgress("ts_restless");
            Apex.AddProgress(Id, 1f);

            bool bleeding = hc.FindActiveEffects<ActiveHealthController.LightBleeding>(EBodyPart.Common).Any()
                         || hc.FindActiveEffects<ActiveHealthController.HeavyBleeding>(EBodyPart.Common).Any();
            if (bleeding) PerkService.AddProgress("ts_ecto");
            if (Perk.Has("ts_ecto"))
            {
                foreach (var e in hc.FindActiveEffects<ActiveHealthController.LightBleeding>(EBodyPart.Common).ToList()) e.ForceResidue();
                foreach (var e in hc.FindActiveEffects<ActiveHealthController.HeavyBleeding>(EBodyPart.Common).ToList()) e.ForceResidue();
                HealMostHurt(hc, EctoHeal);
            }
            if (Perk.Has("ts_d_wail")) Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, WailRadius, AISoundType.gun);
            Hud.Notify(L.T("perk.tombstone").ToUpperInvariant(), L.T("notice.ts_on"), Id);
            Dbg.Log("EFECTO", "Tombstone: espectro " + Duration.ToString("0.0") + " s (siguiente en " + CooldownTime.ToString("0") + " s)");
        }

        private static void Exit(Player me, bool byShot)
        {
            Specter = false;
            float now = Time.time;
            _endedAt = now;
            _hiddenUntil = Perk.Has("ts_trail") ? now + TrailSeconds : now;
            _escapeCheck = now + 10f; _escapeHit = false;
            if (Perk.Has("ts_polter")) Poltergeist(me);
            if (Perk.Has("ts_d_cold") && me.Physical != null && me.Physical.Stamina != null) me.Physical.Stamina.Current = 0f;
            if (Perk.Has("ts_d_marked")) RevealToNearest(me);
            DestroyTomb();
            Dbg.Log("EFECTO", "Tombstone: fin del espectro" + (byShot ? " (disparo de Haunting)" : ""));
        }

        // ---------------------------------------------------------------- cada frame
        internal static void Tick(Player me)
        {
            if (!Active) { if (Specter) Exit(me, false); return; }
            float now = Time.time, dt = Time.deltaTime;
            if (!_readyShown && now >= _readyAt && !Specter)
            {
                _readyShown = true;
                Hud.Notify(L.T("perk.tombstone"), L.T("notice.ts_ready"), Id);
            }
            if (_escapeCheck > 0f && now >= _escapeCheck)
            {
                _escapeCheck = 0f;
                if (!_escapeHit) PerkService.AddProgress("ts_trail");
            }
            if (!Specter) return;
            _totalAcc += dt;
            if (_totalAcc >= 5f) { PerkService.AddProgress("ts_long", (int)_totalAcc); _totalAcc -= (int)_totalAcc; }
            if (now >= _until) { Exit(me, false); return; }
            if (now < _decoyUntil && now >= _nextLure) { _nextLure = now + 0.5f; Lure(me); }
            if (now >= _decoyUntil && _tombGo != null) DestroyTomb();
        }

        // los bots que te tenian creen que estas en la lapida
        private static void Lure(Player me)
        {
            var world = Singleton<GameWorld>.Instance;
            if (world == null) return;
            var groups = new HashSet<object>();
            foreach (var p in world.AllAlivePlayersList)
            {
                var bot = p != null && p.IsAI && p.AIData != null ? p.AIData.BotOwner : null;
                if (bot == null || bot.BotsGroup == null || !groups.Add(bot.BotsGroup) || !bot.BotsGroup.IsEnemy(me)) continue;
                try { bot.BotsGroup.SetEnemyPos(me, _tomb, _tomb + Vector3.up * 1.2f, EEnemyPartVisibleType.Sence); }
                catch (Exception e) { Dbg.Throttled("tslure", 10f, "ERROR", "Tombstone (senuelo): " + e.Message); }
            }
        }

        private static void RevealToNearest(Player me)
        {
            var world = Singleton<GameWorld>.Instance;
            if (world == null) return;
            Player best = null; float bestD = float.MaxValue;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || !p.IsAI || BotPerks.IsFollower(p)) continue;
                float d = (p.Position - me.Position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            var bot = best != null && best.AIData != null ? best.AIData.BotOwner : null;
            if (bot == null || bot.BotsGroup == null) return;
            try
            {
                bot.BotsGroup.SetEnemyPos(me, me.Position, me.WeaponRoot != null ? me.WeaponRoot.position : me.Position, EEnemyPartVisibleType.Sence);
                Dbg.Log("EFECTO", "Marcado: " + best.Profile.Nickname + " sabe donde estas");
            }
            catch (Exception e) { Dbg.Throttled("tsmark", 10f, "ERROR", "Tombstone (marcado): " + e.Message); }
        }

        private static void Poltergeist(Player me)
        {
            var world = Singleton<GameWorld>.Instance;
            if (world == null) return;
            int n = 0;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p.IsYourPlayer || Allies.Is(p) || (p.Position - _tomb).sqrMagnitude > PolterRadius * PolterRadius) continue;
                WidowsWine.Stun(p, PolterStun);
                n++;
            }
            ElectricVfx.Burst(_tomb, PolterRadius);
            Dbg.Log("EFECTO", "Poltergeist: la lapida estalla, " + n + " aturdido(s)");
        }

        private static void HealMostHurt(ActiveHealthController hc, float amount)
        {
            var parts = new[] { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.LeftArm, EBodyPart.RightArm, EBodyPart.LeftLeg, EBodyPart.RightLeg };
            Rev.SelfHeal = true;
            try
            {
                while (amount > 0.1f)
                {
                    EBodyPart best = EBodyPart.Common; float miss = 0.1f;
                    foreach (var p in parts)
                    {
                        if (hc.IsBodyPartDestroyed(p)) continue;
                        var v = hc.GetBodyPartHealth(p);
                        if (v.Maximum - v.Current > miss) { miss = v.Maximum - v.Current; best = p; }
                    }
                    if (best == EBodyPart.Common) break;
                    float h = Mathf.Min(amount, miss);
                    hc.ChangeHealth(best, h, DamageHelper.MedKitUse);
                    amount -= h;
                }
            }
            finally { Rev.SelfHeal = false; }
        }

        // ---------------------------------------------------------------- disparos (Haunting)
        internal static void OnShot(Player me)
        {
            if (!Specter || !Perk.Has("ts_haunt")) return;
            _hauntShot = true;
            _hauntFrame = -1;
            _hauntUntil = Time.time + 1.5f;
            Exit(me, true);
        }
        private static float _hauntUntil;

        internal static float OnDealt(float damage, DamageInfo info)
        {
            if (!_hauntShot || Time.time > _hauntUntil || (info.DamageType & (EDamageType.Bullet | EDamageType.Sniper)) == 0) return damage;
            if (_hauntFrame == -1) _hauntFrame = Time.frameCount;
            if (Time.frameCount != _hauntFrame) { _hauntShot = false; return damage; }
            return damage * 2f;
        }

        // ---------------------------------------------------------------- bajas
        internal static void OnKill(Player me, Player victim, bool boss)
        {
            if (!Active) return;
            if (boss) PerkService.AddProgress("ts_slot");
            float since = Time.time - _endedAt;
            if (since <= 10f) PerkService.AddProgress("ts_polter");
            if (since <= 3f) PerkService.AddProgress("ts_haunt");
            if (Apex.Has(Id) && !Specter && NewPerkUtil.IsPmc(victim)) { _readyAt = Time.time; }        // APEX: cada baja de PMC recarga
        }

        // ---------------------------------------------------------------- Offering: al morir
        internal static void OnDeath(Player me)
        {
            if (Specter) { Specter = false; DestroyTomb(); }
            if (!Active || !Perk.Has("ts_offering") || _offeringUsed || me == null) return;
            _offeringUsed = true;
            try
            {
                var eq = me.Profile.Inventory.Equipment;
                var roots = new List<Item>();
                foreach (var s in new[] { EquipmentSlot.Pockets, EquipmentSlot.TacticalVest })
                {
                    var c = eq.GetSlot(s).ContainedItem;
                    if (c != null) roots.Add(c);
                }
                var arr = new JArray();
                foreach (var root in roots)
                    foreach (var it in root.GetAllItems())
                    {
                        // todo lo de dentro (los cargadores llegan vacios y sus balas aparte, como municion suelta); nada de mision
                        if (ReferenceEquals(it, root) || it.QuestItem) continue;
                        arr.Add(new JObject { ["tpl"] = it.StringTemplateId, ["count"] = Math.Max(1, it.StackObjectsCount) });
                        if (arr.Count >= 40) break;
                    }
                if (arr.Count == 0) return;
                PerkService.RequestOffering(arr.ToString(Newtonsoft.Json.Formatting.None));
                Dbg.Log("EFECTO", "Ofrenda: " + arr.Count + " objeto(s) volveran por correo");
            }
            catch (Exception e) { Dbg.Throttled("tsoffer", 10f, "ERROR", "Ofrenda: " + e.Message); }
        }

        // ---------------------------------------------------------------- la lapida (senuelo visible)
        private static void MakeTomb(Vector3 pos)
        {
            DestroyTomb();
            try
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "TarkovaCola_Tombstone";
                var col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.Destroy(col);
                go.transform.position = pos + Vector3.up * 0.45f;
                go.transform.localScale = new Vector3(0.55f, 0.9f, 0.14f);
                var me = Plugin.Me;
                if (me != null) go.transform.rotation = Quaternion.Euler(0f, me.Transform.rotation.eulerAngles.y, 0f);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.45f, 0.5f, 0.45f);
                var lightGo = new GameObject("TarkovaCola_Tombstone_Light");
                lightGo.transform.SetParent(go.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Point; l.color = new Color(0.4f, 1f, 0.5f); l.range = 4f; l.intensity = 2.5f;
                _tombGo = go;
            }
            catch (Exception e) { Dbg.Throttled("tsmake", 10f, "ERROR", "Tombstone (lapida): " + e.Message); }
        }

        private static void DestroyTomb()
        {
            if (_tombGo != null) { UnityEngine.Object.Destroy(_tombGo); _tombGo = null; }
        }

        internal static void DrawGui()
        {
            if (!Specter) return;
            float left = Mathf.Clamp01((_until - Time.time) / Mathf.Max(0.1f, Duration));
            FinalFx.Vignette(new Color(0.35f, 1f, 0.5f), 0.35f + 0.15f * left);
        }

        // ---------------------------------------------------------------- bots con Tombstone: una vez por raid, bajo el 25%, se curan un 30%
        internal static void BotOnDamage(Player bot, float damage)
        {
            var hc = bot.ActiveHealthController;
            if (hc == null || _botUsed.Contains(bot.ProfileId)) return;
            var common = hc.GetBodyPartHealth(EBodyPart.Common);
            if (common.Maximum <= 0f || damage >= common.Current || (common.Current - damage) / common.Maximum >= Threshold) return;
            _botUsed.Add(bot.ProfileId);
            float heal = common.Maximum * BotHeal;
            _botHeal.Add(new KeyValuePair<Player, float>(bot, heal));
            Dbg.Log("BOTS", bot.Profile.Nickname + " (Tombstone): se cura " + heal.ToString("0"));
        }
        private static readonly List<KeyValuePair<Player, float>> _botHeal = new List<KeyValuePair<Player, float>>();

        internal static void BotTickAll()
        {
            if (_botHeal.Count == 0) return;
            var list = _botHeal.ToList();
            _botHeal.Clear();
            foreach (var kv in list)
                try { if (kv.Key != null && kv.Key.ActiveHealthController != null && kv.Key.ActiveHealthController.IsAlive) HealMostHurt(kv.Key.ActiveHealthController, kv.Value); }
                catch (Exception e) { Dbg.Throttled("tsbot", 10f, "ERROR", "Tombstone (bot): " + e.Message); }
        }
    }

    // ---------- los bots no te ven ni pueden dispararte mientras eres espectro ----------
    [HarmonyPatch(typeof(EnemyInfo), nameof(EnemyInfo.IsVisible), MethodType.Getter)]
    internal static class SpecterVisiblePatch
    {
        [HarmonyPostfix]
        private static void Postfix(EnemyInfo __instance, ref bool __result)
        {
            if (__result && Tombstone.Hidden && __instance.Person != null && __instance.Person.IsYourPlayer) __result = false;
        }
    }

    [HarmonyPatch(typeof(EnemyInfo), nameof(EnemyInfo.CanShoot), MethodType.Getter)]
    internal static class SpecterCanShootPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EnemyInfo __instance, ref bool __result)
        {
            if (__result && Tombstone.Hidden && __instance.Person != null && __instance.Person.IsYourPlayer) __result = false;
        }
    }

    // ---------- el espectro no dispara (salvo Haunting) ----------
    [HarmonyPatch(typeof(Player.FirearmController), nameof(Player.FirearmController.SetTriggerPressed))]
    internal static class SpecterTriggerPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player.FirearmController __instance, ref bool pressed)
        {
            if (!pressed || !Tombstone.Specter || Tombstone.CanShoot || !Util.IsMe(__instance._player)) return;
            pressed = false;
        }
    }
}
