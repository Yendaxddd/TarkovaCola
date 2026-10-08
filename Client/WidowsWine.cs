using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Widow's Wine: tus granadas dejan telarañas.
    //   Base: en cuanto una granada que TU lanzas esta cerca de un enemigo (a menos de 10 m, vuele, ruede o ya este en el suelo
    //   esperando a explotar) el enemigo queda atrapado: su velocidad de movimiento baja un 60% y se renueva mientras la
    //   granada siga cerca, hasta 6 s despues. Tambien se aplica al explotar. Se usa el limitador de velocidad del propio juego
    //   (el mismo que los pantanos), que tambien afecta a los bots. Las granadas de humo no cuentan.
    //   Mejoras: Spinneret, Safe Fuse, Web Burst, Venomous Bite, Quick Spinner, Sticky Floor, Widow's Bite, Extra Slot.
    //   Desventajas ligadas: Loud Throw, Brittle Plates, Bitter Wine, Entangled, Greasy Fingers, Hungry Spider, Dry Throat.
    // Lo que depende de las granadas (lanzar, explotar) esta en PerkFx.GrenadeInitPatch / GrenadeExplosionPatch, que llaman aqui.
    internal static class WidowsWine
    {
        internal const string Id = "widowswine";
        internal const string ItemTpl = "6a1c00000000000000000c05";

        // --- balance ---
        internal const float Radius = 10f;          // base: radio de la telarana alrededor de la granada (m)
        internal const float SpeedLeft = 0.40f;     // velocidad que les queda (x0.40 = -60%)
        internal const float Seconds = 6f;          // duracion (se renueva mientras la granada siga cerca)
        internal const float SpinRadius = 2f;       // Spinneret: doble radio...
        internal const float SpinExtra = 2f;        //   ...y 2 s mas
        internal const float ModRadius = 6f;        // como modificador (Classic): radio, velocidad que les queda y duracion reducidos
        internal const float ModSpeedLeft = 0.75f;
        internal const float ModSeconds = 3f;
        internal const float StunSeconds = 3f;      // Venomous Bite / Web Burst: duracion del aturdimiento
        internal const float TangledLeft = 0.60f;   // Entangled: dentro de tu propia telarana te queda el 60% de velocidad
        internal const float BrittleDamage = 1.25f; // Brittle Plates: la armadura recibe un 25% mas de desgaste de las balas
        internal const float ThrowForce = 1.16f;    // Sticky Floor: x1.16 de fuerza = +35% de alcance (el alcance crece con el cuadrado)
        internal const float LoudRadius = 20f;      // Loud Throw
        internal const int WebbedNeeded = 50;       // logro "Sticky situation": enemigos atrapados en total
        internal const int GrenadeKillsNeeded = 25; // logro "Silk and shrapnel": bajas con granada en total
        internal const int WrapParty = 5;           // logro "Wrap party": enemigos atrapados por una sola granada
        internal const float BurstDamage = 100f;    // Web Burst: dano recibido...
        internal const float BurstWindow = 3f;      //   ...en menos de 3 s
        internal const float BurstCooldown = 300f;  //   una vez cada 5 min
        private const float CheckInterval = 0.2f;

        // granadas lanzadas por el jugador que aun existen
        private static readonly List<Grenade> _mine = new List<Grenade>();
        private static readonly HashSet<int> _announced = new HashSet<int>();
        private static readonly Dictionary<string, float> _until = new Dictionary<string, float>();                  // enemigo -> hasta cuando esta atrapado
        private static readonly Dictionary<int, HashSet<string>> _perGrenade = new Dictionary<int, HashSet<string>>();   // enemigos distintos por granada
        private static readonly HashSet<int> _fuseDone = new HashSet<int>();
        private static readonly List<Vector2> _hits = new List<Vector2>();                                          // x = momento, y = dano
        private static float _nextCheck, _nextBurst, _lastTime;
        private static bool _burstPending, _lastRefunded;
        private static string _lastTpl;

        internal static string Notice() { return Effect(Perks.Power(Id)); }

        internal static string Effect(float power) { return L.T(power >= 1f ? "notice.widowswine.sub" : "notice.widowswine.mod"); }

        internal static void OnRaidStart()
        {
            _mine.Clear(); _announced.Clear(); _until.Clear(); _perGrenade.Clear(); _fuseDone.Clear(); _hits.Clear();
            _nextCheck = 0f; _nextBurst = 0f; _lastTime = 0f; _burstPending = false; _lastRefunded = true; _lastTpl = null;
        }

        // ---------------------------------------------------------------- granadas
        // Una granada lanzada por el jugador (llamado desde GrenadeInitPatch con Widow's Wine activa)
        internal static void OnThrown(Grenade g)
        {
            if (g == null) return;
            PerkService.AddProgress("ww_quick");                                    // desafio de Quick Spinner: granadas lanzadas
            var me = Plugin.Me;
            if (me != null && Perk.Has("dw_loud"))
            {
                Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, LoudRadius, AISoundType.step);
                Dbg.Log("EFECTO", "Loud Throw: ruido de " + LoudRadius.ToString("0") + " m");
            }
            if (g is SmokeGrenade) return;                                          // las de humo no hacen telaranas
            _mine.Add(g);
            Dbg.Log("EFECTO", "Widow's Wine: granada lanzada, vigilando enemigos cercanos");
        }

        // Cada 0.2 s: las granadas vivas telarañan a los enemigos que tengan cerca. Tambien resuelve Web Burst pendiente.
        internal static void Update(Player me)
        {
            if (_burstPending) { _burstPending = false; Burst(me); }
            if (_mine.Count == 0) return;
            float now = Time.time;
            if (now < _nextCheck) return;
            _nextCheck = now + CheckInterval;

            for (int i = _mine.Count - 1; i >= 0; i--)
            {
                var g = _mine[i];
                if (g == null) { _mine.RemoveAt(i); continue; }              // ya exploto o se destruyo
                int key = g.GetInstanceID();
                int n = Web(g.transform.position, key);
                if (n > 0 && _announced.Add(key))
                    Hud.Notify(L.T("perk.widowswine").ToUpperInvariant(), L.F("notice.web.sub", n), Id);
            }
        }

        // La granada explota (llamado desde GrenadeExplosionPatch con la granada del jugador)
        internal static void OnExploded(Grenade g, Item item, Vector3 pos)
        {
            int key = g != null ? g.GetInstanceID() : 0;
            int n = Web(pos, key);
            Dbg.Log("EFECTO", "Widow's Wine: explosion en " + pos.ToString("F1") + ", " + n + " enemigo(s) atrapado(s)");
            if (key != 0) { _perGrenade.Remove(key); _fuseDone.Remove(key); _announced.Remove(key); }
            if (item != null) { _lastTpl = item.StringTemplateId; _lastTime = Time.time; _lastRefunded = false; }
        }

        // Atrapa a todos los enemigos vivos cerca de 'center'. Devuelve cuantos hay dentro ahora.
        //   key = id de la granada (para contar enemigos distintos por granada); 0 = sin seguimiento. burst = Web Burst (aturde a todos).
        internal static int Web(Vector3 center, int key, bool burst = false)
        {
            var world = Singleton<GameWorld>.Instance;
            var me = Plugin.Me;
            if (world == null || me == null) return 0;
            bool spin = Perk.Has("ww_spin"), venom = Perk.Has("ww_venom"), mod = Perks.IsModifier(Id);
            float radius = mod ? ModRadius : Radius * (spin ? SpinRadius : 1f);
            float secs = mod ? ModSeconds : Seconds + (spin ? SpinExtra : 0f);
            float speedLeft = mod ? ModSpeedLeft : SpeedLeft;
            float r2 = radius * radius, now = Time.time;

            HashSet<string> seen = null;
            if (key != 0 && !_perGrenade.TryGetValue(key, out seen)) _perGrenade[key] = seen = new HashSet<string>();

            int n = 0;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p.HealthController == null || !p.HealthController.IsAlive) continue;
                if ((p.Position - center).sqrMagnitude > r2) continue;
                var mc = p.MovementContext;
                if (mc == null) continue;
                if (p == me)
                {
                    // Entangled: tus propias telarañas tambien te frenan a ti (con otra causa, para que Terrain Walker no lo anule)
                    if (Perk.Has("dw_tangled")) mc.ChangeSpeedLimit(TangledLeft * mc.MaxSpeed, Player.ESpeedLimit.Shot, secs);
                    continue;
                }
                mc.ChangeSpeedLimit(speedLeft * mc.MaxSpeed, Player.ESpeedLimit.Swamp, secs);
                n++;

                float until;
                bool fresh = !_until.TryGetValue(p.ProfileId, out until) || until < now;
                _until[p.ProfileId] = now + secs;
                if (fresh)
                {
                    PerkService.AddProgress("ww_spin");                              // desafios de Spinneret y Sticky Floor: enemigos atrapados
                    PerkService.AddProgress("ww_sticky");
                    Achievements.Unlock("ww_first");                                 // logros: primer enemigo atrapado y 50 en total
                    PerkService.AddStat("widow_webbed", 1);
                    if (PerkService.Stat("widow_webbed") >= WebbedNeeded) Achievements.Unlock("ww_webbed");
                    if (venom || burst) Stun(p, StunSeconds);
                }
                if (seen != null) seen.Add(p.ProfileId);
            }
            if (seen != null)
            {
                if (seen.Count >= 3 && _fuseDone.Add(key)) PerkService.AddProgress("ww_fuse");     // desafio de Safe Fuse: 3 enemigos con una sola granada
                if (seen.Count >= WrapParty) Achievements.Unlock("ww_secret");                    // logro: 5 con una sola granada
            }
            return n;
        }

        // Aturdimiento (prototipo): el efecto de aturdimiento del juego + casi sin poder moverse unos segundos
        private static void Stun(Player p, float seconds)
        {
            try
            {
                var hc = p.ActiveHealthController;
                if (hc != null) hc.DoStun(seconds, 1f);
                var mc = p.MovementContext;
                if (mc != null) mc.ChangeSpeedLimit(0.05f * mc.MaxSpeed, Player.ESpeedLimit.Shot, seconds);
                Dbg.Log("EFECTO", "enemigo aturdido " + seconds.ToString("0.0") + " s");
            }
            catch (Exception e) { Dbg.Throttled("stunfail", 10f, "ERROR", "aturdimiento: " + e.Message); }
        }

        internal static bool IsWebbed(Player victim)
        {
            float until;
            return victim != null && _until.TryGetValue(victim.ProfileId, out until) && Time.time <= until + 1.5f;
        }

        // ---------------------------------------------------------------- bajas con granada
        // Una baja del jugador con una granada suya (con Widow's Wine activa): desafios de Web Burst y Widow's Bite, y la devolucion.
        internal static void OnGrenadeKill()
        {
            PerkService.AddProgress("ww_burst");
            PerkService.AddProgress("ww_bite");
            PerkService.AddStat("widow_gkills", 1);                                 // logro "Silk and shrapnel"
            if (PerkService.Stat("widow_gkills") >= GrenadeKillsNeeded) Achievements.Unlock("ww_grenades");
            if (!Perk.Has("ww_bite") || _lastRefunded || _lastTpl == null || Time.time - _lastTime > 20f) return;
            _lastRefunded = true;                                                   // una granada devuelve una sola vez, mate a cuantos mate
            PerkService.RequestRefund(_lastTpl, 1);
            Dbg.Log("EFECTO", "Widow's Bite: granada " + _lastTpl + " devuelta por correo");
            Hud.Notify(Data.Name("ww_bite"), L.T("notice.bite.sub"), Id);
        }

        // ---------------------------------------------------------------- Web Burst
        // Dano recibido (desde DamageHubPatch): 100 de dano en menos de 3 s dispara la ráfaga (una vez cada 5 min)
        internal static void OnDamageTaken(float damage)
        {
            if (!Perk.Has("ww_burst")) return;
            float now = Time.time;
            _hits.Add(new Vector2(now, damage));
            _hits.RemoveAll(h => now - h.x > BurstWindow);
            float sum = 0f;
            foreach (var h in _hits) sum += h.y;
            if (sum >= BurstDamage && now >= _nextBurst) { _burstPending = true; _hits.Clear(); }   // se resuelve en el siguiente Update, no en mitad del dano
        }

        private static void Burst(Player me)
        {
            try
            {
                var g = FindGrenade(me);
                if (g == null) { Dbg.Log("EFECTO", "Web Burst: no llevas ninguna granada que gastar"); return; }
                var op = ItemManipulator.Discard(g, me.InventoryController, true);       // true = solo prepara la operacion; la ejecuta TryRunNetworkTransaction
                if (op.Failed) { Dbg.Log("EFECTO", "Web Burst: no se pudo gastar la granada (" + op.Error + ")"); return; }
                me.InventoryController.TryRunNetworkTransaction(op).ContinueWith(t =>
                {
                    if (t.IsFaulted) Plugin.Log.LogWarning("Web Burst: no se pudo gastar la granada: " + t.Exception?.GetBaseException().Message);
                });

                _nextBurst = Time.time + BurstCooldown;
                int n = Web(me.Position, 0, true);
                Dbg.Log("EFECTO", "Web Burst: granada gastada (" + g.ShortName + "), " + n + " enemigo(s) atrapados y aturdidos");
                Hud.Notify(Data.Name("ww_burst"), L.F("notice.burst.sub", n), Id);
            }
            catch (Exception e) { Plugin.Log.LogError("Web Burst: " + e); }
        }

        // primera granada del inventario que se pueda gastar (no humo ni sonar, y no la que tengas en la mano)
        private static ThrowWeap FindGrenade(Player me)
        {
            var inHand = (me.HandsController as Player.ItemHandsController)?.Item;
            foreach (var it in me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment))
            {
                var tw = it as ThrowWeap;
                if (tw == null || ReferenceEquals(tw, inHand)) continue;
                if (tw.ThrowType == ThrowWeapType.smoke_grenade || tw.ThrowType == ThrowWeapType.sonar) continue;
                return tw;
            }
            return null;
        }
    }
}
