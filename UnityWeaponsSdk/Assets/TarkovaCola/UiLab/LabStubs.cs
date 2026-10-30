// Stubs de lo que UiKit/UiFx usan de EFT y del mod, solo para el banco de pruebas de interfaz (ensamblado TarkovaCola.UiLab)
namespace Comfort.Common
{
    public static class Singleton<T> { public static bool Instantiated => false; public static T Instance => default(T); }
}

namespace EFT.UI
{
    public enum EUISoundType { ButtonClick, ButtonOver, MenuInspectorWindowOpen, MenuEscape }
    public class GUISounds { public void PlayUISound(EUISoundType t) { } }
}

namespace TarkovaCola.Client
{
    internal static class Dbg { internal static void Log(string cat, string msg) { } }
    internal static class Plugin
    {
        internal static readonly LogStub Log = new LogStub();
        internal class LogStub { public void LogWarning(string s) { UnityEngine.Debug.LogWarning(s); } }
    }

    // reloj simulado: el banco de pruebas avanza el tiempo a mano (en batch no hay bucle de juego)
    public static class LabTime
    {
        public static float Now, Dt = 1f / 60f;
    }
}
