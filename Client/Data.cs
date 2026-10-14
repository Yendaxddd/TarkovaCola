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

            // ---- Juggernog / PHD Flopper / Widow's Wine ----
            ["perk.juggernog"]      = new[] { "Juggernog", "Juggernog" },
            ["perk.juggernog.desc"] = new[] {
                "Daño recibido -33% el resto de la raid. Sube de nivel para desbloquear desafíos; cada desafío desbloquea una mejora y su desventaja.",
                "Damage taken -33% for the rest of the raid. Level up to unlock challenges; each challenge unlocks an augment and its drawback." },
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

            // Eternal Perk y powerup de perk aleatoria
            ["eternal.title"]     = new[] { "ETERNAL PERK", "ETERNAL PERK" },
            ["eternal.sub.drb"]   = new[] { "{0}: sin la desventaja {1}", "{0}: drawback {1} removed" },
            ["eternal.sub.none"]  = new[] { "{0}: sin desventajas que quitar", "{0}: no drawbacks to remove" },
            ["eternal.promise"]   = new[] { "Si mueres, la botella vuelve por correo", "If you die, the bottle comes back by mail" },
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
            ["jg_skull.n"]    = new[] { "GORRO DE CALAVERA", "SKULL CAP" },
            ["jg_skull.d"]    = new[] { "Más probabilidad de que las balas reboten en tu casco.", "Higher chance that bullets ricochet off your helmet." },
            ["jg_skull.c"]    = new[] { "Recibe 1,500 de daño con Juggernog activa.", "Take 1,500 damage with Juggernog active." },
            ["jg_thick.n"]    = new[] { "PIEL GRUESA", "THICK SKIN" },
            ["jg_thick.d"]    = new[] { "Sangras menos, pero los huesos se rompen más fácil.", "You bleed less, but your bones break more easily." },
            ["jg_thick.c"]    = new[] { "Arregla 5 sangrados pesados con Juggernog activa.", "Fix 5 heavy bleeds with Juggernog active." },
            ["jg_bones.n"]    = new[] { "HUESOS PESADOS", "HEAVY BONES" },
            ["jg_bones.d"]    = new[] { "Te fracturas menos, pero sangras más fácil.", "Your bones break less, but you bleed more easily." },
            ["jg_bones.c"]    = new[] { "Arregla 5 fracturas con Juggernog activa.", "Fix 5 fractures with Juggernog active." },
            ["jg_patch.n"]    = new[] { "PARCHE RÁPIDO", "QUICK PATCH" },
            ["jg_patch.d"]    = new[] { "Las animaciones de curación van el doble de rápido.", "Healing animations are twice as fast." },
            ["jg_patch.c"]    = new[] { "Usa 10 items médicos con Juggernog activa.", "Use 10 medical items with Juggernog active." },
            ["jg_plates.n"]   = new[] { "PLACAS DE HUESO", "BONE PLATING" },
            ["jg_plates.d"]   = new[] { "Tus placas de armadura aguantan el doble de daño.", "Your armor plates take twice the damage." },
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
            ["dj_dead.n"]     = new[] { "PESO MUERTO", "DEAD WEIGHT" },
            ["dj_dead.d"]     = new[] { "Correr gasta un 20% más de estamina.", "Sprinting burns 20% more stamina." },
            ["dj_sluggish.n"] = new[] { "REFLEJOS LENTOS", "SLUGGISH REFLEXES" },
            ["dj_sluggish.d"] = new[] { "Sacar y cambiar de arma es un 20% más lento.", "Drawing and swapping weapons is 20% slower." },
            ["dj_metab.n"]    = new[] { "METABOLISMO LENTO", "SLOW METABOLISM" },
            ["dj_metab.d"]    = new[] { "El hambre y la sed bajan un 20% más rápido.", "Hunger and thirst drop 20% faster." },
            ["dj_chew.n"]     = new[] { "MASTICAR LENTO", "SLOW CHEWER" },
            ["dj_chew.d"]     = new[] { "Comer y beber tarda un 30% más.", "Eating and drinking takes 30% longer." },

            // ---- PHD Flopper ----
            ["pf_absorb.n"]   = new[] { "ABSORBEDOR DE EXPLOSIONES", "BLAST ABSORBER" },
            ["pf_absorb.d"]   = new[] { "Curas el 25% del daño de explosión que causas.", "You heal for 25% of the explosion damage you deal." },
            ["pf_absorb.c"]   = new[] { "Causa 500 de daño con explosiones con PHD Flopper activa.", "Deal 500 explosion damage with PHD Flopper active." },
            ["pf_shock.n"]    = new[] { "ONDA EXPANSIVA", "SHOCKWAVE" },
            ["pf_shock.d"]    = new[] { "Tus granadas tienen el doble de radio y la mitad de tiempo de mecha.", "Your grenades have double the radius and half the fuse time." },
            ["pf_shock.c"]    = new[] { "Mata a 5 enemigos con granadas con PHD Flopper activa.", "Kill 5 enemies with grenades with PHD Flopper active." },
            ["pf_soles.n"]    = new[] { "SUELAS SUAVES", "SOFT SOLES" },
            ["pf_soles.d"]    = new[] { "Aterrizar tras una caída no te frena.", "Landing after a fall doesn't slow you down." },
            ["pf_soles.c"]    = new[] { "Recorre 2 km con PHD Flopper activa.", "Travel 2 km with PHD Flopper active." },
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
            ["dp_stagger.n"]  = new[] { "TAMBALEO", "STAGGER" },
            ["dp_stagger.d"]  = new[] { "Aterrizar de una caída de 3 m o más te quita 40 de estamina.", "Landing from a fall of 3 m or more costs you 40 stamina." },
            ["dp_lazy.n"]     = new[] { "PIERNAS PEREZOSAS", "LAZY LEGS" },
            ["dp_lazy.d"]     = new[] { "La estamina de piernas baja un 15%.", "Leg stamina is reduced by 15%." },
            ["dp_pockets.n"]  = new[] { "BOLSILLOS PESADOS", "HEAVY POCKETS" },
            ["dp_pockets.d"]  = new[] { "Llevas un 10% más de peso.", "You carry 10% more weight." },
            ["dp_stomp.n"]    = new[] { "PISADAS FUERTES", "HARD STOMP" },
            ["dp_stomp.d"]    = new[] { "Tus pasos se oyen a 15 m.", "Your footsteps are audible from 15 m." },

            // ---- Widow's Wine ----
            ["ww_spin.n"]     = new[] { "HILERA", "SPINNERET" },
            ["ww_spin.d"]     = new[] { "La telaraña tiene el doble de radio y dura 2 s más.", "The web has double the radius and lasts 2 s longer." },
            ["ww_spin.c"]     = new[] { "Atrapa a 25 enemigos con Widow's Wine activa.", "Web 25 enemies with Widow's Wine active." },
            ["ww_fuse.n"]     = new[] { "MECHA SEGURA", "SAFE FUSE" },
            ["ww_fuse.d"]     = new[] { "Las explosiones no dañan tu armadura.", "Explosions don't damage your armor." },
            ["ww_fuse.c"]     = new[] { "Atrapa a 3 enemigos con una sola granada.", "Web 3 enemies with a single grenade." },
            ["ww_burst.n"]    = new[] { "RÁFAGA DE TELARAÑAS", "WEB BURST" },
            ["ww_burst.d"]    = new[] { "Si recibes 100 de daño en menos de 3 s, gastas una granada del inventario: los enemigos cercanos quedan atrapados y aturdidos (1 vez cada 5 min).", "If you take 100 damage in under 3 s, a grenade from your inventory is spent: nearby enemies are webbed and stunned (once every 5 min)." },
            ["ww_burst.c"]    = new[] { "Mata a 3 enemigos con granadas con Widow's Wine activa.", "Kill 3 enemies with grenades with Widow's Wine active." },
            ["ww_venom.n"]    = new[] { "MORDIDA VENENOSA", "VENOMOUS BITE" },
            ["ww_venom.d"]    = new[] { "Los enemigos atrapados en la telaraña también quedan aturdidos.", "Enemies caught in the web are also stunned." },
            ["ww_venom.c"]    = new[] { "Mata a 5 enemigos atrapados con Widow's Wine activa.", "Kill 5 webbed enemies with Widow's Wine active." },
            ["ww_quick.n"]    = new[] { "HILANDERO RÁPIDO", "QUICK SPINNER" },
            ["ww_quick.d"]    = new[] { "Preparas y lanzas las granadas un 40% más rápido.", "You ready and throw grenades 40% faster." },
            ["ww_quick.c"]    = new[] { "Lanza 10 granadas con Widow's Wine activa.", "Throw 10 grenades with Widow's Wine active." },
            ["ww_sticky.n"]   = new[] { "SUELO PEGAJOSO", "STICKY FLOOR" },
            ["ww_sticky.d"]   = new[] { "Puedes lanzar las granadas un 35% más lejos.", "You can throw grenades 35% farther." },
            ["ww_sticky.c"]   = new[] { "Atrapa a 15 enemigos con Widow's Wine activa.", "Web 15 enemies with Widow's Wine active." },
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
            ["dw_bitter.n"]   = new[] { "VINO AMARGO", "BITTER WINE" },
            ["dw_bitter.d"]   = new[] { "Correr gasta un 15% más de estamina.", "Sprinting burns 15% more stamina." },
            ["dw_tangled.n"]  = new[] { "ENREDADO", "ENTANGLED" },
            ["dw_tangled.d"]  = new[] { "Te frenas un 40% dentro de tus propias telarañas.", "You slow down by 40% inside your own webs." },
            ["dw_greasy.n"]   = new[] { "DEDOS GRASIENTOS", "GREASY FINGERS" },
            ["dw_greasy.d"]   = new[] { "Sacar y cambiar de arma es un 20% más lento.", "Drawing and swapping weapons is 20% slower." },
            ["dw_hungry.n"]   = new[] { "ARAÑA HAMBRIENTA", "HUNGRY SPIDER" },
            ["dw_hungry.d"]   = new[] { "La energía baja un 20% más rápido.", "Energy drops 20% faster." },
            ["dw_dry.n"]      = new[] { "GARGANTA SECA", "DRY THROAT" },
            ["dw_dry.d"]      = new[] { "La hidratación baja un 20% más rápido.", "Hydration drops 20% faster." },

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
