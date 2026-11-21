using System;
using System.Collections.Generic;
using System.Linq;
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
    // Caja de perks: un estuche (copia del estuche de inyectores, con su modelo) de 2x2 por fuera y 5x4 por dentro donde solo caben
    // las botellas / inyectores de perk y el nucleo APEX. Therapist (nivel 1) y mercadillo.
    [Injectable(TypePriority = OnLoadOrder.Preload + 3)]
    public class TarkovaColaPerkCase : IOnLoad
    {
        internal const string ItemId = "6a1c00000000000000ca5e01";
        internal const string OfferId = "6a1c00000000000000ca5e02";
        private const string InjectorCase = "619cbf7d23893217ec30b689";
        private const int Price = 65000;

        private readonly CustomItemService _items;
        private readonly TemplateTable _templates;
        private readonly TradersTable _traders;

        public TarkovaColaPerkCase(CustomItemService items, TemplateTable templates, TradersTable traders)
        {
            _items = items; _templates = templates; _traders = traders;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var baseItem = _templates.Items[new MongoId(InjectorCase)];
                _items.CreateItemFromClone(new NewItemFromCloneDetails
                {
                    ItemTplToClone = new MongoId(InjectorCase),
                    ParentId = baseItem.Parent,
                    NewId = new MongoId(ItemId),
                    NewItemName = "tarkovacola_perk_case",
                    OverrideProperties = new TemplateItemProperties { Width = 2, Height = 2, CanSellOnRagfair = true },
                    FleaPriceRoubles = Price,
                    HandbookPriceRoubles = Price,
                    HandbookParentId = "5b5f6fa186f77409407a7eb7",          // handbook: contenedores
                    Locales = new Dictionary<string, LocaleDetails>
                    {
                        ["en"] = new LocaleDetails { Name = "Perk-a-Cola case", ShortName = "Perks",
                            Description = "A padded case for Perk-a-Cola bottles and injectors. Keeps them cold, sorted and in one piece. Only perks (and the APEX core) fit inside." },
                        ["es"] = new LocaleDetails { Name = "Caja de Perk-a-Cola", ShortName = "Perks",
                            Description = "Un estuche acolchado para botellas e inyectores de Perk-a-Cola. Las mantiene frías, ordenadas y enteras. Solo caben perks (y el núcleo APEX)." },
                    },
                });

                // por dentro: 5 x 4 y solo perks
                var allowed = new HashSet<MongoId>(Perks.All.Select(p => new MongoId(p.ItemId)));
                allowed.Add(new MongoId(Apex.CoreId));
                var tpl = _templates.Items[new MongoId(ItemId)];
                var grids = tpl.Properties.Grids?.ToList();
                if (grids != null && grids.Count > 0)
                {
                    var g = grids[0];
                    g.Properties.CellsH = 5;
                    g.Properties.CellsV = 4;
                    g.Properties.Filters = new List<GridFilter> { new GridFilter { Filter = allowed, ExcludedFilter = new HashSet<MongoId>() } };
                    tpl.Properties.Grids = new List<Grid> { g };
                }

                // se puede meter donde se mete el estuche de inyectores (contenedor seguro, mochilas con filtro...)
                int places = 0;
                foreach (var item in _templates.Items.Values)
                {
                    if (item.Properties?.Grids == null) continue;
                    foreach (var grid in item.Properties.Grids)
                        foreach (var f in grid.Properties?.Filters ?? Enumerable.Empty<GridFilter>())
                            if (f.Filter != null && f.Filter.Contains(new MongoId(InjectorCase)) && f.Filter.Add(new MongoId(ItemId))) places++;
                }

                var assort = _traders[Traders.THERAPIST].Assort;
                var offer = new MongoId(OfferId);
                assort.Items.Add(new Item { Id = offer, Template = new MongoId(ItemId), ParentId = "hideout", SlotId = "hideout",
                    Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true } });
                assort.BarterScheme[offer] = new List<List<BarterScheme>> { new List<BarterScheme> { new BarterScheme { Count = Price, Template = new MongoId("5449016a4bdc2d6f028b456f") } } };
                assort.LoyalLevelItems[offer] = 1;
                Console.WriteLine("[Tarkova-Cola] caja de perks creada (Therapist nivel 1, " + Price + " RUB; admitida en " + places + " contenedores)");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando la caja de perks: " + e); }
            return Task.CompletedTask;
        }
    }
}
