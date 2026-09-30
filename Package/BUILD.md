

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

### Material que no incluye el repo / Not included in this repo
El modelo 3D de la botella, sus texturas y los iconos de Call of Duty son de terceros y no se redistribuyen aquí (ver `Client/assets/README.txt`).
Para regenerar el modelo: `python prepare_model.py <carpeta_del_modelo>` y `./build_bundle.ps1`.

The bottle 3D model, its textures and the Call of Duty icons are third-party material and are not redistributed here.

### Estructura / Layout
- `Client/` plugin de BepInEx (parches Harmony, pantalla nativa uGUI, HUD, efectos de las mejoras)
- `Server/` mod de servidor de SPT (item, tienda, guardado de progreso por perfil)
- `UnityBundle/` proyecto de Unity que construye el asset bundle del item
- `UI/` maqueta HTML original de la pantalla (referencia de diseño)
