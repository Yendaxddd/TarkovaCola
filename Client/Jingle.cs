using System;
using Comfort.Common;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TarkovaCola.Client
{
    // Cancioncita al beber la perk: se carga una vez desde assets/ y se reproduce en 2D.
    internal static class Jingle
    {
        private static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> _loading = new HashSet<string>();

        public static void Play(PerkDef perk)
        {
            if (!Plugin.CfgJingle.Value || perk == null || string.IsNullOrEmpty(perk.JingleFile)) return;
            AudioClip clip;
            if (_clips.TryGetValue(perk.JingleFile, out clip)) { Emit(clip); return; }
            if (!_loading.Add(perk.JingleFile)) return;
            Plugin.Instance.StartCoroutine(Load(perk.JingleFile));
        }

        private static IEnumerator Load(string file)
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
            if (clip != null) { _clips[file] = clip; Emit(clip); }
        }

        // Se reproduce por el sistema de audio del propio juego (BetterAudio), en un grupo no espacial: asi respeta su mezclador
        // y no interfiere con el resto del sonido de la raid.
        private static void Emit(AudioClip clip)
        {
            if (!Singleton<BetterAudio>.Instantiated) { Dbg.Log("AUDIO", "BetterAudio no disponible"); return; }
            Singleton<BetterAudio>.Instance.PlayNonspatial(clip, BetterAudio.AudioSourceGroupType.NonspatialBypass, 0f, Mathf.Clamp01(Plugin.CfgJingleVolume.Value));
            Dbg.Log("AUDIO", "jingle");
        }
    }
}
