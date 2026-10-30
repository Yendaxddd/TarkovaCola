using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  APEX: un efecto MUY fuerte por perk. Solo se lleva UNO (en Modern y en Classic) y funciona con su perk activa.
    //   1) Se consigue un Nucleo APEX en raid (objeto del servidor, raro: cajas fuertes, alijos, jefes...). Al cogerlo se absorbe;
    //      si extraes con vida, queda un nucleo disponible (si mueres, se pierde).
    //   2) En Research > APEX se gasta un nucleo para empezar el desafio de un APEX (dificil; solo avanza con su perk activa).
    //   3) Superado el desafio, se puede equipar (uno a la vez).
    //  Estado en el JSON de progreso:  apex: { cores:n, started:{id:true}, prog:{id:n}, done:{id:true}, equipped:"id" }
    // ============================================================================================
    internal class ApexDef
    {
        public string Id;          // "apex_<perk>"
        public string Perk;
        public int Goal;
        public ProgUnit Unit;
    }

    internal static class Apex
    {
        internal const string CoreTpl = "6a1c00000000000000a0c001";       // Nucleo APEX (Server/Apex.cs)
        internal const float PenBonus = 10f;                               // Deadshot: +1 clase de blindaje = +10 de penetracion
        internal const float SprintBonus = 1.10f;                          // Stamin-Up: esprintar +10%
        internal const float VultureChance = 0.5f;
        internal const float WebTrigger = 3f;                              // Widow's Wine: distancia a un enemigo que hace detonar

        internal static readonly ApexDef[] All =
        {
            new ApexDef { Id = "apex_juggernog",      Perk = Juggernog.Id,      Goal = 1500 },                          // recibir 1.500 de dano
            new ApexDef { Id = "apex_speedcola",      Perk = "speedcola",       Goal = 40 },                            // 40 bajas
            new ApexDef { Id = "apex_staminup",       Perk = StaminUp.Id,       Goal = 15000, Unit = ProgUnit.Meters }, // esprintar 15 km
            new ApexDef { Id = "apex_phdflopper",     Perk = PhdFlopper.Id,     Goal = 30 },                            // absorber 30 explosiones o caidas
            new ApexDef { Id = "apex_widowswine",     Perk = WidowsWine.Id,     Goal = 40 },                            // atrapar a 40 enemigos
            new ApexDef { Id = "apex_deadshot",       Perk = Deadshot.Id,       Goal = 30 },                            // 30 bajas en la cabeza
            new ApexDef { Id = "apex_bandolier",      Perk = Bandolier.Id,      Goal = 60 },                            // 60 bajas
            new ApexDef { Id = "apex_vultureaid",     Perk = VultureAid.Id,     Goal = 25 },                            // registrar 25 cuerpos de PMC
            new ApexDef { Id = "apex_meleemacchiato", Perk = MeleeMacchiato.Id, Goal = 25 },                            // 25 bajas cuerpo a cuerpo
        };

        // objetos que puede llevar un enemigo con el APEX de Vulture Aid (valiosos y de misiones)
        private static readonly string[] VultureLoot =
        {
            "5c0530ee86f774697952d952" /* LEDX */, "57347ca924597744596b4e71" /* tarjeta grafica */, "59faff1d86f7746c51718c9c" /* bitcoin */,
            "5bc9bc53d4351e00367fbcee" /* gallo de oro */, "5c94bbff86f7747ee735c08f" /* tarjeta de Labs */, "5c05308086f7746b2101e90b" /* Virtex */,
            "5c05300686f7746dce784e5d" /* VPX */, "590c621186f774138d11ea29" /* memoria USB segura */, "5c12613b86f7743bbe2c3f76" /* carpeta de inteligencia */,
            "590a3efd86f77437d351a25b" /* analizador de gases */,
        };

        internal static ApexDef Get(string id) { return All.FirstOrDefault(a => a.Id == id); }
        internal static ApexDef ForPerk(string perk) { return All.FirstOrDefault(a => a.Perk == perk); }
        internal static string Name(ApexDef a) { return L.T(a.Id + ".n"); }
        internal static string Desc(ApexDef a) { return L.T(a.Id + ".d"); }
        internal static string Challenge(ApexDef a) { return L.T(a.Id + ".c"); }

        // ---------------------------------------------------------------- estado guardado
        private static JObject Root
        {
            get
            {
                var st = PerkService.State;
                var r = st["apex"] as JObject;
                if (r == null) st["apex"] = r = new JObject();
                return r;
            }
        }

        private static JObject Sub(string key)
        {
            var o = Root[key] as JObject;
            if (o == null) Root[key] = o = new JObject();
            return o;
        }

        internal static int Cores { get { return (int?)Root["cores"] ?? 0; } }
        internal static bool IsStarted(string id) { return (bool?)Sub("started")[id] == true; }
        internal static bool IsUnlocked(string id) { return (bool?)Sub("done")[id] == true; }
        internal static int Progress(string id) { return (int?)Sub("prog")[id] ?? 0; }
        internal static string Equipped { get { var e = (string)Root["equipped"]; return e != null && IsUnlocked(e) ? e : null; } }

        // Gasta un nucleo para empezar el desafio. Devuelve null si fue bien, o la clave del error.
        internal static string Start(string id)
        {
            if (Get(id) == null || IsStarted(id) || IsUnlocked(id)) return "toast.locked";
            if (Cores <= 0) return "apex.nocore";
            Root["cores"] = Cores - 1;
            Sub("started")[id] = true;
            PerkService.Save(); PerkService.Flush();
            Dbg.Log("APEX", "desafio empezado: " + id + " (quedan " + Cores + " nucleos)");
            return null;
        }

        internal static string Toggle(string id)
        {
            if (Get(id) == null || !IsUnlocked(id)) return "toast.locked";
            Root["equipped"] = Equipped == id ? null : id;              // solo uno: equipar otro sustituye al anterior
            PerkService.Save();
            Refresh();
            Dbg.Log("APEX", "equipado: " + (Equipped ?? "(ninguno)"));
            return null;
        }

        // ---------------------------------------------------------------- cache (se consulta en caminos calientes)
        private static string _eqPerk;

        internal static void Refresh()
        {
            try { var e = Get(Equipped ?? ""); _eqPerk = e != null ? e.Perk : null; }
            catch (Exception ex) { _eqPerk = null; Plugin.Log.LogWarning("APEX: no se pudo leer lo equipado: " + ex.Message); }
        }

        // el APEX de esta perk esta equipado y la perk activa
        internal static bool Has(string perk) { return _eqPerk != null && _eqPerk == perk && Perks.IsActive(perk); }

        // ---------------------------------------------------------------- desafios (solo avanzan con su perk activa)
        private static readonly Dictionary<string, float> _acc = new Dictionary<string, float>();

        internal static void AddProgress(string perk, float n)
        {
            var a = ForPerk(perk);
            if (a == null || n <= 0f || !IsStarted(a.Id) || IsUnlocked(a.Id) || !Perks.IsActive(perk)) return;
            float acc; _acc.TryGetValue(a.Id, out acc); acc += n;
            int whole = (int)acc;
            _acc[a.Id] = acc - whole;
            if (whole <= 0) return;
            int now = Mathf.Min(a.Goal, Progress(a.Id) + whole);
            Sub("prog")[a.Id] = now;
            PerkService.Save();
            Dbg.Throttled("apexprog" + a.Id, 5f, "APEX", a.Id + ": " + now + " / " + a.Goal);
            if (now < a.Goal) return;
            Sub("done")[a.Id] = true;
            PerkService.Save(); PerkService.Flush();
            Dbg.Log("APEX", a.Id + " desbloqueado");
            Hud.Enqueue(L.T("apex.unlocked"), Name(a), a.Perk);
        }

        internal static void OnDamageTaken(float damage) { AddProgress(Juggernog.Id, damage); }
        internal static void OnRun(float meters) { AddProgress(StaminUp.Id, meters); }
        internal static void OnAbsorb() { AddProgress(PhdFlopper.Id, 1f); }
        internal static void OnWebbed() { AddProgress(WidowsWine.Id, 1f); }
        internal static void OnLootedPmc() { AddProgress(VultureAid.Id, 1f); }

        // Baja del jugador (PerkFx.OnKill): desafios y efectos de Bandolier Bandit y PHD Flopper
        internal static void OnKill(Player me, Player victim, DamageInfo info, EBodyPart part)
        {
            AddProgress("speedcola", 1f);
            AddProgress(Bandolier.Id, 1f);
            if (part == EBodyPart.Head) AddProgress(Deadshot.Id, 1f);
            if ((info.DamageType & EDamageType.Melee) != 0) AddProgress(MeleeMacchiato.Id, 1f);
            if (Has(Bandolier.Id)) RefillMag(me);
            if (Has(PhdFlopper.Id) && victim != null) Blast(me, victim.Position + Vector3.up * 0.3f);
        }

        // ---------------------------------------------------------------- nucleos: se absorben al cogerlos y se guardan al extraer
        private static int _pending;
        private static float _next;
        private static readonly HashSet<string> _seen = new HashSet<string>();

        // OnRaidStart va en GameWorld.OnGameStarted (la raid ya empezo); se absorbe desde 3 s despues hasta el final de la raid
        private static float _absorbFrom = float.MaxValue;

        internal static void OnRaidStart() { _pending = 0; _next = 0f; _seen.Clear(); _acc.Clear(); _absorbFrom = Time.time + 3f; Refresh(); }

        internal static void OnRaidEnd(ExitStatus status)
        {
            _absorbFrom = float.MaxValue;
            bool alive = status == ExitStatus.Survived || status == ExitStatus.Runner || status == ExitStatus.Transit;
            if (_pending > 0)
            {
                if (alive) { Root["cores"] = Cores + _pending; PerkService.Save(); PerkService.Flush(); }
                Dbg.Log("APEX", _pending + " nucleo(s) " + (alive ? "guardados (total " + Cores + ")" : "perdidos (" + status + ")"));
            }
            _pending = 0;
        }

        // ---------------------------------------------------------------- cada frame en raid (Plugin)
        internal static void Tick(Player me)
        {
            if (me == null) return;
            if (Has(StaminUp.Id) && me.Physical != null)
            {
                if (me.Physical.Stamina != null) me.Physical.Stamina.Current = me.Physical.Stamina.TotalCapacity;
                if (me.Physical.HandsStamina != null) me.Physical.HandsStamina.Current = me.Physical.HandsStamina.TotalCapacity;
            }
            if (Has(WidowsWine.Id)) DetonateNear(me);
            if (Time.time < _next) return;
            _next = Time.time + 0.5f;
            AbsorbCores(me);
            if (Has(Juggernog.Id)) StopBleeding(me);
            if (Has(VultureAid.Id)) SeedEnemies(me);
        }

        // solo con la raid ya en marcha (en la cuenta atras quitar un objeto cerraba el juego) y en local, como Machines.Charge (la operacion
        // por red de un Remove no es del tipo que espera). Nunca con el inventario abierto: si el nucleo se esta arrastrando con el raton,
        // quitarlo deja a EFT con un arrastre de un objeto que ya no existe y la interfaz se rompe (3.0.1); se absorbe al cerrarlo.
        private static float _closedAt;
        private static void AbsorbCores(Player me)
        {
            if (Time.time < _absorbFrom) return;
            if (Hud.MenuOpen) { _closedAt = float.MaxValue; return; }
            if (_closedAt == float.MaxValue) _closedAt = Time.time;
            if (Time.time < _closedAt + 0.5f) return;
            foreach (var it in me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).Where(i => i != null && i.StringTemplateId == CoreTpl).ToList())
            {
                var op = ItemManipulator.Remove(it, me.InventoryController, false);
                if (op.Failed) { Dbg.Throttled("apexcore", 10f, "APEX", "no se pudo absorber el nucleo: " + op.Error); continue; }
                _pending++;
                Hud.Notify(L.T("apex.core.title"), L.T("apex.core.sub"), null);
                Dbg.Log("APEX", "nucleo absorbido (" + _pending + " en esta raid)");
            }
        }

        // Juggernog: ningun sangrado (los que aparezcan se cortan enseguida)
        private static void StopBleeding(Player me)
        {
            var hc = me.ActiveHealthController;
            if (hc == null) return;
            foreach (var e in hc.FindActiveEffects<ActiveHealthController.LightBleeding>(EBodyPart.Common).ToList()) e.ForceResidue();
            foreach (var e in hc.FindActiveEffects<ActiveHealthController.HeavyBleeding>(EBodyPart.Common).ToList()) e.ForceResidue();
        }

        // Widow's Wine: tus granadas explotan en cuanto pasan cerca de un enemigo
        private static void DetonateNear(Player me)
        {
            var world = Singleton<GameWorld>.Instance;
            if (world == null) return;
            foreach (var g in WidowsWine.Mine.ToList())
            {
                if (g == null) continue;
                var pos = g.transform.position;
                foreach (var p in world.AllAlivePlayersList)
                {
                    if (p == null || p.IsYourPlayer || BotPerks.IsFollower(p) || (p.Position - pos).sqrMagnitude > WebTrigger * WebTrigger) continue;
                    try { g.InvokeBlowUpEvent(); Dbg.Log("APEX", "Widow's Wine: granada detonada junto a " + p.Profile.Nickname); }
                    catch (Exception e) { Dbg.Throttled("apexww", 10f, "ERROR", "APEX (detonar): " + e.Message); }
                    break;
                }
            }
        }

        // Vulture Aid: la mitad de los enemigos llevan un objeto valioso o de mision (sin aviso)
        private static void SeedEnemies(Player me)
        {
            var world = Singleton<GameWorld>.Instance;
            if (world == null) return;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p.IsYourPlayer || !_seen.Add(p.ProfileId) || BotPerks.IsFollower(p)) continue;
                if (UnityEngine.Random.value >= VultureChance) continue;
                string tpl = VultureLoot[UnityEngine.Random.Range(0, VultureLoot.Length)];
                bool ok = NewPerkUtil.Give(p.Profile, tpl);
                Dbg.Log("APEX", "Vulture Aid: " + p.Profile.Nickname + " lleva " + tpl + (ok ? "" : " (no cupo)"));
            }
        }

        // Bandolier Bandit: cada baja rellena un cargador entero (el del arma o, si esta lleno, uno del chaleco o los bolsillos)
        private static void RefillMag(Player me)
        {
            var w = me.HandsController != null ? me.HandsController.Item as Weapon : null;
            var all = me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).ToList();
            var mag = w != null ? w.GetCurrentMagazine() : null;
            if (mag == null || mag.Count >= mag.MaxCount) mag = all.OfType<Magazine>().FirstOrDefault(m => m.Count < m.MaxCount && !(m.Parent.Container.ParentItem is Weapon) && m.Cartridges.Last != null);
            if (mag == null) return;
            string tpl = mag.Cartridges.Last != null ? mag.Cartridges.Last.TemplateId.ToString()
                       : w != null && w.Chambers.Length > 0 && w.Chambers[0].ContainedItem != null ? w.Chambers[0].ContainedItem.TemplateId.ToString() : null;
            if (tpl == null) return;
            int need = mag.MaxCount - mag.Count;
            if (!NewPerkUtil.Give(me.Profile, tpl, need)) return;
            var ammo = me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).OfType<Ammo>()
                .Where(a => a.TemplateId.ToString() == tpl && a.Parent.Container != null && !(a.Parent.Container.ParentItem is Magazine) && !(a.Parent.Container.ParentItem is Weapon))
                .OrderByDescending(a => a.StackObjectsCount).FirstOrDefault();
            if (ammo != null && NewPerkUtil.MoveRounds(me, ammo, mag, need)) Dbg.Log("APEX", "Bandolier: cargador rellenado (" + mag.Count + "/" + mag.MaxCount + ")");
        }

        // PHD Flopper: explosion de granada entera en el cuerpo de la victima (tu eres inmune con PHD)
        private static void Blast(Player me, Vector3 pos)
        {
            try
            {
                var w = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), "5448be9a4bdc2dfd2f8b456a", null) as ThrowWeap;   // RGD-5
                if (w == null) return;
                if (!string.IsNullOrEmpty(w.ExplosionEffectType)) Singleton<Systems.Effects.Effects>.Instance.EmitGrenade(w.ExplosionEffectType, pos, Vector3.up);
                var calc = Singleton<IGameLevel>.Instance != null ? Singleton<IGameLevel>.Instance.CreateBallisticCalculator(0) : null;
                Grenade.Explosion(null, w, pos, me.ProfileId, calc, w, new Vector3(0f, 0.08f, 0f));
                Dbg.Log("APEX", "PHD Flopper: explosion en " + pos.ToString("F1"));
            }
            catch (Exception e) { Dbg.Throttled("apexphd", 10f, "ERROR", "APEX (explosion): " + e.Message); }
        }
    }

    // ---------- Speed Cola: el arma no se encasquilla ----------
    [HarmonyPatch(typeof(Player.FirearmController), "GetTotalMalfunctionChance")]
    internal static class ApexJamPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Player.FirearmController __instance, ref float __result)
        {
            if (Apex.Has("speedcola") && Util.IsMe(__instance._player)) __result = 0f;
        }
    }

    // ---------- Stamin-Up: esprintar +10% (la velocidad es (fisico * esprint + 1); se ajusta el esprint para que el total suba un 10%) ----------
    [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.SprintingSpeed), MethodType.Getter)]
    internal static class ApexSprintPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MovementContext __instance, ref float __result)
        {
            if (!Apex.Has(StaminUp.Id)) return;
            var me = Plugin.Me;
            if (me == null || !ReferenceEquals(me.MovementContext, __instance) || me.Physical == null) return;
            float s = me.Physical.SprintSpeed;
            if (s > 0.01f) __result = (Apex.SprintBonus * (s * __result + 1f) - 1f) / s;
        }
    }
}
