using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT;
using UnityEngine;
using UnityEngine.Rendering;

namespace TarkovaCola.Client
{
    // ===================================================================================================================
    // Death Perception: contorno de los enemigos marcados (en vez del rombo sobre la cabeza).
    //   1) Antes de los efectos de imagen (con las matrices de la camara activas) se pinta la silueta de cada enemigo marcado en una
    //      textura propia, con el color de su marca.
    //   2) Despues de los efectos de imagen se mezcla sobre la imagen solo el borde de esa silueta, mas un relleno muy suave.
    //   Las mallas con piel se "hornean" en CPU cada fotograma (BakeMesh) con la pose actual de los huesos: dibujar el SkinnedMeshRenderer
    //   directamente usaba el resultado del skinning de la GPU, que con la optimizacion de bots (culling) o el reescalado salia desplazado
    //   o congelado. Shaders propios en assets/outline.bundle (UnityBundle/Assets/Shaders, BuildOutline.Run).
    // ===================================================================================================================
    internal static class DpOutline
    {
        internal struct Entry { public Player P; public Color C; }

        private const int MaxEnemies = 8;                 // limite de enemigos con contorno a la vez (coste del horneado)

        private class Part { public Renderer R; public SkinnedMeshRenderer Sk; public Mesh Src; public Mesh Baked; public int Subs; }

        private static readonly List<Entry> _entries = new List<Entry>();
        private static readonly Dictionary<string, (float at, Part[] parts)> _parts = new Dictionary<string, (float, Part[])>();
        private static readonly List<Mesh> _meshPool = new List<Mesh>();
        private static CommandBuffer _maskCmd, _compCmd;
        private static Camera _cam;
        private static RenderTexture _rt;
        private static Material _mask, _comp;
        private static bool _loaded, _hooked;
        private static readonly int ColorId = Shader.PropertyToID("_TcOutlineColor");
        private static readonly int WidthId = Shader.PropertyToID("_TcOutlineWidth");
        private static readonly int AlphaId = Shader.PropertyToID("_TcOutlineAlpha");

        // lista de este fotograma (DeathPerception.Tick); vacia = sin contorno
        internal static void Set(List<Entry> entries)
        {
            _entries.Clear();
            if (entries != null) _entries.AddRange(entries);
            if (!_hooked) { _hooked = true; Camera.onPreCull += OnPreCull; }
        }

        internal static void Clear()
        {
            _entries.Clear();
            foreach (var kv in _parts) Recycle(kv.Value.parts);
            _parts.Clear();
            if (_maskCmd != null) _maskCmd.Clear();
            if (_compCmd != null) _compCmd.Clear();
        }

        private static bool Load()
        {
            if (_loaded) return _mask != null;
            _loaded = true;
            try
            {
                string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", "outline.bundle"));
                var b = File.Exists(path) ? AssetBundle.LoadFromFile(path) : null;
                if (b == null) { Plugin.Log.LogWarning("Death Perception: falta assets/outline.bundle (sin contorno)"); return false; }
                var shaders = b.LoadAllAssets<Shader>();
                var ms = shaders.FirstOrDefault(s => s.name == "TarkovaCola/OutlineMask");
                var cs = shaders.FirstOrDefault(s => s.name == "TarkovaCola/OutlineComposite");
                if (ms == null || cs == null || !ms.isSupported || !cs.isSupported) { Plugin.Log.LogWarning("Death Perception: shaders del contorno no disponibles"); return false; }
                _mask = new Material(ms) { name = "TC_DpOutlineMask" };
                _comp = new Material(cs) { name = "TC_DpOutlineComposite" };
                _maskCmd = new CommandBuffer { name = "TarkovaCola Death Perception (mascara)" };
                _compCmd = new CommandBuffer { name = "TarkovaCola Death Perception (contorno)" };
                Plugin.Log.LogInfo("Death Perception: contorno listo");
                return true;
            }
            catch (Exception e) { Plugin.Log.LogError("Death Perception (contorno): " + e); return false; }
        }

