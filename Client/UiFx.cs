using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TarkovaCola.Client
{
    // Pequenas animaciones de interfaz (uGUI). Todas con tiempo no escalado: el menu sigue vivo aunque el juego este en pausa.

    // Respira: el alfa del grafico sube y baja entre min y max
    internal class UiPulse : MonoBehaviour
    {
        internal Graphic Target;
        internal float Min = 0.35f, Max = 1f, Speed = 2.2f, Phase;
        private void Update()
        {
            if (Target == null) return;
            var c = Target.color;
            c.a = Mathf.Lerp(Min, Max, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Speed + Phase));
            Target.color = c;
        }
    }

    // Deriva lenta (lissajous) alrededor de su posicion: para los resplandores del fondo
    internal class UiDrift : MonoBehaviour
    {
        internal Vector2 Amp = new Vector2(80f, 50f);
        internal float Speed = 0.12f, Phase;
        private Vector2 _base; private bool _init;
        private void Update()
        {
            var rt = (RectTransform)transform;
            if (!_init) { _base = rt.anchoredPosition; _init = true; }
            float t = Time.unscaledTime * Speed + Phase;
            rt.anchoredPosition = _base + new Vector2(Mathf.Sin(t) * Amp.x, Mathf.Sin(t * 1.37f + 1.1f) * Amp.y);
        }
    }

    // Cambia de color suavemente hacia Target (p. ej. el resplandor del fondo al elegir otra perk)
    internal class UiTint : MonoBehaviour
    {
        internal Graphic Graphic;
        internal Color Target;
        internal float Speed = 3f;
        private void Update() { if (Graphic != null) Graphic.color = Color.Lerp(Graphic.color, Target, 1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime)); }
    }

    // Burbujas de cola que suben despacio por el area del objeto, con un poco de vaiven, y se desvanecen arriba
    internal class UiBubbles : MonoBehaviour
    {
        internal Color Tint = new Color(0.55f, 0.95f, 0.75f, 0.10f);
        internal int Count = 26;
        private class B { public RectTransform Rt; public Image Img; public float X, Y, Speed, Size, Wob, Phase; }
        private readonly List<B> _b = new List<B>();

        private void Start()
        {
            var area = ((RectTransform)transform).rect;
            for (int i = 0; i < Count; i++)
            {
                var img = UiKit.Img(transform, "Bubble", Tint, UiKit.Bubble);
                var b = new B { Rt = img.rectTransform, Img = img };
                Reset(b, area, true);
                _b.Add(b);
            }
        }

        private static void Reset(B b, Rect area, bool anywhere)
        {
            b.Size = Random.Range(6f, 26f);
            b.X = Random.Range(0f, area.width);
            b.Y = anywhere ? Random.Range(0f, area.height) : -b.Size;
            b.Speed = Random.Range(18f, 55f) * (1.2f - b.Size / 40f);
            b.Wob = Random.Range(4f, 16f); b.Phase = Random.Range(0f, 6.28f);
            b.Rt.anchorMin = b.Rt.anchorMax = Vector2.zero; b.Rt.pivot = new Vector2(0.5f, 0.5f);
            b.Rt.sizeDelta = new Vector2(b.Size, b.Size);
        }

        private void Update()
        {
            var area = ((RectTransform)transform).rect;
            float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;
            foreach (var b in _b)
            {
                b.Y += b.Speed * dt;
                if (b.Y > area.height + b.Size) Reset(b, area, false);
                b.Rt.anchoredPosition = new Vector2(b.X + Mathf.Sin(t * 0.9f + b.Phase) * b.Wob, b.Y);
                var c = Tint; c.a *= Mathf.Clamp01(1f - b.Y / area.height) * Mathf.Clamp01(b.Y / 60f + 0.2f);
                b.Img.color = c;
            }
        }
    }

    // Lineas horizontales tenues que bajan (pantalla de monitor)
    internal class UiScan : MonoBehaviour
    {
        internal RawImage Img; internal float Speed = 0.04f;
        private void Update() { if (Img != null) { var r = Img.uvRect; r.y = (r.y - Speed * Time.unscaledDeltaTime) % 1f; Img.uvRect = r; } }
    }

    // Brillo que cruza el objeto en diagonal (al pasar el raton por un boton). Va en una capa hija que recorta (RectMask2D) para no
    // recortar lo que sobresale del propio objeto.
    internal class UiShine : MonoBehaviour
    {
        private RectTransform _band, _mask; private Image _img; private float _t0 = -10f; private const float Dur = 0.55f;

        internal static UiShine On(Transform host)
        {
            return host.GetComponent<UiShine>() ?? host.gameObject.AddComponent<UiShine>();
        }

        internal void Play(float delay = 0f)
        {
            if (_band == null)
            {
                _mask = UiKit.Rect(transform, "ShineMask"); UiKit.Stretch(_mask);
                _mask.gameObject.AddComponent<RectMask2D>();
                _img = UiKit.Img(_mask, "Shine", new Color(1f, 1f, 1f, 0f), UiKit.Band);
                _band = _img.rectTransform;
                _band.anchorMin = new Vector2(0f, 0f); _band.anchorMax = new Vector2(0f, 1f); _band.pivot = new Vector2(0.5f, 0.5f);
                _band.localRotation = Quaternion.Euler(0f, 0f, -18f);
            }
            _t0 = Time.unscaledTime + delay;
        }

        private void Update()
        {
            if (_band == null) return;
            _mask.SetAsLastSibling();                                              // por encima del contenido
            float p = (Time.unscaledTime - _t0) / Dur;
            var w = ((RectTransform)transform).rect.width;
            if (p < 0f || p > 1f) { _img.color = new Color(1f, 1f, 1f, 0f); return; }
            float e = p * p * (3f - 2f * p);
            _band.sizeDelta = new Vector2(Mathf.Max(40f, w * 0.18f), 60f);
            _band.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.2f, w * 1.2f, e), 0f);
            _img.color = new Color(1f, 1f, 1f, 0.16f * Mathf.Sin(p * Mathf.PI));
        }
    }
}
