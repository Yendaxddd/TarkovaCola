using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using UnityEngine;

namespace TarkovaCola.Client
{
    // "le creatura": M4 SOPMOD II Jr (Girls' Frontline) como granada de racimo. Por dentro es una RGD-5 (Server/Creatura.cs):
    //   - su modelo (en la mano, en el suelo, en el icono y lanzada) se cambia por el peluche de Jr (assets/jr.bundle);
    //   - al lanzarla suena uno de 5 sonidos al azar;
    //   - al explotar suelta 3 explosiones pequenas alrededor ("Frenzied Circus", la habilidad de SOP-II).
    // La opcion secreta de F12 hace que Therapist la venda; si no, solo se encuentra en el mercadillo buscando "le creatura".
    internal static class Creatura
    {
        internal const string ItemId = "6a1c00000000000000000b01";
        internal const string Rgd5 = "5448be9a4bdc2dfd2f8b456a";
        private static readonly string[] Sounds = { "jr_throw_1.mp3", "jr_throw_2.mp3", "jr_throw_3.mp3", "jr_throw_4.mp3", "jr_throw_5.mp3" };

        // racimo
        private const float SoundSeconds = 10f;      // los sonidos se cortan a los 10 s

        internal const int Bomblets = 3;
        private const float BombletDelay = 0.35f, BombletGap = 0.18f;
        private const float BombletRadiusMin = 1.8f, BombletRadiusMax = 3.2f;
        private const float BombletFragments = 0.4f, BombletRange = 0.6f;      // cada explosion pequena: 40% de fragmentos y 60% de radio

        private static GameObject _prefab;
        private static bool _loadFailed;
        [ThreadStatic] private static bool _inBomblet;

        internal static bool Is(Item item) { return item != null && item.StringTemplateId == ItemId; }

        // ---------------------------------------------------------------- opcion secreta
        internal static void SyncSale()
        {
            try
            {
                bool on = Plugin.CfgJr.Value;
                var body = new JObject { ["on"] = on }.ToString(Formatting.None);
                RequestHandler.PostJsonAsync("/tarkovacola/jr", body).ContinueWith(t =>
                {
                    if (t.IsFaulted) Plugin.Log.LogWarning("le creatura: no se pudo avisar al servidor: " + t.Exception?.GetBaseException().Message);
                });
                if (on) Dbg.Log("JR", "la opcion secreta esta activada. Por que.");
            }
            catch (Exception e) { Plugin.Log.LogWarning("le creatura (opcion): " + e.Message); }
        }

