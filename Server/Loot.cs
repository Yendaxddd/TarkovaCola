using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace TarkovaCola.Server
{
    // Speed Cola aparece sola en el mundo: cajas de municion y cajas fuertes (contenedores estaticos) y en el botin de Rogues y Killa.
    // Cada valor es la probabilidad aproximada de que UN objeto sacado de ese contenedor / bolsa sea una Speed Cola.
    internal static class LootChances
    {
        internal const string AmmoCrate = "5909e4b686f7747f5b744fa4";      // Ammo crate
        internal const string Safe = "578f8778245977358849a9b5";           // Safe

        internal const double AmmoCrateChance = 0.012;
        internal const double SafeChance = 0.06;
        internal const double RogueChance = 0.04;   // por objeto de mochila de un Rogue (exUsec)
        internal const double KillaChance = 0.15;   // por objeto de mochila de Killa (lleva muy pocos)
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 1)]
    public class TarkovaColaLoot : IOnLoad
    {
        private readonly LocationTable _locations;
        private readonly BotTable _bots;

        public TarkovaColaLoot(LocationTable locations, BotTable bots)
        {
            _locations = locations;
            _bots = bots;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try { HookStaticLoot(); } catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR loot estatico: " + e); }
            try { HookBots(); } catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR loot de bots: " + e); }
            return Task.CompletedTask;
        }

        // Probabilidad relativa que hay que anadir a un reparto cuya suma es 'sum' para que el nuevo objeto salga con probabilidad 'p'.
        private static double Weight(double sum, double p) { return Math.Max(1.0, Math.Round(sum * p / (1.0 - p))); }

        private void HookStaticLoot()
        {
            var cola = new MongoId(Perks.SpeedColaId);
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
                    Add(table, LootChances.AmmoCrate, LootChances.AmmoCrateChance, cola);
                    Add(table, LootChances.Safe, LootChances.SafeChance, cola);
                    return table;
                });
            }
            if (Environment.GetEnvironmentVariable("TARKOVACOLA_CHECK") == "1")
            {
                var t = _locations.Bigmap.StaticLoot.Value;
                foreach (var c in new[] { LootChances.AmmoCrate, LootChances.Safe })
                {
                    var d = t[new MongoId(c)].ItemDistribution.ToList();
                    double sum = d.Sum(i => i.RelativeProbability ?? 0), me = d.Where(i => i.Tpl == cola).Sum(i => i.RelativeProbability ?? 0);
                    Console.WriteLine("[Tarkova-Cola][check] " + c + ": cola " + me + " de " + sum + " = " + (100 * me / sum).ToString("0.00") + "%");
                }
            }
            Console.WriteLine("[Tarkova-Cola] Speed Cola anadida al loot estatico de " + maps + " mapas (cajas de municion y cajas fuertes)");
        }

        private static void Add(Dictionary<MongoId, StaticLootDetails> table, string container, double chance, MongoId cola)
        {
            if (!table.TryGetValue(new MongoId(container), out var details) || details?.ItemDistribution == null) return;
            var list = details.ItemDistribution.ToList();
            if (list.Any(i => i.Tpl == cola)) return;
            double sum = list.Sum(i => i.RelativeProbability ?? 0);
            list.Add(new ItemDistribution { Tpl = cola, RelativeProbability = (float)Weight(sum, chance) });
            details.ItemDistribution = list;
        }

        private void HookBots()
        {
            var cola = new MongoId(Perks.SpeedColaId);
            var done = new List<string>();
            foreach (var (name, chance) in new[] { ("exusec", LootChances.RogueChance), ("bosskilla", LootChances.KillaChance) })
            {
                if (!_bots.Types.TryGetValue(name, out var bot) || bot?.BotInventory?.Items?.Backpack == null) continue;
                var pool = bot.BotInventory.Items.Backpack;
                double sum = pool.Values.Sum();
                pool[cola] = Weight(sum, chance);
                done.Add(name);
            }
            Console.WriteLine("[Tarkova-Cola] Speed Cola anadida al botin de: " + string.Join(", ", done));
        }
    }
}
