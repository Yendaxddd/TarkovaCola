using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.InRaid;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace TarkovaCola.Server
{
    // Mule Kick (3.1): una 3.a ranura de arma ("MuleKick") en el inventario del personaje, con los mismos filtros que el arma principal.
    // El cliente la oculta si no tienes la perk activa (salvo que aun tenga un arma dentro, para poder sacarla). Al morir se pierde
    // igual que el arma principal. Compatible con la funda izquierda de DualWieldLab (el cliente asigna los indices por nombre).
    internal static class MuleSlot
    {
        internal const string SlotName = "MuleKick";
        internal static readonly MongoId SlotId = new MongoId("6a1c0000000000000000e0b1");
        internal static readonly MongoId DefaultInventory = new MongoId("55d7217a4bdc2d86028b456d");
        internal static LostOnDeathConfig Death;
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 50)]
    public class TarkovaColaMuleKick : IOnLoad
    {
        private readonly TemplateTable _templates;
        private readonly LostOnDeathConfig _death;

        public TarkovaColaMuleKick(TemplateTable templates, LostOnDeathConfig death) { _templates = templates; _death = death; }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!_templates.Items.TryGetValue(MuleSlot.DefaultInventory, out var inv) || inv.Properties?.Slots == null)
                {
                    Console.WriteLine("[Tarkova-Cola] Mule Kick: no se encontro el inventario por defecto; sin 3.a ranura");
                    return Task.CompletedTask;
                }
                var slots = inv.Properties.Slots.ToList();
                if (!slots.Any(s => s.Name == MuleSlot.SlotName))
                {
                    var primary = slots.FirstOrDefault(s => s.Name == "FirstPrimaryWeapon");
                    if (primary == null) { Console.WriteLine("[Tarkova-Cola] Mule Kick: sin ranura de arma principal que copiar"); return Task.CompletedTask; }
                    var mule = primary with { Name = MuleSlot.SlotName, Id = MuleSlot.SlotId };
                    slots.Add(mule);                                   // al final (el cliente le da el ultimo indice)
                    inv.Properties.Slots = slots;
                }
                MuleSlot.Death = _death;
                new MuleDeathPatch().Enable();
                Console.WriteLine("[Tarkova-Cola] Mule Kick: 3.a ranura de arma anadida (" + slots.Count + " ranuras de equipo)");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] Mule Kick: no se pudo anadir la ranura: " + e.Message); }
            return Task.CompletedTask;
        }
    }

    // al morir, el arma de la ranura de Mule Kick se pierde (o se conserva) igual que el arma principal
    public class MuleDeathPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InRaidHelper).GetMethod("IsItemKeptAfterDeath", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        [PatchPrefix]
        public static bool Prefix(PmcData pmcData, Item itemToCheck, ref bool __result)
        {
            if (itemToCheck == null || itemToCheck.SlotId != MuleSlot.SlotName || MuleSlot.Death == null) return true;
            var eq = pmcData?.Inventory?.Equipment;
            if (!eq.HasValue || itemToCheck.ParentId != eq.Value.ToString()) return true;
            __result = !(MuleSlot.Death.Equipment.FirstPrimaryWeapon);
            return false;
        }
    }
}
