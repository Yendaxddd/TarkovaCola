using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Achievements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;

namespace TarkovaCola.Client
{
    // Logros NATIVOS de EFT (Perfil > Logros). El servidor los registra (Server/Achievements.cs); aqui se detecta cuando se cumplen
    // y se desbloquean con la funcion del propio juego, que muestra su aviso y sonido. El estado propio ("ach" en el progreso)
    // evita repetirlos; el servidor guarda el logro en el perfil y entrega la recompensa una sola vez.
    internal static class Achievements
    {
        internal class Def { public string Key, ServerId; public bool Secret; }

        internal static readonly Def[] All =
        {
            new Def { Key = "a_first", ServerId = "6a1c00000000000000000a01" },
            new Def { Key = "a_lvl5",  ServerId = "6a1c00000000000000000a02" },
            new Def { Key = "a_all",   ServerId = "6a1c00000000000000000a03" },
            new Def { Key = "a_found", ServerId = "6a1c00000000000000000a04" },
            new Def { Key = "a_die",   ServerId = "6a1c00000000000000000a05" },
            new Def { Key = "a_hand",  ServerId = "6a1c00000000000000000a06", Secret = true },
        };

        private static JObject Store { get { return PerkService.AchState; } }

        internal static bool IsDone(string key) { return (bool?)Store[key] == true; }

        internal static void Unlock(string key)
        {
            if (IsDone(key)) return;
            var def = All.First(a => a.Key == key);
            Store[key] = true;
            Dbg.Log("LOGRO", "desbloqueado: " + key);

            bool native = false;
            try
            {
                var me = Plugin.Me;
                AchievementsController controller = me != null ? me.AchievementsController : MenuController();
                native = controller != null && controller.UnlockAchievementForced(def.ServerId);
            }
            catch (Exception e) { Plugin.Log.LogWarning("No se pudo desbloquear el logro nativo " + key + ": " + e.Message); }
            if (!native) Hud.Notify(L.T("notice.ach.title"), (def.ServerId + " name").Localized());   // respaldo si el juego no lo mostro

            PerkService.Save();
            Claim(new[] { def.ServerId });
        }

        // El controlador de logros del menu principal vive dentro de la operacion de menu de la aplicacion.
        private static AchievementsController MenuController()
        {
            var app = Singleton<ClientApplication<IEftSession>>.Instance;
            if (app == null) return null;
            var op = app.GetType().GetField("_menuOperation", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(app);
            return op?.GetType().GetField("achievementsController", BindingFlags.Public | BindingFlags.Instance)?.GetValue(op) as AchievementsController;
        }

        // Logros que dependen del estado (nivel, mejoras): se comprueban al ganar XP, completar desafios y empezar raid.
        internal static void CheckState()
        {
            if (PerkService.Level >= PerkService.MaxLevel) Unlock("a_lvl5");
            if (Data.Augs.All(a => PerkService.IsDone(a.Id))) Unlock("a_all");
        }

        // Pide al servidor las recompensas pendientes (es idempotente: solo concede las que faltan).
        internal static void ClaimPending() { Claim(All.Where(a => IsDone(a.Key)).Select(a => a.ServerId).ToArray()); }

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
