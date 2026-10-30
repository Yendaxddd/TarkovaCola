using System;
using System.IO;
using System.Text.Json.Serialization;

namespace TarkovaCola.Server
{
    // Maquinas de perks colocadas en los mapas: user/mods/TarkovaCola/machines.json (lista JSON).
    //   [{ "map": "bigmap", "perk": "speedcola", "x": 0, "y": 0, "z": 0, "rot": 90 }, ...]
    // El cliente las lee al empezar la raid y el editor de maquinas guarda la lista de un mapa entera.
    internal static class Machines
    {
        private static readonly object _lock = new object();
        private static string FilePath { get { return Path.Combine(QuestIds.ModDir, "machines.json"); } }

        internal static string Read()
        {
            lock (_lock)
            {
                try { return File.Exists(FilePath) ? File.ReadAllText(FilePath) : "[]"; }
                catch (Exception e) { Console.WriteLine("[Tarkova-Cola] machines.json no se pudo leer: " + e.Message); return "[]"; }
            }
        }

        // El cliente manda la lista completa (todas las maquinas de todos los mapas, ya editada)
        internal static bool Write(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart()[0] != '[') return false;
            lock (_lock)
            {
                try
                {
                    if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".bak", true);
                    File.WriteAllText(FilePath, json);
                    Console.WriteLine("[Tarkova-Cola] machines.json guardado (editor de maquinas)");
                    return true;
                }
                catch (Exception e) { Console.WriteLine("[Tarkova-Cola] machines.json no se pudo guardar: " + e.Message); return false; }
            }
        }
    }

    public class MachinesRequest : SPTarkov.Server.Core.Models.Utils.IRequestData
    {
        [JsonPropertyName("json")] public string Json { get; set; }
    }
}