        private static void OnPreCull(Camera cam)
        {
            try
            {
                var main = Powerup.Cam();
                if (cam == null || cam != main) return;
                if (!Load()) return;
                if (_cam != cam)
                {
                    if (_cam != null)
                        try { _cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _maskCmd); _cam.RemoveCommandBuffer(CameraEvent.AfterImageEffects, _compCmd); } catch { }
                    _cam = cam;
                    cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, _maskCmd);
                    cam.AddCommandBuffer(CameraEvent.AfterImageEffects, _compCmd);
                }
                Record(cam);
            }
            catch (Exception e) { Dbg.Throttled("dpoutline", 10f, "ERROR", "Death Perception (contorno): " + e.Message); }
        }

        // ---------------------------------------------------------------- piezas (renderers) de cada enemigo
        private static Part[] PartsOf(Player p)
        {
            float now = Time.time;
            (float at, Part[] parts) c;
            if (_parts.TryGetValue(p.ProfileId, out c) && now - c.at < 1f) return c.parts;
            if (c.parts != null) Recycle(c.parts);
            var list = new List<Part>();
            foreach (var r in p.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r.forceRenderingOff || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;   // piezas invisibles (solo sombra)
                string n = r.name;
                if (n.IndexOf("LOD1", StringComparison.Ordinal) >= 0 || n.IndexOf("LOD2", StringComparison.Ordinal) >= 0 || n.IndexOf("LOD3", StringComparison.Ordinal) >= 0) continue;
                var sk = r as SkinnedMeshRenderer;
                if (sk != null)
                {
                    if (sk.sharedMesh == null) continue;
                    list.Add(new Part { R = r, Sk = sk, Src = sk.sharedMesh, Baked = _meshPool.Count > 0 ? PopMesh() : new Mesh { name = "TC_DpBaked" }, Subs = sk.sharedMesh.subMeshCount });
                    continue;
                }
                if (!(r is MeshRenderer)) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                list.Add(new Part { R = r, Src = mf.sharedMesh, Subs = mf.sharedMesh.subMeshCount });
            }
            var arr = list.ToArray();
            _parts[p.ProfileId] = (now, arr);
            return arr;
        }

        private static Mesh PopMesh() { var m = _meshPool[_meshPool.Count - 1]; _meshPool.RemoveAt(_meshPool.Count - 1); return m; }

        private static void Recycle(Part[] parts)
        {
            if (parts == null) return;
            foreach (var x in parts) if (x.Baked != null) { x.Baked.Clear(); _meshPool.Add(x.Baked); x.Baked = null; }
        }

        private static void EnsureRt(Camera cam)
        {
            int w = Mathf.Max(16, cam.pixelWidth), h = Mathf.Max(16, cam.pixelHeight);
            if (_rt != null && _rt.width == w && _rt.height == h) return;
            if (_rt != null) { _rt.Release(); UnityEngine.Object.Destroy(_rt); }
            _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "TC_DpOutlineMask", filterMode = FilterMode.Bilinear };
            _rt.Create();
        }

        private static Mesh _quad;
        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            _quad = new Mesh { name = "TC_DpQuad" };
            _quad.vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(1f, -1f, 0f) };
            // en DirectX (EFT) la textura empieza arriba: sin darle la vuelta el contorno salia invertido en vertical
            // (subia cuando mirabas abajo y bajaba cuando mirabas arriba)
            float lo = SystemInfo.graphicsUVStartsAtTop ? 1f : 0f, hi = 1f - lo;
            _quad.uv = new[] { new Vector2(0f, lo), new Vector2(0f, hi), new Vector2(1f, hi), new Vector2(1f, lo) };
            _quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * 1e5f);
            return _quad;
        }

        private static void Record(Camera cam)
        {
            _maskCmd.Clear();
            _compCmd.Clear();
            if (_entries.Count == 0 || !DeathPerception.Active) return;

            // los mas cercanos primero
            var camPos = cam.transform.position;
            var list = _entries.Where(e => e.P != null && e.P.HealthController != null && e.P.HealthController.IsAlive)
                               .OrderBy(e => (e.P.Position - camPos).sqrMagnitude).Take(MaxEnemies).ToList();
            if (list.Count == 0) return;

            EnsureRt(cam);
            _maskCmd.SetRenderTarget(_rt);
            // la mascara ocupa TODA su textura: con resolucion dinamica (DynamicExternalResolution, FSR...) la camara dibuja en un trozo
            // de la imagen (rect 0.58) y ese recorte se heredaba aqui; luego el contorno se volvia a encoger al mezclarlo -> las siluetas
            // salian encogidas hacia la esquina inferior izquierda
            _maskCmd.SetViewport(new Rect(0f, 0f, _rt.width, _rt.height));
            _maskCmd.ClearRenderTarget(false, true, new Color(0f, 0f, 0f, 0f));
            // sin el temblor del antialiasing temporal (TAA): la imagen final ya no lo lleva y el contorno "bailaba" un poco
            _maskCmd.SetViewProjectionMatrices(cam.worldToCameraMatrix, GL.GetGPUProjectionMatrix(cam.nonJitteredProjectionMatrix, true));
            int drawn = 0;
            foreach (var e in list)
            {
                _maskCmd.SetGlobalColor(ColorId, e.C);
                foreach (var part in PartsOf(e.P))
                {
                    var r = part.R;
                    if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                    Mesh mesh;
                    Matrix4x4 m;
                    if (part.Sk != null)
                    {
                        // pose actual de los huesos (ya animados este fotograma); la malla horneada lleva la escala, se dibuja sin ella
                        part.Sk.BakeMesh(part.Baked, true);
                        mesh = part.Baked;
                        // huesos sin actualizar (pieza oculta, LOD o fuera de pantalla): la malla sale como triangulos estirados por el
                        // mapa (la "figura geometrica verde"). Una persona no mide mas de 3 m: si sale mas grande, no se dibuja
                        mesh.RecalculateBounds();
                        if (mesh.bounds.size.sqrMagnitude > 9f || mesh.bounds.center.sqrMagnitude > 9f) continue;
                        var t = part.Sk.transform;
                        m = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
                    }
                    else { mesh = part.Src; m = r.localToWorldMatrix; }
                    int subs = Mathf.Min(part.Subs, mesh.subMeshCount);
                    for (int i = 0; i < subs; i++) { _maskCmd.DrawMesh(mesh, m, _mask, i, 0); drawn++; }
                }
            }
            _maskCmd.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
            if (drawn == 0) { _maskCmd.Clear(); return; }
            // solo el borde, fino (antes 1.5-4 px con relleno suave: cegaba)
            _compCmd.SetGlobalFloat(WidthId, Mathf.Clamp(cam.pixelHeight / 1080f * 1.4f, 1f, 2.5f));
            _compCmd.SetGlobalFloat(AlphaId, Plugin.CfgOutlineAlpha != null ? Plugin.CfgOutlineAlpha.Value : 0.5f);
            // mezcla a pantalla COMPLETA: despues de los efectos la imagen ya esta ampliada a toda la pantalla, pero el viewport sigue
            // siendo el recorte de la resolucion dinamica (58%) y Blit lo respetaba -> contorno encogido hacia la esquina
            _comp.SetTexture("_MainTex", _rt);
            _compCmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            _compCmd.SetViewport(new Rect(0f, 0f, Screen.width, Screen.height));
            _compCmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            _compCmd.DrawMesh(Quad(), Matrix4x4.identity, _comp, 0, 0);
        }
    }
}
