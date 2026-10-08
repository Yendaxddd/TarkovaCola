using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Desafio opcional de cada raid en modo Classic. Se sortea al empezar la raid (si alguna perk principal puede ganar una mejora):
    // si se cumple y extraes con vida, una perk principal al azar gana una 2.a mejora menor (ver ClassicMode.GrantSecondAugment).
    // El estado vive solo durante la raid. Los textos estan en Data.cs ("cc.<id>.n" nombre, "cc.<id>.d" descripcion).
    internal static class ClassicChallenge
    {
        internal class Def
        {
            public string Id;
            public int Goal;
        }

        // Todos se pueden hacer en una raid normal. "ghost" es especial: se cumple al extraer sin haber recibido dano.
        internal static readonly Def[] All =
        {
            new Def { Id = "kills",   Goal = 5 },        // mata a 5 enemigos
            new Def { Id = "heads",   Goal = 3 },        // 3 disparos a la cabeza que maten
            new Def { Id = "far",     Goal = 1 },        // una baja desde 60 m o mas
            new Def { Id = "pmc",     Goal = 2 },        // 2 PMC
            new Def { Id = "grenade", Goal = 1 },        // una baja con granada
            new Def { Id = "melee",   Goal = 1 },        // una baja cuerpo a cuerpo
            new Def { Id = "run",     Goal = 800 },      // 800 m corriendo
            new Def { Id = "travel",  Goal = 2000 },     // 2000 m recorridos
            new Def { Id = "time",    Goal = 900 },      // 15 minutos con vida (segundos)
            new Def { Id = "pair",    Goal = 1 },        // 2 bajas en menos de 10 s
            new Def { Id = "medic",   Goal = 3 },        // usar 3 objetos medicos
            new Def { Id = "ghost",   Goal = 1 },        // extraer sin recibir dano
        };

        internal const float FarMeters = 60f;
        internal const float PairSeconds = 10f;

        private static Def _cur;
        private static int _progress;
        private static bool _done, _hurt;
        private static float _acc;
        private static readonly List<float> _killTimes = new List<float>();

        internal static Def Current { get { return _cur; } }
        internal static int Progress { get { return _progress; } }
        internal static bool Done { get { return _done; } }
        internal static bool Hurt { get { return _hurt; } }          // (solo importa en "ghost") ya recibiste dano: el desafio esta perdido
        internal static bool Failed { get { return _cur != null && _cur.Id == "ghost" && _hurt; } }

        internal static void OnRaidStart()
        {
            _cur = null; _progress = 0; _done = false; _hurt = false; _acc = 0f;
            _killTimes.Clear();
        }

        // Sortea el desafio si aun no hay uno y alguna perk principal puede ganar una mejora. Se llama al empezar la raid y tras beber.
        internal static void EnsureRolled()
        {
            if (_cur != null || !ClassicMode.Enabled || !ClassicMode.HasEligiblePerk()) return;
            var last = ClassicMode.LastChallenge;
            var pool = All.Where(d => d.Id != last).ToList();            // no se repite el de la raid anterior
            _cur = pool[UnityEngine.Random.Range(0, pool.Count)];
            _progress = 0; _done = false; _hurt = false; _acc = 0f;
            _killTimes.Clear();
            ClassicMode.SetLastChallenge(_cur.Id);
            Dbg.Log("CLASSIC", "desafio de raid: " + _cur.Id + " (objetivo " + _cur.Goal + ")");
            Hud.Enqueue(L.T("cc.start"), Name(_cur.Id) + ": " + Desc(_cur.Id), null);
        }

        internal static string Name(string id) { return L.T("cc." + id + ".n"); }
        internal static string Desc(string id) { return L.T("cc." + id + ".d"); }

        private static void Add(int n)
        {
            if (_cur == null || _done || n <= 0) return;
            _progress = Mathf.Min(_cur.Goal, _progress + n);
            Dbg.Log("CLASSIC", "desafio " + _cur.Id + " " + _progress + "/" + _cur.Goal);
            if (_progress >= _cur.Goal)
            {
                _done = true;
                Hud.Notify(L.T("cc.done"), L.T("cc.done.sub"), null);
            }
        }

        // acumula valores fraccionarios (metros, segundos) y suma solo los enteros
        private static void AddFraction(float amount)
        {
            _acc += amount;
            int whole = (int)_acc;
            if (whole > 0) { _acc -= whole; Add(whole); }
        }

        // ---------------------------------------------------------------- eventos
        // Baja del jugador (desde PerkFx.OnKill)
        internal static void OnKill(Player me, Player victim, DamageInfo info, EBodyPart part)
        {
            if (_cur == null || _done) return;
            switch (_cur.Id)
            {
                case "kills": Add(1); break;
                case "heads": if (part == EBodyPart.Head) Add(1); break;
                case "far": if (Vector3.Distance(me.Position, victim.Position) >= FarMeters) Add(1); break;
                case "pmc": if (IsPmc(victim)) Add(1); break;
                case "grenade": if ((info.DamageType & (EDamageType.Explosion | EDamageType.GrenadeFragment)) != 0) Add(1); break;
                case "melee": if ((info.DamageType & EDamageType.Melee) != 0) Add(1); break;
                case "pair":
                    float now = Time.time;
                    _killTimes.Add(now);
                    _killTimes.RemoveAll(t => now - t > PairSeconds);
                    if (_killTimes.Count >= 2) Add(1);
                    break;
            }
        }

        private static bool IsPmc(Player p)
        {
            var info = p != null && p.Profile != null ? p.Profile.Info : null;
            if (info == null) return false;
            if (info.Side == EPlayerSide.Usec || info.Side == EPlayerSide.Bear) return true;
            string role = info.Settings != null ? info.Settings.Role.ToString() : "";
            return role.Equals("pmcBEAR", StringComparison.OrdinalIgnoreCase) || role.Equals("pmcUSEC", StringComparison.OrdinalIgnoreCase);
        }

        // Cada frame con el jugador vivo (desde PerkFx.Tick): metros recorridos, corriendo o no, y tiempo transcurrido
        internal static void OnMove(float meters, bool sprinting, float dt)
        {
            if (_cur == null || _done) return;
            switch (_cur.Id)
            {
                case "run": if (sprinting) AddFraction(meters); break;
                case "travel": AddFraction(meters); break;
                case "time": AddFraction(dt); break;
            }
        }

        // Comer, beber o curarse con algo que no sea una perk (desde DrinkPatch)
        internal static void OnConsumed(Item item)
        {
            if (_cur == null || _done || _cur.Id != "medic") return;
            if (item is Meds && !(item is Stimulator)) Add(1);
        }

        // Dano recibido por el jugador (desde DamageHubPatch)
        internal static void OnDamageTaken(DamageInfo info)
        {
            if (_cur == null || _hurt || _cur.Id != "ghost") return;
            if ((info.DamageType & (PerkFx.Hits | EDamageType.Fall | EDamageType.Barbed)) == 0) return;      // hambre, sangrado... no cuentan
            _hurt = true;
            Dbg.Log("CLASSIC", "desafio ghost perdido: dano recibido (" + info.DamageType + ")");
        }

        // Al terminar la raid con vida: ¿esta cumplido? ("ghost" se cumple si no hubo dano)
        internal static bool CompletedAtEnd()
        {
            if (_cur == null) return false;
            if (_cur.Id == "ghost") return !_hurt;
            return _done;
        }
    }
}
