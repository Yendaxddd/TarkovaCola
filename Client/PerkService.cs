using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;

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
            try
            {
                var resp = JObject.Parse(RequestHandler.PostJson("/tarkovacola/state/get", "{}"));
                var json = (string)resp["json"];
                _state = string.IsNullOrEmpty(json) ? Default() : JObject.Parse(json);
                Plugin.Log.LogInfo("Progreso de perks cargado (" + (string.IsNullOrEmpty(json) ? "nuevo" : "existente") + ")");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("No se pudo cargar el progreso del servidor: " + e.Message);
                if (_state == null) _state = Default();
            }
        }

        internal static void Save()
        {
            if (_state == null) return;
            try
            {
                var body = new JObject { ["json"] = _state.ToString(Formatting.None) }.ToString(Formatting.None);
                RequestHandler.PostJsonAsync("/tarkovacola/state/save", body).ContinueWith(t =>
                {
                    if (t.IsFaulted) Plugin.Log.LogWarning("No se pudo guardar el progreso: " + t.Exception?.GetBaseException().Message);
                });
            }
            catch (Exception e) { Plugin.Log.LogWarning("Error guardando el progreso: " + e.Message); }
        }

        private static JObject Sub(JObject parent, string key)
        {
            var o = parent[key] as JObject;
            if (o == null) parent[key] = o = new JObject();
            return o;
        }

        internal static JObject AchState { get { return Sub(State, "ach"); } }

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
            if (IsMax(perk)) { Dbg.Log("XP", perk + ": nivel maximo, no se suma XP"); return; }
            int before = Level(perk);
            Sub(State, "xp")[perk] = Xp(perk) + amount;
            Plugin.Log.LogInfo(perk + " +" + amount + " XP (total " + Xp(perk) + ")");
            if (Level(perk) > before) Hud.Notify(L.F("notice.level.title", Perks.Get(perk).Name.ToUpperInvariant(), Level(perk)), L.T("notice.level.sub"), perk);
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
            return (int)Math.Round(100.0 * augs.Count(a => IsDone(a.Id)) / augs.Length);
        }
    }
}
