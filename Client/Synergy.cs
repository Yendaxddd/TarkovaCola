using System.Collections.Generic;
using System.Linq;
using EFT;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Sinergias: bonos pequenos que se activan cuando llevas DOS perks concretas a la vez.
    //   - Cada sinergia se desbloquea con un desafio (que solo avanza con las dos perks activas en la raid).
    //   - Se pueden equipar hasta 2 sinergias desbloqueadas (pestana "Sinergia" de Research).
    //   - Funcionan en Modern y en Classic. En Classic una perk "modificador" da menos efecto, y la sinergia tambien
    //     (la fuerza es la de la mas debil de las dos).
    //  Estado en el JSON de progreso:  syn: { prog:{id:n}, done:{id:true}, equipped:[id,...] }
    // ============================================================================================
    internal class SynDef
    {
        public string Id;
        public string A, B;          // las dos perks
        public int Goal;             // objetivo del desafio
        public ProgUnit Unit;
    }

    internal static class Synergy
    {
        internal const int MaxEquipped = 1;

        internal const string Cardio = "syn_cardio", Iron = "syn_iron", Blast = "syn_blast";

        // --- balance ---
        internal const float CardioDraw = 0.12f;       // Speed Cola + Stamin-Up: sacar el arma +12%
        internal const float CardioStamina = 0.05f;    //   y correr gasta un 5% menos de estamina
        internal const float IronDamage = 0.03f;       // Juggernog + Widow's Wine: -3% de dano recibido
        internal const float BlastReload = 0.20f;      // Speed Cola + PHD Flopper: tras absorber una explosion o caida, recarga +20%...
        internal const float BlastSeconds = 6f;        //   ...durante 6 s

        internal static readonly SynDef[] All =
        {
            new SynDef { Id = Cardio, A = "speedcola", B = "staminup",   Goal = 3000, Unit = ProgUnit.Meters },   // correr 3 km
            new SynDef { Id = Iron,   A = "juggernog", B = "widowswine", Goal = 5 },                              // 5 bajas de enemigos atrapados en tu telarana
            new SynDef { Id = Blast,  A = "speedcola", B = "phdflopper", Goal = 5 },                              // absorber 5 explosiones o caidas
        };

        internal static SynDef Get(string id) { return All.FirstOrDefault(s => s.Id == id); }
        internal static string Name(SynDef s) { return L.T(s.Id + ".n"); }
        internal static string Desc(SynDef s) { return L.T(s.Id + ".d"); }
        internal static string Challenge(SynDef s) { return L.T(s.Id + ".c"); }

        // ---------------------------------------------------------------- estado guardado
        private static JObject Root
        {
            get
            {
                var st = PerkService.State;
                var r = st["syn"] as JObject;
                if (r == null) st["syn"] = r = new JObject();
                return r;
            }
        }

        private static JObject Sub(string key)
        {
            var r = Root;
            var o = r[key] as JObject;
            if (o == null) r[key] = o = new JObject();
            return o;
        }

        internal static bool IsUnlocked(string id) { return (bool?)Sub("done")[id] == true; }
        internal static int Progress(string id) { return (int?)Sub("prog")[id] ?? 0; }

        internal static List<string> Equipped()
        {
            var list = new List<string>();
            var arr = Root["equipped"] as JArray;
            if (arr != null) foreach (var t in arr) if (t.Type == JTokenType.String && Get((string)t) != null && IsUnlocked((string)t) && !list.Contains((string)t)) list.Add((string)t);
            if (list.Count > MaxEquipped) list.RemoveRange(MaxEquipped, list.Count - MaxEquipped);      // (guardado de cuando se permitian 2)
            return list;
        }

        internal static bool IsEquipped(string id) { return Equipped().Contains(id); }

        // Equipa / quita. Devuelve null si fue bien, o la clave del error.
        internal static string Toggle(string id)
        {
            if (Get(id) == null || !IsUnlocked(id)) return "toast.locked";
            var list = Equipped();
            if (list.Contains(id)) list.Remove(id);
            else { list.Clear(); list.Add(id); }                          // solo se lleva una: equipar otra sustituye a la anterior
            Root["equipped"] = new JArray(list);
            PerkService.Save();
            Refresh();
            Dbg.Log("SINERGIA", "equipadas: " + (list.Count == 0 ? "(ninguna)" : string.Join(", ", list.ToArray())));
            return null;
        }

        // ---------------------------------------------------------------- cache de lo equipado (se consulta en caminos calientes)
        private static readonly HashSet<string> _eq = new HashSet<string>();

        internal static void Refresh()
        {
            _eq.Clear();
            try { foreach (var id in Equipped()) _eq.Add(id); }
            catch (System.Exception e) { Plugin.Log.LogWarning("Sinergias: no se pudo leer lo equipado: " + e.Message); }
        }

        // Fuerza de la sinergia ahora mismo: 0 si no esta equipada o falta una de las dos perks; 1 con las dos completas;
        // menos si una es modificador de Classic.
        internal static float Power(string id)
        {
            if (!_eq.Contains(id)) return 0f;
            var s = Get(id);
            return Mathf.Min(Perks.Power(s.A), Perks.Power(s.B));
        }

        // ---------------------------------------------------------------- bonos
        private static float _blastUntil;
        private static bool _blastWas;

        internal static float DrawMult { get { float p = Power(Cardio); return p > 0f ? 1f + CardioDraw * p : 1f; } }
        internal static float StaminaSave { get { return Power(Cardio) * CardioStamina; } }
        internal static float DamageMult { get { float p = Power(Iron); return p > 0f ? 1f - IronDamage * p : 1f; } }
        internal static float ReloadBoost
        {
            get { float p = Power(Blast); return p > 0f && Time.time < _blastUntil ? 1f + BlastReload * p : 1f; }
        }

        internal static bool AnyEquipped { get { return _eq.Count > 0; } }

        // ---------------------------------------------------------------- desafios
        // Las dos perks de la sinergia estan activas en esta raid
        private static bool Both(SynDef s) { return Perks.IsActive(s.A) && Perks.IsActive(s.B); }

        internal static void AddProgress(string id, int n = 1)
        {
            var s = Get(id);
            if (s == null || n <= 0 || IsUnlocked(id) || !Both(s)) return;
            int now = Mathf.Min(s.Goal, Progress(id) + n);
            Sub("prog")[id] = now;
            PerkService.Save();
            Dbg.Log("SINERGIA", id + ": " + now + " / " + s.Goal);
            if (now >= s.Goal)
            {
                Sub("done")[id] = true;
                PerkService.Save();
                PerkService.Flush();
                Dbg.Log("SINERGIA", id + " desbloqueada");
                Hud.Enqueue(L.T("syn.unlocked"), Name(s), s.A);
            }
        }

        private static float _meters;

        // Distancia corriendo con las dos perks de Cardio activas (desde PerkFx.Tick)
        internal static void OnRun(float meters)
        {
            if (IsUnlocked(Cardio) || !Both(Get(Cardio))) return;
            _meters += meters;
            if (_meters < 10f) return;
            int whole = (int)_meters; _meters -= whole;
            AddProgress(Cardio, whole);
        }

        // PHD Flopper anulo una explosion o una caida
        internal static void OnAbsorb(Player me)
        {
            AddProgress(Blast, 1);
            if (Power(Blast) <= 0f) return;
            _blastUntil = Time.time + BlastSeconds;
            (me.HandsController as Player.FirearmController)?.SyncWithCharacterSkills();       // la recarga del arma en mano usa el nuevo multiplicador
            Hud.Notify(Name(Get(Blast)), L.F("syn.blast.sub", Mathf.RoundToInt(BlastReload * 100f), Mathf.RoundToInt(BlastSeconds)), "speedcola");
        }

        // Baja de un enemigo atrapado en tu telarana (Widow's Wine) con Juggernog tambien activa
        internal static void OnWebbedKill() { AddProgress(Iron, 1); }

        internal static void OnRaidStart()
        {
            _blastUntil = 0f; _meters = 0f; _blastWas = false;
            Refresh();
        }

        // Cada frame: cuando termina el empujon de recarga se vuelve a calcular la velocidad del arma
        internal static void Tick(Player me)
        {
            bool on = _blastUntil > 0f && Time.time < _blastUntil;
            if (on == _blastWas) return;
            _blastWas = on;
            if (!on) (me.HandsController as Player.FirearmController)?.SyncWithCharacterSkills();
        }
    }
}
