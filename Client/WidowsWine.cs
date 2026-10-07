using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Widow's Wine: tus granadas dejan telarañas.
    //   Base: cuando una granada que TU lanzas explota, todos los enemigos a menos de 10 m quedan atrapados: su velocidad de movimiento
    //   baja un 60% durante 6 s. Se usa el limitador de velocidad del propio juego (el mismo que los pantanos), que tambien afecta a los bots.
    internal static class WidowsWine
    {
        internal const string Id = "widowswine";
        internal const string ItemTpl = "6a1c00000000000000000c05";

        internal const float Radius = 10f;      // base: radio de la telarana (m)
        internal const float SpeedLeft = 0.40f; // velocidad que les queda (x0.40 = -60%)
        internal const float Seconds = 6f;      // duracion

        internal static string Notice() { return L.T("notice.widowswine.sub"); }

        // Atrapa a todos los enemigos vivos cerca de 'center'. Devuelve cuantos.
        internal static int Web(Vector3 center)
        {
            var world = Singleton<GameWorld>.Instance;
            var me = Plugin.Me;
            if (world == null || me == null) return 0;
            int n = 0;
            float r2 = Radius * Radius;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p == me || p.HealthController == null || !p.HealthController.IsAlive) continue;
                if ((p.Position - center).sqrMagnitude > r2) continue;
                var mc = p.MovementContext;
                if (mc == null) continue;
                mc.ChangeSpeedLimit(SpeedLeft * mc.MaxSpeed, Player.ESpeedLimit.Swamp, Seconds);
                n++;
            }
            return n;
        }
    }

    // Granada que explota: Grenade.Explosion(Grenade, IExplosiveItem, Vector3 posicion, string perfilQueLaLanzo, ...) (estatico)
    [HarmonyPatch]
    internal static class WidowsWineGrenadePatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return typeof(Grenade).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == "Explosion" && m.GetParameters().Length >= 4);
        }

        [HarmonyPostfix]
        private static void Postfix(Vector3 grenadePosition, string playerProfileIDWhoThrew)
        {
            try
            {
                if (!Perks.IsActive(WidowsWine.Id)) return;
                var me = Plugin.Me;
                if (me == null || me.ProfileId != playerProfileIDWhoThrew) return;
                int n = WidowsWine.Web(grenadePosition);
                Dbg.Log("EFECTO", "Widow's Wine: telarana en " + grenadePosition.ToString("F1") + ", " + n + " enemigo(s) atrapado(s)");
                if (n > 0) Hud.Notify(L.T("perk.widowswine").ToUpperInvariant(), L.F("notice.web.sub", n), WidowsWine.Id);
            }
            catch (Exception e) { Plugin.Log.LogError("Widow's Wine: " + e); }
        }
    }
}
