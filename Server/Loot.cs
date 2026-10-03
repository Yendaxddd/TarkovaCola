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
    // Las perks aparecen solas en el mundo: cajas de municion y cajas fuertes (contenedores estaticos) y en el botin de Rogues y Killa.
    // Las probabilidades de cada perk estan en Perks.All (PerkDef).
    internal static class LootContainers
    {
        internal const string AmmoCrate = "5909e4b686f7747f5b744fa4";      // Ammo crate
        internal const string Safe = "578f8778245977358849a9b5";           // Safe
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
                    foreach (var perk in Perks.All)
                    {
                        var tpl = new MongoId(perk.ItemId);
                        Add(table, LootContainers.AmmoCrate, perk.AmmoCrate, tpl);
                        Add(table, LootContainers.Safe, perk.Safe, tpl);
                    }
                    return table;
                });
            }
            Console.WriteLine("[Tarkova-Cola] Perks anadidas al loot estatico de " + maps + " mapas (cajas de municion y cajas fuertes)");
        }

        private static void Add(Dictionary<MongoId, StaticLootDetails> table, string container, double chance, MongoId perk)
        {
            if (chance <= 0) return;
            if (!table.TryGetValue(new MongoId(container), out var details) || details?.ItemDistribution == null) return;
            var list = details.ItemDistribution.ToList();
            if (list.Any(i => i.Tpl == perk)) return;
            double sum = list.Sum(i => i.RelativeProbability ?? 0);
            list.Add(new ItemDistribution { Tpl = perk, RelativeProbability = (float)Weight(sum, chance) });
            details.ItemDistribution = list;
        }

        private void HookBots()
        {
            var done = new List<string>();
            foreach (var (bot, pick) in new (string, Func<PerkDef, double>)[] { ("exusec", p => p.Rogue), ("bosskilla", p => p.Killa) })
            {
                if (!_bots.Types.TryGetValue(bot, out var type) || type?.BotInventory?.Items?.Backpack == null) continue;
                var pool = type.BotInventory.Items.Backpack;
                foreach (var perk in Perks.All)
                {
                    double chance = pick(perk);
                    if (chance <= 0) continue;
                    pool[new MongoId(perk.ItemId)] = Weight(pool.Values.Sum(), chance);
                }
                done.Add(bot);
            }
            Console.WriteLine("[Tarkova-Cola] Perks anadidas al botin de: " + string.Join(", ", done));
        }
    }
}
