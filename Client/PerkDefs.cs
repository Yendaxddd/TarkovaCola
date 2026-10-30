using System;
using System.Collections.Generic;
using System.Linq;
using EFT.HealthSystem;

namespace TarkovaCola.Client
{
    internal enum NodeKind { Major, Minor, Special }

    // como se muestra el progreso de un desafio: cuenta normal, metros (se ven en km) o segundos (se ven como m:ss)
    internal enum ProgUnit { None, Meters, Seconds }

    internal class AugDef
    {
        public string Id;
        public string Perk;    // perk a la que pertenece (se rellena al registrarla)
        public NodeKind Kind;
        public int Seq;        // orden en el camino en serpiente (1..9)
        public int Lvl;        // nivel de la perk que revela el desafio
        public int Goal;       // objetivo del desafio
        public ProgUnit Unit;  // unidad del progreso (solo para mostrarlo)
    }

    // Una perk: su item, sus recursos y su arbol de mejoras. Los ids de mejoras/desventajas deben ser unicos entre perks.
    // El estado guardado en el servidor ya esta organizado por Id de perk (xp, prog, researched, equipped).
    internal class PerkDef
    {
        public string Id;                  // clave de estado y de textos ("perk.<Id>")
        public string ItemTpl;             // id del item (lo mismo que Server/Mod.cs)
        public string IconFile;            // icono (HUD, Research, panel del ESC) dentro de assets/
        public string JingleFile;          // cancion al beberla
        public string HandBundle;          // modelo en la mano (null = se deja el de la lata normal)
        public float HandRefHeight = 0.122f;   // altura en metros del modelo de la mano, para escalarlo
        public AugDef[] Augs;
        public string[] DrbMajor, DrbMinor;     // desventaja i = la ligada a la mejora i de su tipo
        public Func<string> DrinkNotice;        // subtitulo del aviso al beberla (segun la fuerza con que esta activa)
        public Func<float, string> EffectText;  // texto del efecto base segun la fuerza (1 = completo, Perks.ModifierPower = modificador de Classic)
        public Action<ActiveHealthController> OnDrink;   // efectos propios de la perk al beberla

        // Una perk solo sale en Research y en el panel del ESC cuando ya tiene su arbol de mejoras. Mientras no lo tenga se puede beber
        // (efecto base, cancion, modelo en mano) y acumula XP, pero no hay nada que equipar ni desafios que mostrar.
        public bool HasTree { get { return Augs.Length > 0; } }

        public string Name { get { return L.T("perk." + Id); } }
        public string Desc { get { return L.T("perk." + Id + ".desc"); } }
        public IEnumerable<AugDef> AugsOf(NodeKind k) { return Augs.Where(a => a.Kind == k); }
        public bool Has(string nodeId) { return Augs.Any(a => a.Id == nodeId) || DrbMajor.Contains(nodeId) || DrbMinor.Contains(nodeId); }
    }

    internal static class Perks
    {
        internal const string SpeedColaId = "6a1c00000000000000000c01";

