using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Animaciones de BO7 en la VMP y la MK35 (recarga, inspeccion, sacar/guardar): clips horneados sobre el rig de EFT (UnityWeaponsSdk, Bo7Anim.cs) en
    //  assets/bo7_anims.bundle. Con el arma en la mano, el controlador de animaciones del arma base se cambia por un
    //  AnimatorOverrideController (como Manimal.WatchAnims) que pone nuestros clips en lugar de los de EFT en los mismos estados: asi los
    //  eventos de EFT (sonidos, sacar/meter cargador, red) siguen saltando, y los clips estan hechos para que caigan en su momento.
    //  Solo se cambia con las manos en reposo (cambiar el controlador reinicia la maquina de estados) y se deshace al cambiar de arma.
    // ============================================================================================
    internal static class Bo7Anims
    {
        private const string OurName = "TarkovaCola BO7";

        // prefijo de los clips horneados de cada arma: el clip "<prefijo><clip de EFT>" (o sin su _N final: mp5_mag_out_0..2 ->
        // vmp_mp5_mag_out) va en lugar de ese clip de EFT
        private static readonly Dictionary<string, string> Prefix = Bo7Guns.All.ToDictionary(g => g.Tpl, g => g.Prefix);

        private static bool Ours(string prefix, string eft, out AnimationClip clip)
        {
            if (_clips.TryGetValue(prefix + eft, out clip)) return true;
            int u = eft.LastIndexOf('_');
            return u > 0 && u + 1 < eft.Length && char.IsDigit(eft[u + 1]) && _clips.TryGetValue(prefix + eft.Substring(0, u), out clip);
        }

        private static AssetBundle _bundle;
        private static bool _bundleFailed;
        private static readonly Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();
        private static readonly HashSet<AnimationClip> _ours = new HashSet<AnimationClip>();

        // armas cuerpo a cuerpo de EFT con clips de BO7 (bo7_melee.json: tpl -> prefijo)
        private static readonly Dictionary<string, string> _melee = new Dictionary<string, string>();
        internal static string MeleePrefix(string tpl) { string p; return tpl != null && LoadClips() && _melee.TryGetValue(tpl, out p) ? p : null; }

        internal static bool IsOurClip(AnimationClip c) { return c != null && _ours.Contains(c); }
        // animador -> controlador original (para deshacer)
        private static readonly Dictionary<IAnimator, RuntimeAnimatorController> _original = new Dictionary<IAnimator, RuntimeAnimatorController>();
        private static float _next;

        // Eventos de EFT recolocados (bo7_events.json: arma -> estado -> evento -> tiempo normalizado). Las animaciones de BO7 van a su ritmo:
        // en vez de deformarlas para que cuadren con los eventos de EFT (cargador fuera/dentro, bala en recamara...), se clona el StaticData
        // del arma y se mueven los eventos al momento en que ocurren en BO7. Cada estado lo lleva su AnimationEventsStateBehaviour.
        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, float>>> _events = new Dictionary<string, Dictionary<string, Dictionary<string, float>>>();
        private static readonly List<KeyValuePair<AnimationEventSystem.AnimationEventsStateBehaviour, AnimationEventSystem.AnimatorControllerStaticData>> _retimed = new List<KeyValuePair<AnimationEventSystem.AnimationEventsStateBehaviour, AnimationEventSystem.AnimatorControllerStaticData>>();

        private static void Retime(Animator unity, string key)
        {
            Unretime();
            Dictionary<string, Dictionary<string, float>> states;
            if (unity == null || !_events.TryGetValue(key, out states)) return;
            var fCols = HarmonyLib.AccessTools.Field(typeof(AnimationEventSystem.AnimatorControllerStaticData), "_stateHashToEventsCollection");
            var fEvs = HarmonyLib.AccessTools.Field(typeof(AnimationEventSystem.EventsCollection), "_animationEvents");
            var clones = new Dictionary<AnimationEventSystem.AnimatorControllerStaticData, AnimationEventSystem.AnimatorControllerStaticData>();
            int n = 0;
            foreach (var b in unity.GetBehaviours<AnimationEventSystem.AnimationEventsStateBehaviour>())
            {
                if (b == null || b.EventsData == null || string.IsNullOrEmpty(b.FullName)) continue;
                int dot = b.FullName.IndexOf('.');
                Dictionary<string, float> times;
                if (!states.TryGetValue(dot >= 0 ? b.FullName.Substring(dot + 1) : b.FullName, out times)) continue;
                AnimationEventSystem.AnimatorControllerStaticData clone;
                if (!clones.TryGetValue(b.EventsData, out clone)) clones[b.EventsData] = clone = Object.Instantiate(b.EventsData);
                var cols = fCols.GetValue(clone) as System.Collections.IList;
                if (cols == null || b.EventsListId < 0 || b.EventsListId >= cols.Count) continue;
                var evs = fEvs.GetValue(cols[b.EventsListId]) as System.Collections.IList;
                if (evs == null) continue;
                var seen = new Dictionary<string, int>();
                foreach (AnimationEventSystem.AnimationEvent e in evs)
                {
                    string name = e.FunctionName ?? "";
                    int k; seen.TryGetValue(name, out k); seen[name] = ++k;
                    float t;
                    if (times.TryGetValue(k == 1 ? name : name + "#" + k, out t)) { e.Time = t; n++; }
                }
                _retimed.Add(new KeyValuePair<AnimationEventSystem.AnimationEventsStateBehaviour, AnimationEventSystem.AnimatorControllerStaticData>(b, b.EventsData));
                b.EventsData = clone;
            }
            Dbg.Log("BO7", "eventos recolocados (" + key + "): " + n + " en " + _retimed.Count + " estados");
        }

        private static void Unretime()
        {
            foreach (var kv in _retimed) if (kv.Key != null) kv.Key.EventsData = kv.Value;
            _retimed.Clear();
        }

        private static bool LoadClips()
        {
            if (_bundle != null) return true;
            if (_bundleFailed) return false;
            string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", "bo7_anims.bundle"));
            _bundle = File.Exists(path) ? AssetBundle.LoadFromFile(path) : null;
            if (_bundle == null) { _bundleFailed = true; Plugin.Log.LogWarning("Animaciones BO7: no se pudo cargar " + path); return false; }
            foreach (var c in _bundle.LoadAllAssets<AnimationClip>()) { _clips[c.name] = c; _ours.Add(c); }
            var me = _bundle.LoadAllAssets<TextAsset>().FirstOrDefault(t => t.name == "bo7_melee");
            if (me != null)
                try { foreach (var kv in Newtonsoft.Json.Linq.JObject.Parse(me.text)) _melee[kv.Key] = (string)kv.Value; }
                catch (System.Exception e) { Plugin.Log.LogWarning("Animaciones BO7: bo7_melee.json: " + e.Message); }
            var ev = _bundle.LoadAllAssets<TextAsset>().FirstOrDefault(t => t.name == "bo7_events");
            if (ev != null)
                try
                {
                    foreach (var gun in Newtonsoft.Json.Linq.JObject.Parse(ev.text))
                    {
                        var states = _events[gun.Key] = new Dictionary<string, Dictionary<string, float>>();
                        foreach (var st in (Newtonsoft.Json.Linq.JObject)gun.Value)
                            states[st.Key] = ((Newtonsoft.Json.Linq.JObject)st.Value).Properties().ToDictionary(p => p.Name, p => (float)p.Value);
                    }
                }
                catch (System.Exception e) { Plugin.Log.LogWarning("Animaciones BO7: bo7_events.json: " + e.Message); }
            Dbg.Log("BO7", "animaciones cargadas: " + string.Join(", ", _clips.Keys.ToArray()));
            return true;
        }

        internal static void Tick()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.05f;
            var me = Plugin.AnyMe;
            var hc = me != null ? me.HandsController : null;
            var fa = hc != null ? hc.FirearmsAnimator : null;
            var animator = fa != null ? fa.Animator : null;
            if (animator == null) return;
            var ctrl = animator.runtimeAnimatorController;
            bool oursNow = ctrl != null && ctrl.name == OurName;
            string tpl = hc.Item != null ? hc.Item.StringTemplateId : null;
            string prefix;
            bool want = tpl != null && Prefix.TryGetValue(tpl, out prefix);
            if (want == oursNow || !fa.IsIdling()) return;
            if (want) Apply(animator, Prefix[tpl]);
            else Restore(animator);
        }

        internal static void Apply(IAnimator animator, string prefix, string name = OurName)
        {
            if (!LoadClips()) return;
            var current = animator.runtimeAnimatorController;
            if (current == null) return;
            // el del juego suele ser ya un override ("_simple"): se parte de su controlador base y se copian sus cambios
            var currentOv = current as AnimatorOverrideController;
            var baseCtrl = currentOv != null ? currentOv.runtimeAnimatorController : current;
            var ov = new AnimatorOverrideController(baseCtrl) { name = name };
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(ov.overridesCount);
            ov.GetOverrides(pairs);
            var inherited = new Dictionary<AnimationClip, AnimationClip>();
            if (currentOv != null)
            {
                var cur = new List<KeyValuePair<AnimationClip, AnimationClip>>(currentOv.overridesCount);
                currentOv.GetOverrides(cur);
                foreach (var kv in cur) if (kv.Key != null && kv.Value != null) inherited[kv.Key] = kv.Value;
            }
            int n = 0;
            for (int i = 0; i < pairs.Count; i++)
            {
                var key = pairs[i].Key;
                if (key == null) continue;
                AnimationClip value;
                if (Ours(prefix, key.name, out value)) n++;
                else inherited.TryGetValue(key, out value);
                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, value);
            }
            ov.ApplyOverrides(pairs);
            _original[animator] = current;
            animator.runtimeAnimatorController = ov;
            // los clips llevan eventos "Bo7Snd" (sonidos de BO7): Unity los manda a los componentes del GameObject del animador
            var unity = (animator as AnimationSystem.UnityAnimatorWrapper)?.Animator;
            if (unity != null && unity.GetComponent<Bo7SoundRelay>() == null) unity.gameObject.AddComponent<Bo7SoundRelay>();
            var gun = Bo7Guns.All.FirstOrDefault(g => g.Prefix == prefix);
            if (gun != null) Retime(unity, gun.Key);
            Dbg.Log("BO7", "animaciones de BO7 puestas en " + baseCtrl.name + ": " + n + " clips cambiados");
        }

        internal static void Restore(IAnimator animator)
        {
            RuntimeAnimatorController orig;
            if (!_original.TryGetValue(animator, out orig) || orig == null) return;
            Unretime();
            animator.runtimeAnimatorController = orig;
            _original.Remove(animator);
            Dbg.Log("BO7", "animaciones de BO7 quitadas (" + orig.name + ")");
        }
    }
}
