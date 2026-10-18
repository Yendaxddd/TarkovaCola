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
        public double AmmoBox { get; set; } = 0.024;          // cajas de municion de madera: todas
        public double WoodenCrate { get; set; } = 0.03;       // cajas de madera: todas
        public double GrenadeBox { get; set; } = 0.08;        // cajas de granadas: Widow's Wine y PHD Flopper
        public double RationCrate { get; set; } = 0.06;       // cajas de raciones: Juggernog
        public double PmcBody { get; set; } = 0.03;           // cuerpos de PMC del mapa: todas
        public double Safe { get; set; } = 0.45;              // cajas fuertes: casi seguro que sale alguna
        public int BossPerks { get; set; } = 2;               // jefes: siempre llevan este numero de perks distintas
        public double PmcChance { get; set; } = 0.06;         // PMC con vida
        public double ScavChance { get; set; } = 0.01;        // Scavs
        public double RogueChance { get; set; } = 0.08;       // Rogues
    }

    internal static class LootContainers
    {
        internal static readonly string[] AmmoBox = { "5909d45286f77465a8136dc6", "67adf4b81c58bd68b2002fec" };
        internal static readonly string[] WoodenCrate = { "578f87ad245977356274f2cc", "67adf4db515e3dd542077a1d" };
        internal static readonly string[] GrenadeBox = { "5909d36d86f774660f0bb900", "67adf4eb110ba15da90c6413" };
        internal static readonly string[] RationCrate = { "5d6fd13186f77424ad2a8c69" };
        internal static readonly string[] PmcBody = { "6582e6d7b14c3f72eb071420" };
        internal static readonly string[] Safe = { "578f8782245977354405a1e3", "64d11702dd0cd96ab82c3280", "66acff0a1d8e1083b303f5af" };

        internal static LootSettings Settings = new LootSettings();
        internal static BotGeneratorHelper BotHelper;
        internal static readonly Random Rng = new Random();
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 1)]
    public class TarkovaColaLoot : IOnLoad
    {
        private readonly LocationTable _locations;

        public TarkovaColaLoot(LocationTable locations, BotGeneratorHelper botHelper)
        {
            _locations = locations;
            LootContainers.BotHelper = botHelper;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            LoadSettings();
            try { HookStaticLoot(); } catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR loot estatico: " + e); }
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
                if (File.Exists(path)) LootContainers.Settings = JsonSerializer.Deserialize<LootSettings>(File.ReadAllText(path), opts) ?? new LootSettings();
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
                (LootContainers.RationCrate, new[] { Perks.Juggernog }, s.RationCrate),
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
            Console.WriteLine("[Tarkova-Cola] Perks anadidas al loot estatico de " + maps + " mapas (municion, madera, granadas, raciones, cuerpos de PMC y cajas fuertes)");
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
