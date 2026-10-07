using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Widow's Wine: tus granadas dejan telarañas.
    //   Base: en cuanto una granada que TU lanzas esta cerca de un enemigo (a menos de 10 m, vuele, ruede o ya este en el suelo
    //   esperando a explotar) el enemigo queda atrapado: su velocidad de movimiento baja un 60% y se renueva mientras la
    //   granada siga cerca, hasta 6 s despues. Tambien se aplica al explotar. Se usa el limitador de velocidad del propio juego
    //   (el mismo que los pantanos), que tambien afecta a los bots. Las granadas de humo no cuentan.
    internal static class WidowsWine
    {
        internal const string Id = "widowswine";
        internal const string ItemTpl = "6a1c00000000000000000c05";

        internal const float Radius = 10f;      // base: radio de la telarana alrededor de la granada (m)
        internal const float SpeedLeft = 0.40f; // velocidad que les queda (x0.40 = -60%)
        internal const float Seconds = 6f;      // duracion (se renueva mientras la granada siga cerca)
        private const float CheckInterval = 0.2f;

        // granadas lanzadas por el jugador que aun existen
        private static readonly List<Grenade> _mine = new List<Grenade>();
        private static readonly HashSet<int> _announced = new HashSet<int>();
        private static float _nextCheck;

        internal static string Notice() { return L.T("notice.widowswine.sub"); }

        internal static void OnRaidStart() { _mine.Clear(); _announced.Clear(); _nextCheck = 0f; }

        internal static void Register(Grenade g) { if (g != null) _mine.Add(g); }

        // Cada 0.2 s (solo con la perk activa): las granadas vivas telarañan a los enemigos que tengan cerca.
        internal static void Tick()
        {
            if (_mine.Count == 0) return;
            float now = Time.time;
            if (now < _nextCheck) return;
            _nextCheck = now + CheckInterval;

            if (!Perks.IsActive(Id)) { _mine.Clear(); return; }
            for (int i = _mine.Count - 1; i >= 0; i--)
            {
                var g = _mine[i];
                if (g == null) { _mine.RemoveAt(i); continue; }              // ya exploto o se destruyo
                int n = Web(g.transform.position);
                if (n > 0 && _announced.Add(g.GetInstanceID()))
                    Hud.Notify(L.T("perk.widowswine").ToUpperInvariant(), L.F("notice.web.sub", n), Id);
            }
        }

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

    // Una granada recien lanzada: Grenade.Init(...) (la base la llaman tambien los tipos derivados). Se guardan las del jugador.
    [HarmonyPatch(typeof(Grenade), "Init")]
    internal static class WidowsWineGrenadeInitPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Grenade __instance, string profileId)
        {
            try
            {
                if (!Perks.IsActive(WidowsWine.Id) || __instance is SmokeGrenade) return;
                var me = Plugin.Me;
                if (me == null || me.ProfileId != profileId) return;
                WidowsWine.Register(__instance);
                Dbg.Log("EFECTO", "Widow's Wine: granada lanzada, vigilando enemigos cercanos");
            }
            catch (Exception e) { Plugin.Log.LogError("Widow's Wine (lanzamiento): " + e); }
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
        private static void Postfix(Grenade grenade, Vector3 grenadePosition, string playerProfileIDWhoThrew)
        {
            try
            {
                if (!Perks.IsActive(WidowsWine.Id) || grenade is SmokeGrenade) return;
                var me = Plugin.Me;
                if (me == null || me.ProfileId != playerProfileIDWhoThrew) return;
                int n = WidowsWine.Web(grenadePosition);
                Dbg.Log("EFECTO", "Widow's Wine: explosion en " + grenadePosition.ToString("F1") + ", " + n + " enemigo(s) atrapado(s)");
            }
            catch (Exception e) { Plugin.Log.LogError("Widow's Wine (explosion): " + e); }
        }
    }
}
