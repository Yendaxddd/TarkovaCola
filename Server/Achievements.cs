using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Commerce;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;

namespace TarkovaCola.Server
{
    // Logros NATIVOS de EFT. Se registran en la base de datos de logros del juego (aparecen en Perfil > Logros, con su imagen,
    // texto, rareza y recompensas). El cliente los desbloquea con la funcion del propio juego cuando se cumple la condicion
    // (las condiciones estandar del juego no pueden expresar "con Speed Cola activa"); aqui se guarda el logro en el perfil y
    // se entregan las recompensas una sola vez por correo, igual que hace SPT con los logros normales.
    internal static class AchievementData
    {
        internal const string Roubles = "5449016a4bdc2d6f028b456f";

        internal class Def
        {
            public string Id, Key, Rarity;
            public bool Hidden;
            public string NameEn, NameEs, DescEn, DescEs;
            public (string Tpl, int Count)[] Rewards;
        }

        internal static readonly Def[] All =
        {
            new Def { Id = "6a1c00000000000000000a01", Key = "first", Rarity = "Common",
                NameEn = "Tastes...sweet?", NameEs = "Sabe...¿dulce?",
                DescEn = "Kill your first enemy with Speed Cola active.", DescEs = "Mata a tu primer enemigo con Speed Cola activa.",
                Rewards = new[] { (Roubles, 25000) } },
            new Def { Id = "6a1c00000000000000000a02", Key = "lvl5", Rarity = "Rare",
                NameEn = "As long as no one gargles balls with this...", NameEs = "Mientras nadie se gargarice con esto...",
                DescEn = "Take Speed Cola to level 5.", DescEs = "Lleva Speed Cola al nivel 5.",
                Rewards = new[] { (Perks.GpCoin, 4) } },
            new Def { Id = "6a1c00000000000000000a03", Key = "all", Rarity = "Legendary",
                NameEn = "Speed is EVERYTHING.", NameEs = "¡La velocidad lo es TODO!",
                DescEn = "Unlock every Speed Cola augment.", DescEs = "Consigue todas las mejoras de Speed Cola.",
                Rewards = new[] { (Perks.SpeedColaId, 3) } },
            new Def { Id = "6a1c00000000000000000a04", Key = "found", Rarity = "Rare",
                NameEn = "...what is this?", NameEs = "...¿qué es esto?",
                DescEn = "Find a Speed Cola randomly in a raid.", DescEs = "Encuentra una Speed Cola al azar en una raid.",
                Rewards = new[] { (Perks.SpeedColaId, 1), (Roubles, 15000) } },
            new Def { Id = "6a1c00000000000000000a05", Key = "die", Rarity = "Common",
                NameEn = "Speeds up your life, literally.", NameEs = "Acelera tu vida, literalmente.",
                DescEn = "Die with Speed Cola active.", DescEs = "Muere con Speed Cola activa.",
                Rewards = new[] { (Perks.SpeedColaId, 1) } },
            new Def { Id = "6a1c00000000000000000a06", Key = "hand", Rarity = "Legendary", Hidden = true,
                NameEn = "Master of hand-tricks", NameEs = "Maestro de los trucos de mano",
                DescEn = "Kill 3 enemies in under 1 minute with Speed Cola active.", DescEs = "Mata a 3 enemigos en menos de 1 minuto con Speed Cola activa.",
                Rewards = new[] { (Perks.GpCoin, 5) } },
        };

