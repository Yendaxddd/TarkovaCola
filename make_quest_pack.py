"""Genera el pack de misiones de Tarkova-Cola para VisitAPI: Server/Packs/TarkovaCola/ (pack.json, quests, locales, spawns).

El build del servidor lo copia a user/mods/VisitAPI-Server/packs/TarkovaCola/ cuando VisitAPI esta instalado.
Para colocar el punto de los guardias: en raid, tecla F10 (copia mapa y coordenadas), y pon aqui POST_MAP / POST_AT / POST_READY.
"""
import json, pathlib

ROOT = pathlib.Path(__file__).parent
OUT = ROOT / "Server" / "Packs" / "TarkovaCola"

# ---------------------------------------------------------------- punto de los guardias (se rellena con la herramienta F10)
POST_MAP = "RezervBase"                      # id de mapa del juego (bigmap = Customs, shoreline, woods, ...)
POST_AT = {"x": 59.51, "y": -6.70, "z": 65.68}
POST_RADIUS = 7.0
POST_READY = True                        # True cuando las coordenadas son de verdad

SKIER, THERAPIST = "58330581ace78e27b8b10cee", "54cb57776803fa99248b456e"
ROUBLES = "5449016a4bdc2d6f028b456f"
Q1 = "6a1c00000000000000000e01"
ENVELOPE = "6a1c00000000000000000f01"
PHD_ITEM, PHD_OFFER = "6a1c00000000000000000c04", "6a1c00000000000000000d04"

C_LEVEL, C_FIND, C_HAND = "6a1c00000000000000000e11", "6a1c00000000000000000e12", "6a1c00000000000000000e13"
R_XP, R_REP, R_MONEY, R_MONEY_ITEM, R_UNLOCK = ("6a1c00000000000000000e2%d" % i for i in range(1, 6))


def cond_base(cid, index):
    return {"id": cid, "index": index, "parentId": "", "dynamicLocale": False, "visibilityConditions": [], "globalQuestCounterId": ""}


quest = {
    "_id": Q1,
    "QuestName": "tarkovacola_skier_1",
    "traderId": SKIER,
    "location": "any",
    "image": "/files/quest/icon/5788d151d53a280a8e8798cc.jpg",
    "type": "PickUp",
    "side": "Pmc",
    "isKey": False,
    "restartable": False,
    "instantComplete": False,
    "secretQuest": False,
    "notDisplayedQuest": False,
    "canShowNotificationsInGame": True,
    "status": 0,
    "name": Q1 + " name",
    "description": Q1 + " description",
    "note": Q1 + " note",
    "successMessageText": Q1 + " successMessageText",
    "failMessageText": Q1 + " failMessageText",
    "startedMessageText": Q1 + " startedMessageText",
    "changeQuestMessageText": Q1 + " changeQuestMessageText",
    "acceptPlayerMessage": Q1 + " acceptPlayerMessage",
    "declinePlayerMessage": Q1 + " declinePlayerMessage",
    "completePlayerMessage": Q1 + " completePlayerMessage",
    "acceptanceAndFinishingSource": "eft",
    "progressSource": "eft",
    "rankingModes": [], "gameModes": [], "arenaLocations": [],
    "conditions": {
        "AvailableForStart": [
            dict(cond_base(C_LEVEL, 0), isEncoded=False, compareMethod=">=", value=12, conditionType="Level"),
        ],
        "AvailableForFinish": [
            dict(cond_base(C_FIND, 0), target=[ENVELOPE], value=1, minDurability=0, maxDurability=100, dogtagLevel=0,
                 onlyFoundInRaid=True, isEncoded=False, countInRaid=False, includeEquipment=False, conditionType="FindItem"),
            dict(cond_base(C_HAND, 1), target=[ENVELOPE], value=1, minDurability=0, maxDurability=100, dogtagLevel=0,
                 onlyFoundInRaid=True, isEncoded=False, includeEquipment=False, conditionType="HandoverItem"),
        ],
        "Fail": [],
    },
    "rewards": {
        "Started": [],
        "Success": [
            {"id": R_XP, "type": "Experience", "value": "4500", "index": 0, "unknown": False, "gameMode": []},
            {"id": R_REP, "type": "TraderStanding", "value": "0.03", "target": SKIER, "index": 1, "unknown": False, "gameMode": []},
            {"id": R_MONEY, "type": "Item", "value": "150000", "target": R_MONEY_ITEM, "index": 2, "findInRaid": True, "unknown": False, "gameMode": [],
             "items": [{"_id": R_MONEY_ITEM, "_tpl": ROUBLES, "upd": {"StackObjectsCount": 150000}}]},
            {"id": R_UNLOCK, "type": "AssortmentUnlock", "target": PHD_OFFER, "traderId": THERAPIST, "loyaltyLevel": 1, "index": 3, "unknown": False, "gameMode": [],
             "items": [{"_id": PHD_OFFER, "_tpl": PHD_ITEM, "parentId": "hideout", "slotId": "hideout", "upd": {"StackObjectsCount": 1}}]},
        ],
        "Fail": [],
    },
}

