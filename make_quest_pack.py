"""Genera la historia de Tarkova-Cola (misiones para VisitAPI) y los datos que usa el servidor.

Salida:
  Server/Packs/TarkovaCola/      pack de VisitAPI: pack.json, quests, locales (en/es), zones, spawns
                                 (el build del servidor lo copia a user/mods/VisitAPI-Server/packs/TarkovaCola/)
  Server/ModFiles/story.json     objetos de mision, recetas del hideout y ventas de Therapist que desbloquea cada mision

Historia: los Embotelladores (ex-PMC que se han hecho con las perk-a-colas) y su jefe, el Barman.
  1-3  introduccion (Skier y Therapist): desbloquean las RECETAS de las perks en la Unidad de nutricion
  4-9  una mision por perk (Therapist): desbloquea la VENTA de esa perk en Therapist
  10   final (Skier): el Barman, con TODAS las perks (4 en modo Classic)
Los puntos de los guardias salen de los puntos de aparicion de bots de la base de datos de SPT (son suelo caminable), salvo el de
la mision 1, que se coloco a mano con la tecla F10.
"""
import json, pathlib, shutil

ROOT = pathlib.Path(__file__).parent
OUT = ROOT / "Server" / "Packs" / "TarkovaCola"
STORY = ROOT / "Server" / "ModFiles" / "story.json"
SPT_DB = ROOT.parent / "SPT_Runtime" / "SPT_Data" / "database"

SKIER, THERAPIST = "58330581ace78e27b8b10cee", "54cb57776803fa99248b456e"
ROUBLES, GP = "5449016a4bdc2d6f028b456f", "5d235b4d86f7742e017bc88a"
NUTRITION = 8                                                     # Unidad de nutricion del hideout
TARCOLA = "57514643245977207f2c2d09"
PERK_ITEM = {"speedcola": "6a1c00000000000000000c01", "staminup": "6a1c00000000000000000c02", "juggernog": "6a1c00000000000000000c03",
             "phdflopper": "6a1c00000000000000000c04", "widowswine": "6a1c00000000000000000c05", "deadshot": "6a1c00000000000000000c06"}
PERK_OFFER = {k: v[:-3] + "d" + v[-2:] for k, v in PERK_ITEM.items()}      # 6a1c...d01..d06 (ofertas de Therapist)
PERK_NAME = {"speedcola": "Speed Cola", "staminup": "Stamin-Up", "juggernog": "Juggernog", "phdflopper": "PHD Flopper",
             "widowswine": "Widow's Wine", "deadshot": "Deadshot Daiquiri"}


def oid(kind, q, j=0):
    """id de 24 hex unico: 6a1c + ceros + tipo + mision + indice"""
    return "6a1c" + "0" * 14 + "%02x%02x%02x" % (kind, q, j)


QUEST, COND, REWARD, RITEM, VIS, RECIPE = 0x51, 0x52, 0x53, 0x54, 0x55, 0x56


def post_of(map_dir, zone, nth=0):
    """posicion de un punto de aparicion de bots de la zona (suelo caminable)"""
    b = json.loads((SPT_DB / "locations" / map_dir / "base.json").read_text(encoding="utf-8"))
    pts = [p for p in b["SpawnPointParams"] if p.get("BotZoneName") == zone and "Bot" in p.get("Categories", [])]
    p = pts[min(nth, len(pts) - 1)]["Position"]
    return b["Id"], {"x": round(p["x"], 2), "y": round(p["y"], 2), "z": round(p["z"], 2)}


# ====================================================================================== misiones
# texto: en / es. Campos: name, desc (descripcion larga), start (mensaje al aceptar), success (mensaje al completar), fail,
#   visit / find / hand (objetivos), item (objeto que lleva un guardia: nombre corto, nombre, descripcion)
M = []

