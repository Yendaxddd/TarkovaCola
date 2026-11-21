using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimationSystem;
using Comfort.Common;
using EFT;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Sonidos de BO7 en la VMP y la MK35 (assets/bo7_sounds.bundle, de tools/bo7_sounds.py y Bo7Anim.BakeRun):
    //   - disparo: WeaponSoundPlayer.FireBullet se sustituye por el disparo de BO7 con sus capas (disparo -> mecanica -> graves) y la
    //     variante de la ultima bala y con silenciador (la VMP no tiene silenciador exportado: con silenciador suena el de EFT)
    //   - recarga, inspeccion, sacar/guardar, selector: los clips horneados llevan eventos "Bo7Snd" con el alias de BO7 en su fotograma;
    //     los recibe Bo7SoundRelay (en el animador del arma, lo pone Bo7Anims) y mientras suena uno de nuestros clips se callan los
    //     sonidos de EFT de ese arma (si no, sonarian las dos recargas a la vez)
    // ============================================================================================
    internal static class Bo7Sounds
    {
        private sealed class Alias { public AudioClip[] Clips; public string Next; }

        private static readonly Dictionary<string, Alias> _alias = new Dictionary<string, Alias>();
        private static AssetBundle _bundle;
        private static bool _failed;
        private static readonly List<AudioSource> _pool = new List<AudioSource>();
        private static GameObject _root;

        // alias del disparo por arma (Bo7Guns.All): normal, ultima bala, silenciado, silenciado ultima bala
        private static readonly Dictionary<string, string[]> Shots = Bo7Guns.All.ToDictionary(g => g.Tpl, g => g.Shots);

        internal static bool IsOurs(string tpl) { return tpl != null && Shots.ContainsKey(tpl); }

        private static bool Load()
        {
            if (_bundle != null) return true;
            if (_failed) return false;
            string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", "bo7_sounds.bundle"));
            _bundle = File.Exists(path) ? AssetBundle.LoadFromFile(path) : null;
            if (_bundle == null) { _failed = true; Plugin.Log.LogWarning("Sonidos BO7: no se pudo cargar " + path); return false; }
            var clips = _bundle.LoadAllAssets<AudioClip>().ToDictionary(c => c.name, c => c);
            var index = _bundle.LoadAllAssets<TextAsset>().FirstOrDefault(t => t.name == "bo7_sounds");
            if (index == null) { _failed = true; Plugin.Log.LogWarning("Sonidos BO7: falta bo7_sounds.json en el bundle"); return false; }
            foreach (var kv in JObject.Parse(index.text))
            {
                var files = ((JArray)kv.Value["files"]).Select(f => { AudioClip c; return clips.TryGetValue((string)f, out c) ? c : null; }).Where(c => c != null).ToArray();
                _alias[kv.Key] = new Alias { Clips = files, Next = kv.Value["next"]?.Type == JTokenType.String ? (string)kv.Value["next"] : null };
            }
            Dbg.Log("BO7", "sonidos cargados: " + _alias.Count + " alias, " + clips.Count + " clips");
            return true;
        }

        internal static bool Has(string alias) { Alias a; return Load() && alias != null && _alias.TryGetValue(alias, out a) && a.Clips.Length > 0; }

        internal static string ShotAlias(string tpl, bool silenced, bool last)
        {
            string[] s;
            if (!Shots.TryGetValue(tpl, out s)) return null;
            string a = s[(silenced ? 2 : 0) + (last ? 1 : 0)];
            if (!Has(a) && last) a = s[silenced ? 2 : 0];                  // sin variante de ultima bala: el disparo normal
            return Has(a) ? a : null;
        }

        // el alias y su cadena de secundarios (disparo -> mecanica -> graves...), cada uno con una variante al azar
        internal static void Play(string alias, Vector3 pos, bool firstPerson, bool shot)
        {
            if (!Load()) return;
            var seen = new HashSet<string>();
            for (string a = alias; a != null && seen.Add(a);)
            {
                Alias e;
                if (!_alias.TryGetValue(a, out e)) break;
                if (e.Clips.Length > 0) Emit(e.Clips[Random.Range(0, e.Clips.Length)], pos, firstPerson, shot);
                a = e.Next;
            }
        }

        private static void Emit(AudioClip clip, Vector3 pos, bool firstPerson, bool shot)
        {
            var src = Source();
            src.transform.position = pos;
            src.spatialBlend = firstPerson ? 0f : 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = shot ? 6f : 1f;
            src.maxDistance = shot ? 500f : 25f;
            src.volume = shot ? 0.9f : 1f;
            src.pitch = 1f;
            try
            {
                if (Singleton<BetterAudio>.Instantiated)
                    src.outputAudioMixerGroup = shot ? Singleton<BetterAudio>.Instance.GunshotMixerGroup : Singleton<BetterAudio>.Instance.VeryStandartMixerGroup;
            }
            catch { }
            src.clip = clip;
            src.Play();
        }

        // fuente libre (o la que lleva mas rato sonando)
        private static AudioSource Source()
        {
            if (_root == null) { _root = new GameObject("TarkovaCola BO7 audio"); Object.DontDestroyOnLoad(_root); _pool.Clear(); }
            foreach (var s in _pool) if (s != null && !s.isPlaying) return s;
            if (_pool.Count < 24)
            {
                var go = new GameObject("src" + _pool.Count);
                go.transform.SetParent(_root.transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.dopplerLevel = 0f;
                _pool.Add(s);
                return s;
            }
            return _pool.OrderByDescending(s => s.time).First();
        }

        // ---------------------------------------------------------------- uno de nuestros clips suena ahora en este animador
        private static Animator _lastAnim;
        private static int _lastFrame = -1;
        private static bool _lastOurs;

        internal static bool PlayingOurs(Animator anim)
        {
            if (anim == null) return false;
            if (anim == _lastAnim && Time.frameCount == _lastFrame) return _lastOurs;
            bool ours = false;
            for (int l = 0; l < anim.layerCount && !ours; l++)
            {
                if (l > 0 && anim.GetLayerWeight(l) < 0.5f) continue;
                foreach (var ci in anim.GetCurrentAnimatorClipInfo(l)) if (ci.weight > 0.3f && Bo7Anims.IsOurClip(ci.clip)) { ours = true; break; }
                if (!ours) foreach (var ci in anim.GetNextAnimatorClipInfo(l)) if (ci.weight > 0.3f && Bo7Anims.IsOurClip(ci.clip)) { ours = true; break; }
            }
            _lastAnim = anim; _lastFrame = Time.frameCount; _lastOurs = ours;
            return ours;
        }

        internal static Animator AnimatorOf(BaseSoundPlayer.IObserverToPlayerBridge bridge)
        {
            var wrapper = bridge?.FirearmsAnimator?.Animator as UnityAnimatorWrapper;
            return wrapper != null ? wrapper.Animator : null;
        }
    }

    // recibe los eventos "Bo7Snd" de nuestros clips (va en el GameObject del animador del arma)
    internal class Bo7SoundRelay : MonoBehaviour
    {
        private string _last;
        private float _lastAt;

        public void Bo7Snd(string alias)
        {
            // el selector suena en dos capas (Hands y Selector) a la vez: un mismo alias seguido solo una vez
            if (alias == _last && Time.time - _lastAt < 0.1f) return;
            _last = alias; _lastAt = Time.time;
            Bo7Sounds.Play(alias, transform.position, true, false);
        }
    }

    [HarmonyPatch(typeof(WeaponSoundPlayer), nameof(WeaponSoundPlayer.FireBullet))]
    internal static class Bo7FirePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(WeaponSoundPlayer __instance, BaseSoundPlayer.IObserverToPlayerBridge ___playersBridge)
        {
            try
            {
                var w = ___playersBridge?.Weapon;
                string tpl = w?.Item?.StringTemplateId;
                if (!Bo7Sounds.IsOurs(tpl)) return true;
                string alias = Bo7Sounds.ShotAlias(tpl, __instance.IsSilenced, w.GetCurrentMagazineCount() == 0);
                if (alias == null) return true;
                Bo7Sounds.Play(alias, __instance.WeaponPosition, ___playersBridge.PointOfView == EPointOfView.FirstPerson, true);
                return false;
            }
            catch (System.Exception e) { Dbg.Throttled("bo7fire", 10f, "ERROR", "disparo BO7: " + e.Message); return true; }
        }
    }

    // sonidos de EFT del arma (recarga, sacar, inspeccion...) callados mientras suena un clip nuestro, que trae los de BO7
    [HarmonyPatch]
    internal static class Bo7MuteEftFoleyPatch
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BaseSoundPlayer), nameof(BaseSoundPlayer.SoundEventHandler));
            yield return AccessTools.Method(typeof(BaseSoundPlayer), nameof(BaseSoundPlayer.SoundAtPointEventHandler));
            yield return AccessTools.Method(typeof(BaseSoundPlayer), nameof(BaseSoundPlayer.EventReceiver));
        }

        [HarmonyPrefix]
        private static bool Prefix(BaseSoundPlayer __instance, BaseSoundPlayer.IObserverToPlayerBridge ___playersBridge)
        {
            try
            {
                if (!(__instance is WeaponSoundPlayer) || !Bo7Sounds.IsOurs(___playersBridge?.Weapon?.Item?.StringTemplateId)) return true;
                return !Bo7Sounds.PlayingOurs(Bo7Sounds.AnimatorOf(___playersBridge));
            }
            catch { return true; }
        }
    }
}
