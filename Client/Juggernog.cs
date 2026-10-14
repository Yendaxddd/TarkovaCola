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
        internal const int RudeDamage = 4000;             // logro "Rude suggestions": dano total recibido
        internal const int PincushionHits = 25;           // logro "Pincushion": impactos en una raid (y extraer con vida)

        // Lo que NO se reduce: efectos del propio cuerpo (hambre, sed, sangrado, veneno, medicinas...), no golpes.
        private const EDamageType NotReduced = EDamageType.Medicine | EDamageType.Existence | EDamageType.Dehydration | EDamageType.Exhaustion
            | EDamageType.RadExposure | EDamageType.Stimulator | EDamageType.Poison | EDamageType.LethalToxin
            | EDamageType.LightBleeding | EDamageType.HeavyBleeding;

        private static bool _bastionUsed, _red;
        private static float _dmgAcc;
        private static int _raidHits;

        internal static void OnRaidStart() { _bastionUsed = false; _red = false; _dmgAcc = 0f; _raidHits = 0; _polled = false; }

        // Desafios de Heavy Bones (arreglar fracturas) y Thick Skin (arreglar sangrados pesados): se vigila cada segundo que una fractura
        // o un sangrado pesado DESAPAREZCA de una parte del cuerpo que sigue en pie (si se pierde la parte, no cuenta como arreglo).
        private static readonly EBodyPart[] Parts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.LeftArm, EBodyPart.RightArm, EBodyPart.LeftLeg, EBodyPart.RightLeg };
        private static readonly bool[] _wasBroken = new bool[7], _wasHeavy = new bool[7];
        private static bool _polled;

        internal static void PollHealing(IHealthController hc)
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                var part = Parts[i];
                bool destroyed = hc.IsBodyPartDestroyed(part);
                bool broken = hc.IsBodyPartBroken(part);
                bool heavy = false;
                foreach (var e in hc.GetAllActiveEffects(part))
                    if (e.Type != null && e.Type.Name.IndexOf("HeavyBleeding", System.StringComparison.OrdinalIgnoreCase) >= 0) { heavy = true; break; }
                if (_polled && !destroyed)
                {
                    if (_wasBroken[i] && !broken) { Dbg.Log("DESAFIO", "fractura arreglada: " + part); PerkService.AddProgress("jg_bones"); }
                    if (_wasHeavy[i] && !heavy) { Dbg.Log("DESAFIO", "sangrado pesado arreglado: " + part); PerkService.AddProgress("jg_thick"); }
                }
                _wasBroken[i] = broken; _wasHeavy[i] = heavy;
            }
            _polled = true;
        }

        internal static bool Reduces(EDamageType type) { return (type & NotReduced) == 0; }

        internal static string Notice() { return Effect(Perks.Power(Id)); }

        internal static string Effect(float power) { return L.F("notice.juggernog.sub", ((1f - DamageMult) * 100f * power).ToString("0")); }

        internal static void OnDrink(ActiveHealthController health)
        {
            Achievements.Unlock("jg_fresh");
        }

        // La salud esta en rojo ahora mismo (lo actualiza PerkFx cada segundo): para el logro de extraer con vida en rojo
        internal static void SetRed(bool red) { _red = red; }

        // Fin de raid (desde PerkFx.OnRaidEnd): desafio de Last Bastion y logros que piden extraer con vida
        internal static void OnRaidEnd(bool alive)
        {
            if (!alive || !Perks.IsActive(Id)) return;
            PerkService.AddProgress("jg_bastion");
            if (_red) Achievements.Unlock("jg_red");
            if (_raidHits >= PincushionHits) Achievements.Unlock("jg_secret");
        }

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

            float before = damage;
            if (Reduces(t)) damage *= 1f - (1f - DamageMult) * Perks.Power(Id);      // como modificador (Classic) reduce solo una parte
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

            // desafio de Skull Cap y logros: dano de golpes recibido (en enteros) e impactos de la raid
            if (hit && damage > 0f)
            {
                _raidHits++;
                _dmgAcc += damage;
                int whole = (int)_dmgAcc;
                if (whole > 0)
                {
                    _dmgAcc -= whole;
                    PerkService.AddProgress("jg_skull", whole);
                    PerkService.AddStat("juggernog_dmg", whole);
                    if (PerkService.Stat("juggernog_dmg") >= RudeDamage) Achievements.Unlock("jg_rude");
                }
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