M.append(dict(
    n=1, id="6a1c00000000000000000e01", trader=SKIER, level=12, after=[],
    map="RezervBase", at={"x": 59.51, "y": -6.70, "z": 65.68},
    role="pmcBEAR", guards=4, perks="random",
    item_id="6a1c00000000000000000f01",
    cond_ids={"find": "6a1c00000000000000000e12", "hand": "6a1c00000000000000000e13", "level": "6a1c00000000000000000e11"},
    recipes=["speedcola", "staminup"], xp=6000, money=120000, rep=0.03,
    en=dict(
        name="Cold Cargo",
        desc="Listen. There is a new crew in town and they call themselves the Bottlers. Ex-PMCs, both sides, the kind who stopped caring about flags the day the money stopped coming. "
             "They got their hands on a shipment of very strange sodas. One can makes you reload like a machine, another lets you run all day... I thought it was a fairy tale, until I saw one of them jump off a roof and walk away whistling.\n\n"
             "They have dug in at the Reserve base, by the old rail depot. One of them carries an envelope that belongs to me: papers from a deal that went sour. "
             "Find their post, take the envelope off them and bring it here. And be careful, friend: whatever they drink, it is real.",
        start="The Bottlers are at Reserve. Four of them, each one with a bottle in the belly. Do not let them land the first shot.",
        success="So it is true... these papers are not mine at all. Recipes, numbers, a stamp from a lab nobody talks about. "
                "Keep this between us. Take your money, and take this: with the right ingredients you can brew a couple of those sodas yourself, in your own hideout.",
        fail="",
        visit="Locate the Bottlers' post at Reserve",
        find="Find the sealed envelope",
        hand="Hand over the sealed envelope to Skier",
        item=("Envelope", "Sealed envelope", "A sealed envelope with no sender. It smells faintly of cola, and something inside rattles like a bottle cap."),
    ),
    es=dict(
        name="Carga fría",
        desc="Escucha. Hay un grupo nuevo en la ciudad y se hacen llamar los Embotelladores. Ex-PMC, de los dos bandos, de los que dejaron de creer en banderas el día que dejó de llegar el dinero. "
             "Se han hecho con un cargamento de refrescos muy raros. Una lata te hace recargar como una máquina, otra te deja correr todo el día... Pensé que era un cuento, hasta que vi a uno de ellos tirarse desde un tejado y marcharse silbando.\n\n"
             "Se han atrincherado en la base Reserva, junto al viejo depósito ferroviario. Uno de ellos lleva un sobre que es mío: papeles de un trato que salió mal. "
             "Encuentra su puesto, quítales el sobre y tráemelo. Y ten cuidado, amigo: lo que beben es de verdad.",
        start="Los Embotelladores están en Reserva. Son cuatro, y cada uno lleva una botella en la tripa. No les dejes disparar primero.",
        success="Así que era verdad... estos papeles no son míos. Recetas, cifras, el sello de un laboratorio del que nadie habla. "
                "Que quede entre nosotros. Toma tu dinero, y toma esto: con los ingredientes adecuados puedes preparar un par de esos refrescos tú mismo, en tu propio refugio.",
        fail="",
        visit="Localiza el puesto de los Embotelladores en Reserva",
        find="Encuentra el sobre sellado",
        hand="Entrega el sobre sellado a Skier",
        item=("Sobre", "Sobre sellado", "Un sobre sellado y sin remitente. Huele un poco a cola, y dentro algo suena como una chapa de botella."),
    ),
))

M.append(dict(
    n=2, trader=THERAPIST, level=13, after=[1],
    zone=("shoreline", "ZoneSanatorium1"), role="pmcUSEC", guards=4, perks="random",
    recipes=["juggernog", "phdflopper"], xp=8000, money=150000, rep=0.03,
    en=dict(
        name="The Formula",
        desc="Skier came to me with a pile of papers he did not understand. I do. These are production notes, and the stamp belongs to the old research wing of the Health Resort on the shore. "
             "Someone there was trying to make soldiers who do not tire, do not bleed, do not fall. They called the drinks perks. Ridiculous name, brilliant chemistry.\n\n"
             "The Bottlers went back to the resort to take what was left. Their courier carries a laboratory notebook with the rest of the formula. "
             "I want that notebook. Not for them, not for Skier: for the people this city keeps breaking. Bring it to me and I will teach you two more recipes.",
        start="The Bottlers are inside the Health Resort, west wing. The courier with the notebook will not leave his friends' side.",
        success="Look at these annotations... dosage, half-life, side effects. Somebody wrote 'subject flew off the balcony, laughed' in the margin. "
                "This is enough to brew Juggernog and PHD Flopper safely. The recipes are yours; use the Nutrition Unit.",
        fail="",
        visit="Locate the Bottlers inside the Health Resort",
        find="Find the laboratory notebook",
        hand="Hand over the laboratory notebook to Therapist",
        item=("Notebook", "Laboratory notebook", "A water-stained notebook full of formulas, dosages and a coffee ring shaped like a bottle cap."),
    ),
    es=dict(
        name="La fórmula",
        desc="Skier vino a verme con un montón de papeles que no entendía. Yo sí. Son notas de producción, y el sello es del antiguo pabellón de investigación del sanatorio de la costa. "
             "Alguien allí intentaba crear soldados que no se cansan, no sangran y no caen. A las bebidas las llamaban perks. Un nombre ridículo para una química brillante.\n\n"
             "Los Embotelladores han vuelto al sanatorio a llevarse lo que quedaba. Su mensajero lleva un cuaderno de laboratorio con el resto de la fórmula. "
             "Quiero ese cuaderno. No para ellos, ni para Skier: para la gente que esta ciudad sigue rompiendo. Tráemelo y te enseñaré dos recetas más.",
        start="Los Embotelladores están dentro del sanatorio, en el ala oeste. El mensajero del cuaderno no se separa de sus compañeros.",
        success="Mira estas anotaciones... dosis, vida media, efectos secundarios. Alguien escribió en el margen: 'el sujeto salió volando del balcón, se reía'. "
                "Con esto se pueden preparar Juggernog y PHD Flopper sin riesgos. Las recetas son tuyas; usa la Unidad de nutrición.",
        fail="",
        visit="Localiza a los Embotelladores dentro del sanatorio",
        find="Encuentra el cuaderno de laboratorio",
        hand="Entrega el cuaderno de laboratorio a Therapist",
        item=("Cuaderno", "Cuaderno de laboratorio", "Un cuaderno manchado de agua, lleno de fórmulas, dosis y un cerco de café con forma de chapa."),
    ),
))

