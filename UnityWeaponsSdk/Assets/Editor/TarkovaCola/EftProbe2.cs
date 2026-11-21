using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AnimationEventSystem;
using UnityEditor;
using UnityEngine;

// Sonda de un arma base de EFT: jerarquia del arma (ranuras y huesos), clips con su duracion, y por cada estado con eventos
// (AnimationEventsStateBehaviour.FullName) sus eventos (StaticData, tiempo normalizado) y los clips que reproduce.
//   EFT_PROBE="ak12=assets/content/weapons/ak12/weapon_izhmash_ak12_545x39_container.bundle;..."  -> Preview/probe_<nombre>.txt
//   Unity.exe -batchmode -quit -projectPath UnityWeaponsSdk -executeMethod TarkovaCola.EftProbe2.Run
namespace TarkovaCola
{
    public static class EftProbe2
    {
        public static void Run()
        {
            foreach (var entry in (System.Environment.GetEnvironmentVariable("EFT_PROBE") ?? "").Split(';').Where(s => s.Contains("=")))
            {
                string name = entry.Split('=')[0], bundle = entry.Substring(name.Length + 1);
                try { Probe(name, bundle); } catch (System.Exception e) { Debug.LogError("[PROBE] " + name + ": " + e); }
                EftBundles.UnloadAll();
            }
        }

        private static void Probe(string name, string bundle)
        {
            var ab = EftBundles.Load(bundle);
            var sb = new StringBuilder();
            var rig = EftLab.LoadRig(bundle);
            sb.AppendLine("### " + bundle);
            sb.AppendLine("## jerarquia (desde Weapon_root)");
            var wr = rig.Root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Weapon_root");
            if (wr != null) Walk(wr, 0, sb, 5);
            sb.AppendLine("## clips");
            foreach (var c in rig.Clips.Where(c => !c.name.Contains(".generated")).OrderBy(c => c.name)) sb.AppendLine("  " + c.name + " " + c.length.ToString("F3") + (c.isLooping ? " loop" : ""));

            var ctrls = EftBundles.Family(bundle).SelectMany(b => b.LoadAllAssets<RuntimeAnimatorController>()).Where(c => !(c is AnimatorOverrideController)).ToList();
            foreach (var c in ctrls) sb.AppendLine("## controlador disponible: " + c.name + " (" + c.animationClips.Length + " clips)");
            var want = System.Environment.GetEnvironmentVariable("EFT_PROBE_CTRL");
            var ctrl = ctrls.FirstOrDefault(c => want != null && c.name.Contains(want)) ?? ctrls.FirstOrDefault();
            if (ctrl == null) { sb.AppendLine("sin controlador"); Save(name, sb); return; }
            var an = rig.Root.GetComponent<Animator>() ?? rig.Root.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            an.Rebind(); an.Update(0f);
            sb.AppendLine("## controlador " + ctrl.name + ": capas " + string.Join(", ", Enumerable.Range(0, an.layerCount).Select(i => i + ":" + an.GetLayerName(i))));
            sb.AppendLine("## parametros: " + string.Join(" ", an.parameters.Select(p => p.name + ":" + p.type.ToString()[0] + "=" + (p.type == AnimatorControllerParameterType.Float ? p.defaultFloat.ToString("0.##") : p.type == AnimatorControllerParameterType.Int ? p.defaultInt.ToString() : p.defaultBool ? "1" : "0"))));
            var floats = an.parameters.Where(p => p.type == AnimatorControllerParameterType.Float).Select(p => p.name).ToArray();
            string[] sweep = { "RelTypeOld", "RelTypeNew", "AnimationVariant", "IdleVar", "FireVar", "MalfType", "WeaponLevel", "StockAnimationIndex", "ChamberIndex", "AmmoInChamber", "ShellsInWeapon", "CamoraIndex", "FireMode" };
            sb.AppendLine("## estados con eventos");
            var behs = an.GetBehaviours<AnimationEventsStateBehaviour>();
            foreach (var b in behs.OrderBy(b => b.FullName))
            {
                int hash = Animator.StringToHash(b.FullName);
                int layer = -1;
                for (int i = 0; i < an.layerCount; i++) if (an.HasState(i, hash)) { layer = i; break; }
                var clips = new SortedDictionary<string, float>();
                float speed = 1f, len = 0f;
                if (layer >= 0)
                {
                    foreach (var p in sweep.Where(floats.Contains).Concat(new[] { "" }))
                        for (int v = 0; v <= (p == "" ? 0 : 7); v++)
                        {
                            if (p != "") an.SetFloat(p, v);
                            an.Play(hash, layer, 0f); an.Update(0f);
                            var si = an.GetCurrentAnimatorStateInfo(layer);
                            if (si.fullPathHash != hash) continue;
                            speed = si.speed * si.speedMultiplier; len = si.length;
                            foreach (var ci in an.GetCurrentAnimatorClipInfo(layer)) if (ci.clip != null && ci.weight > 0.01f) clips[ci.clip.name] = ci.clip.length;
                            if (p != "") an.SetFloat(p, an.parameters.First(q => q.name == p).defaultFloat);
                        }
                }
                sb.AppendLine("  [" + (layer >= 0 ? an.GetLayerName(layer) : "?") + "] " + b.FullName + "  vel " + speed.ToString("0.##") + "  clips: " + string.Join(", ", clips.Select(c => c.Key + "(" + c.Value.ToString("F2") + ")")));
                var evs = Events(b.EventsData, b.EventsListId);
                if (evs.Count > 0) sb.AppendLine("      " + string.Join("  ", evs));
            }
            Save(name, sb);
            Object.DestroyImmediate(rig.Root);
        }

