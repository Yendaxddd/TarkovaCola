using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;

namespace TarkovaCola.Client
{
    // Opcion de F12 "perks como inyectores": el servidor crea las perks como estimulantes (animacion de inyectarse y modelo del stim
    // de BO6) en vez de latas. Los objetos se crean al arrancar el servidor, asi que el cambio se ve al reiniciar servidor y juego.
    internal static class InjectorMode
    {
        // EFT guarda en disco los iconos de los objetos por id; las perks conservan el id al pasar de lata (1x2) a inyector (1x1), asi que
        // seguia saliendo el icono viejo de lata con su tamano y la cuadricula se volvia loca (huecos, objetos que se intercambiaban).
        // Al arrancar el juego, si el servidor tiene las perks en otro modo que la ultima vez, se borra la cache de iconos (se rehace sola).
        internal static void ClearIconsIfModeChanged()
        {
            try
            {
                var body = new JObject { ["on"] = Plugin.CfgInjectors.Value }.ToString(Formatting.None);
                string mode = (string)JObject.Parse(RequestHandler.PostJson("/tarkovacola/injectors", body))["json"];
                if (mode != "inj" && mode != "can") return;
                string file = Path.Combine(BepInEx.Paths.ConfigPath, "TarkovaCola.iconmode");
                string last = File.Exists(file) ? File.ReadAllText(file).Trim() : null;
                if (last == mode) return;
                if (last != null || mode == "inj")
                {
                    ItemIconCache.ClearIconCache();
                    Plugin.Log.LogInfo("Inyectores: las perks cambiaron de modo (" + (last ?? "?") + " -> " + mode + "): cache de iconos borrada");
                }
                File.WriteAllText(file, mode);
            }
            catch (Exception e) { Plugin.Log.LogWarning("Inyectores (cache de iconos): " + e.Message); }
        }

        // userChanged: el jugador acaba de tocar la opcion (avisar siempre que haga falta reiniciar)
        internal static void Sync(bool userChanged)
        {
            try
            {
                bool on = Plugin.CfgInjectors.Value;
                var body = new JObject { ["on"] = on }.ToString(Formatting.None);
                // llamada corta y en el hilo principal (el aviso de la HUD no se puede poner desde otro hilo)
                bool applied = (string)JObject.Parse(RequestHandler.PostJson("/tarkovacola/injectors", body))["json"] == "inj";
                Dbg.Log("INYECTOR", "opcion=" + on + " aplicado en el servidor=" + applied);
                if (applied != on) Hud.Enqueue(L.T("inj.title"), L.F("inj.restart", L.T(on ? "inj.on" : "inj.off")));
                else if (userChanged) Plugin.Log.LogInfo("Inyectores: el servidor ya tiene las perks como " + (on ? "inyectores" : "latas"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("Inyectores (opcion): " + e.Message); }
        }
    }
}
