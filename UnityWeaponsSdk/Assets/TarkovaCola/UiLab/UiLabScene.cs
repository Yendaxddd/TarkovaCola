using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarkovaCola.Client
{
    // Banco de pruebas de la interfaz: una pantalla de Research de ejemplo hecha con el UiKit/UiFx del mod (copias en Src/, con el reloj
    // simulado de LabTime), para ver el estilo y sus animaciones sin abrir el juego.
    public static class UiLabScene
    {
        private static readonly string[] Perks = { "speedcola", "staminup", "juggernog", "phdflopper", "widowswine", "deadshot", "bandolier", "vultureaid", "meleemacchiato" };
        private static readonly string[] Names = { "Speed Cola", "Stamin-Up", "Juggernog", "PHD Flopper", "Widow's Wine", "Deadshot Daiquiri", "Bandolier Bandit", "Vulture Aid", "Melee Macchiato" };

        public static GameObject Build(Camera cam, string iconDir, int selected, out UiButton hoverMe)
        {
            UiKit.EnsureAssets();
            var go = new GameObject("Lab");
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var t = go.transform;
            Color pc = UiKit.PerkColor(Perks[selected]);

            // ---- fondo (el mismo que ResearchScreen.Build)
            UiKit.Stretch(UiKit.Img(t, "Dim", UiKit.C(3, 8, 13, 0.94f)).rectTransform);
            var glow = UiKit.Img(t, "PerkGlow", new Color(pc.r, pc.g, pc.b, 0.16f), UiKit.Glow);
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.68f, 0.55f); glow.rectTransform.sizeDelta = new Vector2(1620, 1620);
            glow.gameObject.AddComponent<UiDrift>().Amp = new Vector2(115, 54);
            var cold = UiKit.Img(t, "ColdGlow", UiKit.C(40, 140, 220, 0.07f), UiKit.Glow);
            cold.rectTransform.anchorMin = cold.rectTransform.anchorMax = new Vector2(0.12f, 0.2f); cold.rectTransform.sizeDelta = new Vector2(1300, 1300);
            var bub = UiKit.Rect(t, "Bubbles"); UiKit.Stretch(bub); bub.gameObject.AddComponent<UiBubbles>();
            var scan = UiKit.Rect(t, "Scan"); UiKit.Stretch(scan);
            var raw = scan.gameObject.AddComponent<RawImage>(); raw.texture = UiKit.Scan; raw.uvRect = new Rect(0, 0, 1, 360); scan.gameObject.AddComponent<UiScan>().Img = raw;
            UiKit.Stretch(UiKit.Img(t, "Vignette", UiKit.C(0, 0, 0, 0.55f), UiKit.Vignette).rectTransform);

            var content = UiKit.Rect(t, "Content"); content.anchorMin = content.anchorMax = new Vector2(.5f, .5f); content.sizeDelta = new Vector2(1920, 1080);

            // ---- cabecera: titulo y pestanas
            Txt(content, "RESEARCH", 60, 38, 500, 50, 40, UiKit.White, true);
            string[] tabs = { "PROGRESS", "EQUIP", "SYNERGY", "APEX" };
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = i == 0;
                var p = UiKit.Panel(content, 540 + i * 130, 40, 126, 50, on ? UiKit.C(31, 138, 76, 0.85f) : UiKit.C(18, 34, 50, 0.9f), on ? UiKit.C(109, 240, 160, 0.9f) : UiKit.Line);
                Txt(p.transform, tabs[i], 0, 14, 126, 24, 18, on ? UiKit.White : UiKit.Dim, true, TextAlignmentOptions.Top);
                UiButton.Make(content, 540 + i * 130, 40, 126, 50, null, UiKit.C(255, 255, 255, 0f), UiKit.C(255, 255, 255, 0.06f));
            }

            // ---- lista de perks (como BuildLeft)
            hoverMe = null;
            for (int i = 0; i < Perks.Length; i++)
            {
                bool sel = i == selected; float y = 150 + i * 76;
                var btn = UiKit.Panel(content, 60, y, 320, 64, sel ? UiKit.C(31, 138, 76, 0.85f) : UiKit.C(18, 34, 50, 0.9f), sel ? UiKit.C(109, 240, 160, 0.9f) : UiKit.Line);
                var icon = Icon(iconDir, Perks[i]);
                if (icon != null) UiKit.Place(UiKit.Img(btn.transform, "Icon", Color.white, icon).rectTransform, 12, 12, 40, 40);
                Txt(btn.transform, Names[i], 66, 20, 180, 26, 20, sel ? UiKit.White : UiKit.Dim, true);
                Txt(btn.transform, (100 - i * 9) + "%", 240, 22, 70, 22, 16, sel ? UiKit.C(234, 255, 241) : UiKit.Dim, false, TextAlignmentOptions.TopRight);
                var b = UiButton.Make(content, 60, y, 320, 64, null, UiKit.C(255, 255, 255, 0f), UiKit.C(255, 255, 255, 0.06f));
                if (i == 3) hoverMe = b;
            }

            // ---- cabecera de la perk (como BuildHead)
            var stage = UiKit.Rect(content, "Stage"); UiKit.Place(stage, 420, 120, 1440, 930);
            UiKit.Panel(stage, 0, 0, 100, 100, UiKit.C(0, 0, 0, 0), UiKit.C(120, 190, 225, 0.5f), 2f);
            var big = Icon(iconDir, Perks[selected]); if (big != null) UiKit.Place(UiKit.Img(stage, "PerkIcon", Color.white, big).rectTransform, 5, 5, 90, 90);
            Txt(stage, Names[selected], 124, -2, 800, 54, 46, UiKit.White, true);
            Txt(stage, "Reload speed, fast hands and a taste of lime. Every level unlocks a new upgrade.", 124, 54, 1000, 44, 16, UiKit.Text, false);
            Txt(stage, "LEVEL 4", 124, 104, 300, 20, 14, UiKit.Green, true);
            Txt(stage, "6 120 / 9 000 XP", 124, 104, 560, 20, 14, UiKit.Dim, false, TextAlignmentOptions.TopRight);
            var bar = UiKit.Img(stage, "BarBack", UiKit.C(255, 255, 255, 0.10f)); UiKit.Place(bar.rectTransform, 124, 126, 560, 10);
            var fill = UiKit.Img(bar.transform, "Fill", UiKit.C(31, 174, 93)); UiKit.Place(fill.rectTransform, 0, 0, 560 * 0.68f, 10);

            // ---- arbol de mejoras (insignias) y tarjeta de detalle
            var tree = UiKit.Panel(stage, 0, 170, 900, 740, UiKit.C(10, 20, 30, 0.6f), UiKit.Line);
            string[] cls = { "am", "an", "dm", "dn", "sp" };
            var rnd = new System.Random(4);
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                {
                    float cx = 130 + c * 210, cy = 110 + r * 170; string k = cls[(r + c) % cls.Length];
                    if (c < 3) UiKit.Place(UiKit.Img(tree.transform, "Link", UiKit.C(120, 190, 225, 0.25f)).rectTransform, cx + 40, cy - 1, 130, 2);
                    var ic = Icon(iconDir, Perks[(r * 4 + c) % Perks.Length]);
                    UiKit.Badge(tree.transform, cx, cy, k == "am" || k == "dm" ? 84 : 68, k == "am" || k == "dm", UiKit.ClassColor(k), ic, Color.white);
                }
            var card = UiKit.Panel(stage, 930, 170, 510, 740, UiKit.Panel2, new Color(pc.r, pc.g, pc.b, 0.75f), 2f);
            Txt(card.transform, "STEADY HANDS", 24, 22, 460, 40, 28, UiKit.White, true);
            Txt(card.transform, "MAJOR UPGRADE", 24, 62, 460, 20, 14, UiKit.Gold, true);
            Txt(card.transform, "Reloads are 25% faster while you are not aiming, and swapping weapons no longer interrupts the reload.", 24, 100, 460, 120, 18, UiKit.Text, false, TextAlignmentOptions.TopLeft, true);
            var eq = UiKit.Panel(card.transform, 24, 650, 462, 60, UiKit.C(31, 138, 76, 0.9f), UiKit.C(109, 240, 160, 0.9f), 2f);
            Txt(eq.transform, "EQUIP", 0, 16, 462, 30, 22, UiKit.White, true, TextAlignmentOptions.Top);
            UiButton.Make(card.transform, 24, 650, 462, 60, null, UiKit.C(255, 255, 255, 0f), UiKit.C(255, 255, 255, 0.08f));
            return go;
        }

        // avanza el reloj y llama a Start/Update de todos los componentes (en batch no hay bucle de juego)
        private static readonly HashSet<MonoBehaviour> _started = new HashSet<MonoBehaviour>();
        public static void Step(GameObject root, float seconds)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds / LabTime.Dt));
            for (int i = 0; i < n; i++)
            {
                LabTime.Now += LabTime.Dt;
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true).ToList())
                {
                    if (mb == null) continue;
                    var type = mb.GetType();
                    if (!(type.Namespace ?? "").StartsWith("TarkovaCola")) continue;
                    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                    if (_started.Add(mb)) type.GetMethod("Start", F)?.Invoke(mb, null);
                    type.GetMethod("Update", F)?.Invoke(mb, null);
                }
            }
        }

        private static Sprite Icon(string dir, string id)
        {
            var path = Path.Combine(dir, id + "_icon.png");
            if (!File.Exists(path)) return null;
            var tex = new Texture2D(2, 2); tex.LoadImage(File.ReadAllBytes(path));
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f);
        }

        private static TextMeshProUGUI Txt(Transform p, string s, float x, float y, float w, float h, float size, Color c, bool bold, TextAlignmentOptions a = TextAlignmentOptions.TopLeft, bool wrap = false)
        {
            var t = UiKit.Txt(p, s, size, c, a, wrap, bold); UiKit.Place(t.rectTransform, x, y, w, h); return t;
        }
    }
}
