

---

## Compilar desde el código / Building from source

**Requisitos / Requirements:** .NET SDK 9 (con el runtime 10 instalado: el servidor de SPT 4.1.x va contra .NET 10), SPT 4.1.x instalado, Unity **2022.3.43f1** (solo para regenerar el modelo), Python 3 + Pillow.

Esta carpeta debe vivir **dentro de la carpeta de SPT** (`<SPT>/TarkovaCola-src`): los `.csproj` buscan `EscapeFromTarkov_Data`, `BepInEx` y `SPT_Runtime` un nivel por encima.
This folder must live **inside your SPT folder** (`<SPT>/TarkovaCola-src`): the `.csproj` files look for `EscapeFromTarkov_Data`, `BepInEx` and `SPT_Runtime` one level up.

```
dotnet build Client -c Release     # plugin de BepInEx -> BepInEx/plugins/TarkovaCola
dotnet build Server -c Release     # mod de servidor  -> SPT_Runtime/user/mods/TarkovaCola
python package.py                  # genera dist/TarkovaCola-<version>.zip
```

Cada `dotnet build` copia el resultado a la carpeta del juego. / Every build deploys into the game folder.

### Material de terceros incluido / Third-party material included
El repo incluye, por comodidad, el modelo 3D de la botella (Sketchfab), sus texturas y los iconos de Call of Duty. **No** están cubiertos por la licencia MIT y siguen siendo de sus autores; si eres el autor y quieres que se retiren, abre un issue (ver `LICENSE`).
Para regenerar el modelo: `python prepare_model.py <carpeta_del_modelo>` y `./build_bundle.ps1`.

The repo bundles, for convenience, the bottle 3D model (Sketchfab), its textures and the Call of Duty icons. They are **not** covered by the MIT license and remain the property of their owners; if you are an owner and want them removed, open an issue (see `LICENSE`).

### Estructura / Layout
- `Client/` plugin de BepInEx (parches Harmony, pantalla nativa uGUI, HUD, efectos de las mejoras)
- `Server/` mod de servidor de SPT (item, tienda, guardado de progreso por perfil)
- `UnityBundle/` proyecto de Unity que construye el asset bundle del item
- `UI/` maqueta HTML original de la pantalla (referencia de diseño)
