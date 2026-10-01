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
            ["notice.level.title"] = new[] { "SPEED COLA · NIVEL {0}", "SPEED COLA · LEVEL {0}" },
            ["notice.level.sub"]   = new[] { "Nuevos desafíos disponibles", "New challenges available" },
            ["notice.chal.title"]  = new[] { "DESAFÍO COMPLETADO", "CHALLENGE COMPLETE" },
            ["notice.chal.sub"]    = new[] { "{0} desbloqueada", "{0} unlocked" },
            ["notice.ach.title"]   = new[] { "LOGRO DESBLOQUEADO", "ACHIEVEMENT UNLOCKED" },

            ["notice.over.sub"]    = new[] { "El cargador se vació", "The magazine emptied" },

            // ---- panel en raid (menu ESC) ----
            ["raid.title"]      = new[] { "SPEED COLA", "SPEED COLA" },
            ["raid.level"]      = new[] { "NIVEL {0}", "LEVEL {0}" },
            ["raid.xpraid"]     = new[] { "+{0} XP esta raid (x1.25)", "+{0} XP this raid (x1.25)" },
            ["raid.drink"]      = new[] { "Bebe Speed Cola en la raid para ganar XP de la perk.", "Drink Speed Cola in raid to earn perk XP." },
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
        };
    }

    // ============================================================================================
    //  Definiciones de Speed Cola (mejoras, desventajas y desafios)
    // ============================================================================================
    internal enum NodeKind { Major, Minor, Special }

    internal class AugDef
    {
        public string Id;
        public NodeKind Kind;
        public int Seq;        // orden en el camino en serpiente (1..9)
        public int Lvl;        // nivel de la perk que revela el desafio
        public int Goal;       // objetivo del desafio
    }

    internal static class Data
    {
        // Cada mejora desbloquea la desventaja del mismo tipo y misma posicion en su lista.
        internal static readonly AugDef[] Augs =
        {
            new AugDef{Id="a_quick",  Kind=NodeKind.Major,   Seq=1, Lvl=1, Goal=3},
            new AugDef{Id="a_bolt",   Kind=NodeKind.Major,   Seq=3, Lvl=2, Goal=5},
            new AugDef{Id="a_rush",   Kind=NodeKind.Major,   Seq=7, Lvl=4, Goal=4},
            new AugDef{Id="a_steady", Kind=NodeKind.Minor,   Seq=2, Lvl=1, Goal=4},
            new AugDef{Id="a_check",  Kind=NodeKind.Minor,   Seq=5, Lvl=3, Goal=20},
            new AugDef{Id="a_mags",   Kind=NodeKind.Minor,   Seq=4, Lvl=2, Goal=20},
            new AugDef{Id="a_count",  Kind=NodeKind.Minor,   Seq=6, Lvl=3, Goal=15},
            new AugDef{Id="s_slot",   Kind=NodeKind.Special, Seq=9, Lvl=5, Goal=1},
        };

        internal static readonly string[] DrbMajor = { "d_jitter", "d_over", "d_crash" };
        internal static readonly string[] DrbMinor = { "d_dry", "d_slurp", "d_jam", "d_sweet" };

        // posicion (col,fila) de cada paso del camino en serpiente 3x3
        internal static readonly int[][] Snake =
        {
            null, new[] {0,0}, new[] {1,0}, new[] {2,0}, new[] {2,1}, new[] {1,1}, new[] {0,1}, new[] {0,2}, new[] {1,2}, new[] {2,2},
        };

        internal static AugDef Aug(string id) { return Augs.FirstOrDefault(a => a.Id == id); }
        internal static IEnumerable<AugDef> AugsOf(NodeKind k) { return Augs.Where(a => a.Kind == k); }

        internal static bool IsDrawback(string id) { return id.StartsWith("d_"); }

        // desventaja ligada a una mejora (null para el desbloqueo especial)
        internal static string PairOf(string augId)
        {
            var a = Aug(augId);
            if (a == null || a.Kind == NodeKind.Special) return null;
            int i = AugsOf(a.Kind).ToList().FindIndex(x => x.Id == augId);
            return a.Kind == NodeKind.Major ? DrbMajor[i] : DrbMinor[i];
        }

        // mejora que desbloquea una desventaja
        internal static string AugOf(string drbId)
        {
            int i = Array.IndexOf(DrbMajor, drbId);
            if (i >= 0) return AugsOf(NodeKind.Major).ElementAt(i).Id;
            i = Array.IndexOf(DrbMinor, drbId);
            return i >= 0 ? AugsOf(NodeKind.Minor).ElementAt(i).Id : null;
        }

        internal static string Name(string id) { return L.T(id + ".n"); }
        internal static string Desc(string id) { return L.T(id + ".d"); }
        internal static string Chal(string id) { return L.T(id + ".c"); }
    }
}
