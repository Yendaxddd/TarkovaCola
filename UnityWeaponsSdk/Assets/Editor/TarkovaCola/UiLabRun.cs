using System.IO;
using System.Linq;
using TarkovaCola.Client;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TarkovaCola
{
    // Banco de pruebas de la interfaz del mod:
    //   1) copiar Client/UiKit.cs y Client/UiFx.cs a Assets/TarkovaCola/UiLab/Src (reloj -> LabTime)  [tools/uilab.ps1]
    //   2) Unity.exe -batchmode -quit -projectPath UnityWeaponsSdk -executeMethod TarkovaCola.UiLabRun.Run   (sin -nographics)
    // Salida: Preview/uilab_frames.png (instantes 0.3 / 1.2 / 2.5 / 4 s) y Preview/uilab_hover.png (raton sobre una perk)
    public static class UiLabRun
    {
        public static void Essentials()
        {
            var pkg = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).FirstOrDefault();
            if (pkg != null) { AssetDatabase.ImportPackage(pkg, false); Debug.Log("[UILAB] TMP importado: " + pkg); }
        }

        public static void Run()
        {
            Directory.CreateDirectory("Preview");
            var cam = new GameObject("cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.orthographic = true;
            var rt = new RenderTexture(1920, 1080, 24) { antiAliasing = 4 }; cam.targetTexture = rt;
            string icons = Path.GetFullPath("../Client/assets");
            var root = UiLabScene.Build(cam, icons, 0, out var hover);
            Canvas.ForceUpdateCanvases();

            float[] times = { 0.3f, 1.2f, 2.5f, 4f };
            var sheet = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            float now = 0f;
            for (int i = 0; i < times.Length; i++)
            {
                UiLabScene.Step(root, times[i] - now); now = times[i];
                if (i == 2 && hover != null) hover.OnPointerEnter(new PointerEventData(null));       // el raton entra en una perk
                Canvas.ForceUpdateCanvases(); cam.Render();
                var tile = Grab(rt);
                var small = Scale(tile, 960, 540);
                sheet.SetPixels((i % 2) * 960, (1 - i / 2) * 540, 960, 540, small.GetPixels());
                if (i == 3) File.WriteAllBytes("Preview/uilab_full.png", tile.EncodeToPNG());
            }
            sheet.Apply(); File.WriteAllBytes("Preview/uilab_frames.png", sheet.EncodeToPNG());
            // con el raton encima: a mitad del brillo
            hover.OnPointerEnter(new PointerEventData(null)); UiLabScene.Step(root, 0.25f);
            Canvas.ForceUpdateCanvases(); cam.Render();
            File.WriteAllBytes("Preview/uilab_hover.png", Grab(rt).EncodeToPNG());
            Debug.Log("[UILAB] listo");
        }

        private static Texture2D Grab(RenderTexture rt)
        {
            RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
            return t;
        }

        private static Texture2D Scale(Texture2D src, int w, int h)
        {
            var r = RenderTexture.GetTemporary(w, h); Graphics.Blit(src, r);
            var t = Grab(r); RenderTexture.ReleaseTemporary(r); return t;
        }
    }
}
