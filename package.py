"""Empaqueta Tarkova-Cola para distribuirlo.

Uso:  python package.py            (compila cliente y servidor en Release y genera dist/)

Genera:
  dist/TarkovaCola-<version>.zip          -> se extrae encima de la carpeta de SPT
        BepInEx/plugins/TarkovaCola/      cliente (dll + iconos + bundle del modelo en mano)
        SPT_Runtime/user/mods/TarkovaCola/        servidor (dll + bundles.json + bundle del item)
        README.md
  dist/TarkovaCola-<version>-src.zip      -> codigo fuente (sin binarios ni la cache de Unity), como copia de seguridad
"""
import pathlib, subprocess, sys, zipfile, re

ROOT = pathlib.Path(__file__).parent.resolve()
DIST = ROOT / "dist"
VERSION = re.search(r'"Tarkova-Cola",\s*"([\d.]+)"', (ROOT / "Client" / "Plugin.cs").read_text(encoding="utf-8")).group(1)


def build(project):
    print("compilando", project)
    r = subprocess.run(["dotnet", "build", str(ROOT / project), "-c", "Release", "-v", "q"], capture_output=True, text=True)
    if r.returncode != 0:
        print(r.stdout[-2000:]); sys.exit("fallo la compilacion de " + project)


def add(z, src, arc):
    z.write(src, arc.replace("\\", "/"))       # rutas con '/', como esperan todos los descompresores


def main():
    build("Client"); build("Server")
    DIST.mkdir(exist_ok=True)

    client_dll = ROOT / "Client" / "bin" / "Release" / "net472" / "TarkovaCola.Client.dll"
    server_dll = ROOT / "Server" / "bin" / "Release" / "net9.0" / "TarkovaCola.Server.dll"
    assets = ROOT / "Client" / "assets"
    modfiles = ROOT / "Server" / "ModFiles"

    out = DIST / f"TarkovaCola-{VERSION}.zip"
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        add(z, client_dll, "BepInEx/plugins/TarkovaCola/TarkovaCola.Client.dll")
        for f in assets.rglob("*"):
            if f.is_file(): add(z, f, "BepInEx/plugins/TarkovaCola/assets/" + str(f.relative_to(assets)))
        add(z, server_dll, "SPT_Runtime/user/mods/TarkovaCola/TarkovaCola.Server.dll")
        for f in modfiles.rglob("*"):
            if f.is_file(): add(z, f, "SPT_Runtime/user/mods/TarkovaCola/" + str(f.relative_to(modfiles)))
        add(z, ROOT / "Package" / "README.md", "README.md")
    print("->", out, round(out.stat().st_size / 1e6, 1), "MB")

    # copia del codigo fuente
    skip_dirs = {"bin", "obj", "Library", "Temp", "Logs", "Build", "Preview", "dist", ".vs", "profiles"}
    skip_ext = {".log", ".bak-4.1.2"}
    src = DIST / f"TarkovaCola-{VERSION}-src.zip"
    with zipfile.ZipFile(src, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for f in ROOT.rglob("*"):
            if not f.is_file(): continue
            rel = f.relative_to(ROOT)
            if any(p in skip_dirs for p in rel.parts) or f.suffix in skip_ext: continue
            add(z, f, "TarkovaCola-src/" + str(rel))
    print("->", src, round(src.stat().st_size / 1e6, 1), "MB")


main()
