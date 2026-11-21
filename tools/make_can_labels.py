# Genera las etiquetas (cover_albedo 3600x1800) de las latas de BO6 que no existen en el juego: Bandolier Bandit y Widow's Wine.
# Misma maquetacion que las latas de BO6 (logo circular centrado en x~1015, tabla nutricional a la derecha, eslogan arriba).
#   python tools/make_can_labels.py <salida_dir> <dir_iconos>
import math, random, sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

W, H = 3600, 1800
CX, CY = 1015, 830          # centro del logo (igual que Vulture Aid)
FONTS = "C:/Windows/Fonts/"

def font(name, size):
    return ImageFont.truetype(FONTS + name, size)

def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))

def vgrad(c1, c2, w=W, h=H):
    g = Image.new("RGB", (1, h))
    for y in range(h): g.putpixel((0, y), lerp(c1, c2, y / (h - 1)))
    return g.resize((w, h))

def radial(c_in, c_out, cx, cy, r, w=W, h=H):
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    steps = 80
    for i in range(steps, 0, -1):
        rr = r * i / steps
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=int(255 * (1 - i / steps)))
    m = m.filter(ImageFilter.GaussianBlur(20))
    return Image.composite(Image.new("RGB", (w, h), c_in), Image.new("RGB", (w, h), c_out), m)

def noise(img, amount=10, seed=1):
    rnd = random.Random(seed)
    n = Image.effect_noise((W // 4, H // 4), 40).resize((W, H), Image.BILINEAR).convert("RGB")
    fine = Image.effect_noise((W, H), 25).convert("RGB")
    out = ImageChops.blend(img, ImageChops.overlay(img, n), amount / 100)
    return ImageChops.blend(out, ImageChops.overlay(out, fine), amount / 200)

def paste_rot(dst, tile, cx, cy, ang):
    t = tile.rotate(ang, resample=Image.BICUBIC, expand=True)
    dst.alpha_composite(t, (int(cx - t.width / 2), int(cy - t.height / 2)))

def char_tile(ch, f, fill, stroke, sw):
    l, t, r, b = f.getbbox(ch, stroke_width=sw)
    tile = Image.new("RGBA", (r - l + 8, b - t + 8), (0, 0, 0, 0))
    ImageDraw.Draw(tile).text((4 - l, 4 - t), ch, font=f, fill=fill, stroke_width=sw, stroke_fill=stroke)
    return tile

def arc_text(img, text, f, r, top, fill, stroke, sw, track=1.0, cx=CX, cy=CY):
    widths = [f.getlength(c) * track for c in text]
    total = sum(widths) / r
    a = (-math.pi / 2 - total / 2) if top else (math.pi / 2 + total / 2)
    for c, w in zip(text, widths):
        da = w / r
        mid = a + da / 2 if top else a - da / 2
        x, y = cx + r * math.cos(mid), cy + r * math.sin(mid)
        rot = -(math.degrees(mid) + 90) if top else -(math.degrees(mid) - 90)
        if c != " ": paste_rot(img, char_tile(c, f, fill, stroke, sw), x, y, rot)
        a = a + da if top else a - da

def fit_font(name, text, max_w, size):
    while size > 10 and font(name, size).getlength(text) > max_w: size -= 2
    return font(name, size)

def logo(img, icon, top, bottom, c_outer, c_outer_edge, c_band, c_text, c_text_stroke, c_disc, ring_font, glow):
    # halo
    g = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse([CX - 440, CY - 440, CX + 440, CY + 440], fill=glow)
    img.alpha_composite(g.filter(ImageFilter.GaussianBlur(35)))
    d = ImageDraw.Draw(img)
    d.ellipse([CX - 405, CY - 405, CX + 405, CY + 405], fill=c_outer_edge)
    d.ellipse([CX - 392, CY - 392, CX + 392, CY + 392], fill=c_outer)
    d.ellipse([CX - 372, CY - 372, CX + 372, CY + 372], fill=c_outer_edge)
    d.ellipse([CX - 362, CY - 362, CX + 362, CY + 362], fill=c_band)
    d.ellipse([CX - 268, CY - 268, CX + 268, CY + 268], fill=c_outer)
    d.ellipse([CX - 254, CY - 254, CX + 254, CY + 254], fill=c_disc)
    # remaches a los lados (como los tornillos de Vulture Aid)
    for sx in (-1, 1):
        bx = CX + sx * 315
        d.ellipse([bx - 26, CY - 26, bx + 26, CY + 26], fill=c_outer_edge)
        d.ellipse([bx - 20, CY - 20, bx + 20, CY + 20], fill=c_outer)
        d.ellipse([bx - 9, CY - 13, bx + 3, CY - 3], fill=(255, 255, 255, 120))
    arc_text(img, top, font(ring_font, 92), 315, True, c_text, c_text_stroke, 6, 1.02)
    arc_text(img, bottom, font(ring_font, 74), 318, False, c_text, c_text_stroke, 5, 1.05)
    ic = icon.resize((420, 420), Image.LANCZOS)
    sh = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sh.alpha_composite(Image.new("RGBA", ic.size, (0, 0, 0, 160)), (CX - 210 + 10, CY - 210 + 14))
    a = ic.split()[3]
    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shadow.paste(Image.new("RGBA", ic.size, (0, 0, 0, 150)), (CX - 210 + 10, CY - 205 + 14), a)
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(8)))
    img.alpha_composite(ic, (CX - 210, CY - 205))

