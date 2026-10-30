using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TarkovaCola.Server
{
    // Opciones del servidor que elige el jugador desde F12: user/mods/TarkovaCola/settings.json
    //   { "injectors": false }   injectors = las perks son inyectores (estimulantes) en vez de latas
    // Cambian como se crean los objetos al arrancar, asi que se aplican al reiniciar el servidor y el juego.
    internal static class Settings
    {
        internal class Data
        {
            [JsonPropertyName("injectors")] public bool Injectors { get; set; }
        }

        private static readonly object _lock = new object();
        private static Data _data;
        private static string FilePath { get { return Path.Combine(QuestIds.ModDir, "settings.json"); } }

        internal static Data Current
        {
            get
            {
                lock (_lock)
                {
                    if (_data != null) return _data;
                    try { _data = File.Exists(FilePath) ? JsonSerializer.Deserialize<Data>(File.ReadAllText(FilePath)) : null; }
                    catch (Exception e) { Console.WriteLine("[Tarkova-Cola] settings.json no se pudo leer: " + e.Message); }
                    return _data ??= new Data();
                }
            }
        }

        // devuelve true si el valor cambio (hay que reiniciar para verlo)
        internal static bool SetInjectors(bool on)
        {
            lock (_lock)
            {
                var d = Current;
                if (d.Injectors == on) return false;
                d.Injectors = on;
                try { File.WriteAllText(FilePath, JsonSerializer.Serialize(d, new JsonSerializerOptions { WriteIndented = true })); }
                catch (Exception e) { Console.WriteLine("[Tarkova-Cola] settings.json no se pudo guardar: " + e.Message); }
                Console.WriteLine("[Tarkova-Cola] perks como " + (on ? "INYECTORES" : "LATAS") + " (se aplica al reiniciar el servidor)");
                return true;
            }
        }
    }

    public class InjectorsRequest : SPTarkov.Server.Core.Models.Utils.IRequestData
    {
        [JsonPropertyName("on")] public bool On { get; set; }
    }
}