M.append(dict(
    n=3, trader=THERAPIST, level=14, after=[2],
    zone=("bigmap", "ZoneFactoryCenter"), role="pmcBEAR", guards=4, perks="random",
    handover_extra=[("590a3b0486f7743954552bdb", 1), ("5d1b39a386f774252339976f", 2), ("5733279d245977289b77ec24", 1)],
    recipes=["widowswine", "deadshot"], xp=9000, money=160000, rep=0.03,
    en=dict(
        name="Bottling Line",
        desc="A formula is useless without a way to bottle it. The Bottlers do not brew by hand: they run a small bottling line in the Customs factory, and the schematics travel with one of their men. "
             "Get me those schematics. While you are at it, I need parts to build our own line: a printed circuit board, two silicone tubes and a car battery.\n\n"
             "When this is done, the city will have its sodas without having to beg those thugs for them.",
        start="The Bottlers' line is at the Customs factory. Bring the schematics and the parts, in any order.",
        success="Circuit board, tubes, battery and the schematics... yes. The line works. "
                "Now we can bottle the last two recipes: Widow's Wine and Deadshot Daiquiri. And now that the Bottlers know who you are, they will be waiting for you.",
        fail="",
        visit="Locate the Bottlers' bottling line at the Customs factory",
        find="Find the bottling schematics",
        hand="Hand over the bottling schematics to Therapist",
        extra=["Hand over a printed circuit board", "Hand over 2 silicone tubes", "Hand over a car battery"],
        item=("Schematics", "Bottling schematics", "Folded blueprints of a small bottling line. Someone drew a skull with a straw in the corner."),
    ),
    es=dict(
        name="Línea de embotellado",
        desc="Una fórmula no sirve de nada sin una forma de embotellarla. Los Embotelladores no preparan nada a mano: tienen una pequeña línea de embotellado en la fábrica de Aduanas, y los esquemas viajan con uno de sus hombres. "
             "Consígueme esos esquemas. Y ya que estás, necesito piezas para montar nuestra propia línea: una placa de circuito impreso, dos tubos de silicona y una batería de coche.\n\n"
             "Cuando esto esté hecho, la ciudad tendrá sus refrescos sin tener que suplicárselos a esos matones.",
        start="La línea de los Embotelladores está en la fábrica de Aduanas. Trae los esquemas y las piezas, en el orden que quieras.",
        success="Placa, tubos, batería y los esquemas... sí. La línea funciona. "
                "Ahora podemos embotellar las dos últimas recetas: Widow's Wine y Deadshot Daiquiri. Y ahora que los Embotelladores saben quién eres, te estarán esperando.",
        fail="",
        visit="Localiza la línea de embotellado en la fábrica de Aduanas",
        find="Encuentra los esquemas de embotellado",
        hand="Entrega los esquemas de embotellado a Therapist",
        extra=["Entrega una placa de circuito impreso", "Entrega 2 tubos de silicona", "Entrega una batería de coche"],
        item=("Esquemas", "Esquemas de embotellado", "Planos doblados de una pequeña línea de embotellado. Alguien dibujó una calavera con pajita en una esquina."),
    ),
))

