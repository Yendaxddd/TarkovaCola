using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Herramienta de autor: graba unos segundos de la pantalla y los junta en UNA hoja de fotogramas
    // (BepInEx/plugins/TarkovaCola/clips/<fecha>/hoja.png + datos.txt) para revisar animaciones fotograma a fotograma.
    // Cada fotograma lleva su tiempo en segundos (tiempo real, no el de camara lenta). Lo que sale en pantalla se graba tal cual,
    // asi que el panel de depuracion del cuerpo a cuerpo (F12 > Herramientas) queda dentro de la captura.
    internal static class ClipCapture
    {
        private const int TileW = 480, TileH = 270, Cols = 4;
        private static bool _busy;
        private static float _savedScale = 1f;
        private static readonly List<string> _states = new List<string>();

        internal static bool Recording { get { return _busy; } }

        internal static void Tick()
        {
            if (!_busy && Input.GetKeyDown(Plugin.CfgClipKey.Value)) Plugin.Instance.StartCoroutine(Record());
        }

        private static IEnumerator Record()
        {
            _busy = true;
            float seconds = Plugin.CfgClipSeconds.Value, fps = Plugin.CfgClipFps.Value, slow = Plugin.CfgClipSlow.Value;
            int count = Mathf.Max(1, Mathf.RoundToInt(seconds * fps));
            float step = 1f / fps;
            var tiles = new List<Texture2D>(count);
            var times = new List<float>(count);
            _states.Clear();
            _savedScale = Time.timeScale;
            Time.timeScale = Mathf.Clamp(slow, 0.05f, 1f);
            Hud.Notify(L.T("clip.title"), L.F("clip.start", seconds.ToString("0.#"), count));
            float t0 = Time.realtimeSinceStartup, next = t0;
            for (int i = 0; i < count; i++)
            {
                while (Time.realtimeSinceStartup < next) yield return null;
                yield return new WaitForEndOfFrame();
                try
                {
                    var full = ScreenCapture.CaptureScreenshotAsTexture();
                    tiles.Add(Shrink(full));
                    UnityEngine.Object.Destroy(full);
                    times.Add(Time.realtimeSinceStartup - t0);
                    _states.Add(MeleeStyle.Snapshot());
                }
                catch (Exception e) { Plugin.Log.LogWarning("Captura de clip: " + e.Message); break; }
                next += step;
            }
            Time.timeScale = _savedScale;
            try { Save(tiles, times, fps, slow); }
            catch (Exception e) { Plugin.Log.LogError("Captura de clip (guardar): " + e); }
            foreach (var t in tiles) UnityEngine.Object.Destroy(t);
            _busy = false;
        }

        private static Texture2D Shrink(Texture2D src)
        {
            var rt = RenderTexture.GetTemporary(TileW, TileH, 0);
            if (Plugin.CfgClipHands.Value) Graphics.Blit(src, rt, new Vector2(0.6f, 0.6f), new Vector2(0.4f, 0f));   // solo la zona de las manos (abajo a la derecha)
            else Graphics.Blit(src, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var res = new Texture2D(TileW, TileH, TextureFormat.RGB24, false);
            res.ReadPixels(new Rect(0, 0, TileW, TileH), 0, 0); res.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            return res;
        }

        private static void Save(List<Texture2D> tiles, List<float> times, float fps, float slow)
        {
            if (tiles.Count == 0) return;
            string dir = Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "clips"), DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(dir);
            int rows = (tiles.Count + Cols - 1) / Cols;
            var sheet = new Texture2D(TileW * Cols, TileH * rows, TextureFormat.RGB24, false);
            sheet.SetPixels(FillArray(new Color(0.08f, 0.09f, 0.11f), sheet.width * sheet.height));
            for (int i = 0; i < tiles.Count; i++)
            {
                int x = (i % Cols) * TileW, y = (rows - 1 - i / Cols) * TileH;
                sheet.SetPixels(x, y, TileW, TileH, tiles[i].GetPixels());
                DrawNumber(sheet, x + 6, y + TileH - 22, i + "  " + times[i].ToString("0.00"));
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(dir, "hoja.png"), sheet.EncodeToPNG());
            UnityEngine.Object.Destroy(sheet);

            var sb = new StringBuilder();
            sb.AppendLine("fotogramas: " + tiles.Count + "  (" + fps + " por segundo, camara lenta x" + slow + ")");
            sb.AppendLine("orden: izquierda a derecha, arriba a abajo; cada uno lleva su numero y su tiempo");
            for (int i = 0; i < times.Count; i++) sb.AppendLine(i + "\t" + times[i].ToString("0.000") + " s	" + (i < _states.Count ? _states[i] : ""));
            File.WriteAllText(Path.Combine(dir, "datos.txt"), sb.ToString());
            Hud.Notify(L.T("clip.title"), L.F("clip.done", tiles.Count, dir));
            Dbg.Log("CLIP", "guardado en " + dir);
        }

        private static Color[] FillArray(Color c, int n) { var a = new Color[n]; for (int i = 0; i < n; i++) a[i] = c; return a; }

        // ---- numeros de 3x5 pixeles ampliados x3 (sin depender de fuentes) ----
        private static readonly string[] Glyphs =
        {
            "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
            "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111",
            "000000000000010", "000000000000000",    // '.', ' '
            "110101110100100", "000000000000000"      // 's' aproximada, reservado
        };

        private static void DrawNumber(Texture2D t, int x, int y, string s)
        {
            const int k = 3;
            int cx = x;
            foreach (char ch in s)
            {
                int g = ch >= '0' && ch <= '9' ? ch - '0' : ch == '.' ? 10 : ch == 's' ? 12 : 11;
                string bits = Glyphs[g];
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                        if (bits[row * 3 + col] == '1')
                            for (int dy = 0; dy < k; dy++)
                                for (int dx = 0; dx < k; dx++)
                                {
                                    int px = cx + col * k + dx, py = y + (4 - row) * k + dy;
                                    if (px >= 0 && py >= 0 && px < t.width && py < t.height) t.SetPixel(px, py, new Color(1f, 0.9f, 0.2f));
                                }
                cx += 4 * k;
            }
        }
    }
}
