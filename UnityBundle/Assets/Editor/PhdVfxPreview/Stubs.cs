// Sustitutos minimos de lo que PhdVfx.cs pide al plugin (solo para la previsualizacion en el editor de Unity).
using UnityEngine;

namespace TarkovaCola.Client
{
    internal class StubConfig { public bool Value = true; }
    internal class StubLog
    {
        public void LogInfo(string m) { Debug.Log(m); }
        public void LogWarning(string m) { Debug.LogWarning(m); }
    }
    internal static class Plugin
    {
        public static StubConfig CfgPhdVfx = new StubConfig();
        public static StubLog Log = new StubLog();
    }
    internal static class Dbg
    {
        public static void Log(string tag, string msg) { Debug.Log("[" + tag + "] " + msg); }
    }
}
