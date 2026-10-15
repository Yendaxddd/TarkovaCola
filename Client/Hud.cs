using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT.Counters;
using EFT.UI.Screens;
using UnityEngine;

namespace TarkovaCola.Client
{
    // HUD dibujado con IMGUI: aviso tipo noticia, icono permanente y panel de progreso al abrir el menu (ESC) en raid.
    internal static class Hud
    {
        private static readonly Color Green = new Color(0.35f, 0.9f, 0.4f, 1f);
        private static readonly Color Gold = new Color(0.94f, 0.76f, 0.29f, 1f);

        private static readonly Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> _iconSprites = new Dictionary<string, Sprite>();
        private static Texture2D _researchIcon;
        private static Texture2D _white;
        private static GUIStyle _title, _sub, _small, _label;
        private static Sprite _researchSprite;

        private static float _noticeStart = -100f;
        private static string _noticeTitle = "", _noticeSub = "", _noticePerk;

        private static bool _escOpen;
        private static bool _menuOpen;        // hay una pantalla del juego abierta (inventario, tareas, mapa, ESC...): el HUD no se dibuja encima
        private static float _escOpenedAt;
        private static object _lastScreen;

        // icono de una perk (null si no cargo)
        internal static Sprite IconSprite(string perkId)
        {
            Sprite sp;
            if (_iconSprites.TryGetValue(perkId, out sp) && sp != null) return sp;
            Texture2D tex;
            if (!_icons.TryGetValue(perkId, out tex) || tex == null) return null;
            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            _iconSprites[perkId] = sp;
            return sp;
        }

        internal static Texture2D IconTex(string perkId)
        {
            Texture2D tex;
            if (perkId != null && _icons.TryGetValue(perkId, out tex)) return tex;
            return Perks.All.Length > 0 && _icons.TryGetValue(Perks.All[0].Id, out tex) ? tex : null;
        }

        // icono del boton Research del menu principal (imagen completa, sin margen)
        internal static Sprite ResearchSprite
        {
            get
            {
                if (_researchSprite == null && _researchIcon != null)
                    _researchSprite = Sprite.Create(_researchIcon, new Rect(0, 0, _researchIcon.width, _researchIcon.height), new Vector2(0.5f, 0.5f));
                return _researchSprite;
            }
        }

