using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using HarmonyLib;

namespace TarkovaCola.Client
{
    // PHD Flopper: las caidas y las explosiones ya no te hacen dano.
    //   Base: inmunidad total al dano por caida y por explosiones (granadas, fragmentos, minas, artilleria, termobaricas).
    //   Con dano 0 el juego tampoco calcula sangrado ni fracturas por ese golpe.
    internal static class PhdFlopper
    {
        internal const string Id = "phdflopper";
        internal const string ItemTpl = "6a1c00000000000000000c04";

        private const EDamageType Immune = EDamageType.Fall | EDamageType.Explosion | EDamageType.GrenadeFragment | EDamageType.Landmine
            | EDamageType.Artillery | EDamageType.ThermobaricExplosion;

        internal static bool IsImmune(EDamageType type) { return (type & Immune) != 0; }

        internal static string Notice() { return L.T("notice.phdflopper.sub"); }
    }

    [HarmonyPatch(typeof(ActiveHealthController), "ApplyDamage")]
    internal static class PhdFlopperDamagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ActiveHealthController __instance, ref float damage, DamageInfo damageInfo)
        {
            if (!Perks.IsActive(PhdFlopper.Id) || damage <= 0f) return;
            if (!Util.IsMe(__instance.Player) || !PhdFlopper.IsImmune(damageInfo.DamageType)) return;
            Dbg.Throttled("phd", 5f, "EFECTO", "PHD Flopper: " + damage.ToString("0.0") + " de dano por " + damageInfo.DamageType + " anulado");
            damage = 0f;
        }
    }
}
