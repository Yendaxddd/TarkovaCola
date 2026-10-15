using System.Collections.Generic;
using System.Linq;
using EFT;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Powerup de PERK ALEATORIA (como en Call of Duty Zombies)
    //   - Al matar, hay un 4% de que caiga una botella flotante en el lugar de la baja (como mucho una por raid). Dura 60 s.
    //   - Se recoge acercandote. Suena la voz del locutor y te da una perk al azar:
    //       Modern:  una de las que aun no son eternas. Si ya la llevabas, se vuelve ETERNA; si no, la activas.
    //                Si todas tus perks ya son eternas, te llega una botella extra por correo.
    //       Classic: una perk que no lleves y que quepa; si no queda ninguna, botella por correo.
    // ============================================================================================
    internal static class Powerup
    {
        internal const float Lifetime = 60f;
        private const float PickRadius = 1.8f, PickHeight = 2.8f;

        private static bool _spawned;                      // ya cayo uno en esta raid
        private static PowerupFx _fx;
        private static GUIStyle _marker, _markerSmall;
        private static Camera _cam;

        internal static bool Active { get { return _fx != null; } }

        internal static void OnRaidStart()
        {
            if (_fx != null) Object.Destroy(_fx.gameObject);
            _fx = null;
            _spawned = false;
            Jingle.Preload(Jingle.RandomVoice);                       // las voces se cargan ya: suenan sin retraso y se sabe lo que duran
            Jingle.Preload(Jingle.EternalVoice);
        }

        // Baja del jugador (desde PerkFx.OnKill)
        internal static void OnKill(Player me, Player victim)
        {
            if (!Plugin.CfgPowerup.Value || _spawned || victim == null || victim == me) return;
            float chance = Mathf.Clamp(Plugin.CfgPowerupChance.Value, 0f, 100f) / 100f;
            if (Random.value >= chance) return;
            _spawned = true;
            Spawn(victim.Position + Vector3.up * 1.1f);
        }

        private static void Spawn(Vector3 pos)
        {
            try
            {
                var go = new GameObject("TarkovaCola_RandomPerk");
                _fx = go.AddComponent<PowerupFx>();
                _fx.Init(pos);
                Dbg.Log("POWERUP", "cae una perk aleatoria en " + pos + " (" + Lifetime + " s)");
                Hud.Notify(L.T("powerup.title"), L.F("powerup.spawn", Mathf.RoundToInt(Lifetime)), Perks.All[Random.Range(0, Perks.All.Length)].Id);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("powerup (aparecer): " + e);
                if (_fx != null) Object.Destroy(_fx.gameObject);
                _fx = null;
            }
        }

        // Cada frame: caducidad y recogida
        internal static void Tick(Player me)
        {
            if (_fx == null) return;
            if (_fx.Expired) { Dbg.Log("POWERUP", "la perk aleatoria caduco"); Object.Destroy(_fx.gameObject); _fx = null; return; }
            var hc = me.HealthController;
            if (hc == null || !hc.IsAlive) return;
            var d = me.Position - _fx.Position;
            if (new Vector2(d.x, d.z).magnitude > PickRadius || Mathf.Abs(d.y) > PickHeight) return;
            Collect(me);
        }

        private static void Collect(Player me)
        {
            AchTrack.OnPowerupPicked(_fx.TimeLeft);
            _fx.Burst();
            _fx = null;
            Jingle.Voice(Jingle.RandomVoice);
            GrantRandom(me);
        }

        internal static void GrantRandom(Player me)
        {
            PerkDef[] pool = ClassicMode.Enabled
                ? Perks.All.Where(p => ClassicMode.DrinkBlockReason(p) == null).ToArray()
                : Perks.All.Where(p => !Eternal.Is(p.Id)).ToArray();

            if (pool.Length == 0)
            {
                var def = Perks.All[Random.Range(0, Perks.All.Length)];
                PerkService.RequestRefund(def.ItemTpl, 1);
                Dbg.Log("POWERUP", "no queda ninguna perk que dar: botella extra de " + def.Id + " por correo");
                Hud.Notify(L.T("powerup.title"), L.F("powerup.mail", def.Name), def.Id);
                return;
            }
            var pick = pool[Random.Range(0, pool.Length)];
            Dbg.Log("POWERUP", "perk aleatoria: " + pick.Id + (!ClassicMode.Enabled && Perks.IsActive(pick.Id) ? " (ya la llevabas: eterna)" : ""));
            Hud.Notify(L.T("powerup.title"), pick.Name, pick.Id);
            DrinkAnim.Play(pick, () => { if (Plugin.InRaid) PerkGrant.Apply(pick, Plugin.Me, true); });     // se bebe la botella y, a media animacion, hace efecto
        }

        // ---------------------------------------------------------------- marcador en pantalla (distancia y tiempo)
        internal static Camera Cam()
        {
            if (_cam != null && _cam.enabled && _cam.gameObject.activeInHierarchy) return _cam;
            Camera c = null;
            try { if (EFT.CameraControl.CameraManager.Exist) c = EFT.CameraControl.CameraManager.Instance.Camera; } catch { }      // la camara del jugador ("FPS Camera")
            if (c == null) c = Camera.allCameras.FirstOrDefault(x => x.enabled && x.name.IndexOf("FPS", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (c == null) c = Camera.main;
            if (c != null) Dbg.Log("POWERUP", "camara: " + c.name + " (fov " + c.fieldOfView.ToString("0") + ")");
            _cam = c;
            return c;
        }

        // Tiempo que espera la voz de Eternal tras aplicarse la perk del powerup (para no pisar la voz de "Random Perk")
        internal static float EternalVoiceDelay { get { return Mathf.Max(0f, Jingle.Length(Jingle.RandomVoice) + 0.2f - DrinkAnim.ApplyAt); } }

        internal static void DrawMarker(float scale, Texture2D white)
        {
            if (_fx == null) return;
            var me = Plugin.Me;
            var cam = Cam();
            if (me == null || cam == null) return;
            if (_marker == null)
            {
                _marker = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                _markerSmall = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
            }
            // Coordenadas de "viewport" (0..1) y no de pixeles: EFT renderiza la camara a otra resolucion (DLSS/FSR/supermuestreo), y los
            // pixeles de la camara no son los de la pantalla. Asi el marcador cae encima de la botella a cualquier escala de render.
            var vp = cam.WorldToViewportPoint(_fx.Position + Vector3.up * 1.0f);
            float x = vp.x * Screen.width, y = (1f - vp.y) * Screen.height;
            bool behind = Vector3.Dot(cam.transform.forward, _fx.Position - cam.transform.position) < 0f;       // (el signo de sp.z no es fiable en EFT)
            if (behind) { x = Screen.width - x; y = Screen.height - y; }         // detras: se pega al borde contrario
            float m = 54f * scale;
            x = Mathf.Clamp(x, m, Screen.width - m);
            y = Mathf.Clamp(y, m, Screen.height - m);

            float t = _fx.TimeLeft;
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 6f);
            if (t < 12f && Mathf.Repeat(Time.time * 3f, 1f) < 0.5f) pulse *= 0.35f;      // parpadea cuando va a caducar
            Color c = _fx.CurrentColor; c.a = pulse;
            float s = 52f * scale, b = 3f * scale;

            // marco de color con el icono de una perk que cambia sin parar (como la botella)
            var icon = Hud.IconTex(Perks.All[((int)(Time.time * 3f)) % Perks.All.Length].Id);
            GUI.color = new Color(0f, 0f, 0f, 0.6f * pulse);
            GUI.DrawTexture(new Rect(x - s * 0.5f - b - 2f * scale, y - s * 0.5f - b - 2f * scale, s + 2f * (b + 2f * scale), s + 2f * (b + 2f * scale)), white);
            GUI.color = c;
            GUI.DrawTexture(new Rect(x - s * 0.5f - b, y - s * 0.5f - b, s + 2f * b, s + 2f * b), white);
            GUI.color = new Color(0.07f, 0.1f, 0.14f, 0.95f);
            GUI.DrawTexture(new Rect(x - s * 0.5f, y - s * 0.5f, s, s), white);
            if (icon != null)
            {
                GUI.color = new Color(1f, 1f, 1f, pulse);
                GUI.DrawTexture(new Rect(x - s * 0.5f + 3f * scale, y - s * 0.5f + 3f * scale, s - 6f * scale, s - 6f * scale), icon, ScaleMode.ScaleToFit, true);
            }

            _markerSmall.fontSize = Mathf.RoundToInt(17f * scale);
            string txt = Mathf.RoundToInt(Vector3.Distance(me.Position, _fx.Position)) + " m  ·  " + Mathf.CeilToInt(t) + " s";
            float ty = y + s * 0.5f + b + 3f * scale;
            _markerSmall.normal.textColor = new Color(0f, 0f, 0f, 0.85f * pulse);
            GUI.Label(new Rect(x - 90f * scale + 1f, ty + 1f, 180f * scale, 24f * scale), txt, _markerSmall);
            _markerSmall.normal.textColor = new Color(1f, 1f, 1f, pulse);
            GUI.Label(new Rect(x - 90f * scale, ty, 180f * scale, 24f * scale), txt, _markerSmall);
            GUI.color = Color.white;
        }
    }

    // La botella flotante: modelos de las 5 perks que cambian sin parar (como la maquina de perks al azar), columna de luz,
    // resplandor, luz puntual y chispas. Todo se construye en codigo salvo los modelos, que salen de los bundles de la mano.
    internal class PowerupFx : MonoBehaviour
    {
        private const float BottleHeight = 0.55f;          // altura de las botellas en el mundo (m)

        private readonly List<GameObject> _bottles = new List<GameObject>();
        private Transform _spin, _glow;
        private GameObject _visual;
        private Light _light;
        private readonly List<Material> _mats = new List<Material>();
        private readonly List<Texture2D> _texs = new List<Texture2D>();
        private Material _glowMat, _beamMat, _sparkMat;
        private ParticleSystem _sparks;
        private Vector3 _base;
        private float _t0, _until;
        private int _shown = -1;
        private bool _bursting;
        private float _burstEnd;
        private Color _color = Color.white;

        internal Vector3 Position { get { return _base; } }
        internal float TimeLeft { get { return Mathf.Max(0f, _until - Time.time); } }
        internal bool Expired { get { return !_bursting && Time.time >= _until; } }
        internal Color CurrentColor { get { return _color; } }

        internal void Init(Vector3 pos)
        {
            _base = pos;
            transform.position = pos;
            _t0 = Time.time;
            _until = Time.time + Powerup.Lifetime;

            var shader = PhdVfx.FindShader();
            var soft = Tex(PhdVfx.MakeTexture(64, r => Mathf.Pow(Mathf.Clamp01(1f - r), 2f)));
            _glowMat = Mat(shader, soft);
            _beamMat = Mat(shader, Tex(BeamTexture()));
            _sparkMat = Mat(shader, soft);

            _visual = new GameObject("Visual");
            _visual.transform.SetParent(transform, false);

            // modelos de botella (los de la mano de cada perk), uno visible cada vez
            _spin = new GameObject("Spin").transform;
            _spin.SetParent(_visual.transform, false);
            foreach (var perk in Perks.All)
            {
                if (string.IsNullOrEmpty(perk.HandBundle)) continue;
                var prefab = HandModel.Load(perk);
                if (prefab == null) continue;
                var b = Instantiate(prefab, _spin, false);
                foreach (var c in b.GetComponentsInChildren<Collider>(true)) Destroy(c);
                b.transform.localPosition = Vector3.zero;
                b.transform.localRotation = Quaternion.identity;
                b.transform.localScale = Vector3.one * (BottleHeight / perk.HandRefHeight);
                b.SetActive(false);
                _bottles.Add(b);
            }

            // columna de luz: dos planos cruzados
            for (int i = 0; i < 2; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = "Beam" + i;
                q.transform.SetParent(_visual.transform, false);
                q.transform.localPosition = new Vector3(0f, 6.9f, 0f);
                q.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
                q.transform.localScale = new Vector3(1.3f, 16f, 1f);
                q.GetComponent<Renderer>().sharedMaterial = _beamMat;
            }

            // resplandor que siempre mira a la camara
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(g.GetComponent<Collider>());
            g.name = "Glow";
            _glow = g.transform;
            _glow.SetParent(_visual.transform, false);
            _glow.localScale = Vector3.one * 2.2f;
            g.GetComponent<Renderer>().sharedMaterial = _glowMat;

            var lg = new GameObject("Light");
            lg.transform.SetParent(_visual.transform, false);
            _light = lg.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = 9f;
            _light.intensity = 1.6f;
            _light.shadows = LightShadows.None;

            Sparkles();
            Update();
        }

        private Material Mat(Shader shader, Texture2D tex)
        {
            var m = new Material(shader) { mainTexture = tex, name = "TC_Powerup" };
            _mats.Add(m);
            return m;
        }

        private Texture2D Tex(Texture2D t) { _texs.Add(t); return t; }

        // plano de la columna: suave por los lados, se desvanece hacia arriba
        private static Texture2D BeamTexture()
        {
            const int W = 32, H = 128;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "TC_Beam" };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                    float side = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u * 2f - 1f)), 1.6f);
                    float up = v < 0.08f ? v / 0.08f : Mathf.Pow(Mathf.Clamp01(1f - (v - 0.08f) / 0.92f), 1.7f);
                    px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(side * up * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        private void Sparkles()
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(_visual.transform, false);
            _sparks = go.AddComponent<ParticleSystem>();
            _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = _sparkMat;
            var main = _sparks.main;
            main.loop = true; main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.9f), new Color(0.6f, 1f, 0.9f, 0.9f));
            main.gravityModifier = -0.04f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            var em = _sparks.emission; em.rateOverTime = 22f;
            var shape = _sparks.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.45f;
            var col = _sparks.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                       new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(gr);
            _sparks.Play();
        }

        // al recogerla: la botella y la columna desaparecen y estalla un puñado de chispas
        internal void Burst()
        {
            if (_bursting) return;
            _bursting = true;
            _visual.SetActive(true);                                  // (si estaba apagado por el parpadeo)
            _burstEnd = Time.time + 1.4f;
            foreach (var b in _bottles) b.SetActive(false);
            foreach (var rend in _visual.GetComponentsInChildren<MeshRenderer>(true)) rend.enabled = false;
            if (_light != null) { _light.intensity = 3f; _light.range = 12f; }
            var em = _sparks.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
            var main = _sparks.main; main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f); main.gravityModifier = 0.15f;
            _sparks.Clear();
            _sparks.Play();
        }

        private void Update()
        {
            float now = Time.time, t = now - _t0;
            if (_bursting)
            {
                if (_light != null) _light.intensity = Mathf.Max(0f, 3f * (1f - (now - (_burstEnd - 1.4f)) / 0.5f));
                if (now >= _burstEnd) Destroy(gameObject);
                return;
            }

            transform.position = _base + Vector3.up * (Mathf.Sin(t * 2.2f) * 0.12f);
            _spin.Rotate(0f, 130f * Time.deltaTime, 0f, Space.World);

            // un modelo distinto cada cuarto de segundo
            if (_bottles.Count > 0)
            {
                int i = ((int)(t * 4f)) % _bottles.Count;
                if (i != _shown)
                {
                    if (_shown >= 0) _bottles[_shown].SetActive(false);
                    _bottles[i].SetActive(true);
                    _shown = i;
                }
            }

            _color = Color.HSVToRGB(Mathf.Repeat(t * 0.22f, 1f), 0.55f, 1f);
            Tint(_glowMat, _color, 0.9f);
            Tint(_beamMat, _color, 0.8f);
            if (_light != null) _light.color = _color;

            var cam = Powerup.Cam();
            if (cam != null) _glow.rotation = Quaternion.LookRotation(_glow.position - cam.transform.position);

            // en los ultimos segundos parpadea
            bool blink = TimeLeft < 12f && Mathf.Repeat(t * (TimeLeft < 5f ? 6f : 3f), 1f) < 0.5f;
            if (_visual.activeSelf == blink) _visual.SetActive(!blink);
        }

        private static void Tint(Material m, Color c, float a)
        {
            if (m == null) return;
            var tint = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, a);            // los shaders aditivos duplican el tinte: 0.5 = neutro
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", tint);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(c.r, c.g, c.b, a));
        }

        private void OnDestroy()
        {
            foreach (var m in _mats) if (m != null) Destroy(m);
            foreach (var t in _texs) if (t != null) Destroy(t);
        }
    }
}

