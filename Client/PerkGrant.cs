using EFT;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Dar una perk al jugador, venga de una botella que bebe o del powerup de perk aleatoria. Un solo camino para los dos:
    //   Classic: la perk entra en la lista persistente (principal con mejora al azar, o modificador); si no cabe se devuelve la botella.
    //   Modern:  una perk nueva se activa; si ya la llevas activa se vuelve ETERNA (Eternal.cs).
    internal static class PerkGrant
    {
        internal static void Apply(PerkDef def, Player me, bool powerup)
        {
            // Modo Classic: la perk entra en la lista persistente (principal con mejora al azar, o modificador); si no cabe se devuelve la botella
            ClassicMode.Entry entry = null;
            if (ClassicMode.Enabled)
            {
                entry = ClassicMode.OnDrink(def);
                if (entry == null) return;
            }
            else if (Perks.IsActive(def.Id))
            {
                if (!Eternal.Is(def.Id)) { Eternal.Grant(def, me, powerup); return; }
                if (!powerup)                                              // (no deberia pasar: se bloquea antes de beber)
                {
                    Dbg.Log("ETERNAL", "botella de " + def.Id + " devuelta por correo (ya es eterna)");
                    PerkService.RequestRefund(def.ItemTpl, 1);
                    Hud.Notify(L.T("eternal.title"), L.F("classic.refund", def.Name), def.Id);
                }
                return;
            }

            Perks.Activate(def.Id, entry != null && entry.Modifier);
            if (!powerup) Jingle.Play(def);                               // el powerup ya trae su propia voz
            Perk.Refresh();
            PerkFx.RefreshPhysical(me);                                   // capacidad de estamina y de aire, y peso, con las perks activas
            Perk.LastKillOrDrink = Time.time;
            string title = def.Name.ToUpperInvariant(), sub = def.DrinkNotice != null ? def.DrinkNotice() : "";
            if (powerup) Hud.Enqueue(title, sub, def.Id); else Hud.Notify(title, sub, def.Id);
            Dbg.Log("BEBIDA", def.Id + " activada" + (powerup ? " (powerup)" : "") + (entry != null ? (entry.Modifier ? " (modificador)" : " (Classic)") : ""));
            if (entry != null) ClassicMode.AfterDrink(def, entry);       // avisa de la mejora al azar
            if (def.OnDrink != null) def.OnDrink(me.ActiveHealthController);
        }
    }
}
