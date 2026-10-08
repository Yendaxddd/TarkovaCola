using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using UnityEngine;

namespace TarkovaCola.Client
{
    // PHD Flopper: las caidas y las explosiones ya no te hacen dano.
    //   Base: inmunidad total al dano por caida y por explosiones (granadas, fragmentos, minas, artilleria, termobaricas).
    //   Con dano 0 el juego tampoco calcula sangrado ni fracturas por ese golpe.
    //   Mejoras: Blast Absorber, Shockwave, Soft Soles, Pit Stop, Ear Plugs, Terrain Walker, Extra Slot.
    //   Desventajas ligadas: Concussed, Heavy Throw, Stagger, Lazy Legs, Heavy Pockets, Hard Stomp.
    // Aqui estan los efectos que dependen del dano y de las explosiones; el resto (movimiento, peso, estamina...) esta en PerkFx.
    internal static class PhdFlopper
    {
        internal const string Id = "phdflopper";
        internal const string ItemTpl = "6a1c00000000000000000c04";

        // --- balance ---
        internal const float AbsorbRatio = 0.25f;     // Blast Absorber: curas el 25% del dano de explosion que causas
        internal const float ShockRadius = 2f;        // Shockwave: doble radio...
        internal const float ShockFuse = 0.5f;        //   ...y la mitad de mecha
        internal const float StaggerHeight = 3f;      // Stagger: caidas de 3 m o mas...
        internal const float StaggerCost = 40f;       //   ...quitan 40 de estamina
        internal const float ThrowCost = 25f;         // Heavy Throw: lanzar una granada quita 25 de estamina

        // logros
        internal const float MinFall = 2f;            // caidas de menos de 2 m no cuentan para nada
        internal const float FlopHeight = 5f;         // "Flop!": aterrizar una caida de 5 m o mas
        internal const int GravityMeters = 200;       // "Gravity is a suggestion": metros caidos en total
        internal const int HugsNeeded = 10;           // "Very loud hugs": explosiones ignoradas
        private const float BlastGap = 1.2f;          // varios impactos de una misma explosion llegan seguidos: cuentan como una
        private const float SingleGrenade = 1.5f;     // "Explosive personality": 3 bajas con granada en menos de 1.5 s = una sola granada

        private const EDamageType Immune = EDamageType.Fall | PerkFx.Blast;

        private static readonly EBodyPart[] Parts =
            { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.LeftArm, EBodyPart.RightArm, EBodyPart.LeftLeg, EBodyPart.RightLeg };

        private static float _acc, _lastBlast = -99f;
        private static readonly System.Collections.Generic.List<float> _grenadeKills = new System.Collections.Generic.List<float>();

        internal static void OnRaidStart() { _acc = 0f; _lastBlast = -99f; _grenadeKills.Clear(); }

        internal static bool IsImmune(EDamageType type) { return (type & Immune) != 0; }

        internal static string Notice() { return Effect(Perks.Power(Id)); }

        internal static string Effect(float power)
        {
            return power >= 1f ? L.T("notice.phdflopper.sub") : L.F("notice.phdflopper.mod", (power * 100f).ToString("0"));
        }

        // Dano que recibe el jugador (desde DamageHubPatch): caidas y explosiones se anulan; Terrain Walker anula el alambre de puas.
        internal static float OnDamage(EBodyPart part, float damage, DamageInfo info)
        {
            var t = info.DamageType;
            if (IsImmune(t))
            {
                float p = Perks.Power(Id);                                   // 1 = inmunidad total; como modificador (Classic) solo una reduccion
                if (p < 1f)
                {
                    Dbg.Throttled("phd", 5f, "EFECTO", "PHD Flopper (modificador): " + damage.ToString("0.0") + " de dano por " + t + " -> " + (damage * (1f - p)).ToString("0.0"));
                    return damage * (1f - p);
                }
                Dbg.Throttled("phd", 5f, "EFECTO", "PHD Flopper: " + damage.ToString("0.0") + " de dano por " + t + " anulado");
                var me = Plugin.Me;
                if (me != null) PhdVfx.Play(me.Position, (t & EDamageType.Fall) != 0 ? Mathf.Clamp01(damage / 40f) : 1f);   // efecto visual (caidas pequenas = efecto pequeno)
                if ((t & PerkFx.Blast) != 0) OnBlast();
                return 0f;
            }
            if (t == EDamageType.Barbed && Perk.Has("pf_terrain"))
            {
                Dbg.Throttled("phdwire", 5f, "EFECTO", "Terrain Walker: alambre de puas ignorado");
                return 0f;
            }
            return damage;
        }

