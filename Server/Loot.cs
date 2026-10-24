using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators.Loot;
using SPTarkov.Server.Core.Helpers.Bot;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace TarkovaCola.Server
{
    // Probabilidades del botin. Se pueden cambiar en user/mods/TarkovaCola/loot.json (se crea con estos valores si no existe).
    // Contenedores: probabilidad TOTAL de que un objeto sacado del contenedor sea una perk (se reparte entre las perks que caben ahi).
    // Bots: probabilidad de que el bot lleve una perk encima al aparecer.
    internal class LootSettings
    {
        internal const int CurrentVersion = 3;               // al subirla, un loot.json mas viejo se cambia por los valores nuevos
        public int Version { get; set; } = CurrentVersion;
        public double AmmoBox { get; set; } = 0.04;           // cajas de municion de madera: todas
        public double WoodenCrate { get; set; } = 0.05;       // cajas de madera: todas
        public double GrenadeBox { get; set; } = 0.10;        // cajas de granadas: Widow's Wine y PHD Flopper
        public double RationCrate { get; set; } = 0.10;       // cajas de raciones: Juggernog y Stamin-Up
        public double MedCase { get; set; } = 0.06;           // botiquines y bolsas medicas: Juggernog y PHD Flopper
        public double WeaponBox { get; set; } = 0.05;         // cajas de armas: Deadshot y Speed Cola
        public double DuffleBag { get; set; } = 0.03;         // bolsas de deporte: todas
        public double DeadScav { get; set; } = 0.03;          // Scavs muertos del mapa: todas
        public double Cache { get; set; } = 0.12;             // alijos enterrados y barriles: todas
        public double Drawer { get; set; } = 0.01;            // cajones y chaquetas: todas (poco, hay muchos)
        public double PmcBody { get; set; } = 0.06;           // cuerpos de PMC del mapa: todas
        public double Safe { get; set; } = 0.45;              // cajas fuertes: casi seguro que sale alguna
        public double LooseDrinks { get; set; } = 0.04;       // sueltas por el mapa, donde pueden salir bebidas (Tar-Cola, bebidas energeticas...)
        public int BossPerks { get; set; } = 2;               // jefes: siempre llevan este numero de perks distintas
        public double PmcChance { get; set; } = 0.10;         // PMC con vida
        public double ScavChance { get; set; } = 0.03;        // Scavs
        public double RogueChance { get; set; } = 0.12;       // Rogues
    }

    internal static class LootContainers
    {
        internal static readonly string[] AmmoBox = { "5909d45286f77465a8136dc6", "67adf4b81c58bd68b2002fec" };
        internal static readonly string[] WoodenCrate = { "578f87ad245977356274f2cc", "67adf4db515e3dd542077a1d" };
        internal static readonly string[] GrenadeBox = { "5909d36d86f774660f0bb900", "67adf4eb110ba15da90c6413" };
        internal static readonly string[] RationCrate = { "5d6fd13186f77424ad2a8c69" };
        internal static readonly string[] PmcBody = { "6582e6d7b14c3f72eb071420" };
        internal static readonly string[] MedCase = { "5909d4c186f7746ad34e805a", "5909d24f86f77466f56e6855", "61aa1ead84ea0800645777fd", "5d6fe50986f77449d97f7463" };
        internal static readonly string[] WeaponBox = { "5909d5ef86f77467974efbd8", "5909d76c86f77471e53d2adf", "5909d7cf86f77470ee57d75a", "5909d89086f77472591234a0", "61aa1e6984ea0800645777f9" };
        internal static readonly string[] DuffleBag = { "578f87a3245977356274f2cb", "61aa1e9a32a4743c3453d2cf" };
        internal static readonly string[] DeadScav = { "5909e4b686f7747f5b744fa4", "6582e6bb0c3b9823fe6d1840", "67adf5752fc5ee84020a9940" };
        internal static readonly string[] Cache = { "5d6d2b5486f774785c2ba8ea", "5d6d2bb386f774785b07a77a" };
        internal static readonly string[] Drawer = { "578f87b7245977356274f2cd", "578f8778245977358849a9b5", "67adf4a95247ac91530fcec7" };
        internal const string DrinksParent = "5448e8d64bdc2dce718b4568";
        internal static readonly string[] Safe = { "578f8782245977354405a1e3", "64d11702dd0cd96ab82c3280", "66acff0a1d8e1083b303f5af" };

        internal static LootSettings Settings = new LootSettings();
        internal static BotGeneratorHelper BotHelper;
        internal static readonly Random Rng = new Random();
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 1)]
    public class TarkovaColaLoot : IOnLoad
    {
        private readonly LocationTable _locations;
        private readonly TemplateTable _templates;

        public TarkovaColaLoot(LocationTable locations, BotGeneratorHelper botHelper, TemplateTable templates)
        {
            _locations = locations;
            _templates = templates;
            LootContainers.BotHelper = botHelper;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            LoadSettings();
            try { HookStaticLoot(); } catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR loot estatico: " + e); }
            try { HookLooseLoot(); } catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR loot suelto: " + e); }
            try { new BotPerksPatch().Enable(); Console.WriteLine("[Tarkova-Cola] Perks en bots: jefes x" + LootContainers.Settings.BossPerks + ", PMC " + LootContainers.Settings.PmcChance * 100 + "%, Scavs " + LootContainers.Settings.ScavChance * 100 + "%, Rogues " + LootContainers.Settings.RogueChance * 100 + "%"); }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR perks en bots: " + e); }
            return Task.CompletedTask;
        }

        private static void LoadSettings()
        {
            var path = System.IO.Path.Combine(QuestIds.ModDir, "loot.json");
            var opts = new JsonSerializerOptions { WriteIndented = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
            try
            {
                if (File.Exists(path))
                {
                    var raw = File.ReadAllText(path);
                    var read = JsonSerializer.Deserialize<LootSettings>(raw, opts) ?? new LootSettings();
                    int fileVersion = 0;                  // (sin la clave en el archivo, la propiedad tendria el valor por defecto)
                    using (var doc = JsonDocument.Parse(raw, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }))
                        if (doc.RootElement.TryGetProperty("Version", out var ver) && ver.TryGetInt32(out var vv)) fileVersion = vv;
                    if (fileVersion < LootSettings.CurrentVersion)
                    {
                        File.Copy(path, path + ".old", true);
                        Console.WriteLine("[Tarkova-Cola] loot.json era de una version anterior: se usan las probabilidades nuevas (el viejo queda en loot.json.old)");
                        read = new LootSettings();
                    }
                    LootContainers.Settings = read;
                }
                File.WriteAllText(path, JsonSerializer.Serialize(LootContainers.Settings, opts));      // anade las claves nuevas que falten
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] loot.json no se pudo leer, se usan los valores por defecto: " + e.Message); }
        }

        // Probabilidad relativa que hay que anadir a un reparto cuya suma es 'sum' para que el nuevo objeto salga con probabilidad 'p'.
        private static double Weight(double sum, double p) { return Math.Max(1.0, Math.Round(sum * p / (1.0 - p))); }

        private void HookStaticLoot()
        {
            var s = LootContainers.Settings;
            var all = Perks.All;
            var rules = new (string[] Containers, PerkDef[] Perks, double Total)[]
            {
                (LootContainers.AmmoBox, all, s.AmmoBox),
                (LootContainers.WoodenCrate, all, s.WoodenCrate),
                (LootContainers.GrenadeBox, new[] { Perks.WidowsWine, Perks.PhdFlopper }, s.GrenadeBox),
                (LootContainers.RationCrate, new[] { Perks.Juggernog, Perks.StaminUp }, s.RationCrate),
                (LootContainers.MedCase, new[] { Perks.Juggernog, Perks.PhdFlopper }, s.MedCase),
                (LootContainers.WeaponBox, new[] { Perks.Deadshot, Perks.SpeedCola }, s.WeaponBox),
                (LootContainers.DuffleBag, all, s.DuffleBag),
                (LootContainers.DeadScav, all, s.DeadScav),
                (LootContainers.Cache, all, s.Cache),
                (LootContainers.Drawer, all, s.Drawer),
                (LootContainers.PmcBody, all, s.PmcBody),
                (LootContainers.Safe, all, s.Safe),
            };
            int maps = 0;
            foreach (var prop in typeof(LocationTable).GetProperties())
            {
                if (prop.PropertyType != typeof(Location)) continue;
                var loc = prop.GetValue(_locations) as Location;
                if (loc == null || loc.StaticLoot == null) continue;
                maps++;
                // StaticLoot se carga bajo demanda y puede recargarse: el transformador se aplica cada vez.
                loc.StaticLoot.AddTransformer(table =>
                {
                    foreach (var rule in rules)
                        foreach (var container in rule.Containers)
                            Add(table, container, rule.Perks, rule.Total);
                    return table;
                });
            }
            Console.WriteLine("[Tarkova-Cola] Perks anadidas al loot estatico de " + maps + " mapas (" + rules.Length + " tipos de contenedor)");
        }

        // Botin suelto: en cada punto del mapa donde puede salir una bebida, una perk puede salir en su lugar
        private void HookLooseLoot()
        {
            double total = LootContainers.Settings.LooseDrinks;
            if (total <= 0) return;
            var drinks = new HashSet<MongoId>(_templates.Items.Values.Where(t => t.Parent == new MongoId(LootContainers.DrinksParent)).Select(t => t.Id));
            var perkTpls = new HashSet<MongoId>(Perks.All.Select(p => new MongoId(p.ItemId)));
            int maps = 0;
            foreach (var prop in typeof(LocationTable).GetProperties())
            {
                if (prop.PropertyType != typeof(Location)) continue;
                var loc = prop.GetValue(_locations) as Location;
                if (loc == null || loc.LooseLoot == null) continue;
                maps++;
                string mapName = prop.Name;
                loc.LooseLoot.AddTransformer(loose =>
                {
                    int points = 0;
                    foreach (var sp in loose?.Spawnpoints ?? Enumerable.Empty<Spawnpoint>())
                    {
                        var items = sp.Template?.Items?.ToList();
                        var dist = sp.ItemDistribution?.ToList();
                        if (items == null || dist == null || sp.Template.IsContainer == true) continue;
                        if (!items.Any(i => i.ParentId == null && drinks.Contains(i.Template))) continue;
                        if (items.Any(i => perkTpls.Contains(i.Template))) continue;          // ya tratado (el transformador puede repetirse)
                        double sum = dist.Sum(d => d.RelativeProbability ?? 0);
                        if (sum <= 0) continue;
                        double each = Math.Max(1.0, Math.Round(Weight(sum, total) / Perks.All.Length));
                        foreach (var p in Perks.All)
                        {
                            string key = "tarkovacola_" + p.Id;
                            items.Add(new SptLootItem { Id = new MongoId(), Template = new MongoId(p.ItemId), ComposedKey = key, Upd = new Upd { StackObjectsCount = 1 } });
                            dist.Add(new LooseLootItemDistribution { ComposedKey = new ComposedKey { Key = key }, RelativeProbability = each });
                        }
                        sp.Template.Items = items;
                        sp.ItemDistribution = dist;
                        points++;
                    }
                    Console.WriteLine("[Tarkova-Cola] " + mapName + ": perks sueltas posibles en " + points + " puntos de bebidas");
                    return loose;
                });
            }
        }

        // Anade las perks al reparto del contenedor: entre todas suman 'total' de probabilidad por objeto sacado.
        private static void Add(Dictionary<MongoId, StaticLootDetails> table, string container, PerkDef[] perks, double total)
        {
            if (total <= 0 || perks.Length == 0) return;
            if (!table.TryGetValue(new MongoId(container), out var details) || details?.ItemDistribution == null) return;
            var list = details.ItemDistribution.Where(i => !perks.Any(p => i.Tpl == new MongoId(p.ItemId))).ToList();
            double sum = list.Sum(i => i.RelativeProbability ?? 0);
            if (sum <= 0) return;
            double each = Weight(sum, Math.Min(0.95, total)) / perks.Length;
            foreach (var p in perks)
                list.Add(new ItemDistribution { Tpl = new MongoId(p.ItemId), RelativeProbability = (float)Math.Max(1.0, Math.Round(each)) });
            details.ItemDistribution = list;
        }
    }

    // Despues de generar el botin de cada bot: jefes con perks garantizadas, PMC / Scavs / Rogues con una probabilidad
    public class BotPerksPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod() { return typeof(BotLootGenerator).GetMethod(nameof(BotLootGenerator.GenerateLoot)); }

        private static readonly HashSet<EquipmentSlots> Slots = new HashSet<EquipmentSlots> { EquipmentSlots.Backpack, EquipmentSlots.TacticalVest, EquipmentSlots.Pockets };
        private static readonly string[] Scavs = { "assault", "cursedassault", "marksman" };
        private static readonly string[] BossFollowers = { "followerbigpipe", "followerbirdeye" };     // los Goons (Knight es "bossknight")

        [PatchPostfix]
        public static void Postfix(MongoId botId, BotGenerationDetails botGenerationDetails, BotBaseInventory botInventory)
        {
            try
            {
                var s = LootContainers.Settings;
                string role = botGenerationDetails?.RoleLowercase ?? "";
                int count = 0;
                if ((role.StartsWith("boss") && role != "bosstest") || BossFollowers.Contains(role)) count = s.BossPerks;
                else if (botGenerationDetails.IsPmc) count = Roll(s.PmcChance);
                else if (role == "exusec") count = Roll(s.RogueChance);
                else if (Scavs.Contains(role)) count = Roll(s.ScavChance);
                if (count <= 0 || LootContainers.BotHelper == null) return;

                var pool = Perks.All.OrderBy(_ => LootContainers.Rng.Next()).Take(count);
                foreach (var perk in pool)
                {
                    var id = new MongoId();
                    var tpl = new MongoId(perk.ItemId);
                    var item = new Item { Id = id, Template = tpl };
                    var r = LootContainers.BotHelper.AddItemWithChildrenToEquipmentSlot(botId, Slots, id, tpl, new List<Item> { item }, botInventory);
                    if (r != ItemAddedResult.SUCCESS) Console.WriteLine("[Tarkova-Cola] No cupo " + perk.Id + " en " + role + ": " + r);
                }
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR anadiendo perks a un bot: " + e.Message); }
        }

        private static int Roll(double chance) { lock (LootContainers.Rng) return LootContainers.Rng.NextDouble() < chance ? 1 : 0; }
    }
}
