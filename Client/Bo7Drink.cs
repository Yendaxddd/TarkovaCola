using System;
using EFT;
using HarmonyLib;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Beber perks con la animacion de BO7: clips drink_item_drink_cans_* (UnityWeaponsSdk, Bo7Drink.cs) horneados sobre el rig de la
    //  TarCola, que es el objeto en mano de las latas de perk. El controlador de la lata se reutiliza (pool) para la TarCola normal: justo
    //  antes de que empiece a beber (MedsController.Spawn) se ponen nuestros clips si es una perk y se quitan si no. Los eventos de EFT van
    //  en tiempo normalizado, asi que siguen saltando aunque los clips duren distinto.
    // ============================================================================================
    [HarmonyPatch(typeof(Player.MedsController), nameof(Player.MedsController.Spawn))]
    internal static class Bo7DrinkPatch
    {
        private const string Name = "TarkovaCola BO7 drink";

        [HarmonyPrefix]
        private static void Prefix(Player.MedsController __instance)
        {
            try
            {
                var animator = __instance.FirearmsAnimator?.Animator;
                if (animator == null) return;
                var item = __instance.Item;
                bool want = Plugin.CfgCodDrink.Value && item != null && Perks.ByItem(item.StringTemplateId) != null && !HandModel.IsInjector(item);
                var ctrl = animator.runtimeAnimatorController;
                bool ours = ctrl != null && ctrl.name == Name;
                if (want == ours) return;
                if (want) Bo7Anims.Apply(animator, "drink_", Name); else Bo7Anims.Restore(animator);
                // cambiar el controlador reinicia los parametros del animador: los que pone MedsController.Setup se vuelven a poner
                var fa = __instance.FirearmsAnimator;
                var player = Traverse.Create(__instance).Field("_player").GetValue<Player>();
                fa.SkipTime(0.0001f);
                if (player != null) fa.SetUseTimeMultiplier(1f + (float)player.Skills.SurgerySpeed);
            }
            catch (Exception e) { Dbg.Throttled("bo7drink", 10f, "ERROR", "animacion de beber de BO7: " + e); }
        }
    }
}
