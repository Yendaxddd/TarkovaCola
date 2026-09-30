using System;
using System.Collections.Generic;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Registro de depuracion barato: se apaga con la opcion "Debug" y solo escribe cuando ocurre un evento.
    // Los ganchos que se ejecutan muchas veces por segundo NO llaman a esto si esta apagado (se comprueba Dbg.On antes
    // de construir el texto) y usan Throttled para no saturar el log.
    //
    // Formato:  [Tarkova-Cola] [CATEGORIA] +mm:ss mensaje      (mm:ss = tiempo desde que empezo la raid)
    internal static class Dbg
    {
        internal static bool On;                        // copia local de la opcion, para no leer el ConfigEntry en cada llamada
        private static float _raidStart = -1f;
        private static readonly Dictionary<string, float> _last = new Dictionary<string, float>();

        internal static void Bind(BepInEx.Configuration.ConfigEntry<bool> entry)
        {
            On = entry.Value;
            entry.SettingChanged += (s, e) => On = entry.Value;
        }

        internal static void RaidStarted()
        {
            _raidStart = Time.time;
            _last.Clear();
        }

        private static string Stamp()
        {
            if (_raidStart < 0f) return "--:--";
            int s = Mathf.Max(0, Mathf.RoundToInt(Time.time - _raidStart));
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        // Evento puntual (baja, bebida, desafio, cambio de estado...).
        internal static void Log(string cat, string msg)
        {
            if (!On) return;
            Plugin.Log.LogInfo("[" + cat + "] +" + Stamp() + " " + msg);
        }

        // Para ganchos frecuentes: como mucho una linea por 'key' cada 'seconds'.
        internal static void Throttled(string key, float seconds, string cat, string msg)
        {
            if (!On) return;
            float now = Time.time, last;
            if (_last.TryGetValue(key, out last) && now - last < seconds) return;
            _last[key] = now;
            Log(cat, msg);
        }
    }
}
