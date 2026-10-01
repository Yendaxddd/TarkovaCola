using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;

namespace TarkovaCola.Server
{
    // Recompensas de los logros. El cliente decide cuando se consigue un logro y lo reclama aqui; el servidor
    // las entrega UNA sola vez por perfil (por correo de Therapist) y recuerda cuales ya se entregaron.
    internal static class AchievementRewards
    {
        internal class Reward
        {
            public string En, Es;
            public (string Tpl, int Count)[] Items;
        }

        private const string Roubles = "5449016a4bdc2d6f028b456f";

        internal static readonly Dictionary<string, Reward> All = new Dictionary<string, Reward>
        {
            ["a_first"] = new Reward { En = "Tastes...sweet?",                 Es = "Sabe...dulce?",                    Items = new[] { (Roubles, 25000) } },
            ["a_lvl5"]  = new Reward { En = "As long as no one gargles balls with this...", Es = "Mientras nadie se gargarice con esto...", Items = new[] { (Perks.GpCoin, 4) } },
            ["a_all"]   = new Reward { En = "Speed is EVERYTHING.",            Es = "La velocidad lo es TODO.",         Items = new[] { (Perks.SpeedColaId, 3) } },
            ["a_found"] = new Reward { En = "...what is this?",                Es = "...que es esto?",                  Items = new[] { (Perks.SpeedColaId, 1), (Roubles, 15000) } },
            ["a_die"]   = new Reward { En = "Speeds up your life, literally.", Es = "Acelera tu vida, literalmente.",   Items = new[] { (Perks.SpeedColaId, 1) } },
            ["a_hand"]  = new Reward { En = "Master of hand-tricks",           Es = "Maestro de los trucos de mano",    Items = new[] { (Perks.GpCoin, 5) } },
        };
    }

    [Injectable(InjectionType.Singleton)]
    public class AchievementStore
    {
        private readonly MailSendService _mail;
        private readonly string _dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(AchievementStore).Assembly.Location) ?? ".", "profiles");
        private readonly object _lock = new object();

        public AchievementStore(MailSendService mail) { _mail = mail; }

        private string PathFor(string sessionId)
        {
            var safe = new string(Array.FindAll(sessionId.ToCharArray(), char.IsLetterOrDigit));
            return System.IO.Path.Combine(_dir, safe + ".achievements.json");
        }

        private HashSet<string> LoadClaimed(string path)
        {
            try { return File.Exists(path) ? new HashSet<string>(JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? new List<string>()) : new HashSet<string>(); }
            catch { return new HashSet<string>(); }
        }

        // Entrega las recompensas de los ids que aun no se hayan entregado. Devuelve los ids entregados ahora.
        public List<string> Claim(MongoId sessionId, IEnumerable<string> ids)
        {
            var granted = new List<string>();
            lock (_lock)
            {
                Directory.CreateDirectory(_dir);
                var path = PathFor(sessionId.ToString());
                var claimed = LoadClaimed(path);

                foreach (var id in ids.Distinct())
                {
                    if (id == null || claimed.Contains(id) || !AchievementRewards.All.TryGetValue(id, out var r)) continue;

                    var items = new List<Item>();
                    foreach (var (tpl, count) in r.Items)
                    {
                        bool stackable = tpl == Perks.GpCoin || tpl == "5449016a4bdc2d6f028b456f";
                        int stacks = stackable ? 1 : count;
                        for (int i = 0; i < stacks; i++)
                            items.Add(new Item
                            {
                                Id = new MongoId(),
                                Template = new MongoId(tpl),
                                ParentId = new MongoId().ToString(),
                                SlotId = "hideout",
                                Upd = new Upd { StackObjectsCount = stackable ? count : 1 },
                            });
                    }

                    _mail.SendDirectNpcMessageToPlayer(sessionId, Traders.THERAPIST, MessageType.MessageWithItems,
                        "Tarkova-Cola - Achievement / Logro: \"" + r.En + "\" / \"" + r.Es + "\"", items, 72 * 3600);
                    claimed.Add(id);
                    granted.Add(id);
                    Console.WriteLine("[Tarkova-Cola] Logro '" + id + "' entregado a " + sessionId);
                }

                if (granted.Count > 0)
                {
                    var tmp = path + ".tmp";
                    File.WriteAllText(tmp, JsonSerializer.Serialize(claimed.ToList()));
                    File.Move(tmp, path, overwrite: true);
                }
            }
            return granted;
        }
    }

    public class AchRequest : SPTarkov.Server.Core.Models.Utils.IRequestData
    {
        [JsonPropertyName("ids")] public List<string> Ids { get; set; }
    }
}
