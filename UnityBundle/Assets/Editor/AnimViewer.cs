using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TarkovaCola
{
    // Visor de animaciones (Cast -> JSON) sobre un modelo con esqueleto, sin abrir el juego.
    //   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.AnimViewer.Run -logFile v.log
    // Variables de entorno: VIEW_MODEL (FBX), VIEW_ANIM (JSON), VIEW_FRAMES ("0,10,20" o "0-100/5"), VIEW_OUT (nombre de la hoja),
    // VIEW_YAW (giro del modelo), VIEW_CAM ("x,y,z" posicion de la camara respecto al modelo), VIEW_FLIP ("x" invierte ese eje del cuaternion).
    public static class AnimViewer
    {
        private static string Env(string k, string d) { var v = System.Environment.GetEnvironmentVariable(k); return string.IsNullOrEmpty(v) ? d : v; }

        // ---- JSON minimo (el de cast2json.py) ----
        internal class Curve { public string bone, prop; public int[] frames; public float[][] values; }
        internal class Clip { public float fps; public int frames; public List<Curve> curves = new List<Curve>(); public Dictionary<string, int[]> notes = new Dictionary<string, int[]>(); }

        internal static Clip Load(string path)
        {
            var clip = new Clip();
            var j = MiniJson.Parse(File.ReadAllText(path)) as Dictionary<string, object>;
            clip.fps = System.Convert.ToSingle(j["fps"]); clip.frames = System.Convert.ToInt32(j["frames"]);
            foreach (Dictionary<string, object> c in (List<object>)j["curves"])
            {
                var cv = new Curve { bone = (string)c["bone"], prop = (string)c["prop"] };
                cv.frames = ((List<object>)c["frames"]).Select(System.Convert.ToInt32).ToArray();
                cv.values = ((List<object>)c["values"]).Select(v => v is List<object> l ? l.Select(System.Convert.ToSingle).ToArray() : new[] { System.Convert.ToSingle(v) }).ToArray();
                clip.curves.Add(cv);
            }
            foreach (Dictionary<string, object> n in (List<object>)j["notes"])
                clip.notes[(string)n["name"]] = ((List<object>)n["frames"]).Select(System.Convert.ToInt32).ToArray();
            return clip;
        }

        internal static float[] Sample(Curve c, float f)
        {
            int i = 0;
            while (i + 1 < c.frames.Length && c.frames[i + 1] <= f) i++;
            if (i + 1 >= c.frames.Length || f <= c.frames[0]) return c.values[Mathf.Min(i, c.values.Length - 1)];
            float t = (f - c.frames[i]) / (c.frames[i + 1] - c.frames[i]);
            var a = c.values[i]; var b = c.values[i + 1];
            if (a.Length == 4) { var q = Quaternion.Slerp(new Quaternion(a[0], a[1], a[2], a[3]), new Quaternion(b[0], b[1], b[2], b[3]), t); return new[] { q.x, q.y, q.z, q.w }; }
            return new[] { Mathf.Lerp(a[0], b[0], t) };
        }

        // CoD (Z arriba, mano derecha) -> espacio local del FBX ya importado por Unity: se prueba con VIEW_FLIP hasta que cuadre
        private static Quaternion Conv(float[] q)
        {
            string flip = Env("VIEW_FLIP", "");
            var r = new Quaternion(q[0], q[1], q[2], q[3]);
            if (flip.Contains("x")) r.x = -r.x;
            if (flip.Contains("y")) r.y = -r.y;
            if (flip.Contains("z")) r.z = -r.z;
            return r;
        }

        private static Dictionary<string, Transform> Bones(GameObject root)
        {
            var d = new Dictionary<string, Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) d[t.name] = t;
            return d;
        }

        private static void Pose(Dictionary<string, Transform> bones, Clip clip, float frame, Dictionary<string, Quaternion> rest)
        {
            foreach (var c in clip.curves)
            {
                Transform t;
                if (!bones.TryGetValue(c.bone, out t)) continue;
                var v = Sample(c, frame);
                if (c.prop == "rq") t.localRotation = Conv(v);
            }
        }

        public static void Run()
        {
            string model = Env("VIEW_MODEL", "Assets/Models/bo6/offhand_t10_vm_zmb_perk_can/offhand_t10_vm_zmb_perk_can.fbx");
            string anim = Env("VIEW_ANIM", "Assets/Anim/t10_vm_gesture_zm_perk_can_drink.json");
            var clip = Load(anim);
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            if (fbx == null) throw new System.Exception("no se encontro " + model);
            var go = Object.Instantiate(fbx);
            go.transform.rotation = Quaternion.Euler(0f, float.Parse(Env("VIEW_YAW", "0")), 0f);
            var bones = Bones(go);

            // informe: huesos de la animacion que existen en el modelo y su rotacion local de reposo frente a la del fotograma 0
            Debug.Log("[VIEW] huesos del modelo: " + string.Join(", ", bones.Keys.Where(k => k.StartsWith("j_") || k.StartsWith("tag_")).ToArray()));
            foreach (var c in clip.curves.Where(c => c.prop == "rq"))
            {
                Transform t;
                if (!bones.TryGetValue(c.bone, out t)) continue;
                var v = c.values[0];
                Debug.Log("[VIEW] " + c.bone + " reposo(Unity)=" + t.localRotation.ToString("F3") + " cast0=(" + string.Join(",", v.Select(x => x.ToString("F3")).ToArray()) + ")");
            }

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var sk = r as SkinnedMeshRenderer;
                if (sk == null) { Debug.Log("[VIEW] malla rigida: " + r.name + " padre=" + r.transform.parent.name); continue; }
                int used = 0; var w = sk.sharedMesh.boneWeights; var infl = new HashSet<int>(); foreach (var bw in w) { if (bw.weight0 > 0) infl.Add(bw.boneIndex0); if (bw.weight1 > 0) infl.Add(bw.boneIndex1); }
                Debug.Log("[VIEW] malla con piel: " + sk.name + " huesos=" + sk.bones.Length + " [" + string.Join(",", sk.bones.Select(x => x != null ? x.name : "null").ToArray()) + "] con peso=" + string.Join(",", infl.Select(i => i < sk.bones.Length && sk.bones[i] != null ? sk.bones[i].name : "?" + i).ToArray()) + " root=" + (sk.rootBone != null ? sk.rootBone.name : "null"));
            }
            // camara y luz
            var camGo = new GameObject("cam"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.24f, 0.28f);
            var lightGo = new GameObject("luz"); var light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            var rends = go.GetComponentsInChildren<Renderer>();
            Bounds b = rends.Length > 0 ? rends[0].bounds : new Bounds(go.transform.position, Vector3.one * 0.1f);
            foreach (var r in rends) b.Encapsulate(r.bounds);

            var frames = ParseFrames(Env("VIEW_FRAMES", "0-100/10"), clip.frames);
            int W = 420, H = 420, cols = Mathf.Min(frames.Count, 6), rows = (frames.Count + cols - 1) / cols;
            var sheet = new Texture2D(W * cols, H * rows, TextureFormat.RGB24, false);
            sheet.SetPixels(Enumerable.Repeat(cam.backgroundColor, W * cols * H * rows).ToArray());
            var baked = new List<GameObject>();
            var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
            var cp = Env("VIEW_CAM", "0.6,0.5,-1").Split(',').Select(float.Parse).ToArray();      // direccion de la camara; la distancia sale del tamano del modelo
            float zoom = float.Parse(Env("VIEW_ZOOM", "1.2")), focusY = float.Parse(Env("VIEW_FOCUSY", "0"));
            var dir = new Vector3(cp[0], cp[1], cp[2]).normalized;
            var focus = b.center + Vector3.up * (b.size.y * focusY);
            for (int i = 0; i < frames.Count; i++)
            {
                Pose(bones, clip, frames[i], null);
                // en batch Unity no actualiza el skinning entre fotogramas: se hornea la malla con el pose actual y se dibuja una copia rigida
                foreach (var old in baked) Object.DestroyImmediate(old);
                baked.Clear();
                foreach (var sk in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    sk.enabled = false;
                    var m = new Mesh(); sk.BakeMesh(m);
                    var bgo = new GameObject("baked"); bgo.transform.SetPositionAndRotation(sk.transform.position, sk.transform.rotation); bgo.transform.localScale = sk.transform.lossyScale;
                    bgo.AddComponent<MeshFilter>().sharedMesh = m; bgo.AddComponent<MeshRenderer>().sharedMaterials = sk.sharedMaterials;
                    baked.Add(bgo);
                }
                cam.transform.position = focus + dir * (b.size.magnitude * zoom); cam.transform.LookAt(focus);
                cam.nearClipPlane = b.size.magnitude * 0.02f; cam.farClipPlane = b.size.magnitude * 20f;
                cam.Render();
                RenderTexture.active = rt;
                var tile = new Texture2D(W, H, TextureFormat.RGB24, false); tile.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                int cx = (i % cols) * W, cy = (rows - 1 - i / cols) * H;
                sheet.SetPixels(cx, cy, W, H, tile.GetPixels());
                Object.DestroyImmediate(tile);
            }
            RenderTexture.active = null;
            Directory.CreateDirectory("Preview");
            string outName = "Preview/" + Env("VIEW_OUT", "anim_sheet") + ".png";
            File.WriteAllBytes(outName, sheet.EncodeToPNG());
            Debug.Log("[VIEW] hoja guardada: " + outName + " (" + string.Join(",", frames.Select(f => f.ToString("0.#")).ToArray()) + ")");
        }

        private static List<float> ParseFrames(string spec, int total)
        {
            var res = new List<float>();
            var m = Regex.Match(spec, @"^(\d+)-(\d+)/(\d+)$");
            if (m.Success) { int a = int.Parse(m.Groups[1].Value), z = int.Parse(m.Groups[2].Value), s = int.Parse(m.Groups[3].Value); for (int f = a; f <= z; f += s) res.Add(f); }
            else foreach (var p in spec.Split(',')) res.Add(float.Parse(p));
            return res;
        }
    }

    // Lector JSON minimo (Unity no trae uno con diccionarios)
    internal static class MiniJson
    {
        public static object Parse(string s) { int i = 0; return Val(s, ref i); }
        private static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        private static object Val(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i); var k = (string)Val(s, ref i); Ws(s, ref i); i++;   // :
                    d[k] = Val(s, ref i); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return d;
                }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Val(s, ref i)); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return l;
                }
            }
            if (c == '"') { int e = i + 1; var sb = new System.Text.StringBuilder(); while (s[e] != '"') { if (s[e] == '\\') e++; sb.Append(s[e]); e++; } i = e + 1; return sb.ToString(); }
            if (s.Substring(i, 4) == "true") { i += 4; return true; }
            if (s.Substring(i, 5) == "false") { i += 5; return false; }
            if (s.Substring(i, 4) == "null") { i += 4; return null; }
            int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