namespace TarkovaCola.Client
{
    // Animacion de beber para el powerup: la botella de la perk sube hasta la boca delante de la camara, se inclina y baja. No hay botella
    // real en la mano (viene del suelo), asi que el movimiento se hace en codigo con el modelo de la mano de esa perk. El efecto de la
    // perk se aplica a media animacion.
    internal class DrinkAnim : MonoBehaviour
    {
        internal const float ApplyAt = 0.95f, Total = 1.8f;
        private Transform _bottle;
        private float _t0;
        private System.Action _apply;
        private bool _applied;

        internal static void Play(PerkDef perk, System.Action apply)
        {
            var cam = Powerup.Cam();
            var prefab = cam != null ? HandModel.Load(perk) : null;
            if (prefab == null) { apply(); return; }                          // sin modelo o camara: sin animacion, pero la perk se da igual
            var go = new GameObject("TarkovaCola_DrinkAnim");
            go.transform.SetParent(cam.transform, false);
            var d = go.AddComponent<DrinkAnim>();
            d._apply = apply;
            d._t0 = Time.time;
            var b = Instantiate(prefab, go.transform, false);
            foreach (var c in b.GetComponentsInChildren<Collider>(true)) Destroy(c);
            b.transform.localScale = Vector3.one * (0.2f / perk.HandRefHeight);
            d._bottle = b.transform;
            var lg = new GameObject("Light");
            lg.transform.SetParent(go.transform, false);
            lg.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = 2f; l.intensity = 1.2f; l.shadows = LightShadows.None;       // para que se vea tambien de noche
            d.Pose(0f);
        }

