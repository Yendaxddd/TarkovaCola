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
    [BepInPlugin("com.tarkovacola.server", "Tarkova-Cola", "2.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string SpeedColaId = "6a1c00000000000000000c01";

        internal static BepInEx.Logging.ManualLogSource Log;
        internal static bool SpeedColaActive { get { return Perks.IsActive("speedcola"); } }      // se bebio Speed Cola en esta raid

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
        internal static ConfigEntry<bool> CfgPhdVfx;
        internal static ConfigEntry<bool> CfgWebVfx;
        internal static ConfigEntry<bool> CfgClassicBlock;
        internal static ConfigEntry<KeyCode> CfgPosKey;
        internal static ConfigEntry<bool> CfgVoice;
        internal static ConfigEntry<bool> CfgGuardsAlways;
        internal static ConfigEntry<bool> CfgLeftKeeps;
        internal static ConfigEntry<bool> CfgSalesUnlocked;
        internal static ConfigEntry<bool> CfgShowIcons;
        internal static ConfigEntry<bool> CfgSense;
        internal static ConfigEntry<float> CfgSenseRadius;
        internal static ConfigEntry<float> CfgVoiceVolume;
        internal static ConfigEntry<bool> CfgPowerup;
        internal static ConfigEntry<float> CfgPowerupChance;
        internal static Plugin Instance;

        private void Awake()
        {
            Log = Logger;
            L.Init(Config);
            Cfg.Init(Config);
            CfgDebug = Cfg.Bind("Debug", "Registro detallado", false, "Detailed log",
                "Escribe en el log cada evento (bebida, bajas, desafios, efectos activados...). Solo escribe cuando pasa algo, no afecta a los FPS",
                "Writes every event to the log (drinks, kills, challenges, effects...). Only writes when something happens, no FPS cost");
            Dbg.Bind(CfgDebug);
            CfgShowIcons = Cfg.Bind("HUD", "Mostrar iconos de perks", true, "Show perk icons",
                "Muestra la fila de iconos de tus perks activas abajo en el centro de la pantalla durante la raid",
                "Shows the row of your active perk icons at the bottom center of the screen during the raid");
            CfgShowNotice = Cfg.Bind("HUD", "Mostrar aviso", true, "Show notices",
                "Muestra el aviso al consumir una perk o completar un desafio", "Shows a notice when you drink a perk or complete a challenge");
            CfgNoticeSeconds = Cfg.Bind("HUD", "Duracion del aviso (s)", 4f, "Notice duration (s)",
                "Segundos que se queda el aviso en pantalla", "Seconds the notice stays on screen");
            CfgIconSize = Cfg.Bind("HUD", "Tamano del icono (v2)", 72f, "Icon size",
                "Tamano de los iconos de perks (px a 1080p)", "Size of the perk icons (px at 1080p)");
            CfgIconX = Cfg.Bind("HUD", "Icono X (desde el centro)", 0f, "Icons X (from center)",
                "Desplazamiento horizontal de la fila de iconos respecto al centro de la pantalla (px a 1080p)", "Horizontal offset of the icon row from the screen center (px at 1080p)");
            CfgIconY = Cfg.Bind("HUD", "Icono Y (desde abajo, v2)", 24f, "Icons Y (from bottom)",
                "Distancia al borde inferior (px a 1080p)", "Distance from the bottom edge (px at 1080p)");
            CfgRaidPanel = Cfg.Bind("HUD", "Panel de progreso en raid", true, "In-raid progress panel",
                "Muestra nivel, XP y desafios pendientes al abrir el menu (ESC) en raid", "Shows level, XP and pending challenges when you open the menu (ESC) in raid");
            CfgSense = Cfg.Bind("HUD", "Aviso de enemigos con perks", true, "Perk carrier alert",
                "Avisa cuando te acercas a un enemigo que lleva perks encima (una vez por enemigo)", "Alerts you when you get close to an enemy carrying perks (once per enemy)");
            CfgSenseRadius = Cfg.Bind("HUD", "Distancia del aviso de enemigos (m)", 40f, "Perk carrier alert range (m)",
                "Distancia a la que se avisa", "Distance at which the alert triggers", new AcceptableValueRange<float>(5f, 150f));
            CfgPhdVfx = Cfg.Bind("HUD", "Efecto visual de PHD Flopper", true, "PHD Flopper visual effect",
                "Anillo de energia y chispas cuando PHD Flopper te salva de una caida o una explosion", "Energy ring and sparks when PHD Flopper saves you from a fall or blast");
            CfgWebVfx = Cfg.Bind("HUD", "Efecto visual de la telarana", true, "Web visual effect",
                "Telarana en el suelo donde tu granada atrapa enemigos", "Web on the ground where your grenade catches enemies");
            CfgIgnoreArmPenalty = Cfg.Bind("Perks", "Ignorar penalizacion de brazos", true, "Ignore arm penalty",
                "Con un brazo danado el juego anula el buff de recarga; con esto Speed Cola se mantiene", "With a damaged arm the game cancels the reload buff; with this Speed Cola still works");
            CfgClassicBlock = Cfg.Bind("Perks", "Classic: impedir beber perks de mas", true, "Classic: block extra perks",
                "En modo Classic no deja beber una perk que ya llevas ni una de mas. Si da problemas, desactivalo: se podra beber y la botella sobrante se devuelve por correo",
                "In Classic mode, blocks drinking a perk you already have or one too many. If it causes problems, turn it off: you can drink it and the extra bottle is returned by mail");
            CfgLeftKeeps = Cfg.Bind("Perks", "Abandonar la raid no cuenta como muerte", true, "Leaving is not dying",
                "Si sales de la raid o te desconectas (p. ej. con mods que deshacen la raid al desconectarse), no pierdes tus perks de Clasico ni las Eternas",
                "If you leave or disconnect (e.g. with mods that undo the raid on disconnect), you keep your Classic and Eternal perks");
            CfgHandScale = Cfg.Bind("Perks", "Tamano de la lata en la mano", 1.35f, "Bottle size in hand",
                "Multiplicador del tamano del modelo de la perk al beberla", "Size multiplier of the perk model while drinking");
            CfgOpenKey = Cfg.Bind("Menu", "Tecla para abrir Research", KeyCode.F9, "Research shortcut",
                "Atajo alternativo al boton Research del menu principal", "Alternative shortcut to the Research button");
            CfgJingle = Cfg.Bind("Audio", "Cancion al beber", true, "Jingle when drinking",
                "Reproduce la cancion de la perk al beberla", "Plays the perk jingle when drinking");
            CfgJingleVolume = Cfg.Bind("Audio", "Volumen de la cancion", 0.6f, "Jingle volume", "Volumen 0-1", "Volume 0-1", new AcceptableValueRange<float>(0f, 1f));
            CfgVoice = Cfg.Bind("Audio", "Voz del locutor", true, "Announcer voice",
                "Voz de 'Random Perk' y 'Eternal Perk'", "Announcer voice for 'Random Perk' and 'Eternal Perk'");
            CfgVoiceVolume = Cfg.Bind("Audio", "Volumen de la voz", 0.9f, "Announcer volume", "Volumen 0-1", "Volume 0-1", new AcceptableValueRange<float>(0f, 1f));
            CfgPowerup = Cfg.Bind("Powerup", "Perk aleatoria al matar", true, "Random perk on kill",
                "Al matar puede caer una botella flotante que da una perk al azar (como mucho una por raid)", "Kills can drop a floating bottle that gives a random perk (at most one per raid)");
            CfgPowerupChance = Cfg.Bind("Powerup", "Probabilidad por baja (%)", 4f, "Chance per kill (%)",
                "Probabilidad de que caiga al matar a un enemigo", "Chance that it drops when you kill an enemy", new AcceptableValueRange<float>(0f, 100f));
            CfgSwapWindow = Cfg.Bind("Desafios", "Ventana tras cambiar de arma (s)", 6f, "Weapon swap window (s)",
                "Quick Hands: tiempo tras cambiar de arma en que cuentan las bajas", "Quick Hands: time after swapping weapons during which kills count");
            CfgPosKey = Cfg.Bind("Herramientas", "Tecla para copiar la posicion", KeyCode.F10, "Copy position key",
                "En raid, copia al portapapeles el mapa y tus coordenadas (para colocar misiones)", "In raid, copies the map and your coordinates to the clipboard (to place quests)");
            CfgGuardsAlways = Cfg.Bind("Debug", "Guardias siempre", false, "Always spawn guards",
                "Hace aparecer a los Embotelladores de las misiones de este mapa aunque la mision no este en marcha (para probar)",
                "Spawns the quest Bottlers on this map even if the quest is not started (for testing)");
            CfgSalesUnlocked = Cfg.Bind("Debug", "Perks a la venta sin misiones", false, "Perks for sale without quests",
                "Therapist vende todas las perks aunque no hayas hecho sus misiones", "Therapist sells every perk even without their quests");
            CfgSalesUnlocked.SettingChanged += (s, e) => Story.SyncSales();
            Instance = this;
            Player.OnPlayerDeadStatic += PerkFx.OnKill;        // bajas del jugador (Stamin-Up y las perks nuevas)

            Hud.Init(Path.GetDirectoryName(Info.Location));

            // cada parche por separado: si uno falla, los demas siguen funcionando y el log dice cual fue
            var harmony = new Harmony("com.tarkovacola.client");
            int ok = 0, fail = 0;
            foreach (var t in typeof(Plugin).Assembly.GetTypes().Where(x => x.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0))
            {
                try { harmony.CreateClassProcessor(t).Patch(); ok++; Dbg.Log("PARCHE", "ok: " + t.Name); }
                catch (Exception e) { fail++; Log.LogError("parche FALLO " + t.Name + ": " + e.Message); }
            }
            Log.LogInfo("Tarkova-Cola cliente cargado (v2.1.0) - parches: " + ok + " ok, " + fail + " con error");
        }

        private void Update()
        {
            if (!InRaidWorld && Input.GetKeyDown(CfgOpenKey.Value)) { Dbg.Log("MENU", "tecla " + CfgOpenKey.Value + " pulsada"); ResearchScreen.Toggle(); }
            if (InRaid && Input.GetKeyDown(CfgPosKey.Value)) CopyPosition();
            Tracker.Tick();
            PerkSense.Tick();
            Cfg.Tick();
            Hud.TickRaidPanel();
            PerkService.TickSave();      // el progreso se envia al servidor unos segundos despues del ultimo cambio
        }

        private void OnGUI()
        {
            Hud.Draw();
        }

        // Herramienta de autor: copia "mapa x y z" de donde estas para poner las zonas y puntos de aparicion de las misiones
        private static void CopyPosition()
        {
            try
            {
                var me = Me;
                string map = Singleton<GameWorld>.Instance.LocationId;
                var p = me.Position;
                string text = "map=" + map + " at={ \"x\": " + p.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                              + ", \"y\": " + p.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                              + ", \"z\": " + p.z.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " }";
                GUIUtility.systemCopyBuffer = text;
                Log.LogInfo("[POSICION] " + text);
                Hud.Notify(L.T("pos.title"), text, null);
            }
            catch (Exception e) { Log.LogError("No se pudo copiar la posicion: " + e.Message); }
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
        private static int _colaCount, _drinkCount;
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
                _colaCount = 0; _drinkCount = 0;
                try { GuardSpawner.OnPlayerReady(me); }
                catch (Exception e) { Plugin.Log.LogError("Guardias (inicio): " + e); }
                try { AchTrack.OnPlayerReady(); }
                catch (Exception e) { Plugin.Log.LogError("Logros (inicio): " + e); }
                try { Eternal.OnPlayerReady(me); }
                catch (Exception e) { Plugin.Log.LogError("Eternal (inicio): " + e); }
                try { ClassicMode.OnPlayerReady(me); }          // modo Classic: aplica las perks persistentes y sortea el desafio de la raid
                catch (Exception e) { Plugin.Log.LogError("Classic (inicio): " + e); }
                return;
            }

            HandModel.Tick(hc);          // modelo de la perk en la mano (solo actua mientras se bebe)
            try { StaminUp.Tick(me); }   // efectos de Stamin-Up (sale enseguida si no esta activa)
            catch (Exception e) { Dbg.Throttled("staminfail", 10f, "ERROR", "Stamin-Up: " + e); }
            try { ClassicMode.Tick(me); }
            catch (Exception e) { Dbg.Throttled("classicfail", 10f, "ERROR", "Classic (frame): " + e); }
            Synergy.Tick(me);
            try { Powerup.Tick(me); }    // perk aleatoria: caducidad y recogida
            catch (Exception e) { Dbg.Throttled("powerupfail", 10f, "ERROR", "powerup (frame): " + e); }
            try { PerkFx.Tick(me); }     // efectos por frame de Juggernog, PHD Flopper, Widow's Wine y las mejoras comunes (sale enseguida sin perks activas)
            catch (Exception e) { Dbg.Throttled("perkfxfail", 10f, "ERROR", "perks (frame): " + e); }

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

            // Sprint Shooter (desafio): bebidas energeticas encontradas (la cantidad sube durante la raid)
            int drinks = StaminUp.CountDrinks(me);
            if (Time.time - _initTime >= 8f && drinks > _drinkCount && me.HealthController.IsAlive)
            {
                Dbg.Log("DESAFIO", "bebidas energeticas encontradas: +" + (drinks - _drinkCount));
                PerkService.AddProgress("st_shooter", drinks - _drinkCount);
            }
            _drinkCount = drinks;
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
            float extra = PerkFx.BaseHydrationRate * (mult - 1f) * dt;     // sobre el gasto base (no el ritmo actual, que sube al correr)
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
            Perks.ClearActive();
            Hud.ClearQueue();
            RaidPanel.ResetSelection();
            StaminUp.OnRaidStart();
            PerkFx.OnRaidStart();
            Eternal.OnRaidStart();
            Powerup.OnRaidStart();
            Tracker.Reset();
            XpPatch.Awarded = false;
            Dbg.RaidStarted();
            Perk.OnRaidStart();
            PerkService.EnsureLoaded(force: true);
            Synergy.OnRaidStart();                    // sinergias equipadas
            QuestGuards.OnRaidStart();                // puntos de los guardias de las misiones
            AchTrack.OnRaidStart();
            ClassicMode.OnRaidStart();                // modo Classic: reactiva las perks que persisten de raids anteriores
            PerkService.Sanitize();                   // sin su desventaja, una mejora no puede estar equipada
            Perk.Refresh();
            Eternal.Restore();                        // perks eternas guardadas
            Achievements.CheckState();
            Achievements.ClaimPending();
            Dbg.Log("RAID", "empieza la raid (Speed Cola: nivel " + PerkService.Level("speedcola") + ", " + PerkService.Xp("speedcola") + " XP | Stamin-Up: nivel " + PerkService.Level("staminup") + ", " + PerkService.Xp("staminup") + " XP)");
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
        private static void Postfix(ExitStatus __0)
        {
            var me = Plugin.Me;
            if (!Awarded) PerkFx.OnRaidEnd(__0);        // desafios que dependen de extraer con vida
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
                var active = Perks.ActivePerks.ToList();
                Plugin.Log.LogInfo("Fin de raid (" + source + "): XP de la sesion=" + raidXp + ", perks activas=" + (active.Count == 0 ? "(ninguna)" : string.Join(", ", active.Select(p => p.Id).ToArray())));
                if (raidXp > 0)
                    foreach (var perk in active)
                        PerkService.AddXp(perk.Id, (int)Math.Round(raidXp * (1.0 + PerkService.XpBonus)));
                PerkService.Flush();
            }
            catch (Exception e) { Plugin.Log.LogError("Error otorgando XP de perk: " + e); }
        }
    }

    // Detecta que el jugador bebe una perk (cualquiera de Perks.All).
    [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.DoMedEffect))]
    internal static class DrinkPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ActiveHealthController __instance, Item item)
        {
            if (__instance.Player == null || !__instance.Player.IsYourPlayer) return;
            var def = item != null ? Perks.ByItem(item.StringTemplateId) : null;
            if (def == null)                                              // otra comida o medicina: cuenta para los desafios
            {
                Juggernog.OnConsumed(item);
                ClassicChallenge.OnConsumed(item);
                Synergy.OnConsumed(item);
                return;
            }

            PerkGrant.Apply(def, __instance.Player, false);               // activa la perk (o la vuelve eterna si ya la llevabas)
        }
    }

    // Efectos propios de Speed Cola al beberla.
    internal static class SpeedColaDrink
    {
        internal static string Notice() { return L.F("notice.cola.sub", Perk.ReloadMult.ToString("0.0")); }

        // texto del efecto base a una fuerza dada (para la pantalla del modo Classic)
        internal static string Effect(float power) { return L.F("notice.cola.sub", (1f + (Perk.BaseReload - 1f) * power).ToString("0.0")); }

        internal static void OnDrink(ActiveHealthController health)
        {
            Dbg.Log("BEBIDA", "Speed Cola: recarga x" + Perk.ReloadMult.ToString("0.0") + (Perk.Has("a_rush") ? " (Sugar Rush equipada)" : ""));

            // Loud Slurp: beberla hace ruido que oyen los bots cercanos
            if (Perk.Has("d_slurp"))
            {
                var p = health.Player;
                Singleton<GlobalEventDispatcher>.Instance.PlaySound(p, p.Position, Perk.SlurpMeters, AISoundType.step);
                Dbg.Log("EFECTO", "Loud Slurp: ruido de " + Perk.SlurpMeters.ToString("0") + " m");
            }

            // recalcula la recarga del arma en mano ya con el multiplicador
            var fc = health.Player.HandsController as Player.FirearmController;
            if (fc != null) fc.SyncWithCharacterSkills();
        }
    }
}
