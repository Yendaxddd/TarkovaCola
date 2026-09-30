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
            _content.sizeDelta = new Vector2(420f, 700f);
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
            int lvl, cur, max;
            PerkService.LevelInfo(PerkService.Xp, out lvl, out cur, out max);

            // XP ganada en esta raid (aun sin confirmar: se suma al terminar)
            int live = 0;
            var me = Plugin.Me;
            if (me != null && me.Profile != null && Plugin.SpeedColaActive)
                live = Mathf.RoundToInt(me.Profile.EftStats.SessionCounters.GetAllInt(CounterTag.Exp) * (1f + (float)PerkService.XpBonus));

            var active = PerkService.ActiveChallenges.OrderBy(a => a.Seq).ToList();
            const float W = 420f, Pad = 22f, Inner = W - Pad * 2f;

            var panel = UiKit.Panel(_content, 0, 0, W, 300f, UiKit.PanelBg, UiKit.Line);
            var root = panel.transform;
            UiKit.Place(UiKit.Img(root, "Accent", UiKit.Green).rectTransform, 0, 0, 4, 300f);
            float y = 18f;

            // cabecera
            if (Hud.IconSprite != null)
            {
                var ic = UiKit.Img(root, "Icon", Color.white, Hud.IconSprite); UiKit.Place(ic.rectTransform, Pad, y, 52, 52);
            }
            T(root, L.T("raid.title"), Pad + 66, y - 2, Inner - 66, 32, 30, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
            T(root, L.F("raid.level", lvl), Pad + 66, y + 32, Inner - 66, 20, 16, UiKit.Green, TextAlignmentOptions.TopLeft, false, true);
            y += 70f;

            // barra de XP (verde = XP guardada, dorado = XP de esta raid)
            var back = UiKit.Img(root, "XpBack", UiKit.C(255, 255, 255, 0.10f)); UiKit.Place(back.rectTransform, Pad, y, Inner, 10);
            float f0 = Mathf.Clamp01(cur / (float)max), f1 = Mathf.Clamp01((cur + live) / (float)max);
            if (PerkService.IsMax) { live = 0; f0 = 1f; f1 = 1f; }
            if (live > 0) UiKit.Place(UiKit.Img(back.transform, "XpLive", UiKit.Gold).rectTransform, 0, 0, Inner * f1, 10);
            UiKit.Place(UiKit.Img(back.transform, "XpFill", UiKit.C(31, 174, 93)).rectTransform, 0, 0, Inner * f0, 10);
            y += 18f;
            bool isMax = PerkService.IsMax;
            T(root, isMax ? L.T("xp.max") : cur + " / " + max + " XP", Pad, y, Inner, 22, 16, isMax ? UiKit.Gold : UiKit.Text, TextAlignmentOptions.TopLeft, false, isMax);
            y += 24f;
            if (isMax) y += 10f;                                    // en el nivel maximo ya no se gana XP de perk
            else if (Plugin.SpeedColaActive) { T(root, L.F("raid.xpraid", live), Pad, y, Inner, 22, 15, UiKit.Gold, TextAlignmentOptions.TopLeft, false, true); y += 34f; }
            else { T(root, L.T("raid.drink"), Pad, y, Inner, 22, 14, UiKit.Dim, TextAlignmentOptions.TopLeft, true); y += 48f; }

            // desafios
            T(root, L.T("raid.pending"), Pad, y, Inner, 22, 16, UiKit.Gold, TextAlignmentOptions.TopLeft, false, true);
            UiKit.Place(UiKit.Img(root, "Underline", UiKit.Gold).rectTransform, Pad, y + 26f, Inner, 2);
            y += 40f;

            if (active.Count == 0)
            {
                var none = T(root, L.T("raid.none"), Pad, y, Inner, 40, 14, UiKit.Dim, TextAlignmentOptions.TopLeft, true);
                y += none.GetPreferredValues(none.text, Inner, 0f).y + 8f;
            }
            foreach (var c in active)
            {
                int prog = Mathf.Min(PerkService.Progress(c.Id), c.Goal);
                T(root, Data.Name(c.Id), Pad, y, Inner - 70, 22, 18, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
                T(root, prog + " / " + c.Goal, Pad + Inner - 70, y + 1, 70, 22, 16, UiKit.Gold, TextAlignmentOptions.TopRight, false, true);
                y += 26f;
                var d = T(root, Data.Chal(c.Id), Pad, y, Inner, 40, 14, UiKit.Dim, TextAlignmentOptions.TopLeft, true);
                d.lineSpacing = 4f;
                y += d.GetPreferredValues(d.text, Inner, 0f).y + 6f;
                var bb = UiKit.Img(root, "PBack", UiKit.C(255, 255, 255, 0.10f)); UiKit.Place(bb.rectTransform, Pad, y, Inner, 6);
                UiKit.Place(UiKit.Img(bb.transform, "PFill", UiKit.Gold).rectTransform, 0, 0, Inner * Mathf.Clamp01(prog / (float)c.Goal), 6);
                y += 22f;
            }

            float h = y + 6f;
            panel.rectTransform.sizeDelta = new Vector2(W, h);
            root.Find("Accent").GetComponent<RectTransform>().sizeDelta = new Vector2(4, h);
            foreach (Transform t in root) if (t.name == "bl" || t.name == "br") t.GetComponent<RectTransform>().sizeDelta = new Vector2(1, h);
            foreach (Transform t in root) if (t.name == "bb") t.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(h - 1));
            // el contenedor (anclado al centro vertical) toma la altura del panel: queda centrado
            _content.sizeDelta = new Vector2(W, h);
            return panel.rectTransform;
        }
    }
}