PERK_MISSIONS = [
    # n, perk, mapa/zona, rol, nivel, (en name, es name), objeto (en corto, en, es corto, es)
    (4, "speedcola", ("interchange", "ZoneOLI"), "pmcUSEC", 15, ("Fast Hands", "Manos rápidas"),
     ("Manifest", "Speed Cola shipment manifest", "Manifiesto", "Manifiesto de envío de Speed Cola", "el manifiesto de envío de Speed Cola"),
     ("The Bottlers who drink Speed Cola reload faster than you can blink. A squad of them is unloading crates in the OLI warehouse at Interchange, and their shipment manifest says where every can of Speed Cola goes.",
      "Los Embotelladores que beben Speed Cola recargan más rápido de lo que tardas en parpadear. Un escuadrón descarga cajas en el almacén de OLI, en Intercambio, y su manifiesto dice adónde va cada lata de Speed Cola."),
     ("Expect long bursts and very short reloads. Fight them at range, or do not fight them at all.",
      "Espera ráfagas largas y recargas muy cortas. Pelea a distancia, o no pelees.")),
    (5, "staminup", ("woods", "ZoneWoodCutter"), "pmcBEAR", 16, ("Long Haul", "Fondo"),
     ("Route map", "Stamin-Up route map", "Mapa de rutas", "Mapa de rutas de Stamin-Up", "el mapa de rutas de Stamin-Up"),
     ("Stamin-Up turns a tired soldier into a marathon runner. The Bottlers carry it through the Woods, to the lumber mill, on foot, so nobody can track their trucks. Their route map shows every stash on the way.",
      "Stamin-Up convierte a un soldado agotado en un corredor de maratón. Los Embotelladores la llevan a pie por el Bosque, hasta el aserradero, para que nadie siga sus camiones. Su mapa de rutas marca cada escondite del camino."),
     ("They will run circles around you. Do not chase them through the trees: let them come.",
      "Te darán vueltas corriendo. No los persigas entre los árboles: deja que vengan.")),
    (6, "juggernog", ("tarkovstreets", "ZoneConcordia_1"), "pmcUSEC", 18, ("Meat Wall", "Muro de carne"),
     ("Sample", "Juggernog lab sample", "Muestra", "Muestra de laboratorio de Juggernog", "la muestra de laboratorio de Juggernog"),
     ("Juggernog makes a man take three bullets and keep walking. The Bottlers are testing a stronger batch in the Concordia apartments, on the Streets of Tarkov, and one of them carries a sealed sample.",
      "Juggernog hace que un hombre se coma tres balas y siga andando. Los Embotelladores prueban un lote más fuerte en los apartamentos Concordia, en las Calles de Tarkov, y uno de ellos lleva una muestra sellada."),
     ("Aim for the head and bring more ammunition than you think you need.",
      "Apunta a la cabeza y lleva más munición de la que crees que necesitas.")),
    (7, "phdflopper", ("lighthouse", "Zone_Chalet"), "pmcBEAR", 20, ("Zero Gravity", "Gravedad cero"),
     ("Test log", "PHD Flopper test log", "Registro", "Registro de pruebas de PHD Flopper", "el registro de pruebas de PHD Flopper"),
     ("PHD Flopper laughs at falls and explosions. The Bottlers keep their test log in the chalet by the Lighthouse, where they throw volunteers off the cliffs to see who gets back up.",
      "PHD Flopper se ríe de las caídas y de las explosiones. Los Embotelladores guardan su registro de pruebas en el chalé del Faro, donde tiran voluntarios por los acantilados para ver quién se levanta."),
     ("Grenades will not help you here. They will shrug them off.",
      "Las granadas no te servirán aquí. Se las sacuden de encima.")),
    (8, "widowswine", ("shoreline", "ZonePowerStation"), "pmcUSEC", 22, ("Black Widow", "Viuda negra"),
     ("Canister", "Widow's Wine web canister", "Bote", "Bote de telaraña de Widow's Wine", "el bote de telaraña de Widow's Wine"),
     ("Widow's Wine turns every hit into a trap. The Bottlers hide their web canisters at the power station on the shore. One canister is enough to synthesize the drink for good.",
      "Widow's Wine convierte cada golpe en una trampa. Los Embotelladores esconden sus botes de telaraña en la central eléctrica de la costa. Basta un bote para sintetizar la bebida para siempre."),
     ("If they hit you, you will feel your legs turn to glue. Keep your distance and keep moving.",
      "Si te dan, sentirás las piernas pegadas al suelo. Mantén la distancia y no dejes de moverte.")),
    (9, "deadshot", ("bigmap", "ZoneDormitory"), "pmcBEAR", 24, ("Hawkeye", "Ojo de halcón"),
     ("Scope log", "Deadshot Daiquiri scope log", "Registro de mira", "Registro de mira de Deadshot Daiquiri", "el registro de mira de Deadshot Daiquiri"),
     ("Deadshot Daiquiri steadies the hand and the eye. The Bottlers' marksmen have taken the dorms at Customs, and their scope log holds the last piece of the recipe.",
      "Deadshot Daiquiri le da pulso firme a la mano y al ojo. Los tiradores de los Embotelladores han tomado los dormitorios de Aduanas, y su registro de mira guarda la última pieza de la receta."),
     ("Their bullets go through armor like paper. Do not trust your plate to save you.",
      "Sus balas atraviesan la armadura como si fuera papel. No confíes en que tu placa te salve.")),
]

