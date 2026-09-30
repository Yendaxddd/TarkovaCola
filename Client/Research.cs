using System;
using System.Linq;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarkovaCola.Client
{
    // Boton "RESEARCH" en la barra inferior de EFT, a la derecha del de Handbook. Se construye desde cero (clonar el boton
    // original arrastraba layouts, animadores y scripts de localizacion que dejaban el texto inactivo): aqui se copian solo
    // su fondo, fuente, tamano y color, y el boton queda fuera del reparto automatico de la barra para no apretar a los demas.
    [HarmonyLib.HarmonyPatch(typeof(MenuTaskBar), nameof(MenuTaskBar.Awake))]
    internal static class MenuButtonPatch
    {
        private const string ButtonName = "TarkovaCola_Research";

        [HarmonyLib.HarmonyPostfix]
        private static void Postfix(MenuTaskBar __instance)
        {
            try { Create(__instance); }
            catch (Exception e) { Plugin.Log.LogError("No se pudo crear el boton Research: " + e); }
        }

        private static void Create(MenuTaskBar bar)
        {
            var buttons = bar._toggleButtons;
            if (buttons == null || buttons.Count == 0) { Plugin.Log.LogWarning("MenuTaskBar sin botones"); return; }

            AnimatedToggle src;
            if (!buttons.TryGetValue(EMenuType.Handbook, out src)) src = buttons.Values.First();
            var parent = src.transform.parent as RectTransform;
            if (parent == null || parent.Find(ButtonName) != null) return;     // ya creado

            // aspecto del original
            var srcBg = src.targetGraphic as Image;
            var srcText = src.GetComponentInChildren<TMP_Text>(true);
            float fontSize = srcText != null ? srcText.fontSize : 16f;
            Color textColor = srcText != null ? srcText.color : new Color(0.77f, 0.76f, 0.70f, 1f);
            var font = srcText != null ? srcText.font : null;
            string label = L.T("btn.research");

            var root = new GameObject(ButtonName, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;     // fuera del reparto de la barra

            // fondo (copia el del original si existe)
            var bg = root.gameObject.AddComponent<Image>();
            if (srcBg != null) { bg.sprite = srcBg.sprite; bg.type = srcBg.type; bg.material = srcBg.material; bg.color = srcBg.color; }
            else bg.color = new Color(0f, 0f, 0f, 0.35f);
            bg.raycastTarget = true;
            Color normal = bg.color, hover = new Color(Mathf.Min(1f, normal.r + 0.12f), Mathf.Min(1f, normal.g + 0.12f), Mathf.Min(1f, normal.b + 0.12f), Mathf.Max(normal.a, 0.5f));

            // texto
            var tgo = new GameObject("Label", typeof(RectTransform));
            tgo.transform.SetParent(root, false);
            var text = tgo.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.color = textColor;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.text = label;
            float textW = text.GetPreferredValues(label).x;

            // icono cuadrado a la izquierda del texto
            float h = Mathf.Max(srcBgHeight(src), 24f), icon = Mathf.Round(h * 0.62f), pad = 12f;
            var igo = new GameObject("Icon", typeof(RectTransform));
            igo.transform.SetParent(root, false);
            var img = igo.AddComponent<Image>();
            img.sprite = Hud.ResearchSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var irt = (RectTransform)igo.transform;
            irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f); irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(pad, 0f); irt.sizeDelta = new Vector2(icon, icon);

            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(pad + icon + 8f, 0f); trt.offsetMax = new Vector2(-pad, 0f);

            float width = pad + icon + 8f + textW + pad;
            var dock = root.gameObject.AddComponent<ResearchDock>();
            dock.Source = src.GetComponent<RectTransform>();
            dock.Width = width;

            var btn = root.gameObject.AddComponent<UiButton>();
            btn.Target = bg; btn.Normal = normal; btn.Hover = hover;
            btn.OnClick = () => { Dbg.Log("MENU", "clic en el boton Research"); ResearchScreen.Toggle(); };

            Dbg.Log("MENU", "boton Research creado junto a " + src.name + " (ancho " + width.ToString("0") + ", fuente " + (font != null ? font.name : "?") + " " + fontSize + ")");
        }

        private static float srcBgHeight(AnimatedToggle src)
        {
            var rt = src.GetComponent<RectTransform>();
            return rt != null ? rt.rect.height : 28f;
        }
    }

    // Coloca el boton justo a la derecha del original (mismo anclaje y altura). Todo en coordenadas del mundo, para no
    // depender de como esten anclados los botones de cada pantalla (lobby, hideout...).
    internal class ResearchDock : MonoBehaviour
    {
        internal RectTransform Source;
        internal float Width = 140f;
        private const float Gap = 8f;

        private readonly Vector3[] _c = new Vector3[4];
        private int _frames;

        private void LateUpdate()
        {
            var rt = transform as RectTransform;
            if (Source == null || rt == null) return;

            Source.GetWorldCorners(_c);                                   // 0=abajo-izq 1=arriba-izq 2=arriba-der 3=abajo-der
            float scale = Source.lossyScale.x;
            float srcRight = _c[3].x, midY = (_c[0].y + _c[1].y) * 0.5f, height = _c[1].y - _c[0].y;
            rt.anchorMin = Source.anchorMin; rt.anchorMax = Source.anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Source.rect.height);

            // 1) busca un hueco libre en la barra donde quepa el boton (el mas cercano a la derecha de Handbook)
            float need = (Width + 2f * Gap) * scale, left = float.NaN;
            var parent = transform.parent as RectTransform;
            var spans = new System.Collections.Generic.List<Vector2>();      // (xMin, xMax) de cada boton visible de la barra
            if (parent != null)
                foreach (RectTransform ch in parent)
                {
                    if (ch == rt || !ch.gameObject.activeInHierarchy || ch.rect.width < 2f) continue;
                    ch.GetWorldCorners(_c);
                    // solo los que estan en la misma fila que Handbook (misma altura aproximada)
                    if (Mathf.Abs((_c[0].y + _c[1].y) * 0.5f - midY) > height * 0.6f) continue;
                    spans.Add(new Vector2(_c[0].x, _c[3].x));
                }
            spans.Sort((a, b) => a.x.CompareTo(b.x));
            float best = float.MaxValue;
            for (int i = 0; i + 1 < spans.Count; i++)
            {
                float gapL = spans[i].y, gapR = spans[i + 1].x;
                if (gapR - gapL < need) continue;
                float dist = Mathf.Abs(gapL - srcRight);
                if (gapL >= srcRight - 1f) dist -= 1000f * scale;              // prefiere los huecos a la derecha de Handbook
                if (dist < best) { best = dist; left = gapL; }
            }

            float x, y = midY;
            if (!float.IsNaN(left)) x = left + (Gap + Width * 0.5f) * scale;
            else
            {
                // 2) sin huecos: justo ENCIMA de la barra, alineado con Handbook (no tapa a ningun boton)
                x = srcRight + (Gap + Width * 0.5f) * scale;
                y = midY + (height + 4f * scale);
            }
            rt.position = new Vector3(x, y, Source.position.z);

            if (_frames++ == 10)
                Dbg.Log("MENU", "dock: original tam=" + Source.rect.size + " | huecos en la barra=" + Mathf.Max(0, spans.Count - 1) + " | " +
                                (float.IsNaN(left) ? "sin hueco: encima de la barra" : "en un hueco") + " | centro=" + rt.position);
        }
    }
}
