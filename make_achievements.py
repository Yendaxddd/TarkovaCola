"""Logros de la 2.0 (70): genera el codigo de datos del servidor y del cliente y la lista de insignias.

Salida:
  Server/AchievementsExtra.cs     definiciones (nombre, descripcion, rareza, recompensas) que registra el servidor
  Client/AchievementsExtra.cs     clave -> id del juego (y si es secreto) para el cliente
  achievement_badges.json         que icono lleva cada insignia (lo lee make_achievement_icons.py)
Los disparadores (cuando se desbloquea cada uno) estan en Client/AchTrack.cs.
"""
import json, pathlib

ROOT = pathlib.Path(__file__).parent
RUB, GP = "Roubles", "Perks.GpCoin"
BOTTLE = {"speedcola": "Perks.SpeedColaId", "staminup": "Perks.StaminUpId", "juggernog": "Perks.JuggernogId",
          "phdflopper": "Perks.PhdFlopperId", "widowswine": "Perks.WidowsWineId", "deadshot": "Perks.DeadshotId"}

SYN = [  # id, A, B, en, es
    ("cardio", "speedcola", "staminup", "Runner's high", "Euforia del corredor", "Cardio", "Cardio"),
    ("medic", "speedcola", "juggernog", "Doctor's orders", "Órdenes del médico", "Combat Medic", "Médico de combate"),
    ("blast", "speedcola", "phdflopper", "Bang, reload", "Bum, recarga", "Blast Reload", "Recarga explosiva"),
    ("pin", "speedcola", "widowswine", "Pull and pray", "Tira y reza", "Quick Pin", "Anilla rápida"),
    ("trigger", "speedcola", "deadshot", "Trigger happy", "Gatillo alegre", "Hair Trigger", "Gatillo fácil"),
    ("tank", "staminup", "juggernog", "Heavy cardio", "Cardio pesado", "Tank", "Tanque"),
    ("bounce", "staminup", "phdflopper", "Boing", "Boing", "Bounce Back", "Rebote"),
    ("getaway", "staminup", "widowswine", "Web and run", "Atrapa y corre", "Getaway", "Huida"),
    ("breath", "staminup", "deadshot", "Deep breaths", "Respira hondo", "Runner's Breath", "Aliento de corredor"),
    ("shock", "juggernog", "phdflopper", "Shock absorber", "Amortiguador", "Shock Shield", "Escudo de choque"),
    ("iron", "juggernog", "widowswine", "Steel silk", "Seda de acero", "Steel Web", "Telaraña de acero"),
    ("sights", "juggernog", "deadshot", "Iron curtain", "Telón de acero", "Iron Sights", "Mira de hierro"),
    ("webblast", "phdflopper", "widowswine", "Spider bomb", "Bomba araña", "Blast Web", "Red expansiva"),
    ("above", "phdflopper", "deadshot", "Look out below", "Cuidado abajo", "Death From Above", "Muerte desde arriba"),
    ("spider", "widowswine", "deadshot", "Spider sense", "Sentido arácnido", "Spider Eye", "Ojo de araña"),
]
MISSIONS = [("Cold Cargo", "Carga fría"), ("The Formula", "La fórmula"), ("Bottling Line", "Línea de embotellado"),
            ("Fast Hands", "Manos rápidas"), ("Long Haul", "Fondo"), ("Meat Wall", "Muro de carne"), ("Zero Gravity", "Gravedad cero"),
            ("Black Widow", "Viuda negra"), ("Hawkeye", "Ojo de halcón"), ("Last Call", "Última ronda")]

A = []  # key, rarity, hidden, en name, es name, en desc, es desc, rewards [(tpl, n)], icon (perk id, [a, b] o "research")


def add(key, rarity, en, es, den, des, rewards, icon, hidden=False):
    A.append(dict(key=key, rarity=rarity, hidden=hidden, en=en, es=es, den=den, des=des, rewards=rewards, icon=icon))


