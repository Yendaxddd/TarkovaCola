using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Marcadores opcionales en Dynamic Maps (com.mpstark.dynamicmaps) y Maps Expanded (com.dark.mapsexpanded).
    // En el mapa y el minimapa salen:
    //  - las maquinas de perks de la raid (con el icono de la perk; Wunderfizz, caja misteriosa y TEDD con el de Research)
    //  - los enemigos marcados por Death Perception, mientras sigan marcados (siguen al enemigo en directo)
    // Los dos mods se excluyen entre si. Cada uno se engancha por reflexion solo si esta cargado:
    // sin ellos no se hace nada, y no hace falta ninguna de sus DLL para compilar ni para cargar.
    internal static class DynMapsCompat
    {
        private const string Category = "Tarkova-Cola";
        private static bool _looked;
        private static readonly List<MapBackend> _maps = new List<MapBackend>();

        internal static bool Present { get { return Lookup(); } }

        private sealed class MapBackend
        {
            internal string Name;
            internal FieldInfo Instance, Map;
            internal PropertyInfo MapViewProp;
            internal Type DefType;
            internal MethodInfo AddMarker, AddTransform, RemoveMarker, ToMapPos;
            internal object View;
            internal readonly List<UnityEngine.Object> Machines = new List<UnityEngine.Object>();
            internal int MachineCount = -1;
            internal readonly Dictionary<string, UnityEngine.Object> Dp = new Dictionary<string, UnityEngine.Object>();
        }

        private static bool Lookup()
        {
            if (_looked) return _maps.Count > 0;
            _looked = true;
            Try("Dynamic Maps",
                "DynamicMaps.Plugin", "Map",
                "DynamicMaps.UI.ModdedMapScreen", "MapView",
                "DynamicMaps.UI.Components.MapView",
                "DynamicMaps.Data.MapMarkerDef",
                "DynamicMaps.Utils.MathUtils");
            Try("Maps Expanded",
                "MapsExtended.Plugin", "Map",
                "MapsExtended.Presentation.RaidMapHost", "MapCanvas",
                "MapsExtended.Presentation.Overlays.MapCanvas",
                "MapsExtended.Core.MapMarkerDef",
                "MapsExtended.Core.MathUtils");
            return _maps.Count > 0;
        }

        private static void Try(string name, string pluginType, string mapField, string screenType, string viewProp, string viewType, string defType, string mathType)
        {
            try
            {
                var plugin = AccessTools.TypeByName(pluginType);
                var screen = AccessTools.TypeByName(screenType);
                var view = AccessTools.TypeByName(viewType);
                var def = AccessTools.TypeByName(defType);
                var math = AccessTools.TypeByName(mathType);
                if (plugin == null || screen == null || view == null || def == null || math == null) return;
                var backend = new MapBackend
                {
                    Name = name,
                    Instance = AccessTools.Field(plugin, "Instance"),
                    Map = AccessTools.Field(plugin, mapField),
                    MapViewProp = AccessTools.Property(screen, viewProp),
                    DefType = def,
                    AddMarker = AccessTools.Method(view, "AddMapMarker", new[] { def }),
                    RemoveMarker = view.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "RemoveMapMarker"),
                    ToMapPos = AccessTools.Method(math, "ConvertToMapPosition", new[] { typeof(Vector3) }),
                };
                backend.AddTransform = view.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "AddTransformMarker" && m.GetParameters().Length == 5 && m.GetParameters()[4].ParameterType == typeof(Sprite));
                if (backend.Instance == null || backend.Map == null || backend.MapViewProp == null
                    || backend.AddMarker == null || backend.AddTransform == null || backend.RemoveMarker == null || backend.ToMapPos == null)
                {
                    Plugin.Log.LogInfo(name + ": version no compatible (no se anaden marcadores)");
                    return;
                }
                _maps.Add(backend);
                Plugin.Log.LogInfo(name + " detectado: maquinas y enemigos marcados en su mapa");
            }
            catch (Exception e) { Plugin.Log.LogWarning(name + ": " + e.Message); }
        }

        private static object View(MapBackend map)
        {
            var plugin = map.Instance.GetValue(null);
            var screen = plugin != null ? map.Map.GetValue(plugin) : null;
            return screen != null ? map.MapViewProp.GetValue(screen, null) : null;
        }

        internal static void OnRaidStart()
        {
            foreach (var map in _maps)
            {
                map.Machines.Clear();
                map.MachineCount = -1;
                map.Dp.Clear();
                map.View = null;
            }
        }

        private static float _next;

        // cada frame en raid (Plugin)
        internal static void Tick(Player me)
        {
            if (me == null || Time.time < _next || !Lookup()) return;
            _next = Time.time + 0.5f;
            foreach (var map in _maps)
            {
                try
                {
                    var view = View(map);
                    if (view == null || (view is UnityEngine.Object uo && uo == null)) continue;
                    var def = view.GetType().GetProperty("CurrentMapDef");
                    if (def != null && def.GetValue(view, null) == null) continue;
                    if (!ReferenceEquals(view, map.View)) { map.View = view; map.Machines.Clear(); map.MachineCount = -1; map.Dp.Clear(); }
                    Machines_(map, view);
                    Marks(map, view);
                }
                catch (Exception e) { Dbg.Throttled("dynmaps-" + map.Name, 30f, "ERROR", map.Name + ": " + e.GetBaseException().Message); }
            }
        }

        private static void Machines_(MapBackend map, object view)
        {
            var placed = Machines.Placed().ToList();
            bool lost = map.Machines.Any(m => m == null);
            if (placed.Count == map.MachineCount && !lost) return;
            foreach (var m in map.Machines) Remove(map, view, m);
            map.Machines.Clear();
            foreach (var kv in placed)
            {
                var def = Activator.CreateInstance(map.DefType);
                Set(def, "Text", Machines.Name(kv.Key));
                Set(def, "Category", Category);
                Set(def, "ImagePath", "");
                Set(def, "Sprite", Sprite(kv.Key));
                Set(def, "Color", Color.white);
                Set(def, "Position", (Vector3)map.ToMapPos.Invoke(null, new object[] { kv.Value.transform.position }));
                var mk = map.AddMarker.Invoke(view, new[] { def }) as UnityEngine.Object;
                if (mk != null) map.Machines.Add(mk);
            }
            map.MachineCount = placed.Count;
            Dbg.Log("MAPA", map.Name + ": " + map.Machines.Count + " maquina(s) en el mapa");
        }

        private static void Marks(MapBackend map, object view)
        {
            var world = Singleton<GameWorld>.Instance;
            var keep = new HashSet<string>();
            if (world != null && DeathPerception.Active)
                foreach (var p in world.AllAlivePlayersList)
                {
                    if (p == null || p.IsYourPlayer || !DeathPerception.IsMarked(p)) continue;
                    keep.Add(p.ProfileId);
                    UnityEngine.Object mk;
                    if (map.Dp.TryGetValue(p.ProfileId, out mk) && mk != null) continue;
                    mk = map.AddTransform.Invoke(view, new object[] { p.Transform.Original, p.Profile.Nickname, Category, new Color(1f, 0.3f, 0.2f), Sprite(DeathPerception.Id) }) as UnityEngine.Object;
                    if (mk != null) map.Dp[p.ProfileId] = mk;
                }
            foreach (var id in map.Dp.Keys.Where(k => !keep.Contains(k)).ToList())
            {
                Remove(map, view, map.Dp[id]);
                map.Dp.Remove(id);
            }
        }

        private static Sprite Sprite(string id)
        {
            if (Perks.Get(id) != null) return Hud.IconSprite(id);
            return Hud.ResearchSprite;
        }

        private static void Set(object o, string prop, object v) { var p = o.GetType().GetProperty(prop); if (p != null && p.CanWrite) p.SetValue(o, v, null); }

        private static void Remove(MapBackend map, object view, UnityEngine.Object marker)
        {
            if (marker == null) return;
            try { map.RemoveMarker.Invoke(view, new object[] { marker }); } catch { }
        }
    }
}
