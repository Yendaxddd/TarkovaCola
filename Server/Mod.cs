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
        public SptVersion Version { get; init; } = new SptVersion(1, 1, 0);
        public SptRange SptVersion { get; init; } = new SptRange("~4.1.0");
        public bool HasPrepatcher { get; init; } = false;
        public List<string> Incompatibilities { get; init; } = new List<string>();
        public Dictionary<string, SptRange> ModDependencies { get; init; } = new Dictionary<string, SptRange>();
        public string Url { get; init; } = "";
        public string License { get; init; } = "MIT";
    }

    // Definicion de perks. El cliente tiene la misma lista de ids (Client/Plugin.cs).
    internal static class Perks
    {
        internal const string TarCola = "57514643245977207f2c2d09";     // lata base que se clona
        internal const string SpeedColaId = "6a1c00000000000000000c01"; // id fijo de Speed Cola
        internal const string SpeedColaTraderItemId = "6a1c00000000000000000d01";
        internal const string GpCoin = "5d235b4d86f7742e017bc88a";   // GP Coin
        internal const int SpeedColaGpPrice = 7;                       // precio en Therapist: 7 GP Coins
        internal const double SpeedColaPrice = 52500;                  // equivalente en rublos (7 x 7500), para flea/handbook

        internal const string SpeedColaDescEn =
            "Perk-a-cola: A combined taste of some combination of sweet and spicy but doesn't contain any sugar... " +
            "Somehow, drinking this seems to make your body all energetic, and gives you the edge of an ADHD kid with " +
            "unsupervised screen time. Drink it! See what is the worst that could happen!\n\n" +
            "Reload speed x1.5 for the rest of the raid.";

        internal const string SpeedColaDescEs =
            "Perk-a-cola: Una mezcla de sabor dulce y picante que no contiene nada de azucar... " +
            "De alguna forma, beberla hace que tu cuerpo se llene de energia y te da la ventaja de un nino con TDAH " +
            "y pantallas sin supervision. Bebela! A ver que es lo peor que puede pasar!\n\n" +
            "Velocidad de recarga x1.5 el resto de la raid.";
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
            try
            {
                var baseItem = _templates.Items[new MongoId(Perks.TarCola)];
                var result = _items.CreateItemFromClone(new NewItemFromCloneDetails
                {
                    ItemTplToClone = new MongoId(Perks.TarCola),
                    ParentId = baseItem.Parent,
                    NewId = new MongoId(Perks.SpeedColaId),
                    NewItemName = "tarkovacola_speedcola",
                    OverrideProperties = new TemplateItemProperties
                    {
                        Width = 1,
                        Height = 2,      // ocupa 1x2 casillas en el inventario (la TarCola original es 1x1)
                        EffectsHealth = BuildEffects(baseItem.Properties.EffectsHealth, hydration: 50, energy: -15),
                        // Modelo 3D propio (ground/inventario/icono). UsePrefab (en mano) sigue siendo el de la TarCola.
                        Prefab = new Prefab { Path = "assets/content/items/consumables/tarkovacola/speedcola.bundle", Rcid = "" },
                    },
                    FleaPriceRoubles = Perks.SpeedColaPrice,
                    HandbookPriceRoubles = Perks.SpeedColaPrice,
                    HandbookParentId = "5b47574386f77428ca22b335", // handbook: Drinks
                    Locales = new Dictionary<string, LocaleDetails>
                    {
                        ["en"] = new LocaleDetails { Name = "Speed Cola", ShortName = "Speed", Description = Perks.SpeedColaDescEn },
                        ["es"] = new LocaleDetails { Name = "Speed Cola", ShortName = "Speed", Description = Perks.SpeedColaDescEs },
                    },
                });

                var created = _templates.Items[new MongoId(Perks.SpeedColaId)].Properties;
                Console.WriteLine($"[Tarkova-Cola] prefab={created?.Prefab?.Path} usePrefab={created?.UsePrefab?.Path} peso={created?.Weight}");

                AddToTherapist();
                Console.WriteLine("[Tarkova-Cola] Speed Cola creada y en venta en Therapist (LL1, " + Perks.SpeedColaGpPrice + " GP Coin)");
            }
            catch (Exception e)
            {
                Console.WriteLine("[Tarkova-Cola] ERROR creando items: " + e);
            }
            return Task.CompletedTask;
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

        private void AddToTherapist()
        {
            var assort = _traders[Traders.THERAPIST].Assort;
            var id = new MongoId(Perks.SpeedColaTraderItemId);
            assort.Items.Add(new Item
            {
                Id = id,
                Template = new MongoId(Perks.SpeedColaId),
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true },
            });
            assort.BarterScheme[id] = new List<List<BarterScheme>>
            {
                new List<BarterScheme> { new BarterScheme { Count = Perks.SpeedColaGpPrice, Template = new MongoId(Perks.GpCoin) } },
            };
            assort.LoyalLevelItems[id] = 1;
        }
    }
}
