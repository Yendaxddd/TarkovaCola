using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;
using Path = System.IO.Path;

namespace TarkovaCola.Server
{
    // ============================================================================================
    //  La historia de los Embotelladores (misiones via VisitAPI). El pack con las misiones vive en
    //  user/mods/VisitAPI-Server/packs/TarkovaCola/ (lo despliega el build). Este mod pone lo que el pack no puede, leido de story.json
    //  (lo genera make_quest_pack.py):
    //   - los objetos de mision que llevan los guardias (clones de un objeto de informacion),
    //   - las recetas de las perks en la Unidad de nutricion, bloqueadas hasta las misiones de introduccion,
    //   - el bloqueo de la venta de cada perk en Therapist hasta completar su mision,
    //   - la ruta que le dice al cliente donde estan los guardias y que perks llevan.
    //  Si VisitAPI no esta instalado, no hay misiones: las recetas no se anaden y las perks se venden como siempre.
    // ============================================================================================
    internal static class QuestIds
    {
        internal const string PackName = "TarkovaCola";
        internal const string InfoParent = "5448ecbe4bdc2d60728b4568";      // clase base de los objetos de informacion / mision

        internal static string ModDir { get { return Path.GetDirectoryName(typeof(QuestIds).Assembly.Location) ?? "."; } }

        internal static string PackDir
        {
            get { return Path.Combine(Path.GetFullPath(Path.Combine(ModDir, "..")), "VisitAPI-Server", "packs", PackName); }
        }

        // puntos de los guardias (los genera make_quest_pack.py; los hace aparecer el cliente del mod, no VisitAPI)
        internal static string SpawnsFile { get { return Path.Combine(ModDir, "guards.json"); } }
        // el pack esta Y VisitAPI esta instalado (el zip trae el pack aunque no tengas VisitAPI: sin el no hay misiones, asi que no se bloquea nada)
        internal static bool PackInstalled
        {
            get { return File.Exists(Path.Combine(PackDir, "pack.json")) && File.Exists(Path.Combine(PackDir, "..", "..", "VisitAPI-Server.dll")); }
        }

        // Todos los puntos de guardias del pack (JSON de un array), o "" si no hay
        internal static string ReadPosts()
        {
            try { return File.Exists(SpawnsFile) ? File.ReadAllText(SpawnsFile) : ""; }
            catch (Exception ex) { Console.WriteLine("[Tarkova-Cola] No se pudieron leer los puntos de las misiones: " + ex.Message); return ""; }
        }
    }

    // Bloqueo de las ventas de perks en Therapist (se puede quitar con la opcion de depuracion del cliente)
    internal static class SalesLock
    {
        private static TradersTable _traders;
        private static object _assortHelper;                 // TraderAssortHelper: guarda en cache los bloqueos de todas las tiendas
        private static readonly Dictionary<MongoId, MongoId> _locks = new Dictionary<MongoId, MongoId>();
        private static readonly object _gate = new object();

        internal static void Init(TradersTable traders, object assortHelper, Dictionary<MongoId, MongoId> locks)
        {
            _traders = traders;
            _assortHelper = assortHelper;
            foreach (var kv in locks) _locks[kv.Key] = kv.Value;
            Set(false);
        }

        // El servidor junta los bloqueos de todas las tiendas la primera vez y los guarda: se borra esa copia para que se rehaga
        private static void ResetCache()
        {
            try
            {
                var f = _assortHelper?.GetType().GetField("<MergedQuestAssorts>k__BackingField",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (f != null) f.SetValue(_assortHelper, null);
                else Console.WriteLine("[Tarkova-Cola] Aviso: no se encontro la cache de bloqueos de tiendas (el cambio se vera al reiniciar el servidor)");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] No se pudo rehacer la cache de bloqueos: " + e.Message); }
        }

        internal static void Set(bool unlocked)
        {
            lock (_gate)
            {
                if (_traders == null || _locks.Count == 0) return;
                var qa = _traders[Traders.THERAPIST].QuestAssort;
                if (qa == null) return;
                if (!qa.TryGetValue("success", out var success) || success == null) qa["success"] = success = new Dictionary<MongoId, MongoId>();
                foreach (var kv in _locks)
                {
                    if (unlocked) success.Remove(kv.Key);
                    else success[kv.Key] = kv.Value;
                }
                ResetCache();
                Console.WriteLine("[Tarkova-Cola] Ventas de perks en Therapist: " + (unlocked ? "DESBLOQUEADAS (opcion de depuracion)" : "bloqueadas hasta completar su mision"));
            }
        }
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 10)]
    public class TarkovaColaQuests : IOnLoad
    {
        private readonly CustomItemService _items;
        private readonly TemplateTable _templates;
        private readonly TradersTable _traders;
        private readonly HideoutTable _hideout;
        private readonly SPTarkov.Server.Core.Helpers.Traders.TraderAssortHelper _assortHelper;

