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

        // ids (una por pareja de perks: 15 con 6 perks)
        internal const string Cardio = "syn_cardio", Iron = "syn_iron", Blast = "syn_blast";
        internal const string Medic = "syn_medic", Pin = "syn_pin", Trigger = "syn_trigger", Tank = "syn_tank", Bounce = "syn_bounce";
        internal const string Getaway = "syn_getaway", Breath = "syn_breath", Shock = "syn_shock", Sights = "syn_sights";
        internal const string WebBlast = "syn_webblast", Above = "syn_above", Spider = "syn_spider";

        // --- balance (bonos pequenos: solo se lleva una) ---
        internal const float CardioDraw = 0.12f;       // Speed Cola + Stamin-Up: sacar el arma +12%
        internal const float CardioStamina = 0.05f;    //   y correr gasta un 5% menos de estamina
        internal const float IronDamage = 0.03f;       // Juggernog + Widow's Wine: -3% de dano recibido
        internal const float BlastReload = 0.20f;      // Speed Cola + PHD Flopper: tras absorber una explosion o caida, recarga +20%...
        internal const float BlastSeconds = 6f;        //   ...durante 6 s
        internal const float MedicAnim = 1.15f;        // Speed Cola + Juggernog: curarse +15% rapido
        internal const float PinAnim = 1.15f;          // Speed Cola + Widow's Wine: lanzar granadas +15% rapido
        internal const float TriggerAim = 0.08f;       // Speed Cola + Deadshot: apuntar +8%
        internal const float TankWeight = 0.95f;       // Stamin-Up + Juggernog: peso -5%
        internal const float BounceStamina = 0.15f;    // Stamin-Up + PHD Flopper: al absorber, +15% de estamina
        internal const float GetawayStamina = 0.10f;   // Stamin-Up + Widow's Wine: al lanzar una granada, +10% de estamina
        internal const float BreathMult = 1.15f;       // Stamin-Up + Deadshot: aguantar la respiracion +15%
        internal const float ShockDamage = 0.10f;      // Juggernog + PHD Flopper: tras absorber, -10% de dano...
        internal const float ShockSeconds = 5f;        //   ...durante 5 s
        internal const float SightsDamage = 0.05f;     // Juggernog + Deadshot: apuntando, -5% de dano recibido
        internal const float WebRadius = 1.20f;        // PHD Flopper + Widow's Wine: telarana +20% de radio
        internal const float AboveSway = 0.60f;        // PHD Flopper + Deadshot: tras absorber, balanceo -40%...
        internal const float AboveSeconds = 5f;        //   ...durante 5 s (desafio: matar en los 10 s siguientes)
        internal const float AboveWindow = 10f;
        internal const float SpiderDamage = 1.10f;     // Widow's Wine + Deadshot: +10% de dano a enemigos atrapados

        internal static readonly SynDef[] All =
        {
            new SynDef { Id = Cardio,   A = "speedcola",  B = "staminup",   Goal = 3000, Unit = ProgUnit.Meters },   // correr 3 km
            new SynDef { Id = Medic,    A = "speedcola",  B = "juggernog",  Goal = 10 },                              // usar 10 objetos medicos
            new SynDef { Id = Blast,    A = "speedcola",  B = "phdflopper", Goal = 5 },                               // absorber 5 explosiones o caidas
            new SynDef { Id = Pin,      A = "speedcola",  B = "widowswine", Goal = 10 },                              // lanzar 10 granadas
            new SynDef { Id = Trigger,  A = "speedcola",  B = "deadshot",   Goal = 15 },                              // 15 bajas apuntando
            new SynDef { Id = Tank,     A = "staminup",   B = "juggernog",  Goal = 2000, Unit = ProgUnit.Meters },   // correr 2 km
            new SynDef { Id = Bounce,   A = "staminup",   B = "phdflopper", Goal = 8 },                               // absorber 8 explosiones o caidas
            new SynDef { Id = Getaway,  A = "staminup",   B = "widowswine", Goal = 10 },                              // lanzar 10 granadas
            new SynDef { Id = Breath,   A = "staminup",   B = "deadshot",   Goal = 20 },                              // aguantar la respiracion apuntando 20 veces
            new SynDef { Id = Shock,    A = "juggernog",  B = "phdflopper", Goal = 5 },                               // absorber 5 explosiones o caidas
            new SynDef { Id = Iron,     A = "juggernog",  B = "widowswine", Goal = 5 },                               // 5 bajas de enemigos atrapados
            new SynDef { Id = Sights,   A = "juggernog",  B = "deadshot",   Goal = 10 },                              // 10 bajas de enemigos con armadura
            new SynDef { Id = WebBlast, A = "phdflopper", B = "widowswine", Goal = 15 },                              // atrapar a 15 enemigos
            new SynDef { Id = Above,    A = "phdflopper", B = "deadshot",   Goal = 3 },                               // 3 bajas en los 10 s tras absorber
            new SynDef { Id = Spider,   A = "widowswine", B = "deadshot",   Goal = 8 },                               // 8 bajas de enemigos atrapados
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
        private static float _blastUntil, _shockUntil, _aboveUntil, _absorbAt = -99f;
        private static bool _blastWas;

        private static float P(string id) { return _eq.Count == 0 ? 0f : Power(id); }

        internal static float DrawMult { get { float p = P(Cardio); return p > 0f ? 1f + CardioDraw * p : 1f; } }
        internal static float StaminaSave { get { return P(Cardio) * CardioStamina; } }
        internal static float ReloadBoost
        {
            get { float p = P(Blast); return p > 0f && Time.time < _blastUntil ? 1f + BlastReload * p : 1f; }
        }
        internal static float MedicMult { get { float p = P(Medic); return p > 0f ? 1f + (MedicAnim - 1f) * p : 1f; } }
        internal static float PinMult { get { float p = P(Pin); return p > 0f ? 1f + (PinAnim - 1f) * p : 1f; } }
        internal static float AimBonus { get { return TriggerAim * P(Trigger); } }
        internal static float WeightMult { get { float p = P(Tank); return p > 0f ? 1f - (1f - TankWeight) * p : 1f; } }
        internal static float OxygenMult { get { float p = P(Breath); return p > 0f ? 1f + (BreathMult - 1f) * p : 1f; } }
        internal static float WebMult { get { float p = P(WebBlast); return p > 0f ? 1f + (WebRadius - 1f) * p : 1f; } }
        internal static float SwayFactor { get { float p = P(Above); return p > 0f && Time.time < _aboveUntil ? 1f - (1f - AboveSway) * p : 1f; } }
        internal static float OutgoingMult { get { float p = P(Spider); return p > 0f ? 1f + (SpiderDamage - 1f) * p : 1f; } }

        // dano que recibes: Telarana de acero siempre, Escudo de choque tras absorber, Mira de hierro apuntando
        internal static float DamageMult
        {
            get
            {
                if (_eq.Count == 0) return 1f;
                float m = 1f - IronDamage * P(Iron);
                if (Time.time < _shockUntil) m *= 1f - ShockDamage * P(Shock);
                float ps = P(Sights);
                if (ps > 0f) { var me = Plugin.Me; if (me != null && Deadshot.Aiming(me)) m *= 1f - SightsDamage * ps; }
                return m;
            }
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
                Achievements.OnSynergyUnlocked();
            }
        }

        private static readonly Dictionary<string, float> _meters = new Dictionary<string, float>();

        // Distancia corriendo (desde PerkFx.Tick): Cardio y Tanque
        internal static void OnRun(float meters)
        {
            foreach (var id in new[] { Cardio, Tank })
            {
                var s = Get(id);
                if (IsUnlocked(id) || !Both(s)) continue;
                float acc;
                _meters.TryGetValue(id, out acc);
                acc += meters;
                if (acc >= 10f) { int whole = (int)acc; acc -= whole; AddProgress(id, whole); }
                _meters[id] = acc;
            }
        }

        // PHD Flopper anulo una explosion o una caida
        internal static void OnAbsorb(Player me)
        {
            float now = Time.time;
            _absorbAt = now;
            AddProgress(Blast, 1); AddProgress(Bounce, 1); AddProgress(Shock, 1);
            if (P(Blast) > 0f)
            {
                _blastUntil = now + BlastSeconds;
                (me.HandsController as Player.FirearmController)?.SyncWithCharacterSkills();       // la recarga del arma en mano usa el nuevo multiplicador
                Hud.Notify(Name(Get(Blast)), L.F("syn.blast.sub", Mathf.RoundToInt(BlastReload * 100f), Mathf.RoundToInt(BlastSeconds)), "speedcola");
            }
            float pb = P(Bounce);
            if (pb > 0f && me.Physical != null && me.Physical.Stamina != null)
            {
                var st = me.Physical.Stamina;
                st.Current = Mathf.Min(st.TotalCapacity, st.Current + st.TotalCapacity * BounceStamina * pb);
            }
            if (P(Shock) > 0f) _shockUntil = now + ShockSeconds;
            if (P(Above) > 0f) _aboveUntil = now + AboveSeconds;
        }

        // Granada lanzada (desde WidowsWine.OnThrown: Widow's Wine activa)
        internal static void OnThrown(Player me)
        {
            AddProgress(Pin, 1); AddProgress(Getaway, 1);
            float pg = P(Getaway);
            if (pg > 0f && me != null && me.Physical != null && me.Physical.Stamina != null)
            {
                var st = me.Physical.Stamina;
                st.Current = Mathf.Min(st.TotalCapacity, st.Current + st.TotalCapacity * GetawayStamina * pg);
            }
        }

        // Objeto medico usado
        internal static void OnConsumed(EFT.InventoryLogic.Item item)
        {
            if (item is EFT.InventoryLogic.Meds) AddProgress(Medic, 1);
        }

        // Aguantar la respiracion apuntando (desde PerkFx.Tick, con Deadshot activa)
        internal static void OnBreath() { AddProgress(Breath, 1); }

        // Enemigo atrapado por primera vez en una telarana (desde WidowsWine.Web)
        internal static void OnWebbed() { AddProgress(WebBlast, 1); }

        // Baja del jugador (desde PerkFx.OnKill)
        internal static void OnKill(Player me, Player victim, bool webbed, bool armored)
        {
            if (webbed) { AddProgress(Iron, 1); AddProgress(Spider, 1); }
            if (Deadshot.Aiming(me)) AddProgress(Trigger, 1);
            if (armored) AddProgress(Sights, 1);
            if (Time.time - _absorbAt <= AboveWindow) AddProgress(Above, 1);
        }

        internal static void OnRaidStart()
        {
            _blastUntil = 0f; _shockUntil = 0f; _aboveUntil = 0f; _absorbAt = -99f; _blastWas = false;
            _meters.Clear();
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
