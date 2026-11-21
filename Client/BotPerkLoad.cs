using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;

namespace TarkovaCola.Client
{
    // Equilibrio: el servidor da perks a mas enemigos cuantas mas perks lleves (+4% por perk, como mucho 50%).
    // Se le avisa del numero cuando cambia (al beber, al empezar o acabar la raid). Las oleadas que salen despues lo notan.
    // Al empezar la raid las perks se reactivan una a una (1, 2, 3...): se espera 1 s y se manda solo el ultimo numero.
    internal static class BotPerkLoad
    {
        private static int _sent = -1;
        private static volatile int _want = -1;
        private static volatile bool _scheduled;

        internal static void Sync(int count)
        {
            _want = count;
            if (_scheduled) return;
            _scheduled = true;
            Task.Delay(1000).ContinueWith(_ => Send());
        }

        private static void Send()
        {
            _scheduled = false;
            int count = _want;
            if (count == _sent) return;
            _sent = count;
            try
            {
                var body = new JObject { ["count"] = count }.ToString(Formatting.None);
                RequestHandler.PostJsonAsync("/tarkovacola/perkload", body).ContinueWith(t =>
                {
                    if (t.IsFaulted) { _sent = -1; Plugin.Log.LogWarning("perks en enemigos (equilibrio): " + t.Exception?.GetBaseException().Message); }
                });
                Dbg.Log("BOTS", "llevas " + count + " perk(s): se lo digo al servidor");
            }
            catch (Exception e) { _sent = -1; Plugin.Log.LogWarning("perks en enemigos (equilibrio): " + e.Message); }
        }
    }
}
