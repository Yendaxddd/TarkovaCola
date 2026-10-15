"""Genera las insignias de los logros (Server/ModFiles/achievements/*.png): hexagono con borde segun rareza y el icono de la perk."""
import pathlib
from PIL import Image, ImageDraw, ImageFilter

ROOT = pathlib.Path(__file__).parent
OUT = ROOT / "Server" / "ModFiles" / "achievements"
OUT.mkdir(parents=True, exist_ok=True)
ICONS = {n: Image.open(ROOT / "Client" / "assets" / (n + "_icon.png")).convert("RGBA") for n in ("speedcola", "staminup", "juggernog", "phdflopper", "widowswine", "deadshot")}
ICONS["research"] = Image.open(ROOT / "Client" / "assets" / "research_icon.png").convert("RGBA")

W, H = 196, 224
RARITY = {"common": (190, 196, 202), "rare": (79, 170, 255), "legendary": (240, 193, 75)}
# clave -> (rareza, perk del icono)
BADGES = {
    "first": ("common", "speedcola"), "lvl5": ("rare", "speedcola"), "all": ("legendary", "speedcola"),
    "found": ("rare", "speedcola"), "die": ("common", "speedcola"), "hand": ("legendary", "speedcola"),
    "stfresh": ("common", "staminup"), "strushb": ("rare", "staminup"), "stmarathon": ("rare", "staminup"),
    "stlvl5": ("rare", "staminup"), "stcardio": ("legendary", "staminup"), "stdouble": ("legendary", "staminup"),
    "jgfresh": ("common", "juggernog"), "jgrude": ("rare", "juggernog"), "jgred": ("rare", "juggernog"),
    "jglvl5": ("rare", "juggernog"), "jgall": ("legendary", "juggernog"), "jgsecret": ("legendary", "juggernog"),
    "pfflop": ("common", "phdflopper"), "pfhugs": ("rare", "phdflopper"), "pfgravity": ("rare", "phdflopper"),
    "pflvl5": ("rare", "phdflopper"), "pfall": ("legendary", "phdflopper"), "pfsecret": ("legendary", "phdflopper"),
    "wwfirst": ("common", "widowswine"), "wwwebbed": ("rare", "widowswine"), "wwgrenades": ("rare", "widowswine"),
    "wwlvl5": ("rare", "widowswine"), "wwall": ("legendary", "widowswine"), "wwsecret": ("legendary", "widowswine"),
}


def hexagon(cx, cy, r):
    import math
    return [(cx + r * math.sin(math.radians(a)), cy - r * math.cos(math.radians(a))) for a in range(0, 360, 60)]


# logros de la 2.0 (make_achievements.py): el icono puede ser una perk, "research" o una pareja de perks (sinergias)
import json
BADGES.update({k: (v[0], v[1]) for k, v in json.loads((ROOT / "achievement_badges.json").read_text(encoding="utf-8")).items()})

for key, (rar, perk) in BADGES.items():
    col = RARITY[rar]
    pair = isinstance(perk, list)
    icon = ICONS[perk[0] if pair else perk]
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx, cy = W / 2, H / 2
    d.polygon(hexagon(cx, cy, 108), fill=col + (255,))                       # borde
    d.polygon(hexagon(cx, cy, 96), fill=(28, 38, 48, 255))                   # fondo
    inner = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(inner).polygon(hexagon(cx, cy, 96), fill=(60, 78, 94, 255))
    inner = inner.filter(ImageFilter.GaussianBlur(26))                        # brillo suave en el centro
    mask = Image.new("L", (W, H), 0); ImageDraw.Draw(mask).polygon(hexagon(cx, cy, 96), fill=255)
    img.paste(inner, (0, 0), Image.composite(inner.split()[3], Image.new("L", (W, H), 0), mask))
    if pair:                                                                  # sinergia: las dos perks, una delante de la otra
        a = ICONS[perk[0]].resize((80, 80), Image.LANCZOS); b = ICONS[perk[1]].resize((80, 80), Image.LANCZOS)
        img.alpha_composite(a, (int(cx - 72), int(cy - 62)))
        img.alpha_composite(b, (int(cx - 8), int(cy - 18)))
    else:
        ic = icon.resize((120, 120), Image.LANCZOS)
        img.alpha_composite(ic, (int(cx - 60), int(cy - 60)))
    img.save(OUT / f"tarkovacola_{key}.png")
print("ok", sorted(p.name for p in OUT.iterdir()))
