using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace TarkovaCola.Server
{
    // Traducciones del servidor: nombres y descripciones de objetos, logros y misiones del mod (todo lo que el juego saca de sus tablas
    // de idioma con un id del mod, que empiezan por 6a1c).
    //  - Al arrancar escribe locales/en.json y locales/es.json con TODOS los textos del mod (se regeneran siempre: son la plantilla).
    //  - Cualquier otro locales/<idioma>.json (con el codigo de idioma de EFT: ch, ru, fr, de, jp, kr, pl, po, it, cz, hu, sk, tu, ro,
    //    es-mx...) sustituye esos textos en ese idioma. Las claves que falten se quedan en ingles.
    //  - Un archivo "en.custom.json" / "es.custom.json" tambien vale para corregir el ingles o el espanol sin que se borre al regenerar.
    // Va lo ultimo (despues de crear todos los objetos y logros) para que sus cambios ganen.
    [Injectable(TypePriority = OnLoadOrder.PostLoad + 50)]
    public class TarkovaColaLocales : IOnLoad
    {
        private const string Prefix = "6a1c";
        private readonly LocaleTable _locales;

        public TarkovaColaLocales(LocaleTable locales) { _locales = locales; }

        private static string Dir
        {
            get { return Path.Combine(Path.GetDirectoryName(typeof(TarkovaColaLocales).Assembly.Location) ?? ".", "locales"); }
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var opts = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

                // 1) plantillas (en / es) con lo que el mod tiene ahora mismo
                foreach (var lang in new[] { "en", "es" })
                {
                    if (!_locales.Global.TryGetValue(lang, out var lazy)) continue;
                    var mine = new SortedDictionary<string, string>(StringComparer.Ordinal);
                    foreach (var kv in lazy.Value)
                        if (kv.Key != null && kv.Key.StartsWith(Prefix, StringComparison.Ordinal) && kv.Value != null) mine[kv.Key] = kv.Value;
                    var file = new Dictionary<string, object>
                    {
                        ["_readme"] = "Tarkova-Cola: plantilla generada (" + lang + "). No la edites: copiala como <codigo de idioma de EFT>.json " +
                                      "(ch, ru, fr, de, jp, kr, pl, po, it, cz, hu, sk, tu, ro, es-mx...) y traduce los valores. / Generated template: " +
                                      "copy it as <EFT language code>.json and translate the values.",
                        ["texts"] = mine,
                    };
                    File.WriteAllText(Path.Combine(Dir, lang + ".json"), JsonSerializer.Serialize(file, opts));
                }

                // 2) traducciones
                int langs = 0, keys = 0;
                foreach (var path in Directory.GetFiles(Dir, "*.json"))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    bool custom = name.EndsWith(".custom", StringComparison.OrdinalIgnoreCase);
                    string lang = custom ? name.Substring(0, name.Length - 7) : name;
                    if (!custom && (lang == "en" || lang == "es")) continue;          // las plantillas no se aplican
                    if (!_locales.Global.TryGetValue(lang, out var lazy)) { Console.WriteLine("[Tarkova-Cola] traduccion " + name + ".json: el juego no tiene el idioma '" + lang + "'"); continue; }
                    var texts = Read(path);
                    if (texts == null || texts.Count == 0) continue;
                    lazy.AddTransformer(table =>
                    {
                        foreach (var kv in texts) if (!string.IsNullOrEmpty(kv.Value)) table[kv.Key] = kv.Value;
                        return table;
                    });
                    langs++; keys += texts.Count;
                }
                if (langs > 0) Console.WriteLine("[Tarkova-Cola] traducciones del servidor: " + langs + " archivo(s), " + keys + " textos");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR con las traducciones del servidor: " + e.Message); }
            return Task.CompletedTask;
        }

        // { "texts": { clave: texto } } (o el diccionario plano)
        private static Dictionary<string, string> Read(string path)
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                var root = doc.RootElement;
                if (root.TryGetProperty("texts", out var t)) root = t;
                var d = new Dictionary<string, string>();
                foreach (var p in root.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.String && !p.Name.StartsWith("_")) d[p.Name] = p.Value.GetString();
                return d;
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] traduccion " + Path.GetFileName(path) + " con errores: " + e.Message); return null; }
        }
    }
}
