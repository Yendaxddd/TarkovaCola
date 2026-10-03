# Tarkova-Cola 1.2.0

**Perk-a-Colas de Call of Duty Zombies para SPT** · *Call of Duty Zombies Perk-a-Colas for SPT*

Compatible con / Compatible with: **SPT 4.1.x** (single player, sin Fika / no Fika)

---

## Español

### Qué es
Bebidas que encuentras (o compras) y que te dan una habilidad durante la raid. Cuanto más las usas, más subes la perk, desbloqueas desafíos y, con ellos, **mejoras** (y sus **desventajas**), que eliges en un menú nativo de EFT.

Perks disponibles: **Speed Cola** y **Stamin-Up**.

### Speed Cola
- Se vende en **Therapist** (nivel de lealtad 1, **7 GP Coin**). Ocupa 1×2 casillas.
- También aparece al azar en raid: **cajas de munición**, **cajas fuertes**, y en **Rogues** y **Killa**.
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

### Stamin-Up
- Se vende en **Therapist** (nivel de lealtad 1, **7 GP Coin**) y aparece en los mismos sitios que Speed Cola. Ocupa 1×2 casillas.
- Al beberla en raid: **estamina de piernas x1.5** el resto de la raid.
- Misma progresión que Speed Cola (niveles 1 → 5, +25 % de XP mientras está activa), con sus propias mejoras y desventajas:

| Mejora | Efecto | Desafío | Desventaja ligada |
|---|---|---|---|
| Rush Hour (mayor) | Tras una baja, la estamina de piernas se llena | 3 bajas en menos de 3 minutos | Loud Boots: correr se oye a 25 m |
| Sprint Shooter (mayor) | Las animaciones del arma van siempre x1.75 más rápido | Encontrar 15 Hot Rod / RatCola / Max Energy | Burnout: con menos del 30 % de estamina te recuperas a la mitad |
| Second Wind (mayor) | Cada vez que la salud esté en rojo: 5 s de estamina infinita e inmunidad al dolor (sin cooldown) | Quedarte sin estamina 5 veces | Jelly Legs: balanceo del arma +25 % tras correr 5 s |
| Light Feet (menor) | -10 % de peso | Correr 5 km en total | Hungry Legs: +50 % de gasto de energía al correr |
| Quick Recovery (menor) | La estamina se recupera +25 % más rápido | Correr 10 km en total | Cramps: la recuperación tarda 2 s más tras correr |
| Soft Landing (menor) | -30 % de daño por caída | Matar a 3 enemigos al menos 2 m por debajo de ti | Sweaty: +50 % de gasto de hidratación al correr |
| Extra Slot | 2 mejoras menores | Matar a 1 boss | ninguna |

### Logros
Doce logros nativos de EFT (dos secretos), en tu perfil, apartado **Logros**. Cada uno envía su recompensa (rublos, GP Coin o Speed Cola) a tu correo, una sola vez.

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

Perks available: **Speed Cola** and **Stamin-Up**.

### Speed Cola
- Sold by **Therapist** (loyalty level 1, **7 GP Coin**). Takes 1×2 slots.
- Also found randomly in raid: **ammo crates**, **safes**, and on **Rogues** and **Killa**.
- Drink it in raid: **reloads 1.5x faster** for the rest of the raid.
- Levels 1 → 5 with the XP you earn in raid (+25% while the perk is active). Max level is 5.
- Each level reveals challenges; completing one unlocks an **augment** and its linked **drawback** (see the table above).

You equip 1 major + 1 minor augment (2 minors with Extra Slot) and **1 major + 1 minor drawback**. You can't carry a major augment without a major drawback.

### Stamin-Up
- Sold by **Therapist** (loyalty level 1, **7 GP Coin**) and found in the same places as Speed Cola. Takes 1×2 slots.
- Drink it in raid: **leg stamina x1.5** for the rest of the raid.
- Same progression as Speed Cola (levels 1 → 5, +25% XP while active), with its own augments and drawbacks:

| Augment | Effect | Challenge | Linked drawback |
|---|---|---|---|
| Rush Hour (major) | After a kill, leg stamina refills completely | 3 kills in under 3 minutes | Loud Boots: sprinting is audible to bots from 25 m |
| Sprint Shooter (major) | Weapon animations always 1.75x faster | Find 15 Hot Rod / RatCola / Max Energy | Burnout: below 30% stamina you recover at half speed |
| Second Wind (major) | Every time your health is in the red: 5 s of infinite stamina and pain immunity (no cooldown) | Run out of stamina 5 times | Jelly Legs: weapon sway +25% after sprinting 5 s |
| Light Feet (minor) | -10% carried weight | Sprint 5 km in total | Hungry Legs: +50% energy burn while sprinting |
| Quick Recovery (minor) | Stamina recovers +25% faster | Sprint 10 km in total | Cramps: stamina recovery waits 2 s more after sprinting |
| Soft Landing (minor) | -30% fall damage | Kill 3 enemies at least 2 m below you | Sweaty: +50% hydration burn while sprinting |
| Extra Slot | 2 minor augments | Kill 1 boss | none |

### Achievements
Twelve native EFT achievements (two are secret), shown in your profile under **Achievements**. Each one sends its reward (roubles, GP Coin or Speed Cola) to your mail, once.

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
- Iconos y canciones / Icons and jingles: Call of Duty, property of their owners (not MIT).
- Botella de Stamin-Up / Stamin-Up bottle: "cod zombies perks" by **Unknown Dev** ([Sketchfab](https://sketchfab.com/3d-models/cod-zombies-perks-77e9cb71adaf44b5a974c584f96cfc6c)), Sketchfab Free Standard license. Only the compiled asset bundle is included, not the original model files.
