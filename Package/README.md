# Tarkova-Cola 1.0.0

**Perk-a-Colas de Call of Duty Zombies para SPT** · *Call of Duty Zombies Perk-a-Colas for SPT*

Compatible con / Compatible with: **SPT 4.1.x** (single player, sin Fika / no Fika)

---

## Español

### Qué es
Bebidas que encuentras (o compras) y que te dan una habilidad durante la raid. Cuanto más las usas, más subes la perk, desbloqueas desafíos y, con ellos, **mejoras** (y sus **desventajas**), que eliges en un menú nativo de EFT.

Ahora mismo hay una perk: **Speed Cola**.

### Speed Cola
- Se vende en **Therapist** (nivel de lealtad 1, 8 000 ₽). Ocupa 1×2 casillas.
- Al beberla en raid: **recargas ×1.5 más rápidas** el resto de la raid.
- Sube de nivel (1 → 5) con el XP que ganas en raid (+25 % mientras la perk está activa). El nivel máximo es 5.
- Cada nivel revela desafíos; al completar uno se desbloquea una **mejora** y una **desventaja** ligada.

| Mejora | Efecto | Desventaja ligada | Efecto |
|---|---|---|---|
| Quick Hands | sacar/cambiar de arma +50 % | Jittery Hands | +25 % de balanceo con un brazo no sano |
| Bolt Runner | el arma encasquillada se desencasquilla al guardarla | Overpressure | cada 4.º cargador pierde sus balas (vuelven al inventario) |
| Sugar Rush | recarga de Speed Cola ×2.0 en vez de ×1.5 | Sugar Crash | +sed cada 2 min sin matar (máx. ×2) |
| Steady Fingers | −30 % de daño en brazos | Dry Mouth | −20 % de hidratación más rápido |
| Chamber Check | revisar cargadores en inventario +75 % | Loud Slurp | beber hace ruido (30 m) |
| Mag Juggler | cargar cargadores del arma principal +50 % | Sticky Mags | +10 % de encasquillamiento |
| Round Counter | los cargadores muestran su munición exacta | Sweet Tooth | comer/beber otra cosa tarda el doble |
| Extra Slot | permite llevar 2 mejoras secundarias | — | — |

Equipas 1 mejora principal + 1 secundaria (2 con Extra Slot) y **1 desventaja principal + 1 secundaria**. No puedes llevar una mejora principal sin su desventaja principal.

### Cómo se usa
1. Compra una Speed Cola a Therapist.
2. En el menú principal pulsa **RESEARCH** (barra inferior, junto a Handbook) o **F9** para ver el progreso y equipar mejoras.
3. Bébela dentro de la raid. Abre el menú (ESC) en raid para ver tu nivel y los desafíos pendientes.

### Instalación
Extrae el zip **encima de la carpeta de SPT** (donde está `EscapeFromTarkov.exe`). Crea:
- `BepInEx/plugins/TarkovaCola/` (cliente)
- `SPT_Runtime/user/mods/TarkovaCola/` (servidor)

El progreso se guarda por perfil en `user/mods/TarkovaCola/profiles/` (no lo borres al actualizar).

### Configuración (F12, ConfigurationManager)
Idioma (Auto / Español / English), tecla de Research, posición del icono, tamaño de la lata en la mano, registro de depuración, etc.

### Desinstalación
Borra las dos carpetas `TarkovaCola`. Si tenías Speed Cola en el alijo, SPT permite eliminar los items rotos del inventario.

---

## English

### What it is
Drinks you find or buy that grant an in-raid ability. The more you use them, the more the perk levels up, unlocking challenges and, with them, **augments** (and linked **drawbacks**) that you pick in a native EFT-style menu.

There is one perk for now: **Speed Cola**.

### Speed Cola
- Sold by **Therapist** (loyalty level 1, 8,000 ₽). Takes 1×2 slots.
- Drink it in raid: **reloads 1.5x faster** for the rest of the raid.
- Levels 1 → 5 with the XP you earn in raid (+25% while the perk is active). Max level is 5.
- Each level reveals challenges; completing one unlocks an **augment** and its linked **drawback** (see the table above).

You equip 1 major + 1 minor augment (2 minors with Extra Slot) and **1 major + 1 minor drawback**. You can't carry a major augment without a major drawback.

### How to use
1. Buy Speed Cola from Therapist.
2. In the main menu press **RESEARCH** (bottom bar, next to Handbook) or **F9** to see progress and equip augments.
3. Drink it inside a raid. Open the pause menu (ESC) in raid to see your level and pending challenges.

### Installation
Extract the zip **over your SPT folder** (the one with `EscapeFromTarkov.exe`). It creates:
- `BepInEx/plugins/TarkovaCola/` (client)
- `SPT_Runtime/user/mods/TarkovaCola/` (server)

Progress is saved per profile in `user/mods/TarkovaCola/profiles/` (keep it when updating).

### Configuration (F12, ConfigurationManager)
Language (Auto / Español / English), Research hotkey, icon position, hand-model size, debug logging, etc.

### Uninstall
Delete both `TarkovaCola` folders. If you still hold Speed Cola in your stash, SPT lets you remove broken items from the inventory.

---

MIT License (código / code)

**Créditos / Credits**
- Modelo 3D / 3D model: "Speedcola CoD Zombies" by **albertjuli** ([Sketchfab](https://skfb.ly/6VRDU)), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Cambios / Changes: converted to an Unity asset bundle, textures re-prepared (metal/smoothness adjusted), rescaled and rotated for in-game use.
- Iconos y cancion / Icons and jingle: Call of Duty, property of their owners (not MIT).