        private static Texture2D LoadTexture(string pluginDir, string file)
        {
            var path = Path.Combine(pluginDir, Path.Combine("assets", file));
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (File.Exists(path) && ImageConversion.LoadImage(tex, File.ReadAllBytes(path)))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    return tex;
                }
                Plugin.Log.LogWarning("No se pudo cargar la imagen: " + path);
            }
            catch (System.Exception e) { Plugin.Log.LogWarning("Error cargando " + file + ": " + e.Message); }
            return null;
        }

        internal static void Init(string pluginDir)
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();

            foreach (var perk in Perks.All) _icons[perk.Id] = LoadTexture(pluginDir, perk.IconFile);
            _researchIcon = LoadTexture(pluginDir, "research_icon.png");
        }

        // Copia de la textura dentro de un lienzo transparente mayor: los iconos de la barra del juego llevan margen propio,
        // sin el nuestro se ve mucho mas grande que los demas. 'fraction' = parte del lienzo que ocupa la imagen.
        private static Texture2D Padded(Texture2D src, float fraction)
        {
            if (src == null) return null;
            int size = Mathf.RoundToInt(Mathf.Max(src.width, src.height) / fraction);
            var dst = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var clear = new Color[size * size];
            dst.SetPixels(clear);
            dst.SetPixels((size - src.width) / 2, (size - src.height) / 2, src.width, src.height, src.GetPixels());
            dst.Apply();
            return dst;
        }

        internal static void Notify(string title, string sub, string perkId = null)
        {
            _noticeTitle = title;
            _noticeSub = sub;
            _noticePerk = perkId;
            _noticeStart = Time.realtimeSinceStartup;
        }

        // Avisos encadenados: se muestran uno tras otro, cuando termina el que haya en pantalla (p. ej. perk activada -> mejora -> desafio).
        private struct Pending { public float At; public string Title, Sub, Perk; }
        private static readonly List<Pending> _pending = new List<Pending>();
        private static float _queueEnd;

        internal static void Enqueue(string title, string sub, string perkId = null)
        {
            float now = Time.realtimeSinceStartup, dur = Plugin.CfgNoticeSeconds.Value;
            float at = Mathf.Max(now, Mathf.Max(_noticeStart + dur, _queueEnd)) + 0.15f;
            _pending.Add(new Pending { At = at, Title = title, Sub = sub, Perk = perkId });
            _queueEnd = at + dur;
        }

        internal static void ClearQueue() { _pending.Clear(); _queueEnd = 0f; }

        private static void ShowPending()
        {
            if (_pending.Count == 0) return;
            if (_pending[0].At > Time.realtimeSinceStartup) return;
            var p = _pending[0];
            _pending.RemoveAt(0);
            Notify(p.Title, p.Sub, p.Perk);
        }

        // Se llama cada frame: detecta si el menu de pausa (ESC) de la raid esta abierto y si hay alguna pantalla del juego abierta.
        internal static void TickRaidPanel()
        {
            bool open = false, menu = false;
            if (Plugin.InRaid)
            {
                var sm = EftScreenManager.Instance;
                if (sm != null)
                {
                    var ctrl = sm.CurrentScreenController;
                    var cur = ctrl != null ? (object)ctrl.ScreenType : null;
                    if (!Equals(cur, _lastScreen)) { _lastScreen = cur; Dbg.Log("PANTALLA", "pantalla en raid: " + cur); }
                    menu = ctrl != null && ctrl.ScreenType != EEftScreenType.BattleUI;       // BattleUI es el propio HUD de la raid
                    if (Plugin.CfgRaidPanel.Value) open = sm.CheckCurrentScreen(EEftScreenType.MainMenu);
                }
            }
            _menuOpen = menu;
            if (open != _escOpen)
            {
                if (open) { _escOpenedAt = Time.realtimeSinceStartup; PerkService.EnsureLoaded(); }
                Dbg.Log("PANEL", open ? "menu ESC abierto: se muestra el panel" : "menu ESC cerrado: se oculta el panel");
                _escOpen = open;
                RaidPanel.SetOpen(open);          // (antes solo se avisaba al abrir y el panel nunca se ocultaba)
            }
        }

        internal static void Draw()
        {
            if (!Plugin.InRaid) return;

            float scale = Screen.height / 1080f;
            EnsureStyles(scale);

            if (!_escOpen && !_menuOpen) { QuestGuards.DrawIcons(scale); DrawPermanentIcons(scale); Powerup.DrawMarker(scale, _white); }
            if (Event.current != null && Event.current.type == EventType.Repaint) ShowPending();      // (OnGUI se llama varias veces por frame)
            if (Plugin.CfgShowNotice.Value) DrawNotice(scale);
        }

        // ---- contorno de la Eternal Perk: la silueta del icono ensanchada, en blanco (el tinte azul lo pone el dibujo) ----
        private const float OutlineFrac = 0.07f;                      // grosor del contorno, como fraccion del tamano del icono
        private static readonly Dictionary<string, Texture2D> _outlines = new Dictionary<string, Texture2D>();

        private static Texture2D Outline(string perkId)
        {
            Texture2D o;
            if (_outlines.TryGetValue(perkId, out o)) return o;
            Texture2D src;
            o = _icons.TryGetValue(perkId, out src) && src != null ? MakeOutline(src) : null;
            _outlines[perkId] = o;
            return o;
        }

        // Se calcula una sola vez por perk, a una resolucion de como mucho 128 px (un contorno no necesita mas)
        private static Texture2D MakeOutline(Texture2D src)
        {
            int step = Mathf.Max(1, Mathf.Max(src.width, src.height) / 128);
            int w = src.width / step, h = src.height / step;
            int r = Mathf.Max(2, Mathf.RoundToInt(Mathf.Max(w, h) * OutlineFrac));
            int mw = w + 2 * r, mh = h + 2 * r;
            var px = src.GetPixels32();
            var a = new float[mw * mh];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    a[(y + r) * mw + x + r] = px[(y * step) * src.width + x * step].a / 255f;

            // dilatacion circular
            var offs = new List<int[]>();
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r * r) offs.Add(new[] { dx, dy });
            var d = new float[mw * mh];
            for (int y = 0; y < mh; y++)
                for (int x = 0; x < mw; x++)
                {
                    float m = 0f;
                    for (int k = 0; k < offs.Count && m < 1f; k++)
                    {
                        int xx = x + offs[k][0], yy = y + offs[k][1];
                        if (xx < 0 || yy < 0 || xx >= mw || yy >= mh) continue;
                        float v = a[yy * mw + xx];
                        if (v > m) m = v;
                    }
                    d[y * mw + x] = m;
                }

            // suavizado 3x3 para que el borde no se vea escalonado
            var o = new Color32[mw * mh];
            for (int y = 0; y < mh; y++)
                for (int x = 0; x < mw; x++)
                {
                    float s = 0f; int n = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= mw || yy >= mh) continue;
                            s += d[yy * mw + xx]; n++;
                        }
                    o[y * mw + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(s / n) * 255f));
                }
            var tex = new Texture2D(mw, mh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "TC_Outline" };
            tex.SetPixels32(o);
            tex.Apply(false, true);
            return tex;
        }

        // un icono por cada perk activa, apilados hacia arriba desde la posicion configurada
        private static void DrawPermanentIcons(float scale)
        {
            var list = Perks.ActivePerks.Where(p => _icons.ContainsKey(p.Id) && _icons[p.Id] != null).ToList();
            if (list.Count == 0) return;
            float size = Plugin.CfgIconSize.Value * scale, gap = 6f * scale;
            float total = list.Count * size + (list.Count - 1) * gap;
            float x0 = (Screen.width - total) / 2f + Plugin.CfgIconX.Value * scale;          // fila centrada, abajo en el centro de la pantalla
            float y = Screen.height - size - Plugin.CfgIconY.Value * scale;
            for (int i = 0; i < list.Count; i++)
            {
                var perk = list[i];
                float x = x0 + i * (size + gap);
                if (Eternal.Is(perk.Id))                                                                  // Eternal Perk: contorno azul que late despacio
                {
                    var outline = Outline(perk.Id);
                    if (outline != null)
                    {
                        float pad = size * OutlineFrac;
                        GUI.color = new Color(0.25f, 0.65f, 1f, 0.8f + 0.2f * Mathf.Sin(Time.realtimeSinceStartup * 3f));
                        GUI.DrawTexture(new Rect(x - pad, y - pad, size + pad * 2f, size + pad * 2f), outline, ScaleMode.ScaleToFit, true);
                    }
                }
                GUI.color = Perks.IsModifier(perk.Id) ? new Color(1f, 1f, 1f, 0.55f) : Color.white;       // el modificador (Classic) se ve atenuado
                GUI.DrawTexture(new Rect(x, y, size, size), _icons[perk.Id], ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        private static void DrawNotice(float scale)
        {
            float t = Time.realtimeSinceStartup - _noticeStart;
            float dur = Plugin.CfgNoticeSeconds.Value;
            if (t < 0f || t > dur) return;

            float alpha = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((dur - t) / 0.6f);
            float slide = (1f - Mathf.Clamp01(t / 0.3f)) * 30f * scale; // entra deslizando desde arriba

            float h = 84f * scale, w = 460f * scale;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height * 0.24f - slide;       // por debajo de la brujula, que ocupa la parte alta

            GUI.color = new Color(0f, 0f, 0f, 0.65f * alpha);
            GUI.DrawTexture(new Rect(x, y, w, h), _white);
            GUI.color = new Color(Green.r, Green.g, Green.b, alpha);
            GUI.DrawTexture(new Rect(x, y, 4f * scale, h), _white); // barra de acento

            float pad = 10f * scale;
            float iconSize = h - pad * 2f;
            var noticeIcon = IconTex(_noticePerk);
            if (noticeIcon != null)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(x + pad + 6f * scale, y + pad, iconSize, iconSize), noticeIcon, ScaleMode.ScaleToFit, true);
            }

            float tx = x + pad * 2f + iconSize + 6f * scale;
            float textW = w - (tx - x) - pad * 0.5f;
            _title.normal.textColor = new Color(Green.r, Green.g, Green.b, alpha);
            _sub.normal.textColor = new Color(1f, 1f, 1f, alpha);

            // textos largos (descripciones de mejoras, nombres de desafios): el titulo se reduce hasta caber; el subtitulo se reduce y,
            // si aun asi no cabe, pasa a dos lineas
            _title.fontSize = FitSize(_title, _noticeTitle, textW, Mathf.RoundToInt(28f * scale), Mathf.RoundToInt(17f * scale));
            int subSize = FitSize(_sub, _noticeSub, textW, Mathf.RoundToInt(20f * scale), Mathf.RoundToInt(14f * scale));
            _sub.fontSize = subSize;
            bool wrap = _sub.CalcSize(new GUIContent(_noticeSub)).x > textW;
            _sub.wordWrap = wrap;
            GUI.Label(new Rect(tx, y + 8f * scale, textW, 40f * scale), _noticeTitle, _title);
            GUI.Label(new Rect(tx, y + (wrap ? 42f : 46f) * scale, textW, (wrap ? 40f : 32f) * scale), _noticeSub, _sub);
            _sub.wordWrap = false;
            GUI.color = Color.white;
        }

        // Tamano de fuente mas grande (entre min y max) con el que el texto cabe en 'width' en una linea; 'min' si ni asi cabe.
        private static int FitSize(GUIStyle style, string text, float width, int max, int min)
        {
            int saved = style.fontSize;
            style.fontSize = max;
            float w = style.CalcSize(new GUIContent(text)).x;
            style.fontSize = saved;
            if (w <= width || w <= 0f) return max;
            return Mathf.Max(min, Mathf.FloorToInt(max * width / w));
        }

        private static void SetAlpha(float a)
        {
            _title.normal.textColor = new Color(Green.r, Green.g, Green.b, a);
            _label.normal.textColor = new Color(0.7f, 0.8f, 0.88f, a);
        }

        private static void EnsureStyles(float scale)
        {
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
                _sub = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
                _small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
                _label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            }
            _title.fontSize = Mathf.RoundToInt(28f * scale);
            _sub.fontSize = Mathf.RoundToInt(20f * scale);
            _small.fontSize = Mathf.RoundToInt(15f * scale);
            _label.fontSize = Mathf.RoundToInt(16f * scale);
        }
    }
}