def nutrition(img, x0, y0, cal, ingredients, frame, rows):
    d = ImageDraw.Draw(img)
    w, h = 520, 700
    d.rectangle([x0 - 14, y0 - 14, x0 + w + 14, y0 + h + 14], fill=frame)
    d.rectangle([x0, y0, x0 + w, y0 + h], fill=(250, 248, 240), outline=(0, 0, 0), width=4)
    fb, fr, fs = font("arialbd.ttf", 60), font("arial.ttf", 30), font("arialbd.ttf", 30)
    y = y0 + 8
    d.text((x0 + 14, y), "Nutrition Facts", font=fb, fill=(0, 0, 0)); y += 74
    d.text((x0 + 14, y), "Serv. Size 1 Can", font=fr, fill=(0, 0, 0)); y += 42
    d.rectangle([x0 + 10, y, x0 + w - 10, y + 16], fill=(0, 0, 0)); y += 24
    d.text((x0 + 14, y), "Amount Per Serving", font=font("arialbd.ttf", 24), fill=(0, 0, 0)); y += 32
    d.text((x0 + 14, y), "Calories", font=font("arialbd.ttf", 40), fill=(0, 0, 0))
    d.text((x0 + 200, y + 2), cal, font=font("arial.ttf", 38), fill=(0, 0, 0)); y += 52
    d.rectangle([x0 + 10, y, x0 + w - 10, y + 8], fill=(0, 0, 0)); y += 14
    d.text((x0 + w - 200, y), "% Daily Value", font=font("arialbd.ttf", 24), fill=(0, 0, 0)); y += 34
    for name, amt, pct in rows:
        d.line([x0 + 10, y, x0 + w - 10, y], fill=(0, 0, 0), width=2); y += 6
        d.text((x0 + 14, y), name, font=fs, fill=(0, 0, 0))
        d.text((x0 + 18 + fs.getlength(name + " "), y), amt, font=fr, fill=(0, 0, 0))
        d.text((x0 + w - 16 - fs.getlength(pct), y), pct, font=fs, fill=(0, 0, 0)); y += 40
    d.rectangle([x0 + 10, y, x0 + w - 10, y + 14], fill=(0, 0, 0)); y += 26
    fi = font("arial.ttf", 25)
    line = ""
    for word in ("INGREDIENTS: " + ingredients).split():
        if fi.getlength(line + word) > w - 30:
            d.text((x0 + 14, y), line, font=fi, fill=(0, 0, 0)); y += 31; line = ""
        line += word + " "
    d.text((x0 + 14, y), line, font=fi, fill=(0, 0, 0))

def border(img, y, c1, c2):
    d = ImageDraw.Draw(img)
    d.rectangle([0, y, W, y + 26], fill=c1)
    d.rectangle([0, y + 34, W, y + 44], fill=c2)

