using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using EFT;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Idiomas: todo texto visible sale de aqui. Cada entrada es { español, ingles }.
    //  El idioma sigue al del juego (Auto) salvo que se fuerce en la configuracion (F12).
    // ============================================================================================
    internal static class L
    {
        private static ConfigEntry<string> _cfg;
        private const string Auto = "Auto", Es = "Español", En = "English";

        internal static void Init(ConfigFile config)
        {
            _cfg = config.Bind("Idioma", "Idioma / Language", Auto,
                new ConfigDescription("Auto sigue el idioma del juego. / Auto follows the game's language.",
                    new AcceptableValueList<string>(Auto, Es, En)));
        }

        // true = español
        internal static bool Spanish
        {
            get
            {
                string v = _cfg != null ? _cfg.Value : Auto;
                if (v == Es) return true;
                if (v == En) return false;
                try
                {
                    var c = LocalizationManager.Instance != null ? LocalizationManager.Instance.Culture : null;
                    if (!string.IsNullOrEmpty(c)) return c.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                }
                catch { }
                return false;
            }
        }

        internal static string T(string key)
        {
            string[] v;
            if (!S.TryGetValue(key, out v)) return key;
            return Spanish ? v[0] : v[1];
        }

        internal static string F(string key, params object[] args) { return string.Format(T(key), args); }

        private static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>
        {
            // ---- maquinas ----
            ["machine.buy"]          = new[] { "Mantén {0}: {1} — {2} ₽", "Hold {0}: {1} — {2} ₽" },
            ["notice.botweb"]        = new[] { "Un enemigo con Widow's Wine te ha atrapado", "An enemy with Widow's Wine webbed you" },
            ["pit.title"]            = new[] { "Perks del compañero", "Teammate perks" },
            ["pit.header"]           = new[] { "Perks de {0}", "{0}'s perks" },
            ["pit.hint"]             = new[] { "Clic en + para equipar una perk que lleves · clic en una perk para quitarla", "Click + to equip a perk you carry · click a perk to take it back" },
            ["pit.pick"]             = new[] { "Elige una perk de tu equipo", "Pick a perk from your gear" },
            ["pit.none"]             = new[] { "No llevas ninguna perk que pueda usar", "You carry no perk they can use" },
            ["pit.lost"]             = new[] { "{0} ha muerto y ha perdido sus perks", "{0} died and lost their perks" },
            ["pit.nobottle"]         = new[] { "Ya no tienes la botella de {0}: no se ha equipado", "You no longer have the {0} bottle: it was not equipped" },
            ["pit.nospace"]          = new[] { "No cabe la botella de {0} en tu equipo", "No room in your gear for the {0} bottle" },
            ["machine.wunder"]     = new[] { "Mantén {0}: Der Wunderfizz, una perk al azar — {1} ₽", "Hold {0}: Der Wunderfizz, a random perk — {1} ₽" },
            ["machine.wunder.none"]  = new[] { "Der Wunderfizz: no queda ninguna perk que te pueda dar", "Der Wunderfizz: no perk left to give you" },
            ["box.name"]           = new[] { "Caja misteriosa", "Mystery Box" },
            ["box.use"]            = new[] { "Mantén {0}: arma al azar  ·  Mantén {1}: accesorio al azar — {2} ₽", "Hold {0}: random weapon  ·  Hold {1}: random attachment — {2} ₽" },
            ["box.take"]           = new[] { "{0} — míralo y cógelo con la tecla de recoger antes de que vuelva a la caja", "{0} — look at it and pick it up before it sinks back into the box" },
            ["box.teddy"]          = new[] { "¡Adiós, adiós! La caja se va a otro sitio (te devuelve {0} ₽)", "Bye bye! The box is moving somewhere else ({0} ₽ refunded)" },
            ["box.moved"]          = new[] { "La caja misteriosa ha aparecido en otro sitio del mapa", "The Mystery Box has appeared somewhere else on the map" },
            ["tedd.here"]          = new[] { "TEDD está en algún lugar del mapa. Encuéntralo para una prueba", "TEDD is somewhere on the map. Find him for a trial" },
            ["tedd.title"]         = new[] { "PRUEBA DE TEDD", "TEDD TRIAL" },
            ["tedd.start"]         = new[] { "Mantén {0}: aceptar la prueba de TEDD", "Hold {0}: accept TEDD's trial" },
            ["tedd.t.fetch"]       = new[] { "Consigue {0} y tráeselo a TEDD", "Find {0} and bring it to TEDD" },
            ["tedd.t.kill"]        = new[] { "Abate al objetivo marcado", "Take down the marked target" },
            ["tedd.t.hold"]        = new[] { "Aguanta junto a TEDD: quedan {0} s", "Hold out next to TEDD: {0} s left" },
            ["tedd.t.money"]       = new[] { "Consigue {0} ₽ y págaselos a TEDD", "Get {0} ₽ and pay TEDD" },
            ["tedd.deliver"]       = new[] { "Mantén {0}: entregar {1}", "Hold {0}: hand over {1}" },
            ["tedd.pay"]           = new[] { "Mantén {0}: pagar {1} ₽", "Hold {0}: pay {1} ₽" },
            ["tedd.busy"]          = new[] { "TEDD: {0}", "TEDD: {0}" },
            ["tedd.outside"]       = new[] { "¡Vuelve junto a TEDD!", "Get back to TEDD!" },
            ["tedd.done"]          = new[] { "¡Prueba superada!", "Trial complete!" },
            ["tedd.fail"]          = new[] { "Prueba fallida: se acabó el tiempo", "Trial failed: time's up" },
            ["tedd.over"]          = new[] { "TEDD ya no tiene más pruebas para ti", "TEDD has no more trials for you" },
            ["tedd.reward"]        = new[] { "Recompensa: {0}", "Reward: {0}" },
            ["tedd.target"]        = new[] { "OBJETIVO", "TARGET" },
            ["tedd.item"]          = new[] { "OBJETO", "ITEM" },
            ["tedd.money"]         = new[] { "DINERO", "MONEY" },
            // ---- APEX ----
            ["apex.kicker"]        = new[] { "UN EFECTO DEFINITIVO", "ONE ULTIMATE EFFECT" },
            ["apex.rules.title"]   = new[] { "CÓMO FUNCIONA", "HOW IT WORKS" },
            ["apex.rules"]         = new[] {
                "Cada perk tiene un APEX: un único efecto muy potente.\n1. Encuentra un Núcleo APEX en raid (cajas fuertes, alijos, jefes...). Al cogerlo se absorbe; si extraes con vida, lo conservas.\n2. Gasta un núcleo aquí para empezar el desafío de un APEX. Solo avanza con su perk activa.\n3. Supera el desafío y equípalo.\nSolo puedes llevar UN APEX, en Modern y en Classic, y funciona mientras su perk esté activa.",
                "Every perk has an APEX: a single, very powerful effect.\n1. Find an APEX Core in raid (safes, caches, bosses...). It is absorbed when you pick it up; extract alive to keep it.\n2. Spend a core here to start an APEX challenge. It only progresses with its perk active.\n3. Complete the challenge and equip it.\nYou can only run ONE APEX, in Modern and Classic, and it works while its perk is active." },
            ["apex.equipped"]      = new[] { "APEX", "APEX" },
            ["apex.cores"]         = new[] { "Núcleos APEX: {0}", "APEX Cores: {0}" },
            ["apex.tag.eq"]        = new[] { "EQUIPADO", "EQUIPPED" },
            ["apex.tag.un"]        = new[] { "DESBLOQUEADO", "UNLOCKED" },
            ["apex.tag.run"]       = new[] { "DESAFÍO EN CURSO", "CHALLENGE IN PROGRESS" },
            ["apex.tag.lock"]      = new[] { "BLOQUEADO", "LOCKED" },
            ["apex.effect"]        = new[] { "EFECTO", "EFFECT" },
            ["apex.need"]          = new[] { "El desafío solo avanza y el efecto solo funciona con {0} activa.", "The challenge only progresses and the effect only works with {0} active." },
            ["apex.use"]           = new[] { "Usar un núcleo APEX (tienes {0})", "Use an APEX Core (you have {0})" },
            ["apex.running"]       = new[] { "Supera el desafío para desbloquearlo", "Complete the challenge to unlock it" },
            ["apex.nocore"]        = new[] { "Necesitas un Núcleo APEX (se encuentra en raid)", "You need an APEX Core (found in raid)" },
            ["apex.unlocked"]      = new[] { "APEX DESBLOQUEADO", "APEX UNLOCKED" },
            ["apex.core.title"]    = new[] { "NÚCLEO APEX", "APEX CORE" },
            ["apex.core.sub"]      = new[] { "Absorbido. Extrae con vida para conservarlo", "Absorbed. Extract alive to keep it" },
            ["apex_juggernog.n"]   = new[] { "Piel de acero", "Steel Skin" },
            ["apex_juggernog.d"]   = new[] { "Inmune a los ataques cuerpo a cuerpo y a cualquier sangrado.", "Immune to melee attacks and to any bleeding." },
            ["apex_juggernog.c"]   = new[] { "Recibe 1.500 de daño con Juggernog activa.", "Take 1,500 damage with Juggernog active." },
            ["apex_speedcola.n"]   = new[] { "Mecanismo perfecto", "Flawless Action" },
            ["apex_speedcola.d"]   = new[] { "Tu arma no puede encasquillarse.", "Your weapon cannot malfunction." },
            ["apex_speedcola.c"]   = new[] { "Consigue 40 bajas con Speed Cola activa.", "Get 40 kills with Speed Cola active." },
            ["apex_staminup.n"]    = new[] { "Sin límites", "Limitless" },
            ["apex_staminup.d"]    = new[] { "Estamina infinita (piernas y brazos) y esprintas un 10% más rápido.", "Infinite stamina (legs and arms) and you sprint 10% faster." },
            ["apex_staminup.c"]    = new[] { "Esprinta 15 km con Stamin-Up activa.", "Sprint 15 km with Stamin-Up active." },
            ["apex_phdflopper.n"]  = new[] { "Detonador", "Detonator" },
            ["apex_phdflopper.d"]  = new[] { "Cada baja hace explotar el cuerpo de la víctima (a ti no te afecta).", "Every kill makes the victim's body explode (it doesn't affect you)." },
            ["apex_phdflopper.c"]  = new[] { "Absorbe 30 explosiones o caídas con PHD Flopper.", "Absorb 30 explosions or falls with PHD Flopper." },
            ["apex_widowswine.n"]  = new[] { "Trampa viva", "Live Trap" },
            ["apex_widowswine.d"]  = new[] { "Tus explosivos detonan al instante en cuanto están cerca de un enemigo.", "Your explosives detonate instantly as soon as they get near an enemy." },
            ["apex_widowswine.c"]  = new[] { "Atrapa a 40 enemigos en telarañas.", "Trap 40 enemies in webs." },
            ["apex_deadshot.n"]    = new[] { "Perforador", "Piercer" },
            ["apex_deadshot.d"]    = new[] { "Tus balas tienen un nivel más de penetración, sea cual sea la munición.", "Your bullets get one extra penetration level, whatever the ammo." },
            ["apex_deadshot.c"]    = new[] { "Consigue 30 bajas con disparo a la cabeza con Deadshot activa.", "Get 30 headshot kills with Deadshot active." },
            ["apex_bandolier.n"]   = new[] { "Cinta eterna", "Endless Belt" },
            ["apex_bandolier.d"]   = new[] { "Cada baja te rellena un cargador completo.", "Every kill refills one full magazine." },
            ["apex_bandolier.c"]   = new[] { "Consigue 60 bajas con Bandolier Bandit activa.", "Get 60 kills with Bandolier Bandit active." },
            ["apex_vultureaid.n"]  = new[] { "Ojo del buitre", "Vulture's Eye" },
            ["apex_vultureaid.d"]  = new[] { "La mitad de los enemigos llevan encima un objeto valioso o de misión. Nadie te avisa de cuáles.", "Half of the enemies carry a valuable or quest item. Nobody tells you which." },
            ["apex_vultureaid.c"]  = new[] { "Registra 25 cuerpos de PMC con Vulture Aid activa.", "Search 25 PMC bodies with Vulture Aid active." },
            ["apex_meleemacchiato.n"] = new[] { "Torbellino", "Whirlwind" },
            ["apex_meleemacchiato.d"] = new[] { "No recibes daño mientras dura un ataque cuerpo a cuerpo.", "You take no damage while a melee attack is playing." },
            ["apex_meleemacchiato.c"] = new[] { "Consigue 25 bajas cuerpo a cuerpo con Melee Macchiato activa.", "Get 25 melee kills with Melee Macchiato active." },
            ["machine.buy.eternal"]  = new[] { "Mantén {0}: {1} ETERNA — {2} ₽", "Hold {0}: ETERNAL {1} — {2} ₽" },
            ["machine.nopower"]      = new[] { "{0}: necesita energía", "{0}: needs power" },
            ["machine.poor"]         = new[] { "Te faltan {0} ₽", "You need {0} more ₽" },
            ["machine.switch"]       = new[] { "Interruptor de la luz", "Power switch" },
            ["machine.power.hold"]   = new[] { "Mantén {0}: encender la luz", "Hold {0}: turn on the power" },
            ["machine.power.on"]     = new[] { "La luz ya está encendida", "The power is already on" },
            ["machine.power.title"]  = new[] { "¡LUZ ENCENDIDA!", "POWER ON!" },
            ["machine.power.sub"]    = new[] { "Las máquinas de perks están listas", "The perk machines are ready" },

            // ---- editor de maquinas ----
            ["machine.editor"]  = new[] { "EDITOR DE MÁQUINAS", "MACHINE EDITOR" },
            ["machine.aim"]     = new[] { "apunta al suelo", "aim at the floor" },
            ["machine.keys"]    = new[] { "Perk: {0}/{1}   Girar: {2}/{3} (redondear: {4})   Colocar: {5}   Borrar cercana: {6}   Foto: {7}", "Perk: {0}/{1}   Rotate: {2}/{3} (snap: {4})   Place: {5}   Delete nearest: {6}   Photo: {7}" },
            ["machine.count"]   = new[] { "{0} máquinas en {1}", "{0} machines on {1}" },
            ["machine.placed"]  = new[] { "{0} colocada en {1}", "{0} placed on {1}" },
            ["machine.deleted"] = new[] { "{0} borrada", "{0} deleted" },
            ["machine.none"]    = new[] { "No hay ninguna máquina a menos de 10 m", "No machine within 10 m" },
            ["machine.photo"]   = new[] { "Foto guardada: {0}", "Photo saved: {0}" },

            // ---- Arena Mode ----
            ["arena.title"]     = new[] { "TARKOVA-COLA · ZOMBIES", "TARKOVA-COLA · ZOMBIES" },
            ["arena.start"]     = new[] { "Perk aleatoria cada {0} oleadas. Esta partida no cuenta para tu perfil", "Random perk every {0} waves. This match does not count for your profile" },
            ["arena.full"]      = new[] { "Ya tienes todas las perks", "You already have every perk" },

            // ---- aviso de enemigos con perks ----
            ["sense.title"]     = new[] { "PERK-A-COLA CERCA", "PERK-A-COLA NEARBY" },
            ["sense.sub"]       = new[] { "Un enemigo cercano lleva {0}", "A nearby enemy is carrying {0}" },
            ["sense.guard"]     = new[] { "Un Embotellador anda cerca", "A Bottler is close by" },

            // ---- pantalla: general ----
            ["augments"]        = new[] { "MEJORAS", "AUGMENTS" },
            ["title.progress"]  = new[] { "PROGRESO", "PROGRESS" },
            ["title.equip"]     = new[] { "EQUIPAR", "EQUIP" },
            ["nav.progress"]    = new[] { "PROGRESO", "PROGRESS" },
            ["nav.equip"]       = new[] { "EQUIPAR", "EQUIP" },
            ["nav.syn"]         = new[] { "SINERGIA", "SYNERGY" },
            ["nav.summary"]     = new[] { "RESUMEN", "SUMMARY" },
            ["level"]           = new[] { "NIVEL", "LEVEL" },
            ["btn.research"]    = new[] { "INVESTIGAR", "RESEARCH" },
            ["perk.speedcola"]  = new[] { "SPEED COLA", "SPEED COLA" },
            ["perk.speedcola.desc"] = new[]
            {
                "Recargas 1.5x más rápidas durante el resto de la raid. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Reloads 1.5x faster for the rest of the raid. Level up to unlock challenges; each challenge unlocks an augment and its drawback.",
            },

            // ---- progreso ----
            ["status.done"]     = new[] { "DESBLOQUEADA", "UNLOCKED" },
            ["status.level"]    = new[] { "NIVEL {0}", "LEVEL {0}" },
            ["status.challenge"]= new[] { "DESAFÍO {0}/{1}", "CHALLENGE {0}/{1}" },
            ["unlocks"]         = new[] { "Desbloquea: {0}", "Unlocks: {0}" },
            ["info.prompt.title"] = new[] { "Selecciona un nodo", "Select a node" },
            ["info.prompt.text"]  = new[]
            {
                "Sube de nivel para revelar desafíos. Al completar uno se desbloquea su mejora y la desventaja ligada.",
                "Level up to reveal challenges. Completing one unlocks its augment and its linked drawback.",
            },
            ["kind.major"]      = new[] { "MEJORA PRINCIPAL", "MAJOR AUGMENT" },
            ["kind.minor"]      = new[] { "MEJORA SECUNDARIA", "MINOR AUGMENT" },
            ["kind.special"]    = new[] { "DESBLOQUEO ESPECIAL", "SPECIAL UNLOCK" },
            ["info.header"]     = new[] { "{0}  ·  DESAFÍO DE NIVEL {1}", "{0}  ·  LEVEL {1} CHALLENGE" },
            ["info.locked"]     = new[] { "Alcanza el nivel {0} para desbloquear este desafío.", "Reach level {0} to unlock this challenge." },
            ["info.challenge"]  = new[] { "Desafío: {0}", "Challenge: {0}" },
            ["info.drawback"]   = new[] { "Desbloquea la desventaja {0}: {1}", "Unlocks the drawback {0}: {1}" },

            // ---- equipar ----
            ["sec.am"]          = new[] { "MEJORA PRINCIPAL", "MAJOR AUGMENT" },
            ["sec.an"]          = new[] { "MEJORA SECUNDARIA", "MINOR AUGMENT" },
            ["sec.dm"]          = new[] { "DESVENTAJA PRINCIPAL", "MAJOR DRAWBACK" },
            ["sec.dn"]          = new[] { "DESVENTAJA SECUNDARIA", "MINOR DRAWBACK" },
            ["tag.equipped"]    = new[] { "EQUIPADA", "EQUIPPED" },
            ["tag.notunlocked"] = new[] { "NO DESBLOQUEADA", "NOT UNLOCKED" },
            ["tag.locked"]      = new[] { "BLOQUEADA", "LOCKED" },
            ["tile.challenge"]  = new[] { "Desafío de nivel {0}", "Level {0} challenge" },
            ["tile.unlocks"]    = new[] { "Se desbloquea con {0}", "Unlocked by {0}" },
            ["slot.major"]      = new[] { "PRINCIPAL", "MAJOR" },
            ["slot.minor"]      = new[] { "SECUNDARIA", "MINOR" },
            ["slot.level5"]     = new[] { "NIVEL 5", "LEVEL 5" },
            ["equip.augments"]  = new[] { "MEJORAS", "AUGMENTS" },
            ["equip.drawbacks"] = new[] { "DESVENTAJAS", "DRAWBACKS" },
            ["equip.rules"]     = new[]
            {
                "Mejoras: 1 principal + {0} secundaria(s).\nDesventajas: 1 principal + 1 secundaria.\nNo puedes llevar una mejora principal sin su desventaja principal, ni una secundaria sin su desventaja secundaria.",
                "Augments: 1 major + {0} minor.\nDrawbacks: 1 major + 1 minor.\nYou can't carry a major augment without a major drawback, nor a minor augment without a minor drawback.",
            },
            ["xp.max"]          = new[] { "NIVEL MÁXIMO", "MAX LEVEL" },
            ["xp.mult"]         =new[] { "MULTIPLICADOR DE XP", "XP MULTIPLIER" },
            ["xp.note"]         = new[] { "+25% de XP mientras la perk está activa en la raid", "+25% XP while the perk is active in raid" },
            ["err.needdrb"]     = new[] { "Equipa primero una desventaja principal.", "Equip a major drawback first." },
            ["err.needdrbn"]    = new[] { "Equipa primero una desventaja secundaria.", "Equip a minor drawback first." },
            ["toast.drbremoved"]= new[] { "Sin desventaja principal, la mejora principal también se desequipa.", "Without a major drawback, the major augment is unequipped too." },
            ["toast.drbremoved.minor"] = new[] { "Sin desventaja secundaria, las mejoras secundarias también se desequipan.", "Without a minor drawback, minor augments are unequipped too." },
            ["toast.sanitized"] = new[] { "Se desequiparon mejoras que no tenían su desventaja.", "Augments without their drawback were unequipped." },
            ["toast.locked"]    = new[] { "Aún no está desbloqueada.", "Not unlocked yet." },

            // ---- avisos en pantalla (HUD) ----
            ["notice.cola.title"]  = new[] { "SPEED COLA", "SPEED COLA" },
            ["notice.cola.sub"]    = new[] { "Velocidad de recarga x{0}", "Reload speed x{0}" },
            ["notice.level.title"] = new[] { "{0} · NIVEL {1}", "{0} · LEVEL {1}" },
            ["notice.level.sub"]   = new[] { "Nuevos desafíos disponibles", "New challenges available" },
            ["notice.chal.title"]  = new[] { "DESAFÍO COMPLETADO", "CHALLENGE COMPLETE" },
            ["notice.chal.sub"]    = new[] { "{0} desbloqueada", "{0} unlocked" },
            ["notice.ach.title"]   = new[] { "LOGRO DESBLOQUEADO", "ACHIEVEMENT UNLOCKED" },

            ["notice.over.sub"]    = new[] { "El cargador se vació", "The magazine emptied" },

            // ---- panel en raid (menu ESC) ----
            ["raid.title"]      = new[] { "SPEED COLA", "SPEED COLA" },
            ["raid.level"]      = new[] { "NIVEL {0}", "LEVEL {0}" },
            ["raid.active"]     = new[] { "ACTIVA", "ACTIVE" },
            ["raid.xpraid"]     = new[] { "+{0} XP esta raid (x1.25)", "+{0} XP this raid (x1.25)" },
            ["raid.drink"]      = new[] { "Bebe {0} en la raid para ganar XP de la perk.", "Drink {0} in raid to earn perk XP." },
            ["raid.pending"]    = new[] { "DESAFÍOS PENDIENTES ({0})", "PENDING CHALLENGES ({0})" },
            ["rp.tabs.hint"]    = new[] { "Clic en un icono, o la rueda del ratón sobre el panel, para cambiar de perk.", "Click an icon, or use the mouse wheel over the panel, to switch perk." },
            ["raid.alldone"]    = new[] { "¡Todos los desafíos completados!", "All challenges completed!" },
            ["raid.eternal"]    = new[] { "ETERNA", "ETERNAL" },
            ["raid.none"]       = new[] { "Sin desafíos activos. Sube de nivel para revelar más.", "No active challenges. Level up to reveal more." },

            // ---- mejoras: nombre (.n), efecto (.d), desafio (.c) ----
            ["a_quick.n"]  = new[] { "MANOS RÁPIDAS", "QUICK HANDS" },
            ["a_quick.d"]  = new[] { "Sacar y cambiar de arma es un 50% más rápido.", "Drawing and swapping weapons is 50% faster." },
            ["a_quick.c"]  = new[] { "Mata a 3 enemigos poco después de cambiar de arma, con Speed Cola activa.", "Kill 3 enemies shortly after swapping weapons, with Speed Cola active." },

            ["a_bolt.n"]   = new[] { "RECARGA EN CALIENTE", "HOT RELOAD" },
            ["a_bolt.d"]   = new[] { "Al recargar un cargador vacío, tu cadencia sube un 15% durante 4 s.", "Reloading an empty magazine raises your fire rate by 15% for 4 s." },
            ["a_bolt.c"]   = new[] { "Recarga 10 cargadores vacíos con Speed Cola activa.", "Reload 10 empty magazines with Speed Cola active." },

            ["a_rush.n"]   = new[] { "SUBIDÓN DE AZÚCAR", "SUGAR RUSH" },
            ["a_rush.d"]   = new[] { "El bonus de recarga de Speed Cola sube de x1.5 a x2.0.", "Speed Cola's reload bonus rises from x1.5 to x2.0." },
            ["a_rush.c"]   = new[] { "Mata a 4 PMC.", "Kill 4 PMCs." },

            ["a_steady.n"] = new[] { "PULSO FIRME", "STEADY FINGERS" },
            ["a_steady.d"] = new[] { "Tus brazos reciben un 30% menos de daño.", "Your arms take 30% less damage." },
            ["a_steady.c"] = new[] { "Ten los brazos rotos 4 veces con Speed Cola activa.", "Get your arms broken 4 times with Speed Cola active." },

            ["a_check.n"]  = new[] { "REVISIÓN RÁPIDA", "CHAMBER CHECK" },
            ["a_check.d"]  = new[] { "Descargar cargadores en el inventario es un 75% más rápido.", "Unloading magazines in your inventory is 75% faster." },
            ["a_check.c"]  = new[] { "Revisa el cargador de tu arma 20 veces en raid.", "Check your weapon's magazine 20 times in raid." },

            ["a_mags.n"]   = new[] { "MALABARISTA", "MAG JUGGLER" },
            ["a_mags.d"]   = new[] { "Los cargadores de tu arma principal se cargan un 50% más rápido en el inventario.", "Magazines for your primary weapon load 50% faster in your inventory." },
            ["a_mags.c"]   = new[] { "Carga 20 cargadores en raid con Speed Cola activa.", "Load 20 magazines in raid with Speed Cola active." },

            ["a_count.n"]  = new[] { "CONTEO DE BALAS", "ROUND COUNTER" },
            ["a_count.d"]  = new[] { "Los cargadores muestran su munición exacta, los hayas revisado o no.", "Magazines show their exact ammo count, checked or not." },
            ["a_count.c"]  = new[] { "Descarga 15 cargadores en raid con Speed Cola activa.", "Unload 15 magazines in raid with Speed Cola active." },

            ["s_slot.n"]   = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["s_slot.d"]   = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["s_slot.c"]   = new[] { "Mata a 1 boss con Speed Cola activa.", "Kill 1 boss with Speed Cola active." },

            // ---- desventajas ----
            ["d_jitter.n"] = new[] { "MANOS TEMBLOROSAS", "JITTERY HANDS" },
            ["d_jitter.d"] = new[] { "Si un brazo tiene un efecto no saludable (fractura, destruido, sangrado leve/fuerte), el balanceo del arma sube un 25%.", "If an arm has an unhealthy effect (fracture, destroyed, light/heavy bleed), weapon sway rises by 25%." },
            ["d_over.n"]   = new[] { "SOBREPRESIÓN", "OVERPRESSURE" },
            ["d_over.d"]   = new[] { "Cada 4.º cargador pierde todas sus balas al recargar (vuelven a tu inventario).", "Every 4th magazine loses all its rounds when reloaded (they return to your inventory)." },
            ["d_crash.n"]  = new[] { "BAJÓN", "SUGAR CRASH" },
            ["d_crash.d"]  = new[] { "Si pasas 2 min sin matar, pierdes el bonus de recarga de Speed Cola hasta tu siguiente baja.", "Go 2 min without a kill and you lose Speed Cola's reload bonus until your next kill." },
            ["notice.d_crash"] = new[] { "2 min sin matar: sin bonus de recarga hasta tu próxima baja", "2 min without a kill: no reload bonus until your next kill" },
            ["d_dry.n"]    = new[] { "A MEDIAS", "HALF MEASURES" },
            ["d_dry.d"]    = new[] { "Recargar con balas aún en el cargador no tiene el bonus de recarga.", "Reloading with rounds still in the magazine gets no reload bonus." },
            ["d_slurp.n"]  = new[] { "SORBO RUIDOSO", "LOUD SLURP" },
            ["d_slurp.d"]  = new[] { "Beberla hace ruido y los bots cercanos te oyen.", "Drinking it makes noise that nearby bots can hear." },
            ["d_jam.n"]    = new[] { "CARGADORES PEGAJOSOS", "STICKY MAGS" },
            ["d_jam.d"]    = new[] { "+10% de probabilidad de encasquillamiento.", "+10% jam chance." },
            ["d_sweet.n"]  = new[] { "DIENTE DULCE", "SWEET TOOTH" },
            ["d_sweet.d"]  = new[] { "Comer o beber otra cosa tarda el doble.", "Eating or drinking anything else takes twice as long." },

            // ---- Stamin-Up ----
            ["perk.staminup"]      = new[] { "Stamin-Up", "Stamin-Up" },
            ["perk.staminup.desc"] = new[] {
                "Estamina de piernas x1.5 el resto de la raid. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Leg stamina x1.5 for the rest of the raid. Level up to unlock challenges; each challenge unlocks an augment and its drawback." },
            ["notice.stamin.sub"]  = new[] { "Estamina de piernas x{0}", "Leg stamina x{0}" },
            ["notice.wind.sub"]    = new[] { "Estamina infinita e inmunidad al dolor", "Infinite stamina and pain immunity" },

            // ---- Juggernog / PHD Flopper / Widow's Wine ----
            ["perk.juggernog"]      = new[] { "Juggernog", "Juggernog" },
            ["perk.juggernog.desc"] = new[] {
                "Daño recibido -33% el resto de la raid. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Damage taken -33% for the rest of the raid. Level up to unlock challenges; each challenge unlocks an augment and its drawback." },
            ["perk.deadshot"]       = new[] { "Deadshot Daiquiri", "Deadshot Daiquiri" },
            ["perk.deadshot.desc"]  = new[] {
                "Apuntas un 15% más rápido y el balanceo del arma baja un 25% el resto de la raid.",
                "You aim 15% faster and weapon sway drops by 25% for the rest of the raid." },
            ["perk.bandolier"]       = new[] { "Bandolier Bandit", "Bandolier Bandit" },
            ["perk.bandolier.desc"]  = new[] {
                "Los cargadores del chaleco y los bolsillos se rellenan 1 bala cada 2 s con la munición suelta que llevas, mientras no disparas. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Magazines in your rig and pockets refill 1 round every 2 s from the loose ammo you carry, while you are not shooting. Level up to unlock challenges; each challenge unlocks an augment and its drawback." },
            ["notice.bandolier.sub"] = new[] { "Cargadores: +1 bala cada {0} s", "Magazines: +1 round every {0} s" },
            ["perk.vultureaid"]       = new[] { "Vulture Aid Elixir", "Vulture Aid Elixir" },
            ["perk.vultureaid.desc"]  = new[] {
                "Los contenedores y cuerpos sin registrar con objetos de valor se ven a través de las paredes a menos de 15 m. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Unsearched containers and bodies with valuables show through walls within 15 m. Level up to unlock challenges; each challenge unlocks an augment and its drawback." },
            ["notice.vultureaid.sub"] = new[] { "Botín de valor visible a {0} m", "Valuable loot visible within {0} m" },
            ["perk.meleemacchiato"]       = new[] { "Melee Macchiato", "Melee Macchiato" },
            ["perk.meleemacchiato.desc"]  = new[] {
                "Daño cuerpo a cuerpo x2 y golpes un 30% más rápidos el resto de la raid. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Melee damage x2 and melee attacks 30% faster for the rest of the raid. Level up to unlock challenges; each challenge unlocks an augment and its drawback." },
            ["notice.meleemacchiato.sub"] = new[] { "Cuerpo a cuerpo: daño x{0} · velocidad +{1}%", "Melee: damage x{0} · speed +{1}%" },
            ["notice.deadshot.sub"] = new[] { "Apuntar +{0}% · balanceo -{1}%", "Aim +{0}% · sway -{1}%" },
            ["notice.juggernog.sub"] = new[] { "Daño recibido -{0}%", "Damage taken -{0}%" },
            ["perk.phdflopper"]      = new[] { "PHD Flopper", "PHD Flopper" },
            ["perk.phdflopper.desc"] = new[] {
                "Inmune al daño por caídas y explosiones el resto de la raid. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Immune to fall and explosion damage for the rest of the raid. Level up to unlock challenges; each challenge unlocks an augment and its drawback." },
            ["notice.phdflopper.sub"] = new[] { "Inmune a caídas y explosiones", "Immune to falls and explosions" },
            ["perk.widowswine"]      = new[] { "Widow's Wine", "Widow's Wine" },
            ["perk.widowswine.desc"] = new[] {
                "Tus granadas atrapan en telarañas a los enemigos cercanos el resto de la raid. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Your grenades web nearby enemies for the rest of the raid. Level up to unlock challenges; each challenge unlocks an augment and its drawback." },
            ["notice.widowswine.sub"] = new[] { "Tus granadas atrapan en telarañas", "Your grenades web enemies" },
            ["notice.widowswine.mod"] = new[] { "Tus granadas frenan un poco a los enemigos cercanos", "Your grenades slow nearby enemies a little" },

            // ---- modo Classic ----
            ["mode.label"]        = new[] { "MODO", "MODE" },
            ["mode.modern"]       = new[] { "MODERNO", "MODERN" },
            ["mode.classic"]      = new[] { "CLÁSICO", "CLASSIC" },
            ["classic.kicker"]    = new[] { "PERKS", "PERKS" },
            ["classic.title"]     = new[] { "MODO CLÁSICO", "CLASSIC MODE" },
            ["classic.rules.title"] = new[] { "CÓMO FUNCIONA", "HOW IT WORKS" },
            ["classic.rules"]     = new[]
            {
                "- Las perks siguen activas entre raids: solo las pierdes si tu operador muere.\n- Hasta 4 perks principales y una quinta como modificador (efecto reducido).\n- Cada perk principal lleva 1 mejora menor al azar al beberla.\n- Cada raid trae un desafío opcional: cúmplelo y extrae con vida para darle una 2.ª mejora menor a una perk al azar.\n- Sin Research, sin desventajas y sin XP: tu progreso del modo Moderno queda intacto.",
                "- Perks stay active between raids: you only lose them if your operator dies.\n- Up to 4 main perks and a 5th as a modifier (reduced effect).\n- Each main perk comes with 1 random minor augment when you drink it.\n- Every raid has an optional challenge: complete it and extract alive to give a random perk a 2nd minor augment.\n- No Research, no drawbacks and no XP: your Modern progress stays untouched.",
            },
            ["classic.active"]    = new[] { "PERKS ACTIVAS", "ACTIVE PERKS" },
            ["classic.count"]     = new[] { "{0} / {1} principales", "{0} / {1} main" },
            ["classic.slot.free"] = new[] { "modificador libre", "modifier free" },
            ["classic.slot.used"] = new[] { "modificador ocupado", "modifier used" },
            ["classic.tag.main"]  = new[] { "PERK PRINCIPAL", "MAIN PERK" },
            ["classic.tag.mod"]   = new[] { "MODIFICADOR", "MODIFIER" },
            ["classic.aug"]       = new[] { "MEJORA", "AUGMENT" },
            ["classic.aug2"]      = new[] { "2.ª MEJORA (DESAFÍO)", "2ND AUGMENT (CHALLENGE)" },
            ["classic.new"]       = new[] { "NUEVA", "NEW" },
            ["classic.empty.main"] = new[] { "RANURA LIBRE", "EMPTY SLOT" },
            ["classic.empty.mod"] = new[] { "MODIFICADOR LIBRE", "FREE MODIFIER" },
            ["classic.empty.hint"] = new[] { "Bebe una perk en raid para activarla.", "Drink a perk in a raid to activate it." },
            ["classic.emptymod.hint"] = new[] { "La 5.ª perk que bebas será un modificador.", "The 5th perk you drink becomes a modifier." },
            ["classic.mod.note"]  = new[] { "Da el {0}% de su efecto y no lleva mejora.", "Gives {0}% of its effect and has no augment." },
            ["classic.chal.title"] = new[] { "DESAFÍO DE RAID", "RAID CHALLENGE" },
            ["classic.chal.text"] = new[]
            {
                "Cada raid trae un desafío opcional (míralo con ESC dentro de la raid). Si lo cumples y extraes con vida, una perk principal al azar gana una 2.ª mejora menor (máximo 2 por perk).",
                "Every raid brings an optional challenge (see it with ESC inside the raid). Complete it and extract alive, and a random main perk gains a 2nd minor augment (max 2 per perk).",
            },
            ["classic.lost.hint"] = new[] { "Si tu operador muere o no extrae, pierdes todas estas perks.", "If your operator dies or doesn't extract, you lose all these perks." },

            ["pos.title"]       = new[] { "POSICIÓN COPIADA", "POSITION COPIED" },

            // sinergias
            ["syn.kicker"]      = new[] { "PERKS", "PERKS" },
            ["syn.title"]       = new[] { "SINERGIAS", "SYNERGIES" },
            ["syn.equipped"]    = new[] { "Sinergia equipada", "Equipped synergy" },
            ["syn.bonus"]       = new[] { "BONO", "BONUS" },
            ["syn.challenge"]   = new[] { "DESAFÍO PARA DESBLOQUEARLA", "UNLOCK CHALLENGE" },
            ["syn.tag.eq"]      = new[] { "EQUIPADA", "EQUIPPED" },
            ["syn.tag.un"]      = new[] { "DESBLOQUEADA", "UNLOCKED" },
            ["syn.tag.lock"]    = new[] { "BLOQUEADA", "LOCKED" },
            ["syn.done"]        = new[] { "Desafío completado", "Challenge completed" },
            ["syn.need"]        = new[] { "Hace falta llevar {0} y {1} a la vez en la raid, para el desafío y para el bono.", "You need {0} and {1} active together in the raid, for the challenge and for the bonus." },
            ["syn.equip"]       = new[] { "EQUIPAR", "EQUIP" },
            ["syn.unequip"]     = new[] { "QUITAR", "REMOVE" },
            ["syn.locked"]      = new[] { "BLOQUEADA", "LOCKED" },
            ["syn.unlocked"]    = new[] { "SINERGIA DESBLOQUEADA", "SYNERGY UNLOCKED" },
            ["syn.blast.sub"]   = new[] { "Recarga +{0}% durante {1} s", "Reload +{0}% for {1} s" },
            ["err.synmax"]      = new[] { "Ya llevas 2 sinergias equipadas. Quita una primero.", "You already have 2 synergies equipped. Remove one first." },
            ["syn.rules.title"] = new[] { "CÓMO FUNCIONAN", "HOW THEY WORK" },
            ["syn.rules"]       = new[]
            {
                "Una sinergia es un pequeño bono que se activa al llevar dos perks concretas a la vez.\nCada una se desbloquea con su propio desafío, que solo avanza con las dos perks activas en la raid.\nSolo puedes llevar 1 sinergia equipada; equipar otra sustituye a la anterior.\nEn el modo Clásico también valen: si una de las dos perks es el modificador, el bono es más pequeño.",
                "A synergy is a small bonus that kicks in when you carry two specific perks at once.\nEach one unlocks with its own challenge, which only advances with both perks active in the raid.\nYou can only carry 1 equipped synergy; equipping another replaces the first.\nThey also work in Classic mode: if one of the two perks is the modifier, the bonus is smaller.",
            },
            ["syn_cardio.n"]    = new[] { "Cardio", "Cardio" },
            ["syn_cardio.d"]    = new[] { "Sacas y cambias de arma un 12% más rápido y correr gasta un 5% menos de estamina.", "You draw and swap weapons 12% faster and sprinting uses 5% less stamina." },
            ["syn_cardio.c"]    = new[] { "Corre 3 km con Speed Cola y Stamin-Up activas.", "Sprint 3 km with Speed Cola and Stamin-Up active." },
            ["syn_iron.n"]      = new[] { "Telaraña de acero", "Steel Web" },
            ["syn_iron.d"]      = new[] { "Recibes un 3% menos de daño.", "You take 3% less damage." },
            ["syn_iron.c"]      = new[] { "Mata a 5 enemigos atrapados en tu telaraña con Juggernog y Widow's Wine activas.", "Kill 5 enemies caught in your web with Juggernog and Widow's Wine active." },
            ["syn_blast.n"]     = new[] { "Recarga explosiva", "Blast Reload" },
            ["syn_blast.d"]     = new[] { "Cuando PHD Flopper anula una explosión o una caída, recargas un 20% más rápido durante 6 s.", "When PHD Flopper cancels an explosion or a fall, you reload 20% faster for 6 s." },
            ["syn_blast.c"]     = new[] { "Absorbe 5 explosiones o caídas con Speed Cola y PHD Flopper activas.", "Absorb 5 explosions or falls with Speed Cola and PHD Flopper active." },

            ["syn_medic.n"]     = new[] { "Médico de combate", "Combat Medic" },
            ["syn_medic.d"]     = new[] { "Curarte con objetos médicos va un 15% más rápido.", "Healing with medical items is 15% faster." },
            ["syn_medic.c"]     = new[] { "Usa 10 objetos médicos con Speed Cola y Juggernog activas.", "Use 10 medical items with Speed Cola and Juggernog active." },
            ["syn_pin.n"]       = new[] { "Anilla rápida", "Quick Pin" },
            ["syn_pin.d"]       = new[] { "Lanzas las granadas un 15% más rápido.", "You throw grenades 15% faster." },
            ["syn_pin.c"]       = new[] { "Lanza 10 granadas con Speed Cola y Widow's Wine activas.", "Throw 10 grenades with Speed Cola and Widow's Wine active." },
            ["syn_trigger.n"]   = new[] { "Gatillo fácil", "Hair Trigger" },
            ["syn_trigger.d"]   = new[] { "Apuntas un 8% más rápido.", "You aim down sights 8% faster." },
            ["syn_trigger.c"]   = new[] { "Mata a 15 enemigos apuntando, con Speed Cola y Deadshot activas.", "Kill 15 enemies while aiming, with Speed Cola and Deadshot active." },
            ["syn_tank.n"]      = new[] { "Tanque", "Tank" },
            ["syn_tank.d"]      = new[] { "Llevas un 5% menos de peso.", "You carry 5% less weight." },
            ["syn_tank.c"]      = new[] { "Corre 2 km con Stamin-Up y Juggernog activas.", "Sprint 2 km with Stamin-Up and Juggernog active." },
            ["syn_bounce.n"]    = new[] { "Rebote", "Bounce Back" },
            ["syn_bounce.d"]    = new[] { "Cuando PHD Flopper anula una explosión o una caída, recuperas un 15% de estamina.", "When PHD Flopper cancels an explosion or a fall, you recover 15% stamina." },
            ["syn_bounce.c"]    = new[] { "Absorbe 8 explosiones o caídas con Stamin-Up y PHD Flopper activas.", "Absorb 8 explosions or falls with Stamin-Up and PHD Flopper active." },
            ["syn_getaway.n"]   = new[] { "Huida", "Getaway" },
            ["syn_getaway.d"]   = new[] { "Al lanzar una granada recuperas un 10% de estamina.", "Throwing a grenade restores 10% stamina." },
            ["syn_getaway.c"]   = new[] { "Lanza 10 granadas con Stamin-Up y Widow's Wine activas.", "Throw 10 grenades with Stamin-Up and Widow's Wine active." },
            ["syn_breath.n"]    = new[] { "Aliento de corredor", "Runner's Breath" },
            ["syn_breath.d"]    = new[] { "Aguantas la respiración un 15% más.", "You hold your breath 15% longer." },
            ["syn_breath.c"]    = new[] { "Aguanta la respiración apuntando 20 veces, con Stamin-Up y Deadshot activas.", "Hold your breath while aiming 20 times, with Stamin-Up and Deadshot active." },
            ["syn_shock.n"]     = new[] { "Escudo de choque", "Shock Shield" },
            ["syn_shock.d"]     = new[] { "Cuando PHD Flopper anula una explosión o una caída, recibes un 10% menos de daño durante 5 s.", "When PHD Flopper cancels an explosion or a fall, you take 10% less damage for 5 s." },
            ["syn_shock.c"]     = new[] { "Absorbe 5 explosiones o caídas con Juggernog y PHD Flopper activas.", "Absorb 5 explosions or falls with Juggernog and PHD Flopper active." },
            ["syn_sights.n"]    = new[] { "Mira de hierro", "Iron Sights" },
            ["syn_sights.d"]    = new[] { "Mientras apuntas recibes un 5% menos de daño.", "While aiming you take 5% less damage." },
            ["syn_sights.c"]    = new[] { "Mata a 10 enemigos con armadura, con Juggernog y Deadshot activas.", "Kill 10 armored enemies with Juggernog and Deadshot active." },
            ["syn_webblast.n"]  = new[] { "Red expansiva", "Blast Web" },
            ["syn_webblast.d"]  = new[] { "Tus telarañas cubren un 20% más de radio.", "Your webs cover a 20% larger radius." },
            ["syn_webblast.c"]  = new[] { "Atrapa a 15 enemigos con PHD Flopper y Widow's Wine activas.", "Web 15 enemies with PHD Flopper and Widow's Wine active." },
            ["syn_above.n"]     = new[] { "Muerte desde arriba", "Death From Above" },
            ["syn_above.d"]     = new[] { "Cuando PHD Flopper anula una explosión o una caída, el balanceo del arma baja un 40% durante 5 s.", "When PHD Flopper cancels an explosion or a fall, weapon sway drops 40% for 5 s." },
            ["syn_above.c"]     = new[] { "Mata a 3 enemigos en los 10 s siguientes a absorber una explosión o caída, con PHD Flopper y Deadshot activas.", "Kill 3 enemies within 10 s of absorbing an explosion or fall, with PHD Flopper and Deadshot active." },
            ["syn_spider.n"]    = new[] { "Ojo de araña", "Spider Eye" },
            ["syn_spider.d"]    = new[] { "Haces un 10% más de daño a los enemigos atrapados en tu telaraña.", "You deal 10% more damage to enemies caught in your web." },
            ["syn_spider.c"]    = new[] { "Mata a 8 enemigos atrapados en tu telaraña, con Widow's Wine y Deadshot activas.", "Kill 8 enemies caught in your web, with Widow's Wine and Deadshot active." },

            // Eternal Perk y powerup de perk aleatoria
            ["eternal.title"]     = new[] { "ETERNAL PERK", "ETERNAL PERK" },
            ["eternal.sub.drb"]   = new[] { "{0}: sin la desventaja {1}", "{0}: drawback {1} removed" },
            ["eternal.sub.none"]  = new[] { "{0}: sin desventajas que quitar", "{0}: no drawbacks to remove" },
            ["eternal.promise"]   = new[] { "Si mueres, la botella vuelve por correo", "If you die, the bottle comes back by mail" },
            ["eternal.start"]     = new[] { "{0} perk(s) eterna(s) activa(s)", "{0} eternal perk(s) active" },
            ["nav.eternal"]       = new[] { "ETERNAS", "ETERNAL" },
            ["eternal.page"]      = new[] { "PERKS ETERNAS", "ETERNAL PERKS" },
            ["eternal.rules.title"] = new[] { "CÓMO FUNCIONAN", "HOW THEY WORK" },
            ["eternal.rules"]     = new[]
            {
                "Si bebes una perk que ya llevas activa en una raid, se vuelve eterna.\nUna perk eterna se queda contigo en las siguientes raids, activa desde el primer segundo, y pierde una desventaja: la mayor que lleves equipada y, si no tienes, una menor.\nSi mueres o no extraes, su botella vuelve por correo y deja de ser eterna.",
                "If you drink a perk you already have active in a raid, it becomes eternal.\nAn eternal perk stays with you in the following raids, active from the first second, and loses one drawback: the major one you have equipped or, if none, a minor one.\nIf you die or fail to extract, its bottle comes back by mail and it stops being eternal.",
            },
            ["eternal.active"]    = new[] { "ETERNAS ACTIVAS", "ACTIVE ETERNAL PERKS" },
            ["eternal.slot.empty"] = new[] { "SIN PERK ETERNA", "NO ETERNAL PERK" },
            ["eternal.slot.hint"] = new[] { "Bebe dos veces la misma perk en una raid.", "Drink the same perk twice in a raid." },
            ["eternal.tag"]       = new[] { "ETERNA", "ETERNAL" },
            ["eternal.removed"]   = new[] { "DESVENTAJA QUITADA", "DRAWBACK REMOVED" },
            ["eternal.removed.none"] = new[] { "No llevas desventajas equipadas en esta perk: no hay nada que quitar.", "No drawbacks equipped on this perk: nothing to remove." },
            ["eternal.block"]     = new[] { "{0} ya es eterna.", "{0} is already eternal." },
            ["eternal.returned"]  = new[] { "Perk eterna: botella devuelta por correo ({0})", "Eternal perk: bottle returned by mail ({0})" },
            ["powerup.title"]     = new[] { "PERK ALEATORIA", "RANDOM PERK" },
            ["powerup.spawn"]     = new[] { "Ha caído una botella: recógela en {0} s", "A bottle dropped: grab it within {0} s" },
            ["powerup.mail"]      = new[] { "Todas tus perks son eternas: {0} por correo", "All your perks are eternal: {0} sent by mail" },

            // avisos del modo Classic
            ["classic.start.sub"] = new[] { "{0} perk(s) activa(s)", "{0} active perk(s)" },
            ["classic.mod.sub"]   = new[] { "Efecto reducido: {0}% de la perk", "Reduced effect: {0}% of the perk" },
            ["classic.refund"]    = new[] { "{0}: botella devuelta por correo", "{0}: bottle returned by mail" },
            ["classic.block.dup"] = new[] { "{0} ya está activa.", "{0} is already active." },
            ["classic.block.full"] = new[] { "Máximo: {0} perks + 1 modificador.", "Maximum: {0} perks + 1 modifier." },
            ["classic.lost"]      = new[] { "Tu operador murió: has perdido tus perks del modo Clásico.", "Your operator died: you lost your Classic perks." },
            ["cc.start"]          = new[] { "DESAFÍO DE RAID", "RAID CHALLENGE" },
            ["cc.done"]           = new[] { "DESAFÍO CUMPLIDO", "RAID CHALLENGE DONE" },
            ["cc.done.sub"]       = new[] { "Extrae con vida para ganar una mejora", "Extract alive to earn an augment" },
            ["cc.reward"]         = new[] { "{0} gana la mejora {1}", "{0} gained the augment {1}" },

            // panel en raid (Classic)
            ["rp.classic.empty"]  = new[] { "No tienes perks activas. Bebe una para empezar.", "No active perks. Drink one to start." },
            ["rp.chal.none"]      = new[] { "Sin desafío en esta raid.", "No challenge this raid." },
            ["rp.chal.done"]      = new[] { "CUMPLIDO: extrae con vida", "DONE: extract alive" },
            ["rp.chal.ghost.ok"]  = new[] { "Sin daño por ahora", "No damage so far" },
            ["rp.chal.ghost.fail"] = new[] { "Recibiste daño: desafío perdido", "You took damage: challenge lost" },
            ["rp.chal.reward"]    = new[] { "Premio: 2.ª mejora menor para una perk al azar", "Reward: a 2nd minor augment for a random perk" },

            // desafios de raid (nombre .n, descripcion .d)
            ["cc.kills.n"]   = new[] { "Cazador", "Hunter" },
            ["cc.kills.d"]   = new[] { "Mata a 5 enemigos.", "Kill 5 enemies." },
            ["cc.heads.n"]   = new[] { "Cazacabezas", "Headhunter" },
            ["cc.heads.d"]   = new[] { "Mata a 3 enemigos con disparos a la cabeza.", "Kill 3 enemies with headshots." },
            ["cc.far.n"]     = new[] { "Tiro lejano", "Long shot" },
            ["cc.far.d"]     = new[] { "Mata a un enemigo desde 60 m o más.", "Kill an enemy from 60 m or more." },
            ["cc.pmc.n"]     = new[] { "Operador caído", "Operator down" },
            ["cc.pmc.d"]     = new[] { "Mata a 2 PMC.", "Kill 2 PMCs." },
            ["cc.grenade.n"] = new[] { "¡Granada va!", "Fire in the hole" },
            ["cc.grenade.d"] = new[] { "Mata a un enemigo con una granada.", "Kill an enemy with a grenade." },
            ["cc.melee.n"]   = new[] { "Acero frío", "Cold steel" },
            ["cc.melee.d"]   = new[] { "Mata a un enemigo cuerpo a cuerpo.", "Kill an enemy in melee." },
            ["cc.run.n"]     = new[] { "Cardio", "Cardio" },
            ["cc.run.d"]     = new[] { "Corre 800 m en carrera.", "Sprint 800 m." },
            ["cc.travel.n"]  = new[] { "Explorador", "Explorer" },
            ["cc.travel.d"]  = new[] { "Recorre 2.000 m.", "Travel 2,000 m." },
            ["cc.time.n"]    = new[] { "Superviviente", "Survivor" },
            ["cc.time.d"]    = new[] { "Sobrevive 15 minutos en la raid.", "Stay alive for 15 minutes in the raid." },
            ["cc.pair.n"]    = new[] { "Doble toque", "Double tap" },
            ["cc.pair.d"]    = new[] { "Mata a 2 enemigos en menos de 10 segundos.", "Kill 2 enemies within 10 seconds." },
            ["cc.medic.n"]   = new[] { "Médico de campo", "Field medic" },
            ["cc.medic.d"]   = new[] { "Usa 3 objetos médicos.", "Use 3 medical items." },
            ["cc.ghost.n"]   = new[] { "Fantasma", "Ghost" },
            ["cc.ghost.d"]   = new[] { "Extrae sin recibir ningún daño.", "Extract without taking any damage." },
            ["notice.phdflopper.mod"] = new[] { "Daño por caídas y explosiones -{0}%", "Fall and explosion damage -{0}%" },
            ["notice.web.sub"]        = new[] { "{0} enemigo(s) atrapado(s)", "{0} enemy(ies) webbed" },
            ["notice.skull.sub"]      = new[] { "¡La bala rebota en tu casco!", "The bullet ricochets off your helmet!" },
            ["notice.bastion.sub"]    = new[] { "Un golpe letal te deja a 1 PV", "A lethal hit leaves you at 1 HP" },
            ["notice.concussed.sub"]  = new[] { "Explosión cercana: el arma se balancea más", "Nearby explosion: your weapon sways more" },
            ["notice.bite.sub"]       = new[] { "Tu granada vuelve por correo", "Your grenade comes back by mail" },
            ["notice.burst.sub"]      = new[] { "{0} enemigo(s) atrapados y aturdidos", "{0} enemy(ies) webbed and stunned" },

            // ---- Stamin-Up: Iron Lungs ----
            ["st_lungs.n"]    = new[] { "PULMONES DE ACERO", "IRON LUNGS" },
            ["st_lungs.d"]    = new[] { "Aguantas la respiración un 60% más y la recuperas más rápido.", "You hold your breath 60% longer and recover it faster." },
            ["st_lungs.c"]    = new[] { "Aguanta la respiración 20 veces con Stamin-Up activa.", "Hold your breath 20 times with Stamin-Up active." },
            ["dt_chest.n"]    = new[] { "PECHO APRETADO", "TIGHT CHEST" },
            ["dt_chest.d"]    = new[] { "Con menos del 30% de estamina, el balanceo del arma sube un 10%.", "Below 30% stamina, weapon sway rises by 10%." },

            // ---- Juggernog ----
            ["clip.title"] = new[] { "Grabando clip", "Recording clip" },
            ["clip.start"] = new[] { "{0} s, {1} fotogramas (cámara lenta)", "{0} s, {1} frames (slow motion)" },
            ["clip.done"]  = new[] { "{0} fotogramas guardados en {1}", "{0} frames saved to {1}" },
            ["inj.title"]   = new[] { "Perks como inyectores", "Perks as injectors" },
            ["inj.restart"] = new[] { "Reinicia el servidor y el juego para que las perks pasen a ser {0}", "Restart the server and the game so perks become {0}" },
            ["inj.on"]      = new[] { "inyectores", "injectors" },
            ["inj.off"]     = new[] { "latas", "cans" },
            // ---- Bandolier Bandit / Vulture Aid / Melee Macchiato ----
            ["bb_sleek.n"] = new[] { "ELEGANTE", "SLEEK" },
            ["bb_sleek.d"] = new[] { "Al vaciar el cargador, 20% de que se rellene solo con la munición suelta que llevas.", "When you empty a magazine, 20% chance it refills itself from the loose ammo you carry." },
            ["bb_sleek.c"] = new[] { "Mata a 3 enemigos sin recargar entre medias, 5 veces, con Bandolier activa.", "Kill 3 enemies without reloading in between, 5 times, with Bandolier active." },
            ["bb_disc.n"] = new[] { "CINTURÓN DE BALAS", "BULLET BELT" },
            ["bb_disc.d"] = new[] { "Llevando 6 cargadores llenos o más (fuera del arma), tus balas hacen un 10% más de daño.", "While carrying 6 or more full magazines (outside your weapon), your bullets deal 10% more damage." },
            ["bb_disc.c"] = new[] { "Mata a 5 enemigos llevando 6 cargadores llenos, con Bandolier activa.", "Kill 5 enemies while carrying 6 full magazines, with Bandolier active." },
            ["bb_bring.n"] = new[] { "¡QUE VENGAN!", "BRING IT ON!" },
            ["bb_bring.d"] = new[] { "La munición y los cargadores no pesan nada.", "Ammo and magazines weigh nothing." },
            ["bb_bring.c"] = new[] { "Mata a 10 enemigos cargando 50 kg o más, con Bandolier activa.", "Kill 10 enemies while carrying 50 kg or more, with Bandolier active." },
            ["bb_pouch.n"] = new[] { "BOLSILLO EXTRA", "EXTRA POUCH" },
            ["bb_pouch.d"] = new[] { "Cada baja tiene un 10% de darte una granada F-1.", "Each kill has a 10% chance to give you an F-1 grenade." },
            ["bb_pouch.c"] = new[] { "Mata a 5 enemigos con explosivos, con Bandolier activa.", "Kill 5 enemies with explosives, with Bandolier active." },
            ["bb_cold.n"] = new[] { "CAÑÓN FRÍO", "COLD BARREL" },
            ["bb_cold.d"] = new[] { "Tus armas se calientan un 30% más despacio.", "Your weapons heat up 30% slower." },
            ["bb_cold.c"] = new[] { "Dispara 1,000 balas con Bandolier activa.", "Fire 1,000 rounds with Bandolier active." },
            ["bb_leech.n"] = new[] { "SANGUIJUELA DE MUNICIÓN", "AMMO LEECH" },
            ["bb_leech.d"] = new[] { "Matar de un tiro a la cabeza devuelve 2 balas al cargador.", "A headshot kill puts 2 rounds back in your magazine." },
            ["bb_leech.c"] = new[] { "Mata a 15 enemigos de un tiro a la cabeza, con Bandolier activa.", "Kill 15 enemies with headshots, with Bandolier active." },
            ["bb_strap.n"] = new[] { "CORREA ACOLCHADA", "PADDED STRAP" },
            ["bb_strap.d"] = new[] { "Tus armas equipadas pesan la mitad.", "Your equipped weapons weigh half." },
            ["bb_strap.c"] = new[] { "Recorre 3 km con dos armas principales equipadas, con Bandolier activa.", "Travel 3 km with two primary weapons equipped, with Bandolier active." },
            ["bb_slot.n"] = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["bb_slot.d"] = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["bb_slot.c"] = new[] { "Mata a 1 boss con Bandolier activa.", "Kill 1 boss with Bandolier active." },
            ["bb_d_loose.n"] = new[] { "BALAS SUELTAS", "LOOSE ROUNDS" },
            ["bb_d_loose.d"] = new[] { "Al recargar, 10% de que se te caiga una bala del cargador.", "When reloading, 10% chance a round falls out of the magazine." },
            ["bb_d_numb.n"] = new[] { "ENTUMECIDO", "NUMB" },
            ["bb_d_numb.d"] = new[] { "Los analgésicos duran un 30% menos.", "Painkillers last 30% shorter." },
            ["bb_d_clatter.n"] = new[] { "TRASTOS RUIDOSOS", "GEAR CLATTER" },
            ["bb_d_clatter.d"] = new[] { "Saltar y aterrizar se oye a 20 m.", "Jumping and landing can be heard from 20 m." },
            ["bb_d_pin.n"] = new[] { "ANILLA FLOJA", "ITCHY PIN" },
            ["bb_d_pin.d"] = new[] { "Tus granadas tienen 0.5 s menos de mecha.", "Your grenades have a 0.5 s shorter fuse." },
            ["bb_d_brass.n"] = new[] { "LATÓN CALIENTE", "HOT BRASS" },
            ["bb_d_brass.d"] = new[] { "Tus armas se desgastan un 20% más rápido.", "Your weapons wear out 20% faster." },
            ["bb_d_fumble.n"] = new[] { "TORPEZA", "FUMBLE" },
            ["bb_d_fumble.d"] = new[] { "Te mueves un 20% más lento mientras recargas.", "You move 20% slower while reloading." },
            ["bb_d_burn.n"] = new[] { "ROCE DE CORREA", "STRAP BURN" },
            ["bb_d_burn.d"] = new[] { "El aguante de los brazos se recupera un 20% más lento.", "Arm stamina recovers 20% slower." },
            ["notice.bb_sleek"] = new[] { "¡Cargador rellenado!", "Magazine refilled!" },
            ["notice.bb_pouch"] = new[] { "Has encontrado una granada", "You found a grenade" },
            ["va_scent.n"] = new[] { "OLOR A DINERO", "SCENT OF MONEY" },
            ["va_scent.d"] = new[] { "Los PMC que llevan más de 250k ₽ encima se marcan 4 s a través de las paredes (hasta 80 m; uno cada 60 s).", "PMCs carrying more than 250k ₽ are marked through walls for 4 s (up to 80 m; one every 60 s)." },
            ["va_scent.c"] = new[] { "Registra 10 cuerpos de PMC con Vulture activa.", "Search 10 PMC bodies with Vulture active." },
            ["va_bonus.n"] = new[] { "BOTÍN DE CARROÑA", "CARRION BONUS" },
            ["va_bonus.d"] = new[] { "Tus bajas de PMC y jefes tienen un 15% de llevar algo extra (GP Coin, tarjeta de Labs, tarjeta de Labrys o medalla LEGA). Los Scavs, un 5% de GP Coin.", "Your PMC and boss kills have a 15% chance to carry something extra (GP Coin, Labs keycard, Labrys keycard or LEGA medal). Scavs, 5% for a GP Coin." },
            ["va_bonus.c"] = new[] { "Mata a 25 enemigos con Vulture activa.", "Kill 25 enemies with Vulture active." },
            ["va_reroll.n"] = new[] { "RE-ROLL", "RE-ROLL" },
            ["va_reroll.d"] = new[] { "Mirando un contenedor, pulsa la tecla de re-roll para volver a tirar su botín. 10 puntos por raid: normal 1, Scav muerto 2, caja fuerte 5, aéreo 7 (+0.5 cada vez que repites). Puede fallar (15-40%) y te quedas con lo que había.", "Looking at a container, press the re-roll key to roll its loot again. 10 points per raid: normal 1, dead Scav 2, safe 5, airdrop 7 (+0.5 each repeat). It can fail (15-40%) and you keep what was there." },
            ["va_reroll.c"] = new[] { "Registra 40 contenedores con Vulture activa.", "Search 40 containers with Vulture active." },
            ["va_sharp.n"] = new[] { "VISTA AGUDA", "SHARP EYES" },
            ["va_sharp.d"] = new[] { "Las marcas de botín llegan a 30 m.", "Loot markers reach 30 m." },
            ["va_sharp.c"] = new[] { "Registra 20 contenedores con Vulture activa.", "Search 20 containers with Vulture active." },
            ["va_leftover.n"] = new[] { "SOBRAS", "LEFTOVERS" },
            ["va_leftover.d"] = new[] { "Tus bajas tienen un 25% de llevar munición de tu calibre.", "Your kills have a 25% chance to carry ammo of your caliber." },
            ["va_leftover.c"] = new[] { "Mata a 15 enemigos con Vulture activa.", "Kill 15 enemies with Vulture active." },
            ["va_kit.n"] = new[] { "BOTIQUÍN DE CAMPO", "FIELD KIT" },
            ["va_kit.d"] = new[] { "Tus bajas tienen un 15% de llevar un botiquín AI-2.", "Your kills have a 15% chance to carry an AI-2 medkit." },
            ["va_kit.c"] = new[] { "Mata a 10 enemigos a menos de 15 m, con Vulture activa.", "Kill 10 enemies within 15 m, with Vulture active." },
            ["va_lucky.n"] = new[] { "HALLAZGO AFORTUNADO", "LUCKY FIND" },
            ["va_lucky.d"] = new[] { "Al registrar un contenedor, 10% de que haya una GP Coin dentro.", "When you search a container, 10% chance there's a GP Coin inside." },
            ["va_lucky.c"] = new[] { "Registra 15 cuerpos con Vulture activa.", "Search 15 bodies with Vulture active." },
            ["va_slot.n"] = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["va_slot.d"] = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["va_slot.c"] = new[] { "Mata a 1 boss con Vulture activa.", "Kill 1 boss with Vulture active." },
            ["va_d_stench.n"] = new[] { "HEDOR", "STENCH" },
            ["va_d_stench.d"] = new[] { "Tus pasos al andar se oyen a 15 m.", "Your footsteps are audible from 15 m when walking." },
            ["va_d_greedy.n"] = new[] { "CODICIA", "GREED" },
            ["va_d_greedy.d"] = new[] { "Registrar cuerpos y contenedores es un 25% más lento.", "Searching bodies and containers is 25% slower." },
            ["va_d_hunch.n"] = new[] { "CARROÑERO MARCADO", "MARKED SCAVENGER" },
            ["va_d_hunch.d"] = new[] { "Al registrar un cuerpo de PMC, los bots cercanos saben dónde estás durante 10 s.", "When you search a PMC body, nearby bots know where you are for 10 s." },
            ["notice.va_d_hunch"] = new[] { "Los bots cercanos saben dónde estás", "Nearby bots know where you are" },
            ["va_d_dry.n"] = new[] { "MOCHILA LLENA", "STUFFED PACK" },
            ["va_d_dry.d"] = new[] { "Con la mochila llena más de un 80%, te mueves un 5% más lento.", "With your backpack over 80% full, you move 5% slower." },
            ["va_d_myopia.n"] = new[] { "MIOPÍA", "MYOPIA" },
            ["va_d_myopia.d"] = new[] { "Apuntas un 10% más lento.", "You aim down sights 10% slower." },
            ["va_d_clumsy.n"] = new[] { "GARRAS TORPES", "CLUMSY TALONS" },
            ["va_d_clumsy.d"] = new[] { "Sacar y cambiar de arma es un 10% más lento.", "Drawing and swapping weapons is 10% slower." },
            ["va_d_grip.n"] = new[] { "RUIDO DE BOLSAS", "RUSTLING BAGS" },
            ["va_d_grip.d"] = new[] { "Mover objetos en el inventario durante la raid hace ruido (8 m).", "Moving items in your inventory during a raid makes noise (8 m)." },
            ["notice.va_scent"] = new[] { "PMC con {1}k ₽ a {0} m", "PMC carrying {1}k ₽ at {0} m" },
            ["notice.va_drop"] = new[] { "Tu baja llevaba algo extra", "Your kill was carrying something extra" },
            ["notice.va_lucky"] = new[] { "Hay una GP Coin dentro", "There's a GP Coin inside" },
            ["notice.reroll_ok"] = new[] { "Botín nuevo: {0} objetos · quedan {1} puntos", "New loot: {0} items · {1} points left" },
            ["notice.reroll_fail"] = new[] { "Ha fallado: se queda el botín original · quedan {0} puntos", "It failed: the original loot stays · {0} points left" },
            ["notice.reroll_points"] = new[] { "Cuesta {0} y te quedan {1} puntos", "It costs {0} and you have {1} points left" },
            ["notice.reroll_no"] = new[] { "Este contenedor no se puede re-tirar", "This container can't be re-rolled" },
            ["prompt.reroll"] = new[] { "[{0}] Re-roll del contenedor ({1} puntos)", "[{0}] Re-roll container ({1} points)" },
            ["mm_charge.n"] = new[] { "CARGA", "CHARGE" },
            ["mm_charge.d"] = new[] { "Corriendo con un arma cuerpo a cuerpo en la mano, recibes un 40% menos de daño.", "While sprinting with a melee weapon in hand, you take 40% less damage." },
            ["mm_charge.c"] = new[] { "Corre 2 km con un arma cuerpo a cuerpo en la mano, con Macchiato activa.", "Sprint 2 km with a melee weapon in hand, with Macchiato active." },
            ["mm_frenzy.n"] = new[] { "FRENESÍ", "FRENZY" },
            ["mm_frenzy.d"] = new[] { "Cada golpe seguido al mismo objetivo hace x1.2 de daño (hasta x3).", "Each consecutive hit on the same target deals x1.2 damage (up to x3)." },
            ["mm_frenzy.c"] = new[] { "Golpea 3 veces seguidas al mismo enemigo, 10 veces, con Macchiato activa.", "Hit the same enemy 3 times in a row, 10 times, with Macchiato active." },
            ["mm_iron.n"] = new[] { "CRÁNEO DE HIERRO", "IRON SKULL" },
            ["mm_iron.d"] = new[] { "Con un arma cuerpo a cuerpo en la mano, un golpe en la cabeza no te mata.", "With a melee weapon in hand, a hit to the head can't kill you." },
            ["mm_iron.c"] = new[] { "Mata a 10 enemigos cuerpo a cuerpo, con Macchiato activa.", "Kill 10 enemies in melee, with Macchiato active." },
            ["mm_quick.n"] = new[] { "DESENFUNDE RÁPIDO", "QUICK DRAW" },
            ["mm_quick.d"] = new[] { "Sacar el arma cuerpo a cuerpo es instantáneo.", "Drawing your melee weapon is instant." },
            ["mm_quick.c"] = new[] { "Saca el arma cuerpo a cuerpo 30 veces, con Macchiato activa.", "Draw your melee weapon 30 times, with Macchiato active." },
            ["mm_hone.n"] = new[] { "PIEDRA DE AFILAR", "WHETSTONE" },
            ["mm_hone.d"] = new[] { "Cada golpe que acierta repara 2 puntos tu arma cuerpo a cuerpo.", "Each hit that lands repairs your melee weapon by 2 points." },
            ["mm_hone.c"] = new[] { "Acierta 50 golpes cuerpo a cuerpo, con Macchiato activa.", "Land 50 melee hits, with Macchiato active." },
            ["mm_flash.n"] = new[] { "GOLPE RELÁMPAGO", "FLASH STRIKE" },
            ["mm_flash.d"] = new[] { "El ataque rápido es casi instantáneo (x2.5 de velocidad).", "The quick attack is almost instant (x2.5 speed)." },
            ["mm_flash.c"] = new[] { "Ataca 200 veces cuerpo a cuerpo, con Macchiato activa.", "Make 200 melee attacks, with Macchiato active." },
            ["mm_wind.n"] = new[] { "SEGUNDO ALIENTO", "SECOND WIND" },
            ["mm_wind.d"] = new[] { "Cada ataque te devuelve 5 de estamina; si acierta, 15.", "Each attack restores 5 stamina; 15 if it lands." },
            ["mm_wind.c"] = new[] { "Ataca 25 veces con menos del 30% de estamina, con Macchiato activa.", "Attack 25 times below 30% stamina, with Macchiato active." },
            ["mm_slot.n"] = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["mm_slot.d"] = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["mm_slot.c"] = new[] { "Mata a 1 boss cuerpo a cuerpo, con Macchiato activa.", "Kill 1 boss in melee, with Macchiato active." },
            ["mm_d_glass.n"] = new[] { "MANDÍBULA DE CRISTAL", "GLASS JAW" },
            ["mm_d_glass.d"] = new[] { "Con un arma de fuego en la mano, recibes un 10% más de daño.", "With a firearm in hand, you take 10% more damage." },
            ["mm_d_jitter.n"] = new[] { "TEMBLOR DE CAFEÍNA", "CAFFEINE JITTERS" },
            ["mm_d_jitter.d"] = new[] { "Tus armas de fuego se balancean un 20% más.", "Your firearms sway 20% more." },
            ["mm_d_crash.n"] = new[] { "SED DE SANGRE", "BLOODLUST" },
            ["mm_d_crash.d"] = new[] { "Si pasas 90 s sin acertar un golpe cuerpo a cuerpo, tus balas hacen un 15% menos de daño hasta que aciertes uno.", "Go 90 s without landing a melee hit and your bullets deal 15% less damage until you land one." },
            ["mm_d_butter.n"] = new[] { "MANOS DE MANTEQUILLA", "BUTTERFINGERS" },
            ["mm_d_butter.d"] = new[] { "Sacar armas de fuego es un 15% más lento.", "Drawing firearms is 15% slower." },
            ["mm_d_bitter.n"] = new[] { "IMPULSIVO", "IMPULSIVE" },
            ["mm_d_bitter.d"] = new[] { "Al guardar el arma cuerpo a cuerpo, tu arma de fuego tarda 0.3 s más en poder apuntar y disparar.", "After putting away your melee weapon, your firearm takes 0.3 s longer before it can aim and fire." },
            ["mm_d_loud.n"] = new[] { "PASOS FUERTES", "LOUD STEPS" },
            ["mm_d_loud.d"] = new[] { "Tus pasos al andar se oyen a 12 m.", "Your footsteps are audible from 12 m when walking." },
            ["mm_d_sugar.n"] = new[] { "GOLPE EN FALSO", "WHIFF" },
            ["mm_d_sugar.d"] = new[] { "Fallar un golpe cuerpo a cuerpo te quita 10 de estamina.", "Missing a melee swing costs 10 stamina." },
            ["dd_aim.n"]      = new[] { "DEAD AIM", "DEAD AIM" },
            ["dd_aim.d"]      = new[] { "El arma se queda fija en el centro: sin balanceo al moverte, al girar ni al apuntar (el retroceso se mantiene).", "Your weapon stays locked to the center: no sway while moving, turning or aiming (recoil still applies)." },
            ["dd_aim.c"]      = new[] { "Mata a 20 enemigos mientras te mueves, con Deadshot activa.", "Kill 20 enemies while moving, with Deadshot active." },
            ["dd_pen.n"]      = new[] { "INCREÍBLE", "INCREDIBLE" },
            ["dd_pen.d"]      = new[] { "Tus balas penetran un 30% más la armadura.", "Your bullets have 30% more armor penetration." },
            ["dd_pen.c"]      = new[] { "Mata a 15 enemigos con armadura, con Deadshot activa.", "Kill 15 armored enemies with Deadshot active." },
            ["dd_sky.n"]      = new[] { "HACIA EL CIELO", "TO THE SKIES" },
            ["dd_sky.d"]      = new[] { "El retroceso vertical y horizontal baja un 50%.", "Vertical and horizontal recoil drop by 50%." },
            ["dd_sky.c"]      = new[] { "Dispara 1,500 balas con Deadshot activa.", "Fire 1,500 rounds with Deadshot active." },
            ["dd_quick.n"]    = new[] { "MIRA RÁPIDA", "QUICK SCOPE" },
            ["dd_quick.d"]    = new[] { "Apuntas un 20% más rápido.", "You aim down sights 20% faster." },
            ["dd_quick.c"]    = new[] { "Mata a 10 enemigos apuntando, con Deadshot activa.", "Kill 10 enemies while aiming, with Deadshot active." },
            ["dd_steady.n"]   = new[] { "PULSO FIRME", "STEADY PULSE" },
            ["dd_steady.d"]   = new[] { "Aguantas la respiración un 40% más.", "You hold your breath 40% longer." },
            ["dd_steady.c"]   = new[] { "Aguanta la respiración apuntando 25 veces, con Deadshot activa.", "Hold your breath while aiming 25 times, with Deadshot active." },
            ["dd_head.n"]     = new[] { "CAZACABEZAS", "HEADHUNTER" },
            ["dd_head.d"]     = new[] { "Matar con un tiro a la cabeza te devuelve el 20% de la estamina.", "A headshot kill restores 20% of your stamina." },
            ["dd_head.c"]     = new[] { "Mata a 15 enemigos de un tiro a la cabeza, con Deadshot activa.", "Kill 15 enemies with headshots, with Deadshot active." },
            ["dd_still.n"]    = new[] { "QUIETUD", "STILLNESS" },
            ["dd_still.d"]    = new[] { "Agachado o tumbado recuperas la estamina y el aguante de los brazos más rápido.", "While crouched or prone, stamina and arm stamina recover faster." },
            ["dd_still.c"]    = new[] { "Mata a 10 enemigos agachado o tumbado, con Deadshot activa.", "Kill 10 enemies while crouched or prone, with Deadshot active." },
            ["dd_slot.n"]     = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["dd_slot.d"]     = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["dd_slot.c"]     = new[] { "Mata a 1 boss con Deadshot activa.", "Kill 1 boss with Deadshot active." },
            ["dd_d_tunnel.n"] = new[] { "VISIÓN DE TÚNEL", "TUNNEL VISION" },
            ["dd_d_tunnel.d"] = new[] { "Al apuntar, tu campo de visión se estrecha un 10%.", "When aiming, your field of view narrows by 10%." },
            ["dd_d_breath.n"] = new[] { "TODO O NADA", "ALL OR NOTHING" },
            ["dd_d_breath.d"] = new[] { "Los disparos que no dan en la cabeza hacen un 10% menos de daño.", "Shots that don't hit the head deal 10% less damage." },
            ["dd_d_shaky.n"]  = new[] { "GATILLO PESADO", "HEAVY TRIGGER" },
            ["dd_d_shaky.d"]  = new[] { "Cada disparo gasta un poco de aguante de los brazos.", "Every shot drains a bit of arm stamina." },
            ["dd_d_strain.n"] = new[] { "PULSO NERVIOSO", "NERVOUS HANDS" },
            ["dd_d_strain.d"] = new[] { "Disparar desde la cadera tiene un 20% más de dispersión.", "Hip-fire has 20% more spread." },
            ["dd_d_lime.n"]   = new[] { "OJO SECO", "DRY EYE" },
            ["dd_d_lime.d"]   = new[] { "Si apuntas más de 6 s seguidos, la mira empieza a temblar (balanceo +50%).", "Aim for more than 6 s straight and your sight starts to shake (+50% sway)." },
            ["dd_d_sugar.n"]  = new[] { "TIRO LIMPIO", "CLEAN SHOT" },
            ["dd_d_sugar.d"]  = new[] { "Disparar menos de 0.5 s después de correr tiene un 30% más de retroceso.", "Firing within 0.5 s of sprinting has 30% more recoil." },
            ["dd_d_squeak.n"] = new[] { "BOTAS CHIRRIANTES", "SQUEAKY BOOTS" },
            ["dd_d_squeak.d"] = new[] { "Tus pasos al andar se oyen a 12 m.", "Your footsteps are audible from 12 m when walking." },
            ["jg_skull.n"]    = new[] { "GORRO DE CALAVERA", "SKULL CAP" },
            ["jg_skull.d"]    = new[] { "Más probabilidad de que las balas reboten en tu casco.", "Higher chance that bullets ricochet off your helmet." },
            ["jg_skull.c"]    = new[] { "Recibe 1,500 de daño con Juggernog activa.", "Take 1,500 damage with Juggernog active." },
            ["jg_thick.n"]    = new[] { "PIEL GRUESA", "THICK SKIN" },
            ["jg_thick.d"]    = new[] { "Los sangrados te quitan la mitad de vida.", "Bleeding drains half as much health." },
            ["jg_thick.c"]    = new[] { "Arregla 5 sangrados pesados con Juggernog activa.", "Fix 5 heavy bleeds with Juggernog active." },
            ["jg_bones.n"]    = new[] { "HUESOS PESADOS", "HEAVY BONES" },
            ["jg_bones.d"]    = new[] { "La mitad de las fracturas no llegan a ocurrir.", "Half of all fractures never happen." },
            ["jg_bones.c"]    = new[] { "Arregla 5 fracturas con Juggernog activa.", "Fix 5 fractures with Juggernog active." },
            ["jg_patch.n"]    = new[] { "PARCHE RÁPIDO", "QUICK PATCH" },
            ["jg_patch.d"]    = new[] { "Las animaciones de curación van el doble de rápido.", "Healing animations are twice as fast." },
            ["jg_patch.c"]    = new[] { "Usa 10 items médicos con Juggernog activa.", "Use 10 medical items with Juggernog active." },
            ["jg_plates.n"]   = new[] { "PLACAS DE HUESO", "BONE PLATING" },
            ["jg_plates.d"]   = new[] { "Tus placas de armadura pierden durabilidad la mitad de rápido.", "Your armor plates lose durability half as fast." },
            ["jg_plates.c"]   = new[] { "Recibe 60 impactos en tu armadura con Juggernog activa.", "Take 60 hits on your armor with Juggernog active." },
            ["jg_keep.n"]     = new[] { "SIGUE ADELANTE", "KEEP GOING" },
            ["jg_keep.d"]     = new[] { "El hambre y la sed te quitan vida mucho más lento.", "Hunger and thirst drain your health much more slowly." },
            ["jg_keep.c"]     = new[] { "Come o bebe 15 veces con Juggernog activa.", "Eat or drink 15 times with Juggernog active." },
            ["jg_bastion.n"]  = new[] { "ÚLTIMO BALUARTE", "LAST BASTION" },
            ["jg_bastion.d"]  = new[] { "1 vez por raid, un golpe letal a la cabeza o al torso te deja a 1 PV.", "Once per raid, a lethal hit to the head or thorax leaves you at 1 HP." },
            ["jg_bastion.c"]  = new[] { "Extrae con vida 5 veces con Juggernog activa.", "Extract alive 5 times with Juggernog active." },
            ["jg_slot.n"]     = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["jg_slot.d"]     = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["jg_slot.c"]     = new[] { "Mata a 1 boss con Juggernog activa.", "Kill 1 boss with Juggernog active." },
            ["dj_clank.n"]    = new[] { "PLACAS RUIDOSAS", "CLANKY PLATES" },
            ["dj_clank.d"]    = new[] { "Tus pasos al andar se oyen a 12 m.", "Your footsteps are audible from 12 m when walking." },
            ["dj_plates.n"]   = new[] { "PLACAS PESADAS", "HEAVY PLATES" },
            ["dj_plates.d"]   = new[] { "Llevas un 10% más de peso.", "You carry 10% more weight." },
            ["dj_hands.n"]    = new[] { "MANOS PESADAS", "HEAVY HANDS" },
            ["dj_hands.d"]    = new[] { "El balanceo del arma sube un 20%.", "Weapon sway rises by 20%." },
            ["dj_dead.n"]     = new[] { "BLANCO GRANDE", "BIG TARGET" },
            ["dj_dead.d"]     = new[] { "Los bots te ven desde un 15% más lejos.", "Bots can see you from 15% farther away." },
            ["dj_sluggish.n"] = new[] { "CURA LENTA", "SLOW MENDING" },
            ["dj_sluggish.d"] = new[] { "Los botiquines curan un 25% menos por uso.", "Medkits heal 25% less per use." },
            ["dj_metab.n"]    = new[] { "TORPE", "CLUMSY" },
            ["dj_metab.d"]    = new[] { "Saltar gasta el doble de estamina.", "Jumping costs twice the stamina." },
            ["dj_chew.n"]     = new[] { "MASTICAR LENTO", "SLOW CHEWER" },
            ["dj_chew.d"]     = new[] { "Comer y beber tarda un 30% más.", "Eating and drinking takes 30% longer." },

            // ---- PHD Flopper ----
            ["pf_absorb.n"]   = new[] { "ABSORBEDOR DE EXPLOSIONES", "BLAST ABSORBER" },
            ["pf_absorb.d"]   = new[] { "Curas el 25% del daño de explosión que causas.", "You heal for 25% of the explosion damage you deal." },
            ["pf_absorb.c"]   = new[] { "Causa 500 de daño con explosiones con PHD Flopper activa.", "Deal 500 explosion damage with PHD Flopper active." },
            ["pf_shock.n"]    = new[] { "ONDA EXPANSIVA", "SHOCKWAVE" },
            ["pf_shock.d"]    = new[] { "Tus granadas tienen el doble de radio y la mitad de tiempo de mecha.", "Your grenades have double the radius and half the fuse time." },
            ["pf_shock.c"]    = new[] { "Mata a 5 enemigos con granadas con PHD Flopper activa.", "Kill 5 enemies with grenades with PHD Flopper active." },
            ["pf_drop.n"]     = new[] { "CAÍDA EXPLOSIVA", "EXPLOSIVE DROP" },
            ["pf_drop.d"]     = new[] { "Caer desde 3 m o más crea una explosión pequeña a tu alrededor que a ti no te hace daño.", "Falling from 3 m or higher sets off a small explosion around you that doesn't hurt you." },
            ["pf_drop.c"]     = new[] { "Aguanta 15 explosiones con PHD Flopper activa.", "Shrug off 15 explosions with PHD Flopper active." },
            ["pf_soles.n"]    = new[] { "SUELAS SUAVES", "SOFT SOLES" },
            ["pf_soles.d"]    = new[] { "Aterrizar tras una caída no te frena.", "Landing after a fall doesn't slow you down." },
            ["pf_soles.c"]    = new[] { "Cae desde 3 m o más 10 veces con PHD Flopper activa.", "Fall from 3 m or higher 10 times with PHD Flopper active." },
            ["pf_stop.n"]     = new[] { "PARADA TÉCNICA", "PIT STOP" },
            ["pf_stop.d"]     = new[] { "Si te quedas quieto, la estamina se recupera el doble de rápido.", "Standing still recovers stamina twice as fast." },
            ["pf_stop.c"]     = new[] { "Quédate quieto 5 minutos en total con PHD Flopper activa.", "Stand still for 5 minutes in total with PHD Flopper active." },
            ["pf_ears.n"]     = new[] { "TAPONES", "EAR PLUGS" },
            ["pf_ears.d"]     = new[] { "Eres inmune a la contusión.", "You are immune to contusion." },
            ["pf_ears.c"]     = new[] { "Sufre 5 contusiones con PHD Flopper activa.", "Suffer 5 contusions with PHD Flopper active." },
            ["pf_terrain.n"]  = new[] { "CAMINANTE DEL TERRENO", "TERRAIN WALKER" },
            ["pf_terrain.d"]  = new[] { "Eres inmune al alambre de púas y al barro.", "You are immune to barbed wire and mud." },
            ["pf_terrain.c"]  = new[] { "Recorre 5 km con PHD Flopper activa.", "Travel 5 km with PHD Flopper active." },
            ["pf_slot.n"]     = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["pf_slot.d"]     = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["pf_slot.c"]     = new[] { "Mata a 1 boss con PHD Flopper activa.", "Kill 1 boss with PHD Flopper active." },
            ["dp_concussed.n"] = new[] { "CONMOCIÓN", "CONCUSSED" },
            ["dp_concussed.d"] = new[] { "Tras una explosión cercana, el balanceo del arma sube un 40% durante 8 s.", "After a nearby explosion, weapon sway rises by 40% for 8 s." },
            ["dp_throw.n"]    = new[] { "LANZAMIENTO PESADO", "HEAVY THROW" },
            ["dp_throw.d"]    = new[] { "Lanzar una granada te quita 25 de estamina.", "Throwing a grenade costs you 25 stamina." },
            ["dp_powder.n"]   = new[] { "ADICTO A LA PÓLVORA", "POWDER JUNKIE" },
            ["dp_powder.d"]   = new[] { "Sin granadas en el inventario, el balanceo del arma sube un 15%.", "With no grenades in your inventory, weapon sway rises by 15%." },
            ["dp_stagger.n"]  = new[] { "TAMBALEO", "STAGGER" },
            ["dp_stagger.d"]  = new[] { "Aterrizar de una caída de 3 m o más te quita 40 de estamina.", "Landing from a fall of 3 m or more costs you 40 stamina." },
            ["dp_lazy.n"]     = new[] { "METRALLA", "SHRAPNEL" },
            ["dp_lazy.d"]     = new[] { "Una explosión a menos de 10 m tiene un 30% de causarte un sangrado leve (la metralla atraviesa tu inmunidad).", "An explosion within 10 m has a 30% chance to give you a light bleed (shrapnel gets past your immunity)." },
            ["dp_pockets.n"]  = new[] { "PÁNICO", "PANIC" },
            ["dp_pockets.d"]  = new[] { "Una explosión a menos de 10 m te quita 20 de estamina.", "An explosion within 10 m costs you 20 stamina." },
            ["dp_stomp.n"]    = new[] { "PISADAS FUERTES", "HARD STOMP" },
            ["dp_stomp.d"]    = new[] { "Tus pasos se oyen a 15 m.", "Your footsteps are audible from 15 m." },

            // ---- Widow's Wine ----
            ["ww_spin.n"]     = new[] { "HILERA", "SPINNERET" },
            ["ww_spin.d"]     = new[] { "La telaraña tiene el doble de radio y dura 2 s más.", "The web has double the radius and lasts 2 s longer." },
            ["ww_spin.c"]     = new[] { "Atrapa a 25 enemigos con Widow's Wine activa.", "Web 25 enemies with Widow's Wine active." },
            ["ww_fuse.n"]     = new[] { "NIDO", "NEST" },
            ["ww_fuse.d"]     = new[] { "Cada telaraña deja una trampa en el suelo durante 20 s: el primer enemigo que la pisa queda atrapado 4 s.", "Each web leaves a trap on the ground for 20 s: the first enemy to step on it is stuck for 4 s." },
            ["notice.ww_fuse"] = new[] { "Un enemigo ha caído en tu nido", "An enemy walked into your nest" },
            ["ww_fuse.c"]     = new[] { "Atrapa a 3 enemigos con una sola granada.", "Web 3 enemies with a single grenade." },
            ["ww_burst.n"]    = new[] { "RÁFAGA DE TELARAÑAS", "WEB BURST" },
            ["ww_burst.d"]    = new[] { "Si recibes 100 de daño en menos de 3 s, gastas una granada del inventario: los enemigos cercanos quedan atrapados y aturdidos (1 vez cada 5 min).", "If you take 100 damage in under 3 s, a grenade from your inventory is spent: nearby enemies are webbed and stunned (once every 5 min)." },
            ["ww_burst.c"]    = new[] { "Recibe 100 de daño en menos de 3 s, 3 veces, con Widow's Wine activa.", "Take 100 damage in under 3 s, 3 times, with Widow's Wine active." },
            ["ww_venom.n"]    = new[] { "MORDIDA VENENOSA", "VENOMOUS BITE" },
            ["ww_venom.d"]    = new[] { "Los enemigos atrapados en la telaraña también quedan aturdidos.", "Enemies caught in the web are also stunned." },
            ["ww_venom.c"]    = new[] { "Mata a 5 enemigos atrapados con Widow's Wine activa.", "Kill 5 webbed enemies with Widow's Wine active." },
            ["ww_quick.n"]    = new[] { "HILANDERO RÁPIDO", "QUICK SPINNER" },
            ["ww_quick.d"]    = new[] { "Preparas y lanzas las granadas un 40% más rápido.", "You ready and throw grenades 40% faster." },
            ["ww_quick.c"]    = new[] { "Lanza 10 granadas con Widow's Wine activa.", "Throw 10 grenades with Widow's Wine active." },
            ["ww_sticky.n"]   = new[] { "SUELO PEGAJOSO", "STICKY FLOOR" },
            ["ww_sticky.d"]   = new[] { "Puedes lanzar las granadas un 35% más lejos.", "You can throw grenades 35% farther." },
            ["ww_sticky.c"]   = new[] { "Atrapa a 10 enemigos con telarañas que estallen a 20 m o más de ti, con Widow's Wine activa.", "Web 10 enemies with webs that burst at least 20 m away from you, with Widow's Wine active." },
            ["ww_bite.n"]     = new[] { "PICADURA DE LA VIUDA", "WIDOW'S BITE" },
            ["ww_bite.d"]     = new[] { "Si una granada tuya mata a un enemigo, te la devuelven (llega por correo al terminar la raid).", "If one of your grenades kills an enemy, it is returned to you (it arrives by mail after the raid)." },
            ["ww_bite.c"]     = new[] { "Mata a 10 enemigos con granadas con Widow's Wine activa.", "Kill 10 enemies with grenades with Widow's Wine active." },
            ["ww_slot.n"]     = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["ww_slot.d"]     = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["ww_slot.c"]     = new[] { "Mata a 1 boss con Widow's Wine activa.", "Kill 1 boss with Widow's Wine active." },
            ["dw_loud.n"]     = new[] { "LANZAMIENTO RUIDOSO", "LOUD THROW" },
            ["dw_loud.d"]     = new[] { "Lanzar una granada hace ruido (20 m).", "Throwing a grenade makes noise (20 m)." },
            ["dw_brittle.n"]  = new[] { "PLACAS FRÁGILES", "BRITTLE PLATES" },
            ["dw_brittle.d"]  = new[] { "Tu armadura recibe un 25% más de daño de las balas.", "Your armor takes 25% more damage from bullets." },
            ["dw_bitter.n"]   = new[] { "SIN RED", "NO NET" },
            ["dw_bitter.d"]   = new[] { "Si no llevas granadas, recibes un 10% más de daño.", "If you carry no grenades, you take 10% more damage." },
            ["dw_tangled.n"]  = new[] { "ENREDADO", "ENTANGLED" },
            ["dw_tangled.d"]  = new[] { "Te frenas un 40% dentro de tus propias telarañas.", "You slow down by 40% inside your own webs." },
            ["dw_greasy.n"]   = new[] { "SEDA PEGAJOSA", "STICKY SILK" },
            ["dw_greasy.d"]   = new[] { "Preparar y lanzar granadas es un 25% más lento.", "Readying and throwing grenades is 25% slower." },
            ["dw_hungry.n"]   = new[] { "HUEVOS FRÁGILES", "FRAGILE EGGS" },
            ["dw_hungry.d"]   = new[] { "Un disparo en el torso tiene un 25% de romper una de las granadas que llevas (la pierdes).", "A shot to the torso has a 25% chance to break one of the grenades you carry (you lose it)." },
            ["notice.dw_hungry"] = new[] { "Se ha roto tu {0}", "Your {0} broke" },
            ["dw_dry.n"]      = new[] { "TELARAÑA ENREDOSA", "TANGLING WEB" },
            ["dw_dry.d"]      = new[] { "Tus propias telarañas te frenan un 80% (el doble que Enredado).", "Your own webs slow you by 80% (twice as much as Entangled)." },

            ["st_rush.n"]     = new[] { "HORA PUNTA", "RUSH HOUR" },
            ["st_rush.d"]     = new[] { "Tras una baja, recuperas toda la estamina de las piernas.", "After a kill, your leg stamina refills completely." },
            ["st_rush.c"]     = new[] { "Haz 3 bajas en menos de 3 minutos con Stamin-Up activa.", "Get 3 kills in under 3 minutes with Stamin-Up active." },
            ["st_shooter.n"]  = new[] { "TIRADOR EN CARRERA", "SPRINT SHOOTER" },
            ["st_shooter.d"]  = new[] { "Las animaciones del arma van siempre x1.75 más rápido (las recargas no cambian).", "Weapon animations always run 1.75x faster (reloads are not affected)." },
            ["st_shooter.c"]  = new[] { "Encuentra 15 Hot Rod, RatCola o Max Energy en raid.", "Find 15 Hot Rod, RatCola or Max Energy in raid." },
            ["st_wind.n"]     = new[] { "SEGUNDO ALIENTO", "SECOND WIND" },
            ["st_wind.d"]     = new[] { "Cada vez que tu salud esté en rojo: 5 s de estamina infinita e inmunidad al dolor, sin cooldown. Aún puedes recibir daño.", "Every time your health is in the red: 5 s of infinite stamina and pain immunity, no cooldown. You can still take damage." },
            ["st_wind.c"]     = new[] { "Quédate sin estamina 5 veces con Stamin-Up activa.", "Run out of stamina 5 times with Stamin-Up active." },
            ["st_feet.n"]     = new[] { "PIES LIGEROS", "LIGHT FEET" },
            ["st_feet.d"]     = new[] { "Llevas un 10% menos de peso.", "You carry 10% less weight." },
            ["st_feet.c"]     = new[] { "Corre 5 km en total con Stamin-Up activa.", "Sprint 5 km in total with Stamin-Up active." },
            ["st_recover.n"]  = new[] { "RECUPERACIÓN RÁPIDA", "QUICK RECOVERY" },
            ["st_recover.d"]  = new[] { "La estamina se recupera un 25% más rápido.", "Stamina recovers 25% faster." },
            ["st_recover.c"]  = new[] { "Mata a 10 enemigos menos de 3 s después de esprintar, con Stamin-Up activa.", "Kill 10 enemies within 3 s of sprinting, with Stamin-Up active." },
            ["st_land.n"]     = new[] { "ATERRIZAJE SUAVE", "SOFT LANDING" },
            ["st_land.d"]     = new[] { "El daño por caída se reduce un 30%.", "Fall damage is reduced by 30%." },
            ["st_land.c"]     = new[] { "Mata a 3 enemigos que estén al menos 2 m por debajo de ti, con Stamin-Up activa.", "Kill 3 enemies who are at least 2 m below you, with Stamin-Up active." },
            ["st_slot.n"]     = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["st_slot.d"]     = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["st_slot.c"]     = new[] { "Mata a 1 boss con Stamin-Up activa.", "Kill 1 boss with Stamin-Up active." },

            ["dt_boots.n"]    = new[] { "BOTAS RUIDOSAS", "LOUD BOOTS" },
            ["dt_boots.d"]    = new[] { "Correr hace ruido: los bots te oyen desde 25 m.", "Sprinting is noisy: bots can hear you from 25 m." },
            ["dt_burn.n"]     = new[] { "AGOTAMIENTO", "BURNOUT" },
            ["dt_burn.d"]     = new[] { "Con menos del 30% de estamina, te recuperas a la mitad de velocidad.", "Below 30% stamina, you recover at half speed." },
            ["dt_jelly.n"]    = new[] { "PIERNAS DE GELATINA", "JELLY LEGS" },
            ["dt_jelly.d"]    = new[] { "Tras correr 5 s seguidos, el balanceo del arma sube un 25% durante 6 s.", "After sprinting 5 s in a row, weapon sway rises by 25% for 6 s." },
            ["dt_hungry.n"]   = new[] { "INQUIETO", "RESTLESS" },
            ["dt_hungry.d"]   = new[] { "Si estás quieto más de 8 s, la estamina deja de recuperarse.", "Stand still for more than 8 s and your stamina stops recovering." },
            ["dt_cramps.n"]   = new[] { "CALAMBRES", "CRAMPS" },
            ["dt_cramps.d"]   = new[] { "Al dejar de correr, la estamina tarda 2 s más en recuperarse.", "After you stop sprinting, stamina takes 2 s longer to start recovering." },
            ["dt_sweaty.n"]   = new[] { "INERCIA", "MOMENTUM" },
            ["dt_sweaty.d"]   = new[] { "Al dejar de correr, no puedes apuntar durante 0.4 s.", "After you stop sprinting, you can't aim for 0.4 s." },
        };
    }

    // ============================================================================================
    //  Consultas sobre las definiciones de mejoras/desventajas (los datos estan en PerkDefs.cs)
    // ============================================================================================
    internal static class Data
    {
        // posicion (col,fila) de cada paso del camino en serpiente 3x3 (comun a todas las perks)
        internal static readonly int[][] Snake =
        {
            null, new[] {0,0}, new[] {1,0}, new[] {2,0}, new[] {2,1}, new[] {1,1}, new[] {0,1}, new[] {0,2}, new[] {1,2}, new[] {2,2},
        };

        internal static AugDef Aug(string id) { return Perks.All.SelectMany(p => p.Augs).FirstOrDefault(a => a.Id == id); }

        internal static bool IsDrawback(string id)
        {
            return Perks.All.Any(p => p.DrbMajor.Contains(id) || p.DrbMinor.Contains(id));
        }

        // desventaja ligada a una mejora (null para el desbloqueo especial)
        internal static string PairOf(string augId)
        {
            var a = Aug(augId);
            if (a == null || a.Kind == NodeKind.Special) return null;
            var perk = Perks.Get(a.Perk);
            int i = perk.AugsOf(a.Kind).ToList().FindIndex(x => x.Id == augId);
            return a.Kind == NodeKind.Major ? perk.DrbMajor[i] : perk.DrbMinor[i];
        }

        // mejora que desbloquea una desventaja
        internal static string AugOf(string drbId)
        {
            var perk = Perks.OfNode(drbId);
            if (perk == null) return null;
            int i = Array.IndexOf(perk.DrbMajor, drbId);
            if (i >= 0) return perk.AugsOf(NodeKind.Major).ElementAt(i).Id;
            i = Array.IndexOf(perk.DrbMinor, drbId);
            return i >= 0 ? perk.AugsOf(NodeKind.Minor).ElementAt(i).Id : null;
        }

        // progreso de un desafio con su unidad: 6300 m -> "6.3 km", 125 s -> "2:05"
        internal static string Prog(AugDef c, int v)
        {
            switch (c.Unit)
            {
                case ProgUnit.Meters: return v == 0 || v >= 1000 ? (v / 1000f).ToString(v % 1000 == 0 ? "0" : "0.0") + " km" : v + " m";
                case ProgUnit.Seconds: return (v / 60) + ":" + (v % 60).ToString("00");
                default: return v.ToString();
            }
        }

        internal static string Name(string id) { return L.T(id + ".n"); }
        internal static string Desc(string id) { return L.T(id + ".d"); }
        internal static string Chal(string id) { return L.T(id + ".c"); }
    }
}
