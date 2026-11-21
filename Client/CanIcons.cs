using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // El render de iconos de EFT pintaba blancas las latas de Bandolier Bandit y Widow's Wine (la etiqueta no salia). Para esas dos se
    // devuelve un icono ya hecho (assets/<id>_caniconv.png, tools/make_can_icons.py); el modelo 3D no cambia.
    [HarmonyPatch(typeof(ItemIconCreator), nameof(ItemIconCreator.GetItemIcon))]
    internal static class CanIconPatch
    {
        private static readonly Dictionary<string, string> Files = new Dictionary<string, string>
        {
            { "6a1c00000000000000000c07", "bandolier_caniconv.png" }, { "6a1c00000000000000000c05", "widowswine_caniconv.png" },
        };
        private static readonly Dictionary<string, ItemIcon> Cache = new Dictionary<string, ItemIcon>();

        [HarmonyPrefix]
        // La vista del inventario usa el sprite a su tamano real: hay que darlo al tamano que pide EFT (el de la celda, 'size'),
        // como hace su render de iconos; con el png de 256 px la lata se salia de la casilla.
        private static bool Prefix(EFT.InventoryLogic.Item item, in IntVec2 size, ref ItemIcon __result)
        {
            string file;
            if (item == null || !Files.TryGetValue(item.StringTemplateId ?? "", out file)) return true;
            try
            {
                int w = Mathf.Max(8, size.X), h = Mathf.Max(8, size.Y);
                string key = file + "@" + w + "x" + h;
                ItemIcon icon;
                if (!Cache.TryGetValue(key, out icon) || icon.Sprite == null)
                {
                    string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", file));
                    if (!File.Exists(path)) return true;
                    var src = new Texture2D(2, 2, TextureFormat.ARGB32, false) { filterMode = FilterMode.Trilinear };
                    src.LoadImage(File.ReadAllBytes(path));
                    // encaja la lata (cuadrada en el png) en la celda manteniendo la proporcion, centrada
                    var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
                    float s = Mathf.Min((float)w / src.width, (float)h / src.height);
                    float dw = src.width * s, dh = src.height * s;
                    GL.PushMatrix(); GL.LoadPixelMatrix(0, w, h, 0);
                    Graphics.DrawTexture(new Rect((w - dw) * 0.5f, (h - dh) * 0.5f, dw, dh), src);
                    GL.PopMatrix();
                    var tex = new Texture2D(w, h, TextureFormat.ARGB32, false) { filterMode = FilterMode.Trilinear, name = "TC_CanIcon" };
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                    tex.Apply();
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                    Object.Destroy(src);
                    icon = new ItemIcon(IconsHash.GetItemHash(item)) { Sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f) };
                    Cache[key] = icon;
                }
                __result = icon;
                return false;
            }
            catch (System.Exception e) { Dbg.Throttled("canicon", 30f, "ERROR", "icono de lata: " + e.Message); return true; }
        }
    }
}
