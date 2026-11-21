using System;
using EFT;
using HarmonyLib;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Cuerpo a cuerpo con animaciones de BO7: clips me_<arma>_<clip de EFT> (UnityWeaponsSdk, Bo7Melee.cs) horneados sobre el rig de cada
    //  arma cuerpo a cuerpo de EFT. Justo antes de sacarla (KnifeController.Spawn) se ponen en su controlador; el objeto se reutiliza para
    //  la misma arma, asi que basta con ponerlos una vez (y quitarlos si se apaga la opcion). Los eventos de EFT (golpe, sonido) van en
    //  tiempo normalizado y siguen saltando.
    // ============================================================================================
    [HarmonyPatch(typeof(Player.KnifeController), nameof(Player.KnifeController.Spawn))]
    internal static class Bo7MeleePatch
    {
        private const string Name = "TarkovaCola BO7 melee";

        [HarmonyPrefix]
        private static void Prefix(Player.KnifeController __instance)
        {
            try
            {
                var animator = __instance.FirearmsAnimator?.Animator;
                if (animator == null) return;
                string prefix = Plugin.CfgBo7Melee.Value ? Bo7Anims.MeleePrefix(__instance.Item?.StringTemplateId) : null;
                var ctrl = animator.runtimeAnimatorController;
                bool ours = ctrl != null && ctrl.name == Name;
                if ((prefix != null) == ours) return;
                if (prefix != null) Bo7Anims.Apply(animator, prefix, Name); else Bo7Anims.Restore(animator);
                // cambiar el controlador reinicia los parametros del animador
                var player = Traverse.Create(__instance).Field("_player").GetValue<Player>();
                if (player != null) __instance.SetMeleeSpeed(player.Physical.MeleeSpeed);
                Dbg.Log("BO7", "cuerpo a cuerpo: " + (prefix != null ? "clips de BO7 (" + prefix + ")" : "clips de EFT"));
            }
            catch (Exception e) { Dbg.Throttled("bo7melee", 10f, "ERROR", "cuerpo a cuerpo de BO7: " + e); }
        }
    }
}
