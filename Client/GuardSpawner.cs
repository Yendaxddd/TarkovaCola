using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Quests;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Aparicion de los Embotelladores (guardias de las misiones y el Barman). Propio del mod: VisitAPI pedia los bots con bando
    //  "Savage" aunque el rol fuera pmcBEAR/pmcUSEC, y el juego descartaba los perfiles (no coincidia el bando) -> no aparecia nadie.
    //  Aqui el bando sale del rol, la eleccion de perfil es tolerante (si la dificultad no coincide, vale igual) y cada paso se
    //  registra en el log con [GUARDIAS] para poder ver exactamente que pasa.
    //  Cuando: al empezar la raid, por cada punto de la mision en este mapa cuya mision este EN MARCHA (o siempre, con la opcion de
    //  depuracion de F12). Se espera a que el sistema de bots este listo, se buscan puntos caminables alrededor, se generan los
    //  perfiles (con reintentos), se equipan (QuestGuards.Equip) y se activan. Si faltan, se reintenta con el resto.
    // ============================================================================================
    internal static class GuardSpawner
    {
        private const float ReadyTimeout = 150f, ProfilesTimeout = 90f;
        private static int _raid;                                  // para cortar las rutinas de una raid anterior

        internal static void Log(string msg) { Plugin.Log.LogInfo("[GUARDIAS] " + msg); }

        // Desde el inicio del jugador (Tracker): lanza una rutina por cada punto que toque en este mapa
        internal static void OnPlayerReady(Player me)
        {
            _raid++;
            var posts = QuestGuards.Posts;
            string map = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance.LocationId : "?";
            if (posts == null || posts.Count == 0) { Log("no hay puntos de guardias (el servidor no envio ninguno)"); return; }
            Log("raid en '" + map + "': " + posts.Count + " punto(s) en el pack; opcion 'siempre' = " + Plugin.CfgGuardsAlways.Value);
            foreach (var post in posts.OfType<JObject>())
            {
                string pmap = (string)post["map"];
                int mission = post["tc"] != null ? ((int?)post["tc"]["mission"] ?? 0) : 0;
                if (!string.Equals(pmap, map, StringComparison.OrdinalIgnoreCase)) { Log("  mision " + mission + ": es de '" + pmap + "', no de este mapa"); continue; }
                string status = QuestStatus(me, (string)post["quest"]);
                bool go = Plugin.CfgGuardsAlways.Value || status == "Started";
                Log("  mision " + mission + ": estado de la mision = " + status + (go ? " -> aparecen" : " -> no aparecen (la mision no esta en marcha)"));
                if (go) Plugin.Instance.StartCoroutine(Spawn(post, _raid));
            }
        }

        private static string QuestStatus(Player me, string questId)
        {
            try
            {
                var q = me.Profile.QuestsData.FirstOrDefault(x => x.Id == questId);
                return q == null ? "sin empezar" : q.Status.ToString();
            }
            catch (Exception e) { return "desconocido (" + e.Message + ")"; }
        }

        // Bots sueltos para otras cosas del mod (pruebas de TEDD): sin equipo de mision; onSpawn recibe cada bot al aparecer
        internal static void SpawnBots(string tag, string role, int count, Vector3 at, float radius, Action<BotOwner> onSpawn)
        {
            var post = new JObject
            {
                ["tag"] = tag, ["role"] = role, ["difficulty"] = "normal", ["max"] = count, ["radius"] = radius, ["noEquip"] = true,
                ["at"] = new JObject { ["x"] = at.x, ["y"] = at.y, ["z"] = at.z },
            };
            Plugin.Instance.StartCoroutine(Spawn(post, _raid, onSpawn));
        }

        private static IEnumerator Spawn(JObject post, int raid, Action<BotOwner> onSpawn = null)
        {
            var tc = post["tc"] as JObject;
            int mission = tc != null ? ((int?)tc["mission"] ?? 0) : 0;
            string tag = post["tag"] != null ? (string)post["tag"] + ": " : "mision " + mission + (tc != null && (bool?)tc["boss"] == true ? " (JEFE)" : "") + ": ";
            float t0 = Time.realtimeSinceStartup;

            // 1) esperar a que el juego y el sistema de bots esten listos
            BotSpawner spawner = null;
            while (Time.realtimeSinceStartup - t0 < ReadyTimeout)
            {
                if (raid != _raid || !Plugin.InRaidWorld) { Log(tag + "la raid termino antes de poder generar"); yield break; }
                try
                {
                    if (Singleton<AbstractGame>.Instantiated && (int)Singleton<AbstractGame>.Instance.Status == 4 && Singleton<IBotGame>.Instantiated)
                    {
                        var s = Singleton<IBotGame>.Instance.BotsController != null ? Singleton<IBotGame>.Instance.BotsController._botSpawner : null;
                        if (s != null && s._botCreator != null) spawner = s;
                    }
                }
                catch (Exception e) { Log(tag + "esperando al sistema de bots: " + e.Message); }
                if (spawner != null) break;
                yield return new WaitForSeconds(1f);
            }
            if (spawner == null) { Log(tag + "ERROR: el sistema de bots no estuvo listo en " + ReadyTimeout + " s"); yield break; }
            Log(tag + "sistema de bots listo tras " + (Time.realtimeSinceStartup - t0).ToString("0.0") + " s");

            // 2) parametros del punto
            string roleName = (string)post["role"] ?? "pmcBEAR";
            WildSpawnType role;
            try { role = (WildSpawnType)Enum.Parse(typeof(WildSpawnType), roleName, true); }
            catch { Log(tag + "ERROR: el rol '" + roleName + "' no existe en el juego"); yield break; }
            BotDifficulty dif;
            try { dif = (BotDifficulty)Enum.Parse(typeof(BotDifficulty), (string)post["difficulty"] ?? "normal", true); }
            catch { dif = BotDifficulty.normal; }
            EPlayerSide side = roleName.IndexOf("bear", StringComparison.OrdinalIgnoreCase) >= 0 ? EPlayerSide.Bear
                             : roleName.IndexOf("usec", StringComparison.OrdinalIgnoreCase) >= 0 ? EPlayerSide.Usec : EPlayerSide.Savage;
            int want = Mathf.Max(1, (int?)post["max"] ?? (int?)post["min"] ?? 1);
            var at = post["at"];
            var center = new Vector3((float)at["x"], (float)at["y"], (float)at["z"]);
            float radius = (float?)post["radius"] ?? 6f;
            Log(tag + want + " x " + role + " (bando " + side + ", " + dif + ") en " + center.ToString("F1") + ", radio " + radius);

            // 3) puntos caminables (si no hay bastantes, se amplia el radio)
            var points = Points(center, radius, want);
            for (int k = 2; points.Count < want && k <= 4; k *= 2) points = Points(center, radius * k, want);
            if (points.Count == 0) { Log(tag + "ERROR: no hay suelo caminable cerca del punto (revisa las coordenadas con F10)"); yield break; }
            if (points.Count < want) Log(tag + "solo " + points.Count + "/" + want + " sitios libres: apareceran menos");
            Log(tag + "sitios: " + string.Join(" ", points.Select(p => p.ToString("F1")).ToArray()));

            var anchor = Anchor(spawner, center);
            if (anchor.zone == null) { Log(tag + "ERROR: este mapa no tiene zonas de bots"); yield break; }
            Log(tag + "zona de bots mas cercana: " + anchor.zone.name + " (punto " + anchor.core + ")");

            // 4) perfiles: se piden DIRECTAMENTE al servidor (sin pasar por la cola de bots del juego, que en algunas partidas no
            //    respondia) y se cargan sus modelos. Cada paso con su tiempo en el log. Hasta 3 intentos para completar el grupo.
            var session = Session(spawner);
            if (session == null) { Log(tag + "ERROR: no hay sesion con el servidor para pedir bots"); yield break; }
            var profiles = new List<Profile>();
            for (int attempt = 1; attempt <= 3 && profiles.Count < points.Count; attempt++)
            {
                int need = points.Count - profiles.Count;
                Task<Profile[]> load = null;
                float tStart = Time.realtimeSinceStartup;
                try { load = session.LoadBots(new List<CountTypeBotWave> { new CountTypeBotWave(need, role, dif) }); }
                catch (Exception e) { Log(tag + "intento " + attempt + ": ERROR al pedir los bots: " + e.Message); }
                while (load != null && !load.IsCompleted && Time.realtimeSinceStartup - tStart < ProfilesTimeout)
                {
                    if (raid != _raid || !Plugin.InRaidWorld) yield break;
                    yield return null;
                }
                if (load == null) continue;
                if (!load.IsCompleted) { Log(tag + "intento " + attempt + ": el servidor no devolvio los bots en " + ProfilesTimeout + " s"); continue; }
                if (load.IsFaulted) { Log(tag + "intento " + attempt + ": error del servidor: " + load.Exception?.GetBaseException().Message); continue; }
                var arr = load.Result ?? new Profile[0];
                Log(tag + "intento " + attempt + ": el servidor devolvio " + arr.Length + "/" + need + " perfiles en " + (Time.realtimeSinceStartup - tStart).ToString("0.0") + " s: " +
                    string.Join(", ", arr.Where(x => x != null).Select(x => x.Info.Nickname + " (" + x.Info.Side + "/" + x.Info.Settings.Role + "/" + x.Info.Settings.BotDifficulty + ")").ToArray()));
                foreach (var prof in arr)
                {
                    if (prof == null || profiles.Count >= points.Count) continue;
                    // modelos (ropa, armas...) del bot; sin esto no se puede crear en el mundo
                    Task bundles = null;
                    float tb = Time.realtimeSinceStartup;
                    try
                    {
                        bundles = Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(ObjectsFactory.PoolsCategory.Raid, ObjectsFactory.AssemblyType.Local,
                            prof.GetAllPrefabPaths(false).ToArray(), Diz.Jobs.JobYieldPriority.General, null, ObjectsFactory.DefaultCancellationToken);
                    }
                    catch (Exception e) { Log(tag + "ERROR cargando los modelos de " + prof.Info.Nickname + ": " + e.Message); }
                    while (bundles != null && !bundles.IsCompleted && Time.realtimeSinceStartup - tb < ProfilesTimeout)
                    {
                        if (raid != _raid || !Plugin.InRaidWorld) yield break;
                        yield return null;
                    }
                    if (bundles == null || !bundles.IsCompleted || bundles.IsFaulted)
                    {
                        Log(tag + "modelos de " + prof.Info.Nickname + ": " + (bundles == null ? "no se pudieron pedir" : !bundles.IsCompleted ? "no cargaron en " + ProfilesTimeout + " s" : "error " + bundles.Exception?.GetBaseException().Message));
                        continue;
                    }
                    profiles.Add(prof);
                }
                Log(tag + "intento " + attempt + ": " + profiles.Count + "/" + points.Count + " guardias listos (perfil y modelos)");
            }
            if (profiles.Count == 0) { Log(tag + "ERROR: no se pudo preparar ningun guardia de " + role); yield break; }
            if (raid != _raid || !Plugin.InRaidWorld) yield break;

            var param = new GuardProfileData(new GetProfileDataParams(side, role, dif, 0f,
                            new BotSpawnParams { ShallBeGroup = new ShallBeGroupParams(true, false, profiles.Count) }, true));
            var data = BotCreationData.CreateWithoutProfile(param);
            data.AddProfiles(profiles);

            // 5) equipar (perks, botellas, objeto de mision) y activar. Como hace el propio juego: una sola activacion para todo el
            //    grupo (el juego crea un bot por cada perfil mientras queden posiciones) y el contador de "apareciendo" sube antes.
            while (data.Profiles.Count > points.Count) data.RemoveProfile(data.Profiles[data.Profiles.Count - 1]);     // perfiles de sobra
            int toSpawn = data.Profiles.Count;
            if ((bool?)post["noEquip"] != true) QuestGuards.Equip(data, post);
            for (int i = 0; i < toSpawn; i++) data.AddPosition(points[i], anchor.core);
            try
            {
                spawner._inSpawnProcess += toSpawn;
                spawner.method_10(anchor.zone, data, onSpawn != null ? b => { try { onSpawn(b); } catch (Exception e) { Log(tag + "aviso de aparicion: " + e.Message); } } : (Action<BotOwner>)Hold,
                    spawner.GetCancelToken());
                Log(tag + toSpawn + "/" + want + " guardias enviados a aparecer (cada uno avisara al llegar a su puesto)");
            }
            catch (Exception e) { Log(tag + "ERROR al activar los guardias: " + e); }
        }

        // la sesion con el servidor que usa el propio juego para pedir bots (o la de la aplicacion, si no)
        private static IEftSession Session(BotSpawner spawner)
        {
            try
            {
                var creator = spawner._botCreator as BotCreatorClient;
                var client = creator != null ? creator._creatorProfile as BotProfileClient : null;
                if (client != null && client._backEndSession != null) return client._backEndSession;
            }
            catch (Exception e) { Log("sesion del creador de bots no disponible: " + e.Message); }
            var app = Singleton<ClientApplication<IEftSession>>.Instance;
            return app != null ? app.GetClientBackEndSession() : null;
        }

        // los guardias se quedan en su puesto (no patrullan)
        private static void Hold(BotOwner bot)
        {
            try
            {
                if (bot == null) return;
                bot.PatrollingData?.Pause();
                Log("guardia en su puesto: " + bot.Profile.Info.Nickname + " (" + bot.Profile.Info.Settings.Role + ") en " + bot.Position.ToString("F1"));
            }
            catch (Exception e) { Log("no se pudo dejar al guardia en su puesto: " + e.Message); }
        }

        private static List<Vector3> Points(Vector3 at, float radius, int count)
        {
            var list = new List<Vector3>();
            NavMeshHit hit;
            float baseY = NavMesh.SamplePosition(at, out hit, 3f, -1) ? hit.position.y : at.y;
            if (NavMesh.SamplePosition(at, out hit, 3f, -1)) list.Add(hit.position);                // el propio punto primero
            for (int i = 0; i < 80 && list.Count < count; i++)
            {
                var r = UnityEngine.Random.insideUnitCircle * radius;
                var p = at + new Vector3(r.x, 0f, r.y);
                if (!NavMesh.SamplePosition(p, out hit, 2f, -1)) continue;
                if (Mathf.Abs(hit.position.y - baseY) > 1.5f) continue;                                 // mismo piso
                var h = hit.position;
                if (list.Any(q => (q - h).sqrMagnitude < 1.44f)) continue;                               // separados 1.2 m
                list.Add(h);
            }
            return list;
        }

        private static (BotZone zone, int core) Anchor(BotSpawner spawner, Vector3 at)
        {
            BotZone best = null; int core = 0; float d = float.MaxValue;
            foreach (var z in spawner._allBotZones ?? new BotZone[0])
            {
                if (z == null || z.SpawnPointMarkers == null) continue;
                foreach (var m in z.SpawnPointMarkers)
                {
                    if (m == null || m.SpawnPoint == null) continue;
                    float dd = (m.SpawnPoint.Position - at).sqrMagnitude;
                    if (dd < d) { d = dd; best = z; core = m.SpawnPoint.CorePointId; }
                }
            }
            return (best, core);
        }
    }

    // Peticion de perfiles como la del juego, pero tolerante al elegir: el servidor (o mods de bots) pueden devolver la dificultad
    // cambiada; si no hay ninguno exacto, vale uno del mismo rol.
    internal class GuardProfileData : IGetProfileData
    {
        private readonly GetProfileDataParams _p;
        internal int Picked, Relaxed;
        internal GuardProfileData(GetProfileDataParams p) { _p = p; }

        public EPlayerSide? Side { get { return _p.Side; } }
        public BotSpawnParams SpawnParams { get { return _p.SpawnParams; } set { _p.SpawnParams = value; } }
        public bool KeepZoneOnSpawn { get { return _p.KeepZoneOnSpawn; } }
        public bool TryGetRole(out WildSpawnType role, out BotDifficulty difficulty) { return _p.TryGetRole(out role, out difficulty); }

        public Profile ChooseProfile(List<Profile> profiles2Select, bool withDelete)
        {
            var exact = _p.ChooseProfile(profiles2Select, withDelete);
            if (exact != null) { Picked++; return exact; }
            var list = profiles2Select.Where(x => x.Info.Settings.Role == _p.Role).ToList();
            if (list.Count == 0) return null;
            var p = list[UnityEngine.Random.Range(0, list.Count)];
            if (withDelete) profiles2Select.Remove(p);
            Picked++; Relaxed++;
            return p;
        }

        public CountTypeBotWave[] PrepareToLoadBackend(int count) { return _p.PrepareToLoadBackend(count); }
        public bool IsValidSpawnType(WildSpawnType t) { return _p.IsValidSpawnType(t); }
        public string GetDebugLocalName() { return _p.GetDebugLocalName(); }
        public string GetDebugData() { return "TarkovaCola" + _p.GetDebugData(); }
        public bool ShallChooseByData() { return _p.ShallChooseByData(); }
        public bool IsBossOrFollowerByTime() { return _p.IsBossOrFollowerByTime(); }
        public bool IsZeroWave() { return _p.IsZeroWave(); }
        public bool IsBossOrFollower() { return _p.IsBossOrFollower(); }
        public bool IsSpawnOnStart() { return _p.IsSpawnOnStart(); }
        public bool CanAtZoneByType(BotZone botZone, BotZonesLeaveController c) { return _p.CanAtZoneByType(botZone, c); }
    }
}
