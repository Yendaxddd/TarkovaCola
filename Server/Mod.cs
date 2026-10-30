using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;
using SptRange = SemanticVersioning.Range;
using SptVersion = SemanticVersioning.Version;

namespace TarkovaCola.Server
{
    public class ModMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "com.tarkovacola.server";
        public string Name { get; init; } = "Tarkova-Cola";
        public string Author { get; init; } = "Yendaxddd";
        public List<string> Contributors { get; init; } = new List<string>();
        public SptVersion Version { get; init; } = new SptVersion(3, 0, 0);
        public SptRange SptVersion { get; init; } = new SptRange("~4.1.0");
        public bool HasPrepatcher { get; init; } = false;
        public List<string> Incompatibilities { get; init; } = new List<string>();
        public Dictionary<string, SptRange> ModDependencies { get; init; } = new Dictionary<string, SptRange>();
        public string Url { get; init; } = "";
        public string License { get; init; } = "MIT";
    }

    // Definicion de perks (datos). El cliente tiene su propia lista con los mismos ids (Client/PerkDefs.cs).
    internal class PerkDef
    {
        public string Id;                 // clave de estado (la misma que usa el cliente)
        public string ItemId;             // id fijo del item
        public string TraderItemId;       // id de la oferta en Therapist
        public string ItemName;           // nombre interno del item
        public string NameEn, NameEs, ShortName;
        public string DescEn, DescEs;
        public int GpPrice;               // precio en GP Coin
        public string Bundle;             // ruta del bundle del item (suelo/inventario/icono)
    }

    internal static class Perks
    {
        internal const string TarCola = "57514643245977207f2c2d09";     // lata base que se clona
        internal const string Propital = "5c0e530286f7747fa1419862";    // inyector base (modo inyectores)
        internal static bool InjectorsApplied;                          // como se crearon los objetos en este arranque
        internal const string GpCoin = "5d235b4d86f7742e017bc88a";      // GP Coin
        internal const double GpCoinRoubles = 7500;                     // valor de 1 GP Coin en el handbook

        // acceso directo a Speed Cola (lo usan los logros)
        internal const string SpeedColaId = "6a1c00000000000000000c01";
        internal const string StaminUpId = "6a1c00000000000000000c02";
        internal const string JuggernogId = "6a1c00000000000000000c03";
        internal const string PhdFlopperId = "6a1c00000000000000000c04";
        internal const string WidowsWineId = "6a1c00000000000000000c05";
        internal const string DeadshotId = "6a1c00000000000000000c06";
        internal const string BandolierId = "6a1c00000000000000000c07";
        internal const string VultureAidId = "6a1c00000000000000000c08";
        internal const string MeleeMacchiatoId = "6a1c00000000000000000c09";

        internal static readonly PerkDef SpeedCola = new PerkDef
        {
            Id = "speedcola",
            ItemId = SpeedColaId,
            TraderItemId = "6a1c00000000000000000d01",
            ItemName = "tarkovacola_speedcola",
            NameEn = "Speed Cola", NameEs = "Speed Cola", ShortName = "Speed",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/speedcola.bundle",
            DescEn =
                "Perk-a-cola: A combined taste of some combination of sweet and spicy but doesn't contain any sugar... " +
                "Somehow, drinking this seems to make your body all energetic, and gives you the edge of an ADHD kid with " +
                "unsupervised screen time. Drink it! See what is the worst that could happen!\n\n" +
                "Reload speed x1.5 for the rest of the raid.",
            DescEs =
                "Perk-a-cola: Una mezcla de sabor dulce y picante que no contiene nada de azucar... " +
                "De alguna forma, beberla hace que tu cuerpo se llene de energia y te da la ventaja de un nino con TDAH " +
                "y pantallas sin supervision. Bebela! A ver que es lo peor que puede pasar!\n\n" +
                "Velocidad de recarga x1.5 el resto de la raid.",
        };

        internal static readonly PerkDef StaminUp = new PerkDef
        {
            Id = "staminup",
            ItemId = StaminUpId,
            TraderItemId = "6a1c00000000000000000d02",
            ItemName = "tarkovacola_staminup",
            NameEn = "Stamin-Up", NameEs = "Stamin-Up", ShortName = "Stamin",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/staminup.bundle",
            DescEn =
                "Perk-a-cola: Tastes like cough syrup and ambition, with a hint of lemon that has clearly never met a real lemon. " +
                "Your legs suddenly feel like they were built for running, and whatever you were running from can wait... or maybe it can't. " +
                "Side effects may include: sprinting right past your own squad, forgetting where the extract is, and a firm belief that stairs are optional. " +
                "Drink it! See how far those legs will take you!\n\n" +
                "Leg stamina x1.5 for the rest of the raid.",
            DescEs =
                "Perk-a-cola: Sabe a jarabe para la tos y a ambicion, con un toque de limon que claramente nunca ha conocido un limon de verdad. " +
                "De pronto tus piernas sienten que nacieron para correr, y de lo que huias puede esperar... o quiza no. " +
                "Efectos secundarios: adelantar a tu propio escuadron, olvidar donde esta la extraccion y creer sin motivo que las escaleras son opcionales. " +
                "Bebela! A ver hasta donde te llevan esas piernas!\n\n" +
                "Estamina de piernas x1.5 el resto de la raid.",
        };

        internal static readonly PerkDef Juggernog = new PerkDef
        {
            Id = "juggernog",
            ItemId = JuggernogId,
            TraderItemId = "6a1c00000000000000000d03",
            ItemName = "tarkovacola_juggernog",
            NameEn = "Juggernog", NameEs = "Juggernog", ShortName = "Jugg",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/juggernog.bundle",
            DescEn =
                "Perk-a-cola: Tastes like a cherry-flavored brick wall. Bullets suddenly feel more like rude suggestions, and your skin has started negotiating with them. " +
                "Side effects may include: opening doors with your forehead, ignoring the word \"cover\", and a deep, irrational trust in your own ribcage. " +
                "Drink it! See how many bullets it takes to change your mind!\n\n" +
                "Damage taken -33% for the rest of the raid.",
            DescEs =
                "Perk-a-cola: Sabe a una pared de ladrillos de cereza. De pronto las balas parecen más bien sugerencias groseras, y tu piel ha empezado a negociar con ellas. " +
                "Efectos secundarios: abrir puertas con la frente, ignorar la palabra \"cobertura\" y una confianza profunda e irracional en tu caja torácica. " +
                "¡Bébela! ¡A ver cuántas balas hacen falta para que cambies de opinión!\n\n" +
                "Daño recibido -33% el resto de la raid.",
        };

        internal static readonly PerkDef PhdFlopper = new PerkDef
        {
            Id = "phdflopper",
            ItemId = PhdFlopperId,
            TraderItemId = "6a1c00000000000000000d04",
            ItemName = "tarkovacola_phdflopper",
            NameEn = "PHD Flopper", NameEs = "PHD Flopper", ShortName = "PHD",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/phdflopper.bundle",
            DescEn =
                "Perk-a-cola: Tastes like grape soda and questionable decisions. Gravity is now more of a suggestion, and explosions are just very loud hugs. " +
                "Side effects may include: jumping off things to see what happens, hugging live grenades, and calling bruises \"overrated\". " +
                "Drink it! Flop with confidence!\n\n" +
                "Immune to fall and explosion damage for the rest of the raid.",
            DescEs =
                "Perk-a-cola: Sabe a refresco de uva y a malas decisiones. La gravedad ahora es más bien una sugerencia, y las explosiones son abrazos muy ruidosos. " +
                "Efectos secundarios: saltar desde cualquier sitio para ver qué pasa, abrazar granadas activas y llamar \"exagerados\" a los moretones. " +
                "¡Bébela! ¡A caer con confianza!\n\n" +
                "Inmune al daño por caídas y explosiones el resto de la raid.",
        };

        internal static readonly PerkDef WidowsWine = new PerkDef
        {
            Id = "widowswine",
            ItemId = WidowsWineId,
            TraderItemId = "6a1c00000000000000000d05",
            ItemName = "tarkovacola_widowswine",
            NameEn = "Widow's Wine", NameEs = "Widow's Wine", ShortName = "Widow",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/widowswine.bundle",
            DescEn =
                "Perk-a-cola: Tastes like dark wine, cobwebs and a grudge. The grenades you throw now leave a sticky surprise, and everyone nearby moves like they are wading through honey. " +
                "Side effects may include: spinning webs in your sleep, an unhealthy love of corners, and the sudden urge to say \"come into my parlor\". " +
                "Drink it! Let them come to you!\n\n" +
                "Grenades you throw web the enemies within 10 m: -60% movement speed for 6 seconds.",
            DescEs =
                "Perk-a-cola: Sabe a vino oscuro, telarañas y rencor. Las granadas que lanzas ahora dejan una sorpresa pegajosa, y todos los de alrededor se mueven como si caminaran en miel. " +
                "Efectos secundarios: tejer redes mientras duermes, un amor poco sano por las esquinas y unas ganas repentinas de decir \"pasa a mi salón\". " +
                "¡Bébela! ¡Que vengan a ti!\n\n" +
                "Tus granadas atrapan en telarañas a los enemigos a menos de 10 m: -60% de velocidad de movimiento durante 6 segundos.",
        };

        internal static readonly PerkDef Deadshot = new PerkDef
        {
            Id = "deadshot",
            ItemId = DeadshotId,
            TraderItemId = "6a1c00000000000000000d06",
            ItemName = "tarkovacola_deadshot",
            NameEn = "Deadshot Daiquiri", NameEs = "Deadshot Daiquiri", ShortName = "Deadshot",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/deadshot.bundle",
            DescEn =
                "Perk-a-cola: Tastes like lime, sugar and a very steady trigger finger. Your crosshair suddenly has opinions, and they all point at heads. " +
                "Side effects may include: squinting at everything, counting breaths out loud, and an irresistible urge to call your shots. " +
                "Drink it! Aim small, miss small!\n\n" +
                "You aim 15% faster and weapon sway drops by 25% for the rest of the raid.",
            DescEs =
                "Perk-a-cola: Sabe a lima, azúcar y a un dedo muy firme en el gatillo. Tu punto de mira de repente tiene opiniones, y todas apuntan a cabezas. " +
                "Efectos secundarios: entrecerrar los ojos ante todo, contar las respiraciones en voz alta y unas ganas irresistibles de cantar tus disparos. " +
                "¡Bébela! ¡Apunta pequeño, falla pequeño!\n\n" +
                "Apuntas un 15% más rápido y el balanceo del arma baja un 25% el resto de la raid.",
        };

        internal static readonly PerkDef Bandolier = new PerkDef
        {
            Id = "bandolier",
            ItemId = BandolierId,
            TraderItemId = "6a1c00000000000000000d07",
            ItemName = "tarkovacola_bandolier",
            NameEn = "Bandolier Bandit", NameEs = "Bandolier Bandit", ShortName = "Bandolier",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/bandolier.bundle",
            DescEn =
                "Perk-a-cola: Tastes like gunpowder, leather and a pocketful of spare change that turned out to be bullets. " +
                "Your magazines suddenly feel lonely when they are not full, and your fingers know exactly where every loose round is hiding. " +
                "Side effects may include: jingling when you walk, counting rounds in your sleep, and hugging ammo crates a little too long. " +
                "Drink it! Never run dry again!\n\n" +
                "Magazines in your rig and pockets refill 1 round every 2 seconds from loose ammo you carry, while you are not shooting.",
            DescEs =
                "Perk-a-cola: Sabe a pólvora, cuero y a un bolsillo lleno de monedas que resultaron ser balas. " +
                "De pronto tus cargadores se sienten solos cuando no están llenos, y tus dedos saben dónde se esconde cada bala suelta. " +
                "Efectos secundarios: tintinear al caminar, contar balas en sueños y abrazar cajas de munición un poco más de la cuenta. " +
                "¡Bébela! ¡No te quedes seco nunca más!\n\n" +
                "Los cargadores del chaleco y los bolsillos se rellenan 1 bala cada 2 segundos con la munición suelta que llevas, mientras no disparas.",
        };

        internal static readonly PerkDef VultureAid = new PerkDef
        {
            Id = "vultureaid",
            ItemId = VultureAidId,
            TraderItemId = "6a1c00000000000000000d08",
            ItemName = "tarkovacola_vultureaid",
            NameEn = "Vulture Aid Elixir", NameEs = "Vulture Aid Elixir", ShortName = "Vulture",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/vultureaid.bundle",
            DescEn =
                "Perk-a-cola: Tastes like old coins, wet cardboard and somebody else's lunch. You can smell a stash through three walls and a locked door. " +
                "Side effects may include: circling bodies before they hit the ground, an unhealthy interest in jackets, and calling everything \"mine\". " +
                "Drink it! One man's corpse is another man's treasure!\n\n" +
                "Unsearched containers and bodies with valuables glow through walls within 15 m for the rest of the raid.",
            DescEs =
                "Perk-a-cola: Sabe a monedas viejas, cartón mojado y al almuerzo de otro. Hueles un alijo a través de tres paredes y una puerta cerrada. " +
                "Efectos secundarios: dar vueltas alrededor de los cuerpos antes de que caigan, un interés poco sano por las chaquetas y llamar \"mío\" a todo. " +
                "¡Bébela! ¡El cadáver de uno es el tesoro de otro!\n\n" +
                "Los contenedores y cuerpos sin registrar con objetos de valor brillan a través de las paredes a menos de 15 m el resto de la raid.",
        };

        internal static readonly PerkDef MeleeMacchiato = new PerkDef
        {
            Id = "meleemacchiato",
            ItemId = MeleeMacchiatoId,
            TraderItemId = "6a1c00000000000000000d09",
            ItemName = "tarkovacola_meleemacchiato",
            NameEn = "Melee Macchiato", NameEs = "Melee Macchiato", ShortName = "Macchiato",
            GpPrice = 7,
            Bundle = "assets/content/items/consumables/tarkovacola/meleemacchiato.bundle",
            DescEn =
                "Perk-a-cola: Tastes like burnt espresso, brass knuckles and a bad attitude. Guns suddenly feel like a coward's hobby. " +
                "Side effects may include: bringing a knife to a gunfight on purpose, punching doors open, and growling at people who reload. " +
                "Drink it! Get up close and personal!\n\n" +
                "Melee damage x2 and melee attacks 30% faster for the rest of the raid.",
            DescEs =
                "Perk-a-cola: Sabe a espresso quemado, nudilleras y mala actitud. De pronto las armas de fuego parecen un pasatiempo de cobardes. " +
                "Efectos secundarios: llevar un cuchillo a un tiroteo a propósito, abrir puertas a puñetazos y gruñir a quien recarga. " +
                "¡Bébela! ¡De cerca y en persona!\n\n" +
                "Daño cuerpo a cuerpo x2 y golpes un 30% más rápidos el resto de la raid.",
        };

        internal static readonly PerkDef[] All = { SpeedCola, StaminUp, Juggernog, PhdFlopper, WidowsWine, Deadshot, Bandolier, VultureAid, MeleeMacchiato };
    }

    // Fase 1: crea el item Speed Cola (clon de TarCola) y lo vende en Therapist (LL1).
    [Injectable(TypePriority = OnLoadOrder.Preload)]
    public class TarkovaColaMod : IOnLoad
    {
        private readonly CustomItemService _items;
        private readonly TemplateTable _templates;
        private readonly TradersTable _traders;

        public TarkovaColaMod(CustomItemService items, TemplateTable templates, TradersTable traders)
        {
            _items = items;
            _templates = templates;
            _traders = traders;
        }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            Perks.InjectorsApplied = Settings.Current.Injectors;
            if (Perks.InjectorsApplied) Console.WriteLine("[Tarkova-Cola] modo inyectores: las perks se crean como estimulantes");
            foreach (var perk in Perks.All)
            {
                try { CreatePerk(perk); }
                catch (Exception e) { Console.WriteLine("[Tarkova-Cola] ERROR creando " + perk.Id + ": " + e); }
            }
            return Task.CompletedTask;
        }

        // Crea el item de una perk (clon de TarCola, o del Propital en modo inyectores) y lo pone en venta en Therapist (LL1).
        // El id no cambia entre modos: las perks que ya tengas en el alijo pasan a ser inyectores (o latas) al reiniciar.
        private void CreatePerk(PerkDef perk)
        {
            bool inj = Perks.InjectorsApplied;
            string baseTpl = inj ? Perks.Propital : Perks.TarCola;
            var baseItem = _templates.Items[new MongoId(baseTpl)];
            double roubles = perk.GpPrice * Perks.GpCoinRoubles;
            var props = inj
                ? new TemplateItemProperties
                {
                    Width = 1,
                    Height = 1,
                    // un estimulante sin efectos propios: lo que hace lo pone el cliente al usarlo (igual que la lata)
                    StimulatorBuffs = "",
                    EffectsHealth = new Dictionary<HealthFactor, EffectsHealthProperties>(),
                    EffectsDamage = new Dictionary<DamageEffectType, EffectsDamageProperties>(),
                    Prefab = new Prefab { Path = perk.Bundle.Replace(".bundle", "_inj.bundle"), Rcid = "" },
                }
                : new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,      // ocupa 1x2 casillas en el inventario (la TarCola original es 1x1)
                    EffectsHealth = BuildEffects(baseItem.Properties.EffectsHealth, hydration: 50, energy: 0),
                    // Modelo 3D propio (ground/inventario/icono). UsePrefab (en mano) sigue siendo el de la TarCola.
                    Prefab = new Prefab { Path = perk.Bundle, Rcid = "" },
                };
            _items.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = new MongoId(baseTpl),
                ParentId = baseItem.Parent,
                NewId = new MongoId(perk.ItemId),
                NewItemName = perk.ItemName,
                OverrideProperties = props,
                FleaPriceRoubles = roubles,
                HandbookPriceRoubles = roubles,
                HandbookParentId = inj ? "5b47574386f77428ca22b33a" : "5b47574386f77428ca22b335", // handbook: Stimulants / Drinks
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails { Name = perk.NameEn, ShortName = perk.ShortName, Description = perk.DescEn },
                    ["es"] = new LocaleDetails { Name = perk.NameEs, ShortName = perk.ShortName, Description = perk.DescEs },
                },
            });

            var created = _templates.Items[new MongoId(perk.ItemId)].Properties;
            Console.WriteLine($"[Tarkova-Cola] {perk.Id}: prefab={created?.Prefab?.Path} peso={created?.Weight}");
            AddToTherapist(perk);
            Console.WriteLine($"[Tarkova-Cola] {perk.NameEn} creada y en venta en Therapist (LL1, {perk.GpPrice} GP Coin)");
        }

        // Copia los efectos de la lata base y sustituye solo el valor de hidratacion y energia.
        private static Dictionary<HealthFactor, EffectsHealthProperties> BuildEffects(
            Dictionary<HealthFactor, EffectsHealthProperties> baseEffects, double hydration, double energy)
        {
            var result = new Dictionary<HealthFactor, EffectsHealthProperties>();
            if (baseEffects != null)
                foreach (var kv in baseEffects)
                    result[kv.Key] = kv.Value with { };
            result[HealthFactor.Hydration] = (result.TryGetValue(HealthFactor.Hydration, out var h) ? h : new EffectsHealthProperties()) with { Value = hydration };
            result[HealthFactor.Energy] = (result.TryGetValue(HealthFactor.Energy, out var e) ? e : new EffectsHealthProperties()) with { Value = energy };
            return result;
        }

        private void AddToTherapist(PerkDef perk)
        {
            var assort = _traders[Traders.THERAPIST].Assort;
            var id = new MongoId(perk.TraderItemId);
            assort.Items.Add(new Item
            {
                Id = id,
                Template = new MongoId(perk.ItemId),
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd { StackObjectsCount = 999999, UnlimitedCount = true },
            });
            assort.BarterScheme[id] = new List<List<BarterScheme>>
            {
                new List<BarterScheme> { new BarterScheme { Count = perk.GpPrice, Template = new MongoId(Perks.GpCoin) } },
            };
            assort.LoyalLevelItems[id] = 1;
        }
    }
}