for (n, perk, zone, role, lvl, names, item, story, tip) in PERK_MISSIONS:
    nm = PERK_NAME[perk]
    M.append(dict(
        n=n, trader=THERAPIST, level=lvl, after=[3],
        zone=zone, role=role, guards=4, perks=[perk],
        sale=perk, xp=10000 + (n - 4) * 1500, money=180000 + (n - 4) * 20000, rep=0.04,
        en=dict(
            name=names[0],
            desc=story[0] + "\n\n"
                 "Every Bottler in that squad has " + nm + " in the blood, and it shows. Bring me what their courier carries and I will finally be able to sell " + nm + " myself, "
                 "fresh and safe, to anyone who needs it.",
            start=tip[0],
            success="This is it. With this I can bottle " + nm + " in my own clinic. From today you will find it on my shelf. Thank you, and drink responsibly.",
            fail="",
            visit="Locate the Bottlers' " + nm + " squad",
            find="Find the " + item[1],
            hand="Hand over the " + item[1] + " to Therapist",
            item=(item[0], item[1], "Property of the Bottlers. Contents: everything needed to make " + nm + "."),
        ),
        es=dict(
            name=names[1],
            desc=story[1] + "\n\n"
                 "Todos los Embotelladores de ese escuadrón llevan " + nm + " en la sangre, y se nota. Tráeme lo que lleva su mensajero y por fin podré vender " + nm + " yo misma, "
                 "fresca y segura, a quien la necesite.",
            start=tip[1],
            success="Esto es. Con esto puedo embotellar " + nm + " en mi propia clínica. Desde hoy la tendrás en mi estante. Gracias, y bebe con moderación.",
            fail="",
            visit="Localiza al escuadrón de " + nm + " de los Embotelladores",
            find="Encuentra " + item[4],
            hand="Entrega " + item[4] + " a Therapist",
            item=(item[2], item[3], "Propiedad de los Embotelladores. Contiene todo lo necesario para preparar " + nm + "."),
        ),
    ))

M.append(dict(
    n=10, trader=SKIER, level=28, after=[4, 5, 6, 7, 8, 9],
    zone=("rezervbase", "ZoneSubCommand"), role="pmcUSEC", guards=3, perks="random",
    boss=dict(role="pmcUSEC", difficulty="hard", perks="all"),
    xp=30000, money=500000, rep=0.08, final=True,
    en=dict(
        name="Last Call",
        desc="You have been busy, friend. Therapist sells the sodas now, the Bottlers are bleeding, and their boss is not happy. "
             "They call him the Bartender. He drinks every perk they ever brewed, all at once, and he has holed up in the underground command post at Reserve with his last loyal men.\n\n"
             "He keeps the master formula on him, the original, the one everything else was copied from. Kill him, take it, and bring it to me. "
             "After that, the Bottlers are a story people tell around barrels. And you will be the one who ended it.",
        start="The Bartender is in the Reserve command bunker. Every perk at once: he hits hard, runs fast, takes a beating and does not care about grenades. Bring friends, or a plan.",
        success="The master formula. Hah! I will sell copies to everyone and nobody will ever corner this market again. "
                "Here, a little thank-you from the both of us: a crate of every soda and enough coins to make the Bartender cry, if he still could.",
        fail="",
        visit="Locate the Bartender's hideout at Reserve",
        find="Find the master formula",
        hand="Hand over the master formula to Skier",
        item=("Formula", "Master formula", "The original perk-a-cola formula, signed by a doctor whose name has been scratched out. It is still sticky."),
    ),
    es=dict(
        name="Última ronda",
        desc="Has estado ocupado, amigo. Therapist ya vende los refrescos, los Embotelladores se desangran y su jefe no está contento. "
             "Le llaman el Barman. Bebe todas las perks que han preparado, todas a la vez, y se ha encerrado en el puesto de mando subterráneo de Reserva con sus últimos hombres leales.\n\n"
             "Lleva encima la fórmula maestra, la original, de la que salieron todas las copias. Mátalo, quítasela y tráemela. "
             "Después de eso, los Embotelladores serán una historia que se cuenta alrededor de un barril. Y tú serás quien le puso fin.",
        start="El Barman está en el búnker de mando de Reserva. Todas las perks a la vez: pega fuerte, corre rápido, aguanta palizas y se ríe de las granadas. Lleva amigos, o un plan.",
        success="La fórmula maestra. ¡Ja! Venderé copias a todo el mundo y nadie volverá a acaparar este mercado. "
                "Toma, un pequeño agradecimiento de parte de los dos: una caja con cada refresco y suficientes monedas para hacer llorar al Barman, si todavía pudiera.",
        fail="",
        visit="Localiza el escondite del Barman en Reserva",
        find="Encuentra la fórmula maestra",
        hand="Entrega la fórmula maestra a Skier",
        item=("Fórmula", "Fórmula maestra", "La fórmula original de las perk-a-colas, firmada por un doctor cuyo nombre está tachado. Todavía está pegajosa."),
    ),
))

