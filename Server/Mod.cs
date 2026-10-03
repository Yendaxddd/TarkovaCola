using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;
using SptRange = SemanticVersioning.Range;
using SptVersion = SemanticVersioning.Version;

namespace TarkovaCola.Server
{
    public class ModMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "com.tarkovacola.server";
        public string Name { get; init; } = "Tarkova-Cola";
        public string Author { get; init; } = "Yendaxddd";
        public List<string> Contributors { get; init; } = new List<string>();
        public SptVersion Version { get; init; } = new SptVersion(1, 1, 1);
        public SptRange SptVersion { get; init; } = new SptRange("~4.1.0");
        public bool HasPrepatcher { get; init; } = false;
        public List<string> Incompatibilities { get; init; } = new List<string>();
        public Dictionary<string, SptRange> ModDependencies { get; init; } = new Dictionary<string, SptRange>();
        public string Url { get; init; } = "";
        public string License { get; init; } = "MIT";
    }

    // Definicion de perks (datos). El cliente tiene su propia lista con los mismos ids (Client/PerkDefs.cs).
    internal class PerkDef
    {
        public string Id;                 // clave de estado (la misma que usa el cliente)
        public string ItemId;             // id fijo del item
        public string TraderItemId;       // id de la oferta en Therapist
        public string ItemName;           // nombre interno del item
        public string NameEn, NameEs, ShortName;
        public string DescEn, DescEs;
        public int GpPrice;               // precio en GP Coin
        public string Bundle;             // ruta del bundle del item (suelo/inventario/icono)
        // probabilidad aproximada de que UN objeto sacado de cada sitio sea esta perk (0 = no aparece ahi)
        public double AmmoCrate, Safe, Rogue, Killa;
    }

    internal static class Perks
    {
        internal const string TarCola = "57514643245977207f2c2d09";     // lata base que se clona
        internal const string GpCoin = "5d235b4d86f7742e017bc88a";      // GP Coin
        internal const double GpCoinRoubles = 7500;                     // valor de 1 GP Coin en el handbook

        // acceso directo a Speed Cola (lo usan los logros)
        internal const string SpeedColaId = "6a1c00000000000000000c01";

        internal static readonly PerkDef SpeedCola = new PerkDef
        {
            Id = "speedcola",
            ItemId = SpeedColaId,
            TraderItemId = "6a1c00000000000000000d01",
            ItemName = "tarkovacola_speedcola",
            NameEn = "Speed Cola", NameEs = "Speed Cola", ShortName = "Speed",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/speedcola.bundle",
            DescEn =
                "Perk-a-cola: A combined taste of some combination of sweet and spicy but doesn't contain any sugar... " +
                "Somehow, drinking this seems to make your body all energetic, and gives you the edge of an ADHD kid with " +
                "unsupervised screen time. Drink it! See what is the worst that could happen!\n\n" +
                "Reload speed x1.5 for the rest of the raid.",
            DescEs =
                "Perk-a-cola: Una mezcla de sabor dulce y picante que no contiene nada de azucar... " +
                "De alguna forma, beberla hace que tu cuerpo se llene de energia y te da la ventaja de un nino con TDAH " +
                "y pantallas sin supervision. Bebela! A ver que es lo peor que puede pasar!\n\n" +
                "Velocidad de recarga x1.5 el resto de la raid.",
            AmmoCrate = 0.012, Safe = 0.06, Rogue = 0.04, Killa = 0.15,
        };

        internal static readonly PerkDef[] All = { SpeedCola };
    }

    // Fase 1: crea el item Speed Cola (clon de TarCola) y lo vende en Therapist (LL1).
    [Injectable(TypePriority = OnLoadOrder.Preload)]
    public class TarkovaColaMod : IOnLoad
    {
        private readonly CustomItemService _items;
        private readonly TemplateTable _templates;
        private readonly TradersTable _traders;

        public TarkovaColaMod(CustomItemService items, TemplateTable templates, TradersTable traders)
        {
            _items = items;
            _templates = templates;
            _traders = traders;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            foreach (var perk in Perks.All)
            {
                try { CreatePerk(perk); }
                catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando " + perk.Id + ": " + e); }
            }
            return Task.CompletedTask;
        }

        // Crea el item de una perk (clon de TarCola) y lo pone en venta en Therapist (LL1).
        private void CreatePerk(PerkDef perk)
        {
            var baseItem = _templates.Items[new MongoId(Perks.TarCola)];
            double roubles = perk.GpPrice * Perks.GpCoinRoubles;
            _items.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = new MongoId(Perks.TarCola),
                ParentId = baseItem.Parent,
                NewId = new MongoId(perk.ItemId),
                NewItemName = perk.ItemName,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,      // ocupa 1x2 casillas en el inventario (la TarCola original es 1x1)
                    EffectsHealth = BuildEffects(baseItem.Properties.EffectsHealth, hydration: 50, energy: -15),
                    // Modelo 3D propio (ground/inventario/icono). UsePrefab (en mano) sigue siendo el de la TarCola.
                    Prefab = new Prefab { Path = perk.Bundle, Rcid = "" },
                },
                FleaPriceRoubles = roubles,
                HandbookPriceRoubles = roubles,
                HandbookParentId = "5b47574386f77428ca22b335", // handbook: Drinks
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails { Name = perk.NameEn, ShortName = perk.ShortName, Description = perk.DescEn },
                    ["es"] = new LocaleDetails { Name = perk.NameEs, ShortName = perk.ShortName, Description = perk.DescEs },
                },
            });

            var created = _templates.Items[new MongoId(perk.ItemId)].Properties;
            Console.WriteLine($"[Tarkova-Cola] {perk.Id}: prefab={created?.Prefab?.Path} peso={created?.Weight}");
            AddToTherapist(perk);
            Console.WriteLine($"[Tarkova-Cola] {perk.NameEn} creada y en venta en Therapist (LL1, {perk.GpPrice} GP Coin)");
        }

        // Copia los efectos de la lata base y sustituye solo el valor de hidratacion y energia.
        private static Dictionary<HealthFactor, EffectsHealthProperties> BuildEffects(
            Dictionary<HealthFactor, EffectsHealthProperties> baseEffects, double hydration, double energy)
        {
            var result = new Dictionary<HealthFactor, EffectsHealthProperties>();
            if (baseEffects != null)
                foreach (var kv in baseEffects)
                    result[kv.Key] = kv.Value with { };
            result[HealthFactor.Hydration] = (result.TryGetValue(HealthFactor.Hydration, out var h) ? h : new EffectsHealthProperties()) with { Value = hydration };
            result[HealthFactor.Energy] = (result.TryGetValue(HealthFactor.Energy, out var e) ? e : new EffectsHealthProperties()) with { Value = energy };
            return result;
        }

        private void AddToTherapist(PerkDef perk)
        {
            var assort = _traders[Traders.THERAPIST].Assort;
            var id = new MongoId(perk.TraderItemId);
            assort.Items.Add(new Item
            {
                Id = id,
                Template = new MongoId(perk.ItemId),
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true },
            });
            assort.BarterScheme[id] = new List<List<BarterScheme>>
            {
                new List<BarterScheme> { new BarterScheme { Count = perk.GpPrice, Template = new MongoId(Perks.GpCoin) } },
            };
            assort.LoyalLevelItems[id] = 1;
        }
    }
}