# ---- Deadshot Daiquiri (6, como las demas perks)
add("dd_fresh", "Common", "Eye of the tiger", "Ojo de tigre", "Drink Deadshot Daiquiri for the first time.", "Bebe Deadshot Daiquiri por primera vez.", [(RUB, 20000)], "deadshot")
add("dd_heads", "Rare", "Head collector", "Coleccionista de cabezas", "Get 100 headshot kills in total with Deadshot Daiquiri active.", "Consigue 100 bajas de un tiro a la cabeza en total con Deadshot Daiquiri activa.", [(BOTTLE["deadshot"], 1), (RUB, 15000)], "deadshot")
add("dd_far", "Rare", "Long distance relationship", "Relación a distancia", "Kill 10 enemies from 100 m or more with Deadshot Daiquiri active.", "Mata a 10 enemigos a 100 m o más con Deadshot Daiquiri activa.", [(GP, 3)], "deadshot")
add("dd_lvl5", "Rare", "Steady as a rock", "Firme como una roca", "Take Deadshot Daiquiri to level 5.", "Lleva Deadshot Daiquiri al nivel 5.", [(GP, 4)], "deadshot")
add("dd_all", "Legendary", "Deadeye", "Ojo infalible", "Unlock every Deadshot Daiquiri augment.", "Consigue todas las mejoras de Deadshot Daiquiri.", [(BOTTLE["deadshot"], 3)], "deadshot")
add("dd_secret", "Legendary", "Five for five", "Cinco de cinco", "Get 5 headshot kills in a single raid with Deadshot Daiquiri active.", "Consigue 5 bajas de un tiro a la cabeza en una misma raid con Deadshot Daiquiri activa.", [(GP, 5)], "deadshot", hidden=True)

# ---- Sinergias (18)
for sid, a, b, en, es, sen, ses in SYN:
    add("syn_" + sid, "Common", en, es, "Unlock the " + sen + " synergy.", "Desbloquea la sinergia " + ses + ".", [(RUB, 15000)], [a, b])
add("syn_five", "Rare", "Chemistry", "Química", "Unlock 5 synergies.", "Desbloquea 5 sinergias.", [(GP, 3)], ["speedcola", "juggernog"])
add("syn_all", "Legendary", "Perfect blend", "Mezcla perfecta", "Unlock all 15 synergies.", "Desbloquea las 15 sinergias.", [(GP, 8)], ["widowswine", "deadshot"])
add("syn_extract", "Common", "Teamwork", "Trabajo en equipo", "Extract alive with an equipped synergy working (both of its perks active).", "Extrae con vida con una sinergia equipada funcionando (sus dos perks activas).", [(RUB, 25000)], ["staminup", "phdflopper"])

# ---- Historia (13)
for i, (en, es) in enumerate(MISSIONS, 1):
    rar = "Common" if i <= 3 else ("Rare" if i <= 9 else "Legendary")
    rew = [(RUB, 20000 + 5000 * i)] if i <= 3 else ([(GP, 2)] if i <= 9 else [(GP, 6)])
    add("q_%02d" % i, rar, en, es, 'Complete the mission "' + en + '".', 'Completa la misión "' + es + '".', rew, "research")
add("q_guards", "Rare", "Bottle breaker", "Rompebotellas", "Kill 25 Bottlers.", "Mata a 25 Embotelladores.", [(GP, 3)], "research")
add("q_boss", "Legendary", "Closing time", "Hora de cerrar", "Kill the Bartender.", "Mata al Barman.", [(GP, 5)], "research")
add("q_sober", "Legendary", "Sober", "Sobrio", "Kill the Bartender with no perks active.", "Mata al Barman sin ninguna perk activa.", [(GP, 6)], "research", hidden=True)