# ====================================================================================== recetas (Unidad de nutricion)
RECIPES = {
    "speedcola":  [(TARCOLA, 1), ("5751496424597720a27126da", 1), ("59e3577886f774176a362503", 1)],     # TarCola + Hot Rod + azucar
    "staminup":   [(TARCOLA, 1), ("5751435d24597720a27126d1", 1), ("57513f07245977207e26a311", 1)],     # + Max Energy + zumo de manzana
    "juggernog":  [(TARCOLA, 1), ("57513f9324597720a7128161", 1), ("5e831507ea0a7c419c2f9bd9", 1)],     # + zumo Grand + torniquete
    "phdflopper": [(TARCOLA, 1), ("57513fcc24597720a31c09a6", 1), ("590c5a7286f7747884343aea", 1)],     # + zumo Vita + polvora Kite
    "widowswine": [(TARCOLA, 1), ("5d40407c86f774318526545a", 1), ("5e2af2bc86f7746d3f3c33fc", 1)],     # + vodka + cerillas
    "deadshot":   [(TARCOLA, 1), ("5d403f9186f7743cac3f229b", 1), ("59e3577886f774176a362503", 1)],     # + whisky + azucar
}
RECIPE_TIME = {"speedcola": 2700, "staminup": 2700, "juggernog": 3600, "phdflopper": 3600, "widowswine": 3600, "deadshot": 3600}
PERK_INDEX = {k: i + 1 for i, k in enumerate(PERK_ITEM)}


def recipe_id(perk): return oid(RECIPE, 0, PERK_INDEX[perk])


# ====================================================================================== construccion
def cond_base(cid, index, vis=None):
    return {"id": cid, "index": index, "parentId": "", "dynamicLocale": False,
            "visibilityConditions": vis or [], "globalQuestCounterId": ""}


def visible_after(m, cond_id, j):
    return [{"id": oid(VIS, m["n"], j), "target": cond_id, "conditionType": "CompleteCondition"}]


quests, locales, zones, spawns, story_items = {}, {"en": {}, "es": {}}, [], [], []
qid = {m["n"]: m.get("id") or oid(QUEST, m["n"]) for m in M}

