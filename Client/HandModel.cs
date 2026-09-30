using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Modelo de la Speed Cola en la MANO. El juego usa un prefab distinto para el objeto en mano (el de la TarCola) y
    // ese asset no se puede sustituir desde el servidor, asi que al beber colocamos nuestra lata en lugar de la malla original
    // y lo deshacemos al terminar (los modelos en mano se reutilizan, no debe quedar oculta la TarCola normal).
    internal static class HandModel
    {
        private static AssetBundle _bundle;
        private static GameObject _prefab;
        private static bool _loadFailed;

        private static int _doneId;                       // controlador de manos ya procesado
        private static int _tries;
        private static GameObject _instance;
        private static readonly List<Renderer> _hidden = new List<Renderer>();

        private static bool Load()
        {
            if (_prefab != null) return true;
            if (_loadFailed) return false;
            try
            {
                string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", "speedcola_hand.bundle"));
                _bundle = AssetBundle.LoadFromFile(path);
                if (_bundle == null) { Plugin.Log.LogWarning("No se pudo cargar el bundle de la mano: " + path); _loadFailed = true; return false; }
                _prefab = _bundle.LoadAllAssets<GameObject>().FirstOrDefault();
                if (_prefab == null) { Plugin.Log.LogWarning("El bundle de la mano no contiene ningun prefab"); _loadFailed = true; return false; }
                Dbg.Log("MANO", "bundle de la mano cargado: " + _prefab.name);
                return true;
            }
            catch (System.Exception e) { Plugin.Log.LogError("Error cargando el modelo de la mano: " + e); _loadFailed = true; return false; }
        }

        // Se llama cada frame con el controlador de manos actual (barato: sale enseguida si no es la Speed Cola).
        internal static void Tick(object hands)
        {
            var ihc = hands as Player.ItemHandsController;
            var item = ihc != null ? ihc.Item : null;
            bool isCola = item != null && item.TemplateId == Plugin.SpeedColaId;

            if (!isCola)
            {
                if (_instance != null || _hidden.Count > 0) Restore();
                _doneId = 0; _tries = 0;
                return;
            }

            // OJO: el controlador es un componente del JUGADOR; el modelo del objeto en mano es su ControllerGameObject
            var root = ihc.ControllerGameObject;
            if (root == null) return;
            int id = root.GetInstanceID();
            if (id == _doneId) return;
            if (!Load()) { _doneId = id; return; }

            // el modelo puede tardar unos frames en aparecer: reintenta hasta ~2 s
            if (!Apply(root) && ++_tries < 120) return;
            _doneId = id;
        }

        private static bool Apply(GameObject root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(false)
                .Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null && f.GetComponent<MeshRenderer>().enabled
                            && f.GetComponentInParent<HandModelMarker>() == null)
                .ToList();

            if (_tries == 0)
                Dbg.Log("MANO", "objeto en mano '" + root.name + "' - renderizadores: " + string.Join(" | ", root.GetComponentsInChildren<Renderer>(false)
                    .Select(r => r.GetType().Name + ":" + r.gameObject.name + (r is MeshRenderer ? "(" + (r.GetComponent<MeshFilter>()?.sharedMesh?.name ?? "?") + ")" : "")).ToArray()));

            if (filters.Count == 0) return false;

            // referencia: la malla mas grande (el cuerpo de la lata)
            var reference = filters.OrderByDescending(f => { var s = f.sharedMesh.bounds.size; return s.x * s.y * s.z; }).First();
            var bounds = reference.sharedMesh.bounds;
            var size = bounds.size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;       // eje largo de la malla = altura de la lata
            Vector3 up = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
            float height = size[axis];

            _instance = Object.Instantiate(_prefab, reference.transform, false);
            _instance.name = "TarkovaColaHand";
            _instance.AddComponent<HandModelMarker>();
            foreach (var c in _instance.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            SetLayer(_instance, reference.gameObject.layer);
            _instance.transform.localRotation = Quaternion.FromToRotation(Vector3.up, up);
            _instance.transform.localScale = Vector3.one * (height / 0.122f) * Plugin.CfgHandScale.Value;     // nuestra lata mide 0.122 m
            _instance.transform.localPosition = bounds.center;

            foreach (var f in filters)
            {
                var r = f.GetComponent<MeshRenderer>();
                r.enabled = false;
                _hidden.Add(r);
            }
            Dbg.Log("MANO", "modelo de la mano reemplazado (" + filters.Count + " malla(s), eje " + axis + ", altura local " + height.ToString("0.000") + ")");
            return true;
        }

        private static void Restore()
        {
            if (_instance != null) Object.Destroy(_instance);
            foreach (var r in _hidden) if (r != null) r.enabled = true;
            _hidden.Clear();
            _instance = null;
            Dbg.Log("MANO", "modelo de la mano restaurado");
        }

        private static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }
    }

    // marca nuestro modelo para no confundirlo con las mallas originales
    internal class HandModelMarker : MonoBehaviour { }
}