        internal static string IdOf(string key) { return All.FirstOrDefault(a => a.Key == key)?.Id; }
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 2)]
    public class TarkovaColaAchievements : IOnLoad
    {
        private readonly TemplateTable _templates;
        private readonly LocaleTable _locales;
        private readonly ImageRouter _images;
        private readonly JsonUtil _json;

        public TarkovaColaAchievements(TemplateTable templates, LocaleTable locales, ImageRouter images, JsonUtil json)
        {
            _templates = templates;
            _locales = locales;
            _images = images;
            _json = json;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(TarkovaColaAchievements).Assembly.Location) ?? ".", "achievements");
                int index = 15000;
                foreach (var def in AchievementData.All)
                {
                    var file = System.IO.Path.Combine(dir, "tarkovacola_" + def.Key + ".png");
                    if (File.Exists(file)) _images.AddRoute("/files/achievement/tarkovacola_" + def.Key, file);
                    else Console.WriteLine("[Tarkova-Cola] falta la imagen del logro: " + file);

                    var achievement = _json.Deserialize<Achievement>(BuildJson(def, index++));
                    if (achievement == null) { Console.WriteLine("[Tarkova-Cola] no se pudo crear el logro " + def.Key); continue; }
                    _templates.CustomAchievements.Add(achievement);
                }
                AddLocales();
                Console.WriteLine("[Tarkova-Cola] " + AchievementData.All.Length + " logros registrados");
            }
            catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR registrando logros: " + e); }
            return Task.CompletedTask;
        }

        private void AddLocales()
        {
            foreach (var lang in _locales.Languages.Keys)
            {
                if (!_locales.Global.TryGetValue(lang, out var lazy)) continue;
                bool es = lang.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                lazy.AddTransformer(table =>
                {
                    foreach (var def in AchievementData.All)
                    {
                        table[def.Id + " name"] = es ? def.NameEs : def.NameEn;
                        table[def.Id + " description"] = es ? def.DescEs : def.DescEn;
                        table[def.Id + " successMessage"] = "";
                    }
                    return table;
                });
            }
        }

        // La condicion es un contador de bajas contra un rol que no existe: el juego nunca la completa por si solo.
        // El logro lo desbloquea el cliente del mod (UnlockAchievementForced) cuando se cumple lo que describe su texto.
        private static string BuildJson(AchievementData.Def def, int index)
        {
            string NewId() { return new MongoId().ToString(); }

            var rewards = new List<object>();
            foreach (var (tpl, count) in def.Rewards)
            {
                var itemId = NewId();
                rewards.Add(new Dictionary<string, object>
                {
                    ["id"] = NewId(),
                    ["unknown"] = false,
                    ["gameMode"] = new[] { "regular", "pve" },
                    ["availableInGameEditions"] = new string[0],
                    ["illustrationConfig"] = null,
                    ["isHidden"] = false,
                    ["target"] = itemId,
                    ["value"] = count,
                    ["isEncoded"] = false,
                    ["findInRaid"] = true,
                    ["items"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["_id"] = itemId,
                            ["_tpl"] = tpl,
                            ["upd"] = new Dictionary<string, object> { ["SpawnedInSession"] = true, ["StackObjectsCount"] = count },
                        }
                    },
                    ["type"] = "Item",
                });
            }

            var condition = new Dictionary<string, object>
            {
                ["completeInSeconds"] = 0,
                ["conditionType"] = "CounterCreator",
                ["counter"] = new Dictionary<string, object>
                {
                    ["id"] = NewId(),
                    ["conditions"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["bodyPart"] = new string[0],
                            ["compareMethod"] = ">=",
                            ["conditionType"] = "Kills",
                            ["daytime"] = new Dictionary<string, object> { ["from"] = 0, ["to"] = 0 },
                            ["distance"] = new Dictionary<string, object> { ["compareMethod"] = ">=", ["value"] = 0 },
                            ["dynamicLocale"] = false,
                            ["enemyEquipmentExclusive"] = new string[0],
                            ["enemyEquipmentInclusive"] = new string[0],
                            ["enemyHealthEffects"] = new string[0],
                            ["id"] = NewId(),
                            ["resetOnSessionEnd"] = false,
                            ["savageRole"] = new[] { "tarkovacola_never" },
                            ["target"] = "Savage",
                            ["value"] = 1,
                            ["weapon"] = new string[0],
                            ["weaponCaliber"] = new string[0],
                            ["weaponModsExclusive"] = new string[0],
                            ["weaponModsInclusive"] = new string[0],
                        }
                    },
                },
                ["doNotResetIfCounterCompleted"] = false,
                ["dynamicLocale"] = false,
                ["globalQuestCounterId"] = "",
                ["id"] = NewId(),
                ["index"] = 0,
                ["isNecessary"] = false,
                ["isResetOnConditionFailed"] = false,
                ["oneSessionOnly"] = false,
                ["parentId"] = "",
                ["type"] = "Completion",
                ["value"] = 1,
                ["visibilityConditions"] = new string[0],
            };

            var achievement = new Dictionary<string, object>
            {
                ["id"] = def.Id,
                ["imageUrl"] = "/files/achievement/tarkovacola_" + def.Key + ".png",
                ["assetPath"] = "",
                ["rewards"] = rewards,
                ["conditions"] = new Dictionary<string, object> { ["availableForFinish"] = new[] { condition }, ["fail"] = new object[0] },
                ["instantComplete"] = false,
                ["showNotificationsInGame"] = true,
                ["showProgress"] = false,
                ["prefab"] = "",
                ["rarity"] = def.Rarity,
                ["hidden"] = def.Hidden,
                ["showConditions"] = false,
                ["progressBarEnabled"] = false,
                ["side"] = "Pmc",
                ["index"] = index,
            };
            return JsonSerializer.Serialize(achievement);
        }
    }

    // Guarda el logro en el perfil y entrega sus recompensas (una sola vez por logro).
    [Injectable(InjectionType.Singleton)]
    public class AchievementStore
    {
        private readonly ProfileHelper _profiles;
        private readonly RewardHelper _rewards;
        private readonly MailSendService _mail;
        private readonly TemplateTable _templates;
        private readonly SPTarkov.Server.Core.Utils.TimeUtil _time;
        private readonly object _lock = new object();

        public AchievementStore(ProfileHelper profiles, RewardHelper rewards, MailSendService mail, TemplateTable templates, SPTarkov.Server.Core.Utils.TimeUtil time)
        {
            _profiles = profiles;
            _rewards = rewards;
            _mail = mail;
            _templates = templates;
            _time = time;
        }

        // Devuelve los ids (de logro del juego) que se han concedido ahora.
        public List<string> Claim(MongoId sessionId, IEnumerable<string> ids)
        {
            var granted = new List<string>();
            lock (_lock)
            {
                var full = _profiles.GetFullProfile(sessionId);
                var pmc = full?.CharacterData?.PmcData;
                if (pmc == null) return granted;
                if (pmc.Achievements == null) pmc.Achievements = new Dictionary<MongoId, long>();

                foreach (var key in ids.Distinct())
                {
                    var def = AchievementData.All.FirstOrDefault(a => a.Id == key);
                    if (def == null) continue;
                    var id = new MongoId(def.Id);
                    if (!pmc.Achievements.TryAdd(id, _time.GetTimeStamp())) continue;     // ya lo tenia: no se repite la recompensa

                    var template = _templates.Achievements.FirstOrDefault(a => a.Id == id);
                    if (template != null)
                    {
                        var items = _rewards.ApplyRewards(template.Rewards, "achievement", full, pmc, id);
                        if (items != null && items.Count > 0)
                            _mail.SendLocalisedSystemMessageToPlayer(sessionId, "670547bb5fa0b1a7c30d5836 0", items, new List<ProfileChangeEvent>(), _time.GetHoursAsSeconds(168));
                    }
                    granted.Add(def.Id);
                    Console.WriteLine("[Tarkova-Cola] Logro '" + def.Key + "' concedido a " + sessionId);
                }
            }
            return granted;
        }
    }

    public class AchRequest : SPTarkov.Server.Core.Models.Utils.IRequestData
    {
        [JsonPropertyName("ids")] public List<string> Ids { get; set; }
    }
}
