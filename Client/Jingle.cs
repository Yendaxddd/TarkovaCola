using System;
using Comfort.Common;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TarkovaCola.Client
{
    // Cancioncita al beber la perk: se carga una vez desde assets/ y se reproduce en 2D.
    internal static class Jingle
    {
        private static AudioClip _clip;
        private static bool _loading;

        public static void Play()
        {
            if (!Plugin.CfgJingle.Value) return;
            if (_clip != null) { Emit(); return; }
            if (_loading) return;
            _loading = true;
            Plugin.Instance.StartCoroutine(Load());
        }

        private static IEnumerator Load()
        {
            var path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", "speedcola_jingle.mp3"));
            if (!File.Exists(path)) { Dbg.Log("AUDIO", "falta " + path); _loading = false; yield break; }
            using (var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.MPEG))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) _clip = DownloadHandlerAudioClip.GetContent(req);
                else Dbg.Log("AUDIO", "no se pudo cargar: " + req.error);
            }
            _loading = false;
            if (_clip != null) Emit();
        }

        // Se reproduce por el sistema de audio del propio juego (BetterAudio), en un grupo no espacial: asi respeta su mezclador
        // y no interfiere con el resto del sonido de la raid.
        private static void Emit()
        {
            if (!Singleton<BetterAudio>.Instantiated) { Dbg.Log("AUDIO", "BetterAudio no disponible"); return; }
            Singleton<BetterAudio>.Instance.PlayNonspatial(_clip, BetterAudio.AudioSourceGroupType.NonspatialBypass, 0f, Mathf.Clamp01(Plugin.CfgJingleVolume.Value));
            Dbg.Log("AUDIO", "jingle");
        }
    }
}