for m in M:
    n, q = m["n"], qid[m["n"]]
    if "zone" in m: m["map"], m["at"] = post_of(*m["zone"])
    ids = m.get("cond_ids", {})
    c_level = ids.get("level", oid(COND, n, 1))
    c_visit = oid(COND, n, 2)
    c_visit_inner = oid(COND, n, 3)
    c_find = ids.get("find", oid(COND, n, 4))
    c_hand = ids.get("hand", oid(COND, n, 5))
    c_extra = [oid(COND, n, 0x10 + i) for i in range(len(m.get("handover_extra", [])))]
    item_id = m.get("item_id", "6a1c00000000000000000f%02x" % n)
    zone_id = "tarkovacola_m%02d_post" % n

    start = [dict(cond_base(c_level, 0), isEncoded=False, compareMethod=">=", value=m["level"], conditionType="Level")]
    for k, prev in enumerate(m["after"]):
        start.append(dict(cond_base(oid(COND, n, 0x20 + k), k + 1), target=qid[prev], status=[4], availableAfter=0, dispersion=0, conditionType="Quest"))

    finish = [
        dict(cond_base(c_visit, 0), value=1, type="Exploration", oneSessionOnly=False, completeInSeconds=0, doNotResetIfCounterCompleted=False,
             isResetOnConditionFailed=False, isNecessary=False, isNotGroupProgress=False,
             counter={"id": oid(COND, n, 6), "conditions": [{"id": c_visit_inner, "dynamicLocale": False, "target": zone_id, "value": 1, "conditionType": "VisitPlace"}]},
             conditionType="CounterCreator"),
        dict(cond_base(c_find, 1, visible_after(m, c_visit, 1)), target=[item_id], value=1, minDurability=0, maxDurability=100, dogtagLevel=0,
             onlyFoundInRaid=True, isEncoded=False, countInRaid=False, includeEquipment=False, conditionType="FindItem"),
        dict(cond_base(c_hand, 2, visible_after(m, c_find, 2)), target=[item_id], value=1, minDurability=0, maxDurability=100, dogtagLevel=0,
             onlyFoundInRaid=True, isEncoded=False, includeEquipment=False, conditionType="HandoverItem"),
    ]
    for i, (tpl, count) in enumerate(m.get("handover_extra", [])):
        finish.append(dict(cond_base(c_extra[i], 3 + i), target=[tpl], value=count, minDurability=0, maxDurability=100, dogtagLevel=0,
                           onlyFoundInRaid=False, isEncoded=False, includeEquipment=False, conditionType="HandoverItem"))

    rewards, ri = [], 0

    def reward(**kw):
        global ri
        r = dict(id=oid(REWARD, n, len(rewards) + 1), index=len(rewards), unknown=False, gameMode=[])
        r.update(kw)
        rewards.append(r)

    reward(type="Experience", value=str(m["xp"]))
    reward(type="TraderStanding", value=str(m["rep"]), target=m["trader"])
    money_item = oid(RITEM, n, 1)
    reward(type="Item", value=str(m["money"]), target=money_item, findInRaid=True, items=[{"_id": money_item, "_tpl": ROUBLES, "upd": {"StackObjectsCount": m["money"]}}])
    for k, perk in enumerate(m.get("recipes", [])):
        it = oid(RITEM, n, 0x10 + k)
        reward(type="ProductionScheme", target=it, traderId=str(NUTRITION), loyaltyLevel=1,
               items=[{"_id": it, "_tpl": PERK_ITEM[perk], "upd": {"StackObjectsCount": 1}}])
    if "sale" in m:
        offer = PERK_OFFER[m["sale"]]
        reward(type="AssortmentUnlock", target=offer, traderId=THERAPIST, loyaltyLevel=1,
               items=[{"_id": offer, "_tpl": PERK_ITEM[m["sale"]], "parentId": "hideout", "slotId": "hideout", "upd": {"StackObjectsCount": 1}}])
    if m.get("final"):
        for k, perk in enumerate(PERK_ITEM):
            it = oid(RITEM, n, 0x20 + k)
            reward(type="Item", value="2", target=it, findInRaid=True, items=[{"_id": it, "_tpl": PERK_ITEM[perk], "upd": {"StackObjectsCount": 2}}])
        it = oid(RITEM, n, 0x30)
        reward(type="Item", value="15", target=it, findInRaid=True, items=[{"_id": it, "_tpl": GP, "upd": {"StackObjectsCount": 15}}])

    quests[q] = {
        "_id": q, "QuestName": "tarkovacola_%02d" % n, "traderId": m["trader"], "location": "any",
        "image": "/files/quest/icon/5788d151d53a280a8e8798cc.jpg", "type": "PickUp", "side": "Pmc",
        "isKey": False, "restartable": False, "instantComplete": False, "secretQuest": False, "notDisplayedQuest": False,
        "canShowNotificationsInGame": True, "status": 0,
        **{k: q + " " + k for k in ("name", "description", "note", "successMessageText", "failMessageText", "startedMessageText",
                                    "changeQuestMessageText", "acceptPlayerMessage", "declinePlayerMessage", "completePlayerMessage")},
        "acceptanceAndFinishingSource": "eft", "progressSource": "eft", "rankingModes": [], "gameModes": [], "arenaLocations": [],
        "conditions": {"AvailableForStart": start, "AvailableForFinish": finish, "Fail": []},
        "rewards": {"Started": [], "Success": rewards, "Fail": []},
    }

    for lang in ("en", "es"):
        t = m[lang]
        L = locales[lang]
        L[q + " name"] = t["name"]; L[q + " description"] = t["desc"]; L[q + " note"] = ""
        L[q + " successMessageText"] = t["success"]; L[q + " startedMessageText"] = t["start"]; L[q + " failMessageText"] = t["fail"]
        L[q + " acceptPlayerMessage"] = ""; L[q + " declinePlayerMessage"] = ""; L[q + " completePlayerMessage"] = ""; L[q + " changeQuestMessageText"] = ""
        L[c_level] = ("Reach level %d" if lang == "en" else "Alcanza el nivel %d") % m["level"]
        L[c_visit] = t["visit"]; L[c_find] = t["find"]; L[c_hand] = t["hand"]
        for i, c in enumerate(c_extra): L[c] = t["extra"][i]
        for k, prev in enumerate(m["after"]):
            L[oid(COND, n, 0x20 + k)] = ('Complete "%s"' if lang == "en" else 'Completa "%s"') % next(x for x in M if x["n"] == prev)[lang]["name"]

    zones.append({"id": zone_id, "type": "visit", "locations": [m["map"]],
                  "position": {"x": m["at"]["x"], "y": m["at"]["y"] + 1.0, "z": m["at"]["z"]},
                  "rotation": {"x": 0, "y": 0, "z": 0, "w": 1}, "size": {"x": 40, "y": 14, "z": 40},
                  "source": "Tarkova-Cola: puesto de los Embotelladores (mision %d)" % n})

    tc = {"mission": n, "perks": m["perks"], "item": item_id}
    spawns.append({"map": m["map"], "quest": q, "statuses": [2], "role": m["role"], "min": m["guards"], "max": m["guards"],
                   "difficulty": "normal", "at": m["at"], "radius": 7.0, "ready": True, "tc": tc,
                   "source": "Tarkova-Cola mision %d: guardias de los Embotelladores" % n})
    if "boss" in m:
        # el jefe va en el MISMO grupo que su escolta (si fueran dos grupos de PMC, se dispararian entre ellos): el primero es el
        # jefe, lleva todas las perks y el objeto de la mision
        b = m["boss"]
        s = spawns[-1]
        s["min"] = s["max"] = m["guards"] + 1
        s["difficulty"] = b["difficulty"]
        s["tc"].update({"boss": True, "bossPerks": b["perks"]})
        s["source"] = "Tarkova-Cola mision %d: el Barman (el primero del grupo) y su escolta" % n

    story_items.append({"id": item_id, "nameEn": m["en"]["item"][1], "shortEn": m["en"]["item"][0], "descEn": m["en"]["item"][2],
                        "nameEs": m["es"]["item"][1], "shortEs": m["es"]["item"][0], "descEs": m["es"]["item"][2]})

