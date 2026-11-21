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
        internal const string Mk35 = "6a1c00000000000000b70001", Vmp = "6a1c00000000000000b70021", Pk = "6a1c00000000000000b70101";
        internal const string Hk416 = "5bb2475ed4351e00853264e3", Mp5 = "5926bb2186f7744b1c6c6e60", Ak12 = "6499849fc93611967b034949";

        // un arma de BO7: plantilla, arma base de EFT (su modelo y controlador), prefijo de sus clips horneados, ranuras del arma base que
        // se ocultan ademas del armazon, y alias del disparo (normal, ultima bala, silenciado, silenciado ultima bala)
        // AimDepth: profundidad del ojo al apuntar (WeaponPrefab.DefaultAimPlane.Depth: el ojo va a esa distancia delante del origen del
        // arma, 0.075-0.08 en las de EFT). Menos = ojo mas atras. 0 = la del arma base. Medido con Bo7Anim.Run2 BO7_AIM=2.
        // FpHideSlots: ranuras cuyas piezas no se dibujan en la 1a persona del jugador (la culata larga de la Voyak, con el empuje hacia delante
        // y el encuadre de BO7, quedaba a la vista como una losa negra junto al arma); en inventario, modding y 3a persona si se ven.
        internal sealed class Gun { public string Tpl, Base, Prefix, Key; public string[] HideSlots, Shots, FpHideSlots; public float AimDepth; }
        private static string[] ShotsOf(string cod) { return new[] { "wpn_" + cod + "_fire_plr_shot", "wpn_" + cod + "_fire_plr_last_shot", "wpn_" + cod + "_sup_plr_shot", "wpn_" + cod + "_sup_plr_last_shot" }; }
        internal static readonly Gun[] All =
        {
            new Gun { Tpl = Mk35, Base = Hk416, Prefix = "mk35_", Key = "mk35", HideSlots = new[] { "mod_charge" }, Shots = ShotsOf("ar_finch") },
            new Gun { Tpl = Vmp, Base = Mp5, Prefix = "vmp_", Key = "vmp", HideSlots = new[] { "mod_charge" }, Shots = new[] { "wpn_sm_civet_fire_plr", "wpn_sm_civet_fire_plr_last", "wpn_sm_civet_sup_plr_shot", "wpn_sm_civet_sup_plr_last_shot" } },
            new Gun { Tpl = "6a1c00000000000000b70201", Base = "5ac4cd105acfc40016339859", Prefix = "vk_", Key = "vk", HideSlots = new[] { "mod_charge", "mod_stock_release" }, FpHideSlots = new[] { "mod_stock" }, Shots = ShotsOf("ar_heron") },   // sin AimDepth: el arma va 7 cm mas adelante (GunDef.ViewPush) y la mira trasera queda a ~15 cm del ojo
            new Gun { Tpl = "6a1c00000000000000b70301", Base = "6184055050224f204c1da540", Prefix = "mx_", Key = "mx", Shots = ShotsOf("ar_eagle"),   // sin AimDepth: ViewPush 4 cm, mira trasera a ~17 cm del ojo
                      HideSlots = new[] { "mod_charge", "mod_catch", "mod_release", "mod_release_000", "mod_trigger_group", "mod_hammer", "mod_selector", "mod_trigger" } },
            new Gun { Tpl = "6a1c00000000000000b70401", Base = "5e00903ae9dc277128008b87", Prefix = "vs_", Key = "vs", Shots = new[] { "wpn_sm_bear_fire_plr", "wpn_sm_bear_fire_plr_last", "wpn_sm_bear_sup_plr_shot", "wpn_sm_bear_sup_plr_last_shot" },
                      HideSlots = new[] { "mod_charge", "mod_stock_lock" } },
            new Gun { Tpl = "6a1c00000000000000b70501", Base = "5e870397991fd70db46995c8", Prefix = "br_", Key = "br", HideSlots = new string[0],
                      Shots = new[] { "wpn_sh_corso_fire_plr", "wpn_sh_corso_fire_plr_last", "wpn_sh_corso_sup_plr", "wpn_sh_corso_sup_plr_last" } },
            new Gun { Tpl = "6a1c00000000000000b70601", Base = "627e14b21713922ded6f2c15", Prefix = "hw_", Key = "hw", HideSlots = new[] { "mod_charge" },
                      Shots = new[] { "wpn_sn_aden_fire_plr_shot", "wpn_sn_aden_fire_plr_last_shot", "wpn_sn_aden_fire_sup_plr_shot", "wpn_sn_aden_fire_sup_plr_last_shot" } },
            new Gun { Tpl = Pk, Base = Ak12, Prefix = "pk_", Key = "pk", HideSlots = new[] { "mod_charge", "mod_stock_release" }, Shots = ShotsOf("ar_condor") },
        };
        private static readonly Dictionary<string, Gun> ByTpl = All.ToDictionary(g => g.Tpl, g => g);
        private static readonly HashSet<string> Bases = new HashSet<string>(All.Select(g => g.Base));
        internal static Gun Of(string tpl) { Gun g; return tpl != null && ByTpl.TryGetValue(tpl, out g) ? g : null; }
        internal static bool Touches(string tpl) { return tpl != null && (ByTpl.ContainsKey(tpl) || Bases.Contains(tpl)); }

        internal static void Apply(GameObject go, Item item, bool firstPerson = false)
        {
            if (go == null || item == null) return;
            string tpl = item.StringTemplateId;
            var skin = go.GetComponent<Bo7Skin>();
            var gun = Of(tpl);
            if (gun == null) { if (skin != null) skin.Restore(); return; }
            if (skin == null) skin = go.AddComponent<Bo7Skin>();
            skin.Restore();
            skin.Gun = gun;
            var weapon = go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "weapon");
            if (weapon == null) { Dbg.Throttled("bo7weapon", 10f, "BO7", "sin hueso 'weapon' en " + go.name); return; }
            skin.Hide(weapon, firstPerson && gun.FpHideSlots != null ? gun.HideSlots.Concat(gun.FpHideSlots).ToArray() : gun.HideSlots);
            skin.Movers(weapon);
        }
    }

    // estado de un modelo de arma "vestido" de BO7, para poder deshacerlo cuando el pool lo reutiliza
    internal class Bo7Skin : MonoBehaviour
    {
        private readonly List<Renderer> _hidden = new List<Renderer>();
        internal Bo7Guns.Gun Gun;

        // mallas que cuelgan del hueso weapon sin pasar por una ranura "mod_*" (las de los accesorios si se ven), y lo que haya en 'alsoSlot'
        internal void Hide(Transform weapon, string[] alsoSlots)
        {
            foreach (var r in weapon.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                bool mod = false;
                for (var t = r.transform; t != null && t != weapon; t = t.parent) if (t.name.StartsWith("mod_")) { mod = System.Array.IndexOf(alsoSlots, t.name) < 0; break; }
                if (mod) continue;
                r.enabled = false; _hidden.Add(r);
            }
        }

        // Partes moviles (cerrojo, selector...): los nodos "bo7mv:<hueso>" de los accesorios siguen a un nodo guia "bo7_<hueso>" hijo del
        // hueso weapon, que es el que animan los clips horneados (el movimiento del hueso respecto a su reposo, en el espacio del arma)
        private sealed class Mover { public Transform Node, Driver; public Matrix4x4 Rest; }
        private readonly List<Mover> _movers = new List<Mover>();
        private Transform _weapon;

        internal void Movers(Transform weapon)
        {
            _weapon = weapon; _movers.Clear();
            foreach (var t in weapon.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("bo7mv:")) continue;
                string dn = "bo7_" + t.name.Substring(6);
                var d = weapon.Find(dn);
                if (d == null) { d = new GameObject(dn).transform; d.SetParent(weapon, false); }
                var rest = t.GetComponent<Bo7Rest>();
                if (rest == null) { rest = t.gameObject.AddComponent<Bo7Rest>(); rest.Local = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale); }
                _movers.Add(new Mover { Node = t, Driver = d, Rest = rest.Local });
            }
        }

        private void LateUpdate()
        {
            if (_weapon == null) return;
            for (int i = 0; i < _movers.Count; i++)
            {
                var m = _movers[i];
                if (m.Node == null || m.Driver == null || m.Node.parent == null) continue;
                var c = _weapon.worldToLocalMatrix * m.Node.parent.localToWorldMatrix;
                var x = c.inverse * Matrix4x4.TRS(m.Driver.localPosition, m.Driver.localRotation, Vector3.one) * c * m.Rest;
                m.Node.localPosition = x.GetColumn(3);
                Vector3 f = x.GetColumn(2), u = x.GetColumn(1);
                if (f.sqrMagnitude > 1e-12f && u.sqrMagnitude > 1e-12f) m.Node.localRotation = Quaternion.LookRotation(f, u);
            }
        }

        internal void Restore()
        {
            Gun = null;
            _movers.Clear();
            foreach (var r in _hidden) if (r != null) r.enabled = true;
            _hidden.Clear();
        }
    }

    internal class Bo7Rest : MonoBehaviour { internal Matrix4x4 Local; }

    // todos los modelos de objetos (en la mano, en el suelo, iconos) salen de aqui; los accesorios se montan antes de terminar
    [HarmonyPatch]
    internal static class Bo7CreateItemPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.GetDeclaredMethods(typeof(ObjectsFactory)).First(m => m.Name == "CreateItemAsync" && m.GetParameters().Length == 6);

        [HarmonyPostfix]
        private static void Postfix(Item item, IPlayer player, bool isAnimated, ref Task<GameObject> __result)
        {
            var sid = item?.StringTemplateId;
            if (!Bo7Guns.Touches(sid)) return;
            var me = player as Player;
            __result = Wrap(__result, item, isAnimated && me != null && me.IsYourPlayer);       // el arma que lleva el jugador en las manos
        }

        private static async Task<GameObject> Wrap(Task<GameObject> task, Item item, bool firstPerson)
        {
            var go = await task;
            try { Bo7Guns.Apply(go, item, firstPerson); } catch (System.Exception e) { Dbg.Throttled("bo7apply", 10f, "ERROR", "armas BO7: " + e); }
            return go;
        }
    }

    // Al apuntar, EFT pone el ojo a DefaultAimPlane.Depth delante del origen del arma (el del prefab del arma base), sin mirar donde esta
    // la mira trasera: con las piezas de BO7 quedaba pegado a ella. Se cambia por la profundidad propia del arma (Gun.AimDepth).
    [HarmonyPatch(typeof(EFT.Animations.ProceduralWeaponAnimation), nameof(EFT.Animations.ProceduralWeaponAnimation.InitAimPlanes))]
    internal static class Bo7AimPlanePatch
    {
        [HarmonyPostfix]
        private static void Postfix(EFT.Animations.ProceduralWeaponAnimation __instance, WeaponPrefab wp)
        {
            if (wp == null) return;
            var skin = wp.GetComponent<Bo7Skin>() ?? wp.GetComponentInParent<Bo7Skin>();
            var gun = skin != null ? skin.Gun : null;
            if (gun == null || gun.AimDepth <= 0f) return;
            var hc = __instance.HandsContainer;
            var plane = new WeaponPrefab.AimPlane { Name = wp.DefaultAimPlane != null ? wp.DefaultAimPlane.Name : "default", Depth = gun.AimDepth };
            hc.DefaultAimPlane = plane;
            if (hc.CustomAimPlanes != null && hc.CustomAimPlanes.Length > 0) hc.CustomAimPlanes[0] = plane;
        }
    }

    // Iconos y vista previa: EFT encuadra con la caja de TODAS las mallas (tambien las desactivadas); las del arma base que ocultamos
    // agrandaban la caja y el arma salia pequena y descentrada. Para las armas de BO7 solo cuentan las mallas visibles.
    [HarmonyPatch(typeof(EFT.UI.WeaponModding.WeaponPreview), nameof(EFT.UI.WeaponModding.WeaponPreview.GetBounds))]
    internal static class Bo7IconBoundsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameObject itemGameObject, ref Bounds __result)
        {
            if (itemGameObject == null || itemGameObject.GetComponentInChildren<Bo7Skin>(true) == null) return;
            bool any = false; var b = default(Bounds);
            foreach (var r in itemGameObject.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || r.name.Contains("linza") || r.name == "MuzzleJetCombinedMesh" || r.bounds.extents == Vector3.zero) continue;
                if (any) b.Encapsulate(r.bounds); else { b = r.bounds; any = true; }
            }
            if (any) __result = b;
        }
    }
}
