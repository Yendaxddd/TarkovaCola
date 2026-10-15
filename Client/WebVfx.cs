using System;
using System.Collections.Generic;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Efecto visual de Widow's Wine: una telarana morada se extiende por el suelo donde tu granada atrapa enemigos, y cada enemigo
    // atrapado queda con una telarana pequena bajo los pies mientras dure el efecto. Todo en codigo: la textura de la telarana se
    // dibuja una vez (radios y anillos) y se usa en sistemas de particulas planos sobre el suelo.
    internal static class WebVfx
    {
        private static Texture2D _web;
        private static Material _mat, _glowMat;
        private static bool _failed;
        private static readonly HashSet<int> _burst = new HashSet<int>();

        internal static void OnRaidStart()
        {
            _burst.Clear();
            if (Plugin.CfgWebVfx.Value && !_failed)                     // la textura se dibuja al cargar la raid, no en mitad de un combate
                try { Build(); } catch (Exception e) { _failed = true; Plugin.Log.LogWarning("Widow's Wine: efecto de telarana: " + e.Message); }
        }

        // Telarana grande en el sitio de la granada (una sola vez por granada)
        internal static void Burst(int key, Vector3 pos, float radius, float seconds)
        {
            if (key != 0 && !_burst.Add(key)) return;
            Spawn(pos, radius * 2f, seconds, true);
        }

        // Telarana pequena bajo un enemigo atrapado
        internal static void OnVictim(Vector3 pos, float seconds)
        {
            Spawn(pos, 2.4f, seconds, false);
        }

        private static void Spawn(Vector3 pos, float size, float seconds, bool big)
        {
            if (_failed || !Plugin.CfgWebVfx.Value) return;
            try
            {
                Build();
                var root = new GameObject("TarkovaCola_Web");
                root.transform.position = Ground(pos) + Vector3.up * 0.05f;

                var ps = NewSystem(root.transform, "Web", _mat);
                var main = ps.main;
                main.duration = seconds; main.loop = false;
                main.startLifetime = seconds; main.startSpeed = 0f;
                main.startSize = size;
                main.startRotation = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                main.startColor = Color.white;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 2;
                var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                var shape = ps.shape; shape.enabled = false;
                // se abre deprisa y se mantiene; al final se desvanece
                var sz = ps.sizeOverLifetime; sz.enabled = true;
                float open = Mathf.Clamp01(0.35f / Mathf.Max(0.5f, seconds));
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.1f, 0f, 6f), new Keyframe(open, 1f), new Keyframe(1f, 1f)));
                var col = ps.colorOverLifetime; col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(new Color(0.92f, 0.85f, 1f), 0f), new GradientColorKey(new Color(0.75f, 0.55f, 0.95f), 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, open), new GradientAlphaKey(0.85f, 0.8f), new GradientAlphaKey(0f, 1f) });
                col.color = new ParticleSystem.MinMaxGradient(g);
                ps.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                ps.Play();

                if (big) Glow(root.transform, size);
                UnityEngine.Object.Destroy(root, seconds + 0.5f);
            }
            catch (Exception e)
            {
                _failed = true;                     // solo es cosmetico: si falla no se reintenta
                Plugin.Log.LogWarning("Widow's Wine: no se pudo crear el efecto de telarana: " + e.Message);
            }
        }

        // destello morado breve al abrirse la telarana grande
        private static void Glow(Transform parent, float size)
        {
            var ps = NewSystem(parent, "Glow", _glowMat);
            var main = ps.main;
            main.duration = 0.6f; main.loop = false;
            main.startLifetime = 0.6f; main.startSpeed = 0f;
            main.startSize = size * 0.8f;
            main.startColor = new Color(0.7f, 0.3f, 1f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ps.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            ps.Play();
        }

        // el suelo bajo el punto (la granada puede estar en el aire o los pies del enemigo algo por encima)
        private static Vector3 Ground(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 3f, LayersMaskController.HighPolyWithTerrainMask, QueryTriggerInteraction.Ignore)) return hit.point;
            return p;
        }

        private static ParticleSystem NewSystem(Transform parent, string name, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = mat;
            return ps;
        }

        private static void Build()
        {
            if (_mat != null) return;
            _web = WebTexture(256);
            Shader blend = null;
            foreach (var n in new[] { "Legacy Shaders/Particles/Alpha Blended", "Particles/Alpha Blended", "Mobile/Particles/Alpha Blended", "Sprites/Default" })
                if ((blend = Shader.Find(n)) != null) break;
            if (blend == null) blend = PhdVfx.FindShader();
            _mat = new Material(blend) { mainTexture = _web, name = "TC_Web" };
            _glowMat = new Material(PhdVfx.FindShader()) { mainTexture = PhdVfx.MakeTexture(64, r => Mathf.Pow(Mathf.Clamp01(1f - r), 2f)), name = "TC_WebGlow" };
            Plugin.Log.LogInfo("Widow's Wine: efecto de telarana listo (shader " + blend.name + ")");
        }

        // Telarana: 14 radios y anillos que "cuelgan" un poco entre radio y radio; borde suave hacia fuera
        private static Texture2D WebTexture(int size)
        {
            const int Spokes = 14;
            float[] rings = { 0.14f, 0.26f, 0.38f, 0.50f, 0.62f, 0.74f, 0.86f, 0.96f };
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "TC_WebTex" };
            var px = new Color32[size * size];
            float c = (size - 1) / 2f, line = 1.6f / size * 2f;           // grosor de hilo (en unidades de radio)
            var segs = new List<Vector4>();
            var ang = new float[Spokes];
            var rnd = new System.Random(7);
            for (int i = 0; i < Spokes; i++) ang[i] = (i + (float)rnd.NextDouble() * 0.3f) / Spokes * Mathf.PI * 2f;
            for (int i = 0; i < Spokes; i++) segs.Add(new Vector4(0f, 0f, Mathf.Cos(ang[i]), Mathf.Sin(ang[i])));
            foreach (float rr in rings)
                for (int i = 0; i < Spokes; i++)
                {
                    float a0 = ang[i], a1 = ang[(i + 1) % Spokes];
                    var p0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * rr;
                    var p1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * rr;
                    var mid = (p0 + p1) * 0.5f * 0.93f;                      // el hilo cuelga un poco hacia el centro
                    segs.Add(new Vector4(p0.x, p0.y, mid.x, mid.y));
                    segs.Add(new Vector4(mid.x, mid.y, p1.x, p1.y));
                }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x - c) / c, (y - c) / c);
                    float r = p.magnitude;
                    if (r > 1f) { px[y * size + x] = new Color32(255, 255, 255, 0); continue; }
                    float d = float.MaxValue;
                    foreach (var s in segs) d = Mathf.Min(d, DistSeg(p, new Vector2(s.x, s.y), new Vector2(s.z, s.w)));
                    float a = Mathf.Clamp01(1f - d / line);
                    a *= Mathf.Clamp01((1f - r) / 0.08f);                     // se desvanece en el borde
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            t.SetPixels32(px);
            t.Apply(true, true);
            return t;
        }

        private static float DistSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }
    }
}
