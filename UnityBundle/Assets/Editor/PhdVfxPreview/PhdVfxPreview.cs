using System.IO;
using UnityEditor;
using UnityEngine;

namespace TarkovaCola
{
    // Previsualiza el efecto visual de PHD Flopper (Client/PhdVfx.cs, copiado aqui por preview_phdvfx.ps1) en una escena de prueba.
    // Salida: Preview/phdvfx_<segundos>.png. Se lanza SIN -nographics (hace falta la GPU):
    //   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.PhdVfxPreview.Run
    public static class PhdVfxPreview
    {
        public static void Run()
        {
            Directory.CreateDirectory("Preview");

            var camGo = new GameObject("PreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.14f);
            cam.fieldOfView = 75f;
            cam.transform.position = new Vector3(0f, 1.7f, -1.2f);       // altura de los ojos, mirando al suelo delante del jugador
            cam.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
            cam.nearClipPlane = 0.05f;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            var gm = new Material(Shader.Find("Standard")) { color = new Color(0.32f, 0.34f, 0.30f) };
            ground.GetComponent<Renderer>().sharedMaterial = gm;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.intensity = 0.8f;

            TarkovaCola.Client.PhdVfx.Play(Vector3.zero, 1f);
            var root = GameObject.Find("TarkovaCola_PHD_VFX");
            if (root == null) { Debug.LogError("PhdVfx.Play no creo el efecto"); EditorApplication.Exit(2); return; }
            var systems = root.GetComponentsInChildren<ParticleSystem>();
            var light = root.GetComponent<Light>();

            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            foreach (float t in new[] { 0.08f, 0.2f, 0.35f, 0.55f, 0.85f })
            {
                foreach (var ps in systems) ps.Simulate(t, false, true, true);
                if (light != null) light.intensity = 3.2f * Mathf.Clamp01(1f - t / 0.4f);
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes("Preview/phdvfx_" + t.ToString("0.00") + ".png", tex.EncodeToPNG());
            }
            Debug.Log("PhdVfxPreview: listo");
        }
    }
}
