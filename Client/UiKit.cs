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
        internal static Sprite WhiteSprite, Circle, Hex, Grad, Band, Bubble, Glow, Vignette;
        internal static Texture2D Scan;
        private static bool _ready;

        // color de cada perk (el de la luz de su maquina): tinte del fondo y de los acentos
        private static readonly System.Collections.Generic.Dictionary<string, Color> PerkColors = new System.Collections.Generic.Dictionary<string, Color>
        {
            ["speedcola"] = C(77, 230, 110), ["staminup"] = C(255, 170, 60), ["juggernog"] = C(255, 70, 60), ["phdflopper"] = C(190, 105, 255),
            ["widowswine"] = C(255, 80, 120), ["deadshot"] = C(255, 215, 80), ["bandolier"] = C(150, 190, 70), ["vultureaid"] = C(40, 200, 185),
            ["meleemacchiato"] = C(205, 140, 80),
        };
        internal static Color PerkColor(string id) { Color c; return id != null && PerkColors.TryGetValue(id, out c) ? c : Green; }

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
            Grad = MakeTex(4, 64, (u, v) => v * v);                                                     // claro arriba, nada abajo
            Band = MakeTex(64, 4, (u, v) => Mathf.Pow(Mathf.Sin(u * Mathf.PI), 2f));                   // banda de brillo
            Glow = MakeTex(128, 128, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 2f; return Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f); });
            Vignette = MakeTex(128, 128, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 1.42f; return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.45f) / 0.55f)); });
            Bubble = MakeTex(64, 64, (u, v) =>
            {
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 2f;
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) * 9f), fill = d < 0.9f ? 0.18f : 0f;
                float spec = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(.36f, .66f)) * 7f);
                return Mathf.Max(Mathf.Max(rim, fill), spec);
            });
            // lineas de monitor (se repite; UiScan las hace bajar)
            Scan = new Texture2D(1, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            Scan.SetPixels(new[] { new Color(1, 1, 1, 0.05f), Color.clear, Color.clear, Color.clear }); Scan.Apply();
        }

        // sprite blanco con alfa = f(u, v) (u, v en 0..1, v hacia arriba)
        private static Sprite MakeTex(int w, int h, Func<float, float, float> alpha)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = new Color(1, 1, 1, Mathf.Clamp01(alpha((x + .5f) / w, (y + .5f) / h)));
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f);
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
            // relieve: un poco de luz arriba que se apaga hacia abajo (va primero: el contenido queda encima)
            if (bg.a > 0.05f && h >= 24f) { var sheen = Img(back.transform, "Sheen", new Color(1f, 1f, 1f, 0.055f), Grad); Stretch(sheen.rectTransform); }
            if (border.a > 0f)
            {
                Place(Img(back.transform, "bt", border).rectTransform, 0, 0, w, bw);
                Place(Img(back.transform, "bb", border).rectTransform, 0, h - bw, w, bw);
                Place(Img(back.transform, "bl", border).rectTransform, 0, 0, bw, h);
                Place(Img(back.transform, "br", border).rectTransform, w - bw, 0, bw, h);
                // esquinas en escuadra, mas vivas que el borde (estilo CoD/EFT), en tarjetas de cierto tamano
                if (w >= 100f && h >= 44f)
                {
                    var cc = new Color(border.r, border.g, border.b, Mathf.Min(1f, border.a * 2.4f + 0.2f));
                    float L = Mathf.Min(16f, Mathf.Min(w, h) / 4f), t = bw + 1f;
                    foreach (var (cx, cy, sx, sy) in new[] { (0f, 0f, 1f, 1f), (w, 0f, -1f, 1f), (0f, h, 1f, -1f), (w, h, -1f, -1f) })
                    {
                        Place(Img(back.transform, "c", cc).rectTransform, sx > 0 ? cx : cx - L, sy > 0 ? cy : cy - t, L, t);
                        Place(Img(back.transform, "c", cc).rectTransform, sx > 0 ? cx : cx - t, sy > 0 ? cy : cy - L, t, L);
                    }
                }
            }
            return back;
        }

        // Icono con anillo de color (hexagono para las principales, circulo para las demas).
        internal static RectTransform Badge(Transform parent, float cx, float cy, float size, bool hex, Color ring, Sprite icon, Color iconTint, string label = null)
        {
            var root = Rect(parent, "Badge");
            Place(root, cx - size / 2f, cy - size / 2f, size, size);
            // halo del color del anillo, respirando despacio
            var halo = Img(root, "Halo", new Color(ring.r, ring.g, ring.b, 0.25f), Glow);
            halo.rectTransform.anchorMin = Vector2.zero; halo.rectTransform.anchorMax = Vector2.one;
            halo.rectTransform.offsetMin = new Vector2(-size * 0.28f, -size * 0.28f); halo.rectTransform.offsetMax = new Vector2(size * 0.28f, size * 0.28f);
            var pulse = halo.gameObject.AddComponent<UiPulse>(); pulse.Target = halo; pulse.Min = 0.08f; pulse.Max = 0.3f; pulse.Speed = 1.6f; pulse.Phase = cx * 0.013f + cy * 0.007f;
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
        private bool _over;
        private float _flash;

        public void OnPointerClick(PointerEventData e)
        {
            if (!Enabled || e.button != PointerEventData.InputButton.Left) return;
            UiKit.Sound(EUISoundType.ButtonClick);
            _flash = 1f;                                                           // destello al pulsar
            if (OnClick != null) OnClick();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Enabled) return;
            _over = true;
            UiShine.On(transform).Play();                                          // un brillo cruza el boton
            UiKit.Sound(EUISoundType.ButtonOver);
        }

        public void OnPointerExit(PointerEventData e) { _over = false; }

        // el color pasa suavemente de normal a resaltado (antes cambiaba de golpe)
        private void Update()
        {
            if (Target == null) return;
            var to = _over && Enabled ? Hover : Normal;
            if (_flash > 0f) { to = Color.Lerp(to, new Color(Hover.r, Hover.g, Hover.b, Mathf.Min(1f, Hover.a + 0.25f)), _flash); _flash = Mathf.Max(0f, _flash - Time.unscaledDeltaTime * 4f); }
            Target.color = Color.Lerp(Target.color, to, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
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
