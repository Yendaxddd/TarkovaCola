using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TarkovaCola
{
    // Laboratorio de animaciones con el esqueleto REAL de EFT: carga el rig de primera persona de un arma del juego (brazos Base Human*,
    // Camera_animated, Weapon_root), le pone las manos (piel) del juego, muestrea un clip fotograma a fotograma y guarda una hoja PNG
    // vista desde la camara de primera persona (o desde un lado). Asi se ve una animacion antes de llevarla al juego.
    //   LAB_WEAPON=assets/content/weapons/m48_kukri/weapon_united_cutlery_m48_kukri_container.bundle
    //   LAB_CLIP=kukri_fire1   LAB_FRAMES=0-21/1 (fotogramas a 30 fps; inicio-fin/paso)   LAB_VIEW=fps|side|both   LAB_OUT=<png>
    //   Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.EftLab.Run -logFile l.log      (sin -nographics)
    public static class EftLab
    {
        private const string HandsBundle = "assets/content/hands/bear/bear_hands_skin.bundle";

        private static string Env(string k, string d) { return System.Environment.GetEnvironmentVariable(k) ?? d; }

        public static void Run()
        {
            try { Render(); }
            catch (System.Exception e) { Debug.LogError("[LAB] " + e); }
            finally { EftBundles.UnloadAll(); }
        }

        internal sealed class Rig
        {
            public GameObject Root;
            public Dictionary<string, Transform> Bones = new Dictionary<string, Transform>();
            public Transform CamAnim;
            public List<AnimationClip> Clips = new List<AnimationClip>();
        }

        internal static Rig LoadRig(string weaponBundle)
        {
            var ab = EftBundles.Load(weaponBundle);
            var rig = new Rig();
            // armas: *_model.generated; objetos usables (latas, inyectores): *.generated (sin _simple)
            var all = ab.LoadAllAssets<GameObject>();
            var prefab = all.FirstOrDefault(g => g.name.EndsWith("_model.generated"))
                         ?? all.FirstOrDefault(g => g.name.EndsWith(".generated") && !g.name.Contains("_simple") && g.transform.Find("Base HumanLCollarbone") != null);
            if (prefab == null) throw new System.Exception("el bundle no trae un rig de primera persona (*.generated): " + weaponBundle);
            rig.Root = Object.Instantiate(prefab);
            rig.Root.transform.position = Vector3.zero;
            foreach (var t in rig.Root.GetComponentsInChildren<Transform>(true)) if (!rig.Bones.ContainsKey(t.name)) rig.Bones[t.name] = t;
            rig.CamAnim = rig.Bones["Camera_animated"];
            // clips: los de la sobreescritura "simple" (los que usa el juego en primera persona) y los del controlador base
            foreach (var ov in ab.LoadAllAssets<AnimatorOverrideController>())
            {
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(); ov.GetOverrides(pairs);
                foreach (var p in pairs) if (p.Value != null && !rig.Clips.Contains(p.Value)) rig.Clips.Add(p.Value);   // nombre: <clip>_simple.generated
            }
            foreach (var ctrl in ab.LoadAllAssets<RuntimeAnimatorController>())
                foreach (var c in ctrl.animationClips) if (c != null && rig.Clips.All(x => x.name != c.name)) rig.Clips.Add(c);
            // apagar LODs lejanos y colisionadores visibles
            foreach (var r in rig.Root.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("LOD1") || r.name.Contains("LOD2") || r.name.Contains("LOD3")) r.enabled = false;

            // manos: piel del juego enlazada por nombre a los huesos del rig (los nombres salen de tools/eft_skin_bones.py)
            var hab = EftBundles.Load(HandsBundle);
            var skin = Object.Instantiate(hab.LoadAllAssets<GameObject>().First());
            var map = MiniJson(System.IO.File.ReadAllText("Assets/EftLab/bear_hands_bones.json"));
            foreach (var sk in skin.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                sk.bones = map.Select(n => rig.Bones.ContainsKey(n) ? rig.Bones[n] : null).ToArray();
                sk.rootBone = rig.Bones["Base HumanRUpperarm"];
                sk.transform.SetParent(rig.Root.transform, false);
            }
            Object.DestroyImmediate(skin);
            Simplify(rig.Root);
            return rig;
        }

        // los shaders de EFT dependen de su render; para ver la forma basta con Standard y la textura base
        private static void Simplify(GameObject go)
        {
            var std = Shader.Find("Standard");
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials.Select(m =>
                {
                    var n = new Material(std);
                    if (m == null) return n;
                    if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null) n.mainTexture = m.GetTexture("_MainTex");
                    if (m.HasProperty("_Color")) n.color = m.GetColor("_Color");
                    if (m.name.ToLower().Contains("glass")) n.color = new Color(0.6f, 0.7f, 0.8f);
                    n.SetFloat("_Glossiness", 0.25f);
                    return n;
                }).ToArray();
                r.sharedMaterials = mats;
            }
        }

        private static List<string> MiniJson(string json)
        {
            int i = json.IndexOf("\"bones\""); int e = json.IndexOf(']', i);
            return System.Text.RegularExpressions.Regex.Matches(json.Substring(i + 7, e - i - 7), "\"([^\"]+)\"").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).ToList();
        }

        // primero la version "simple" (la de primera persona), luego el nombre exacto
        internal static AnimationClip FindClip(Rig rig, string name)
        {
            string set = Env("LAB_SET", "base");   // base = clip completo (brazos, cuchillo, camara, IK); simple/spirit solo traen brazos o nada
            return (set == "base" ? null : rig.Clips.FirstOrDefault(c => c.name == name + "_" + set + ".generated")) ?? rig.Clips.FirstOrDefault(c => c.name == name);
        }

        internal static List<int> ParseFrames(string spec, int max)
        {
            var res = new List<int>();
            foreach (var part in spec.Split(','))
            {
                var p = part.Split('/'); int step = p.Length > 1 ? int.Parse(p[1]) : 1;
                var r = p[0].Split('-'); int a = int.Parse(r[0]), b = r.Length > 1 ? (r[1] == "end" ? max : int.Parse(r[1])) : a;
                for (int f = a; f <= Mathf.Min(b, max); f += step) res.Add(f);
            }
            return res;
        }

        private static void Render()
        {
            var rig = LoadRig(Env("LAB_WEAPON", "assets/content/weapons/m48_kukri/weapon_united_cutlery_m48_kukri_container.bundle"));
            string clipName = Env("LAB_CLIP", "kukri_fire1");
            if (Env("LAB_LIST", "0") == "1") Debug.Log("[LAB] clips: " + string.Join(", ", rig.Clips.Select(c => c.name + "(" + c.length.ToString("F2") + ")").ToArray()));
            var clip = FindClip(rig, clipName);
            if (clip == null) throw new System.Exception("no hay clip " + clipName + "; hay: " + string.Join(", ", rig.Clips.Select(c => c.name).ToArray()));
            if (Env("LAB_LIST", "0") == "1")
            {
                // los clips de un bundle no exponen sus curvas: se mide que huesos cambian entre dos instantes
                var snap = new Dictionary<Transform, Matrix4x4>();
                clip.SampleAnimation(rig.Root, 0f);
                foreach (var t in rig.Bones.Values) snap[t] = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
                clip.SampleAnimation(rig.Root, clip.length * 0.4f);
                foreach (var t in rig.Bones.Values)
                {
                    var a = snap[t];
                    float dp = Vector3.Distance(a.GetColumn(3), t.localPosition), dr = Quaternion.Angle(a.rotation, t.localRotation);
                    if (dp > 1e-4f || dr > 0.05f) Debug.Log("[LAB] se mueve " + t.name + " dp=" + dp.ToString("F3") + " dr=" + dr.ToString("F1"));
                }
            }
            int total = Mathf.RoundToInt(clip.length * 30f);
            RenderSheet(rig, f => clip.SampleAnimation(rig.Root, f / 30f), ParseFrames(Env("LAB_FRAMES", "0-end/2"), total), Env("LAB_OUT", "lab.png"));
            Debug.Log("[LAB] " + clipName + " " + clip.length.ToString("F2") + " s");
        }

        // Hoja de fotogramas: pose(f) deja el rig en el fotograma f. Vista "fps" (camara de primera persona: Camera_animated, que mira por
        // su +Y con -Z arriba, girado 90 en X), "side" (de lado) o "both".
        internal static void RenderSheet(Rig rig, System.Action<int> pose, List<int> frames, string outPath)
        {
            string view = Env("LAB_VIEW", "fps");
            var views = view == "both" ? new[] { "fps", "side" } : new[] { view };
            var camOffset = Quaternion.Euler(90f, 0f, 0f);
            var camGo = new GameObject("cam"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.36f, 0.42f, 0.48f);
            cam.nearClipPlane = 0.01f; cam.farClipPlane = 20f;
            cam.fieldOfView = float.Parse(Env("LAB_FOV", "65"));
            var light = new GameObject("luz").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(35f, -40f, 0f); light.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
            int W = 480, H = 270, cols = Mathf.Min(int.Parse(Env("LAB_COLS", "6")), frames.Count * views.Length);
            int n = frames.Count * views.Length, rows = (n + cols - 1) / cols;
            var sheet = new Texture2D(W * cols, H * rows, TextureFormat.RGB24, false);
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 }; cam.targetTexture = rt;
            var baked = new List<GameObject>();
            int k = 0;
            foreach (int f in frames)
            {
                pose(f);
                // en batch Unity no actualiza el skinning: se hornea la malla con la pose y se dibuja una copia rigida
                foreach (var old in baked) Object.DestroyImmediate(old);
                baked.Clear();
                foreach (var sk in rig.Root.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!sk.enabled) continue;
                    sk.forceRenderingOff = false;
                    var m = new Mesh(); sk.BakeMesh(m);
                    var b = new GameObject("baked"); b.transform.SetPositionAndRotation(sk.transform.position, sk.transform.rotation);
                    b.AddComponent<MeshFilter>().sharedMesh = m; b.AddComponent<MeshRenderer>().sharedMaterials = sk.sharedMaterials;
                    baked.Add(b);
                    sk.forceRenderingOff = true;
                }
                var camRot = rig.CamAnim.rotation * camOffset;
                foreach (var v in views)
                {
                    if (v == "fps") cam.transform.SetPositionAndRotation(rig.CamAnim.position, camRot);
                    else { var fwd = camRot * Vector3.forward; var right = camRot * Vector3.right; var c = rig.CamAnim.position + fwd * 0.3f - Vector3.up * 0.15f; cam.transform.position = c + right * 1.0f + Vector3.up * 0.2f; cam.transform.LookAt(c); }
                    cam.Render();
                    RenderTexture.active = rt;
                    var tile = new Texture2D(W, H, TextureFormat.RGB24, false); tile.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                    if (v == "fps") { for (int x = W / 2 - 6; x <= W / 2 + 6; x++) tile.SetPixel(x, H / 2, Color.red); for (int y = H / 2 - 6; y <= H / 2 + 6; y++) tile.SetPixel(W / 2, y, Color.red); }
                    Digits(tile, 6, H - 20, f);
                    sheet.SetPixels((k % cols) * W, (rows - 1 - k / cols) * H, W, H, tile.GetPixels());
                    Object.DestroyImmediate(tile);
                    k++;
                }
            }
            sheet.Apply();
            System.IO.File.WriteAllBytes(outPath, sheet.EncodeToPNG());
            foreach (var old in baked) Object.DestroyImmediate(old);
            Object.DestroyImmediate(camGo); Object.DestroyImmediate(light.gameObject);
            Debug.Log("[LAB] " + frames.Count + " fotogramas -> " + outPath);
        }

        private static readonly string[] G = { "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001", "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111" };
        internal static void Digits(Texture2D t, int x, int y, int value)
        {
            int cx = x;
            foreach (char ch in value.ToString())
            {
                string bits = G[ch - '0'];
                for (int r = 0; r < 5; r++) for (int c = 0; c < 3; c++) if (bits[r * 3 + c] == '1')
                    for (int dy = 0; dy < 3; dy++) for (int dx = 0; dx < 3; dx++) t.SetPixel(cx + c * 3 + dx, y + (4 - r) * 3 + dy, new Color(1f, 0.9f, 0.2f));
                cx += 12;
            }
        }
    }
}
