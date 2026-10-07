using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using HarmonyLib;

namespace TarkovaCola.Client
{
    // Juggernog: el cuerpo aguanta mas.
    //   Base: el dano que recibes baja un 33% (= x1.5 de salud efectiva). No toca el maximo de salud (asi no queda nada raro en el
    //   perfil al terminar la raid): se reduce el dano en el momento de aplicarlo.
    internal static class Juggernog
    {
        internal const string Id = "juggernog";
        internal const string ItemTpl = "6a1c00000000000000000c03";

        internal const float DamageMult = 0.67f;      // base: dano recibido x0.67

        // Lo que NO se reduce: efectos del propio cuerpo (hambre, sed, sangrado, veneno, medicinas...), no golpes.
        private const EDamageType NotReduced = EDamageType.Medicine | EDamageType.Existence | EDamageType.Dehydration | EDamageType.Exhaustion
            | EDamageType.RadExposure | EDamageType.Stimulator | EDamageType.Poison | EDamageType.LethalToxin
            | EDamageType.LightBleeding | EDamageType.HeavyBleeding;

        internal static bool Reduces(EDamageType type) { return (type & NotReduced) == 0; }

        internal static string Notice() { return L.F("notice.juggernog.sub", ((1f - DamageMult) * 100f).ToString("0")); }
    }

    [HarmonyPatch(typeof(ActiveHealthController), "ApplyDamage")]
    internal static class JuggernogDamagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ActiveHealthController __instance, ref float damage, DamageInfo damageInfo)
        {
            if (!Perks.IsActive(Juggernog.Id) || damage <= 0f) return;
            if (!Util.IsMe(__instance.Player) || !Juggernog.Reduces(damageInfo.DamageType)) return;
            float before = damage;
            damage *= Juggernog.DamageMult;
            Dbg.Throttled("jugg", 5f, "EFECTO", "Juggernog: dano " + before.ToString("0.0") + " -> " + damage.ToString("0.0") + " (" + damageInfo.DamageType + ")");
        }
    }
}
