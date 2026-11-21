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
        // Peacekeeper Mk1 (ar_condor) sobre el AK-12
        internal const string Pk = "6a1c00000000000000b70101", PkUpper = "6a1c00000000000000b70102", PkBarrel = "6a1c00000000000000b70103", PkMag = "6a1c00000000000000b70104",
                              PkGrip = "6a1c00000000000000b70105", PkStock = "6a1c00000000000000b70106", PkSight = "6a1c00000000000000b70107";
        internal const string Ak12 = "6499849fc93611967b034949";
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
                try { AddPeacekeeper(hbWeapon, upperScope); } catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando la Peacekeeper: " + e); }
                Console.WriteLine("[Tarkova-Cola] armas de BO7: MK35 ISR, VMP, Peacekeeper Mk1, Voyak KT-3, MXR-17, VST, M10 Breacher y Hawker HX (Mechanic nivel " + Bo7Guns.Mechanic1 + ")");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando las armas de BO7: " + e); }
            return Task.CompletedTask;
        }

        // ---- Fusil de BO7 generico: clon de un arma de EFT (su controlador de animaciones) con todas las piezas propias.
        // ids: <Id>01 arma, 02 receptor, 03 canon, 04 cargador, 05 empunadura, 06 culata, 07 miras; preset/oferta <Preset>.
        private sealed class Rifle
        {
            public string Id, Key, Preset, Base, Cover, Mag, Grip, Stock, StockSlot, Name, NameEs, Short, Desc, DescEs, Caliber;
            public int Price, Rpm, Ergo, RecoilUp, RecoilBack;
            public int Size = 5;            // ancho en el inventario con la culata puesta
            public double Coi = 0.04;       // CenterOfImpact del canon (precision)
            public string[] FireModes = { "single", "fullauto" };
            public string T(int n) => Id + n.ToString("00");
        }

        private void AddRifle(Rifle r, Func<string, string> hbWeapon, string[] railScopes)
        {
            // sin sus bundles el arma saldria invisible y con errores: no se crea hasta que esten instalados
            string need = System.IO.Path.Combine(QuestIds.ModDir, "bundles", Bo7Guns.Bundles.Replace('/', System.IO.Path.DirectorySeparatorChar), r.Key + "_upper.bundle");
            if (!System.IO.File.Exists(need)) { Console.WriteLine("[Tarkova-Cola] BO7: " + r.Name + " sin bundles (" + r.Key + "_upper.bundle): no se anade"); return; }
            const string HkUpper = "5bb20d53d4351e4502010a69", HkBarrel = "5bb20d9cd4351e00334c9d8a";
            string gun = r.T(1), upper = r.T(2), barrel = r.T(3), mag = r.T(4), grip = r.T(5), stock = r.T(6), sight = r.T(7), k = r.Key;
            Clone(r.Base, gun, "tarkovacola_" + k, null, r.Name + " " + r.Caliber + " assault rifle", "Fusil de asalto " + r.NameEs + " " + r.Caliber, r.Short, r.Desc, r.Price, hbWeapon(r.Base), r.DescEs);
            var g = T(gun).Properties;
            g.BFirerate = r.Rpm; g.Ergonomics = r.Ergo; g.RecoilForceUp = r.RecoilUp; g.RecoilForceBack = r.RecoilBack;
            g.WeapFireType = new HashSet<string>(r.FireModes);
            Clone(r.Cover, upper, "tarkovacola_" + k + "_upper", k + "_upper", r.Name + " receiver", "Receptor " + r.NameEs, r.Short,
                "Receiver of the " + r.Name + ", with its top rail.", r.Price / 4, hbWeapon(r.Cover), "Receptor del " + r.NameEs + ", con su riel superior.");
            Clone(HkBarrel, barrel, "tarkovacola_" + k + "_barrel", k + "_barrel", r.Name + " barrel and handguard", "Cañón y guardamanos " + r.NameEs, r.Short,
                "Barrel and handguard of the " + r.Name + ".", r.Price / 5, hbWeapon(HkBarrel), "Cañón y guardamanos del " + r.NameEs + ".");
            Clone(r.Mag, mag, "tarkovacola_" + k + "_mag", k + "_mag", r.Name + " " + r.Caliber + " 30-round magazine", "Cargador de 30 " + r.NameEs + " " + r.Caliber, r.Short,
                "30-round magazine of the " + r.Name + ".", 4500, hbWeapon(r.Mag), "Cargador de 30 cartuchos del " + r.NameEs + ".");
            if (r.Grip != null) Clone(r.Grip, grip, "tarkovacola_" + k + "_pgrip", k + "_pgrip", r.Name + " pistol grip", "Empuñadura " + r.NameEs, r.Short,
                "Pistol grip of the " + r.Name + ".", 3000, hbWeapon(r.Grip), "Empuñadura del " + r.NameEs + ".");
            Clone(r.Stock, stock, "tarkovacola_" + k + "_stock", k + "_stock", r.Name + " stock", "Culata " + r.NameEs, r.Short,
                "Stock of the " + r.Name + ".", 7000, hbWeapon(r.Stock), "Culata del " + r.NameEs + ".");
            Clone(Hk416RearSight, sight, "tarkovacola_" + k + "_sight_rear", k + "_sight_rear", r.Name + " iron sights", "Miras de hierro " + r.NameEs, r.Short,
                "Built-in iron sights of the " + r.Name + ". Lets you aim down the sights without an optic.", 2000, hbWeapon(Hk416RearSight),
                "Miras de hierro integradas del " + r.NameEs + ". Permiten apuntar sin mira óptica.");
            T(stock).Properties.Slots = new List<Slot>();
            // tamano: el canon clonado del HK416 sumaba 3 a la izquierda (y el receptor de la SCAR 2 mas); solo el canon da el ancho que falta
            var upp = T(upper).Properties; upp.ExtraSizeLeft = 0; upp.ExtraSizeForceAdd = false;
            var bp = T(barrel).Properties;
            bp.ExtraSizeLeft = Math.Max(0, r.Size - (T(r.Base).Properties.Width ?? 1) - (T(stock).Properties.ExtraSizeRight ?? 0));
            bp.CenterOfImpact = r.Coi; T(gun).Properties.CenterOfImpact = r.Coi;
            // receptor: canon propio, riel para miras y las de hierro
            var muzzles = T(r.Base).Properties.Slots.FirstOrDefault(x => x.Name == "mod_muzzle")?.Properties.Filters.First().Filter.Select(x => x.ToString()).ToArray();
            CopySlots(HkUpper, upper, "mod_barrel", "mod_scope", "mod_sight_rear");
            KeepSlots(upper, new Dictionary<string, string[]> { ["mod_barrel"] = new[] { barrel }, ["mod_scope"] = railScopes, ["mod_sight_rear"] = new[] { sight } });
            foreach (var sl in T(upper).Properties.Slots) sl.Required = sl.Name == "mod_barrel";
            // canon: bocachas del arma base (si las lleva en el arma) y las ranuras del guardamanos
            KeepSlots(barrel, new Dictionary<string, string[]> { ["mod_muzzle"] = muzzles });
            CopySlots(QuadRail, barrel, "mod_foregrip", "mod_tactical_001", "mod_tactical_002");
            foreach (var sl in T(barrel).Properties.Slots) sl.Required = false;
            // sin Grip (MP9): el arma base no tiene ranura de empunadura y la de BO7 va dentro del receptor
            var gunSlots = new Dictionary<string, string[]> { ["mod_reciever"] = new[] { upper }, ["mod_magazine"] = new[] { mag }, [r.StockSlot] = new[] { stock } };
            if (r.Grip != null) gunSlots["mod_pistol_grip"] = new[] { grip };
            KeepSlots(gun, gunSlots);
            var parts = new List<(string slot, string tpl, int parent)>();
            if (r.Grip != null) parts.Add(("mod_pistol_grip", grip, 0));
            parts.Add(("mod_magazine", mag, 0)); parts.Add(("mod_reciever", upper, 0));
            int up = parts.Count;   // indice del receptor (0 = el arma)
            parts.Add(("mod_barrel", barrel, up)); parts.Add((r.StockSlot, stock, 0)); parts.Add(("mod_sight_rear", sight, up));   // sin bocacha: el canon ya trae la suya
            AddGun(gun, "6a1c00000000000000b7c" + r.Preset + "00", "6a1c00000000000000b7a" + r.Preset + "00", "6a1c00000000000000b7b" + r.Preset + "00", parts, r.Price);
        }

        private void AddPeacekeeper(Func<string, string> hbWeapon, string[] railScopes)
        {
            AddRifle(new Rifle
            {
                Id = "6a1c00000000000000b701", Key = "pk", Preset = "5", Base = Bo7Guns.Ak12, Cover = "649ec2f3961514b22506b111", Mag = "649ec30cb013f04a700e60fb",
                Grip = "5beec8ea0db834001a6f9dbf", Stock = "5beec8c20db834001d2c465c", StockSlot = "mod_stock_001", Caliber = "5.45x39",
                Name = "Peacekeeper Mk1", NameEs = "Peacekeeper Mk1", Short = "Peacekeeper", Size = 4, Coi = 0.035, Price = 120000, Rpm = 750, Ergo = 52, RecoilUp = 84, RecoilBack = 268,
                Desc = "The Peacekeeper Mk1 is a compact assault rifle built around a full-length monolithic top rail and a short, free-floated barrel. It was designed to bridge the gap between a submachine gun and a rifle: light and quick to shoulder, yet steady enough in automatic fire to hold a group at carbine distances. This example is chambered in 5.45x39 and feeds from its own polymer 30-round magazines. The bolt locks to the rear on an empty magazine and is released with the left thumb. Flip-up iron sights are built into the rail; the handguard takes a foregrip and side-mounted lights or lasers. Semi-automatic and full-automatic, around 750 rounds per minute.",
                DescEs = "El Peacekeeper Mk1 es un fusil de asalto compacto construido alrededor de un riel superior monolítico y un cañón corto flotante. Se diseñó para cubrir el hueco entre un subfusil y un fusil: ligero y rápido de encarar, pero lo bastante estable en automático para agrupar a distancias de carabina. Este ejemplar dispara 5.45x39 y usa sus propios cargadores de polímero de 30 cartuchos. El cerrojo queda abierto con el cargador vacío y se suelta con el pulgar izquierdo. Lleva miras de hierro abatibles integradas en el riel; el guardamanos admite empuñadura delantera y linternas o láseres laterales. Semiautomático y automático, a unos 750 disparos por minuto.",
            }, hbWeapon, railScopes);
            AddRifle(new Rifle
            {
                Id = "6a1c00000000000000b702", Key = "vk", Preset = "6", Base = "5ac4cd105acfc40016339859", Cover = "5ac50da15acfc4001718d287", Mag = "55d480c04bdc2d1d4e8b456a",
                Grip = "5649ade84bdc2d1b2b8b4587", Stock = "5ac50c185acfc400163398d4", StockSlot = "mod_stock", Caliber = "5.45x39",
                Name = "Voyak KT-3", NameEs = "Voyak KT-3", Short = "Voyak", Size = 5, Coi = 0.045, Price = 105000, Rpm = 620, Ergo = 44, RecoilUp = 104, RecoilBack = 300,
                Desc = "The Voyak KT-3 is a heavy-hitting assault rifle of Kalashnikov lineage: a long-stroke piston action in a modernised receiver with a full top rail and a railed handguard. It fires slower than most rifles of its class, which makes it easy to keep on target and gives every round time to count. The magazine rocks in from the front and the charging handle is on the right side, as on the rifles it descends from. This example is chambered in 5.45x39 and feeds from its own 30-round magazines. Built-in iron sights; the handguard takes a foregrip and side-mounted lights or lasers. Semi-automatic and full-automatic, around 620 rounds per minute.",
                DescEs = "El Voyak KT-3 es un fusil de asalto contundente de linaje Kalashnikov: acción de pistón de carrera larga en un receptor modernizado con riel superior corrido y guardamanos con rieles. Dispara más despacio que la mayoría de los fusiles de su clase, lo que lo hace fácil de mantener en el blanco y da tiempo a que cada bala cuente. El cargador entra basculando desde delante y la palanca de montar va a la derecha, como en los fusiles de los que desciende. Este ejemplar dispara 5.45x39 y usa sus propios cargadores de 30 cartuchos. Miras de hierro integradas; el guardamanos admite empuñadura delantera y linternas o láseres laterales. Semiautomático y automático, a unos 620 disparos por minuto.",
            }, hbWeapon, railScopes);
            AddRifle(new Rifle
            {
                Id = "6a1c00000000000000b703", Key = "mx", Preset = "7", Base = "6184055050224f204c1da540", Cover = "618405198004cc50514c3594", Mag = "61840bedd92c473c77021635",
                Grip = "55d4b9964bdc2d1d4e8b456e", Stock = "61816734d8e3106d9806c1f3", StockSlot = "mod_stock", Caliber = "5.56x45",
                Name = "MXR-17", NameEs = "MXR-17", Short = "MXR-17", Size = 5, Coi = 0.03, Price = 115000, Rpm = 600, Ergo = 42, RecoilUp = 108, RecoilBack = 330,
                Desc = "The MXR-17 is a full-size assault rifle built for reach: a short-stroke piston action in a monolithic aluminium upper with an uninterrupted top rail, a heavy barrel and a side-folding adjustable stock. It trades rate of fire for control, and rewards a shooter who places rounds instead of spraying them. The charging handle is non-reciprocating; the bolt locks open on an empty magazine and is sent home with the release on the left side of the receiver. This example is chambered in 5.56x45 and feeds from its own 30-round magazines. Built-in folding iron sights; the handguard takes a foregrip and side-mounted lights or lasers. Semi-automatic and full-automatic, around 600 rounds per minute.",
                DescEs = "El MXR-17 es un fusil de asalto de tamaño completo pensado para el alcance: acción de pistón de carrera corta en un receptor superior monolítico de aluminio con riel superior corrido, cañón pesado y culata plegable y ajustable. Cambia cadencia por control y premia a quien coloca los disparos en vez de rociarlos. La palanca de montar no se mueve al disparar; el cerrojo queda abierto con el cargador vacío y se suelta con el retén del lado izquierdo del receptor. Este ejemplar dispara 5.56x45 y usa sus propios cargadores de 30 cartuchos. Miras de hierro plegables integradas; el guardamanos admite empuñadura delantera y linternas o láseres laterales. Semiautomático y automático, a unos 600 disparos por minuto.",
            }, hbWeapon, railScopes);
            AddRifle(new Rifle
            {
                Id = "6a1c00000000000000b704", Key = "vs", Preset = "8", Base = "5e00903ae9dc277128008b87", Cover = "5de8e67c4a9f347bc92edbd7", Mag = "5de8eac42a78646d96665d91",
                Grip = null, Stock = "5de910da8b6c4240ba2651b5", StockSlot = "mod_stock", Caliber = "9x19",
                Name = "VST", NameEs = "VST", Short = "VST", Size = 3, Coi = 0.06, Price = 85000, Rpm = 950, Ergo = 72, RecoilUp = 50, RecoilBack = 230,
                Desc = "The VST is a compact submachine gun built for close quarters: a short, closed-bolt blowback action in a slim polymer frame with a full-length top rail. Its high rate of fire shreds targets at room distances, and its light weight keeps it on target while moving. The bolt locks open on an empty magazine and is released with the left hand. This example is chambered in 9x19 and feeds from its own 30-round magazines. Built-in iron sights; the handguard takes side-mounted lights or lasers. Semi-automatic and full-automatic, around 950 rounds per minute.",
                DescEs = "El VST es un subfusil compacto pensado para el combate cercano: acción de retroceso con cerrojo cerrado en un armazón fino de polímero con riel superior corrido. Su alta cadencia destroza objetivos a distancias de habitación, y su poco peso lo mantiene en el blanco en movimiento. El cerrojo queda abierto con el cargador vacío y se suelta con la mano izquierda. Este ejemplar dispara 9x19 y usa sus propios cargadores de 30 cartuchos. Miras de hierro integradas; el guardamanos admite linternas o láseres laterales. Semiautomático y automático, a unos 950 disparos por minuto.",
            }, hbWeapon, railScopes);
            AddShotgun(hbWeapon, railScopes);
            try { AddSniper(hbWeapon, railScopes); } catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando la Hawker HX: " + e); }
        }

        // ---- Hawker HX (sn_aden) sobre la AXMC (.338 LM): en la AXMC todo cuelga de la empunadura (chasis); la de BO7 es una pieza con
        // receptor, canon, empunadura y culata, con riel de mira, bocacha y linterna. El cerrojo de EFT (mod_charge, obligatorio) se queda
        // y el cliente lo oculta. ids: b70601 arma, 04 cargador, 05 pieza; preset/oferta a (con mira S&B PM II 3-12x50 en montura Nightforce).
        private void AddSniper(Func<string, string> hbWeapon, string[] railScopes)
        {
            const string Axmc = "627e14b21713922ded6f2c15", AxGrip = "62811e2510e26c1f344e6554", AxMag = "628120fd5631d45211793c9f", AxBarrel = "628121434fa03b6b6c35dc6a",
                         AxCharge = "62811cd7308cb521f87a8f99", Mount34 = "5aa66a9be5b5b0214e506e89", Scope = "61714eec290d254f5e6b2ffc";
            const string k = "hw", Id = "6a1c00000000000000b706", Name = "Hawker HX", Short = "Hawker";
            string need = System.IO.Path.Combine(QuestIds.ModDir, "bundles", Bo7Guns.Bundles.Replace('/', System.IO.Path.DirectorySeparatorChar), k + "_pgrip.bundle");
            if (!System.IO.File.Exists(need)) { Console.WriteLine("[Tarkova-Cola] BO7: " + Name + " sin bundles (" + k + "_pgrip.bundle): no se anade"); return; }
            string gun = Id + "01", mag = Id + "04", body = Id + "05";
            Clone(Axmc, gun, "tarkovacola_" + k, null, Name + " .338 LM bolt-action sniper rifle", "Fusil de francotirador de cerrojo " + Name + " .338 LM", Short,
                "The Hawker HX is a bolt-action precision rifle in .338 Lapua Magnum with a skeletonised chassis, a fluted heavy barrel and an adjustable stock built into one rigid frame. The bolt is short and smooth enough to cycle without breaking your cheek weld, and a single well-placed round ends most arguments at any distance Tarkov has to offer.",
                150000, hbWeapon(Axmc),
                "El Hawker HX es un fusil de precisión de cerrojo en .338 Lapua Magnum, con chasis esqueletizado, cañón pesado estriado y culata ajustable formando un solo bloque rígido. El cerrojo es corto y suave: se monta sin despegar la cara de la culata, y un solo disparo bien puesto zanja casi cualquier discusión a cualquier distancia que ofrezca Tarkov.");
            var g = T(gun).Properties;
            g.Ergonomics = 38; g.RecoilForceUp = 430; g.RecoilForceBack = 820;
            Clone(AxGrip, body, "tarkovacola_" + k + "_body", k + "_pgrip", Name + " chassis, barrel and stock", "Chasis, cañón y culata " + Name, Short,
                "Receiver, fluted barrel, grip and stock of the " + Name + " in one chassis, with a top rail for optics.", 45000, hbWeapon(AxGrip),
                "Receptor, cañón estriado, empuñadura y culata del " + Name + " en un solo chasis, con riel superior para miras.");
            Clone(AxMag, mag, "tarkovacola_" + k + "_mag", k + "_mag", Name + " .338 LM magazine", "Cargador .338 LM " + Name, Short,
                "Box magazine of the " + Name + ".", 6000, hbWeapon(AxMag), "Cargador de caja del " + Name + ".");
            var bp = T(body).Properties;
            bp.Slots = new List<Slot>();
            bp.ExtraSizeLeft = 4; bp.ExtraSizeRight = 1; bp.ExtraSizeForceAdd = false;
            // pieza: mira de riel, bocachas de la AXMC (opcionales: el canon de BO7 ya trae freno) y linterna
            CopySlots("5bb20d53d4351e4502010a69", body, "mod_scope");
            KeepSlots(body, new Dictionary<string, string[]> { ["mod_scope"] = railScopes });
            CopySlots(AxBarrel, body, "mod_muzzle");
            CopySlots(QuadRail, body, "mod_tactical_001");
            foreach (var sl in T(body).Properties.Slots) sl.Required = false;
            KeepSlots(gun, new Dictionary<string, string[]> { ["mod_pistol_grip"] = new[] { body }, ["mod_magazine"] = new[] { mag }, ["mod_charge"] = null });
            var parts = new List<(string slot, string tpl, int parent)>
            {
                ("mod_pistol_grip", body, 0), ("mod_magazine", mag, 0), ("mod_charge", AxCharge, 0), ("mod_scope", Mount34, 1), ("mod_scope", Scope, 4),
            };
            AddGun(gun, "6a1c00000000000000b7ca00", "6a1c00000000000000b7aa00", "6a1c00000000000000b7ba00", parts, 150000, "single");
        }

        // ---- M10 Breacher (sh_corso) sobre la Mossberg 590A1: el canon de BO7 lleva receptor, canon y bomba (y las ranuras de miras y
        // linterna); la culata va en su ranura y el tubo (cargador de 8 de la 590) es una pieza vacia. Sin guardamanos ni montura de la 590.
        // ids: b70501 arma, 03 canon, 04 tubo, 06 culata, 07 miras; preset/oferta 9.
        private void AddShotgun(Func<string, string> hbWeapon, string[] railScopes)
        {
            const string M590 = "5e870397991fd70db46995c8", M590Barrel = "5e87071478f43e51ca2de5e1", M590Stock = "5e87116b81c4ed43e83cefdd", M590Tube = "5e87080c81c4ed43e83cefda";
            const string k = "br", Id = "6a1c00000000000000b705", Name = "M10 Breacher", Short = "Breacher";
            string need = System.IO.Path.Combine(QuestIds.ModDir, "bundles", Bo7Guns.Bundles.Replace('/', System.IO.Path.DirectorySeparatorChar), k + "_barrel.bundle");
            if (!System.IO.File.Exists(need)) { Console.WriteLine("[Tarkova-Cola] BO7: " + Name + " sin bundles (" + k + "_barrel.bundle): no se anade"); return; }
            string gun = Id + "01", barrel = Id + "03", tube = Id + "04", stock = Id + "06", sight = Id + "07";
            Clone(M590, gun, "tarkovacola_" + k, null, Name + " 12ga pump-action shotgun", "Escopeta de corredera " + Name + " 12ga", Short,
                "The M10 Breacher is a compact 12-gauge pump-action shotgun built for kicking doors and clearing rooms. A short barrel, a slim railed forend and a fixed stock keep it fast to swing in tight corridors, and every pump racks the next shell with a satisfying, final-sounding clack. Hits like a truck up close and falls off quickly past the end of the hallway.",
                85000, hbWeapon(M590),
                "La M10 Breacher es una escopeta compacta de corredera del calibre 12 pensada para reventar puertas y limpiar habitaciones. Su cañón corto, el guardamanos fino con rieles y la culata fija la hacen rápida de mover en pasillos estrechos, y cada bombeo mete el siguiente cartucho con un chasquido rotundo. Pega como un camión de cerca y pierde fuerza en cuanto el pasillo se acaba.");
            var g = T(gun).Properties;
            g.Ergonomics = 58; g.RecoilForceUp = 390; g.RecoilForceBack = 450;
            // las ranuras de arma del personaje (Default Inventory) no aceptan la clase Shotgun sino escopetas concretas (la 590 entre ellas):
            // sin esto no se podia equipar. Va en todas las ranuras que acepten la 590.
            int slotsAdded = 0;
            foreach (var t in _templates.Items.Values)
                foreach (var s in t.Properties?.Slots ?? Enumerable.Empty<Slot>())
                    foreach (var f in s.Properties?.Filters ?? Enumerable.Empty<SlotFilter>())
                        if (f.Filter != null && f.Filter.Contains(new MongoId(M590)) && f.Filter.Add(new MongoId(gun))) slotsAdded++;
            Console.WriteLine("[Tarkova-Cola] BO7: " + Name + " admitida en " + slotsAdded + " ranuras (las de la 590)");
            Clone(M590Barrel, barrel, "tarkovacola_" + k + "_barrel", k + "_barrel", Name + " receiver, barrel and forend", "Cajón, cañón y guardamanos " + Name, Short,
                "Receiver, barrel and pump forend of the " + Name + ", with a top rail.", 20000, hbWeapon(M590Barrel), "Cajón, cañón y guardamanos de corredera de la " + Name + ", con riel superior.");
            Clone(M590Tube, tube, "tarkovacola_" + k + "_mag", k + "_mag", Name + " 12ga 8-shell tube", "Tubo de 8 cartuchos " + Name + " 12ga", Short,
                "Under-barrel tube magazine of the " + Name + ".", 3000, hbWeapon(M590Tube), "Depósito tubular bajo el cañón de la " + Name + ".");
            Clone(M590Stock, stock, "tarkovacola_" + k + "_stock", k + "_stock", Name + " stock", "Culata " + Name, Short,
                "Fixed stock of the " + Name + ".", 6000, hbWeapon(M590Stock), "Culata fija de la " + Name + ".");
            Clone(Hk416RearSight, sight, "tarkovacola_" + k + "_sight_rear", k + "_sight_rear", Name + " iron sights", "Miras de hierro " + Name, Short,
                "Built-in ghost-ring sights of the " + Name + ". Lets you aim down the sights without an optic.", 2000, hbWeapon(Hk416RearSight),
                "Miras de anillo integradas de la " + Name + ". Permiten apuntar sin mira óptica.");
            T(stock).Properties.Slots = new List<Slot>();
            // canon: miras de riel, las de hierro y una linterna
            CopySlots("5bb20d53d4351e4502010a69", barrel, "mod_scope", "mod_sight_rear");
            KeepSlots(barrel, new Dictionary<string, string[]> { ["mod_scope"] = railScopes, ["mod_sight_rear"] = new[] { sight } });
            CopySlots(QuadRail, barrel, "mod_tactical_001");
            foreach (var sl in T(barrel).Properties.Slots) sl.Required = false;
            KeepSlots(gun, new Dictionary<string, string[]> { ["mod_barrel"] = new[] { barrel }, ["mod_magazine"] = new[] { tube }, ["mod_stock"] = new[] { stock } });
            var parts = new List<(string slot, string tpl, int parent)> { ("mod_barrel", barrel, 0), ("mod_magazine", tube, 0), ("mod_stock", stock, 0), ("mod_sight_rear", sight, 1) };
            AddGun(gun, "6a1c00000000000000b7c900", "6a1c00000000000000b7a900", "6a1c00000000000000b7b900", parts, 85000, "single");
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
        private void AddGun(string gun, string presetId, string presetRoot, string offerId, List<(string slot, string tpl, int parent)> parts, int price, string fireMode = "fullauto")
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
            offer[0].Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true, FireMode = new UpdFireMode { FireMode = fireMode } };   // de fabrica en automatico (la escopeta, tiro a tiro)
            assort.Items.AddRange(offer);
            assort.BarterScheme[offer[0].Id] = new List<List<BarterScheme>> { new List<BarterScheme> { new BarterScheme { Count = price, Template = new MongoId("5449016a4bdc2d6f028b456f") } } };
            assort.LoyalLevelItems[offer[0].Id] = Bo7Guns.Mechanic1;
        }
    }
}