story = {
    "items": story_items,
    "recipes": [{"id": recipe_id(p), "perk": p, "product": PERK_ITEM[p], "area": NUTRITION, "time": RECIPE_TIME[p],
                 "quest": qid[next(m["n"] for m in M if p in m.get("recipes", []))], "items": [{"tpl": t, "count": c} for t, c in RECIPES[p]]}
                for p in PERK_ITEM],
    "sales": {PERK_OFFER[m["sale"]]: qid[m["n"]] for m in M if "sale" in m},
    "quests": {str(m["n"]): qid[m["n"]] for m in M},
}

pack = {"name": "TarkovaCola", "requires": "~1.3.5", "description": {"en": "Tarkova-Cola: the Bottlers storyline", "ch": "Tarkova-Cola"},
        "author": "Yendaxddd", "version": "2.0.0"}


def write(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2), encoding="utf-8")


if OUT.exists(): shutil.rmtree(OUT)
write(OUT / "pack.json", pack)
write(OUT / "quests" / "TarkovaCola.json", quests)
write(OUT / "locales" / "en.json", locales["en"])
write(OUT / "locales" / "es.json", locales["es"])
write(OUT / "zones" / "TarkovaCola.json", zones)
write(ROOT / "Server" / "ModFiles" / "guards.json", spawns)          # los hace aparecer el cliente del mod (VisitAPI pedia el bando equivocado)
write(STORY, story)
for m in M: print("mision %2d %-22s %-14s %s" % (m["n"], m["es"]["name"], m["map"], m["at"]))
print("ok ->", OUT, "y", STORY)
