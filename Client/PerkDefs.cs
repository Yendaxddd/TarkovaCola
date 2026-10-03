using System;
using System.Collections.Generic;
using System.Linq;
using EFT.HealthSystem;

namespace TarkovaCola.Client
{
    internal enum NodeKind { Major, Minor, Special }

    internal class AugDef
    {
        public string Id;
        public string Perk;    // perk a la que pertenece (se rellena al registrarla)
        public NodeKind Kind;
        public int Seq;        // orden en el camino en serpiente (1..9)
        public int Lvl;        // nivel de la perk que revela el desafio
        public int Goal;       // objetivo del desafio
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
        public Func<string> DrinkNotice;        // subtitulo del aviso al beberla
        public Action<ActiveHealthController> OnDrink;   // efectos propios de la perk al beberla

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
            Augs = new[]
            {
                new AugDef { Id = "a_quick",  Kind = NodeKind.Major,   Seq = 1, Lvl = 1, Goal = 3 },
                new AugDef { Id = "a_bolt",   Kind = NodeKind.Major,   Seq = 3, Lvl = 2, Goal = 5 },
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
            OnDrink = SpeedColaDrink.OnDrink,
        };

        internal static readonly PerkDef StaminUp = new PerkDef
        {
            Id = TarkovaCola.Client.StaminUp.Id,
            ItemTpl = TarkovaCola.Client.StaminUp.ItemTpl,
            IconFile = "staminup_icon.png",
            JingleFile = "staminup_jingle.mp3",
            HandBundle = "staminup_hand.bundle",
            HandRefHeight = 0.17f,
            Augs = new[]
            {
                new AugDef { Id = "st_rush",    Kind = NodeKind.Major,   Seq = 1, Lvl = 1, Goal = 1 },
                new AugDef { Id = "st_shooter", Kind = NodeKind.Major,   Seq = 6, Lvl = 4, Goal = 15 },
                new AugDef { Id = "st_wind",    Kind = NodeKind.Major,   Seq = 5, Lvl = 3, Goal = 5 },
                new AugDef { Id = "st_feet",    Kind = NodeKind.Minor,   Seq = 2, Lvl = 1, Goal = 5000 },
                new AugDef { Id = "st_recover", Kind = NodeKind.Minor,   Seq = 3, Lvl = 2, Goal = 10000 },
                new AugDef { Id = "st_land",    Kind = NodeKind.Minor,   Seq = 4, Lvl = 2, Goal = 3 },
                new AugDef { Id = "st_slot",    Kind = NodeKind.Special, Seq = 7, Lvl = 5, Goal = 1 },
            },
            DrbMajor = new[] { "dt_boots", "dt_burn", "dt_jelly" },
            DrbMinor = new[] { "dt_hungry", "dt_cramps", "dt_sweaty" },
            DrinkNotice = TarkovaCola.Client.StaminUp.Notice,
            OnDrink = TarkovaCola.Client.StaminUp.OnDrink,
        };

        internal static readonly PerkDef[] All = { SpeedCola, StaminUp };

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

        // ---- perks activas en la raid actual (se bebieron en ella) ----
        private static readonly HashSet<string> _active = new HashSet<string>();
        internal static bool IsActive(string perkId) { return _active.Contains(perkId); }
        internal static void Activate(string perkId) { _active.Add(perkId); }
        internal static void ClearActive() { _active.Clear(); }
        internal static IEnumerable<PerkDef> ActivePerks { get { return All.Where(p => _active.Contains(p.Id)); } }
    }
}
