using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;

namespace TarkovaCola.Client
{
    // Opcion de F12 "perks como inyectores": el servidor crea las perks como estimulantes (animacion de inyectarse y modelo del stim
    // de BO6) en vez de latas. Los objetos se crean al arrancar el servidor, asi que el cambio se ve al reiniciar servidor y juego.
    internal static class InjectorMode
    {
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
