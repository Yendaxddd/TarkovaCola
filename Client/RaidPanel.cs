using System.Collections.Generic;
using System.Linq;
using System.Text;
using EFT.Counters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarkovaCola.Client
{
    // Panel de progreso de las perks que aparece a la izquierda al abrir el menu de pausa (ESC) en raid.
    // Mismo estilo nativo que la pantalla de Research: fuente Bender del juego, paneles oscuros con borde fino.
    //   Modern: una fila de pestanas (una por perk) y debajo el detalle de la perk elegida: nivel, XP y desafios pendientes. Asi el panel
    //           tiene siempre una altura razonable aunque haya 5 perks con muchos desafios. Se cambia de perk con un clic en el icono
    //           o con la rueda del raton sobre el panel.
    //   Classic: las perks activas con sus mejoras y el desafio de la raid.
    internal class RaidPanel : MonoBehaviour
    {
        private const float Width = 360f;
        private static RaidPanel _inst;
        private static string _selPerk;           // perk elegida en las pestanas; se conserva mientras dure la raid

        private CanvasGroup _group;
        private RectTransform _content, _panel;
        private bool _open;
        private float _nextRefresh, _openedAt, _top;
        private string _sig;

        // Lo llama Hud cada frame con "el menu ESC esta abierto".
        internal static void SetOpen(bool open)
        {
            if (open)
            {
                if (_inst == null)
                {
                    UiKit.EnsureAssets();
                    var go = new GameObject("TarkovaCola_RaidPanel");
                    _inst = go.AddComponent<RaidPanel>();
                    _inst.Init();
                }
                if (!_inst._open) { _inst._open = true; _inst._openedAt = Time.unscaledTime; _inst.Refresh(true); }
            }
            else if (_inst != null && _inst._open)
            {
                _inst._open = false;
            }
        }

        internal static void Destroy_()
        {
            if (_inst != null) Destroy(_inst.gameObject);
            _inst = null;
        }

        // al empezar una raid se vuelve a la perk por defecto (la primera que bebas)
        internal static void ResetSelection() { _selPerk = null; }

        private void Init()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29000;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>();     // para que las pestanas reciban clics
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;                   // cerrado no debe robar clics al menu; abierto solo lo hacen las pestanas

            _top = Screen.height * 0.10f;
            _content = UiKit.Rect(transform, "Content");
            _content.anchorMin = _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 1f);            // anclado arriba: al cambiar de perk el panel crece hacia abajo y las pestanas no se mueven
            _content.sizeDelta = new Vector2(Width, 700f);
            float s = Screen.height / 1080f;
            _content.localScale = new Vector3(s, s, 1f);
            _content.anchoredPosition = new Vector2(24f * s, -_top);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (!Plugin.InRaidWorld) { _inst = null; Destroy(gameObject); return; }      // fuera de la raid no debe quedar nada
            float target = _open ? 1f : 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime / 0.25f);
            _group.blocksRaycasts = _open;

            if (_open && _panel != null)
            {
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01((now - _openedAt) / 0.35f), 3f);
                _content.anchoredPosition = new Vector2((24f - (1f - e) * 40f) * _content.localScale.x, -_top);
            }
            if (_open) HandleWheel();
            if (_open && now >= _nextRefresh) Refresh(false);
            if (!_open && _group.alpha <= 0f && _panel != null) { Destroy(_panel.gameObject); _panel = null; _sig = null; }
        }

        // Reconstruye el panel solo si algo de lo que muestra ha cambiado (o se fuerza): asi un clic no se pierde porque el boton
        // se destruya justo debajo del raton.
        private void Refresh(bool force)
        {
            _nextRefresh = Time.unscaledTime + 1f;
            string sig = Signature();
            if (!force && _panel != null && sig == _sig) return;
            _sig = sig;
            if (_panel != null) Destroy(_panel.gameObject);
            _panel = Build();
        }

        private static float RawXp()
        {
            var me = Plugin.Me;
            return me != null && me.Profile != null ? me.Profile.EftStats.SessionCounters.GetAllInt(CounterTag.Exp) : 0f;
        }

        // resumen de todo lo que se dibuja: si no cambia, no hace falta reconstruir
        private string Signature()
        {
            var sb = new StringBuilder(256);
            if (ClassicMode.Enabled)
            {
                sb.Append('C');
                foreach (var e in ClassicMode.Entries())
                    sb.Append('|').Append(e.Perk).Append(e.Modifier ? 'm' : 'p').Append(Perks.IsActive(e.Perk) ? 'a' : 'i').Append(string.Join("+", e.Minors.ToArray()));
                var cur = ClassicChallenge.Current;
                if (cur != null) sb.Append('#').Append(cur.Id).Append(ClassicChallenge.Progress).Append(ClassicChallenge.Done).Append(ClassicChallenge.Hurt);
                return sb.ToString();
            }
            sb.Append('M').Append(_selPerk).Append(Mathf.RoundToInt(RawXp()));
            foreach (var def in Perks.UiPerks)
            {
                sb.Append('|').Append(def.Id).Append(PerkService.Xp(def.Id)).Append(Perks.IsActive(def.Id) ? 'a' : 'i');
                foreach (var c in PerkService.ActiveChallenges(def.Id)) sb.Append(',').Append(c.Id).Append(PerkService.Progress(c.Id));
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- pestanas
        private static PerkDef Selected(List<PerkDef> perks)
        {
            return perks.FirstOrDefault(p => p.Id == _selPerk) ?? perks.FirstOrDefault(p => Perks.IsActive(p.Id)) ?? perks[0];
        }

        private static void Select(string perkId)
        {
            _selPerk = perkId;
            if (_inst != null) _inst.Refresh(true);
        }

        // rueda del raton sobre el panel: perk siguiente / anterior
        private void HandleWheel()
        {
            float dy = Input.mouseScrollDelta.y;
            if (Mathf.Abs(dy) < 0.5f || _panel == null || ClassicMode.Enabled) return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(_panel, Input.mousePosition, null)) return;
            var perks = Perks.UiPerks.ToList();
            if (perks.Count < 2) return;
            int i = Mathf.Max(0, perks.FindIndex(p => p.Id == Selected(perks).Id));
            i = (i + (dy < 0f ? 1 : perks.Count - 1)) % perks.Count;
            Select(perks[i].Id);
        }

        private static TextMeshProUGUI T(Transform p, string s, float x, float y, float w, float h, float size, Color c,
            TextAlignmentOptions a = TextAlignmentOptions.TopLeft, bool wrap = false, bool bold = false)
        {
            var t = UiKit.Txt(p, s, size, c, a, wrap, bold);
            UiKit.Place(t.rectTransform, x, y, w, h);
            return t;
        }

        // texto que se reduce solo para caber en su caja
        private static TextMeshProUGUI Fit(TextMeshProUGUI t, float min, float max)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = min; t.fontSizeMax = max;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        // ---------------------------------------------------------------- construccion
        private RectTransform Build()
        {
            const float W = Width, Pad = 18f, Inner = W - Pad * 2f;

            var panel = UiKit.Panel(_content, 0, 0, W, 300f, UiKit.PanelBg, UiKit.Line);
            var root = panel.transform;
            UiKit.Place(UiKit.Img(root, "Accent", UiKit.Green).rectTransform, 0, 0, 4, 300f);
            float y = 14f;

            y = ClassicMode.Enabled ? ClassicBody(root, y, Pad, Inner) : ModernBody(root, y, Pad, Inner);

            float h = y + 4f;
            panel.rectTransform.sizeDelta = new Vector2(W, h);
            root.Find("Accent").GetComponent<RectTransform>().sizeDelta = new Vector2(4, h);
            foreach (Transform t in root) if (t.name == "bl" || t.name == "br") t.GetComponent<RectTransform>().sizeDelta = new Vector2(1, h);
            foreach (Transform t in root) if (t.name == "bb") t.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(h - 1));
            // el contenedor (anclado arriba) toma la altura del panel. Si no cabe en la pantalla, se reduce todo el panel.
            _content.sizeDelta = new Vector2(W, h);
            float s = Screen.height / 1080f, maxH = Screen.height * 0.84f;
            if (h * s > maxH) s = maxH / h;
            _content.localScale = new Vector3(s, s, 1f);
            return panel.rectTransform;
        }

        // Texto que se pone debajo de 'y' y devuelve la altura que ocupa (con salto de linea automatico)
        private float Para(Transform root, string text, float x, float y, float w, float size, Color c, float spacing = 2f)
        {
            var t = T(root, text, x, y, w, 30, size, c, TextAlignmentOptions.TopLeft, true);
            t.lineSpacing = spacing;
            return t.GetPreferredValues(t.text, w, 0f).y;
        }

        // ---------------------------------------------------------------- Modern
        private float ModernBody(Transform root, float y, float Pad, float Inner)
        {
            var perks = Perks.UiPerks.ToList();
            var sel = Selected(perks);
            y = Tabs(root, perks, sel, y, Pad, Inner);
            y += 14f;
            y = Section(root, sel, y, Pad, Inner, RawXp());
            if (perks.Count > 1)
            {
                y += 4f;
                UiKit.Place(UiKit.Img(root, "Sep", UiKit.Line).rectTransform, Pad, y, Inner, 1);
                y += 8f;
                y += Para(root, L.T("rp.tabs.hint"), Pad, y, Inner, 11, UiKit.Dim) + 6f;
            }
            return y;
        }

        // Una pestana por perk: icono (apagado si no esta activa en esta raid), punto verde si esta activa y cuantos desafios tiene pendientes.
        private float Tabs(Transform root, List<PerkDef> perks, PerkDef sel, float y, float Pad, float Inner)
        {
            const float Tile = 56f;
            int n = perks.Count;
            float gap = n > 1 ? Mathf.Min(10f, (Inner - n * Tile) / (n - 1)) : 0f;
            for (int i = 0; i < n; i++)
            {
                var def = perks[i];
                string id = def.Id;
                bool on = def == sel, active = Perks.IsActive(id);
                float x = Pad + i * (Tile + gap);

                var b = UiButton.Make(root, x, y, Tile, Tile, () => Select(id),
                                      on ? UiKit.C(240, 193, 75, 0.16f) : UiKit.C(18, 34, 50, 0.95f),
                                      on ? UiKit.C(240, 193, 75, 0.26f) : UiKit.C(42, 68, 94, 0.97f));
                Color border = on ? UiKit.Gold : UiKit.Line;
                float bw = on ? 2f : 1f;
                UiKit.Place(UiKit.Img(b.transform, "bt", border).rectTransform, 0, 0, Tile, bw);
                UiKit.Place(UiKit.Img(b.transform, "bb", border).rectTransform, 0, Tile - bw, Tile, bw);
                UiKit.Place(UiKit.Img(b.transform, "bl", border).rectTransform, 0, 0, bw, Tile);
                UiKit.Place(UiKit.Img(b.transform, "br", border).rectTransform, Tile - bw, 0, bw, Tile);

                var icon = Hud.IconSprite(id);
                if (icon != null)
                {
                    var ic = UiKit.Img(b.transform, "Icon", active ? Color.white : UiKit.C(255, 255, 255, 0.45f), icon);
                    UiKit.Place(ic.rectTransform, 8, 8, 40, 40);
                    ic.preserveAspect = true;
                }
                if (active) UiKit.Place(UiKit.Img(b.transform, "Dot", UiKit.Green, UiKit.Circle).rectTransform, Tile - 15, 5, 10, 10);
                int pending = PerkService.ActiveChallenges(id).Count();
                if (pending > 0) T(b.transform, pending.ToString(), Tile - 30, Tile - 17, 26, 14, 11, UiKit.Gold, TextAlignmentOptions.TopRight, false, true);
            }
            return y + Tile;
        }

        // Nivel, XP y desafios pendientes de la perk elegida. Los desafios van ordenados por lo cerca que estan de completarse.
        private float Section(Transform root, PerkDef def, float y, float Pad, float Inner, float rawXp)
        {
            string id = def.Id;
            int lvl, cur, max;
            PerkService.LevelInfo(PerkService.Xp(id), out lvl, out cur, out max);
            bool isMax = PerkService.IsMax(id), active = Perks.IsActive(id);
            int live = active ? Mathf.RoundToInt(rawXp * (1f + (float)PerkService.XpBonus)) : 0;
            var challenges = PerkService.ActiveChallenges(id)
                .OrderByDescending(c => Mathf.Min(PerkService.Progress(c.Id), c.Goal) / (float)c.Goal)
                .ThenBy(c => c.Seq).ToList();

            // cabecera: icono, nombre y nivel en una sola fila
            var icon = Hud.IconSprite(id);
            if (icon != null)
            {
                var ic = UiKit.Img(root, "Icon", Color.white, icon); UiKit.Place(ic.rectTransform, Pad, y, 34, 34);
            }
            T(root, def.Name.ToUpperInvariant(), Pad + 42, y + 2, Inner - 42 - 90, 28, 22, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
            T(root, L.F("raid.level", lvl), Pad + Inner - 90, y + 2, 90, 20, 14, UiKit.Green, TextAlignmentOptions.TopRight, false, true);
            if (active) T(root, L.T("raid.active"), Pad + Inner - 90, y + 20, 90, 16, 11, UiKit.Gold, TextAlignmentOptions.TopRight, false, true);
            y += 42f;

            // barra de XP fina (verde = XP guardada, dorado = XP de esta raid)
            var back = UiKit.Img(root, "XpBack", UiKit.C(255, 255, 255, 0.10f)); UiKit.Place(back.rectTransform, Pad, y, Inner, 6);
            float f0 = Mathf.Clamp01(cur / (float)max), f1 = Mathf.Clamp01((cur + live) / (float)max);
            if (isMax) { live = 0; f0 = 1f; f1 = 1f; }
            if (live > 0) UiKit.Place(UiKit.Img(back.transform, "XpLive", UiKit.Gold).rectTransform, 0, 0, Inner * f1, 6);
            UiKit.Place(UiKit.Img(back.transform, "XpFill", UiKit.C(31, 174, 93)).rectTransform, 0, 0, Inner * f0, 6);
            y += 10f;
            T(root, isMax ? L.T("xp.max") : cur + " / " + max + " XP", Pad, y, Inner * 0.5f, 18, 13, isMax ? UiKit.Gold : UiKit.Text, TextAlignmentOptions.TopLeft, false, isMax);
            if (!isMax && active) T(root, L.F("raid.xpraid", live), Pad + Inner * 0.4f, y, Inner * 0.6f, 18, 13, UiKit.Gold, TextAlignmentOptions.TopRight, false, true);
            y += 22f;
            if (!isMax && !active) { y += Para(root, L.F("raid.drink", def.Name), Pad, y, Inner, 12, UiKit.Dim) + 4f; }

            // desafios pendientes
            y += 4f;
            if (challenges.Count == 0)
            {
                // en el nivel maximo ya estan todos revelados: si no queda ninguno pendiente, es que estan todos completados
                y += Para(root, L.T(isMax ? "raid.alldone" : "raid.none"), Pad, y, Inner, 12, isMax ? UiKit.Green : UiKit.Dim) + 6f;
                return y;
            }
            T(root, L.F("raid.pending", challenges.Count), Pad, y, Inner, 18, 13, UiKit.Gold, TextAlignmentOptions.TopLeft, false, true);
            UiKit.Place(UiKit.Img(root, "Underline", UiKit.Gold).rectTransform, Pad, y + 20f, Inner, 1);
            y += 28f;

            foreach (var c in challenges)
            {
                int prog = Mathf.Min(PerkService.Progress(c.Id), c.Goal);
                // marca de color segun el tipo de mejora (igual que en Research): principal dorado, secundaria cian, especial verde
                Color kind = c.Kind == NodeKind.Major ? UiKit.Gold : c.Kind == NodeKind.Minor ? UiKit.Cyan : UiKit.Green;
                UiKit.Place(UiKit.Img(root, "Kind", kind).rectTransform, Pad, y + 6f, 6, 6);
                Fit(T(root, Data.Name(c.Id), Pad + 12, y, Inner - 12 - 112, 20, 15, UiKit.White, TextAlignmentOptions.TopLeft, false, true), 10, 15);
                T(root, Data.Prog(c, prog) + " / " + Data.Prog(c, c.Goal), Pad + Inner - 110, y + 2, 110, 18, 13, UiKit.Gold, TextAlignmentOptions.TopRight, false, true);
                y += 20f;
                y += Para(root, Data.Chal(c.Id), Pad + 12, y, Inner - 12, 12, UiKit.Dim) + 3f;
                var bb = UiKit.Img(root, "PBack", UiKit.C(255, 255, 255, 0.10f)); UiKit.Place(bb.rectTransform, Pad + 12, y, Inner - 12, 4);
                UiKit.Place(UiKit.Img(bb.transform, "PFill", UiKit.Gold).rectTransform, 0, 0, (Inner - 12) * Mathf.Clamp01(prog / (float)c.Goal), 4);
                y += 14f;
            }
            return y;
        }

        // ---------------------------------------------------------------- Classic
        // Modo Classic: las perks activas (principales con sus mejoras y el modificador) y el desafio de la raid.
        private float ClassicBody(Transform root, float y, float Pad, float Inner)
        {
            T(root, L.T("classic.title"), Pad, y, Inner, 28, 22, UiKit.Gold, TextAlignmentOptions.TopLeft, false, true);
            y += 38f;

            var entries = ClassicMode.Entries();
            if (entries.Count == 0) y += Para(root, L.T("rp.classic.empty"), Pad, y, Inner, 13, UiKit.Dim) + 10f;

            foreach (var e in entries)
            {
                var def = Perks.Get(e.Perk);
                var icon = Hud.IconSprite(def.Id);
                if (icon != null)
                {
                    var ic = UiKit.Img(root, "Icon", Perks.IsActive(e.Perk) ? Color.white : UiKit.C(255, 255, 255, 0.5f), icon);
                    UiKit.Place(ic.rectTransform, Pad, y, 30, 30);
                }
                T(root, def.Name.ToUpperInvariant(), Pad + 38, y + 3, Inner - 38 - 104, 26, 18, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
                T(root, L.T(e.Modifier ? "classic.tag.mod" : "classic.tag.main"), Pad + Inner - 104, y + 8, 104, 16, 11, e.Modifier ? UiKit.Cyan : UiKit.Gold, TextAlignmentOptions.TopRight, false, true);
                y += 38f;

                if (e.Modifier)
                {
                    y += Para(root, L.F("classic.mod.note", Mathf.RoundToInt(Perks.ModifierPower * 100f)), Pad + 8, y, Inner - 8, 12, UiKit.Dim) + 6f;
                }
                foreach (var aug in e.Minors)
                {
                    T(root, Data.Name(aug), Pad + 8, y, Inner - 8, 18, 14, UiKit.Cyan, TextAlignmentOptions.TopLeft, false, true);
                    y += 18f;
                    y += Para(root, Data.Desc(aug), Pad + 8, y, Inner - 8, 12, UiKit.Dim) + 6f;
                }
                y += 4f;
                UiKit.Place(UiKit.Img(root, "Sep", UiKit.Line).rectTransform, Pad, y, Inner, 1);
                y += 10f;
            }

            // desafio de la raid
            T(root, L.T("classic.chal.title"), Pad, y, Inner, 20, 15, UiKit.Gold, TextAlignmentOptions.TopLeft, false, true);
            UiKit.Place(UiKit.Img(root, "Underline", UiKit.Gold).rectTransform, Pad, y + 20f, Inner, 1);
            y += 28f;
            var cur = ClassicChallenge.Current;
            if (cur == null) return y + Para(root, L.T("rp.chal.none"), Pad, y, Inner, 12, UiKit.Dim) + 6f;

            T(root, ClassicChallenge.Name(cur.Id), Pad, y, Inner - 90, 20, 16, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
            if (cur.Id != "ghost")
                T(root, ProgressText(cur, ClassicChallenge.Progress), Pad + Inner - 90, y + 2, 90, 18, 13, UiKit.Gold, TextAlignmentOptions.TopRight, false, true);
            y += 22f;
            y += Para(root, ClassicChallenge.Desc(cur.Id), Pad, y, Inner, 12, UiKit.Dim) + 4f;

            if (cur.Id == "ghost")
            {
                bool failed = ClassicChallenge.Failed;
                T(root, L.T(failed ? "rp.chal.ghost.fail" : "rp.chal.ghost.ok"), Pad, y, Inner, 18, 13, failed ? UiKit.Red : UiKit.Green, TextAlignmentOptions.TopLeft, false, true);
                y += 22f;
            }
            else
            {
                var bb = UiKit.Img(root, "PBack", UiKit.C(255, 255, 255, 0.10f)); UiKit.Place(bb.rectTransform, Pad, y, Inner, 4);
                UiKit.Place(UiKit.Img(bb.transform, "PFill", ClassicChallenge.Done ? UiKit.Green : UiKit.Gold).rectTransform, 0, 0,
                            Inner * Mathf.Clamp01(ClassicChallenge.Progress / (float)cur.Goal), 4);
                y += 12f;
                if (ClassicChallenge.Done) { T(root, L.T("rp.chal.done"), Pad, y, Inner, 18, 13, UiKit.Green, TextAlignmentOptions.TopLeft, false, true); y += 22f; }
            }
            y += Para(root, L.T("rp.chal.reward"), Pad, y, Inner, 11, UiKit.Dim) + 6f;
            return y;
        }

        // progreso legible del desafio: metros, minutos o simplemente "n / meta"
        private static string ProgressText(ClassicChallenge.Def d, int p)
        {
            p = Mathf.Min(p, d.Goal);
            if (d.Id == "run" || d.Id == "travel") return p + " / " + d.Goal + " m";
            if (d.Id == "time") return (p / 60) + ":" + (p % 60).ToString("00") + " / " + (d.Goal / 60) + ":" + (d.Goal % 60).ToString("00");
            return p + " / " + d.Goal;
        }
    }
}
