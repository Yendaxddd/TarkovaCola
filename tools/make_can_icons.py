# Iconos de inventario (256x256) de Bandolier Bandit y Widow's Wine: el render de iconos de EFT las pintaba blancas.
#   python tools/make_can_icons.py <dir con *_cover_albedo.png> <salida: Client/assets>
import sys, math
from PIL import Image, ImageDraw, ImageFilter
src, out = sys.argv[1], sys.argv[2]
S = 256
for name in ("bandolier", "widowswine"):
    cover = Image.open(f"{src}/{name}_cover_albedo.png").convert("RGB")
    W, H = 100, 168                                    # cuerpo de la lata
    # el frente: la franja del logo (x ~ 170..410 de 1024) a todo lo alto de la etiqueta
    lab = cover.crop((130, 0, 450, 512)).resize((W, H), Image.LANCZOS)
    # sombreado cilindrico
    shade = Image.new("L", (W, H))
    px = shade.load()
    for x in range(W):
        t = (x / (W - 1)) * 2 - 1
        k = math.sqrt(max(0.0, 1 - t * t))
        v = int(255 * (0.55 + 0.45 * k) - 40 * max(0, -t) )
        for y in range(H): px[x, y] = max(0, min(255, v))
    lab = Image.composite(lab, Image.new("RGB", (W, H), (0, 0, 0)), shade)
    lab = Image.blend(lab, Image.merge("RGB", [shade] * 3).point(lambda v: v), 0.0)
    can = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    x0, y0 = (S - W) // 2, (S - H) // 2 + 2
    can.paste(lab, (x0, y0))
    d = ImageDraw.Draw(can)
    for (ya, yb) in ((y0 - 14, y0), (y0 + H, y0 + H + 14)):       # tapa y base plateadas
        for x in range(W):
            t = (x / (W - 1)) * 2 - 1
            g = int(120 + 110 * math.sqrt(max(0, 1 - t * t)))
            d.line([(x0 + x, ya), (x0 + x, yb)], fill=(g, g, g + 6, 255))
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).rounded_rectangle([x0, y0 - 14, x0 + W, y0 + H + 14], radius=9, fill=255)
    a = can.getchannel("A")
    can.putalpha(Image.composite(a, Image.new("L", (S, S), 0), m))
    can = can.filter(ImageFilter.GaussianBlur(0.6))
    can.save(f"{out}/{name}_caniconv.png")
print("ok")
