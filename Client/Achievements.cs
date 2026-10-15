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
    internal static partial class Achievements
    {
        internal class Def { public string Key, ServerId; public bool Secret; }

        private static Def[] _all;
        internal static Def[] All { get { return _all ?? (_all = Base.Concat(Extra).ToArray()); } }

        internal static readonly Def[] Base =
        {
            new Def { Key = "a_first", ServerId = "6a1c00000000000000000a01" },
            new Def { Key = "a_lvl5",  ServerId = "6a1c00000000000000000a02" },
            new Def { Key = "a_all",   ServerId = "6a1c00000000000000000a03" },
            new Def { Key = "a_found", ServerId = "6a1c00000000000000000a04" },
            new Def { Key = "a_die",   ServerId = "6a1c00000000000000000a05" },
            new Def { Key = "a_hand",  ServerId = "6a1c00000000000000000a06", Secret = true },
            new Def { Key = "st_fresh",    ServerId = "6a1c00000000000000000a07" },
            new Def { Key = "st_rushb",    ServerId = "6a1c00000000000000000a08" },
            new Def { Key = "st_marathon", ServerId = "6a1c00000000000000000a09" },
            new Def { Key = "st_lvl5",     ServerId = "6a1c00000000000000000a0a" },
            new Def { Key = "st_cardio",   ServerId = "6a1c00000000000000000a0b" },
            new Def { Key = "st_double",   ServerId = "6a1c00000000000000000a0c", Secret = true },
            // Juggernog
            new Def { Key = "jg_fresh",    ServerId = "6a1c00000000000000000a0d" },
            new Def { Key = "jg_rude",     ServerId = "6a1c00000000000000000a0e" },
            new Def { Key = "jg_red",      ServerId = "6a1c00000000000000000a0f" },
            new Def { Key = "jg_lvl5",     ServerId = "6a1c00000000000000000a10" },
            new Def { Key = "jg_all",      ServerId = "6a1c00000000000000000a11" },
            new Def { Key = "jg_secret",   ServerId = "6a1c00000000000000000a12", Secret = true },
            // PHD Flopper
            new Def { Key = "pf_flop",     ServerId = "6a1c00000000000000000a13" },
            new Def { Key = "pf_hugs",     ServerId = "6a1c00000000000000000a14" },
            new Def { Key = "pf_gravity",  ServerId = "6a1c00000000000000000a15" },
            new Def { Key = "pf_lvl5",     ServerId = "6a1c00000000000000000a16" },
            new Def { Key = "pf_all",      ServerId = "6a1c00000000000000000a17" },
            new Def { Key = "pf_secret",   ServerId = "6a1c00000000000000000a18", Secret = true },
            // Widow's Wine
            new Def { Key = "ww_first",    ServerId = "6a1c00000000000000000a19" },
            new Def { Key = "ww_webbed",   ServerId = "6a1c00000000000000000a1a" },
            new Def { Key = "ww_grenades", ServerId = "6a1c00000000000000000a1b" },
            new Def { Key = "ww_lvl5",     ServerId = "6a1c00000000000000000a1c" },
            new Def { Key = "ww_all",      ServerId = "6a1c00000000000000000a1d" },
            new Def { Key = "ww_secret",   ServerId = "6a1c00000000000000000a1e", Secret = true },
        };

        private static JObject Store { get { return PerkService.AchState; } }

        internal static bool IsDone(string key) { return (bool?)Store[key] == true; }

        internal static void Unlock(string key)
        {
            if (IsDone(key)) return;
            var def = All.FirstOrDefault(a => a.Key == key);
            if (def == null) { Plugin.Log.LogWarning("Logro desconocido: " + key); return; }
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
            if (PerkService.IsMax("speedcola")) Unlock("a_lvl5");
            if (Perks.SpeedCola.Augs.All(a => PerkService.IsDone(a.Id))) Unlock("a_all");
            if (PerkService.IsMax("staminup")) Unlock("st_lvl5");
            if (Perks.StaminUp.Augs.All(a => PerkService.IsDone(a.Id))) Unlock("st_cardio");
            if (PerkService.IsMax("juggernog")) Unlock("jg_lvl5");
            if (Perks.Juggernog.Augs.All(a => PerkService.IsDone(a.Id))) Unlock("jg_all");
            if (PerkService.IsMax("phdflopper")) Unlock("pf_lvl5");
            if (Perks.PhdFlopper.Augs.All(a => PerkService.IsDone(a.Id))) Unlock("pf_all");
            if (PerkService.IsMax("widowswine")) Unlock("ww_lvl5");
            if (Perks.WidowsWine.Augs.All(a => PerkService.IsDone(a.Id))) Unlock("ww_all");
            AchTrack.CheckState();                                  // logros de la 2.0
        }

        // Una sinergia se acaba de desbloquear (logros de sinergias)
        internal static void OnSynergyUnlocked() { AchTrack.OnSynergyUnlocked(); }

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