# ---- Perks en general (10)
add("g_six", "Rare", "Full menu", "Menú completo", "Have all six perks active in the same raid.", "Ten las seis perks activas en la misma raid.", [(GP, 3)], "research")
add("g_five", "Common", "Cocktail", "Cóctel", "Have five perks active at once.", "Ten cinco perks activas a la vez.", [(RUB, 25000)], "research")
add("g_drinks50", "Common", "Regular customer", "Cliente habitual", "Drink 50 perks in total.", "Bebe 50 perks en total.", [(RUB, 30000)], "research")
add("g_drinks150", "Rare", "Sugar addict", "Adicto al azúcar", "Drink 150 perks in total.", "Bebe 150 perks en total.", [(GP, 4)], "research")
add("g_lvl5all", "Legendary", "Perk-a-holic", "Perk-adicto", "Take all six perks to level 5.", "Lleva las seis perks al nivel 5.", [(GP, 8)], "research")
add("g_augall", "Legendary", "Mad scientist", "Científico loco", "Unlock every augment of all six perks.", "Consigue todas las mejoras de las seis perks.", [(GP, 10)], "research")
add("g_aug20", "Rare", "Lab rat", "Rata de laboratorio", "Unlock 20 augments in total.", "Consigue 20 mejoras en total.", [(GP, 3)], "research")
add("g_rush", "Rare", "Happy hour", "Hora feliz", "Drink 3 perks in less than 60 seconds.", "Bebe 3 perks en menos de 60 segundos.", [(GP, 2)], "research")
add("g_combo", "Rare", "Party of three", "Fiesta para tres", "Kill 3 enemies in 10 seconds with 3 or more perks active.", "Mata a 3 enemigos en 10 segundos con 3 o más perks activas.", [(GP, 3)], "research")
add("g_bossall", "Legendary", "One of everything", "Uno de cada", "Kill a boss with all six perks active.", "Mata a un jefe con las seis perks activas.", [(GP, 6)], "research", hidden=True)

# ---- Eternal Perk (5)
add("e_first", "Common", "Forever young", "Eternamente joven", "Make a perk eternal.", "Haz que una perk sea eterna.", [(RUB, 25000)], "research")
add("e_three", "Rare", "Collector's edition", "Edición de coleccionista", "Have 3 eternal perks at the same time.", "Ten 3 perks eternas a la vez.", [(GP, 3)], "research")
add("e_all", "Legendary", "Immortal menu", "Menú inmortal", "Have all six perks eternal at the same time.", "Ten las seis perks eternas a la vez.", [(GP, 8)], "research", hidden=True)
add("e_back", "Common", "Return to sender", "Devolver al remitente", "Die with an eternal perk and get its bottle back by mail.", "Muere con una perk eterna y recupera su botella por correo.", [(RUB, 15000)], "research")
add("e_raids", "Rare", "Long live the perk", "Larga vida a la perk", "Start 5 raids with an eternal perk already active.", "Empieza 5 raids con una perk eterna ya activa.", [(GP, 3)], "research")

# ---- Perk aleatoria (3)
add("p_first", "Common", "Lucky drop", "Golpe de suerte", "Pick up a random perk.", "Recoge una perk aleatoria.", [(RUB, 20000)], "research")
add("p_eternal", "Rare", "Double down", "Doble o nada", "Get an eternal perk from a random perk.", "Consigue una perk eterna con una perk aleatoria.", [(GP, 3)], "research")
add("p_late", "Legendary", "Just in time", "Justo a tiempo", "Pick up a random perk with 5 seconds or less left.", "Recoge una perk aleatoria con 5 segundos o menos de margen.", [(GP, 5)], "research", hidden=True)

# ---- Modos (6: 4 de Classic, 2 de Modern)
add("c_full", "Rare", "Full house", "Casa llena", "Classic mode: have 4 main perks and a modifier.", "Modo Clásico: ten 4 perks principales y un modificador.", [(GP, 3)], "research")
add("c_chal", "Common", "Challenger", "Retador", "Classic mode: complete a raid challenge and extract alive.", "Modo Clásico: cumple un desafío de raid y extrae con vida.", [(RUB, 25000)], "research")
add("c_second", "Rare", "Double augment", "Doble mejora", "Classic mode: give a perk its second minor augment.", "Modo Clásico: consigue la segunda mejora menor de una perk.", [(GP, 3)], "research")
add("c_streak", "Legendary", "Survivor", "Superviviente", "Classic mode: extract 5 raids in a row keeping your perks.", "Modo Clásico: extrae 5 raids seguidas conservando tus perks.", [(GP, 6)], "research")
add("m_loaded", "Common", "Fully loaded", "Hasta arriba", "Modern mode: equip a major augment and 2 minor augments on the same perk.", "Modo Moderno: equipa una mejora mayor y 2 menores en la misma perk.", [(RUB, 25000)], "research")
add("m_spread", "Rare", "Well rounded", "Todoterreno", "Modern mode: unlock at least one augment on all six perks.", "Modo Moderno: consigue al menos una mejora en las seis perks.", [(GP, 3)], "research")

