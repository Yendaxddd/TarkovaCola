using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Communications;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Modo Classic (al estilo de Black Ops / MWZ):
    //   - No hay Research ni desventajas: cada perk lleva 1 mejora MENOR al azar (visible) al beberla.
    //   - Hasta 4 perks principales; la 5.a perk bebida es un "modificador": da solo una parte de su efecto base y no lleva mejora.
    //   - Las perks PERSISTEN entre raids: se quedan activas al extraer y se pierden solo cuando el operador muere (o no extrae).
    //   - Cada raid trae un desafio opcional (ClassicChallenge): si se cumple y extraes con vida, una perk principal al azar gana
    //     una 2.a mejora menor (maximo 2 por perk).
    //   - En Classic no se gana XP ni progreso de desafios de Research: el progreso del modo Modern queda intacto.
    //  El estado vive en el JSON de progreso del perfil (servidor):  mode: "classic"|"modern"
    //     classic: { perks:[{id, mod, minors:[...]}], last:"<desafio>", reward:{perk, aug} }
    // ============================================================================================
    internal static class ClassicMode
    {
        internal const int MaxMain = 4;           // perks principales
        internal const int MaxMinors = 2;         // mejoras menores por perk: 1 al beberla + 1 del desafio de raid

        internal class Entry
        {
            public string Perk;
            public bool Modifier;
            public List<string> Minors = new List<string>();
        }

        private static bool _latched;             // el modo con el que empezo la raid actual
        private static float _refreshAt;

        // ---------------------------------------------------------------- modo
        internal static bool Enabled
        {
            get
            {
                var t = PerkService.State["mode"];
                return t != null && t.Type == JTokenType.String && (string)t == "classic";
            }
        }

        // Cambia de modo (solo desde el menu). Nunca durante una raid.
        internal static bool SetEnabled(bool classic)
        {
            if (Plugin.InRaidWorld) return false;
            if (Enabled == classic) return true;
            PerkService.State["mode"] = classic ? "classic" : "modern";
            PerkService.Save();
            PerkService.Flush();
            Dbg.Log("MODO", "modo cambiado a " + (classic ? "Classic" : "Modern"));
            return true;
        }

        // ---------------------------------------------------------------- estado de las perks persistentes
        private static JObject Root
        {
            get
            {
                var s = PerkService.State;
                var c = s["classic"] as JObject;
                if (c == null) s["classic"] = c = new JObject();
                return c;
            }
        }

        internal static List<Entry> Entries()
        {
            var list = new List<Entry>();
            var arr = Root["perks"] as JArray;
            if (arr == null) return list;
            foreach (var t in arr.OfType<JObject>())
            {
                var id = (string)t["id"];
                if (id == null || Perks.Get(id) == null || list.Any(x => x.Perk == id)) continue;
                var e = new Entry { Perk = id, Modifier = (bool?)t["mod"] == true };
                var m = t["minors"] as JArray;
                if (m != null) foreach (var x in m) if (x.Type == JTokenType.String && Perks.OfNode((string)x) != null) e.Minors.Add((string)x);
                list.Add(e);
            }
            return list;
        }

        private static void Store(List<Entry> list)
        {
            Root["perks"] = new JArray(list.Select(e => new JObject { ["id"] = e.Perk, ["mod"] = e.Modifier, ["minors"] = new JArray(e.Minors) }));
            PerkService.Save();
        }

        internal static Entry Find(string perkId) { return Entries().FirstOrDefault(e => e.Perk == perkId); }

        // mejoras menores equipadas por las perks principales (alimenta Perk.Has)
        internal static IEnumerable<string> EquippedAugments()
        {
            return Entries().Where(e => !e.Modifier).SelectMany(e => e.Minors);
        }

        internal static bool HasEligiblePerk() { return Entries().Any(e => !e.Modifier && e.Minors.Count < MaxMinors); }

        // la mejora que acaba de ganar una perk con el desafio de raid (se muestra como NUEVA hasta la siguiente raid)
        internal static string RewardPerk { get { var r = Root["reward"] as JObject; return r != null ? (string)r["perk"] : null; } }
        internal static string RewardAug { get { var r = Root["reward"] as JObject; return r != null ? (string)r["aug"] : null; } }

        internal static string LastChallenge { get { var t = Root["last"]; return t != null && t.Type == JTokenType.String ? (string)t : null; } }
        internal static void SetLastChallenge(string id) { Root["last"] = id; PerkService.Save(); }

        // Una mejora menor de la perk al azar, distinta de las que ya lleva
        private static string Roll(PerkDef def, ICollection<string> have)
        {
            var pool = def.AugsOf(NodeKind.Minor).Select(a => a.Id).Where(id => !have.Contains(id)).ToList();
            return pool.Count == 0 ? null : pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        // ---------------------------------------------------------------- beber
        // null = se puede beber; si no, el motivo (ya traducido) por el que no
        internal static string DrinkBlockReason(PerkDef def)
        {
            if (!Enabled) return Eternal.BlockReason(def);              // Modern: solo se bloquea beber una perk que ya es eterna
            var list = Entries();
            if (list.Any(e => e.Perk == def.Id)) return L.F("classic.block.dup", def.Name);
            if (list.Count(e => !e.Modifier) >= MaxMain && list.Any(e => e.Modifier)) return L.F("classic.block.full", MaxMain);
            return null;
        }

        // Se acaba de beber una perk en modo Classic. Devuelve la entrada creada (null si no se pudo: se devuelve la botella).
        internal static Entry OnDrink(PerkDef def)
        {
            var list = Entries();
            if (list.Any(e => e.Perk == def.Id)) { Refund(def, "ya activa"); return null; }       // (no deberia pasar: se bloquea antes)
            var entry = new Entry { Perk = def.Id };
            if (list.Count(e => !e.Modifier) < MaxMain)
            {
                var aug = Roll(def, entry.Minors);
                if (aug != null) entry.Minors.Add(aug);
            }
            else if (!list.Any(e => e.Modifier)) entry.Modifier = true;
            else { Refund(def, "perks al maximo"); return null; }

            list.Add(entry);
            Store(list);
            PerkService.Flush();                                          // lo importante se guarda ya: si el juego se cierra no se pierde
            Dbg.Log("CLASSIC", def.Id + " activada como " + (entry.Modifier ? "modificador" : "principal") + (entry.Minors.Count > 0 ? ", mejora: " + entry.Minors[0] : ""));
            return entry;
        }

        // Despues de activar la perk: avisos de la mejora (o del modificador) y desafio de raid si aun no habia
        internal static void AfterDrink(PerkDef def, Entry entry)
        {
            if (entry.Modifier)
                Hud.Enqueue(L.T("classic.tag.mod"), L.F("classic.mod.sub", Mathf.RoundToInt(Perks.ModifierPower * 100f)), def.Id);
            else if (entry.Minors.Count > 0)
                Hud.Enqueue(Data.Name(entry.Minors[0]), Data.Desc(entry.Minors[0]), def.Id);
            ClassicChallenge.EnsureRolled();
        }

        private static void Refund(PerkDef def, string why)
        {
            Dbg.Log("CLASSIC", "botella de " + def.Id + " devuelta por correo (" + why + ")");
            PerkService.RequestRefund(def.ItemTpl, 1);
            Hud.Notify(L.T("classic.title"), L.F("classic.refund", def.Name), def.Id);
        }

        // ---------------------------------------------------------------- ciclo de la raid
        internal static void OnRaidStart()
        {
            _latched = Enabled;
            _refreshAt = 0f;
            ClassicChallenge.OnRaidStart();
            if (!_latched) return;
            var list = Entries();
            foreach (var e in list) Perks.Activate(e.Perk, e.Modifier);   // perks persistentes: activas desde el primer segundo
            Dbg.Log("CLASSIC", "raid en modo Classic, perks persistentes: " + (list.Count == 0 ? "(ninguna)" : string.Join(", ", list.Select(e => e.Perk + (e.Modifier ? "(mod)" : "") + "[" + string.Join("+", e.Minors.ToArray()) + "]").ToArray())));
        }

        // El jugador ya existe (primer ciclo del Tracker): se recalculan estamina, aire y peso con las perks ya activas y se
        // avisa de lo que hay activo. Tambien tira el desafio de la raid.
        internal static void OnPlayerReady(Player me)
        {
            if (!_latched) return;
            if (Root.Remove("reward")) PerkService.Save();                // la marca "NUEVA" solo dura hasta la siguiente raid de verdad (no el hideout)
            var list = Entries();
            if (list.Count > 0)
            {
                ApplyEffects(me);
                _refreshAt = Time.time + 2.5f;                            // y otra vez poco despues, por si el equipo aun se estaba cargando
                Hud.Notify(L.T("classic.title"), L.F("classic.start.sub", list.Count), list[0].Perk);
            }
            ClassicChallenge.EnsureRolled();
        }

        internal static void Tick(Player me)
        {
            if (_refreshAt > 0f && Time.time >= _refreshAt) { _refreshAt = 0f; ApplyEffects(me); }
        }

        private static void ApplyEffects(Player me)
        {
            PerkFx.RefreshPhysical(me);
            (me.HandsController as Player.FirearmController)?.SyncWithCharacterSkills();     // recarga de Speed Cola en el arma que ya llevas
        }

        // El operador murio: se pierden todas las perks de Classic
        internal static void OnDeath()
        {
            if (!_latched) return;
            Dbg.Log("CLASSIC", "el operador murio");
            ClearPerks();
            try { NotificationManager.DisplayMessageNotification(L.T("classic.lost")); }
            catch (Exception e) { Dbg.Log("CLASSIC", "no se pudo mostrar el aviso de muerte: " + e.Message); }
        }

        // Fin de la raid con su resultado: sin extraer se pierden las perks; con vida, el desafio puede dar una 2.a mejora.
        internal static void OnRaidEnd(ExitStatus status)
        {
            if (!_latched) return;
            bool alive = status == ExitStatus.Survived || status == ExitStatus.Runner;
            if (!alive && status != ExitStatus.Transit) { ClearPerks(); return; }
            if (alive && ClassicChallenge.CompletedAtEnd()) GrantSecondAugment();
        }

        private static void ClearPerks()
        {
            if (Entries().Count == 0) return;
            Root["perks"] = new JArray();
            Root.Remove("reward");
            PerkService.Save();
            PerkService.Flush();
            Dbg.Log("CLASSIC", "perks de Classic perdidas");
        }

        // Una perk principal al azar (con menos de 2 mejoras) gana una mejora menor mas
        private static void GrantSecondAugment()
        {
            var list = Entries();
            var eligible = list.Where(e => !e.Modifier && e.Minors.Count < MaxMinors).ToList();
            if (eligible.Count == 0) { Dbg.Log("CLASSIC", "desafio cumplido, pero ninguna perk puede ganar mas mejoras"); return; }
            var e2 = eligible[UnityEngine.Random.Range(0, eligible.Count)];
            var def = Perks.Get(e2.Perk);
            var aug = Roll(def, e2.Minors);
            if (aug == null) return;
            e2.Minors.Add(aug);
            Store(list);
            Root["reward"] = new JObject { ["perk"] = e2.Perk, ["aug"] = aug };
            PerkService.Flush();
            Dbg.Log("CLASSIC", "desafio de raid cumplido: " + e2.Perk + " gana la mejora " + aug);
            try { NotificationManager.DisplayMessageNotification(L.F("cc.reward", def.Name, Data.Name(aug))); }
            catch (Exception e) { Dbg.Log("CLASSIC", "no se pudo mostrar el aviso de recompensa: " + e.Message); }
        }
    }

    // ---------- Beber de mas: en Classic no se puede beber una perk que ya llevas ni una 6.a ----------
    // Se corta antes de empezar a beber (Player.Proceed para comida/bebida, que el cliente sobrescribe): el callback falla, asi que la
    // botella no se consume. Si aun asi llegara a beberse, DrinkPatch la devuelve por correo.
    [HarmonyPatch]
    internal static class DrinkBlockPatch
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            return typeof(Player).Assembly.GetTypes()
                .Where(t => typeof(Player).IsAssignableFrom(t))
                .SelectMany(t => t.GetMethods(flags))
                .Where(m => m.Name == "Proceed" && !m.IsAbstract && m.GetParameters().Length >= 4 && m.GetParameters()[0].ParameterType == typeof(EFT.InventoryLogic.FoodDrink));
        }

        [HarmonyPrefix]
        private static bool Prefix(Player __instance, EFT.InventoryLogic.FoodDrink foodDrink, Callback<IMedsController> callback)
        {
            try
            {
                if (!Util.IsMe(__instance) || foodDrink == null) return true;
                if (ClassicMode.Enabled && !Plugin.CfgClassicBlock.Value) return true;      // (el bloqueo de Classic se puede apagar en F12)
                var def = Perks.ByItem(foodDrink.StringTemplateId);
                if (def == null) return true;
                string why = ClassicMode.DrinkBlockReason(def);
                if (why == null) return true;

                Dbg.Log("CLASSIC", "beber " + def.Id + " bloqueado: " + why);
                Hud.Notify(L.T(ClassicMode.Enabled ? "classic.title" : "eternal.title"), why, def.Id);
                __instance.SetInHands(false);                                           // el cuerpo no debe quedarse en "usando un objeto"
                if (callback != null) callback(new Result<IMedsController>(null, why));  // fallo normal: la interfaz se recupera
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Classic (bloquear bebida): " + e);
                return true;
            }
        }
    }
}
