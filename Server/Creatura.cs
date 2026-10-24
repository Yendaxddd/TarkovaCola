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
    // "le creatura": M4 SOPMOD II Jr (Girls' Frontline) como granada de racimo. Es una RGD-5 por dentro; el cliente le pone el
    // modelo de Jr, el sonido al lanzarla y las 3 mini-explosiones. Siempre se puede encontrar en el mercadillo; Therapist solo la
    // vende si el jugador activa la opcion secreta de F12 ("???").
    internal static class Creatura
    {
        internal const string ItemId = "6a1c00000000000000000b01";
        internal const string OfferId = "6a1c00000000000000000b02";
        internal const string Rgd5 = "5448be9a4bdc2dfd2f8b456a";
        private const string NeverQuest = "6a1c0000000000000000ffff";   // mision que no existe: oculta la oferta mientras la opcion esta apagada
        internal const int PriceRoubles = 6661;

        private static TradersTable _traders;
        private static object _assortHelper;

        internal static void Init(TradersTable traders, object assortHelper) { _traders = traders; _assortHelper = assortHelper; }

        internal static void SetForSale(bool on)
        {
            try
            {
                var qa = _traders?[Traders.THERAPIST].QuestAssort;
                if (qa == null) return;
                if (!qa.TryGetValue("success", out var success) || success == null) qa["success"] = success = new Dictionary<MongoId, MongoId>();
                if (on) success.Remove(new MongoId(OfferId)); else success[new MongoId(OfferId)] = new MongoId(NeverQuest);
                var f = _assortHelper?.GetType().GetField("<MergedQuestAssorts>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (f != null) f.SetValue(_assortHelper, null);
                Console.WriteLine("[Tarkova-Cola] le creatura en Therapist: " + (on ? "A LA VENTA (que has hecho...)" : "oculta"));
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] le creatura (venta): " + e.Message); }
        }
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 2)]
    public class TarkovaColaCreatura : IOnLoad
    {
        private readonly CustomItemService _items;
        private readonly TemplateTable _templates;
        private readonly TradersTable _traders;
        private readonly SPTarkov.Server.Core.Helpers.Traders.TraderAssortHelper _assortHelper;

        public TarkovaColaCreatura(CustomItemService items, TemplateTable templates, TradersTable traders, SPTarkov.Server.Core.Helpers.Traders.TraderAssortHelper assortHelper)
        {
            _items = items; _templates = templates; _traders = traders; _assortHelper = assortHelper;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var baseItem = _templates.Items[new MongoId(Creatura.Rgd5)];
                _items.CreateItemFromClone(new NewItemFromCloneDetails
                {
                    ItemTplToClone = new MongoId(Creatura.Rgd5),
                    ParentId = baseItem.Parent,
                    NewId = new MongoId(Creatura.ItemId),
                    NewItemName = "tarkovacola_le_creatura",
                    OverrideProperties = new TemplateItemProperties { CanSellOnRagfair = true },
                    FleaPriceRoubles = Creatura.PriceRoubles,
                    HandbookPriceRoubles = Creatura.PriceRoubles,
                    HandbookParentId = "5b5f7a2386f774093f2ed3c4",      // handbook: Throwables
                    Locales = new Dictionary<string, LocaleDetails>
                    {
                        ["en"] = new LocaleDetails { Name = "le creatura", ShortName = "Jr.",
                            Description = "M4 SOPMOD II Jr. A tiny plush made by SOP-II out of the parts of fallen enemy Dolls. It squeaks. It giggles. When it goes off, it splits into three smaller surprises.\n\n\"Shall I start from the fingernails... Or... Should I rip out your wires one by one?\"\n\nCluster grenade: after the blast, 3 small extra explosions around it." },
                        ["es"] = new LocaleDetails { Name = "le creatura", ShortName = "Jr.",
                            Description = "M4 SOPMOD II Jr. Un peluche diminuto que SOP-II hizo con piezas de muñecas enemigas caídas. Chilla. Se ríe. Al explotar, se divide en tres sorpresas más pequeñas.\n\n\"Shall I start from the fingernails... Or... Should I rip out your wires one by one?\"\n\nGranada de racimo: tras la explosión, 3 explosiones pequeñas más a su alrededor." },
                    },
                });

                var assort = _traders[Traders.THERAPIST].Assort;
                var offer = new MongoId(Creatura.OfferId);
                assort.Items.Add(new Item { Id = offer, Template = new MongoId(Creatura.ItemId), ParentId = "hideout", SlotId = "hideout",
                    Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true } });
                assort.BarterScheme[offer] = new List<List<BarterScheme>> { new List<BarterScheme> { new BarterScheme { Count = Creatura.PriceRoubles, Template = new MongoId("5449016a4bdc2d6f028b456f") } } };
                assort.LoyalLevelItems[offer] = 1;
                Creatura.Init(_traders, _assortHelper);
                Creatura.SetForSale(false);
                Console.WriteLine("[Tarkova-Cola] le creatura creada (granada de racimo, mercadillo; Therapist solo con la opcion secreta)");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando le creatura: " + e); }
            return Task.CompletedTask;
        }
    }

    public class JrRequest : SPTarkov.Server.Core.Models.Utils.IRequestData
    {
        [System.Text.Json.Serialization.JsonPropertyName("on")] public bool On { get; set; }
    }
}
