using System.Collections.Generic;
using System.Linq;
using EFT.Counters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarkovaCola.Client
{
    // Panel de progreso de la perk que aparece a la izquierda (a media altura) al abrir el menu de pausa (ESC) en raid.
    // Mismo estilo nativo que la pantalla de Research: fuente Bender del juego, paneles oscuros con borde fino.
    internal class RaidPanel : MonoBehaviour
    {
        private const float Width = 360f;
        private static RaidPanel _inst;

        private CanvasGroup _group;
        private RectTransform _content, _panel;
        private bool _open;
        private float _nextRefresh, _openedAt;

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
                if (!_inst._open) { _inst._open = true; _inst._openedAt = Time.unscaledTime; _inst.Refresh(); }
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

        private void Init()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29000;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;          // es solo informativo: no debe robar clics al menu

            _content = UiKit.Rect(transform, "Content");
            _content.anchorMin = new Vector2(0f, 0.5f); _content.anchorMax = new Vector2(0f, 0.5f);
            _content.pivot = new Vector2(0f, 0.5f);
            _content.sizeDelta = new Vector2(Width, 700f);
            float s = Screen.height / 1080f;
            _content.localScale = new Vector3(s, s, 1f);
            _content.anchoredPosition = new Vector2(24f * s, 0f);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (!Plugin.InRaidWorld) { _inst = null; Destroy(gameObject); return; }      // fuera de la raid no debe quedar nada
            float target = _open ? 1f : 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime / 0.25f);

            if (_open && _panel != null)
            {
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01((now - _openedAt) / 0.35f), 3f);
                _content.anchoredPosition = new Vector2((24f - (1f - e) * 40f) * _content.localScale.x, 0f);
            }
            if (_open && now >= _nextRefresh) Refresh();
            if (!_open && _group.alpha <= 0f && _panel != null) { Destroy(_panel.gameObject); _panel = null; }
        }

        private void Refresh()
        {
            _nextRefresh = Time.unscaledTime + 1f;
            if (_panel != null) Destroy(_panel.gameObject);
            _panel = Build();
        }

        private static TextMeshProUGUI T(Transform p, string s, float x, float y, float w, float h, float size, Color c,
            TextAlignmentOptions a = TextAlignmentOptions.TopLeft, bool wrap = false, bool bold = false)
        {
            var t = UiKit.Txt(p, s, size, c, a, wrap, bold);
            UiKit.Place(t.rectTransform, x, y, w, h);
            return t;
        }

        private RectTransform Build()
        {
            const float W = Width, Pad = 18f, Inner = W - Pad * 2f;

            // XP ganada en esta raid (aun sin confirmar: se suma al terminar)
            float rawXp = 0f;
            var me = Plugin.Me;
            if (me != null && me.Profile != null) rawXp = me.Profile.EftStats.SessionCounters.GetAllInt(CounterTag.Exp);

            var panel = UiKit.Panel(_content, 0, 0, W, 300f, UiKit.PanelBg, UiKit.Line);
            var root = panel.transform;
            UiKit.Place(UiKit.Img(root, "Accent", UiKit.Green).rectTransform, 0, 0, 4, 300f);
            float y = 14f;

            if (ClassicMode.Enabled)
            {
                y = ClassicBody(root, y, Pad, Inner);                    // modo Classic: perks persistentes y desafio de la raid
            }
            else
            {
                bool first = true;
                foreach (var def in Perks.UiPerks)
                {
                    if (!first)
                    {
                        y += 4f;
                        UiKit.Place(UiKit.Img(root, "Sep", UiKit.Line).rectTransform, Pad, y, Inner, 1);
                        y += 12f;
                    }
                    first = false;
                    y = Section(root, def, y, W, Pad, Inner, rawXp);
                }
            }

            float h = y + 4f;
            panel.rectTransform.sizeDelta = new Vector2(W, h);
            root.Find("Accent").GetComponent<RectTransform>().sizeDelta = new Vector2(4, h);
            foreach (Transform t in root) if (t.name == "bl" || t.name == "br") t.GetComponent<RectTransform>().sizeDelta = new Vector2(1, h);
            foreach (Transform t in root) if (t.name == "bb") t.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(h - 1));
            // el contenedor (anclado al centro vertical) toma la altura del panel: queda centrado. Si no cabe, se reduce todo
            // el panel para que nunca se salga de la pantalla (mas perks = mas alto).
            _content.sizeDelta = new Vector2(W, h);
            float s = Screen.height / 1080f, maxH = Screen.height * 0.88f;
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

        // Nivel, XP y desafios pendientes de una perk (compacto). Devuelve la 'y' donde termina.
        private float Section(Transform root, PerkDef def, float y, float W, float Pad, float Inner, float rawXp)
        {
            string id = def.Id;
            int lvl, cur, max;
            PerkService.LevelInfo(PerkService.Xp(id), out lvl, out cur, out max);
            bool isMax = PerkService.IsMax(id), active = Perks.IsActive(id);
            int live = active ? Mathf.RoundToInt(rawXp * (1f + (float)PerkService.XpBonus)) : 0;
            var challenges = PerkService.ActiveChallenges(id).OrderBy(a => a.Seq).ToList();

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
            y += 20f;
            if (!isMax && !active) { T(root, L.F("raid.drink", def.Name), Pad, y, Inner, 18, 12, UiKit.Dim, TextAlignmentOptions.TopLeft, true); y += 20f; }

            // desafios pendientes
            if (challenges.Count == 0)
            {
                T(root, L.T("raid.none"), Pad, y, Inner, 16, 12, UiKit.Dim, TextAlignmentOptions.TopLeft, true);
                return y + 34f;
            }
            T(root, L.T("raid.pending"), Pad, y, Inner, 18, 13, UiKit.Gold, TextAlignmentOptions.TopLeft, false, true);
            UiKit.Place(UiKit.Img(root, "Underline", UiKit.Gold).rectTransform, Pad, y + 20f, Inner, 1);
            y += 26f;

            foreach (var c in challenges)
            {
                int prog = Mathf.Min(PerkService.Progress(c.Id), c.Goal);
                T(root, Data.Name(c.Id), Pad, y, Inner - 64, 20, 15, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
                T(root, prog + " / " + c.Goal, Pad + Inner - 64, y + 2, 64, 18, 13, UiKit.Gold, TextAlignmentOptions.TopRight, false, true);
                y += 19f;
                var d = T(root, Data.Chal(c.Id), Pad, y, Inner, 30, 12, UiKit.Dim, TextAlignmentOptions.TopLeft, true);
                d.lineSpacing = 2f;
                y += d.GetPreferredValues(d.text, Inner, 0f).y + 3f;
                var bb = UiKit.Img(root, "PBack", UiKit.C(255, 255, 255, 0.10f)); UiKit.Place(bb.rectTransform, Pad, y, Inner, 4);
                UiKit.Place(UiKit.Img(bb.transform, "PFill", UiKit.Gold).rectTransform, 0, 0, Inner * Mathf.Clamp01(prog / (float)c.Goal), 4);
                y += 12f;
            }
            return y;
        }
    }
}