TEXT = {
    "en": {
        "name": "Cold Cargo",
        "description": "Some people have dug in with crates of that cola you all keep asking me about. They are guarding a spot and one of them carries an envelope that belongs to me. Get it back and bring it to me, and I will see that you get something fizzier to drink.",
        "success": "Good, this is what I wanted. Ask Therapist about the new bottle; I put in a word.",
        "started": "Find the guards, take the envelope and bring it back to Skier.",
        "fail": "",
        "accept": "I will take care of it.",
        "decline": "Not now.",
        "complete": "I have the envelope.",
        "change": "",
        "note": "",
        "level": "Reach level 12",
        "find": "Find the sealed envelope in raid",
        "hand": "Hand over the sealed envelope",
    },
    "es": {
        "name": "Carga fría",
        "description": "Unos tipos se han atrincherado con cajas de esa cola de la que todos me preguntan. Vigilan un punto y uno de ellos lleva un sobre que es mío. Recupéralo y tráemelo, y me aseguraré de que tengas algo con más burbujas para beber.",
        "success": "Bien, esto es lo que quería. Pregunta a Therapist por la botella nueva; he hablado con ella.",
        "started": "Encuentra a los guardias, quítales el sobre y llévaselo a Skier.",
        "fail": "",
        "accept": "Yo me encargo.",
        "decline": "Ahora no.",
        "complete": "Tengo el sobre.",
        "change": "",
        "note": "",
        "level": "Alcanza el nivel 12",
        "find": "Encuentra el sobre sellado en raid",
        "hand": "Entrega el sobre sellado",
    },
}


def locale(lang):
    t = TEXT[lang]
    return {
        Q1 + " name": t["name"], Q1 + " description": t["description"], Q1 + " note": t["note"],
        Q1 + " successMessageText": t["success"], Q1 + " startedMessageText": t["started"], Q1 + " failMessageText": t["fail"],
        Q1 + " acceptPlayerMessage": t["accept"], Q1 + " declinePlayerMessage": t["decline"],
        Q1 + " completePlayerMessage": t["complete"], Q1 + " changeQuestMessageText": t["change"],
        C_LEVEL: t["level"], C_FIND: t["find"], C_HAND: t["hand"],
    }


spawns = [{
    "map": POST_MAP,
    "quest": Q1,
    "statuses": [2],
    "role": "pmcBEAR",
    "min": 4,
    "max": 4,
    "difficulty": "normal",
    "at": POST_AT,
    "radius": POST_RADIUS,
    "ready": POST_READY,
    "source": "Los 4 guardias de la mision de Skier. Coordenadas con la tecla F10 en raid (mapa y posicion); pon ready a true cuando sean de verdad.",
}]

pack = {
    "name": "TarkovaCola",
    "requires": "~1.3.5",
    "description": {"en": "Tarkova-Cola quests", "ch": "Tarkova-Cola quests"},
    "author": "Yendaxddd",
    "version": "2.0.0",
}


def write(rel, obj):
    p = OUT / rel
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(obj, ensure_ascii=False, indent=2), encoding="utf-8")


write("pack.json", pack)
write("quests/TarkovaCola_Skier1.json", {Q1: quest})
write("locales/en.json", locale("en"))
write("locales/es.json", locale("es"))
write("spawns/TarkovaCola_Skier1.json", spawns)
print("ok ->", OUT)
