using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TarkovaCola
{
    // Sondeo: abre un bundle de EFT con sus dependencias y vuelca sus assets, la jerarquia de los prefabs y los clips de los controladores.
    //   EFT_BUNDLE=assets/content/... EFT_OUT=<txt> Unity.exe -batchmode -quit -projectPath UnityBundle -executeMethod TarkovaCola.EftProbe.Run
    public static class EftProbe
    {
        public static void Run()
        {
            var sb = new StringBuilder();
            try
            {
                var ab = EftBundles.Load(System.Environment.GetEnvironmentVariable("EFT_BUNDLE"));
                foreach (var n in ab.GetAllAssetNames()) sb.AppendLine("ASSET " + n);
                foreach (var o in ab.LoadAllAssets())
                {
                    sb.AppendLine("OBJ " + o.GetType().Name + " " + o.name);
                    var go = o as GameObject;
                    if (go != null && System.Environment.GetEnvironmentVariable("EFT_TREE") == "1") Dump(go.transform, 0, sb);
                    var ov = o as AnimatorOverrideController;
                    if (ov != null)
                    {
                        var pairs = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                        ov.GetOverrides(pairs);
                        foreach (var p in pairs)
                        {
                            var c = p.Value ?? p.Key;
                            sb.AppendLine("   " + (p.Key != null ? p.Key.name : "?") + " -> " + (c != null ? c.name + " len=" + c.length.ToString("F2") + " fps=" + c.frameRate + " ev=" + string.Join("|", c.events.Select(e => e.functionName + "@" + e.time.ToString("F2")).ToArray()) : "null"));
                        }
                    }
                    else
                    {
                        var ctrl = o as RuntimeAnimatorController;
                        if (ctrl != null) foreach (var c in ctrl.animationClips) if (c != null) sb.AppendLine("   clip " + c.name + " len=" + c.length.ToString("F2"));
                    }
                }
            }
            catch (System.Exception e) { sb.AppendLine("ERROR " + e); }
            System.IO.File.WriteAllText(System.Environment.GetEnvironmentVariable("EFT_OUT") ?? "probe.txt", sb.ToString());
            EftBundles.UnloadAll();
        }

        private static void Dump(Transform t, int d, StringBuilder sb)
        {
            if (d > 40) return;
            var comps = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name);
            sb.AppendLine(new string(' ', d * 2) + t.name + "  [" + string.Join(",", comps.ToArray()) + "]" + (t.GetComponents<Component>().Any(c => c == null) ? " +SCRIPT_FALTANTE" : ""));
            var sk = t.GetComponent<SkinnedMeshRenderer>();
            if (sk != null)
                sb.AppendLine("    huesos=" + sk.bones.Length + " [" + string.Join(",", sk.bones.Select(b => b != null ? b.name : "null").ToArray()) + "] bindposes=" + (sk.sharedMesh != null ? sk.sharedMesh.bindposes.Length : -1)
                              + " root=" + (sk.rootBone != null ? sk.rootBone.name : "null") + " mats=" + string.Join(",", sk.sharedMaterials.Select(m => m != null ? m.name + "/" + (m.shader != null ? m.shader.name : "?") : "null").ToArray()));
            foreach (var mb in t.GetComponents<Component>().Where(c => c == null).Take(1))
            {
                var so = new SerializedObject(t.gameObject);
                var it = so.FindProperty("m_Component");
                sb.AppendLine("    componentes serializados=" + (it != null ? it.arraySize : -1));
            }
            foreach (Transform c in t) Dump(c, d + 1, sb);
        }
    }
}