        // 0 = abajo a la derecha, ~0.35 = junto a la boca (inclinada), 1 = otra vez abajo
        private void Pose(float u)
        {
            var low = new Vector3(0.30f, -0.40f, 0.55f);
            var high = new Vector3(0.0f, -0.17f, 0.36f);
            float up = u < 0.28f ? Mathf.SmoothStep(0f, 1f, u / 0.28f) : u > 0.75f ? Mathf.SmoothStep(1f, 0f, (u - 0.75f) / 0.25f) : 1f;
            float sip = u > 0.28f && u < 0.75f ? Mathf.Sin((u - 0.28f) / 0.47f * Mathf.PI * 2f) * 4f : 0f;
            _bottle.localPosition = Vector3.Lerp(low, high, up);
            _bottle.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -72f + sip, up), Mathf.Lerp(-25f, 0f, up), Mathf.Lerp(12f, 0f, up));
        }

        private void Update()
        {
            float t = Time.time - _t0;
            if (!_applied && t >= ApplyAt) { _applied = true; try { _apply(); } catch (System.Exception e) { Plugin.Log.LogError("powerup (aplicar): " + e); } }
            if (t >= Total || !Plugin.InRaid) { if (!_applied) { _applied = true; if (Plugin.InRaid) _apply(); } Destroy(gameObject); return; }
            Pose(t / Total);
        }
    }
}
