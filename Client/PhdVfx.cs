using System;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Efecto visual de PHD Flopper: cuando la perk te salva de una caida o de una explosion, estalla un anillo de energia rosa y
    // naranja bajo tus pies, con chispas y un destello de luz (como el "flop" de Black Ops, pero sin dano). Todo se construye en
    // codigo (texturas pequenas generadas y sistemas de particulas), asi que no depende de ningun bundle ni shader propio.
    internal static class PhdVfx
    {
        private const float Cooldown = 0.5f;          // una caida o explosion llama varias veces seguidas: un solo efecto
        private static Texture2D _soft, _ring;
        private static Material _softMat, _ringMat;
        private static float _next;
        private static bool _failed;

        // power 0..1: tamano del efecto (una caida pequena = efecto pequeno)
        internal static void Play(Vector3 pos, float power)
        {
            if (_failed || !Plugin.CfgPhdVfx.Value) return;
            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + Cooldown;
            try
            {
                Build();
                float k = Mathf.Lerp(0.55f, 1.15f, Mathf.Clamp01(power));
                var root = new GameObject("TarkovaCola_PHD_VFX");
                root.transform.position = pos + Vector3.up * 0.08f;

                Ring(root.transform, 17f * k, 0.75f, 0f, new Color(1f, 0.5f, 0.95f), new Color(1f, 0.55f, 0.2f));          // onda grande rosa -> naranja
                Ring(root.transform, 10f * k, 0.55f, 0.1f, new Color(1f, 0.85f, 0.5f), new Color(1f, 0.4f, 0.6f));         // segunda onda mas pequena y calida
                Core(root.transform, k);
                Sparks(root.transform, k);
                root.AddComponent<PhdVfxLight>().Init(10f * k, 4f, 0.45f);

                UnityEngine.Object.Destroy(root, 2.5f);
                Dbg.Log("EFECTO", "PHD Flopper: efecto visual (tamano x" + k.ToString("0.00") + ")");
            }
            catch (Exception e)
            {
                _failed = true;                         // si algo falla no se reintenta: el efecto es solo cosmetico
                Plugin.Log.LogWarning("PHD Flopper: no se pudo crear el efecto visual: " + e.Message);
            }
        }

        // ---------------------------------------------------------------- piezas
        // Anillo plano que se expande por el suelo (size = diametro final en metros)
        private static void Ring(Transform parent, float size, float life, float delay, Color from, Color to)
        {
            var ps = NewSystem(parent, "Ring", _ringMat);
            var main = ps.main;
            main.duration = life + delay; main.loop = false;
            main.startDelay = delay;
            main.startLifetime = life; main.startSpeed = 0f;
            main.startSize = size;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4;

            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var shape = ps.shape; shape.enabled = false;

            // empieza pequeno y se abre deprisa (rapido al principio, lento al final)
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.04f, 0f, 3.5f), new Keyframe(1f, 1f, 0.15f, 0f)));

            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(from, to));

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            ps.Play();
        }

        // Destello central, grande y muy breve
        private static void Core(Transform parent, float k)
        {
            var ps = NewSystem(parent, "Core", _softMat);
            var main = ps.main;
            main.duration = 0.3f; main.loop = false;
            main.startLifetime = 0.3f; main.startSpeed = 0f;
            main.startSize = 6f * k;
            main.startColor = new Color(1f, 0.8f, 1f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(Color.white, new Color(1f, 0.4f, 0.8f)));
            ps.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;      // plano sobre el suelo: asi no se corta contra el
            ps.Play();
        }

        // Chispas que saltan hacia fuera y arriba
        private static void Sparks(Transform parent, float k)
        {
            var ps = NewSystem(parent, "Sparks", _softMat);
            var main = ps.main;
            main.duration = 0.4f; main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f * k, 11f * k);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f * k, 0.8f * k);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.85f), new Color(1f, 0.7f, 0.2f));
            main.gravityModifier = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 90;

            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var shape = ps.shape;
            shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = 0.5f * k;
            shape.position = new Vector3(0f, 0.6f, 0f);       // un poco por encima del suelo: las chispas no se cortan contra el

            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(Color.white, new Color(1f, 0.5f, 0.6f)));
            ps.Play();
        }

        // gradiente de opacidad completa a transparente pasando de un color a otro
        private static Gradient Fade(Color from, Color to)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 0.6f), new GradientColorKey(to, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.45f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static ParticleSystem NewSystem(Transform parent, string name, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);      // los valores de duracion solo se cambian parado
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = mat;
            r.sortingFudge = 0f;
            return ps;
        }

        // ---------------------------------------------------------------- texturas y materiales (una sola vez)
        private static void Build()
        {
            if (_softMat != null) return;
            _soft = MakeTexture(64, (r) => Mathf.Pow(Mathf.Clamp01(1f - r), 2f));                                   // mancha suave
            _ring = MakeTexture(128, (r) => Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(r - 0.8f) / 0.2f) * (r <= 1f ? 1f : 0f));   // aro
            var shader = FindShader();
            _softMat = new Material(shader) { mainTexture = _soft, name = "TC_PHD_Soft" };
            _ringMat = new Material(shader) { mainTexture = _ring, name = "TC_PHD_Ring" };
            Plugin.Log.LogInfo("PHD Flopper: efecto visual listo (shader " + shader.name + ")");
        }

        // primer shader de particulas que exista en el juego (los aditivos dan el brillo; Sprites/Default siempre esta)
        private static Shader FindShader()
        {
            foreach (var n in new[] { "Legacy Shaders/Particles/Additive", "Mobile/Particles/Additive", "Particles/Additive", "Sprites/Default", "UI/Default" })
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            throw new Exception("no hay ningun shader de particulas disponible");
        }

        // textura blanca cuya opacidad depende de la distancia al centro (0 = centro, 1 = borde)
        private static Texture2D MakeTexture(int size, Func<float, float> alphaByRadius)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "TC_PHD_Tex" };
            var px = new Color32[size * size];
            float c = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alphaByRadius(r)) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }
    }

    // Destello de luz que se apaga solo
    internal class PhdVfxLight : MonoBehaviour
    {
        private Light _light;
        private float _peak, _life, _t;

        internal void Init(float range, float intensity, float life)
        {
            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = range;
            _light.color = new Color(1f, 0.5f, 0.85f);
            _light.shadows = LightShadows.None;
            _light.intensity = intensity;
            _peak = intensity; _life = life;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (_light != null) _light.intensity = _peak * Mathf.Clamp01(1f - _t / _life);
            if (_t >= _life) enabled = false;
        }
    }
}
