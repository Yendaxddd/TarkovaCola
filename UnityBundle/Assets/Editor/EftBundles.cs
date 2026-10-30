using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TarkovaCola
{
    // Carga bundles del juego (EFT usa la misma version de Unity, 2022.3.43f1) con sus dependencias, leidas de Windows.json.
    public static class EftBundles
    {
        public static string Root = System.Environment.GetEnvironmentVariable("EFT_STREAMING")
            ?? @"D:\Escape from Tarkov\SPT v4.1.6\EscapeFromTarkov_Data\StreamingAssets\Windows";

        private static Dictionary<string, string[]> _deps;
        private static readonly Dictionary<string, AssetBundle> _loaded = new Dictionary<string, AssetBundle>();

        private static void ReadManifest()
        {
            if (_deps != null) return;
            _deps = new Dictionary<string, string[]>();
            string json = File.ReadAllText(Path.Combine(Root, "Windows.json"));
            foreach (Match m in Regex.Matches(json, "\"([^\"]+)\":\\{\"FileName\":\"[^\"]*\",\"Crc\":\\d+,\"Hash\":\\{[^}]*\\},\"Dependencies\":\\[([^\\]]*)\\]"))
            {
                var list = new List<string>();
                foreach (Match d in Regex.Matches(m.Groups[2].Value, "\"([^\"]+)\"")) list.Add(d.Groups[1].Value);
                _deps[m.Groups[1].Value] = list.ToArray();
            }
        }

        // rel: ruta como en Windows.json, p. ej. "assets/content/weapons/m48_kukri/weapon_united_cutlery_m48_kukri_container.bundle"
        public static AssetBundle Load(string rel)
        {
            ReadManifest();
            AssetBundle ab;
            if (_loaded.TryGetValue(rel, out ab)) return ab;
            _loaded[rel] = null;   // evita ciclos
            string[] deps;
            if (_deps.TryGetValue(rel, out deps)) foreach (var d in deps) Load(d);
            ab = AssetBundle.LoadFromFile(Path.Combine(Root, rel.Replace('/', Path.DirectorySeparatorChar)));
            if (ab == null) Debug.LogWarning("[EFT] no se pudo abrir " + rel);
            _loaded[rel] = ab;
            return ab;
        }

        public static void UnloadAll()
        {
            foreach (var ab in _loaded.Values) if (ab != null) ab.Unload(true);
            _loaded.Clear();
        }
    }
}
