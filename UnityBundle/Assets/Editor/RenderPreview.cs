using System.IO;
using UnityEditor;
using UnityEngine;

// Renderiza el prefab de Speed Cola en varios giros para elegir la rotacion del icono del inventario.
//   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.RenderPreview.Run
// (sin -nographics: hace falta la GPU para renderizar). Salida: UnityBundle/Preview/yaw_XXX.png
namespace TarkovaCola
{
    public static class RenderPreview
    {
        public static void Run()
        {
            Directory.CreateDirectory("Preview");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/speedcola.prefab");

            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.27f, 0.3f, 1f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 10f;
            cam.orthographicSize = 0.085f;
            camGo.transform.position = new Vector3(0f, 0f, -1f);   // mira hacia +Z
            camGo.transform.rotation = Quaternion.identity;

            var lightGo = new GameObject("light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(30f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

            var rt = new RenderTexture(256, 512, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            foreach (int yaw in new[] { 0, 90, 180, 270 })
            {
                var inst = (GameObject)Object.Instantiate(prefab);
                foreach (var c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 12f);   // como Icon.rotation actual + giro de prueba
                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(256, 512, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 256, 512), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes("Preview/yaw_" + yaw.ToString("000") + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(inst);
            }
            Debug.Log("[TarkovaCola] previews generados");
        }
    }
}
