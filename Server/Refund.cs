using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Commerce;

namespace TarkovaCola.Server
{
    // Devolucion por correo de granadas (mejora Widow's Bite de Widow's Wine). El cliente avisa cuando una granada suya mata a un
    // enemigo; el servidor solo acepta granadas (arma arrojadiza) y como mucho 2 unidades por peticion.
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
                var id = new MongoId(tpl);
                if (!_templates.Items.TryGetValue(id, out var item) || item.Parent != new MongoId(ThrowWeapBase))
                {
                    Console.WriteLine("[Tarkova-Cola] Devolucion rechazada (no es una granada): " + tpl);
                    return false;
                }
                var items = new List<Item>();
                for (int i = 0; i < count; i++)
                    items.Add(new Item { Id = new MongoId(), Template = id, Upd = new Upd { StackObjectsCount = 1 } });
                _mail.SendSystemMessageToPlayer(sessionId,
                    "Widow's Bite (Tarkova-Cola): tu granada ha vuelto. / Your grenade came back.", items, 7 * 24 * 3600L);
                Console.WriteLine("[Tarkova-Cola] Granada devuelta a " + sessionId + ": " + tpl + " x" + count);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[Tarkova-Cola] Devolucion rechazada (" + tpl + "): " + e.Message);
                return false;
            }
        }
    }

    public class RefundRequest : SPTarkov.Server.Core.Models.Utils.IRequestData
    {
        [JsonPropertyName("tpl")] public string Tpl { get; set; }
        [JsonPropertyName("count")] public int Count { get; set; }
    }
}