# ======================================================================= BANDOLIER BANDIT
def cartridge(scale=1.0, shade=1.0):
    w, h = int(30 * scale), int(110 * scale)
    t = Image.new("RGBA", (w + 4, h + 4), (0, 0, 0, 0))
    d = ImageDraw.Draw(t)
    case_h = int(h * 0.68)
    for x in range(w):           # laton cilindrico: degradado horizontal
        k = 0.55 + 0.45 * math.sin(math.pi * (x + 0.5) / w)
        k *= shade
        d.line([x + 2, h - case_h + 2, x + 2, h + 1], fill=(int(214 * k), int(160 * k), int(62 * k), 255))
    tip_h = h - case_h
    for x in range(w):
        k = (0.5 + 0.5 * math.sin(math.pi * (x + 0.5) / w)) * shade
        top = 2 + tip_h * (1 - math.sqrt(max(0.0, 1 - ((x - w / 2) / (w / 2)) ** 2)) * 0.98)
        d.line([x + 2, top + tip_h * 0.15, x + 2, h - case_h + 4], fill=(int(176 * k), int(96 * k), int(56 * k), 255))
    d.rectangle([2, h - 6, w + 1, h + 1], fill=(int(120 * shade), int(84 * shade), int(30 * shade), 255))
    d.line([2, h - case_h + 4, w + 1, h - case_h + 4], fill=(90, 60, 20, 255), width=2)
    return t

def bandolier_strap(length, thick, rnd):
    t = Image.new("RGBA", (length, thick + 160), (0, 0, 0, 0))
    d = ImageDraw.Draw(t)
    y0 = 80
    # cartuchos asomando por arriba de la correa
    for x in range(40, length - 40, 64):
        c = cartridge(1.25, rnd.uniform(0.85, 1.05))
        t.alpha_composite(c, (x, y0 - 70))
    for y in range(thick):       # cuero
        k = 0.7 + 0.3 * math.sin(math.pi * y / thick)
        d.line([0, y0 + y, length, y0 + y], fill=(int(104 * k), int(62 * k), int(30 * k), 255))
    for x in range(40, length - 40, 64):    # presillas
        d.rectangle([x - 6, y0 + 4, x + 46, y0 + thick * 0.62], fill=(82, 48, 22, 255), outline=(52, 30, 12, 255), width=3)
    for yy in (y0 + 10, y0 + thick - 12):  # pespunte
        for x in range(0, length, 26): d.line([x, yy, x + 14, yy], fill=(222, 190, 130, 255), width=4)
    return t

def make_bandolier(icon):
    rnd = random.Random(7)
    img = vgrad((222, 150, 58), (92, 48, 18)).convert("RGBA")
    img = Image.alpha_composite(img, radial((255, 214, 120), (0, 0, 0), CX, CY, 1200).convert("RGBA").point(lambda v: v) if False else img)
    # rayos de sol detras del logo (estilo cartel del oeste)
    rays = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dr = ImageDraw.Draw(rays)
    for i in range(36):
        a0 = math.radians(i * 10); a1 = math.radians(i * 10 + 5)
        dr.polygon([(CX, CY), (CX + 3000 * math.cos(a0), CY + 3000 * math.sin(a0)), (CX + 3000 * math.cos(a1), CY + 3000 * math.sin(a1))], fill=(255, 220, 140, 38))
    img.alpha_composite(rays)
    # casquillos sueltos por todo el fondo
    for _ in range(70):
        x, y = rnd.uniform(0, W), rnd.uniform(120, H - 140)
        if abs(x - CX) < 470 and abs(y - CY) < 470: continue
        if 2300 < x < 2880 and 300 < y < 1230: continue
        paste_rot(img, cartridge(rnd.uniform(0.8, 1.3), rnd.uniform(0.75, 1.0)), x, y, rnd.uniform(0, 360))
    # la cartuchera cruzando la etiqueta en diagonal
    strap = bandolier_strap(4400, 150, rnd)
    paste_rot(img, strap, 1500, 1050, 14)
    border(img, 18, (60, 34, 14), (230, 176, 80))
    border(img, H - 70, (60, 34, 14), (230, 176, 80))
    logo(img, icon, "BANDOLIER", "BANDIT", (214, 160, 62), (52, 30, 12), (48, 28, 14), (246, 222, 160), (40, 20, 6), (240, 226, 190),
         "GOUDYSTO.TTF", (255, 210, 120, 150))
    d = ImageDraw.Draw(img)
    f = font("PLAYBILL.TTF", 170)
    s = "NEVER RUN DRY, PARTNER!"
    d.text((CX - f.getlength(s) / 2, 70), s, font=f, fill=(255, 234, 180), stroke_width=7, stroke_fill=(52, 26, 8))
    nutrition(img, 2330, 330, "5.56", "Carbonated gunpowder, cactus nectar, brass shavings, saltpeter, leather extract, sarsaparilla, lead (less than 2%).",
              (60, 34, 14), [("Total Fat", "12g", "7%"), ("Sodium", "762mg", "39%"), ("Total Carb", "45g", "30%"), ("Sugars", "45g", ""), ("Protein", "0g", "")])
    fb = font("PLAYBILL.TTF", 96)
    d.text((300, H - 190), "ONLY 556 CALORIES", font=fb, fill=(255, 232, 170), stroke_width=4, stroke_fill=(52, 26, 8))
    d.text((1290, H - 190), "12 FL OZ (355 ML)", font=fb, fill=(255, 232, 170), stroke_width=4, stroke_fill=(52, 26, 8))
    return noise(img.convert("RGB"), 14, 3)

