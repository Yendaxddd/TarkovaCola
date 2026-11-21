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
    // Compatibilidad con Dynamic Maps (mpstark, com.mpstark.DynamicMaps): en su mapa (y minimapa) salen
    //  - las maquinas de perks de la raid (con el icono de la perk; Wunderfizz, caja misteriosa y TEDD con el de Research)
    //  - los enemigos marcados por Death Perception, mientras sigan marcados (siguen al enemigo en directo)
    // Todo por reflexion: sin Dynamic Maps no hace nada y no hace falta su DLL para compilar ni para cargar.
    internal static class DynMapsCompat
    {
        private const string Category = "Tarkova-Cola";
        private static bool _looked, _ok;
        private static object _plugin;                     // DynamicMaps.Plugin.Instance
        private static FieldInfo _instance, _map;          // Plugin.Instance, Plugin.Map (ModdedMapScreen)
        private static PropertyInfo _mapViewProp;          // ModdedMapScreen.MapView (internal)
        private static Type _defType;
        private static MethodInfo _addMarker, _addTransform, _removeMarker, _toMapPos;

        private static object _view;                       // MapView donde estan puestos nuestros marcadores
        private static readonly List<UnityEngine.Object> _machines = new List<UnityEngine.Object>();
        private static int _machineCount = -1;
        private static readonly Dictionary<string, UnityEngine.Object> _dp = new Dictionary<string, UnityEngine.Object>();
        private static float _next;

        internal static bool Present { get { return Lookup(); } }

        private static bool Lookup()
        {
            if (_looked) return _ok;
            _looked = true;
            try
            {
                var plugin = AccessTools.TypeByName("DynamicMaps.Plugin");
                var screen = AccessTools.TypeByName("DynamicMaps.UI.ModdedMapScreen");
                var view = AccessTools.TypeByName("DynamicMaps.UI.Components.MapView");
                _defType = AccessTools.TypeByName("DynamicMaps.Data.MapMarkerDef");
                var math = AccessTools.TypeByName("DynamicMaps.Utils.MathUtils");
                if (plugin == null || screen == null || view == null || _defType == null || math == null) return false;
                _instance = AccessTools.Field(plugin, "Instance");
                _map = AccessTools.Field(plugin, "Map");
                _mapViewProp = AccessTools.Property(screen, "MapView");
                _addMarker = AccessTools.Method(view, "AddMapMarker", new[] { _defType });
                _addTransform = view.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "AddTransformMarker" && m.GetParameters().Length == 5 && m.GetParameters()[4].ParameterType == typeof(Sprite));
                _removeMarker = view.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "RemoveMapMarker");
                _toMapPos = AccessTools.Method(math, "ConvertToMapPosition", new[] { typeof(Vector3) });
                _ok = _instance != null && _map != null && _mapViewProp != null && _addMarker != null && _addTransform != null && _removeMarker != null && _toMapPos != null;
                Plugin.Log.LogInfo(_ok ? "Dynamic Maps detectado: maquinas y enemigos marcados en su mapa" : "Dynamic Maps: version no compatible (no se anaden marcadores)");
            }
            catch (Exception e) { _ok = false; Plugin.Log.LogWarning("Dynamic Maps: " + e.Message); }
            return _ok;
        }

        private static object View()
        {
            _plugin = _instance.GetValue(null);
            var map = _plugin != null ? _map.GetValue(_plugin) : null;
            return map != null ? _mapViewProp.GetValue(map, null) : null;
        }

        internal static void OnRaidStart() { _machines.Clear(); _machineCount = -1; _dp.Clear(); _view = null; _next = 0f; }

        // cada frame en raid (Plugin)
        internal static void Tick(Player me)
        {
            if (me == null || Time.time < _next || !Lookup()) return;
            _next = Time.time + 0.5f;
            try
            {
                var view = View();
                if (view == null || (view is UnityEngine.Object uo && uo == null)) return;
                var def = view.GetType().GetProperty("CurrentMapDef");                      // aun sin mapa cargado: se espera
                if (def != null && def.GetValue(view, null) == null) return;
                if (!ReferenceEquals(view, _view)) { _view = view; _machines.Clear(); _machineCount = -1; _dp.Clear(); }
                Machines_(view);
                Marks(view);
            }
            catch (Exception e) { Dbg.Throttled("dynmaps", 30f, "ERROR", "Dynamic Maps: " + e.GetBaseException().Message); }
        }

        // ---------------------------------------------------------------- maquinas
        private static void Machines_(object view)
        {
            var placed = Machines.Placed().ToList();
            // el mapa borra sus marcadores al cambiar de mapa/nivel: si falta alguno, se vuelven a poner todos
            bool lost = _machines.Any(m => m == null);
            if (placed.Count == _machineCount && !lost) return;
            foreach (var m in _machines) Remove(view, m);
            _machines.Clear();
            foreach (var kv in placed)
            {
                var def = Activator.CreateInstance(_defType);
                Set(def, "Text", Machines.Name(kv.Key));
                Set(def, "Category", Category);
                Set(def, "Sprite", Sprite(kv.Key));
                Set(def, "Color", Color.white);
                Set(def, "Position", (Vector3)_toMapPos.Invoke(null, new object[] { kv.Value.transform.position }));
                var mk = _addMarker.Invoke(view, new[] { def }) as UnityEngine.Object;
                if (mk != null) _machines.Add(mk);
            }
            _machineCount = placed.Count;
            Dbg.Log("MAPA", "Dynamic Maps: " + _machines.Count + " maquina(s) en el mapa");
        }

        // ---------------------------------------------------------------- Death Perception: enemigos marcados
        private static void Marks(object view)
        {
            var world = Singleton<GameWorld>.Instance;
            var keep = new HashSet<string>();
            if (world != null && DeathPerception.Active)
                foreach (var p in world.AllAlivePlayersList)
                {
                    if (p == null || p.IsYourPlayer || !DeathPerception.IsMarked(p)) continue;
                    keep.Add(p.ProfileId);
                    UnityEngine.Object mk;
                    if (_dp.TryGetValue(p.ProfileId, out mk) && mk != null) continue;
                    mk = _addTransform.Invoke(view, new object[] { p.Transform.Original, p.Profile.Nickname, Category, new Color(1f, 0.3f, 0.2f), Sprite(DeathPerception.Id) }) as UnityEngine.Object;
                    if (mk != null) _dp[p.ProfileId] = mk;
                }
            foreach (var id in _dp.Keys.Where(k => !keep.Contains(k)).ToList())
            {
                Remove(view, _dp[id]);
                _dp.Remove(id);
            }
        }

        private static Sprite Sprite(string id)
        {
            if (Perks.Get(id) != null) return Hud.IconSprite(id);
            return Hud.ResearchSprite;
        }

        private static void Set(object o, string prop, object v) { var p = o.GetType().GetProperty(prop); if (p != null && p.CanWrite) p.SetValue(o, v, null); }

        private static void Remove(object view, UnityEngine.Object marker)
        {
            if (marker == null) return;
            try { _removeMarker.Invoke(view, new object[] { marker }); } catch { }
        }
    }
}
