using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;

namespace TarkovaCola.Client
{
    // Logros de Tarkova-Cola. El cliente detecta cada logro y lo guarda en el estado del perfil; el servidor entrega
    // la recompensa (correo de Therapist) una sola vez por logro (Server/Achievements.cs).
    internal static class Achievements
    {
        internal class Def { public string Id; public bool Secret; }

        internal static readonly Def[] All =
        {
            new Def { Id = "a_first" },
            new Def { Id = "a_lvl5" },
            new Def { Id = "a_all" },
            new Def { Id = "a_found" },
            new Def { Id = "a_die" },
            new Def { Id = "a_hand", Secret = true },
        };

        private static JObject Store { get { return PerkService.AchState; } }

        internal static bool IsDone(string id) { return (bool?)Store[id] == true; }
        internal static int Count { get { return All.Count(a => IsDone(a.Id)); } }

        internal static void Unlock(string id)
        {
            if (IsDone(id)) return;
            Store[id] = true;
            Dbg.Log("LOGRO", "desbloqueado: " + id);
            Hud.Notify(L.T("notice.ach.title"), L.T(id + ".n"));
            PerkService.Save();
            Claim(new[] { id });
        }

        // Logros que dependen del estado (nivel, mejoras): se comprueban al ganar XP, completar desafios y empezar raid.
        internal static void CheckState()
        {
            if (PerkService.Level >= PerkService.MaxLevel) Unlock("a_lvl5");
            if (Data.Augs.All(a => PerkService.IsDone(a.Id))) Unlock("a_all");
        }

        // Pide al servidor las recompensas pendientes (es idempotente: solo entrega las que faltan).
        internal static void ClaimPending() { Claim(All.Where(a => IsDone(a.Id)).Select(a => a.Id).ToArray()); }

        private static void Claim(string[] ids)
        {
            if (ids.Length == 0) return;
            try
            {
                var body = new JObject { ["ids"] = new JArray(ids) }.ToString(Formatting.None);
                RequestHandler.PostJsonAsync("/tarkovacola/ach/claim", body).ContinueWith(t =>
                {
                    if (t.IsFaulted) Plugin.Log.LogWarning("No se pudo reclamar la recompensa de logros: " + t.Exception?.GetBaseException().Message);
                });
            }
            catch (Exception e) { Plugin.Log.LogWarning("Error reclamando logros: " + e.Message); }
        }
    }
}