# ======================================================================= WIDOW'S WINE
def web(img, cx, cy, r, a0, a1, spokes, rings, col, width=3, seed=0):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    angs = [a0 + (a1 - a0) * i / (spokes - 1) + rnd.uniform(-0.03, 0.03) for i in range(spokes)]
    for a in angs: d.line([cx, cy, cx + r * math.cos(a), cy + r * math.sin(a)], fill=col, width=width)
    for k in range(1, rings + 1):
        rr = r * k / rings * rnd.uniform(0.95, 1.0)
        for i in range(len(angs) - 1):
            p0 = (cx + rr * math.cos(angs[i]), cy + rr * math.sin(angs[i]))
            p1 = (cx + rr * math.cos(angs[i + 1]), cy + rr * math.sin(angs[i + 1]))
            m = ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2)
            ctrl = (m[0] + (cx - m[0]) * 0.18, m[1] + (cy - m[1]) * 0.18)       # el hilo cuelga hacia el centro
            pts = [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * ctrl[0] + t * t * p1[0], (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * ctrl[1] + t * t * p1[1]) for t in [j / 12 for j in range(13)]]
            d.line(pts, fill=col, width=width)

def spider(size, body, mark):
    s = size
    t = Image.new("RGBA", (s * 4, s * 4), (0, 0, 0, 0))
    d = ImageDraw.Draw(t)
    c = s * 2
    for side in (-1, 1):
        for i, (a, b) in enumerate([(-50, -20), (-20, 10), (15, 40), (40, 70)]):
            kx = c + side * s * 0.9 * math.cos(math.radians(a)); ky = c + s * 0.9 * math.sin(math.radians(a)) - s * 0.2
            ex = c + side * s * 1.8 * math.cos(math.radians(b)); ey = c + s * 1.8 * math.sin(math.radians(b)) + s * 0.25
            d.line([(c + side * s * 0.15, c - s * 0.1), (kx, ky - s * 0.35), (ex, ey)], fill=body, width=max(2, s // 9))
    d.ellipse([c - s * 0.32, c - s * 0.75, c + s * 0.32, c - s * 0.1], fill=body)      # cabeza
    d.ellipse([c - s * 0.6, c - s * 0.2, c + s * 0.6, c + s * 1.1], fill=body)         # abdomen
    d.polygon([(c, c + s * 0.15), (c - s * 0.2, c + s * 0.42), (c, c + s * 0.5), (c + s * 0.2, c + s * 0.42)], fill=mark)   # reloj de arena
    d.polygon([(c, c + s * 0.85), (c - s * 0.2, c + s * 0.58), (c, c + s * 0.5), (c + s * 0.2, c + s * 0.58)], fill=mark)
    return t

def drips(img, y, col, hi, rnd):
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, y], fill=col)
    x = 0
    while x < W:
        w = rnd.randint(40, 120); L = max(w + 10, rnd.choice([rnd.randint(30, 80), rnd.randint(80, 240)]))
        d.rectangle([x, y - 2, x + w, y + L - w / 2], fill=col)
        d.ellipse([x, y + L - w, x + w, y + L], fill=col)
        d.ellipse([x + w * 0.28, y + L - w * 0.62, x + w * 0.4, y + L - w * 0.5], fill=hi)
        x += w + rnd.randint(-10, 60)

