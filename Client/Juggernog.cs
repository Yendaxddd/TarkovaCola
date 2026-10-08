using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Juggernog: el cuerpo aguanta mas.
    //   Base: el dano que recibes baja un 33% (= x1.5 de salud efectiva). No toca el maximo de salud (asi no queda nada raro en el
    //   perfil al terminar la raid): se reduce el dano en el momento de aplicarlo.
    //   Mejoras: Quick Patch, Bone Plating, Last Bastion, Skull Cap, Thick Skin, Heavy Bones, Keep Going, Extra Slot.
    //   Desventajas ligadas: Clanky Plates, Heavy Plates, Heavy Hands, Dead Weight, Sluggish Reflexes, Slow Metabolism, Slow Chewer.
    // Aqui estan los efectos que dependen del dano recibido; el resto (peso, balanceo, ruido, animaciones...) esta en PerkFx.
    internal static class Juggernog
    {
        internal const string Id = "juggernog";
        internal const string ItemTpl = "6a1c00000000000000000c03";

        // --- balance ---
        internal const float DamageMult = 0.67f;          // base: dano recibido x0.67
        internal const float SkullChance = 0.15f;         // Skull Cap: 15% de que la bala rebote en el casco
        internal const float ThickBleed = 0.5f;           // Thick Skin: el sangrado quita la mitad...
        internal const float ThickFracture = 0.20f;       //   ...pero cada golpe en un miembro tiene un 20% extra de fracturar
        internal const float BonesFractureBlock = 0.5f;   // Heavy Bones: la mitad de las fracturas no ocurren...
        internal const float BonesBleed = 0.20f;          //   ...pero cada golpe tiene un 20% extra de hacer sangrar
        internal const float KeepGoing = 0.35f;           // Keep Going: hambre y sed hacen un 65% menos de dano
        internal const float PlatesDurability = 0.5f;     // Bone Plating: la armadura se desgasta la mitad

        // Lo que NO se reduce: efectos del propio cuerpo (hambre, sed, sangrado, veneno, medicinas...), no golpes.
        private const EDamageType NotReduced = EDamageType.Medicine | EDamageType.Existence | EDamageType.Dehydration | EDamageType.Exhaustion
            | EDamageType.RadExposure | EDamageType.Stimulator | EDamageType.Poison | EDamageType.LethalToxin
            | EDamageType.LightBleeding | EDamageType.HeavyBleeding;

        private static bool _bastionUsed;
        private static float _dmgAcc;

        internal static void OnRaidStart() { _bastionUsed = false; _dmgAcc = 0f; }

        internal static bool Reduces(EDamageType type) { return (type & NotReduced) == 0; }

        internal static string Notice() { return L.F("notice.juggernog.sub", ((1f - DamageMult) * 100f).ToString("0")); }

        private static bool HasHelmet()
        {
            var me = Plugin.Me;
            if (me == null || me.Equipment == null) return false;
            var slot = me.Equipment.GetSlot(EquipmentSlot.Headwear);
            return slot != null && slot.ContainedItem != null;
        }

        // Se llama desde DamageHubPatch (antes de aplicar el dano al jugador, con Juggernog activa). Devuelve el dano final.
        internal static float OnDamage(ActiveHealthController hc, EBodyPart part, float damage, DamageInfo info)
        {
            var t = info.DamageType;
            bool hit = (t & PerkFx.Hits) != 0;
            if (hit) PerkService.AddProgress("jg_thick");                       // desafio de Thick Skin: golpes recibidos

            float before = damage;
            if (Reduces(t)) damage *= DamageMult;
            else if ((t & PerkFx.Bleeds) != 0) { if (Perk.Has("jg_thick")) damage *= ThickBleed; }
            else if ((t & PerkFx.Hunger) != 0) { if (Perk.Has("jg_keep")) damage *= KeepGoing; }

            // Skull Cap: una bala a la cabeza con casco puesto puede rebotar
            if (part == EBodyPart.Head && (t & (EDamageType.Bullet | EDamageType.Sniper)) != 0 && Perk.Has("jg_skull")
                && HasHelmet() && UnityEngine.Random.value < SkullChance)
            {
                Dbg.Log("EFECTO", "Skull Cap: la bala rebota en el casco (" + damage.ToString("0.0") + " de dano evitado)");
                Hud.Notify(Data.Name("jg_skull"), L.T("notice.skull.sub"), Id);
                return 0f;
            }

            // Last Bastion: una vez por raid, un golpe que mataria por la cabeza o el torso te deja a 1 PV
            if (hit && !_bastionUsed && (part == EBodyPart.Head || part == EBodyPart.Chest) && Perk.Has("jg_bastion"))
            {
                float lim = Mathf.Min(hc.GetBodyPartHealth(part).Current, hc.GetBodyPartHealth(EBodyPart.Common).Current);
                if (damage >= lim)
                {
                    _bastionUsed = true;
                    Dbg.Log("EFECTO", "Last Bastion: golpe letal (" + damage.ToString("0.0") + " >= " + lim.ToString("0.0") + ") -> sobrevives con 1 PV");
                    Hud.Notify(Data.Name("jg_bastion"), L.T("notice.bastion.sub"), Id);
                    damage = Mathf.Max(0f, lim - 1f);
                }
            }

            // desafio de Skull Cap: dano de golpes recibido (en enteros)
            if (hit && damage > 0f)
            {
                _dmgAcc += damage;
                int whole = (int)_dmgAcc;
                if (whole > 0) { _dmgAcc -= whole; PerkService.AddProgress("jg_skull", whole); }
            }

            if (damage != before) Dbg.Throttled("jugg", 5f, "EFECTO", "Juggernog: dano " + before.ToString("0.0") + " -> " + damage.ToString("0.0") + " (" + t + ")");
            return damage;
        }

        // Despues de aplicar el dano: Thick Skin rompe huesos con mas facilidad; Heavy Bones hace sangrar con mas facilidad.
        internal static void AfterDamage(ActiveHealthController hc, EBodyPart part, float damage, DamageInfo info)
        {
            if (damage < 3f || (info.DamageType & PerkFx.Hits) == 0) return;
            bool limb = part == EBodyPart.LeftArm || part == EBodyPart.RightArm || part == EBodyPart.LeftLeg || part == EBodyPart.RightLeg;
            if (limb && Perk.Has("jg_thick") && !hc.IsBodyPartBroken(part) && UnityEngine.Random.value < ThickFracture)
            {
                Dbg.Log("EFECTO", "Thick Skin: " + part + " se fractura");
                hc.DoFracture(part);
            }
            if (Perk.Has("jg_bones") && UnityEngine.Random.value < BonesBleed)
            {
                Dbg.Log("EFECTO", "Heavy Bones: " + part + " sangra");
                hc.DoBleed(false, part);
            }
        }

        // Comer, beber o curarse con otra cosa que no sea una perk (desafios de Keep Going y Quick Patch)
        internal static void OnConsumed(Item item)
        {
            if (!Perks.IsActive(Id) || item == null) return;
            if (item is FoodDrink) PerkService.AddProgress("jg_keep");
            else if (item is Meds && !(item is Stimulator)) PerkService.AddProgress("jg_patch");
        }
    }
}
