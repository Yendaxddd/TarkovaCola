"""Genera las insignias de los logros (Server/ModFiles/achievements/*.png): hexagono con borde segun rareza y la lata de Speed Cola."""
import pathlib
from PIL import Image, ImageDraw, ImageFilter

ROOT = pathlib.Path(__file__).parent
OUT = ROOT / "Server" / "ModFiles" / "achievements"
OUT.mkdir(parents=True, exist_ok=True)
ICONS = {n: Image.open(ROOT / "Client" / "assets" / (n + "_icon.png")).convert("RGBA") for n in ("speedcola", "staminup")}

W, H = 196, 224
RARITY = {"common": (190, 196, 202), "rare": (79, 170, 255), "legendary": (240, 193, 75)}
# clave -> (rareza, perk del icono)
BADGES = {
    "first": ("common", "speedcola"), "lvl5": ("rare", "speedcola"), "all": ("legendary", "speedcola"),
    "found": ("rare", "speedcola"), "die": ("common", "speedcola"), "hand": ("legendary", "speedcola"),
    "stfresh": ("common", "staminup"), "strushb": ("rare", "staminup"), "stmarathon": ("rare", "staminup"),
    "stlvl5": ("rare", "staminup"), "stcardio": ("legendary", "staminup"), "stdouble": ("legendary", "staminup"),
}


def hexagon(cx, cy, r):
    import math
    return [(cx + r * math.sin(math.radians(a)), cy - r * math.cos(math.radians(a))) for a in range(0, 360, 60)]


for key, (rar, perk) in BADGES.items():
    col = RARITY[rar]
    icon = ICONS[perk]
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
    ic = icon.resize((120, 120), Image.LANCZOS)
    img.alpha_composite(ic, (int(cx - 60), int(cy - 60)))
    img.save(OUT / f"tarkovacola_{key}.png")
print("ok", sorted(p.name for p in OUT.iterdir()))
