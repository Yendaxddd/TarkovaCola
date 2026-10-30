using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ConfigurationManager (F12) lee esta clase por su nombre: cambia el nombre, la seccion y la descripcion que se ven, sin cambiar la clave guardada
    internal sealed class ConfigurationManagerAttributes
    {
        public string Category;
        public string DispName;
        public string Description;
        public int? Order;
        public bool? IsAdvanced;
    }

    // Opciones de F12 en los dos idiomas. La clave del archivo .cfg se queda como estaba (en espanol) para no perder lo que cada uno tenia;
    // lo que se ve en F12 sigue el idioma del mod (Auto = el del juego).
    internal static class Cfg
    {
        private static ConfigFile _file;
        private static readonly List<Entry> _entries = new List<Entry>();
        private static bool? _shownSpanish;
        private static float _next;
        private static int _order = 1000;

        private class Entry { public ConfigurationManagerAttributes Attr; public string SecEs, SecEn, NameEs, NameEn, DescEs, DescEn; }

        private static readonly Dictionary<string, string> SectionsEn = new Dictionary<string, string>
        {
            ["Idioma"] = "Language", ["HUD"] = "HUD", ["Perks"] = "Perks", ["Menu"] = "Menu", ["Audio"] = "Audio",
            ["Powerup"] = "Powerup", ["Desafios"] = "Challenges", ["Herramientas"] = "Tools", ["Debug"] = "Debug", ["Arena"] = "Arena", ["???"] = "???",
        };

        private static readonly HashSet<string> DevSections = new HashSet<string> { "Herramientas", "Debug" };

        internal static void Init(ConfigFile file) { _file = file; }

        // dispEs: nombre a mostrar en espanol si no puede ser la clave (las claves no admiten comillas)
        internal static ConfigEntry<T> Bind<T>(string section, string key, T def, string nameEn, string descEs, string descEn, AcceptableValueBase range = null, string dispEs = null)
        {
            // en el orden en que se declaran; las herramientas de desarrollo (editor de maquinas, clips, depuracion) solo con "Advanced settings"
            var attr = new ConfigurationManagerAttributes { Order = _order--, IsAdvanced = DevSections.Contains(section) };
            var e = _file.Bind(section, key, def, new ConfigDescription(descEn, range, attr));
            string secEn;
            _entries.Add(new Entry { Attr = attr, SecEs = section, SecEn = SectionsEn.TryGetValue(section, out secEn) ? secEn : section, NameEs = dispEs ?? key, NameEn = nameEn, DescEs = descEs, DescEn = descEn });
            Apply(L.Spanish);
            return e;
        }

        // Plugin.Update: si cambia el idioma (opcion o idioma del juego) se cambian los textos de F12
        internal static void Tick()
        {
            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + 1f;
            Apply(L.Spanish);
        }

        private static void Apply(bool es)
        {
            if (_shownSpanish == es && _entries.TrueForAll(x => x.Attr.DispName != null)) return;
            _shownSpanish = es;
            foreach (var x in _entries)
            {
                x.Attr.Category = es ? x.SecEs : x.SecEn;
                x.Attr.DispName = es ? x.NameEs : x.NameEn;
                x.Attr.Description = es ? x.DescEs : x.DescEn;
            }
        }
    }
}
