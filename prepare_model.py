"""Copia el modelo de Speed Cola (FBX + texturas PBR) al proyecto Unity y prepara las texturas.
Uso:  python prepare_model.py [carpeta_del_modelo]
Espera la estructura  <carpeta>/source/*.fbx  y  <carpeta>/textures/*_albedo|AO|emissive|metallic|normal|roughness.*
Unity 'Standard' quiere metalico (R) + suavidad (A) en un solo mapa; aqui se construye a partir de metallic y roughness.
"""
import pathlib, shutil, sys
from PIL import Image, ImageOps

src = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\Scorp\Downloads\speedcola-cod-zombies")
dst = pathlib.Path(__file__).parent / "UnityBundle" / "Assets" / "Models" / "cola"
dst.mkdir(parents=True, exist_ok=True)

fbx = next((src / "source").glob("*.fbx"))
shutil.copy(fbx, dst / "cola.fbx")

tex = src / "textures"
find = lambda key: next(p for p in tex.iterdir() if key in p.name.lower() and "scattering" not in p.name.lower())
SIZE = 2048

def load(key, mode):
    im = Image.open(find(key)).convert(mode)
    return im.resize((SIZE, SIZE), Image.LANCZOS) if im.size != (SIZE, SIZE) else im

load("albedo", "RGB").save(dst / "cola_albedo.png")
load("normal", "RGB").save(dst / "cola_normal.png")
load("ao", "L").save(dst / "cola_ao.png")
load("emissive", "RGB").save(dst / "cola_emissive.png")

METAL_SCALE = 0.30   # el metal completo se ve casi negro en el render de iconos del inventario (sin reflejos del entorno)
metal = load("metallic", "L").point(lambda v: int(v * METAL_SCALE))
smooth = ImageOps.invert(load("roughness", "L")).point(lambda v: int(v * 0.6))   # menos brillo especular: mas legible en miniatura
r = metal
Image.merge("RGBA", (r, r, r, smooth)).save(dst / "cola_metal_smooth.png")

print("Modelo preparado en", dst)
for p in sorted(dst.iterdir()):
    print(" ", p.name, p.stat().st_size // 1024, "KB")
