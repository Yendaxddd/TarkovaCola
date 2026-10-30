using System;
using Comfort.Common;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TarkovaCola.Client
{
    // Sonidos del mod: la cancioncita al beber la perk y las voces del locutor (perk aleatoria, Eternal Perk).
    // Cada archivo se carga una vez desde assets/ y se reproduce en 2D.
    internal static class Jingle
    {
        internal const string RandomVoice = "randomperk_voice.wav";
        internal const string EternalVoice = "eternalperk_voice.wav";

        private static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> _loading = new HashSet<string>();

        public static void Play(PerkDef perk)
        {
            if (!Plugin.CfgJingle.Value || perk == null || string.IsNullOrEmpty(perk.JingleFile)) return;
            PlayFile(perk.JingleFile, Plugin.CfgJingleVolume.Value, 0f);
        }

        // Voz del locutor (volumen propio). 'delay' = segundos de espera antes de sonar (para no pisar otra voz).
        public static void Voice(string file, float delay = 0f)
        {
            if (!Plugin.CfgVoice.Value) return;
            PlayFile(file, Plugin.CfgVoiceVolume.Value, delay);
        }

        // Efecto de sonido con duracion maxima (volumen de las canciones, aunque las canciones esten apagadas).
        // Cada uno suena en su propia fuente para poder cortarlo sin cortar lo demas: a los 'maxSeconds' se desvanece en medio segundo.
        public static void Sfx(string file, float maxSeconds)
        {
            Plugin.Instance.StartCoroutine(SfxRoutine(file, Plugin.CfgJingleVolume.Value, maxSeconds, null, Vector3.zero));
        }

        // Igual pero en 3D, sonando desde un punto del mapa (y siguiendo a 'follow' si se da). Si falta el archivo, no suena nada.
        public static void At(string file, Vector3 pos, float maxSeconds, Transform follow = null)
        {
            Plugin.Instance.StartCoroutine(SfxRoutine(file, Plugin.CfgJingleVolume.Value, maxSeconds, follow, pos));
        }

        private static IEnumerator SfxRoutine(string file, float volume, float maxSeconds, Transform follow, Vector3 pos)
        {
            bool spatial = follow != null || pos != Vector3.zero;
            if (!Valid(file))
            {
                if (_loading.Add(file)) yield return Plugin.Instance.StartCoroutine(Load(file, 0f, 0f, false));
                else { float wait = 0f; while (_loading.Contains(file) && wait < 5f) { wait += Time.unscaledDeltaTime; yield return null; } }
            }
            AudioClip clip;
            if (!_clips.TryGetValue(file, out clip) || clip == null) yield break;
            var go = new GameObject("TarkovaCola_Sfx");
            if (!spatial) UnityEngine.Object.DontDestroyOnLoad(go);
            else if (follow != null) { go.transform.SetParent(follow, false); }
            else go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = spatial ? 1f : 0f; src.playOnAwake = false; src.loop = false; src.priority = 40;
            if (spatial) { src.rolloffMode = AudioRolloffMode.Linear; src.minDistance = 3f; src.maxDistance = 45f; src.dopplerLevel = 0f; }
            try { if (Singleton<BetterAudio>.Instantiated) src.outputAudioMixerGroup = Singleton<BetterAudio>.Instance.MasterMixerGroup; } catch { }
            src.clip = clip; src.volume = Mathf.Clamp01(volume);
            src.Play();
            Dbg.Log("AUDIO", file + " (" + clip.length.ToString("0.0") + " s, se corta a los " + maxSeconds + " s)");
            float t = 0f, fadeAt = Mathf.Max(0f, maxSeconds - 0.5f);
            while (src != null && src.isPlaying && t < maxSeconds)
            {
                t += Time.unscaledDeltaTime;
                if (t > fadeAt) src.volume = Mathf.Clamp01(volume) * Mathf.Clamp01((maxSeconds - t) / 0.5f);
                yield return null;
            }
            if (go != null) UnityEngine.Object.Destroy(go);
        }

        // Carga el archivo con antelacion (asi suena sin retraso cuando toca y se conoce lo que dura)
        public static void Preload(string file)
        {
            if (Valid(file) || !_loading.Add(file)) return;
            Plugin.Instance.StartCoroutine(Load(file, 0f, 0f, false));
        }

        // Duracion del archivo si ya esta cargado (0 si aun no)
        public static float Length(string file)
        {
            AudioClip clip;
            return _clips.TryGetValue(file, out clip) && clip != null ? clip.length : 0f;
        }

        // El clip guardado sigue vivo (el juego descarga assets sin usar al cargar la raid: un clip descargado sale con 0 s)
        private static bool Valid(string file)
        {
            AudioClip clip;
            if (!_clips.TryGetValue(file, out clip)) return false;
            if (clip != null && clip.length > 0f) return true;
            _clips.Remove(file);
            Dbg.Log("AUDIO", "el clip " + file + " se habia descargado: se vuelve a cargar");
            return false;
        }

        private static void PlayFile(string file, float volume, float delay)
        {
            AudioClip clip;
            if (Valid(file) && _clips.TryGetValue(file, out clip))
            {
                if (delay <= 0f) Emit(clip, volume);
                else Plugin.Instance.StartCoroutine(Later(clip, volume, delay));
                return;
            }
            if (!_loading.Add(file)) return;
            Plugin.Instance.StartCoroutine(Load(file, volume, delay, true));
        }

        private static IEnumerator Later(AudioClip clip, float volume, float delay)
        {
            yield return new WaitForSeconds(delay);
            Emit(clip, volume);
        }

        private static IEnumerator Load(string file, float volume, float delay, bool thenPlay)
        {
            var path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", file));
            AudioClip clip = null;
            if (!File.Exists(path)) Dbg.Log("AUDIO", "falta " + path);
            else
            {
                using (var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? AudioType.WAV : AudioType.MPEG))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        clip = DownloadHandlerAudioClip.GetContent(req);
                        if (clip != null && clip.length <= 0f) { Dbg.Log("AUDIO", "el archivo no se pudo decodificar: " + file); clip = null; }      // (un mp3 que Unity no decodifica sale con 0 s)
                        else if (clip != null) { clip.name = file; clip.hideFlags = HideFlags.DontUnloadUnusedAsset; }
                    }
                    else Dbg.Log("AUDIO", "no se pudo cargar: " + req.error);
                }
            }
            _loading.Remove(file);
            if (clip == null) yield break;
            _clips[file] = clip;
            if (!thenPlay) yield break;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Emit(clip, volume);
        }

        // Se reproduce en una fuente de audio propia (2D) conectada al mezclador del juego: asi respeta el volumen general y, como se usa
        // PlayOneShot, varios sonidos pueden sonar a la vez (beber dos perks seguidas ya no corta la cancion anterior en seco).
        private static AudioSource _src;

        // Baja el volumen de lo que suena mientras habla otro locutor (Arena Mode)
        internal static void Duck(bool on)
        {
            if (_src != null) _src.volume = on ? 0.35f : 1f;
        }

        private static AudioSource Source()
        {
            if (_src != null) return _src;
            var go = new GameObject("TarkovaCola_Audio");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _src = go.AddComponent<AudioSource>();
            _src.spatialBlend = 0f; _src.playOnAwake = false; _src.loop = false; _src.priority = 32;
            try { if (Singleton<BetterAudio>.Instantiated) _src.outputAudioMixerGroup = Singleton<BetterAudio>.Instance.MasterMixerGroup; }
            catch (Exception e) { Dbg.Log("AUDIO", "sin mezclador del juego: " + e.Message); }
            return _src;
        }

        private static void Emit(AudioClip clip, float volume)
        {
            var src = Source();
            if (src.outputAudioMixerGroup == null && Singleton<BetterAudio>.Instantiated) src.outputAudioMixerGroup = Singleton<BetterAudio>.Instance.MasterMixerGroup;
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
            Dbg.Log("AUDIO", clip.name + " (" + clip.length.ToString("0.0") + " s)");
        }
    }
}
