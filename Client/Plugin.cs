using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    [BepInPlugin("com.tarkovacola.server", "Tarkova-Cola", "1.1.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string SpeedColaId = "6a1c00000000000000000c01";

        internal static BepInEx.Logging.ManualLogSource Log;
        internal static bool SpeedColaActive;      // se bebio Speed Cola en esta raid

        internal static ConfigEntry<bool> CfgDebug;
        internal static ConfigEntry<bool> CfgShowNotice;
        internal static ConfigEntry<float> CfgNoticeSeconds;
        internal static ConfigEntry<float> CfgIconSize;
        internal static ConfigEntry<float> CfgIconX;
        internal static ConfigEntry<float> CfgIconY;
        internal static ConfigEntry<bool> CfgIgnoreArmPenalty;
        internal static ConfigEntry<KeyCode> CfgOpenKey;
        internal static ConfigEntry<bool> CfgRaidPanel;
        internal static ConfigEntry<float> CfgSwapWindow;
        internal static ConfigEntry<float> CfgHandScale;
        internal static ConfigEntry<bool> CfgJingle;
        internal static ConfigEntry<float> CfgJingleVolume;
        internal static Plugin Instance;

        private void Awake()
        {
            Log = Logger;
            L.Init(Config);
            CfgDebug = Config.Bind("Debug", "Registro detallado", false,
                "Escribe en el log cada evento (bebida, bajas, desafios, efectos activados...). Solo escribe cuando pasa algo, no afecta a los FPS");
            Dbg.Bind(CfgDebug);
            CfgShowNotice = Config.Bind("HUD", "Mostrar aviso", true, "Muestra el aviso al consumir una perk o completar un desafío");
            CfgNoticeSeconds = Config.Bind("HUD", "Duracion del aviso (s)", 4f, "Segundos que se queda el aviso en pantalla");
            CfgIconSize = Config.Bind("HUD", "Tamano del icono (v2)", 72f, "Tamano del icono permanente (px a 1080p)");
            CfgIconX = Config.Bind("HUD", "Icono X (borde izquierdo)", 0f, "Distancia al borde izquierdo (px a 1080p); 0 = pegado al borde");
            CfgIconY = Config.Bind("HUD", "Icono Y (desde abajo)", 140f, "Distancia al borde inferior (px a 1080p)");
            CfgRaidPanel = Config.Bind("HUD", "Panel de progreso en raid", true, "Muestra nivel, XP y desafíos pendientes al abrir el menú (ESC) en raid");
            CfgIgnoreArmPenalty = Config.Bind("Perks", "Ignorar penalizacion de brazos", true,
                "Con un brazo danado el juego anula el buff de recarga; con esto Speed Cola se mantiene");
            CfgOpenKey = Config.Bind("Menu", "Tecla para abrir Research", KeyCode.F9, "Atajo alternativo al botón Research del menú principal / Alternative shortcut to the Research button");
            Instance = this;
            CfgJingle = Config.Bind("Audio", "Cancion al beber", true, "Reproduce la cancion de Speed Cola al beberla / Play the jingle when drinking");
            CfgJingleVolume = Config.Bind("Audio", "Volumen de la cancion", 0.6f, new ConfigDescription("Volumen 0-1 / Volume 0-1", new AcceptableValueRange<float>(0f, 1f)));
            CfgHandScale = Config.Bind("Perks", "Tamano de la lata en la mano", 1.35f, "Multiplicador del tamaño del modelo de la Speed Cola al beberla / Hand model size multiplier");
            CfgSwapWindow = Config.Bind("Desafios", "Ventana tras cambiar de arma (s)", 6f, "Quick Hands: tiempo tras cambiar de arma en que cuentan las bajas");

            Hud.Init(Path.GetDirectoryName(Info.Location));

            // cada parche por separado: si uno falla, los demas siguen funcionando y el log dice cual fue
            var harmony = new Harmony("com.tarkovacola.client");
            int ok = 0, fail = 0;
            foreach (var t in typeof(Plugin).Assembly.GetTypes().Where(x => x.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0))
            {
                try { harmony.CreateClassProcessor(t).Patch(); ok++; Dbg.Log("PARCHE", "ok: " + t.Name); }
                catch (Exception e) { fail++; Log.LogError("parche FALLO " + t.Name + ": " + e.Message); }
            }
            Log.LogInfo("Tarkova-Cola cliente cargado (v1.1.1) - parches: " + ok + " ok, " + fail + " con error");
        }

        private void Update()
        {
            if (!InRaidWorld && Input.GetKeyDown(CfgOpenKey.Value)) { Dbg.Log("MENU", "tecla " + CfgOpenKey.Value + " pulsada"); ResearchScreen.Toggle(); }
            Tracker.Tick();
            Hud.TickRaidPanel();
        }

        private void OnGUI()
        {
            Hud.Draw();
        }

        // El jugador de la RAID. En el Hideout tambien existe un mundo de juego con jugador (HideoutPlayer), pero no es una raid:
        // se trata como "sin raid" para que ni el panel de ESC ni los desafios se activen alli y Research funcione.
        internal static Player Me
        {
            get
            {
                if (!Singleton<GameWorld>.Instantiated) return null;
                var p = Singleton<GameWorld>.Instance.MainPlayer;
                return p is HideoutPlayer ? null : p;
            }
        }

        // Hay una raid cargada (aunque estes muerto o en el menu de pausa).
        internal static bool InRaidWorld { get { return Me != null; } }

        // En raid y con el jugador vivo.
        internal static bool InRaid
        {
            get
            {
                var me = Me;
                return me != null && me.HealthController != null && me.HealthController.IsAlive;
            }
        }
    }

    // Detecta eventos de la raid (por sondeo, sin engancharse a mas metodos del juego). Cuesta muy poco por frame:
    // lo caro (efectos de salud, sed) se hace a 4 Hz y 1 Hz.
    internal static class Tracker
    {
        private static bool _init;
        private static int _victims;
        private static object _hands;
        private static Weapon _prevWeapon;
        private static float _swapAt = -999f;
        private static bool _leftBroken, _rightBroken;
        private static float _nextSlow, _nextThirst;
        private static int _crashSteps;
        private static readonly List<float> _colaKills = new List<float>();
        private static bool _deathDone;
        private static int _colaCount;
        private static float _initTime, _nextInv;
        private static bool _inRaidSeen;
        private static Profile _raidProfile;
        private static int _raidXp;

        internal static void Reset()
        {
            _init = false;
            _swapAt = -999f;
            _colaKills.Clear();
            _deathDone = false;
            _inRaidSeen = false;
            _raidProfile = null;
            _raidXp = 0;
        }

        internal static void Tick()
        {
            var me = Plugin.Me;
            if (me == null || me.Profile == null)
            {
                _init = false; Perk.JitterOn = false;
                // la raid termino sin que se llamara al cierre de estadisticas (p. ej. con Fika): se concede el XP ahora
                if (_inRaidSeen) { _inRaidSeen = false; XpPatch.Award("la raid termino", _raidProfile, _raidXp); }
                return;
            }
            var stats = me.Profile.EftStats;
            _inRaidSeen = true;
            _raidProfile = me.Profile;
            _raidXp = stats.TotalSessionExperience;
            var hc = me.HandsController;

            if (!_init)
            {
                _init = true;
                _victims = stats.Victims.Count;
                _hands = hc;
                _prevWeapon = (hc as Player.FirearmController)?.Item;
                _leftBroken = _rightBroken = false;
                _nextSlow = _nextThirst = Time.time;
                _crashSteps = 0;
                _initTime = _nextInv = Time.time;
                _colaCount = 0;
                return;
            }

            HandModel.Tick(hc);          // modelo de la Speed Cola en la mano (solo actua mientras se bebe)

            // ---- cambio de arma ----
            if (!ReferenceEquals(hc, _hands))
            {
                _hands = hc;
                OnHandsChanged(me, hc);
            }

            // ---- bajas nuevas ----
            var victims = stats.Victims;
            while (_victims < victims.Count) OnKill(me, victims[_victims++]);

            // ---- logros: morir con la perk activa / encontrar una Speed Cola en la raid ----
            if (Plugin.SpeedColaActive && !_deathDone && me.HealthController != null && !me.HealthController.IsAlive)
            {
                _deathDone = true;
                Achievements.Unlock("a_die");
            }
            if (Time.time >= _nextInv) { _nextInv = Time.time + 1f; InventoryTick(me); }

            if (!Plugin.SpeedColaActive) return;

            float now = Time.time;

            // ---- 4 Hz: brazos y balanceo ----
            if (now >= _nextSlow)
            {
                _nextSlow = now + 0.25f;
                SlowTick(me);
            }

            // ---- 1 Hz: sed (Dry Mouth / Sugar Crash) ----
            if (now >= _nextThirst)
            {
                float dt = now - _nextThirst + 1f;
                _nextThirst = now + 1f;
                ThirstTick(me, dt);
            }
        }

        private static int CountCola(Player me)
        {
            int n = 0;
            foreach (var it in me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment))
                if (it.TemplateId == Plugin.SpeedColaId) n += it.StackObjectsCount;
            return n;
        }

        // Si la cantidad de Speed Cola que llevas SUBE durante la raid, la has encontrado (las que traias de casa no cuentan).
        // Los primeros segundos solo se fija la cantidad inicial, mientras el equipo termina de cargarse.
        private static void InventoryTick(Player me)
        {
            int now = CountCola(me);
            if (Time.time - _initTime >= 8f && now > _colaCount && me.HealthController.IsAlive)
            {
                Dbg.Log("LOGRO", "Speed Cola encontrada en la raid (" + _colaCount + " -> " + now + ")");
                Achievements.Unlock("a_found");
            }
            _colaCount = now;
        }

        private static void OnHandsChanged(Player me, object hc)
        {
            var newWeapon = (hc as Player.FirearmController)?.Item;
            if (hc is Player.FirearmController)
            {
                _swapAt = Time.time;
                Dbg.Log("ARMA", "cambio de arma: " + (newWeapon != null ? newWeapon.ShortName : "?"));
            }

            // Bolt Runner: al guardar/cambiar el arma, si estaba encasquillada se desencasquilla sola
            var prev = _prevWeapon;
            if (prev != null && !ReferenceEquals(prev, newWeapon) && Perk.Has("a_bolt") && prev.MalfState != null && prev.MalfState.State != Weapon.EMalfunctionState.None)
            {
                Dbg.Log("EFECTO", "Bolt Runner: " + prev.ShortName + " estaba encasquillada (" + prev.MalfState.State + "), se desencasquilla sola");
                prev.MalfState.Repair();
                prev.MalfState.AmmoToFire = null;
                prev.MalfState.AmmoWillBeLoadedToChamber = null;
                prev.MalfState.MalfunctionedAmmo = null;
            }
            _prevWeapon = newWeapon;
        }

        private static void OnKill(Player me, VictimStats v)
        {
            string role = v.Role.ToString();
            bool pmc = v.Side == EPlayerSide.Usec || v.Side == EPlayerSide.Bear
                       || role.Equals("pmcBEAR", StringComparison.OrdinalIgnoreCase) || role.Equals("pmcUSEC", StringComparison.OrdinalIgnoreCase);
            bool boss = role.StartsWith("boss", StringComparison.OrdinalIgnoreCase) || role.Equals("sectantPriest", StringComparison.OrdinalIgnoreCase);
            float sinceSwap = Time.time - _swapAt;
            Dbg.Log("BAJA", v.Name + " (" + role + ", " + v.Side + ") pmc=" + pmc + " boss=" + boss + " desdeCambioArma=" + sinceSwap.ToString("0.0") + "s cola=" + Plugin.SpeedColaActive);

            Perk.LastKillOrDrink = Time.time;                       // Sugar Crash: matar reinicia la cuenta
            if (_crashSteps > 0) Dbg.Log("EFECTO", "Sugar Crash: sed vuelve a x1.00 (baja)");
            _crashSteps = 0;

            if (Plugin.SpeedColaActive && sinceSwap <= Plugin.CfgSwapWindow.Value) PerkService.AddProgress("a_quick");
            if (pmc) PerkService.AddProgress("a_rush");
            if (Plugin.SpeedColaActive && boss) PerkService.AddProgress("s_slot");

            if (Plugin.SpeedColaActive)
            {
                Achievements.Unlock("a_first");
                _colaKills.Add(Time.time);
                _colaKills.RemoveAll(t => Time.time - t > 60f);
                if (_colaKills.Count >= 3) Achievements.Unlock("a_hand");
            }
        }

        private static void SlowTick(Player me)
        {
            var hp = me.HealthController;
            if (hp == null) return;

            // Steady Fingers (desafio): brazos rotos con Speed Cola activa
            bool l = hp.IsBodyPartBroken(EBodyPart.LeftArm) || hp.IsBodyPartDestroyed(EBodyPart.LeftArm);
            bool r = hp.IsBodyPartBroken(EBodyPart.RightArm) || hp.IsBodyPartDestroyed(EBodyPart.RightArm);
            if (l && !_leftBroken) { Dbg.Log("DESAFIO", "brazo izquierdo roto"); PerkService.AddProgress("a_steady"); }
            if (r && !_rightBroken) { Dbg.Log("DESAFIO", "brazo derecho roto"); PerkService.AddProgress("a_steady"); }
            _leftBroken = l; _rightBroken = r;

            // Jittery Hands: un brazo con cualquier efecto no saludable (fractura, destruido, sangrado leve/fuerte)
            bool jitter = false;
            if (Perk.Has("d_jitter"))
                jitter = Unhealthy(hp, EBodyPart.LeftArm) || Unhealthy(hp, EBodyPart.RightArm);
            if (jitter != Perk.JitterOn) Dbg.Log("EFECTO", "Jittery Hands " + (jitter ? "ACTIVO (balanceo +25%)" : "desactivado"));
            Perk.JitterOn = jitter;
            Perk.MyBreath = me.ProceduralWeaponAnimation != null ? me.ProceduralWeaponAnimation.Breath : null;
        }

        private static bool Unhealthy(IHealthController hp, EBodyPart part)
        {
            if (hp.IsBodyPartBroken(part) || hp.IsBodyPartDestroyed(part)) return true;
            foreach (var e in hp.GetAllActiveEffects(part))
            {
                string n = e.Type != null ? e.Type.Name : "";
                if (n.IndexOf("Bleeding", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Fracture", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        // Dry Mouth (+20%) y Sugar Crash (+0.25 cada 2 min sin matar, hasta x2): extra de sed sobre lo que ya drena el juego.
        private static void ThirstTick(Player me, float dt)
        {
            bool dry = Perk.Has("d_dry"), crash = Perk.Has("d_crash");
            if (!dry && !crash) { Perk.CrashMult = 1f; return; }

            float mult = 1f;
            if (dry) mult *= Perk.DryMouth;
            if (crash)
            {
                int steps = Mathf.FloorToInt((Time.time - Perk.LastKillOrDrink) / Perk.CrashInterval);
                Perk.CrashMult = Mathf.Min(Perk.CrashMax, 1f + Perk.CrashStep * steps);
                if (steps != _crashSteps && Perk.CrashMult < Perk.CrashMax + 0.001f)
                {
                    _crashSteps = steps;
                    Dbg.Log("EFECTO", "Sugar Crash: " + steps + " tramo(s) sin matar -> sed x" + Perk.CrashMult.ToString("0.00"));
                }
                mult *= Perk.CrashMult;
            }

            float rate = me.HealthController.HydrationRate;       // negativo mientras la sed baja
            if (rate >= 0f || mult <= 1f) return;
            float extra = -rate * (mult - 1f) * dt;
            me.ActiveHealthController.ChangeHydration(-extra);
            Dbg.Throttled("thirst", 30f, "EFECTO", "sed extra: x" + mult.ToString("0.00") + " (-" + extra.ToString("0.000") + " hidratacion/s)");
        }
    }

    // Cada raid empieza sin perks y con el progreso recargado del servidor.
    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    internal static class RaidStartPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            Plugin.SpeedColaActive = false;
            Tracker.Reset();
            XpPatch.Awarded = false;
            Dbg.RaidStarted();
            Perk.OnRaidStart();
            PerkService.EnsureLoaded(force: true);
            Perk.Refresh();
            Achievements.CheckState();
            Achievements.ClaimPending();
            Dbg.Log("RAID", "empieza la raid (nivel de Speed Cola " + PerkService.Level + ", " + PerkService.Xp + " XP)");
        }
    }

    // Al terminar la raid, el XP de la sesion (el mismo que ves en los resultados) pasa a la perk con +25%.
    [HarmonyPatch]
    internal static class XpPatch
    {
        internal static bool Awarded;

        // EndStatisticsSession(ExitStatus, float) puede estar sobreescrito: se parchean todas las versiones.
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var args = new[] { typeof(ExitStatus), typeof(float) };
            return typeof(BaseStatisticsManager).Assembly.GetTypes()
                .Where(t => typeof(BaseStatisticsManager).IsAssignableFrom(t))
                .Select(t => t.GetMethod("EndStatisticsSession", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, args, null))
                .Where(m => m != null && !m.IsAbstract);
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            var me = Plugin.Me;
            Award("EndStatisticsSession", me != null ? me.Profile : null, 0);
        }

        // Concede el XP de la raid a la perk (una sola vez por raid). Si el jugador ya no es accesible usa el perfil / XP recordados.
        internal static void Award(string source, Profile profile, int knownXp)
        {
            try
            {
                if (Awarded) return;
                Awarded = true;
                int raidXp = knownXp;
                if (profile != null && profile.EftStats != null) raidXp = Math.Max(raidXp, profile.EftStats.TotalSessionExperience);
                Plugin.Log.LogInfo("Fin de raid (" + source + "): XP de la sesion=" + raidXp + ", Speed Cola activa=" + Plugin.SpeedColaActive);
                if (Plugin.SpeedColaActive && raidXp > 0)
                    PerkService.AddXp((int)Math.Round(raidXp * (1.0 + PerkService.XpBonus)));
                PerkService.Save();
            }
            catch (Exception e) { Plugin.Log.LogError("Error otorgando XP de perk: " + e); }
        }
    }

    // Detecta que el jugador bebe una Speed Cola.
    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.DoMedEffect))]
    internal static class DrinkPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ActiveHealthController __instance, Item item)
        {
            if (item == null || item.TemplateId != Plugin.SpeedColaId) return;
            if (__instance.Player == null || !__instance.Player.IsYourPlayer) return;

            Plugin.SpeedColaActive = true;
            Jingle.Play();
            Perk.Refresh();
            Perk.LastKillOrDrink = Time.time;
            Hud.Notify(L.T("notice.cola.title"), L.F("notice.cola.sub", Perk.ReloadMult.ToString("0.0")));
            Dbg.Log("BEBIDA", "Speed Cola activada: recarga x" + Perk.ReloadMult.ToString("0.0") + (Perk.Has("a_rush") ? " (Sugar Rush equipada)" : ""));

            // Loud Slurp: beberla hace ruido que oyen los bots cercanos
            if (Perk.Has("d_slurp"))
            {
                var p = __instance.Player;
                Singleton<GlobalEventDispatcher>.Instance.PlaySound(p, p.Position, Perk.SlurpMeters, AISoundType.step);
                Dbg.Log("EFECTO", "Loud Slurp: ruido de " + Perk.SlurpMeters.ToString("0") + " m");
            }

            // recalcula la recarga del arma en mano ya con el multiplicador
            var fc = __instance.Player.HandsController as Player.FirearmController;
            if (fc != null) fc.SyncWithCharacterSkills();
        }
    }
}
