using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.Communications;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Eternal Perk (modo Modern, como en Black Ops 7)
    //   - Si bebes (o te cae del powerup) una perk que YA llevas activa, se vuelve ETERNA: su icono lleva un contorno azul.
    //   - Una perk eterna pierde una desventaja: la mayor que lleve equipada y, si no tiene, una menor. Si no lleva ninguna se queda como esta.
    //   - Si mueres en la raid, la botella de cada perk eterna vuelve por correo (si extraes con vida no hace falta: sigues teniendola).
    //  Es PERSISTENTE: la perk eterna se queda contigo en las siguientes raids (activa desde el primer segundo) hasta que mueres
    //  (o no extraes): entonces vuelve la botella por correo y deja de ser eterna. Se ve en la pestana ETERNAS de Research.
    //  En Classic no existe: alli no se puede beber una perk que ya llevas.
    //  Estado guardado:  eternal: [perkId, ...]
    // ============================================================================================
    internal static class Eternal
    {
        private static readonly HashSet<string> _set = new HashSet<string>();
        private static readonly Dictionary<string, string> _removed = new Dictionary<string, string>();     // perk -> desventaja que se quito
        private static bool _paid;                                    // la botella ya se pidio en esta raid

        internal static bool Is(string perkId) { return perkId != null && _set.Contains(perkId); }
        internal static bool Any { get { return _set.Count > 0; } }
        internal static int Count { get { return _set.Count; } }
        internal static IEnumerable<PerkDef> List { get { return Perks.All.Where(p => _set.Contains(p.Id)); } }

        internal static void OnRaidStart() { _set.Clear(); _removed.Clear(); _paid = false; }

        // ---------------------------------------------------------------- lo guardado entre raids
        internal static List<string> Stored()
        {
            var list = new List<string>();
            var arr = PerkService.State["eternal"] as Newtonsoft.Json.Linq.JArray;
            if (arr != null) foreach (var t in arr) if (t.Type == Newtonsoft.Json.Linq.JTokenType.String && Perks.Get((string)t) != null && !list.Contains((string)t)) list.Add((string)t);
            return list;
        }

        private static void Store(List<string> list)
        {
            PerkService.State["eternal"] = new Newtonsoft.Json.Linq.JArray(list);
            PerkService.Save();
            PerkService.Flush();                                     // lo importante se guarda ya
        }

        // La desventaja que se quitaria / se quito a una perk eterna (para mostrarla en Research): la mayor equipada, si no una menor
        internal static string PreviewDrawback(PerkDef def)
        {
            var major = PerkService.EquippedMajor(def.Id, true);
            if (major != null) return major;
            var minors = PerkService.EquippedMinors(def.Id, true);
            return minors.Count > 0 ? minors[0] : null;
        }

        // Al empezar la raid (Modern): las perks eternas guardadas vuelven a estar activas. Se llama despues de Perk.Refresh().
        internal static void Restore()
        {
            if (ClassicMode.Enabled) return;
            var stored = Stored();
            if (stored.Count == 0) return;
            foreach (var id in stored)
            {
                var def = Perks.Get(id);
                Perks.Activate(id);
                _set.Add(id);
                string drb = PickDrawback(def);
                if (drb != null) _removed[id] = drb;
            }
            Perk.Refresh();                                          // las desventajas quitadas salen del equipamiento activo
            Dbg.Log("ETERNAL", "perks eternas activas: " + string.Join(", ", stored.ToArray()));
        }

        // El jugador ya existe: se recalculan estamina, aire y peso, y se avisa
        internal static void OnPlayerReady(Player me)
        {
            if (_set.Count == 0) return;
            PerkFx.RefreshPhysical(me);
            (me.HandsController as Player.FirearmController)?.SyncWithCharacterSkills();
            Hud.Notify(L.T("eternal.title"), L.F("eternal.start", _set.Count), List.First().Id);
        }

        // Modern: no se puede beber una perk que ya es eterna (no ganaria nada)
        internal static string BlockReason(PerkDef def)
        {
            return Is(def.Id) ? L.F("eternal.block", def.Name) : null;
        }

        // La desventaja que se quita: la mayor equipada de la perk; si no, una menor equipada
        private static string PickDrawback(PerkDef def)
        {
            foreach (var id in def.DrbMajor) if (Perk.Equipped(id)) return id;
            foreach (var id in def.DrbMinor) if (Perk.Equipped(id)) return id;
            return null;
        }

        // Perk.Refresh() lo llama al final: las desventajas quitadas dejan de contar como equipadas
        internal static void Strip(HashSet<string> equipped)
        {
            foreach (var kv in _removed) equipped.Remove(kv.Value);
        }

        // La perk (activa) pasa a ser eterna. 'viaPowerup': viene del powerup (la voz y los avisos esperan a los del powerup).
        internal static void Grant(PerkDef def, Player me, bool viaPowerup)
        {
            if (!_set.Add(def.Id)) return;
            string drb = PickDrawback(def);
            if (drb != null) _removed[def.Id] = drb;
            Perk.Refresh();                                          // las desventajas quitadas salen del equipamiento activo
            if (me != null) PerkFx.RefreshPhysical(me);              // capacidad de estamina / peso sin la desventaja
            Dbg.Log("ETERNAL", def.Id + " es eterna" + (drb != null ? ", desventaja quitada: " + drb : ", sin desventajas que quitar"));

            Jingle.Voice(Jingle.EternalVoice, viaPowerup ? Powerup.EternalVoiceDelay : 0f);
            string sub = drb != null ? L.F("eternal.sub.drb", def.Name, Data.Name(drb)) : L.F("eternal.sub.none", def.Name);
            if (viaPowerup) Hud.Enqueue(L.T("eternal.title"), sub, def.Id);
            else Hud.Notify(L.T("eternal.title"), sub, def.Id);
            Hud.Enqueue(L.T("eternal.title"), L.T("eternal.promise"), def.Id);
            var stored = Stored(); if (!stored.Contains(def.Id)) { stored.Add(def.Id); Store(stored); }
        }

        // El operador murio (o no extrajo): vuelve la botella de cada perk eterna
        internal static void OnDeath(string why)
        {
            if (_paid || _set.Count == 0) return;
            _paid = true;
            var names = new List<string>();
            foreach (var def in List)
            {
                PerkService.RequestRefund(def.ItemTpl, 1);
                names.Add(def.Name);
            }
            Dbg.Log("ETERNAL", "botellas devueltas por correo (" + why + "): " + string.Join(", ", names.ToArray()));
            Store(new List<string>());                                // ya no son eternas
            try { NotificationManager.DisplayMessageNotification(L.F("eternal.returned", string.Join(", ", names.ToArray()))); }
            catch (Exception e) { Dbg.Log("ETERNAL", "no se pudo mostrar el aviso: " + e.Message); }
        }

        internal static void OnRaidEnd(ExitStatus status)
        {
            bool alive = status == ExitStatus.Survived || status == ExitStatus.Runner;
            if (!alive && status != ExitStatus.Transit) OnDeath("fin de raid: " + status);
        }
    }
}