# ---- Estadisticas (5)
add("s_kills100", "Common", "Bottled fury", "Furia embotellada", "Get 100 kills with a perk active.", "Consigue 100 bajas con alguna perk activa.", [(RUB, 30000)], "speedcola")
add("s_kills500", "Rare", "Perk soldier", "Soldado perk", "Get 500 kills with a perk active.", "Consigue 500 bajas con alguna perk activa.", [(GP, 4)], "juggernog")
add("s_kills1500", "Legendary", "Walking vending machine", "Máquina expendedora andante", "Get 1,500 kills with a perk active.", "Consigue 1.500 bajas con alguna perk activa.", [(GP, 8)], "research")
add("s_run", "Rare", "Iron marathon", "Maratón de hierro", "Sprint 50 km in total with a perk active.", "Corre 50 km en total con alguna perk activa.", [(GP, 3)], "staminup")
add("s_absorb", "Rare", "Shockproof", "A prueba de golpes", "Let PHD Flopper cancel 5,000 damage in total.", "Deja que PHD Flopper anule 5.000 de daño en total.", [(GP, 3)], "phdflopper")

# ---- Combos y extras (4)
add("s_heads", "Rare", "Head game", "Juego de cabezas", "Get 250 headshot kills with a perk active.", "Consigue 250 bajas de un tiro a la cabeza con alguna perk activa.", [(GP, 4)], "deadshot")
add("s_webbed", "Rare", "Arachnophobia", "Aracnofobia", "Web 250 enemies in total.", "Atrapa a 250 enemigos en total.", [(GP, 4)], "widowswine")
add("x_mirror", "Rare", "Mirror match", "Duelo de espejos", "Kill a Bottler who carries a perk you also have active.", "Mata a un Embotellador que lleve una perk que tú también tengas activa.", [(GP, 2)], "research")
add("x_extract6", "Legendary", "Last one standing", "El último en pie", "Extract alive with all six perks active.", "Extrae con vida con las seis perks activas.", [(GP, 6)], "research")

assert len(A) == 70, len(A)


def cs(s): return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def sid(i): return "6a1c00000000000000000a%02x" % (0x1f + i)


# servidor
lines = []
for i, a in enumerate(A):
    rew = ", ".join("(%s, %d)" % (t, n) for t, n in a["rewards"])
    lines.append('            new Def { Id = "%s", Key = %s, Rarity = "%s",%s\n                NameEn = %s, NameEs = %s,\n                DescEn = %s, DescEs = %s,\n                Rewards = new[] { %s } },'
                 % (sid(i), cs(a["key"].replace("_", "")), a["rarity"], " Hidden = true," if a["hidden"] else "", cs(a["en"]), cs(a["es"]), cs(a["den"]), cs(a["des"]), rew))
(ROOT / "Server" / "AchievementsExtra.cs").write_text(
    "// GENERADO por make_achievements.py: no editar a mano.\nnamespace TarkovaCola.Server\n{\n    internal static partial class AchievementData\n    {\n        // logros de la 2.0\n        internal static readonly Def[] Extra =\n        {\n"
    + "\n".join(lines) + "\n        };\n    }\n}\n", encoding="utf-8")

# cliente
lines = ['            new Def { Key = "%s", ServerId = "%s"%s },' % (a["key"], sid(i), ", Secret = true" if a["hidden"] else "") for i, a in enumerate(A)]
(ROOT / "Client" / "AchievementsExtra.cs").write_text(
    "// GENERADO por make_achievements.py: no editar a mano.\nnamespace TarkovaCola.Client\n{\n    internal static partial class Achievements\n    {\n        // logros de la 2.0 (los disparadores estan en AchTrack.cs)\n        internal static readonly Def[] Extra =\n        {\n"
    + "\n".join(lines) + "\n        };\n    }\n}\n", encoding="utf-8")

# insignias
(ROOT / "achievement_badges.json").write_text(json.dumps({a["key"].replace("_", ""): [a["rarity"].lower(), a["icon"]] for a in A}, indent=1), encoding="utf-8")
print("ok:", len(A), "logros")
