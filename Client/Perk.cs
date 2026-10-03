using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Estado en raid de las perks: que mejoras/desventajas lleva equipadas el jugador y valores de los efectos (los de Speed Cola aqui).
    // Todo lo que se consulta muchas veces por segundo son campos simples (sin calculos) para no costar FPS.
    internal static class Perk
    {
        // --- valores de balance (los de estado base se pueden ajustar aqui) ---
        internal const float BaseReload = 1.5f;      // Speed Cola base
        internal const float RushReload = 2.0f;      // Sugar Rush (equipada): el bonus base de la perk sube de x1.5 a x2.0, permanente
        internal const float QuickHandsDraw = 1.5f;  // +50% velocidad al sacar/cambiar de arma
        internal const float CheckSpeed = 1.75f;     // Chamber Check: +75% al revisar cargadores en el inventario
        internal const float MagJugglerSpeed = 1.5f; // Mag Juggler: +50% al cargar cargadores del arma principal
        internal const float SteadyDamage = 0.7f;    // Steady Fingers: -30% de dano en brazos
        internal const float JitterSway = 1.25f;     // Jittery Hands: +25% de balanceo
        internal const float StickyMalf = 1.10f;     // Sticky Mags: +10% de probabilidad de encasquillamiento
        internal const float DryMouth = 1.20f;       // Dry Mouth: la hidratacion baja +20%
        internal const float CrashStep = 0.25f;      // Sugar Crash: +0.25 de sed por tramo
        internal const float CrashMax = 2f;          //   maximo x2
        internal const float CrashInterval = 120f;   //   cada 2 min sin matar
        internal const float SweetToothTime = 2f;    // Sweet Tooth: comer/beber otra cosa tarda el doble
        internal const float SlurpMeters = 30f;      // Loud Slurp: radio del ruido

        private static readonly HashSet<string> _eq = new HashSet<string>();

        // ---- estado dinamico ----
        internal static bool JitterOn;             // Jittery Hands activo ahora mismo (un brazo no sano)
        internal static object MyBreath;            // BreathEffector del jugador
        internal static float LastKillOrDrink;
        internal static int ReloadCount;            // recargas con cargador en esta raid (Overpressure)
        internal static float CrashMult = 1f;

        internal static void OnRaidStart()
        {
            _eq.Clear();
            JitterOn = false; MyBreath = null;
            LastKillOrDrink = Time.time; ReloadCount = 0; CrashMult = 1f;
        }

        // Lee del estado guardado que lleva equipado el jugador, de todas las perks (se llama al empezar la raid y al beber).
        // Los ids de mejoras son unicos entre perks, asi que basta un unico conjunto.
        internal static void Refresh()
        {
            _eq.Clear();
            try
            {
                foreach (var perk in Perks.All)
                {
                    var e = PerkService.State["equipped"]?[perk.Id] as JObject;
                    if (e == null) continue;
                    foreach (var side in new[] { "aug", "drb" })
                    {
                        var s = e[side] as JObject;
                        if (s == null) continue;
                        if (s["major"] != null && s["major"].Type == JTokenType.String) _eq.Add((string)s["major"]);
                        var minors = s["minor"] as JArray;
                        if (minors != null) foreach (var m in minors) if (m.Type == JTokenType.String) _eq.Add((string)m);
                    }
                }
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("No se pudo leer el equipamiento: " + ex.Message); }
            Dbg.Log("EQUIP", "equipado: " + (_eq.Count == 0 ? "(nada)" : string.Join(", ", _eq.ToArray())));
        }

        // La mejora/desventaja esta equipada Y su perk esta activa (se bebio en esta raid).
        internal static bool Has(string id)
        {
            if (!_eq.Contains(id)) return false;
            var perk = Perks.OfNode(id);
            return perk != null && Perks.IsActive(perk.Id);
        }

        internal static float ReloadMult
        {
            get
            {
                if (!Plugin.SpeedColaActive) return 1f;
                return _eq.Contains("a_rush") ? RushReload : BaseReload;
            }
        }

        internal static float DrawMult { get { return Has("a_quick") ? QuickHandsDraw : 1f; } }
    }
}