        internal static readonly PerkDef SpeedCola = new PerkDef
        {
            Id = "speedcola",
            ItemTpl = SpeedColaId,
            IconFile = "speedcola_icon.png",
            JingleFile = "speedcola_jingle.mp3",
            HandBundle = "speedcola_hand.bundle",
            HandRefHeight = 0.13f,                      // lata de BO6
            Augs = new[]
            {
                new AugDef { Id = "a_quick",  Kind = NodeKind.Major,   Seq = 1, Lvl = 1, Goal = 3 },
                new AugDef { Id = "a_bolt",   Kind = NodeKind.Major,   Seq = 3, Lvl = 2, Goal = 10 },     // Recarga en caliente (3.0; antes Bolt Runner)
                new AugDef { Id = "a_rush",   Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 4 },
                new AugDef { Id = "a_steady", Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 4 },
                new AugDef { Id = "a_check",  Kind = NodeKind.Minor,   Seq = 5, Lvl = 3, Goal = 20 },
                new AugDef { Id = "a_mags",   Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 20 },
                new AugDef { Id = "a_count",  Kind = NodeKind.Minor,   Seq = 6, Lvl = 3, Goal = 15 },
                new AugDef { Id = "s_slot",   Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "d_jitter", "d_over", "d_crash" },
            DrbMinor = new[] { "d_dry", "d_slurp", "d_jam", "d_sweet" },
            DrinkNotice = SpeedColaDrink.Notice,
            EffectText = SpeedColaDrink.Effect,
            OnDrink = SpeedColaDrink.OnDrink,
        };

        internal static readonly PerkDef StaminUp = new PerkDef
        {
            Id = TarkovaCola.Client.StaminUp.Id,
            ItemTpl = TarkovaCola.Client.StaminUp.ItemTpl,
            IconFile = "staminup_icon.png",
            JingleFile = "staminup_jingle.mp3",
            HandBundle = "staminup_hand.bundle",
            HandRefHeight = 0.13f,
            Augs = new[]
            {
                new AugDef { Id = "st_rush",    Kind = NodeKind.Major,   Seq = 1, Lvl = 1, Goal = 1 },
                new AugDef { Id = "st_shooter", Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 15 },
                new AugDef { Id = "st_wind",    Kind = NodeKind.Major,   Seq = 5, Lvl = 3, Goal = 5 },
                new AugDef { Id = "st_feet",    Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 5000 , Unit = ProgUnit.Meters },
                new AugDef { Id = "st_recover", Kind = NodeKind.Minor,   Seq = 3, Lvl = 2, Goal = 10 },
                new AugDef { Id = "st_land",    Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 3 },
                new AugDef { Id = "st_lungs",   Kind = NodeKind.Minor,   Seq = 6, Lvl = 3, Goal = 20 },
                new AugDef { Id = "st_slot",    Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "dt_boots", "dt_burn", "dt_jelly" },
            DrbMinor = new[] { "dt_hungry", "dt_cramps", "dt_sweaty", "dt_chest" },
            DrinkNotice = TarkovaCola.Client.StaminUp.Notice,
            EffectText = TarkovaCola.Client.StaminUp.Effect,
            OnDrink = TarkovaCola.Client.StaminUp.OnDrink,
        };

        // ---- perks nuevas: de momento solo la base (item, modelo, icono, cancion y efecto base); el arbol de mejoras llega despues ----
        internal static readonly PerkDef Juggernog = new PerkDef
        {
            Id = TarkovaCola.Client.Juggernog.Id,
            ItemTpl = TarkovaCola.Client.Juggernog.ItemTpl,
            IconFile = "juggernog_icon.png",
            JingleFile = "juggernog_jingle.mp3",
            HandBundle = "juggernog_hand.bundle",
            HandRefHeight = 0.13f,
            // la desventaja i de cada tipo es la ligada a la mejora i de ese tipo (en el orden de esta lista)
            Augs = new[]
            {
                new AugDef { Id = "jg_patch",   Kind = NodeKind.Major,   Seq = 3, Lvl = 2, Goal = 10 },     // curaciones x2 de rapidas
                new AugDef { Id = "jg_plates",  Kind = NodeKind.Major,   Seq = 5, Lvl = 3, Goal = 60 },     // placas aguantan el doble
                new AugDef { Id = "jg_bastion", Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 5 },      // un golpe letal por raid te deja a 1 PV
                new AugDef { Id = "jg_skull",   Kind = NodeKind.Minor,   Seq = 1, Lvl = 1, Goal = 1500 },   // rebote de balas en el casco
                new AugDef { Id = "jg_thick",   Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 5 },      // menos sangrado, mas fracturas (desafio: arreglar 5 sangrados pesados)
                new AugDef { Id = "jg_bones",   Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 5 },      // menos fracturas, mas sangrado (desafio: arreglar 5 fracturas)
                new AugDef { Id = "jg_keep",    Kind = NodeKind.Minor,   Seq = 6, Lvl = 3, Goal = 15 },     // hambre y sed hacen menos dano
                new AugDef { Id = "jg_slot",    Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "dj_clank", "dj_plates", "dj_hands" },
            DrbMinor = new[] { "dj_dead", "dj_sluggish", "dj_metab", "dj_chew" },
            DrinkNotice = TarkovaCola.Client.Juggernog.Notice,
            EffectText = TarkovaCola.Client.Juggernog.Effect,
            OnDrink = TarkovaCola.Client.Juggernog.OnDrink,
        };

        internal static readonly PerkDef PhdFlopper = new PerkDef
        {
            Id = TarkovaCola.Client.PhdFlopper.Id,
            ItemTpl = TarkovaCola.Client.PhdFlopper.ItemTpl,
            IconFile = "phdflopper_icon.png",
            JingleFile = "phdflopper_jingle.mp3",
            HandBundle = "phdflopper_hand.bundle",
            HandRefHeight = 0.13f,
            Augs = new[]
            {
                new AugDef { Id = "pf_absorb",  Kind = NodeKind.Major,   Seq = 1, Lvl = 1, Goal = 500 },    // curas el 25% del dano de explosion que causas
                new AugDef { Id = "pf_shock",   Kind = NodeKind.Major,   Seq = 6, Lvl = 4, Goal = 5 },      // granadas: doble radio y mitad de mecha
                new AugDef { Id = "pf_drop",    Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 15 },     // Caida explosiva (3.0): caer de 3 m o mas explota a tu alrededor
                new AugDef { Id = "pf_soles",   Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 10 },   // aterrizar no te frena
                new AugDef { Id = "pf_stop",    Kind = NodeKind.Minor,   Seq = 3, Lvl = 2, Goal = 300 , Unit = ProgUnit.Seconds },    // quieto: estamina x2 de rapida
                new AugDef { Id = "pf_ears",    Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 5 },      // inmune a la contusion
                new AugDef { Id = "pf_terrain", Kind = NodeKind.Minor,   Seq = 5, Lvl = 3, Goal = 5000 , Unit = ProgUnit.Meters },   // inmune a alambre de puas y barro
                new AugDef { Id = "pf_slot",    Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "dp_concussed", "dp_throw", "dp_powder" },
            DrbMinor = new[] { "dp_stagger", "dp_lazy", "dp_pockets", "dp_stomp" },
            DrinkNotice = TarkovaCola.Client.PhdFlopper.Notice,
            EffectText = TarkovaCola.Client.PhdFlopper.Effect,
        };

        internal static readonly PerkDef WidowsWine = new PerkDef
        {
            Id = TarkovaCola.Client.WidowsWine.Id,
            ItemTpl = TarkovaCola.Client.WidowsWine.ItemTpl,
            IconFile = "widowswine_icon.png",
            JingleFile = "widowswine_jingle.mp3",
            HandBundle = "widowswine_hand.bundle",
            HandRefHeight = 0.13f,                      // lata generica de BO6
            Augs = new[]
            {
                new AugDef { Id = "ww_spin",    Kind = NodeKind.Major,   Seq = 4, Lvl = 3, Goal = 25 },     // telarana x2 de radio y +2 s
                new AugDef { Id = "ww_fuse",    Kind = NodeKind.Major,   Seq = 5, Lvl = 3, Goal = 1 },      // Nido (3.0): cada telaraña deja una trampa 20 s
                new AugDef { Id = "ww_burst",   Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 3 },      // 100 de dano en 3 s: gastas una granada y atrapas
                new AugDef { Id = "ww_venom",   Kind = NodeKind.Minor,   Seq = 1, Lvl = 1, Goal = 5 },      // los atrapados tambien quedan aturdidos
                new AugDef { Id = "ww_quick",   Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 10 },     // granadas mas rapidas de lanzar
                new AugDef { Id = "ww_sticky",  Kind = NodeKind.Minor,   Seq = 3, Lvl = 2, Goal = 10 },     // granadas mas lejos
                new AugDef { Id = "ww_bite",    Kind = NodeKind.Minor,   Seq = 6, Lvl = 4, Goal = 10 },     // si tu granada mata, te la devuelven
                new AugDef { Id = "ww_slot",    Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "dw_loud", "dw_brittle", "dw_bitter" },
            DrbMinor = new[] { "dw_tangled", "dw_greasy", "dw_hungry", "dw_dry" },
            DrinkNotice = TarkovaCola.Client.WidowsWine.Notice,
            EffectText = TarkovaCola.Client.WidowsWine.Effect,
        };

        internal static readonly PerkDef Deadshot = new PerkDef
        {
            Id = TarkovaCola.Client.Deadshot.Id,
            ItemTpl = TarkovaCola.Client.Deadshot.ItemTpl,
            IconFile = "deadshot_icon.png",
            JingleFile = "deadshot_jingle.wav",
            HandBundle = "deadshot_hand.bundle",
            HandRefHeight = 0.13f,
            Augs = new[]
            {
                new AugDef { Id = "dd_aim",     Kind = NodeKind.Major,   Seq = 3, Lvl = 2, Goal = 20 },     // Dead Aim: el arma no se balancea al moverte
                new AugDef { Id = "dd_pen",     Kind = NodeKind.Major,   Seq = 5, Lvl = 3, Goal = 15 },     // Incredible: +30% de penetracion
                new AugDef { Id = "dd_sky",     Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 1500 },   // To the Skies: retroceso -50%
                new AugDef { Id = "dd_quick",   Kind = NodeKind.Minor,   Seq = 1, Lvl = 1, Goal = 10 },     // apuntar +20%
                new AugDef { Id = "dd_steady",  Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 25 },     // aguantar la respiracion +40%
                new AugDef { Id = "dd_head",    Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 15 },     // tiro a la cabeza: +20% de estamina
                new AugDef { Id = "dd_still",   Kind = NodeKind.Minor,   Seq = 6, Lvl = 3, Goal = 10 },     // agachado/tumbado: balanceo -25%
                new AugDef { Id = "dd_slot",    Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "dd_d_tunnel", "dd_d_breath", "dd_d_shaky" },
            DrbMinor = new[] { "dd_d_strain", "dd_d_lime", "dd_d_sugar", "dd_d_squeak" },
            DrinkNotice = TarkovaCola.Client.Deadshot.Notice,
            EffectText = TarkovaCola.Client.Deadshot.Effect,
            OnDrink = TarkovaCola.Client.Deadshot.OnDrink,
        };

        internal static readonly PerkDef Bandolier = new PerkDef
        {
            Id = TarkovaCola.Client.Bandolier.Id,
            ItemTpl = TarkovaCola.Client.Bandolier.ItemTpl,
            IconFile = "bandolier_icon.png", JingleFile = "bandolier_jingle.mp3", HandBundle = "bandolier_hand.bundle", HandRefHeight = 0.13f,
            Augs = new[]
            {
                new AugDef { Id = "bb_sleek",  Kind = NodeKind.Major,   Seq = 3, Lvl = 2, Goal = 5 },      // Sleek: 20% de rellenar el cargador al vaciarlo
                new AugDef { Id = "bb_disc",   Kind = NodeKind.Major,   Seq = 5, Lvl = 3, Goal = 5 },      // Cinturon de balas (3.0; antes Discipline): 6 cargadores llenos, +10% de dano
                new AugDef { Id = "bb_bring",  Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 10 },     // Bring it on!: municion y cargadores no pesan
                new AugDef { Id = "bb_pouch",  Kind = NodeKind.Minor,   Seq = 1, Lvl = 1, Goal = 5 },      // Extra Pouch: 10% de granada al matar
                new AugDef { Id = "bb_cold",   Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 1000 },   // Cold Barrel: -30% de calor
                new AugDef { Id = "bb_leech",  Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 15 },     // Ammo Leech: tiro a la cabeza, +2 balas
                new AugDef { Id = "bb_strap",  Kind = NodeKind.Minor,   Seq = 6, Lvl = 3, Goal = 3000, Unit = ProgUnit.Meters },   // Padded Strap
                new AugDef { Id = "bb_slot",   Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "bb_d_loose", "bb_d_numb", "bb_d_clatter" },
            DrbMinor = new[] { "bb_d_pin", "bb_d_brass", "bb_d_fumble", "bb_d_burn" },
            DrinkNotice = TarkovaCola.Client.Bandolier.Notice,
            EffectText = TarkovaCola.Client.Bandolier.Effect,
        };

        internal static readonly PerkDef VultureAid = new PerkDef
        {
            Id = TarkovaCola.Client.VultureAid.Id,
            ItemTpl = TarkovaCola.Client.VultureAid.ItemTpl,
            IconFile = "vultureaid_icon.png", JingleFile = "vultureaid_jingle.mp3", HandBundle = "vultureaid_hand.bundle", HandRefHeight = 0.13f,
            Augs = new[]
            {
                new AugDef { Id = "va_scent",    Kind = NodeKind.Major,   Seq = 3, Lvl = 2, Goal = 10 },   // Scent of Money: PMC ricos marcados
                new AugDef { Id = "va_bonus",    Kind = NodeKind.Major,   Seq = 5, Lvl = 3, Goal = 25 },   // Carrion Bonus: botin extra en tus bajas
                new AugDef { Id = "va_reroll",   Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 40 },   // Re-roll de contenedores
                new AugDef { Id = "va_sharp",    Kind = NodeKind.Minor,   Seq = 1, Lvl = 1, Goal = 20 },   // Sharp Eyes: 30 m
                new AugDef { Id = "va_leftover", Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 15 },   // Leftovers: municion en tus bajas
                new AugDef { Id = "va_kit",      Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 10 },   // Field Kit: botiquin en tus bajas
                new AugDef { Id = "va_lucky",    Kind = NodeKind.Minor,   Seq = 6, Lvl = 3, Goal = 15 },   // Lucky Find: GP Coin al registrar
                new AugDef { Id = "va_slot",     Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "va_d_stench", "va_d_greedy", "va_d_hunch" },
            DrbMinor = new[] { "va_d_dry", "va_d_myopia", "va_d_clumsy", "va_d_grip" },
            DrinkNotice = TarkovaCola.Client.VultureAid.Notice,
            EffectText = TarkovaCola.Client.VultureAid.Effect,
        };

        internal static readonly PerkDef MeleeMacchiato = new PerkDef
        {
            Id = TarkovaCola.Client.MeleeMacchiato.Id,
            ItemTpl = TarkovaCola.Client.MeleeMacchiato.ItemTpl,
            IconFile = "meleemacchiato_icon.png", JingleFile = "meleemacchiato_jingle.mp3", HandBundle = "meleemacchiato_hand.bundle", HandRefHeight = 0.13f,
            Augs = new[]
            {
                new AugDef { Id = "mm_charge", Kind = NodeKind.Major,   Seq = 3, Lvl = 2, Goal = 2000, Unit = ProgUnit.Meters },   // Charge
                new AugDef { Id = "mm_frenzy", Kind = NodeKind.Major,   Seq = 5, Lvl = 3, Goal = 10 },     // Frenzy: golpes seguidos x1.2
                new AugDef { Id = "mm_iron",   Kind = NodeKind.Major,   Seq = 7, Lvl = 4, Goal = 10 },     // Iron Skull
                new AugDef { Id = "mm_quick",  Kind = NodeKind.Minor,   Seq = 1, Lvl = 1, Goal = 30 },     // Quick Draw
                new AugDef { Id = "mm_hone",   Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 50 },     // Whetstone
                new AugDef { Id = "mm_flash",  Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 200 },    // Flash Strike
                new AugDef { Id = "mm_wind",   Kind = NodeKind.Minor,   Seq = 6, Lvl = 3, Goal = 25 },     // Second Wind
                new AugDef { Id = "mm_slot",   Kind = NodeKind.Special, Seq = 9, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "mm_d_glass", "mm_d_jitter", "mm_d_crash" },
            DrbMinor = new[] { "mm_d_butter", "mm_d_bitter", "mm_d_loud", "mm_d_sugar" },
            DrinkNotice = TarkovaCola.Client.MeleeMacchiato.Notice,
            EffectText = TarkovaCola.Client.MeleeMacchiato.Effect,
        };

        internal static readonly PerkDef[] All = { SpeedCola, StaminUp, Juggernog, PhdFlopper, WidowsWine, Deadshot, Bandolier, VultureAid, MeleeMacchiato };
        // las seis originales (los logros de "todas las perks" se escribieron para ellas)
        internal static readonly PerkDef[] Original = { SpeedCola, StaminUp, Juggernog, PhdFlopper, WidowsWine, Deadshot };

        // perks con arbol de mejoras (las que se muestran en Research y en el panel del ESC)
        internal static IEnumerable<PerkDef> UiPerks { get { return All.Where(p => p.HasTree); } }

        private static readonly Dictionary<string, PerkDef> _byNode = new Dictionary<string, PerkDef>();

        static Perks()
        {
            foreach (var p in All)
            {
                foreach (var a in p.Augs) { a.Perk = p.Id; _byNode[a.Id] = p; }
                foreach (var d in p.DrbMajor.Concat(p.DrbMinor)) _byNode[d] = p;
            }
        }

        internal static PerkDef Get(string id) { return All.FirstOrDefault(p => p.Id == id); }
        internal static PerkDef ByItem(string tpl) { return tpl == null ? null : All.FirstOrDefault(p => p.ItemTpl == tpl); }
        // perk a la que pertenece una mejora o desventaja
        internal static PerkDef OfNode(string nodeId) { PerkDef p; return nodeId != null && _byNode.TryGetValue(nodeId, out p) ? p : null; }

        // ---- perks activas en la raid actual (se bebieron en ella; en Classic tambien las que persisten de raids anteriores) ----
        internal const float ModifierPower = 0.4f;          // Classic: la perk "modificador" da el 40% de su efecto base
        private static readonly HashSet<string> _active = new HashSet<string>();
        private static string _modifier;                    // perk activa como modificador (Classic): efecto reducido
        internal static bool IsActive(string perkId) { return _active.Contains(perkId); }
        internal static bool AnyActive { get { return _active.Count > 0; } }
        internal static bool IsModifier(string perkId) { return perkId != null && perkId == _modifier && _active.Contains(perkId); }
        // Fuerza del efecto base: 0 = inactiva, 1 = completo, ModifierPower = modificador de Classic.
        internal static float Power(string perkId) { return !_active.Contains(perkId) ? 0f : (perkId == _modifier ? ModifierPower : 1f); }
        internal static void Activate(string perkId, bool modifier = false)
        {
            _active.Add(perkId);
            if (modifier) _modifier = perkId; else if (_modifier == perkId) _modifier = null;
        }
        internal static void ClearActive() { _active.Clear(); _modifier = null; }
        internal static IEnumerable<PerkDef> ActivePerks { get { return All.Where(p => _active.Contains(p.Id)); } }
    }
}
