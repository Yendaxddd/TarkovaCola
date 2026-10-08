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

        private const EDamageType Immune = EDamageType.Fall | PerkFx.Blast;

        private static readonly EBodyPart[] Parts =
            { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.LeftArm, EBodyPart.RightArm, EBodyPart.LeftLeg, EBodyPart.RightLeg };

        private static float _acc;

        internal static void OnRaidStart() { _acc = 0f; }

        internal static bool IsImmune(EDamageType type) { return (type & Immune) != 0; }

        internal static string Notice() { return L.T("notice.phdflopper.sub"); }

        // Dano que recibe el jugador (desde DamageHubPatch): caidas y explosiones se anulan; Terrain Walker anula el alambre de puas.
        internal static float OnDamage(EBodyPart part, float damage, DamageInfo info)
        {
            var t = info.DamageType;
            if (IsImmune(t))
            {
                Dbg.Throttled("phd", 5f, "EFECTO", "PHD Flopper: " + damage.ToString("0.0") + " de dano por " + t + " anulado");
                var me = Plugin.Me;
                if (me != null) PhdVfx.Play(me.Position, (t & EDamageType.Fall) != 0 ? Mathf.Clamp01(damage / 40f) : 1f);   // efecto visual (caidas pequenas = efecto pequeno)
                return 0f;
            }
            if (t == EDamageType.Barbed && Perk.Has("pf_terrain"))
            {
                Dbg.Throttled("phdwire", 5f, "EFECTO", "Terrain Walker: alambre de puas ignorado");
                return 0f;
            }
            return damage;
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