def make_widow(icon):
    rnd = random.Random(11)
    img = radial((96, 10, 26), (10, 2, 4), CX, CY, 1700).convert("RGBA")
    webs = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    web(webs, 0, 0, 1100, 0, math.pi / 2, 9, 9, (225, 210, 215, 120), 4, 1)
    web(webs, 2150, H, 900, -math.pi, 0, 13, 7, (225, 210, 215, 90), 3, 2)
    web(webs, W, 0, 900, math.pi / 2, math.pi, 8, 8, (225, 210, 215, 110), 4, 3)
    web(webs, CX, CY, 900, 0, 2 * math.pi, 25, 10, (230, 200, 210, 55), 3, 4)
    img.alpha_composite(webs)
    for _ in range(26):
        x, y = rnd.uniform(0, W), rnd.uniform(200, H - 160)
        if abs(x - CX) < 480 and abs(y - CY) < 480: continue
        if 2300 < x < 2880 and 300 < y < 1230: continue
        paste_rot(img, spider(rnd.randint(28, 60), (8, 4, 6, 255), (210, 20, 40, 255)), x, y, rnd.uniform(0, 360))
    # una araña colgando de su hilo junto al logo
    d = ImageDraw.Draw(img)
    d.line([1580, 0, 1580, 560], fill=(235, 225, 230, 200), width=3)
    paste_rot(img, spider(70, (8, 4, 6, 255), (220, 24, 44, 255)), 1580, 610, 180)
    drips(img, 70, (120, 6, 28, 255), (210, 80, 100, 255), rnd)
    border(img, H - 70, (120, 6, 28), (20, 4, 8))
    logo(img, icon, "WIDOW'S", "WINE", (150, 14, 36), (16, 3, 6), (14, 3, 6), (226, 40, 62), (250, 235, 238), (236, 228, 224),
         "GOUDYSTO.TTF", (230, 40, 70, 150))
    d = ImageDraw.Draw(img)
    f = font("CHILLER.TTF", 190)
    s = "A Bite to Remember..."
    d.text((CX - f.getlength(s) / 2, 160), s, font=f, fill=(240, 228, 232), stroke_width=5, stroke_fill=(70, 0, 12))
    nutrition(img, 2330, 330, "8", "Fermented nightshade, black widow venom (purified), crushed red grapes, cobweb extract, iron, garlic (trace).",
              (120, 6, 28), [("Total Fat", "0g", "0%"), ("Sodium", "88mg", "8%"), ("Total Carb", "8g", "8%"), ("Sugars", "8g", ""), ("Protein", "8g", "")])
    fb = font("CHILLER.TTF", 120)
    d.text((330, H - 210), "Only 8 Calories", font=fb, fill=(240, 220, 226), stroke_width=4, stroke_fill=(70, 0, 12))
    d.text((1300, H - 210), "12 fl oz (355 ml)", font=fb, fill=(240, 220, 226), stroke_width=4, stroke_fill=(70, 0, 12))
    return noise(img.convert("RGB"), 10, 5)

if __name__ == "__main__":
    out, icons = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    make_bandolier(Image.open(os.path.join(icons, "bandolier_icon.png")).convert("RGBA")).save(os.path.join(out, "bandolier_cover_albedo.png"))
    make_widow(Image.open(os.path.join(icons, "widowswine_icon.png")).convert("RGBA")).save(os.path.join(out, "widowswine_cover_albedo.png"))
    print("ok")
