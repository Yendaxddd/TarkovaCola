using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TarkovaCola.Editor
{
    // Imagen promocional de la version: las 6 perks nuevas en fila, casi todo en sombra (solo la parte de abajo iluminada), con humo.
    // Unity.exe -batchmode -projectPath UnityBundle -executeMethod TarkovaCola.Editor.ReleaseTeaser.Run  -> Preview/release_teaser.png
    public static class ReleaseTeaser
    {
        private static readonly string[] Ids = { "quickrevive", "mulekick", "electriccherry", "deathperception", "tombstone", "doubletap" };
        private static readonly Color[] Glow =
        {
            new Color(0.35f, 0.75f, 1f), new Color(0.55f, 0.95f, 0.35f), new Color(0.35f, 0.6f, 1f),
            new Color(1f, 0.45f, 0.12f), new Color(0.75f, 0.8f, 0.85f), new Color(1f, 0.75f, 0.2f),
        };
        private const int W = 2560, H = 1440;

        public static void Run()
        {
            var made = new List<Object>();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.025f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.06f; RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.012f);

            float spacing = 0.1f;
            for (int i = 0; i < Ids.Length; i++)
            {
                string id = Ids[i];
                string path = new[] { "Assets/Prefabs/bo6_can_" + id + ".prefab", "Assets/Prefabs/" + id + ".prefab" }.FirstOrDefault(File.Exists);
                if (path == null) { Debug.LogError("[TEASER] falta " + id); continue; }
                var g = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                made.Add(g);
                // altura comun (las latas de distinto origen tienen escalas distintas): se igualan a 0.12 m de alto
                var b = Bounds(g);
                if (b.size.y > 0.0001f) g.transform.localScale *= 0.12f / b.size.y;
                b = Bounds(g);
                float x = (i - (Ids.Length - 1) / 2f) * spacing;
                g.transform.position += new Vector3(x - b.center.x, -b.min.y, -b.center.z);
                g.transform.rotation = Quaternion.Euler(0f, 180f + (i - 2.5f) * 6f, 0f);
                // luz desde el suelo, del color de la perk: solo la base queda iluminada
                var l = new GameObject("glow_" + id).AddComponent<Light>(); made.Add(l.gameObject);
                l.type = LightType.Spot; l.color = Glow[i]; l.range = 0.5f; l.intensity = 11f; l.spotAngle = 70f;
                l.transform.position = new Vector3(x, 0.002f, -0.12f); l.transform.LookAt(new Vector3(x, 0.03f, 0f));
                var p2 = new GameObject("pool_" + id).AddComponent<Light>(); made.Add(p2.gameObject);
                p2.type = LightType.Point; p2.color = Glow[i]; p2.range = 0.12f; p2.intensity = 3f; p2.transform.position = new Vector3(x, 0.01f, -0.04f);
            }
            // suelo mojado y oscuro
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); made.Add(floor);
            floor.transform.localScale = Vector3.one * 0.3f;
            var fm = new Material(Shader.Find("Standard")) { color = new Color(0.05f, 0.05f, 0.055f) };
            fm.SetFloat("_Glossiness", 0.55f); fm.SetFloat("_Metallic", 0.2f);
            floor.GetComponent<Renderer>().sharedMaterial = fm;
            // contraluz muy tenue arriba (solo un filo en el borde de las latas)
            var rim = new GameObject("rim").AddComponent<Light>(); made.Add(rim.gameObject);
            rim.type = LightType.Directional; rim.intensity = 0.25f; rim.color = new Color(0.6f, 0.7f, 0.9f);
            rim.transform.rotation = Quaternion.Euler(-15f, 160f, 0f);

            // humo
            var smoke = Smoke(); made.Add(smoke.gameObject);

            var cam = new GameObject("cam").AddComponent<Camera>(); made.Add(cam.gameObject);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.008f, 0.008f, 0.01f);
            cam.fieldOfView = 24f; cam.nearClipPlane = 0.01f; cam.farClipPlane = 20f;
            cam.transform.position = new Vector3(0f, 0.05f, -1.0f);
            cam.transform.rotation = Quaternion.Euler(0.5f, 0f, 0f);
            cam.allowHDR = false;

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = null;
            Grade(tex);
            Directory.CreateDirectory("Preview");
            File.WriteAllBytes("Preview/release_teaser.png", tex.EncodeToPNG());
            Debug.Log("[TEASER] Preview/release_teaser.png");
            foreach (var o in made) if (o != null) Object.DestroyImmediate(o);
        }

        private static Bounds Bounds(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        private static ParticleSystem Smoke()
        {
            var go = new GameObject("smoke");
            go.transform.position = new Vector3(0f, 0.005f, 0.02f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.duration = 20f; main.loop = true; main.startLifetime = 14f; main.startSpeed = 0.006f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f); main.maxParticles = 600; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.6f, 0.65f, 0.22f), new Color(0.8f, 0.75f, 0.7f, 0.4f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var em = ps.emission; em.rateOverTime = 40f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.9f, 0.01f, 0.3f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.004f, 0.004f); vel.y = new ParticleSystem.MinMaxCurve(0.0005f, 0.002f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = Puff() };
            m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            r.sharedMaterial = m; r.sortingFudge = 0f;
            ps.Simulate(20f, true, true, true);
            return ps;
        }

        // textura de humo: mancha suave con ruido
        private static Texture2D Puff()
        {
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (N - 1f) * 2f - 1f, v = y / (N - 1f) * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float n = Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 0.6f + Mathf.PerlinNoise(x * 0.15f + 7f, y * 0.15f + 3f) * 0.4f;
                    float a = Mathf.Clamp01(1f - d) * Mathf.Clamp01(n * 1.4f - 0.2f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            t.Apply();
            return t;
        }

        // gradacion: la parte de arriba se funde en negro (sombra en el 80% de los cuerpos), vineta, contraste y algo de grano
        private static void Grade(Texture2D t)
        {
            var px = t.GetPixels();
            var rnd = new System.Random(7);
            for (int y = 0; y < H; y++)
            {
                float fy = y / (float)(H - 1);                         // 0 abajo, 1 arriba
                float shade = Mathf.Lerp(1f, 0.05f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.40f, 0.52f, fy)));
                for (int x = 0; x < W; x++)
                {
                    float fx = x / (float)(W - 1) - 0.5f, cy = fy - 0.38f;
                    float vig = Mathf.Clamp01(1f - (fx * fx * 1.6f + cy * cy * 1.2f) * 1.25f);
                    var c = px[y * W + x] * shade * Mathf.Lerp(0.25f, 1f, vig);
                    c = new Color(Mathf.Pow(c.r, 1.12f), Mathf.Pow(c.g, 1.12f), Mathf.Pow(c.b, 1.08f) * 1.03f);
                    float g = ((float)rnd.NextDouble() - 0.5f) * 0.025f;
                    px[y * W + x] = new Color(c.r + g, c.g + g, c.b + g, 1f);
                }
            }
            t.SetPixels(px); t.Apply();
        }
    }
}