        // los campos del StaticData son privados en el SDK: se leen por JSON
        private static readonly Dictionary<Object, List<object>> _sd = new Dictionary<Object, List<object>>();
        private static List<string> Events(Object data, int id)
        {
            var res = new List<string>();
            if (data == null || id < 0) return res;
            List<object> cols;
            if (!_sd.TryGetValue(data, out cols)) _sd[data] = cols = (List<object>)((Dictionary<string, object>)Bo7Anim.Json.Parse(JsonUtility.ToJson(data)))["_stateHashToEventsCollection"];
            if (id >= cols.Count) return res;
            foreach (Dictionary<string, object> e in ((List<object>)((Dictionary<string, object>)cols[id])["_animationEvents"]).Cast<Dictionary<string, object>>().OrderBy(e => System.Convert.ToDouble(e["_time"])))
            {
                string par = "";
                if (e.TryGetValue("Parameter", out var po) && po is Dictionary<string, object> pd)
                    par = string.Join(",", pd.Where(kv => !kv.Key.StartsWith("ParamType") && kv.Value != null && kv.Value.ToString() != "0" && kv.Value.ToString() != "" && kv.Value.ToString() != "False").Select(kv => kv.Value.ToString()));
                bool cond = e.TryGetValue("EventConditions", out var co) && co is List<object> cl && cl.Count > 0;
                res.Add(e["_functionName"] + (par != "" ? "(" + par + ")" : "") + "@" + System.Convert.ToDouble(e["_time"]).ToString("0.###") + (cond ? "?" : "") + ((e.TryGetValue("Enabled", out var en) && en is bool eb && !eb) ? "!off" : ""));
            }
            return res;
        }

        private static void Save(string name, StringBuilder sb)
        {
            Directory.CreateDirectory("Preview");
            File.WriteAllText("Preview/probe_" + name + ".txt", sb.ToString());
            Debug.Log("[PROBE] " + name + " -> Preview/probe_" + name + ".txt");
        }

        private static void Walk(Transform t, int d, StringBuilder sb, int max)
        {
            if (t.name.StartsWith("Base Human")) return;
            sb.Append(' ', d * 2).Append(t.name);
            var mf = t.GetComponent<MeshFilter>(); if (mf != null && mf.sharedMesh != null) sb.Append(" <malla>");
            sb.AppendLine(" p" + t.localPosition.ToString("F3") + " r" + t.localEulerAngles.ToString("F0"));
            if (d >= max) return;
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), d + 1, sb, max);
        }
    }
}
