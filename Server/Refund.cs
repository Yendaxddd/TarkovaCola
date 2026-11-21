using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Commerce;

namespace TarkovaCola.Server
{
    // Devolucion por correo de items que el cliente pide devolver:
    //   - granadas (mejora Widow's Bite de Widow's Wine): el cliente avisa cuando una granada suya mata a un enemigo;
    //   - botellas de perk (modo Classic): si se bebe una perk que no cabe (ya activa, o 5 perks + modificador) o, mas adelante,
    //     la Eternal Perk al morir.
    // El servidor solo acepta esas dos clases de item y como mucho 2 unidades por peticion.
    [Injectable(InjectionType.Singleton)]
    public class RefundStore
    {
        private const string ThrowWeapBase = "543be6564bdc2df4348b4568";     // clase base de las armas arrojadizas (granadas)
        private readonly MailSendService _mail;
        private readonly TemplateTable _templates;
        private readonly object _lock = new object();

        public RefundStore(MailSendService mail, TemplateTable templates)
        {
            _mail = mail;
            _templates = templates;
        }

        public bool Send(MongoId sessionId, string tpl, int count)
        {
            if (string.IsNullOrEmpty(tpl) || count < 1) return false;
            count = Math.Min(count, 2);
            lock (_lock)
            try
            {
                bool perk = Perks.All.Any(p => p.ItemId == tpl);
                var id = new MongoId(tpl);
                if (!perk && (!_templates.Items.TryGetValue(id, out var item) || item.Parent != new MongoId(ThrowWeapBase)))
                {
                    Console.WriteLine("[Tarkova-Cola] Devolucion rechazada (ni una perk ni una granada): " + tpl);
                    return false;
                }
                var items = new List<Item>();
                for (int i = 0; i < count; i++)
                    items.Add(new Item { Id = new MongoId(), Template = id, Upd = new Upd { StackObjectsCount = 1 } });
                _mail.SendSystemMessageToPlayer(sessionId,
                    perk ? "Tarkova-Cola: tu perk ha vuelto. / Your perk came back."
                         : "Widow's Bite (Tarkova-Cola): tu granada ha vuelto. / Your grenade came back.",
                    items, 7 * 24 * 3600L);
                Console.WriteLine("[Tarkova-Cola] " + (perk ? "Perk" : "Granada") + " devuelta a " + sessionId + ": " + tpl + " x" + count);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[Tarkova-Cola] Devolucion rechazada (" + tpl + "): " + e.Message);
                return false;
            }
        }
    }

    // Ofrenda (Tombstone Soda, 3.1): si mueres con la mejora, lo que llevabas en los bolsillos y el chaleco te llega por correo.
    // El cliente manda la lista [{tpl,count}]; aqui se valida: plantillas que existen, nada de objetos de mision, pilas dentro de su
    // tamano maximo, como mucho MaxOfferingItems objetos, y una sola ofrenda cada pocos minutos por perfil.
    [Injectable(InjectionType.Singleton)]
    public class OfferingStore
    {
        private const int MaxOfferingItems = 40;
        private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(5);
        private readonly MailSendService _mail;
        private readonly TemplateTable _templates;
        private readonly Dictionary<string, DateTime> _last = new Dictionary<string, DateTime>();
        private readonly object _lock = new object();

        public OfferingStore(MailSendService mail, TemplateTable templates)
        {
            _mail = mail;
            _templates = templates;
        }

        public bool Send(MongoId sessionId, string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            lock (_lock)
            try
            {
                string key = sessionId.ToString();
                if (_last.TryGetValue(key, out var at) && DateTime.UtcNow - at < Cooldown)
                {
                    Console.WriteLine("[Tarkova-Cola] Ofrenda rechazada (demasiado pronto): " + key);
                    return false;
                }
                var list = System.Text.Json.JsonSerializer.Deserialize<List<OfferingEntry>>(json) ?? new List<OfferingEntry>();
                var items = new List<Item>();
                foreach (var e in list)
                {
                    if (items.Count >= MaxOfferingItems) break;
                    if (string.IsNullOrEmpty(e?.Tpl) || e.Count < 1 || !MongoId.IsValidMongoId(e.Tpl)) continue;
                    var id = new MongoId(e.Tpl);
                    if (!_templates.Items.TryGetValue(id, out var tpl) || tpl.Properties == null || tpl.Properties.QuestItem == true) continue;
                    int max = Math.Max(1, tpl.Properties.StackMaxSize ?? 1);
                    items.Add(new Item { Id = new MongoId(), Template = id, Upd = new Upd { StackObjectsCount = Math.Min(e.Count, max) } });
                }
                if (items.Count == 0) return false;
                _last[key] = DateTime.UtcNow;
                _mail.SendSystemMessageToPlayer(sessionId,
                    "Ofrenda (Tarkova-Cola): lo que llevabas en los bolsillos y el chaleco. / Offering (Tarkova-Cola): what you carried in your pockets and rig.",
                    items, 7 * 24 * 3600L);
                Console.WriteLine("[Tarkova-Cola] Ofrenda enviada a " + key + ": " + items.Count + " objeto(s)");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[Tarkova-Cola] Ofrenda rechazada: " + e.Message);
                return false;
            }
        }
    }

    public class OfferingEntry
    {
        [JsonPropertyName("tpl")] public string Tpl { get; set; }
        [JsonPropertyName("count")] public int Count { get; set; }
    }

    public class RefundRequest : SPTarkov.Server.Core.Models.Utils.IRequestData
    {
        [JsonPropertyName("tpl")] public string Tpl { get; set; }
        [JsonPropertyName("count")] public int Count { get; set; }
    }
}
