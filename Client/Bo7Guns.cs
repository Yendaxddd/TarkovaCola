using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Armas de Black Ops 7 (Server/Bo7Guns.cs): MK35 ISR sobre la HK416A5 y VMP sobre la MP5 Navy.
    //  Usan el prefab del arma base (animaciones, sonidos) y sus piezas de BO7 son accesorios; al crear su modelo se ocultan las mallas
    //  del arma base que no son accesorios (el armazon) y su asa de carga (el receptor de BO7 ya lleva la suya).
    //  EFT reutiliza los modelos (pool): si el mismo modelo vuelve como HK416/MP5 normal, se deja como estaba.
    // ============================================================================================
    internal static class Bo7Guns
    {
        internal const string Mk35 = "6a1c00000000000000b70001", Vmp = "6a1c00000000000000b70021";
        internal const string Hk416 = "5bb2475ed4351e00853264e3", Mp5 = "5926bb2186f7744b1c6c6e60";

        internal static void Apply(GameObject go, Item item)
        {
            if (go == null || item == null) return;
            string tpl = item.StringTemplateId;
            var skin = go.GetComponent<Bo7Skin>();
            bool ours = tpl == Mk35 || tpl == Vmp;
            if (!ours) { if (skin != null) skin.Restore(); return; }
            if (skin == null) skin = go.AddComponent<Bo7Skin>();
            skin.Restore();
            var weapon = go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "weapon");
            if (weapon == null) { Dbg.Throttled("bo7weapon", 10f, "BO7", "sin hueso 'weapon' en " + go.name); return; }
            skin.Hide(weapon, "mod_charge");
        }
    }

    // estado de un modelo de arma "vestido" de BO7, para poder deshacerlo cuando el pool lo reutiliza
    internal class Bo7Skin : MonoBehaviour
    {
        private readonly List<Renderer> _hidden = new List<Renderer>();

        // mallas que cuelgan del hueso weapon sin pasar por una ranura "mod_*" (las de los accesorios si se ven), y lo que haya en 'alsoSlot'
        internal void Hide(Transform weapon, string alsoSlot)
        {
            foreach (var r in weapon.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                bool mod = false;
                for (var t = r.transform; t != null && t != weapon; t = t.parent) if (t.name.StartsWith("mod_")) { mod = t.name != alsoSlot; break; }
                if (mod) continue;
                r.enabled = false; _hidden.Add(r);
            }
        }

        internal void Restore()
        {
            foreach (var r in _hidden) if (r != null) r.enabled = true;
            _hidden.Clear();
        }
    }

    // todos los modelos de objetos (en la mano, en el suelo, iconos) salen de aqui; los accesorios se montan antes de terminar
    [HarmonyPatch]
    internal static class Bo7CreateItemPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.GetDeclaredMethods(typeof(ObjectsFactory)).First(m => m.Name == "CreateItemAsync" && m.GetParameters().Length == 6);

        [HarmonyPostfix]
        private static void Postfix(Item item, ref Task<GameObject> __result)
        {
            var sid = item?.StringTemplateId;
            if (sid != Bo7Guns.Mk35 && sid != Bo7Guns.Vmp && sid != Bo7Guns.Hk416 && sid != Bo7Guns.Mp5) return;
            __result = Wrap(__result, item);
        }

        private static async Task<GameObject> Wrap(Task<GameObject> task, Item item)
        {
            var go = await task;
            try { Bo7Guns.Apply(go, item); } catch (System.Exception e) { Dbg.Throttled("bo7apply", 10f, "ERROR", "armas BO7: " + e); }
            return go;
        }
    }
}
