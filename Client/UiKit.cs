using System;
using System.Linq;
using Comfort.Common;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TarkovaCola.Client
{
    // Utilidades para construir interfaz uGUI con el aspecto de EFT: fuente Bender del propio juego, sonidos de UI del juego,
    // paneles oscuros con borde fino. Todo en coordenadas de un lienzo de referencia 1920x1080 (origen arriba a la izquierda).
    internal static class UiKit
    {
        internal static Color C(int r, int g, int b, float a = 1f) { return new Color(r / 255f, g / 255f, b / 255f, a); }

        // clases de nodo: mejora principal / secundaria, desventaja principal / secundaria, especial
        internal static readonly Color Gold = C(240, 193, 75), Cyan = C(79, 216, 255), Red = C(255, 77, 77), Pink = C(255, 95, 192), Green = C(75, 226, 124);
        internal static readonly Color Text = C(197, 195, 178), Dim = C(125, 146, 164), White = C(235, 240, 245);
        internal static readonly Color PanelBg = C(10, 20, 30, 0.92f), Panel2 = C(18, 34, 50, 0.95f), Line = C(120, 190, 225, 0.30f);

        private static TMP_FontAsset _font;
        internal static Sprite WhiteSprite, Circle, Hex;
        private static bool _ready;

        internal static Color ClassColor(string cls)
        {
            switch (cls) { case "am": return Gold; case "an": return Cyan; case "dm": return Red; case "dn": return Pink; default: return Green; }
        }

        internal static void EnsureAssets()
        {
            if (_ready) return;
            _ready = true;

            // fuente: la misma que usa EFT (Bender) para que el texto se vea nativo
            try
            {
                var fonts = Resources.LoadAll<TMP_FontAsset>("UI/Fonts");
                Dbg.Log("UI", "fuentes de EFT: " + string.Join(", ", fonts.Select(f => f.name).ToArray()));
                _font = fonts.FirstOrDefault(f => f.name.IndexOf("bender", StringComparison.OrdinalIgnoreCase) >= 0 && f.name.IndexOf("bold", StringComparison.OrdinalIgnoreCase) < 0)
                        ?? fonts.FirstOrDefault(f => f.name.IndexOf("bender", StringComparison.OrdinalIgnoreCase) >= 0)
                        ?? fonts.FirstOrDefault();
            }
            catch (Exception e) { Plugin.Log.LogWarning("No se pudieron leer las fuentes de EFT: " + e.Message); }
            if (_font == null) _font = TMP_Settings.defaultFontAsset;

            var w = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            w.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); w.Apply();
            WhiteSprite = Sprite.Create(w, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 100f);
            Circle = MakeShape(128, false);
            Hex = MakeShape(128, true);
        }

        // Circulo o hexagono con bordes suavizados (mismo hexagono que la maqueta web).
        private static Sprite MakeShape(int size, bool hex)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            Vector2[] poly = { new Vector2(.25f, .03f), new Vector2(.75f, .03f), new Vector2(1f, .5f), new Vector2(.75f, .97f), new Vector2(.25f, .97f), new Vector2(0f, .5f) };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f) / size, v = (y + .5f) / size, dist;
                    if (!hex) dist = (.5f - Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f))) * size;
                    else
                    {
                        // poligono convexo antihorario: la normal (e.y,-e.x) apunta hacia fuera, asi que dentro las distancias son
                        // negativas; la del borde mas cercano es la mayor. Se invierte para que dentro sea positivo.
                        dist = float.MinValue;
                        for (int i = 0; i < poly.Length; i++)
                        {
                            Vector2 a = poly[i], b = poly[(i + 1) % poly.Length], e = b - a, n = new Vector2(e.y, -e.x).normalized;
                            dist = Mathf.Max(dist, Vector2.Dot(new Vector2(u, v) - a, n) * size);
                        }
                        dist = -dist;
                    }
                    px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(dist + .5f));
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
        }

        // ---------- creacion de elementos ----------
        internal static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        internal static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        internal static Image Img(Transform parent, string name, Color c, Sprite s = null, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s ?? WhiteSprite;
            im.color = c;
            im.raycastTarget = raycast;
            return im;
        }

        internal static TextMeshProUGUI Txt(Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool wrap = false, bool bold = false)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = wrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = false;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.text = text;
            return t;
        }

        // rectangulo con borde fino (4 lineas) sobre un fondo
        internal static Image Panel(Transform parent, float x, float y, float w, float h, Color bg, Color border, float bw = 1f)
        {
            var back = Img(parent, "Panel", bg);
            Place(back.rectTransform, x, y, w, h);
            if (border.a > 0f)
            {
                Place(Img(back.transform, "bt", border).rectTransform, 0, 0, w, bw);
                Place(Img(back.transform, "bb", border).rectTransform, 0, h - bw, w, bw);
                Place(Img(back.transform, "bl", border).rectTransform, 0, 0, bw, h);
                Place(Img(back.transform, "br", border).rectTransform, w - bw, 0, bw, h);
            }
            return back;
        }

        // Icono con anillo de color (hexagono para las principales, circulo para las demas).
        internal static RectTransform Badge(Transform parent, float cx, float cy, float size, bool hex, Color ring, Sprite icon, Color iconTint, string label = null)
        {
            var root = Rect(parent, "Badge");
            Place(root, cx - size / 2f, cy - size / 2f, size, size);
            var sprite = hex ? Hex : Circle;
            var outer = Img(root, "Ring", ring, sprite); Stretch(outer.rectTransform);
            var inner = Img(root, "Fill", C(12, 22, 32), sprite);
            float pad = size * (hex ? 0.05f : 0.06f);
            inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one;
            inner.rectTransform.offsetMin = new Vector2(pad, pad); inner.rectTransform.offsetMax = new Vector2(-pad, -pad);
            if (icon != null)
            {
                var ic = Img(root, "Icon", iconTint, icon);
                float m = size * 0.2f;
                ic.rectTransform.anchorMin = Vector2.zero; ic.rectTransform.anchorMax = Vector2.one;
                ic.rectTransform.offsetMin = new Vector2(m, m); ic.rectTransform.offsetMax = new Vector2(-m, -m);
                ic.preserveAspect = true;
            }
            else if (label != null)
            {
                var t = Txt(root, label, size * 0.36f, ring, TextAlignmentOptions.Center, false, true);
                Stretch(t.rectTransform);
            }
            return root;
        }

        internal static void Sound(EUISoundType type)
        {
            try { if (Singleton<GUISounds>.Instantiated) Singleton<GUISounds>.Instance.PlayUISound(type); }
            catch { }
        }
    }

    // Boton generico: clic, resaltado al pasar el raton y sonidos de UI del juego.
    internal class UiButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        internal Action OnClick;
        internal Graphic Target;
        internal Color Normal, Hover;
        internal bool Enabled = true;

        public void OnPointerClick(PointerEventData e)
        {
            if (!Enabled || e.button != PointerEventData.InputButton.Left) return;
            UiKit.Sound(EUISoundType.ButtonClick);
            if (OnClick != null) OnClick();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Enabled) return;
            if (Target != null) Target.color = Hover;
            UiKit.Sound(EUISoundType.ButtonOver);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (Target != null) Target.color = Normal;
        }

        // Convierte un rectangulo en boton: un Image transparente que recibe el raton.
        internal static UiButton Make(Transform parent, float x, float y, float w, float h, Action click, Color normal, Color hover)
        {
            var img = UiKit.Img(parent, "Button", normal, null, raycast: true);
            UiKit.Place(img.rectTransform, x, y, w, h);
            var b = img.gameObject.AddComponent<UiButton>();
            b.OnClick = click; b.Target = img; b.Normal = normal; b.Hover = hover;
            return b;
        }
    }
}
