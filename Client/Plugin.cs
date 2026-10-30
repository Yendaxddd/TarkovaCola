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
    [BepInPlugin("com.tarkovacola.server", "Tarkova-Cola", "3.0.0")]
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
        internal static ConfigEntry<bool> CfgInjectors;
        internal static ConfigEntry<bool> CfgBotPerks;
        internal static ConfigEntry<bool> CfgJingle;
        internal static ConfigEntry<float> CfgJingleVolume;
        internal static ConfigEntry<bool> CfgPhdVfx;
        internal static ConfigEntry<bool> CfgWebVfx;
        internal static ConfigEntry<bool> CfgClassicBlock;
        internal static ConfigEntry<KeyCode> CfgPosKey, CfgRerollKey, CfgClipKey;
        internal static ConfigEntry<float> CfgClipSeconds, CfgClipFps, CfgClipSlow;
        internal static ConfigEntry<bool> CfgClipHands;
        internal static ConfigEntry<bool> CfgVoice;
        internal static ConfigEntry<bool> CfgGuardsAlways;
        internal static ConfigEntry<bool> CfgLeftKeeps;
        internal static ConfigEntry<bool> CfgSalesUnlocked;
        internal static ConfigEntry<bool> CfgShowIcons;
        internal static ConfigEntry<bool> CfgMeleeStyles, CfgMeleeCenter, CfgMeleeDebug;
        internal static ConfigEntry<float> CfgMeleeLift, CfgMeleeSide;
        internal static ConfigEntry<bool> CfgMachineEditor, CfgMachinesEnabled, CfgMachinesNeedPower;
        internal static ConfigEntry<KeyCode> CfgMachineNext, CfgMachinePrev, CfgMachineRotL, CfgMachineRotR, CfgMachineSnap, CfgMachinePlace, CfgMachineDelete, CfgMachinePhoto;
        internal static ConfigEntry<KeyCode> CfgBoxModKey;
        internal static ConfigEntry<bool> CfgCodDrink;
        internal static ConfigEntry<float> CfgTeddChance;
        internal static ConfigEntry<bool> CfgJr;
        internal static ConfigEntry<bool> CfgArenaZombies;
        internal static ConfigEntry<int> CfgArenaWaves;
        internal static ConfigEntry<float> CfgArenaProgress;
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
            CfgInjectors = Cfg.Bind("Perks", "Perks como inyectores", false, "Perks as injectors",
                "Las perks son inyectores (animacion de inyectarse) en vez de latas. Se aplica al reiniciar el servidor y el juego",
                "Perks are injectors (injection animation) instead of cans. Applies after restarting the server and the game");
            CfgInjectors.SettingChanged += (s, e) => InjectorMode.Sync(true);
            CfgBotPerks = Cfg.Bind("Perks", "Efectos de perks en enemigos y companeros", true, "Perk effects on enemies and teammates",
                "Los enemigos que llevan perks encima tienen su efecto (Juggernog aguanta el doble, Speed Cola dispara mas rapido, Deadshot te ve a traves de las paredes...). A los companeros de PIT Fireteam les equipas hasta 4 en las casillas de su inventario",
                "Enemies carrying perks get their effect (Juggernog takes twice the damage, Speed Cola fires faster, Deadshot sees you through walls...). PIT Fireteam teammates get up to 4 you equip in the slots of their inventory");
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
            CfgMeleeStyles = Cfg.Bind("Perks", "Estilos de cuerpo a cuerpo", true, "Melee styles",
                "Combos con armas cuerpo a cuerpo: ligeras (horizontal, vertical, estocada), medias (horizontal, reves, vertical) y pesadas (un golpe con peso)",
                "Melee combos: light (horizontal, vertical, stab), medium (horizontal, backhand, vertical) and heavy (one weighty swing)");
            CfgMeleeCenter = Cfg.Bind("Perks", "Cuchillos mas centrados", true, "Center light melee",
                "Con armas ligeras, el arma se ve un poco mas al centro de la pantalla", "With light melee weapons, the weapon sits a bit closer to the screen center");
            CfgRerollKey = Cfg.Bind("Perks", "Tecla de re-roll (Vulture Aid)", KeyCode.J, "Re-roll key (Vulture Aid)",
                "Mirando un contenedor de cerca, vuelve a tirar su botin (mejora Re-roll de Vulture Aid)", "Looking at a container up close, rolls its loot again (Vulture Aid's Re-roll augment)");
            CfgClipKey = Cfg.Bind("Herramientas", "Grabar clip: tecla", KeyCode.F9, "Record clip: key",
                "Graba unos segundos de pantalla y los junta en una hoja de fotogramas (carpeta clips del mod), para revisar animaciones",
                "Records a few seconds of screen into one frame sheet (the mod's clips folder), to review animations");
            CfgClipSeconds = Cfg.Bind("Herramientas", "Grabar clip: segundos", 2f, "Record clip: seconds", "Duracion de la grabacion (tiempo real)", "Recording length (real time)", new AcceptableValueRange<float>(0.5f, 6f));
            CfgClipFps = Cfg.Bind("Herramientas", "Grabar clip: fotogramas por segundo", 8f, "Record clip: frames per second", "Fotogramas por segundo de tiempo real", "Frames per real-time second", new AcceptableValueRange<float>(2f, 20f));
            CfgClipSlow = Cfg.Bind("Herramientas", "Grabar clip: camara lenta", 0.3f, "Record clip: slow motion",
                "Velocidad del juego mientras se graba (1 = normal). Con 0.3 un golpe de 0.4 s ocupa unos 8 fotogramas", "Game speed while recording (1 = normal). At 0.3 a 0.4 s swing spans about 8 frames", new AcceptableValueRange<float>(0.1f, 1f));
            CfgClipHands = Cfg.Bind("Herramientas", "Grabar clip: solo la zona de las manos", false, "Record clip: hands area only",
                "Graba solo el cuarto inferior derecho de la pantalla, con mas detalle (para ver las manos y el arma)", "Records only the lower-right part of the screen, in more detail (to see hands and weapon)");
            CfgMeleeLift = Cfg.Bind("Perks", "Altura de las manos (cuerpo a cuerpo)", 0.10f, "Melee hands height",
                "Sube las manos y el arma cuerpo a cuerpo en pantalla (metros)", "Raises the hands and melee weapon on screen (meters)", new AcceptableValueRange<float>(0f, 0.3f));
            CfgMeleeSide = Cfg.Bind("Perks", "Manos hacia el centro (cuerpo a cuerpo)", 0.08f, "Melee hands toward center",
                "Mueve las manos y el arma cuerpo a cuerpo hacia el centro de la pantalla (metros)", "Moves the hands and melee weapon toward the screen center (meters)", new AcceptableValueRange<float>(0f, 0.25f));
            CfgMeleeDebug = Cfg.Bind("Herramientas", "Depurar cuerpo a cuerpo", false, "Debug melee",
                "Muestra en pantalla el estado de cada golpe (clase, paso del combo, giro, velocidad, fases)", "Shows each swing's state on screen (class, combo step, roll, speed, phases)");
            CfgMachinesEnabled = Cfg.Bind("Perks", "Maquinas de perks en los mapas", true, "Perk machines on maps",
                "Aparecen las maquinas de perks colocadas en los mapas (comprar: 30.000 rublos; si ya la tienes, eterna por 15.000)",
                "Perk machines placed on maps appear (buy: 30,000 roubles; if you already have it, Eternal for 15,000)");
            CfgMachinesNeedPower = Cfg.Bind("Perks", "Las maquinas necesitan encender la luz", true, "Machines need power",
                "Las maquinas no funcionan hasta que enciendes el interruptor de la luz del mapa (si el mapa no tiene, funcionan desde el principio)",
                "Machines do not work until you turn on the map's power switch (if the map has none, they work from the start)");
            // experimental: se mezcla con la animacion de EFT (manos negras, lata flotando). Clave nueva para que quede apagada en todos.
            CfgCodDrink = Cfg.Bind("Debug", "Animacion de beber de Black Ops (experimental)", false, "Black Ops drinking animation (experimental)",
                "EXPERIMENTAL: al beber una perk, la mano izquierda hace la animacion de Black Ops 7 encima de la de EFT. Aun se ve mal",
                "EXPERIMENTAL: when drinking a perk, the left hand plays the Black Ops 7 animation on top of EFT's. Still looks wrong");
            CfgBoxModKey = Cfg.Bind("Perks", "Caja misteriosa: tecla para accesorio", KeyCode.Y, "Mystery Box: attachment key",
                "Mantener F en la caja da un arma; mantener esta tecla, un accesorio", "Hold F at the box for a weapon; hold this key for an attachment");
            CfgTeddChance = Cfg.Bind("Perks", "Probabilidad de TEDD (%)", 15f, "TEDD chance (%)",
                "Probabilidad de que TEDD aparezca en la raid (en uno de sus puntos del mapa) con una prueba", "Chance that TEDD shows up in the raid (at one of his map spots) with a trial",
                new AcceptableValueRange<float>(0f, 100f));
            CfgMachineEditor = Cfg.Bind("Herramientas", "Editor de maquinas", false, "Machine editor",
                "En raid: una maquina de perks sigue tu mira para colocarla en el mapa (se guarda en el servidor al momento)",
                "In raid: a perk machine follows your aim so you can place it on the map (saved on the server right away)");
            CfgMachineNext = Cfg.Bind("Herramientas", "Editor: siguiente perk", KeyCode.PageUp, "Editor: next perk", "", "");
            CfgMachinePrev = Cfg.Bind("Herramientas", "Editor: perk anterior", KeyCode.PageDown, "Editor: previous perk", "", "");
            CfgMachineRotL = Cfg.Bind("Herramientas", "Editor: girar a la izquierda", KeyCode.LeftBracket, "Editor: rotate left", "", "");
            CfgMachineRotR = Cfg.Bind("Herramientas", "Editor: girar a la derecha", KeyCode.RightBracket, "Editor: rotate right", "", "");
            CfgMachineSnap = Cfg.Bind("Herramientas", "Editor: redondear giro a 45", KeyCode.Backslash, "Editor: snap rotation to 45", "", "");
            CfgMachinePlace = Cfg.Bind("Herramientas", "Editor: colocar", KeyCode.Insert, "Editor: place", "", "");
            CfgMachineDelete = Cfg.Bind("Herramientas", "Editor: borrar la mas cercana", KeyCode.Delete, "Editor: delete nearest", "", "");
            CfgMachinePhoto = Cfg.Bind("Herramientas", "Editor: foto", KeyCode.F11, "Editor: screenshot",
                "Guarda una captura en BepInEx/plugins/TarkovaCola/photos", "Saves a screenshot to BepInEx/plugins/TarkovaCola/photos");
            CfgGuardsAlways = Cfg.Bind("Debug", "Guardias siempre", false, "Always spawn guards",
                "Hace aparecer a los Embotelladores de las misiones de este mapa aunque la mision no este en marcha (para probar)",
                "Spawns the quest Bottlers on this map even if the quest is not started (for testing)");
            CfgSalesUnlocked = Cfg.Bind("Debug", "Perks a la venta sin misiones", false, "Perks for sale without quests",
                "Therapist vende todas las perks aunque no hayas hecho sus misiones", "Therapist sells every perk even without their quests");
            CfgSalesUnlocked.SettingChanged += (s, e) => Story.SyncSales();
            CfgJr = Cfg.Bind("???", "??? - POR NADA DEL MUNDO ACTIVES ESTO, O VAYAS AL FLEA MARKET Y BUSQUES le creatura", false,
                "??? - DO NOT TURN THIS ON FOR ANY REASON, OR GO TO THE FLEA MARKET AND SEARCH \"le creatura\"",
                "...", "...", null,
                "??? - POR NADA DEL MUNDO ACTIVES ESTO, O VAYAS AL FLEA MARKET Y BUSQUES \"le creatura\"");
            CfgJr.SettingChanged += (s, e) => Creatura.SyncSale();
            CfgArenaZombies = Cfg.Bind("Arena", "Soporte del modo Zombies", true, "Zombies mode support",
                "Con Arena Mode: en su modo Zombies cae una perk aleatoria cada pocas oleadas y las perks ganan progreso (lento). Las partidas de Arena nunca cuentan para tu perfil",
                "With Arena Mode: its Zombies mode drops a random perk every few waves and perks gain (slow) progress. Arena matches never count for your profile");
            CfgArenaWaves = Cfg.Bind("Arena", "Perk aleatoria cada N oleadas", 5, "Random perk every N waves",
                "Cada cuantas oleadas superadas aparece la botella de perk aleatoria", "How many cleared waves between random perk bottles", new AcceptableValueRange<int>(1, 50));
            CfgArenaProgress = Cfg.Bind("Arena", "Ritmo de progreso", 0.25f, "Progress rate",
                "Multiplicador del XP y del avance de los desafios de las perks en Zombies (1 = normal)", "Multiplier for perk XP and challenge progress in Zombies (1 = normal)", new AcceptableValueRange<float>(0f, 1f));
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
            Log.LogInfo("Tarkova-Cola cliente cargado (v3.0.0) - parches: " + ok + " ok, " + fail + " con error");
        }

        private void Update()
        {
            if (!InRaidWorld && Input.GetKeyDown(CfgOpenKey.Value)) { Dbg.Log("MENU", "tecla " + CfgOpenKey.Value + " pulsada"); ResearchScreen.Toggle(); }
            if (InRaid && Input.GetKeyDown(CfgPosKey.Value)) CopyPosition();
            Tracker.Tick();
            PerkSense.Tick();
            try { BotPerks.Tick(); } catch (Exception e) { Log.LogError("Perks en bots: " + e); }
            try { PitPerks.Tick(); } catch (Exception e) { Log.LogError("Perks de PIT: " + e); }
            try { Machines.Tick(); } catch (Exception e) { Log.LogError("Maquinas (editor): " + e); }
            ArenaCompat.Tick();
            Cfg.Tick();
            try { ClipCapture.Tick(); } catch (Exception e) { Log.LogError("Clips: " + e); }
            if (InRaidWorld) _lastInWorld = Time.realtimeSinceStartup;
            if (ServerSafe) { try { Achievements.FlushNative(); Achievements.FlushClaims(); PerkService.FlushRefunds(); } catch (Exception e) { Log.LogWarning("logros/correo (menu): " + e.Message); } }
            Hud.TickRaidPanel();
            PerkService.TickSave();      // el progreso se envia al servidor unos segundos despues del ultimo cambio
        }

        private void OnGUI()
        {
            Hud.Draw();
            try { MeleeStyle.DrawDebug(); } catch (Exception e) { Dbg.Throttled("meleedbg", 10f, "ERROR", "cuerpo a cuerpo (debug): " + e.Message); }
            try { VultureAid.DrawGui(); } catch (Exception e) { Dbg.Throttled("vulturegui", 10f, "ERROR", "Vulture (marcas): " + e.Message); }
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

        // Fuera de la raid desde hace unos segundos (el servidor ya proceso el final de la raid): se pueden pedir cambios del perfil
        private static float _lastInWorld = -100f;
        internal static bool ServerSafe { get { return !InRaidWorld && Time.realtimeSinceStartup - _lastInWorld > 8f; } }

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
                if (!ArenaCompat.ArenaNow) try { GuardSpawner.OnPlayerReady(me); }
                catch (Exception e) { Plugin.Log.LogError("Guardias (inicio): " + e); }
                try { AchTrack.OnPlayerReady(); }
                catch (Exception e) { Plugin.Log.LogError("Logros (inicio): " + e); }
                try { Eternal.OnPlayerReady(me); }
                catch (Exception e) { Plugin.Log.LogError("Eternal (inicio): " + e); }
                try { ClassicMode.OnPlayerReady(me); }          // modo Classic: aplica las perks persistentes y sortea el desafio de la raid
                catch (Exception e) { Plugin.Log.LogError("Classic (inicio): " + e); }
                Bandolier.Reset(); VultureAid.Reset(); MeleeMacchiato.Reset(); NewPerkUtil.Reset();
                try { Machines.OnPlayerReady(me); }
                catch (Exception e) { Plugin.Log.LogError("Maquinas (inicio): " + e); }
                try { Achievements.CheckState(); Achievements.ClaimPending(); }     // logros (fuera del arranque de la raid)
                catch (Exception e) { Plugin.Log.LogError("Logros (inicio): " + e); }
                return;
            }

            HandModel.Tick(hc);          // modelo de la perk en la mano (solo actua mientras se bebe)
            try { StaminUp.Tick(me); }   // efectos de Stamin-Up (sale enseguida si no esta activa)
            catch (Exception e) { Dbg.Throttled("staminfail", 10f, "ERROR", "Stamin-Up: " + e); }
            try { ClassicMode.Tick(me); }
            catch (Exception e) { Dbg.Throttled("classicfail", 10f, "ERROR", "Classic (frame): " + e); }
            Synergy.Tick(me);
            if (!ArenaCompat.InArena) try { Apex.Tick(me); }       // APEX: nucleos que coges y efectos continuos
            catch (Exception e) { Dbg.Throttled("apexfail", 10f, "ERROR", "APEX (frame): " + e); }
            try { Powerup.Tick(me); }    // perk aleatoria: caducidad y recogida
            catch (Exception e) { Dbg.Throttled("powerupfail", 10f, "ERROR", "powerup (frame): " + e); }
            try { Bandolier.Tick(me); VultureAid.Tick(me); MeleeMacchiato.Tick(me, Time.deltaTime); }
            catch (Exception e) { Dbg.Throttled("newperksfail", 10f, "ERROR", "Bandolier/Vulture (frame): " + e); }
            try { PerkFx.Tick(me); }     // efectos por frame de Juggernog, PHD Flopper, Widow's Wine y las mejoras comunes (sale enseguida sin perks activas)
            catch (Exception e) { Dbg.Throttled("perkfxfail", 10f, "ERROR", "perks (frame): " + e); }

            // ---- cambio de arma ----
            if (!ReferenceEquals(hc, _hands))
            {
                _hands = hc;
                OnHandsChanged(me, hc);
                if (hc is Player.KnifeController) MeleeMacchiato.OnKnifeDrawn();
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

            Rev.OnHandsChanged(hc);                     // Impulsivo (Melee Macchiato)
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

            Perk.LastKillOrDrink = Time.time;                       // Bajon: matar devuelve el bonus de recarga

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

        // (Dry Mouth y Sugar Crash ya no tocan la sed: en la 3.0 pasaron a ser A medias y Bajon, en Revision.cs)
        private static void ThirstTick(Player me, float dt) { }
    }

    // Cada raid empieza sin perks y con el progreso recargado del servidor.
    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    internal static class RaidStartPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            // Cada paso por separado: si uno falla se registra y los demas siguen. Nada puede escaparse de aqui, porque una
            // excepcion cancelaria el inicio de la raid del juego (y el de otros mods).
            Safe("perks", Perks.ClearActive);
            Safe("hud", Hud.ClearQueue);
            Safe("panel", RaidPanel.ResetSelection);
            Safe("staminup", StaminUp.OnRaidStart);
            Safe("efectos", PerkFx.OnRaidStart);
            Safe("eternal", Eternal.OnRaidStart);
            Safe("powerup", Powerup.OnRaidStart);
            Safe("tracker", Tracker.Reset);
            XpPatch.Awarded = false;
            Safe("dbg", Dbg.RaidStarted);
            Safe("perk", Perk.OnRaidStart);
            Safe("progreso", () => PerkService.EnsureLoaded(force: true));
            Safe("arena", ArenaCompat.OnRaidStart);              // Arena Mode: restaura el perfil si una partida anterior no termino bien
            Safe("sinergias", Synergy.OnRaidStart);              // sinergias equipadas
            Safe("APEX", Apex.OnRaidStart);                      // APEX equipado y nucleos de la raid
            Safe("guardias", QuestGuards.OnRaidStart);           // puntos de los guardias de las misiones
            Safe("logros", AchTrack.OnRaidStart);
            Safe("classic", ClassicMode.OnRaidStart);            // modo Classic: reactiva las perks que persisten de raids anteriores
            Safe("sanear", () => PerkService.Sanitize());        // sin su desventaja, una mejora no puede estar equipada
            Safe("equipo", Perk.Refresh);
            Safe("maquinas", Machines.OnRaidStart);
            Safe("eternas", Eternal.Restore);                    // perks eternas guardadas
            // (los logros se revisan en Tracker, cuando el jugador ya existe: no en mitad del arranque de la raid)
            Safe("log", () => Dbg.Log("RAID", "empieza la raid (Speed Cola: nivel " + PerkService.Level("speedcola") + ", " + PerkService.Xp("speedcola") + " XP | Stamin-Up: nivel " + PerkService.Level("staminup") + ", " + PerkService.Xp("staminup") + " XP)"));
        }

        private static void Safe(string step, Action action)
        {
            try { action(); }
            catch (Exception e) { Plugin.Log.LogError("inicio de raid (" + step + "): " + e); }
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
                raidXp = ArenaCompat.ScaleXp(raidXp);         // Arena: Zombies da XP reducido, los demas modos nada
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
