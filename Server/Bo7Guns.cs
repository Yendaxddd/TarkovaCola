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
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace TarkovaCola.Server
{
    // Armas de Black Ops 7: MK35 ISR (sobre la HK416A5, 5.56) y VMP (sobre la MP5 Navy, 9x19). Mismas animaciones, calibre y
    // estadisticas que el arma base; las piezas de BO7 son accesorios propios (bundles de UnityWeapons/BuildBo7Guns.cs). En la MK35
    // encajan las piezas de AR; en la VMP, bocachas de MP5 y miras de riel. Las vende Mechanic (montadas) y salen en la caja misteriosa.
    // El cliente (Client/Bo7Guns.cs) oculta el armazon del arma base y su asa de carga.
    internal static class Bo7Guns
    {
        internal const string Mk35 = "6a1c00000000000000b70001", Mk35Upper = "6a1c00000000000000b70002", Mk35Barrel = "6a1c00000000000000b70003",
                              Mk35Mag = "6a1c00000000000000b70004", Mk35Grip = "6a1c00000000000000b70005", Mk35Stock = "6a1c00000000000000b70006";
        // la VMP sobre MP7 (b70011-13) se quito: la de la MP5 lleva ids nuevos para que una VMP vieja no rompa el inventario
        internal const string Vmp = "6a1c00000000000000b70021", VmpUpper = "6a1c00000000000000b70022", VmpMag = "6a1c00000000000000b70023", VmpStock = "6a1c00000000000000b70024";
        internal const string Mk35Sight = "6a1c00000000000000b70007", VmpSight = "6a1c00000000000000b70025";   // miras traseras (las de hierro de BO7)
        internal const string Hk416 ="5bb2475ed4351e00853264e3", Mp5 = "5926bb2186f7744b1c6c6e60", Mp5Upper = "5926c0df86f77462f647f764";
        internal const string Bundles = "assets/content/items/mods/tarkovacola/";
        internal const int Mechanic1 = 1;           // nivel de lealtad de Mechanic
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 3)]
    public class TarkovaColaBo7Guns : IOnLoad
    {
        private readonly CustomItemService _items;
        private readonly TemplateTable _templates;
        private readonly TradersTable _traders;
        private readonly GlobalTable _globals;

        public TarkovaColaBo7Guns(CustomItemService items, TemplateTable templates, TradersTable traders, GlobalTable globals)
        { _items = items; _templates = templates; _traders = traders; _globals = globals; }

        private TemplateItem T(string id) => _templates.Items[new MongoId(id)];

        private void Clone(string baseTpl, string id, string name, string bundle, string en, string es, string shortName, string desc, int price, string handbookParent, string descEs = null)
        {
            var b = T(baseTpl);
            _items.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = new MongoId(baseTpl),
                ParentId = b.Parent,
                NewId = new MongoId(id),
                NewItemName = name,
                OverrideProperties = bundle == null ? new TemplateItemProperties() : new TemplateItemProperties { Prefab = new Prefab { Path = Bo7Guns.Bundles + bundle + ".bundle", Rcid = "" } },
                FleaPriceRoubles = price,
                HandbookPriceRoubles = price,
                HandbookParentId = handbookParent,
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails { Name = en, ShortName = shortName, Description = desc + "\n\nBlack Ops 7 — Tarkova-Cola." },
                    ["es"] = new LocaleDetails { Name = es, ShortName = shortName, Description = (descEs ?? desc) + "\n\nBlack Ops 7 — Tarkova-Cola." },
                },
            });
        }

        private static string HandbookOf(TemplateItem t, HandbookBase hb)
        {
            var e = hb?.Items?.FirstOrDefault(i => i.Id == t.Id);
            return e != null ? e.ParentId.ToString() : "5b5f71a686f77447ed5636ab";   // Weapon parts and mods
        }

        // anade 'tpl' a la ranura 'slot' de 'owner'
        private void Allow(string owner, string slot, params string[] tpls)
        {
            var s = T(owner).Properties.Slots.FirstOrDefault(x => x.Name == slot);
            if (s == null) { Console.WriteLine("[Tarkova-Cola] BO7: " + owner + " no tiene la ranura " + slot); return; }
            foreach (var tpl in tpls) s.Properties.Filters.First().Filter.Add(new MongoId(tpl));
        }

        // deja solo esas ranuras (con esos objetos si se dan)
        private void KeepSlots(string owner, Dictionary<string, string[]> keep)
        {
            var p = T(owner).Properties;
            p.Slots = p.Slots.Where(x => keep.ContainsKey(x.Name)).ToList();
            foreach (var s in p.Slots)
                if (keep[s.Name] != null) s.Properties.Filters.First().Filter = new HashSet<MongoId>(keep[s.Name].Select(x => new MongoId(x)));
        }

        // copia ranuras (con su lista de accesorios) de 'from' a 'to', opcionales; las del guardamanos de la HK416 van a las piezas de BO7
        private int _slotIds;
        private const string Hk416RearSight = "5bb20e49d4351e3bac1212de";      // HK 416A5 flip-up rear sight (IronSight)
        private const string QuadRail ="5bb20de5d4351e0035629e59";             // HK 416 Quad Rail
        private void CopySlots(string from, string to, params string[] names)
        {
            var dst = T(to).Properties.Slots.ToList();
            foreach (var n in names)
            {
                var s = T(from).Properties.Slots.First(x => x.Name == n);
                dst.RemoveAll(x => x.Name == n);
                dst.Add(new Slot
                {
                    Name = s.Name, Id = new MongoId("6a1c0000000000000051" + (++_slotIds).ToString("x4")), Parent = to, Required = false,
                    MergeSlotWithChildren = s.MergeSlotWithChildren, Prototype = s.Prototype,
                    Properties = new SlotProperties { Filters = s.Properties.Filters.Select(f => new SlotFilter { Shift = f.Shift, Filter = new HashSet<MongoId>(f.Filter) }).ToList() },
                });
            }
            T(to).Properties.Slots = dst;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var hb = _templates.Handbook;
                string hbWeapon(string tpl) => HandbookOf(T(tpl), hb);

                // ---- MK35 ISR
                Clone(Bo7Guns.Hk416, Bo7Guns.Mk35, "tarkovacola_mk35", null, "MK35 ISR 5.56x45 assault rifle", "Fusil de asalto MK35 ISR 5.56x45", "MK35",
                    "The MK35 ISR (Integrated Suppression Rifle) is a 5.56x45 assault rifle that should not exist yet: a 2035 design that somehow ended up in Tarkov, shipped in crates stencilled with a cola logo nobody remembers ordering. Under the angular receiver it is an HK 416-pattern lower, so it accepts AR-15 grips, buffer tubes, stocks and magazines, and its quad-rail handguard takes foregrips, lights and lasers. Flat-top rail with built-in flip-up iron sights. Fires in semi-automatic and full-automatic at around 850 rounds per minute.", 95000, hbWeapon(Bo7Guns.Hk416),
                    "La MK35 ISR (Integrated Suppression Rifle) es un fusil de asalto de 5.56x45 que todavía no debería existir: un diseño de 2035 que de algún modo acabó en Tarkov, en cajas marcadas con el logo de una cola que nadie recuerda haber pedido. Bajo su receptor anguloso hay un armazón tipo HK 416, así que admite empuñaduras, tubos de culata, culatas y cargadores de AR-15, y su guardamanos con cuatro rieles lleva empuñaduras delanteras, linternas y láseres. Riel superior con miras de hierro plegables integradas. Dispara en semiautomático y automático, a unos 850 disparos por minuto.");
                Clone("5bb20d53d4351e4502010a69", Bo7Guns.Mk35Upper, "tarkovacola_mk35_upper", "mk35_upper", "MK35 ISR upper receiver", "Receptor superior MK35 ISR", "MK35 upper",
                    "Upper receiver of the MK35 ISR.", 18000, hbWeapon("5bb20d53d4351e4502010a69"));
                Clone("5bb20d9cd4351e00334c9d8a", Bo7Guns.Mk35Barrel, "tarkovacola_mk35_barrel", "mk35_barrel", "MK35 ISR barrel and handguard", "Cañón y guardamanos MK35 ISR", "MK35 bar",
                    "Barrel and handguard of the MK35 ISR.", 22000, hbWeapon("5bb20d9cd4351e00334c9d8a"));
                Clone("5c05413a0db834001c390617", Bo7Guns.Mk35Mag, "tarkovacola_mk35_mag", "mk35_mag", "MK35 ISR 5.56x45 30-round magazine", "Cargador de 30 MK35 ISR 5.56x45", "MK35",
                    "30-round magazine of the MK35 ISR.", 4000, hbWeapon("5c05413a0db834001c390617"));
                Clone("5bb20e0ed4351e3bac1212dc", Bo7Guns.Mk35Grip, "tarkovacola_mk35_pgrip", "mk35_pgrip", "MK35 ISR pistol grip", "Empuñadura MK35 ISR", "MK35",
                    "Pistol grip of the MK35 ISR.", 3000, hbWeapon("5bb20e0ed4351e3bac1212dc"));
                Clone("5bb20e70d4351e0035629f8f", Bo7Guns.Mk35Stock, "tarkovacola_mk35_stock", "mk35_stock", "MK35 ISR stock", "Culata MK35 ISR", "MK35",
                    "Stock of the MK35 ISR. Mounts directly on the receiver.", 6000, hbWeapon("5bb20e70d4351e0035629f8f"));
                // el receptor de BO7 lleva su canon y una ranura de mira; el canon, solo bocacha
                var upperScope = T("5bb20d53d4351e4502010a69").Properties.Slots.First(s => s.Name == "mod_scope").Properties.Filters.First().Filter.Select(x => x.ToString()).ToArray();
                KeepSlots(Bo7Guns.Mk35Upper, new Dictionary<string, string[]> { ["mod_barrel"] = new[] { Bo7Guns.Mk35Barrel }, ["mod_scope"] = upperScope });
                // miras de hierro: EFT solo deja apuntar con ellas si hay un objeto de mira; el de BO7 (invisible: la mira la dibuja el receptor)
                // o una de AR en su lugar
                Clone(Hk416RearSight, Bo7Guns.Mk35Sight, "tarkovacola_mk35_sight_rear", "mk35_sight_rear", "MK35 ISR iron sights", "Miras de hierro MK35 ISR", "MK35",
                    "Built-in iron sights of the MK35 ISR. Lets you aim down the sights without an optic.", 2000, hbWeapon(Hk416RearSight));
                CopySlots("5bb20d53d4351e4502010a69", Bo7Guns.Mk35Upper, "mod_sight_rear");
                Allow(Bo7Guns.Mk35Upper, "mod_sight_rear", Bo7Guns.Mk35Sight);
                KeepSlots(Bo7Guns.Mk35Barrel, new Dictionary<string, string[]> { ["mod_muzzle"] = null });
                CopySlots(QuadRail, Bo7Guns.Mk35Barrel, "mod_foregrip", "mod_sight_front", "mod_tactical_000", "mod_tactical_001", "mod_tactical_002", "mod_bipod");
                Allow(Bo7Guns.Mk35, "mod_reciever", Bo7Guns.Mk35Upper);
                Allow(Bo7Guns.Mk35, "mod_magazine", Bo7Guns.Mk35Mag);
                Allow(Bo7Guns.Mk35, "mod_pistol_grip", Bo7Guns.Mk35Grip);
                Allow(Bo7Guns.Mk35, "mod_stock", Bo7Guns.Mk35Stock);
                // el cargador y la empunadura valen tambien en la HK416 normal y en los AR-15 que comparten ranura
                foreach (var t in _templates.Items.Values)
                    foreach (var s in t.Properties?.Slots ?? Enumerable.Empty<Slot>())
                    {
                        var f = s.Properties?.Filters?.FirstOrDefault()?.Filter;
                        if (f == null) continue;
                        if (f.Contains(new MongoId("5c05413a0db834001c390617"))) f.Add(new MongoId(Bo7Guns.Mk35Mag));
                        if (f.Contains(new MongoId("5bb20e0ed4351e3bac1212dc"))) f.Add(new MongoId(Bo7Guns.Mk35Grip));
                    }

                // ---- VMP (sobre la MP5 Navy: en ella la empunadura va en el armazon y el receptor superior es un accesorio)
                Clone(Bo7Guns.Mp5, Bo7Guns.Vmp, "tarkovacola_vmp", null, "VMP 9x19 submachine gun", "Subfusil VMP 9x19", "VMP",
                    "The VMP is a compact 9x19 submachine gun from a war that has not happened yet. Its magazine sits inside the pistol grip, which keeps the weapon short and balanced, and its long frame doubles as the stock. Built around an MP5-pattern fire control group, it takes MP5 muzzle devices and suppressors, rail-mounted optics, a foregrip and side-mounted lights or lasers. Built-in iron sights on the top rail. Semi-automatic, three-round burst and full-automatic.", 70000, hbWeapon(Bo7Guns.Mp5),
                    "La VMP es un subfusil compacto de 9x19 de una guerra que todavía no ha pasado. Su cargador va dentro de la empuñadura, lo que la mantiene corta y equilibrada, y su armazón largo hace de culata. Construida sobre un grupo de disparo tipo MP5, admite bocachas y supresores de MP5, miras sobre riel, empuñadura delantera y linternas o láseres laterales. Miras de hierro integradas en el riel superior. Semiautomático, ráfaga de tres y automático.");
                Clone(Bo7Guns.Mp5Upper, Bo7Guns.VmpUpper, "tarkovacola_vmp_upper", "vmp_upper", "VMP receiver", "Receptor VMP", "VMP",
                    "Receiver, barrel and pistol grip of the VMP, with a top rail.", 20000, hbWeapon(Bo7Guns.Mp5Upper));
                Clone("5926c3b286f774640d189b6b", Bo7Guns.VmpMag, "tarkovacola_vmp_mag", "vmp_mag", "VMP 9x19 30-round magazine", "Cargador de 30 VMP 9x19", "VMP",
                    "30-round magazine of the VMP. It goes in the pistol grip.", 4000, hbWeapon("5926c3b286f774640d189b6b"));
                Clone("5926d3c686f77410de68ebc8", Bo7Guns.VmpStock, "tarkovacola_vmp_stock", "vmp_stock", "VMP stock", "Culata VMP", "VMP",
                    "Buttstock of the VMP.", 6000, hbWeapon("5926d3c686f77410de68ebc8"));
                T(Bo7Guns.VmpStock).Properties.Slots = new List<Slot>();
                // el receptor: su culata, las bocachas de la MP5 y un riel arriba (la montura de la MP5 pasa a ser "mod_scope" con las miras de riel)
                var mount = T(Bo7Guns.VmpUpper).Properties.Slots.FirstOrDefault(s => s.Name == "mod_mount");
                if (mount != null) mount.Name = "mod_scope";
                KeepSlots(Bo7Guns.VmpUpper, new Dictionary<string, string[]> { ["mod_stock"] = new[] { Bo7Guns.VmpStock }, ["mod_muzzle"] = null, ["mod_scope"] = upperScope });
                foreach (var s in T(Bo7Guns.VmpUpper).Properties.Slots) if (s.Name == "mod_scope") s.Required = false;
                CopySlots(QuadRail, Bo7Guns.VmpUpper, "mod_foregrip", "mod_tactical_001", "mod_tactical_002");
                Clone(Hk416RearSight, Bo7Guns.VmpSight, "tarkovacola_vmp_sight_rear", "vmp_sight_rear", "VMP iron sights", "Miras de hierro VMP", "VMP",
                    "Built-in iron sights of the VMP. Lets you aim down the sights without an optic.", 2000, hbWeapon(Hk416RearSight));
                CopySlots("5bb20d53d4351e4502010a69", Bo7Guns.VmpUpper, "mod_sight_rear");
                Allow(Bo7Guns.VmpUpper, "mod_sight_rear", Bo7Guns.VmpSight);
                // su cargador va en la empunadura: los de la MP5 quedarian delante, en el aire
                KeepSlots(Bo7Guns.Vmp, new Dictionary<string, string[]> { ["mod_magazine"] = new[] { Bo7Guns.VmpMag }, ["mod_reciever"] = new[] { Bo7Guns.VmpUpper }, ["mod_charge"] = null });

                // ---- montadas: presets de fabrica (la caja misteriosa y los iconos) y venta en Mechanic
                var mk35 = new List<(string slot, string tpl, int parent)>
                {
                    ("mod_pistol_grip", Bo7Guns.Mk35Grip, 0), ("mod_magazine", Bo7Guns.Mk35Mag, 0), ("mod_reciever", Bo7Guns.Mk35Upper, 0),
                    ("mod_barrel", Bo7Guns.Mk35Barrel, 3), ("mod_muzzle", "544a38634bdc2d58388b4568", 4),
                    ("mod_stock", Bo7Guns.Mk35Stock, 0), ("mod_charge", "5bb20dbcd4351e44f824c04e", 0), ("mod_sight_rear", Bo7Guns.Mk35Sight, 3),
                };
                var vmp = new List<(string slot, string tpl, int parent)>
                {
                    ("mod_magazine", Bo7Guns.VmpMag, 0), ("mod_reciever", Bo7Guns.VmpUpper, 0), ("mod_stock", Bo7Guns.VmpStock, 2),
                    ("mod_muzzle", "5926e16e86f7742f5a0f7ecb", 2), ("mod_charge", "5926c32286f774616e42de99", 0), ("mod_sight_rear", Bo7Guns.VmpSight, 2),
                };
                AddGun(Bo7Guns.Mk35, "6a1c00000000000000b7c100", "6a1c00000000000000b7a100", "6a1c00000000000000b7b100", mk35, 95000);
                // las miras sueltas (para las armas compradas antes de que las llevaran)
                AddOffer(Bo7Guns.Mk35Sight, "6a1c00000000000000b7d100", 2000);
                AddOffer(Bo7Guns.VmpSight, "6a1c00000000000000b7d200", 2000);
                AddGun(Bo7Guns.Vmp, "6a1c00000000000000b7c300", "6a1c00000000000000b7a300", "6a1c00000000000000b7b300", vmp, 70000);
                Console.WriteLine("[Tarkova-Cola] armas de BO7: MK35 ISR y VMP (Mechanic nivel " + Bo7Guns.Mechanic1 + ")");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando las armas de BO7: " + e); }
            return Task.CompletedTask;
        }

        private void AddOffer(string tpl, string offerId, int price)
        {
            var assort = _traders[Traders.MECHANIC].Assort;
            var id = new MongoId(offerId);
            assort.Items.Add(new Item { Id = id, Template = new MongoId(tpl), ParentId = "hideout", SlotId = "hideout", Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true } });
            assort.BarterScheme[id] = new List<List<BarterScheme>> { new List<BarterScheme> { new BarterScheme { Count = price, Template = new MongoId("5449016a4bdc2d6f028b456f") } } };
            assort.LoyalLevelItems[id] = Bo7Guns.Mechanic1;
        }

        // ids: el preset, el arma del preset y la de la oferta (acaban en 00); sus piezas cambian el ultimo byte por su indice
        private void AddGun(string gun, string presetId, string presetRoot, string offerId, List<(string slot, string tpl, int parent)> parts, int price)
        {
            List<Item> Build(string rootId, string rootParent, string rootSlot)
            {
                var ids = new List<MongoId> { new MongoId(rootId) };
                var list = new List<Item> { new Item { Id = ids[0], Template = new MongoId(gun), ParentId = rootParent, SlotId = rootSlot } };
                for (int i = 0; i < parts.Count; i++)
                {
                    var id = new MongoId(rootId.Substring(0, 22) + (i + 1).ToString("x2"));
                    ids.Add(id);
                    list.Add(new Item { Id = id, Template = new MongoId(parts[i].tpl), ParentId = ids[parts[i].parent].ToString(), SlotId = parts[i].slot });
                }
                return list;
            }

            var presetItems = Build(presetRoot, null, null);
            _globals.ItemPresets[new MongoId(presetId)] = new Preset
            {
                Id = new MongoId(presetId), Type = "Preset", ChangeWeaponName = false, Name = T(gun).Name + "_default",
                Parent = presetItems[0].Id, Items = presetItems, Encyclopedia = new MongoId(gun),
            };

            var assort = _traders[Traders.MECHANIC].Assort;
            var offer = Build(offerId, "hideout", "hideout");
            offer[0].Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true, FireMode = new UpdFireMode { FireMode = "fullauto" } };   // de fabrica en automatico
            assort.Items.AddRange(offer);
            assort.BarterScheme[offer[0].Id] = new List<List<BarterScheme>> { new List<BarterScheme> { new BarterScheme { Count = price, Template = new MongoId("5449016a4bdc2d6f028b456f") } } };
            assort.LoyalLevelItems[offer[0].Id] = Bo7Guns.Mechanic1;
        }
    }
}
