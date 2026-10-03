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
            // ---- pantalla: general ----
            ["augments"]        = new[] { "MEJORAS", "AUGMENTS" },
            ["title.progress"]  = new[] { "PROGRESO", "PROGRESS" },
            ["title.equip"]     = new[] { "EQUIPAR", "EQUIP" },
            ["nav.progress"]    = new[] { "PROGRESO", "PROGRESS" },
            ["nav.equip"]       = new[] { "EQUIPAR", "EQUIP" },
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
            ["tile.unlocks"]    = new[] { "Desbloquea {0}", "Unlocks {0}" },
            ["slot.major"]      = new[] { "PRINCIPAL", "MAJOR" },
            ["slot.minor"]      = new[] { "SECUNDARIA", "MINOR" },
            ["slot.level5"]     = new[] { "NIVEL 5", "LEVEL 5" },
            ["equip.augments"]  = new[] { "MEJORAS", "AUGMENTS" },
            ["equip.drawbacks"] = new[] { "DESVENTAJAS", "DRAWBACKS" },
            ["equip.rules"]     = new[]
            {
                "Mejoras: 1 principal + {0} secundaria(s).\nDesventajas: 1 principal + 1 secundaria.\nNo puedes llevar una mejora principal sin su desventaja principal.",
                "Augments: 1 major + {0} minor.\nDrawbacks: 1 major + 1 minor.\nYou can't carry a major augment without a major drawback.",
            },
            ["xp.max"]          = new[] { "NIVEL MÁXIMO", "MAX LEVEL" },
            ["xp.mult"]         =new[] { "MULTIPLICADOR DE XP", "XP MULTIPLIER" },
            ["xp.note"]         = new[] { "+25% de XP mientras la perk está activa en la raid", "+25% XP while the perk is active in raid" },
            ["err.needdrb"]     = new[] { "Equipa primero una desventaja principal.", "Equip a major drawback first." },
            ["toast.drbremoved"]= new[] { "Sin desventaja principal, la mejora principal también se desequipa.", "Without a major drawback, the major augment is unequipped too." },
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
            ["raid.xpraid"]     = new[] { "+{0} XP esta raid (x1.25)", "+{0} XP this raid (x1.25)" },
            ["raid.drink"]      = new[] { "Bebe {0} en la raid para ganar XP de la perk.", "Drink {0} in raid to earn perk XP." },
            ["raid.pending"]    = new[] { "DESAFÍOS PENDIENTES", "PENDING CHALLENGES" },
            ["raid.none"]       = new[] { "Sin desafíos activos. Sube de nivel para revelar más.", "No active challenges. Level up to reveal more." },

            // ---- mejoras: nombre (.n), efecto (.d), desafio (.c) ----
            ["a_quick.n"]  = new[] { "MANOS RÁPIDAS", "QUICK HANDS" },
            ["a_quick.d"]  = new[] { "Sacar y cambiar de arma es un 50% más rápido.", "Drawing and swapping weapons is 50% faster." },
            ["a_quick.c"]  = new[] { "Mata a 3 enemigos poco después de cambiar de arma, con Speed Cola activa.", "Kill 3 enemies shortly after swapping weapons, with Speed Cola active." },

            ["a_bolt.n"]   = new[] { "CERROJO VELOZ", "BOLT RUNNER" },
            ["a_bolt.d"]   = new[] { "Si tu arma se encasquilla, se desencasquilla sola al guardarla.", "If your weapon jams, it clears itself when you put it away." },
            ["a_bolt.c"]   = new[] { "Desencasquilla tu arma 5 veces con Speed Cola activa.", "Clear a weapon jam 5 times with Speed Cola active." },

            ["a_rush.n"]   = new[] { "SUBIDÓN DE AZÚCAR", "SUGAR RUSH" },
            ["a_rush.d"]   = new[] { "El bonus de recarga de Speed Cola sube de x1.5 a x2.0.", "Speed Cola's reload bonus rises from x1.5 to x2.0." },
            ["a_rush.c"]   = new[] { "Mata a 4 PMC.", "Kill 4 PMCs." },

            ["a_steady.n"] = new[] { "PULSO FIRME", "STEADY FINGERS" },
            ["a_steady.d"] = new[] { "Tus brazos reciben un 30% menos de daño.", "Your arms take 30% less damage." },
            ["a_steady.c"] = new[] { "Ten los brazos rotos 4 veces con Speed Cola activa.", "Get your arms broken 4 times with Speed Cola active." },

            ["a_check.n"]  = new[] { "REVISIÓN RÁPIDA", "CHAMBER CHECK" },
            ["a_check.d"]  = new[] { "Revisar cargadores en el inventario es un 75% más rápido.", "Checking magazines in your inventory is 75% faster." },
            ["a_check.c"]  = new[] { "Revisa el cargador de tu arma 20 veces en raid.", "Check your weapon's magazine 20 times in raid." },

            ["a_mags.n"]   = new[] { "MALABARISTA", "MAG JUGGLER" },
            ["a_mags.d"]   = new[] { "Los cargadores de tu arma principal se cargan un 50% más rápido en el inventario.", "Magazines for your primary weapon load 50% faster in your inventory." },
            ["a_mags.c"]   = new[] { "Carga 20 cargadores en raid con Speed Cola activa.", "Load 20 magazines in raid with Speed Cola active." },

            ["a_count.n"]  = new[] { "CONTEO DE BALAS", "ROUND COUNTER" },
            ["a_count.d"]  = new[] { "Los cargadores muestran su munición exacta, los hayas revisado o no.", "Magazines show their exact ammo count, checked or not." },
            ["a_count.c"]  = new[] { "Revisa cargadores dentro del inventario 15 veces.", "Check magazines in your inventory 15 times." },

            ["s_slot.n"]   = new[] { "RANURA EXTRA", "EXTRA SLOT" },
            ["s_slot.d"]   = new[] { "Podrás llevar 2 mejoras secundarias (por defecto solo 1).", "You can carry 2 minor augments (1 by default)." },
            ["s_slot.c"]   = new[] { "Mata a 1 boss con Speed Cola activa.", "Kill 1 boss with Speed Cola active." },

            // ---- desventajas ----
            ["d_jitter.n"] = new[] { "MANOS TEMBLOROSAS", "JITTERY HANDS" },
            ["d_jitter.d"] = new[] { "Si un brazo tiene un efecto no saludable (fractura, destruido, sangrado leve/fuerte), el balanceo del arma sube un 25%.", "If an arm has an unhealthy effect (fracture, destroyed, light/heavy bleed), weapon sway rises by 25%." },
            ["d_over.n"]   = new[] { "SOBREPRESIÓN", "OVERPRESSURE" },
            ["d_over.d"]   = new[] { "Cada 4.º cargador pierde todas sus balas al recargar (vuelven a tu inventario).", "Every 4th magazine loses all its rounds when reloaded (they return to your inventory)." },
            ["d_crash.n"]  = new[] { "BAJÓN DE AZÚCAR", "SUGAR CRASH" },
            ["d_crash.d"]  = new[] { "Cada 2 min sin matar a un enemigo, tu sed se multiplica (máx. x2). Matar la reinicia.", "Every 2 min without killing an enemy, your thirst multiplier rises (max x2). A kill resets it." },
            ["d_dry.n"]    = new[] { "BOCA SECA", "DRY MOUTH" },
            ["d_dry.d"]    = new[] { "La hidratación baja un 20% más rápido.", "Hydration drains 20% faster." },
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

            ["st_rush.n"]     = new[] { "HORA PUNTA", "RUSH HOUR" },
            ["st_rush.d"]     = new[] { "Tras una baja, recuperas toda la estamina de las piernas.", "After a kill, your leg stamina refills completely." },
            ["st_rush.c"]     = new[] { "Haz 3 bajas en menos de 3 minutos con Stamin-Up activa.", "Get 3 kills in under 3 minutes with Stamin-Up active." },
            ["st_shooter.n"]  = new[] { "TIRADOR EN CARRERA", "SPRINT SHOOTER" },
            ["st_shooter.d"]  = new[] { "Al dejar de correr, el arma queda lista mucho más rápido (animaciones x1.75 durante 1.2 s).", "Right after you stop sprinting, your weapon is ready much faster (animations x1.75 for 1.2 s)." },
            ["st_shooter.c"]  = new[] { "Encuentra 15 Hot Rod, RatCola o Max Energy en raid.", "Find 15 Hot Rod, RatCola or Max Energy in raid." },
            ["st_wind.n"]     = new[] { "SEGUNDO ALIENTO", "SECOND WIND" },
            ["st_wind.d"]     = new[] { "Mientras tu salud esté en rojo: estamina infinita e inmunidad al dolor. Aún puedes recibir daño.", "While your health is in the red: infinite stamina and pain immunity. You can still take damage." },
            ["st_wind.c"]     = new[] { "Quédate sin estamina 5 veces con Stamin-Up activa.", "Run out of stamina 5 times with Stamin-Up active." },
            ["st_feet.n"]     = new[] { "PIES LIGEROS", "LIGHT FEET" },
            ["st_feet.d"]     = new[] { "Llevas un 10% menos de peso.", "You carry 10% less weight." },
            ["st_feet.c"]     = new[] { "Corre 5 km en total con Stamin-Up activa.", "Sprint 5 km in total with Stamin-Up active." },
            ["st_recover.n"]  = new[] { "RECUPERACIÓN RÁPIDA", "QUICK RECOVERY" },
            ["st_recover.d"]  = new[] { "La estamina se recupera un 25% más rápido.", "Stamina recovers 25% faster." },
            ["st_recover.c"]  = new[] { "Corre 10 km en total con Stamin-Up activa.", "Sprint 10 km in total with Stamin-Up active." },
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
            ["dt_hungry.n"]   = new[] { "PIERNAS HAMBRIENTAS", "HUNGRY LEGS" },
            ["dt_hungry.d"]   = new[] { "Corriendo gastas un 50% más de energía.", "You burn 50% more energy while sprinting." },
            ["dt_cramps.n"]   = new[] { "CALAMBRES", "CRAMPS" },
            ["dt_cramps.d"]   = new[] { "Al dejar de correr, la estamina tarda 2 s más en recuperarse.", "After you stop sprinting, stamina takes 2 s longer to start recovering." },
            ["dt_sweaty.n"]   = new[] { "SUDOROSO", "SWEATY" },
            ["dt_sweaty.d"]   = new[] { "Corriendo gastas un 50% más de hidratación.", "You burn 50% more hydration while sprinting." },
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

        internal static string Name(string id) { return L.T(id + ".n"); }
        internal static string Desc(string id) { return L.T(id + ".d"); }
        internal static string Chal(string id) { return L.T(id + ".c"); }
    }
}
