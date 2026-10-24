using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Compatibilidad con Arena Mode (maschine34675/ArenaMode), sin depender de el: se lee su estado por reflexion y, si no esta instalado,
    // nada de esto hace nada.
    //   - Cualquier partida de Arena es una "caja cerrada": no cuenta para el perfil. Las perks de Classic y las Eternas guardadas
    //     no se usan dentro, morir no las quita ni devuelve botellas por correo, y al terminar todo vuelve a como estaba.
    //   - Solo el modo Zombies tiene soporte: perk aleatoria cada N oleadas, progreso lento de las perks (XP y desafios).
    //   - Al morir (o reaparecer) se pierden las perks que hayas bebido en la partida: hay que volver a beberlas.
    //   - En los demas modos de Arena las perks funcionan si las bebes, pero sin perk aleatoria ni progreso.
    internal static class ArenaCompat
    {
        private const string BackupKey = "arenaBackup";

        private static bool _looked;
        private static PropertyInfo _isActive, _activeMode, _current;
        private static PropertyInfo _matchMode;
        private static FieldInfo _wave;
        private static object _announcer;      // salida de audio del locutor de Arena
        private static FieldInfo _announcerSrc;
        private static bool _ducked;

        private static bool _inArena;          // esta raid es una partida de Arena (se fija en cuanto se detecta)
        private static int _lastWave, _bottles;
        private static bool _zombies;          // partida de Zombies con soporte (se fija al entrar)
        private static float _next;
        private static readonly System.Collections.Generic.Dictionary<string, float> _chalCarry = new System.Collections.Generic.Dictionary<string, float>();

        // ---------------------------------------------------------------- estado
        internal static bool InArena { get { return _inArena; } }
        internal static bool InZombies { get { return _inArena && _zombies; } }
        // Arena activa ahora mismo (aunque Tick aun no lo haya visto): para lo que se decide al empezar la raid
        internal static bool ArenaNow { get { return _inArena || ArenaActive(); } }

        private static bool Lookup()
        {
            if (_looked) return _isActive != null;
            _looked = true;
            try
            {
                var session = AccessTools.TypeByName("ArenaMode.Session.ArenaSession");
                if (session == null) return false;
                _isActive = session.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Static);
                _activeMode = session.GetProperty("ActiveMode", BindingFlags.Public | BindingFlags.Static);
                var service = AccessTools.TypeByName("ArenaMode.Modes.ArenaMatchService");
                if (service != null) _current = service.GetProperty("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var match = AccessTools.TypeByName("ArenaMode.Modes.ArenaMatch");
                if (match != null) _matchMode = match.GetProperty("Mode", BindingFlags.Public | BindingFlags.Instance);
                var zombie = AccessTools.TypeByName("ArenaMode.Modes.ZombieMode");
                var audio = AccessTools.TypeByName("ArenaMode.Audio.ArenaAnnouncerOutput");
                var outF = audio != null ? audio.GetField("Output", BindingFlags.NonPublic | BindingFlags.Static) : null;
                _announcer = outF != null ? outF.GetValue(null) : null;
                if (_announcer != null) _announcerSrc = _announcer.GetType().GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
                if (zombie != null) _wave = zombie.GetField("_waveNumber", BindingFlags.NonPublic | BindingFlags.Instance);
                Plugin.Log.LogInfo("Arena Mode detectado: compatibilidad activa (oleadas: " + (_wave != null ? "si" : "no") + ")");
            }
            catch (Exception e) { Plugin.Log.LogWarning("Arena Mode: no se pudo leer su estado: " + e.Message); _isActive = null; }
            return _isActive != null;
        }

        private static bool ArenaActive()
        {
            if (!Lookup()) return false;
            try { return (bool)_isActive.GetValue(null); } catch { return false; }
        }

        private static string ModeId()
        {
            try { return _activeMode != null ? _activeMode.GetValue(null) as string : null; } catch { return null; }
        }

        private static int Wave()
        {
            try
            {
                var match = _current != null ? _current.GetValue(null) : null;
                var mode = match != null && _matchMode != null ? _matchMode.GetValue(match) : null;
                if (mode == null || _wave == null || !_wave.DeclaringType.IsInstanceOfType(mode)) return 0;
                return (int)_wave.GetValue(mode);
            }
            catch { return 0; }
        }

        // ---------------------------------------------------------------- ciclo de la raid
        internal static void OnRaidStart()
        {
            if (_ducked) { _ducked = false; Jingle.Duck(false); }
            _inArena = false; _zombies = false; _lastWave = 0; _bottles = 0; _chalCarry.Clear(); _next = 0f;
            RestoreBackup("inicio de raid");             // por si la partida anterior de Arena no termino bien (cierre del juego...)
        }

        // Cada frame (Plugin.Update)
        internal static void Tick()
        {
            if (!Plugin.InRaid) return;
            if (Time.time < _next) return;
            _next = Time.time + 0.5f;
            if (!_inArena)
            {
                if (!ArenaActive()) return;
                Enter();
            }
            if (InZombies) TickWaves();
            TickDuck();
        }

        // Nuestras canciones bajan mientras habla el locutor de Arena
        private static void TickDuck()
        {
            bool talking = false;
            try { var src = _announcerSrc != null ? _announcerSrc.GetValue(_announcer) as AudioSource : null; talking = src != null && src.isPlaying; }
            catch { }
            if (talking == _ducked) return;
            _ducked = talking;
            Jingle.Duck(talking);
        }

        // Entra en la partida: se guarda lo que es del perfil y la partida empieza sin perks
        private static void Enter()
        {
            _inArena = true;
            _zombies = Plugin.CfgArenaZombies.Value && ModeId() == "zombie";
            var st = PerkService.State;
            if (st[BackupKey] == null)
            {
                st[BackupKey] = new JObject { ["classic"] = st["classic"]?.DeepClone(), ["eternal"] = st["eternal"]?.DeepClone() };
                PerkService.Flush();
            }
            if (st["classic"] is JObject c) c["perks"] = new JArray();
            st["eternal"] = new JArray();
            Eternal.OnRaidStart();
            Perks.ClearActive();
            Perk.Refresh();
            var me = Plugin.Me;
            if (me != null) PerkFx.RefreshPhysical(me);
            Plugin.Log.LogInfo("Partida de Arena (" + ModeId() + "): no cuenta para el perfil, se empieza sin perks");
            if (_zombies)
                Hud.Enqueue(L.T("arena.title"), L.F("arena.start", Plugin.CfgArenaWaves.Value), null);
        }

        private static void TickWaves()
        {
            int w = Wave();
            if (w <= 0 || w == _lastWave) return;
            int prev = _lastWave;
            _lastWave = w;
            if (prev <= 0) return;                       // primera lectura
            int every = Mathf.Max(1, Plugin.CfgArenaWaves.Value);
            int cleared = w - 1;                         // al empezar la oleada w se supero la w-1
            if (cleared <= 0 || cleared % every != 0) return;
            var me = Plugin.Me;
            if (me == null || me.HealthController == null || !me.HealthController.IsAlive) return;
            _bottles++;
            Dbg.Log("ARENA", "oleada " + cleared + " superada: perk aleatoria");
            Powerup.SpawnNear(me);
        }

        // El jugador murio en la partida: las perks bebidas se pierden (hay que volver a beberlas)
        internal static void OnDeath()
        {
            if (ClassicMode.Enabled && PerkService.State["classic"] is JObject c) c["perks"] = new JArray();
            PerkService.State["eternal"] = new JArray();
            Eternal.OnRaidStart();
            Perks.ClearActive();
            Perk.Refresh();
            Dbg.Log("ARENA", "muerte en Arena: perks de la partida perdidas");
        }

        // Fin de la partida: vuelve el estado del perfil
        internal static void OnRaidEnd()
        {
            if (!_inArena) return;
            RestoreBackup("fin de la partida de Arena (" + _bottles + " perks aleatorias)");      // (_inArena sigue hasta la proxima raid: el XP se reparte despues)
        }

        private static void RestoreBackup(string why)
        {
            var st = PerkService.State;
            var b = st[BackupKey] as JObject;
            if (b == null) return;
            if (b["classic"] != null && b["classic"].Type != JTokenType.Null) st["classic"] = b["classic"].DeepClone(); else st.Remove("classic");
            if (b["eternal"] != null && b["eternal"].Type != JTokenType.Null) st["eternal"] = b["eternal"].DeepClone(); else st.Remove("eternal");
            st.Remove(BackupKey);
            PerkService.Save();
            PerkService.Flush();
            Plugin.Log.LogInfo("Arena: estado del perfil restaurado (" + why + ")");
        }

        // ---------------------------------------------------------------- progreso lento
        // XP de fin de partida: en Zombies se multiplica; en otros modos de Arena, nada
        internal static int ScaleXp(int xp)
        {
            if (!_inArena) return xp;
            if (!InZombies) return 0;
            return (int)Math.Round(xp * Mathf.Clamp(Plugin.CfgArenaProgress.Value, 0f, 1f));
        }

        // Desafios: en Zombies cuentan a ritmo reducido (se acumula la fraccion); en otros modos de Arena no cuentan
        internal static int ScaleProgress(string id, int n)
        {
            if (!_inArena) return n;
            if (!InZombies) return 0;
            float carry;
            _chalCarry.TryGetValue(id, out carry);
            carry += n * Mathf.Clamp(Plugin.CfgArenaProgress.Value, 0f, 1f);
            int whole = Mathf.FloorToInt(carry);
            _chalCarry[id] = carry - whole;
            return whole;
        }
    }
}
