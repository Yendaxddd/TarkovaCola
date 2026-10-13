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
        internal const string RandomVoice = "randomperk_voice.mp3";
        internal const string EternalVoice = "eternalperk_voice.mp3";

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

        // Carga el archivo con antelacion (asi suena sin retraso cuando toca y se conoce lo que dura)
        public static void Preload(string file)
        {
            if (_clips.ContainsKey(file) || !_loading.Add(file)) return;
            Plugin.Instance.StartCoroutine(Load(file, 0f, 0f, false));
        }

        // Duracion del archivo si ya esta cargado (0 si aun no)
        public static float Length(string file)
        {
            AudioClip clip;
            return _clips.TryGetValue(file, out clip) && clip != null ? clip.length : 0f;
        }

        private static void PlayFile(string file, float volume, float delay)
        {
            AudioClip clip;
            if (_clips.TryGetValue(file, out clip) && clip != null)
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
                using (var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.MPEG))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) clip = DownloadHandlerAudioClip.GetContent(req);
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

        // Se reproduce por el sistema de audio del propio juego (BetterAudio), en un grupo no espacial: asi respeta su mezclador
        // y no interfiere con el resto del sonido de la raid.
        private static void Emit(AudioClip clip, float volume)
        {
            if (!Singleton<BetterAudio>.Instantiated) { Dbg.Log("AUDIO", "BetterAudio no disponible"); return; }
            Singleton<BetterAudio>.Instance.PlayNonspatial(clip, BetterAudio.AudioSourceGroupType.NonspatialBypass, 0f, Mathf.Clamp01(volume));
            Dbg.Log("AUDIO", clip.name + " (" + clip.length.ToString("0.0") + " s)");
        }
    }
}
