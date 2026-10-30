using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace TarkovaCola.Server
{
    // Re-roll de Vulture Aid: genera el botin nuevo de un contenedor con la tabla de botin estatico del mapa.
    // Coste: normal 1, Scav muerto 2, caja fuerte 5, aereo 7 (+0.5 por cada repeticion en el mismo contenedor).
    // Fallo: 15 / 25 / 35 / 40% (el cliente se queda con el botin original, pero el coste se paga igual).
    [Injectable(InjectionType.Singleton)]
    public class Reroll
    {
        private readonly LocationTable _locations;
        private readonly TemplateTable _templates;
        private readonly Random _rng = new Random();
        private const string Roubles = "5449016a4bdc2d6f028b456f";
        private const string AmmoParent = "5485a8684bdc2da71d8b4567";
        private const string MoneyParent = "543be5dd4bdc2deb348b4569";

        public Reroll(LocationTable locations, TemplateTable templates) { _locations = locations; _templates = templates; }

        public string Roll(RerollRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrEmpty(req.Tpl)) return Result("no", 0);
                var (cost, fail) = Price(req.Tpl);
                cost += 0.5 * Math.Max(0, req.Repeat);
                if (cost > req.Points + 1e-6) return Result("points", cost);
                if (_rng.NextDouble() < fail) return Result("fail", cost);

                var details = Table(req.Map, req.Tpl);
                if (details == null || details.ItemDistribution == null || !details.ItemDistribution.Any()) return Result("no", 0);
                int count = PickCount(details);
                var items = new List<object>();
                var dist = details.ItemDistribution.Where(i => (i.RelativeProbability ?? 0) > 0).ToList();
                double sum = dist.Sum(i => (double)(i.RelativeProbability ?? 0));
                for (int n = 0; n < count && sum > 0; n++)
                {
                    double r = _rng.NextDouble() * sum;
                    foreach (var i in dist)
                    {
                        r -= i.RelativeProbability ?? 0;
                        if (r > 0) continue;
                        items.Add(new { tpl = i.Tpl.ToString(), count = Stack(i.Tpl) });
                        break;
                    }
                }
                Console.WriteLine("[Tarkova-Cola] Re-roll en " + req.Map + " (" + req.Tpl + "): " + items.Count + " objetos, coste " + cost);
                return JsonSerializer.Serialize(new { status = "ok", cost, items });
            }
            catch (Exception e)
            {
                Console.WriteLine("[Tarkova-Cola] ERROR re-roll: " + e.Message);
                return Result("no", 0);
            }
        }

        private static string Result(string status, double cost)
        {
            return JsonSerializer.Serialize(new { status, cost, items = new object[0] });
        }

        private (double, double) Price(string tpl)
        {
            if (LootContainers.Safe.Contains(tpl)) return (5, 0.35);
            if (LootContainers.DeadScav.Contains(tpl)) return (2, 0.25);
            var t = _templates.Items.TryGetValue(new MongoId(tpl), out var item) ? item : null;
            if (t != null && t.Name != null && t.Name.Contains("airdrop")) return (7, 0.40);
            return (1, 0.15);
        }

        private StaticLootDetails Table(string map, string tpl)
        {
            var locations = _locations;
            foreach (var prop in locations.GetType().GetProperties())
            {
                if (prop.PropertyType != typeof(Location)) continue;
                var loc = prop.GetValue(locations) as Location;
                if (loc?.Base?.Id == null || !string.Equals(loc.Base.Id, map, StringComparison.OrdinalIgnoreCase)) continue;
                var table = loc.StaticLoot?.Value;
                if (table != null && table.TryGetValue(new MongoId(tpl), out var d)) return d;
            }
            // el mapa no tiene ese contenedor en su tabla (p. ej. un aereo): se busca en cualquier mapa
            foreach (var prop in locations.GetType().GetProperties())
            {
                if (prop.PropertyType != typeof(Location)) continue;
                var table = (prop.GetValue(locations) as Location)?.StaticLoot?.Value;
                if (table != null && table.TryGetValue(new MongoId(tpl), out var d)) return d;
            }
            return null;
        }

        private int PickCount(StaticLootDetails d)
        {
            var counts = d.ItemCountDistribution?.Where(c => (c.RelativeProbability ?? 0) > 0).ToList();
            if (counts == null || counts.Count == 0) return 3;
            double sum = counts.Sum(c => (double)(c.RelativeProbability ?? 0));
            double r = _rng.NextDouble() * sum;
            foreach (var c in counts)
            {
                r -= c.RelativeProbability ?? 0;
                if (r <= 0) return Math.Max(1, (int)(c.Count ?? 1));
            }
            return 3;
        }

        private int Stack(MongoId tpl)
        {
            if (!_templates.Items.TryGetValue(tpl, out var t)) return 1;
            string parent = t.Parent.ToString();
            if (parent == AmmoParent) return _rng.Next(20, 61);
            if (tpl.ToString() == Roubles) return _rng.Next(5, 51) * 1000;
            if (parent == MoneyParent) return _rng.Next(20, 301);
            return 1;
        }
    }

    public class RerollRequest : IRequestData
    {
        [JsonPropertyName("tpl")] public string Tpl { get; set; }
        [JsonPropertyName("map")] public string Map { get; set; }
        [JsonPropertyName("repeat")] public int Repeat { get; set; }
        [JsonPropertyName("points")] public double Points { get; set; }
    }
}