        public TarkovaColaQuests(CustomItemService items, TemplateTable templates, TradersTable traders, HideoutTable hideout, SPTarkov.Server.Core.Helpers.Traders.TraderAssortHelper assortHelper)
        {
            _items = items;
            _templates = templates;
            _traders = traders;
            _hideout = hideout;
            _assortHelper = assortHelper;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            JsonDocument story = null;
            try
            {
                var path = Path.Combine(QuestIds.ModDir, "story.json");
                if (!File.Exists(path)) { Console.WriteLine("[Tarkova-Cola] Falta story.json: no hay historia"); return Task.CompletedTask; }
                story = JsonDocument.Parse(File.ReadAllText(path));
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] story.json no se pudo leer: " + e.Message); return Task.CompletedTask; }

            using (story)
            {
                var root = story.RootElement;
                try { CreateItems(root.GetProperty("items")); }
                catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando los objetos de mision: " + e); }
                if (!QuestIds.PackInstalled)
                {
                    Console.WriteLine("[Tarkova-Cola] VisitAPI no esta instalado: sin misiones, sin recetas, perks a la venta desde el principio");
                    return Task.CompletedTask;
                }
                try { AddRecipes(root.GetProperty("recipes")); }
                catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR anadiendo las recetas: " + e); }
                try { LockSales(root.GetProperty("sales")); }
                catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR bloqueando las ventas: " + e); }
            }
            return Task.CompletedTask;
        }

        // Objetos de mision: clones de un objeto de informacion de 1x1 (preferiblemente ya marcado como objeto de mision)
        private void CreateItems(JsonElement list)
        {
            var info = _templates.Items.Values
                .Where(t => t.Parent == new MongoId(QuestIds.InfoParent) && t.Properties != null && t.Properties.Width == 1 && t.Properties.Height == 1)
                .OrderByDescending(t => t.Properties.QuestItem == true)
                .FirstOrDefault();
            if (info == null) { Console.WriteLine("[Tarkova-Cola] No hay ningun objeto de informacion que clonar"); return; }
            int n = 0;
            foreach (var it in list.EnumerateArray())
            {
                var id = it.GetProperty("id").GetString();
                if (_templates.Items.ContainsKey(new MongoId(id))) continue;
                // cada objeto copia el aspecto de uno parecido del juego (cuaderno, sobre, muestra...); si no existe, la memoria USB de siempre
                var src = info;
                if (it.TryGetProperty("clone", out var cl) && _templates.Items.TryGetValue(new MongoId(cl.GetString()), out var look) && look?.Properties != null) src = look;
                _items.CreateItemFromClone(new NewItemFromCloneDetails
                {
                    ItemTplToClone = src.Id,
                    ParentId = src.Parent,
                    NewId = new MongoId(id),
                    NewItemName = "tarkovacola_story_" + id.Substring(18),
                    OverrideProperties = new TemplateItemProperties { QuestItem = true, ExaminedByDefault = true, CanSellOnRagfair = false },
                    FleaPriceRoubles = 1,
                    HandbookPriceRoubles = 1,
                    HandbookParentId = "5b47574386f77428ca22b2ef",     // handbook: Info
                    Locales = new Dictionary<string, LocaleDetails>
                    {
                        ["en"] = new LocaleDetails { Name = it.GetProperty("nameEn").GetString(), ShortName = it.GetProperty("shortEn").GetString(), Description = it.GetProperty("descEn").GetString() },
                        ["es"] = new LocaleDetails { Name = it.GetProperty("nameEs").GetString(), ShortName = it.GetProperty("shortEs").GetString(), Description = it.GetProperty("descEs").GetString() },
                    },
                });
                n++;
            }
            Console.WriteLine("[Tarkova-Cola] " + n + " objetos de mision creados (clon de " + info.Name + ")");
        }

        // Recetas de las perks en la Unidad de nutricion: bloqueadas hasta que una mision las desbloquea (recompensa "ProductionScheme")
        private void AddRecipes(JsonElement list)
        {
            var recipes = _hideout.Production.Recipes;
            int n = 0;
            foreach (var r in list.EnumerateArray())
            {
                var id = new MongoId(r.GetProperty("id").GetString());
                if (recipes.Any(x => x.Id == id)) continue;
                int area = r.GetProperty("area").GetInt32();
                var reqs = new List<Requirement> { new Requirement { Type = "Area", AreaType = area, RequiredLevel = 1 } };
                foreach (var i in r.GetProperty("items").EnumerateArray())
                    reqs.Add(new Requirement { Type = "Item", TemplateId = new MongoId(i.GetProperty("tpl").GetString()), Count = i.GetProperty("count").GetInt32(), IsFunctional = false, IsEncoded = false, IsSpawnedInSession = false });
                reqs.Add(new Requirement { Type = "QuestComplete", QuestId = new MongoId(r.GetProperty("quest").GetString()) });
                recipes.Add(new HideoutProduction
                {
                    Id = id,
                    AreaType = (SPTarkov.Server.Core.Models.Enums.Hideout.HideoutAreas)area,
                    Requirements = reqs,
                    ProductionTime = r.GetProperty("time").GetInt32(),
                    EndProduct = new MongoId(r.GetProperty("product").GetString()),
                    IsEncoded = false,
                    Locked = true,
                    NeedFuelForAllProductionTime = false,
                    Continuous = false,
                    Count = 1,
                    ProductionLimitCount = 0,
                    IsCodeProduction = false,
                });
                n++;
            }
            Console.WriteLine("[Tarkova-Cola] " + n + " recetas de perks anadidas a la Unidad de nutricion (se desbloquean con la historia)");
        }

        // Cada perk deja de venderse en Therapist hasta completar su mision
        private void LockSales(JsonElement sales)
        {
            var locks = new Dictionary<MongoId, MongoId>();
            foreach (var s in sales.EnumerateObject()) locks[new MongoId(s.Name)] = new MongoId(s.Value.GetString());
            SalesLock.Init(_traders, _assortHelper, locks);
            Console.WriteLine("[Tarkova-Cola] " + locks.Count + " perks bloqueadas en Therapist hasta completar su mision");
        }
    }
}
