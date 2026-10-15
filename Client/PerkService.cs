using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Estado de progreso de las perks, guardado en el servidor por perfil (ver Server/Progress.cs):
    //   { xp:{perk:n}, prog:{perk:{id:n}}, researched:{perk:{id:true}}, equipped:{perk:{aug:{major,minor[]},drb:{...}}}, ach:{...} }
    // Todas las operaciones por perk reciben su id ("speedcola"...). Las que reciben el id de una mejora/desventaja deducen la perk
    // a la que pertenece (los ids de mejoras son unicos entre perks).
    internal static class PerkService
    {
        internal const double XpBonus = 0.25;   // +25% de XP con la perk activa

        private static JObject _state;

        internal static JObject State
        {
            get { EnsureLoaded(); return _state; }
        }

        private static JObject Default()
        {
            return new JObject { ["xp"] = new JObject(), ["prog"] = new JObject(), ["researched"] = new JObject(), ["equipped"] = new JObject() };
        }

        // Carga desde el servidor (una vez, o de nuevo si force=true).
        internal static void EnsureLoaded(bool force = false)
        {
            if (_state != null && !force) return;
            if (force && _dirty) FlushSync();          // lo que aun no se haya enviado se envia antes de volver a leer del servidor
            try
            {
                var resp = JObject.Parse(RequestHandler.PostJson("/tarkovacola/state/get", "{}"));
                var json = (string)resp["json"];
                _state = string.IsNullOrEmpty(json) ? Default() : JObject.Parse(json);
                Plugin.Log.LogInfo("Progreso de perks cargado (" + (string.IsNullOrEmpty(json) ? "nuevo" : "existente") + ")");
                Migrate();
                Story.SyncSales();                               // opcion de depuracion de ventas (F12)
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("No se pudo cargar el progreso del servidor: " + e.Message);
                if (_state == null) _state = Default();
            }
        }

        // Cambios de significado de desafios entre versiones: el progreso antiguo (segundos / golpes) se reinicia si el desafio no estaba completo
        private static void Migrate()
        {
            int v = (int?)_state["chv"] ?? 0;
            if (v >= 1) return;
            var prog = _state["prog"] as JObject;
            var jug = prog != null ? prog["juggernog"] as JObject : null;
            if (jug != null)
                foreach (var id in new[] { "jg_bones", "jg_thick" })
                    if (!IsDone(id) && jug.Remove(id)) Plugin.Log.LogInfo("Desafio " + id + " reiniciado (cambio de objetivo)");
            _state["chv"] = 1;
            Save();
        }

        // Guardar = marcar como pendiente: el envio ocurre unos segundos despues del ultimo cambio (los desafios suman progreso muy a
        // menudo, p. ej. cada 100 m recorridos). Flush() lo envia ya; TickSave() lo llama el plugin cada frame.
        private static bool _dirty;
        private static float _saveAt;

        internal static void Save()
        {
            if (_state == null) return;
            if (!_dirty) _saveAt = Time.realtimeSinceStartup + 2f;
            _dirty = true;
        }

        internal static void TickSave()
        {
            if (_dirty && Time.realtimeSinceStartup >= _saveAt) Flush();
        }

        private static string Body()
        {
            return new JObject { ["json"] = _state.ToString(Formatting.None) }.ToString(Formatting.None);
        }

        internal static void Flush()
        {
            if (_state == null || !_dirty) return;
            _dirty = false;
            try
            {
                RequestHandler.PostJsonAsync("/tarkovacola/state/save", Body()).ContinueWith(t =>
                {
                    if (t.IsFaulted) Plugin.Log.LogWarning("No se pudo guardar el progreso: " + t.Exception?.GetBaseException().Message);
                });
            }
            catch (Exception e) { Plugin.Log.LogWarning("Error guardando el progreso: " + e.Message); }
        }

        private static void FlushSync()
        {
            if (_state == null || !_dirty) return;
            _dirty = false;
            try { RequestHandler.PostJson("/tarkovacola/state/save", Body()); }
            catch (Exception e) { Plugin.Log.LogWarning("Error guardando el progreso: " + e.Message); }
        }

        // Pide al servidor que envie por correo una o mas unidades de un item (Widow's Bite: la granada que mato).
        internal static void RequestRefund(string tpl, int count)
        {
            try
            {
                var body = new JObject { ["tpl"] = tpl, ["count"] = count }.ToString(Formatting.None);
                RequestHandler.PostJsonAsync("/tarkovacola/refund", body).ContinueWith(t =>
                {
                    if (t.IsFaulted) Plugin.Log.LogWarning("No se pudo pedir la devolucion: " + t.Exception?.GetBaseException().Message);
                });
            }
            catch (Exception e) { Plugin.Log.LogWarning("Error pidiendo la devolucion: " + e.Message); }
        }

        private static JObject Sub(JObject parent, string key)
        {
            var o = parent[key] as JObject;
            if (o == null) parent[key] = o = new JObject();
            return o;
        }

        internal static JObject AchState { get { return Sub(State, "ach"); } }

        // contadores acumulados para logros (p. ej. metros corridos)
        internal static int Stat(string key) { return (int?)Sub(State, "stats")[key] ?? 0; }
        internal static void AddStat(string key, int n) { Sub(State, "stats")[key] = Stat(key) + n; Save(); }

        // perk de una mejora/desventaja (null si el id no existe)
        private static string PerkOf(string nodeId) { var p = Perks.OfNode(nodeId); return p != null ? p.Id : null; }

        // ---------- nivel / XP ----------
        internal const int MaxLevel = 5;         // nivel maximo de las perks

        // El nivel L necesita 2700 + 1350*L de XP (9 veces la primera version; se triplico dos veces). Subir de 1 a 5 = 24 300 XP.
        internal static int Need(int level) { return 2700 + 1350 * level; }

        internal static int Xp(string perk) { return (int?)Sub(State, "xp")[perk] ?? 0; }

        // En el nivel maximo la barra queda llena (cur == max) y el XP extra ya no cuenta.
        internal static void LevelInfo(int totalXp, out int level, out int cur, out int max)
        {
            level = 1; cur = totalXp;
            while (level < MaxLevel && cur >= Need(level)) { cur -= Need(level); level++; }
            max = Need(level);
            if (level >= MaxLevel) cur = max;
        }

        internal static int Level(string perk) { int l, c, m; LevelInfo(Xp(perk), out l, out c, out m); return l; }
        internal static bool IsMax(string perk) { return Level(perk) >= MaxLevel; }

        internal static void AddXp(string perk, int amount)
        {
            if (amount <= 0) return;
            if (ClassicMode.Enabled) { Dbg.Log("XP", perk + ": modo Classic, no se suma XP"); return; }       // el progreso de Modern queda intacto
            if (IsMax(perk)) { Dbg.Log("XP", perk + ": nivel maximo, no se suma XP"); return; }
            int before = Level(perk);
            Sub(State, "xp")[perk] = Xp(perk) + amount;
            Plugin.Log.LogInfo(perk + " +" + amount + " XP (total " + Xp(perk) + ")");
            if (Level(perk) > before && Perks.Get(perk).HasTree) Hud.Notify(L.F("notice.level.title", Perks.Get(perk).Name.ToUpperInvariant(), Level(perk)), L.T("notice.level.sub"), perk);
            Save();
            Achievements.CheckState();
        }

        // ---------- desafios ----------
        internal static bool IsDone(string id)
        {
            var perk = PerkOf(id);
            if (perk == null) return false;
            var r = Sub(State, "researched")[perk] as JObject;
            return r != null && (bool?)r[id] == true;
        }

        internal static int Progress(string id)
        {
            var perk = PerkOf(id);
            if (perk == null) return 0;
            var p = Sub(State, "prog")[perk] as JObject;
            return p == null ? 0 : (int?)p[id] ?? 0;
        }

        internal static bool IsActive(AugDef c) { return !IsDone(c.Id) && Level(c.Perk) >= c.Lvl; }

        internal static IEnumerable<AugDef> ActiveChallenges(string perk) { return Perks.Get(perk).Augs.Where(IsActive); }

        // Suma progreso a un desafio (solo si su nivel ya esta desbloqueado y no esta completo).
        internal static void AddProgress(string id, int n = 1)
        {
            if (ClassicMode.Enabled) return;                              // en Classic no hay Research: los desafios de mejoras no avanzan
            var c = Data.Aug(id);
            if (c == null || !IsActive(c)) return;

            var prog = Sub(Sub(State, "prog"), c.Perk);
            int now = Math.Min(c.Goal, ((int?)prog[id] ?? 0) + n);
            prog[id] = now;
            Dbg.Log("DESAFIO", Data.Name(id) + " " + now + "/" + c.Goal);
            if (now >= c.Goal)
            {
                Sub(Sub(State, "researched"), c.Perk)[id] = true;
                Plugin.Log.LogInfo("Desafio completado: " + id);
                Hud.Notify(L.T("notice.chal.title"), L.F("notice.chal.sub", Data.Name(id)), c.Perk);
            }
            Save();
            if (now >= c.Goal) Achievements.CheckState();
        }

        // ---------- equipamiento ----------
        // una mejora esta disponible si su desafio esta completo; una desventaja, si lo esta su mejora pareja
        internal static bool IsUnlocked(string id)
        {
            if (Data.IsDrawback(id)) { var a = Data.AugOf(id); return a != null && IsDone(a); }
            return IsDone(id);
        }

        // ranuras de mejoras menores (con el desbloqueo especial de la perk: 2); las desventajas siempre 1
        internal static int MinorSlots(string perk, bool augment)
        {
            if (!augment) return 1;
            return Perks.Get(perk).Augs.Any(a => a.Kind == NodeKind.Special && IsDone(a.Id)) ? 2 : 1;
        }

        // { aug:{major,minor[]}, drb:{major,minor[]} } de la perk
        internal static JObject Equipped(string perk)
        {
            var all = Sub(State, "equipped");
            var e = all[perk] as JObject;
            if (e == null)
            {
                all[perk] = e = new JObject
                {
                    ["aug"] = new JObject { ["major"] = null, ["minor"] = new JArray() },
                    ["drb"] = new JObject { ["major"] = null, ["minor"] = new JArray() },
                };
            }
            return e;
        }

        private static JObject Side(string perk, bool drawback) { return (JObject)Equipped(perk)[drawback ? "drb" : "aug"]; }

        internal static string EquippedMajor(string perk, bool drawback)
        {
            var t = Side(perk, drawback)["major"];
            return t != null && t.Type == JTokenType.String ? (string)t : null;
        }

        internal static List<string> EquippedMinors(string perk, bool drawback)
        {
            var arr = Side(perk, drawback)["minor"] as JArray;
            return arr == null ? new List<string>() : arr.Where(x => x.Type == JTokenType.String).Select(x => (string)x).ToList();
        }

        internal static bool IsEquipped(string id)
        {
            var perk = PerkOf(id);
            if (perk == null) return false;
            bool drb = Data.IsDrawback(id);
            return EquippedMajor(perk, drb) == id || EquippedMinors(perk, drb).Contains(id);
        }

        internal static bool IsMajor(string id)
        {
            var perk = Perks.OfNode(id);
            if (perk == null) return false;
            if (Data.IsDrawback(id)) return perk.DrbMajor.Contains(id);
            var a = Data.Aug(id);
            return a != null && a.Kind == NodeKind.Major;
        }

        // Hace cumplir la regla de pareja sobre lo que ya hay guardado: sin desventaja principal no hay mejora principal, y sin
        // desventaja secundaria no hay mejoras secundarias. Devuelve true si tuvo que desequipar algo.
        internal static bool Sanitize()
        {
            bool changed = false;
            foreach (var def in Perks.UiPerks)
            {
                if (EquippedMajor(def.Id, false) != null && EquippedMajor(def.Id, true) == null) { Side(def.Id, false)["major"] = null; changed = true; }
                if (EquippedMinors(def.Id, false).Count > 0 && EquippedMinors(def.Id, true).Count == 0) { Side(def.Id, false)["minor"] = new JArray(); changed = true; }
            }
            if (changed) { Save(); Dbg.Log("EQUIP", "estado saneado: mejoras sin su desventaja desequipadas"); }
            return changed;
        }

        // Equipa o desequipa. Devuelve un mensaje de error (ya traducido) si no esta permitido, o null si se hizo.
        // 'info' recibe un aviso si ademas se desequipo algo por regla.
        internal static string ToggleEquip(string id, out string info)
        {
            info = null;
            var perk = PerkOf(id);
            if (perk == null) return null;
            bool drb = Data.IsDrawback(id);
            var side = Side(perk, drb);

            if (IsEquipped(id))
            {
                if (EquippedMajor(perk, drb) == id)
                {
                    side["major"] = null;
                    if (drb && EquippedMajor(perk, false) != null)
                    {
                        Side(perk, false)["major"] = null;
                        info = L.T("toast.drbremoved");
                    }
                }
                else
                {
                    side["minor"] = new JArray(EquippedMinors(perk, drb).Where(x => x != id));
                    // al quitar la ultima desventaja secundaria, las mejoras secundarias tampoco pueden quedarse equipadas
                    if (drb && EquippedMinors(perk, true).Count == 0 && EquippedMinors(perk, false).Count > 0)
                    {
                        Side(perk, false)["minor"] = new JArray();
                        info = L.T("toast.drbremoved.minor");
                    }
                }
                Save();
                return null;
            }

            if (IsMajor(id))
            {
                if (!drb && EquippedMajor(perk, true) == null) return L.T("err.needdrb");
                side["major"] = id;
            }
            else
            {
                if (!drb && EquippedMinors(perk, true).Count == 0) return L.T("err.needdrbn");     // igual que las principales: sin su desventaja no hay mejora
                var list = EquippedMinors(perk, drb);
                list.Add(id);
                int n = MinorSlots(perk, !drb);
                side["minor"] = new JArray(list.Skip(Math.Max(0, list.Count - n)));   // si estan llenas, reemplaza la mas antigua
            }
            Save();
            return null;
        }

        // % de nodos desbloqueados de la perk
        internal static int Percent(string perk)
        {
            var augs = Perks.Get(perk).Augs;
            return augs.Length == 0 ? 0 : (int)Math.Round(100.0 * augs.Count(a => IsDone(a.Id)) / augs.Length);
        }
    }
}
