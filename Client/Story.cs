using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;

namespace TarkovaCola.Client
{
    // Opciones de depuracion de la historia que dependen del servidor.
    internal static class Story
    {
        // "Perks a la venta sin misiones" (F12): el servidor quita o vuelve a poner el bloqueo de las ventas en Therapist.
        // Se envia al cambiar la opcion y cada vez que se carga el progreso (al entrar en el juego).
        internal static void SyncSales()
        {
            try
            {
                bool unlock = Plugin.CfgSalesUnlocked.Value;
                var body = new JObject { ["unlock"] = unlock }.ToString(Formatting.None);
                RequestHandler.PostJsonAsync("/tarkovacola/debug/sales", body).ContinueWith(t =>
                {
                    if (t.IsFaulted) Plugin.Log.LogWarning("No se pudo cambiar el bloqueo de ventas: " + t.Exception?.GetBaseException().Message);
                    else Plugin.Log.LogInfo("[GUARDIAS] ventas de perks en Therapist: " + (unlock ? "DESBLOQUEADAS (depuracion)" : "segun las misiones"));
                });
            }
            catch (Exception e) { Plugin.Log.LogWarning("Error cambiando el bloqueo de ventas: " + e.Message); }
        }
    }
}