        // Una explosion que te habria hecho dano y la perk anula (logro "Very loud hugs")
        private static void OnBlast()
        {
            float now = Time.time;
            bool fresh = now - _lastBlast > BlastGap;
            _lastBlast = now;
            if (!fresh) return;
            PerkService.AddStat("phd_blasts", 1);
            if (PerkService.Stat("phd_blasts") >= HugsNeeded) Achievements.Unlock("pf_hugs");
        }

        // Aterrizaje (desde FallPatch, con la perk activa y una caida de MinFall m o mas): logros de caidas y Stagger.
        internal static void OnFall(Player me, float height)
        {
            if (height >= FlopHeight) Achievements.Unlock("pf_flop");
            PerkService.AddStat("phd_fall_m", Mathf.RoundToInt(height));
            if (PerkService.Stat("phd_fall_m") >= GravityMeters) Achievements.Unlock("pf_gravity");

            // Stagger: aterrizar de 3 m o mas te quita estamina
            if (height >= StaggerHeight && Perk.Has("dp_stagger") && me.Physical != null && me.Physical.Stamina != null)
            {
                me.Physical.Stamina.Current = Mathf.Max(0f, me.Physical.Stamina.Current - StaggerCost);
                Dbg.Log("EFECTO", "Stagger: caida de " + height.ToString("0.0") + " m, -" + StaggerCost + " de estamina");
            }
        }

        // Baja del jugador con una granada (logro "Explosive personality": 3 con una sola granada)
        internal static void OnGrenadeKill()
        {
            float now = Time.time;
            _grenadeKills.Add(now);
            _grenadeKills.RemoveAll(t => now - t > SingleGrenade);
            if (_grenadeKills.Count >= 3) Achievements.Unlock("pf_secret");
        }

        // Dano de explosion que CAUSA el jugador a otros (desde DamageHubPatch): desafio y curacion de Blast Absorber.
        internal static void OnDealt(float damage, DamageInfo info)
        {
            if ((info.DamageType & PerkFx.Blast) == 0) return;
            _acc += damage;
            int whole = (int)_acc;
            if (whole > 0) { _acc -= whole; PerkService.AddProgress("pf_absorb", whole); }
            if (Perk.Has("pf_absorb")) Heal(damage * AbsorbRatio);
        }

        // Cura la parte del cuerpo a la que mas vida le falta
        private static void Heal(float amount)
        {
            var me = Plugin.Me;
            var hc = me != null ? me.ActiveHealthController : null;
            if (hc == null || amount < 0.01f) return;
            EBodyPart best = EBodyPart.Common;
            float bestMissing = 0.5f;
            foreach (var p in Parts)
            {
                if (hc.IsBodyPartDestroyed(p)) continue;
                var v = hc.GetBodyPartHealth(p);
                float missing = v.Maximum - v.Current;
                if (missing > bestMissing) { bestMissing = missing; best = p; }
            }
            if (best == EBodyPart.Common) return;
            hc.ChangeHealth(best, Mathf.Min(amount, bestMissing), DamageHelper.MedKitUse);
            Dbg.Throttled("absorb", 3f, "EFECTO", "Blast Absorber: +" + Mathf.Min(amount, bestMissing).ToString("0.0") + " en " + best);
        }

        // Concussed: tras una explosion cercana (de quien sea), el arma se balancea mas un rato
        internal static void OnExplosionNear(Vector3 pos)
        {
            if (!Perks.IsActive(Id) || !Perk.Has("dp_concussed")) return;
            var me = Plugin.Me;
            if (me == null || (me.Position - pos).sqrMagnitude > PerkFx.ConcussRadius * PerkFx.ConcussRadius) return;
            PerkFx.ConcussUntil = Time.time + PerkFx.ConcussSeconds;
            Dbg.Log("EFECTO", "Concussed: explosion a " + Vector3.Distance(me.Position, pos).ToString("0.0") + " m, balanceo +40% durante " + PerkFx.ConcussSeconds + " s");
            Hud.Notify(Data.Name("dp_concussed"), L.T("notice.concussed.sub"), Id);
        }
    }
}
