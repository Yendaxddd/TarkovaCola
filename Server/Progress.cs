using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace TarkovaCola.Server
{
    // Guarda el progreso de perks de cada perfil como JSON crudo (lo entiende el cliente y la interfaz web):
    //   user/mods/TarkovaCola/profiles/<sessionId>.json
    [Injectable(InjectionType.Singleton)]
    public class PerkStore
    {
        private readonly string _dir = Path.Combine(Path.GetDirectoryName(typeof(PerkStore).Assembly.Location) ?? ".", "profiles");
        private readonly object _lock = new object();

        private string PathFor(string sessionId)
        {
            // el id de sesion es hexadecimal; se filtra por seguridad antes de usarlo como nombre de archivo
            var safe = new string(Array.FindAll(sessionId.ToCharArray(), char.IsLetterOrDigit));
            return Path.Combine(_dir, safe + ".json");
        }

        public string Load(string sessionId)
        {
            lock (_lock)
            {
                var p = PathFor(sessionId);
                return File.Exists(p) ? File.ReadAllText(p) : "";
            }
        }

        public void Save(string sessionId, string json)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_dir);
                var p = PathFor(sessionId);
                var tmp = p + ".tmp";
                File.WriteAllText(tmp, json);
                File.Move(tmp, p, overwrite: true);   // escritura atomica: nunca queda un archivo a medias
            }
        }
    }

    public class PerkRequest : IRequestData
    {
        [JsonPropertyName("json")] public string Json { get; set; }
    }

    public class PerkResponse
    {
        [JsonPropertyName("json")] public string Json { get; set; }
    }

    [Injectable(InjectionType.Transient, int.MaxValue, TypePriority = 400000)]
    public class TarkovaColaRouter : StaticRouter
    {
        public TarkovaColaRouter(JsonUtil jsonUtil, HttpResponseUtil http, PerkStore store)
            : base(jsonUtil, new List<RouteAction>
            {
                new RouteAction<PerkRequest>("/tarkovacola/state/get",
                    async (url, info, sessionId, output, ct) =>
                        http.NoBody(new PerkResponse { Json = store.Load(sessionId.ToString()) })),

                new RouteAction<PerkRequest>("/tarkovacola/state/save",
                    async (url, info, sessionId, output, ct) =>
                    {
                        if (!string.IsNullOrEmpty(info?.Json)) store.Save(sessionId.ToString(), info.Json);
                        return http.NoBody(new PerkResponse { Json = "ok" });
                    }),
            })
        {
        }
    }
}