        // ---------------------------------------------------------------- modelo
        private static GameObject Prefab()
        {
            if (_prefab != null || _loadFailed) return _prefab;
            try
            {
                string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), Path.Combine("assets", "jr.bundle"));
                var bundle = AssetBundle.LoadFromFile(path);
                _prefab = bundle != null ? bundle.LoadAllAssets<GameObject>().FirstOrDefault() : null;
                if (_prefab == null) { _loadFailed = true; Plugin.Log.LogWarning("le creatura: no se pudo cargar " + path); }
            }
            catch (Exception e) { _loadFailed = true; Plugin.Log.LogError("le creatura (bundle): " + e); }
            return _prefab;
        }

        // Pone a Jr en lugar de la malla de la RGD-5 (idempotente)
        internal static void Dress(GameObject go)
        {
            if (go == null || go.GetComponent<JrDress>() != null) return;
            var prefab = Prefab();
            if (prefab == null) return;
            var renderers = go.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToList();
            if (renderers.Count == 0) return;

            var main = renderers.OrderByDescending(r => r.bounds.size.sqrMagnitude).First();
            Bounds local = main is SkinnedMeshRenderer smr ? smr.localBounds
                : (main.GetComponent<MeshFilter>() != null && main.GetComponent<MeshFilter>().sharedMesh != null ? main.GetComponent<MeshFilter>().sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 0.1f));

            var jr = UnityEngine.Object.Instantiate(prefab, main.transform, false);
            jr.name = "le_creatura";
            foreach (var c in jr.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(c);
            float size = Mathf.Max(local.size.x, Mathf.Max(local.size.y, local.size.z));
            float lossy = main.transform.lossyScale.x;
            float target = Mathf.Max(0.09f, size * Mathf.Abs(lossy) * 1.6f);            // un poco mas grande que la granada
            jr.transform.localPosition = local.center;
            // de pie a lo largo del eje largo de la granada (si no, queda tumbada sobre la mano)
            Vector3 axis = local.size.x >= local.size.y && local.size.x >= local.size.z ? Vector3.right : (local.size.z >= local.size.y ? Vector3.forward : Vector3.up);
            jr.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
            Dbg.Log("JR", "vestido " + go.name + " sobre " + main.name + " (malla " + local.size + ", eje " + axis + ")");
            jr.transform.localScale = Vector3.one * (target / 0.13f) / Mathf.Max(0.0001f, Mathf.Abs(lossy));
            int layer = main.gameObject.layer;
            foreach (var t in jr.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;

            var dress = go.AddComponent<JrDress>();
            dress.Model = jr;
            dress.Hidden = renderers;
            foreach (var r in renderers) r.enabled = false;
        }

        // Un objeto de RGD-5 que el juego reutiliza del pool para una RGD-5 normal: se le quita a Jr
        internal static void Undress(GameObject go)
        {
            var dress = go != null ? go.GetComponent<JrDress>() : null;
            if (dress == null) return;
            if (dress.Model != null) UnityEngine.Object.Destroy(dress.Model);
            foreach (var r in dress.Hidden) if (r != null) r.enabled = true;
            UnityEngine.Object.Destroy(dress);
        }

        internal static void OnItemObject(Item item, GameObject go)
        {
            try
            {
                if (go == null || item == null) return;
                if (Is(item)) Dress(go);
                else if (item.StringTemplateId == Rgd5) Undress(go);
            }
            catch (Exception e) { Plugin.Log.LogError("le creatura (modelo): " + e); }
        }

        // ---------------------------------------------------------------- lanzar
        internal static void OnThrown(Grenade g, string profileId, ThrowWeap w)
        {
            if (!Is(w)) return;
            try
            {
                Dress(g.gameObject);
                var me = Plugin.Me;
                if (me != null && me.ProfileId == profileId)
                {
                    string s = Sounds[UnityEngine.Random.Range(0, Sounds.Length)];
                    Jingle.Sfx(s, SoundSeconds);
                    Dbg.Log("JR", "le creatura lanzada (" + s + ")");
                }
            }
            catch (Exception e) { Plugin.Log.LogError("le creatura (lanzar): " + e); }
        }

        // ---------------------------------------------------------------- racimo
        internal static void OnExploded(IExplosiveItem grenadeItem, Vector3 pos, string profileId, Item originalItem)
        {
            if (_inBomblet) return;
            var w = grenadeItem as ThrowWeap;
            if (!Is(w) || Plugin.Instance == null) return;
            Plugin.Instance.StartCoroutine(Cluster(w, pos, profileId, originalItem ?? w));
        }

        private static IEnumerator Cluster(ThrowWeap w, Vector3 center, string profileId, Item original)
        {
            yield return new WaitForSeconds(BombletDelay);
            float start = UnityEngine.Random.Range(0f, 360f);
            for (int i = 0; i < Bomblets; i++)
            {
                if (Singleton<GameWorld>.Instance == null) yield break;
                float ang = (start + i * 360f / Bomblets + UnityEngine.Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
                var pos = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * UnityEngine.Random.Range(BombletRadiusMin, BombletRadiusMax);
                RaycastHit hit;
                if (Physics.Raycast(pos + Vector3.up * 1.2f, Vector3.down, out hit, 4f, LayerMask.GetMask("HighPolyCollider", "LowPolyCollider", "Terrain"))) pos = hit.point + Vector3.up * 0.1f;
                // que no atraviese paredes: si hay algo entre el centro y el punto, se queda delante
                if (Physics.Linecast(center + Vector3.up * 0.3f, pos + Vector3.up * 0.3f, out hit, LayerMask.GetMask("HighPolyCollider", "LowPolyCollider"))) pos = hit.point - (pos - center).normalized * 0.4f;
                Bomblet(w, pos, profileId, original);
                yield return new WaitForSeconds(BombletGap);
            }
        }

        // (tambien la usa la Caida explosiva de PHD Flopper)
        internal static void Bomblet(ThrowWeap w, Vector3 pos, string profileId, Item original)
        {
            var tpl = w.Template as ThrowWeapTemplate;
            if (tpl == null) return;
            int frags = tpl.FragmentsCount; float min = tpl.MinExplosionDistance, max = tpl.MaxExplosionDistance;
            _inBomblet = true;
            try
            {
                tpl.FragmentsCount = Mathf.Max(1, Mathf.RoundToInt(frags * BombletFragments));
                tpl.MinExplosionDistance = min * BombletRange;
                tpl.MaxExplosionDistance = max * BombletRange;
                if (!string.IsNullOrEmpty(w.ExplosionEffectType)) Singleton<Systems.Effects.Effects>.Instance.EmitGrenade(w.ExplosionEffectType, pos, Vector3.up);
                var calc = Singleton<IGameLevel>.Instance != null ? Singleton<IGameLevel>.Instance.CreateBallisticCalculator(0) : null;
                Grenade.Explosion(null, w, pos, profileId, calc, original, new Vector3(0f, 0.08f, 0f));
                Dbg.Log("JR", "mini explosion en " + pos);
            }
            catch (Exception e) { Plugin.Log.LogError("le creatura (racimo): " + e); }
            finally
            {
                tpl.FragmentsCount = frags; tpl.MinExplosionDistance = min; tpl.MaxExplosionDistance = max;
                _inBomblet = false;
            }
        }
    }

    // Marca un objeto vestido de Jr: guarda lo ocultado y lo mantiene oculto (el juego puede volver a encender las mallas)
    internal class JrDress : MonoBehaviour
    {
        internal GameObject Model;
        internal List<Renderer> Hidden = new List<Renderer>();

        private void LateUpdate()
        {
            for (int i = 0; i < Hidden.Count; i++) if (Hidden[i] != null && Hidden[i].enabled) Hidden[i].enabled = false;
        }
    }

    // Todo objeto de un item (en la mano, en el suelo, el del icono) sale de aqui
    [HarmonyPatch]
    internal static class CreatureItemObjectPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return typeof(ObjectsFactory).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateItemAsync" && m.GetParameters().Length >= 5 && m.GetParameters()[0].ParameterType == typeof(Item));
        }

        [HarmonyPostfix]
        private static void Postfix(Item item, ref Task<GameObject> __result)
        {
            if (item == null || __result == null) return;
            string tpl = item.StringTemplateId;
            if (tpl != Creatura.ItemId && tpl != Creatura.Rgd5) return;
            var task = __result;
            if (task.IsCompleted) { if (task.Status == TaskStatus.RanToCompletion) Creatura.OnItemObject(item, task.Result); return; }
            __result = task.ContinueWith(t =>
            {
                var go = t.Result;
                Creatura.OnItemObject(item, go);
                return go;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }

    // Las mini explosiones salen de la explosion de la granada
    [HarmonyPatch]
    internal static class CreatureExplosionPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return typeof(Grenade).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == "Explosion" && m.GetParameters().Length >= 4);
        }

        [HarmonyPostfix]
        private static void Postfix(IExplosiveItem grenadeItem, Vector3 grenadePosition, string playerProfileIDWhoThrew, Item originalWeaponItem)
        {
            try { Creatura.OnExploded(grenadeItem, grenadePosition, playerProfileIDWhoThrew, originalWeaponItem); }
            catch (Exception e) { Plugin.Log.LogError("le creatura (explosion): " + e); }
        }
    }
}
