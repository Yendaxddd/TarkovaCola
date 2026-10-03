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

        private static Texture2D IconTex(string perkId)
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

        // Se llama cada frame: detecta si el menu de pausa (ESC) de la raid esta abierto.
        internal static void TickRaidPanel()
        {
            bool open = false;
            if (Plugin.CfgRaidPanel.Value && Plugin.InRaid)
            {
                var sm = EftScreenManager.Instance;
                if (sm != null)
                {
                    var cur = sm.CurrentScreenController != null ? (object)sm.CurrentScreenController.ScreenType : null;
                    if (!Equals(cur, _lastScreen)) { _lastScreen = cur; Dbg.Log("PANTALLA", "pantalla en raid: " + cur); }
                    open = sm.CheckCurrentScreen(EEftScreenType.MainMenu);
                }
            }
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

            if (!_escOpen) DrawPermanentIcons(scale);
            if (Plugin.CfgShowNotice.Value) DrawNotice(scale);
        }

        // un icono por cada perk activa, apilados hacia arriba desde la posicion configurada
        private static void DrawPermanentIcons(float scale)
        {
            int i = 0;
            foreach (var perk in Perks.ActivePerks)
            {
                Texture2D tex;
                if (!_icons.TryGetValue(perk.Id, out tex) || tex == null) continue;
                float size = Plugin.CfgIconSize.Value * scale;
                float x = Plugin.CfgIconX.Value * scale;
                float y = Screen.height - size - Plugin.CfgIconY.Value * scale - i * (size + 4f * scale);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(x, y, size, size), tex, ScaleMode.ScaleToFit, true);     // sin marco: pegado al borde de la pantalla
                i++;
            }
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
            float y = Screen.height * 0.16f - slide;

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
            _title.normal.textColor = new Color(Green.r, Green.g, Green.b, alpha);
            _sub.normal.textColor = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(tx, y + 8f * scale, w - (tx - x), 40f * scale), _noticeTitle, _title);
            GUI.Label(new Rect(tx, y + 46f * scale, w - (tx - x), 32f * scale), _noticeSub, _sub);
            GUI.color = Color.white;
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
