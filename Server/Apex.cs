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

namespace TarkovaCola.Server
{
    // Nucleo APEX: un procesador Virtex por dentro. Se encuentra en raid o se compra en el mercadillo por 5 millones; el cliente lo absorbe
    // al cogerlo y, si extraes con vida, cuenta como un nucleo para empezar un desafio APEX (Client/Apex.cs). Donde sale: Loot.cs.
    internal static class Apex
    {
        internal const string CoreId = "6a1c00000000000000a0c001";           // (antes 0c01 = el id de la Speed Cola: chocaban)
        internal const string Virtex = "5c05308086f7746b2101e90b";
        internal const int PriceRoubles = 5000000;                   // en el mercadillo, a precio de capricho
        internal const string OfferId = "6a1c00000000000000a0c002";
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 2)]
    public class TarkovaColaApex : IOnLoad
    {
        private readonly CustomItemService _items;
        private readonly TemplateTable _templates;
        private readonly TradersTable _traders;

        public TarkovaColaApex(CustomItemService items, TemplateTable templates, TradersTable traders) { _items = items; _templates = templates; _traders = traders; }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var baseItem = _templates.Items[new MongoId(Apex.Virtex)];
                _items.CreateItemFromClone(new NewItemFromCloneDetails
                {
                    ItemTplToClone = new MongoId(Apex.Virtex),
                    ParentId = baseItem.Parent,
                    NewId = new MongoId(Apex.CoreId),
                    NewItemName = "tarkovacola_apex_core",
                    OverrideProperties = new TemplateItemProperties { CanSellOnRagfair = true, BackgroundColor = "red" },
                    FleaPriceRoubles = Apex.PriceRoubles,
                    HandbookPriceRoubles = Apex.PriceRoubles,
                    HandbookParentId = "5b47574386f77428ca22b2ef",      // handbook: Electronics
                    Locales = new Dictionary<string, LocaleDetails>
                    {
                        ["en"] = new LocaleDetails { Name = "APEX Core", ShortName = "APEX",
                            Description = "A humming core that should not exist. Tarkova-Cola: carry it into a raid and it is absorbed; extract alive to keep it, then spend it in Research > APEX to start the challenge of a perk's APEX." },
                        ["es"] = new LocaleDetails { Name = "Núcleo APEX", ShortName = "APEX",
                            Description = "Un núcleo que zumba y que no debería existir. Tarkova-Cola: llévalo encima a una raid y se absorbe; extrae con vida para conservarlo y gástalo en Research > APEX para empezar el desafío del APEX de una perk." },
                    },
                });
                // a la venta en Mechanic (las ofertas de comerciante tambien salen en el buscador del mercadillo; las del mercadillo de SPT
                // no siempre generan objetos nuevos)
                var assort = _traders[Traders.MECHANIC].Assort;
                var offer = new MongoId(Apex.OfferId);
                assort.Items.Add(new Item { Id = offer, Template = new MongoId(Apex.CoreId), ParentId = "hideout", SlotId = "hideout", Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true } });
                assort.BarterScheme[offer] = new List<List<BarterScheme>> { new List<BarterScheme> { new BarterScheme { Count = Apex.PriceRoubles, Template = new MongoId("5449016a4bdc2d6f028b456f") } } };
                assort.LoyalLevelItems[offer] = 1;
                Console.WriteLine("[Tarkova-Cola] Nucleo APEX creado (Mechanic, " + Apex.PriceRoubles.ToString("N0") + " rublos)");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando el nucleo APEX: " + e); }
            return Task.CompletedTask;
        }
    }
}
