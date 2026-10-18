# Tarkova-Cola 2.1.0

**Perk-a-Colas de Call of Duty Zombies para SPT** · *Call of Duty Zombies Perk-a-Colas for SPT*

Compatible con / Compatible with: **SPT 4.1.x** (single player; no se garantiza que funcione con Fika / not guaranteed to work with Fika, report issues on the SPT page or GitHub).
Opcional / Optional: **VisitAPI 1.3.5+** (para la historia y las misiones / for the storyline and quests).

---

## Español

### Novedades de la 2.1
- **Menú F12 en inglés** (o español): los nombres de las opciones siguen el idioma del mod (Auto = idioma del juego). Tu configuración se conserva.
- **Opción para ocultar los iconos de perks** de abajo de la pantalla (F12 > HUD > Mostrar iconos de perks).
- **Aviso de enemigos con perks:** un aviso cuando te acercas a un enemigo que lleva perks (distancia en F12).
- **Las perks aparecen en más sitios:** cajas de madera, cajas de munición, cajas de granadas (Widow's Wine / PHD Flopper), cajas de raciones (Juggernog), cuerpos de PMC y cajas fuertes (casi siempre). **Los jefes siempre llevan 2 perks**; algunos PMC, Rogues y (rara vez) Scavs llevan una. Las probabilidades están en `user/mods/TarkovaCola/loot.json`.
- **Arreglo:** las perks se añadían a contenedores equivocados (Scavs muertos y chaquetas en vez de cajas de munición y cajas fuertes).
- Los objetos de misión ahora tienen su aspecto (cuaderno, sobre, muestra...).

### Qué es
Bebidas que encuentras, compras o fabricas y que te dan una habilidad durante la raid. Cuanto más las usas, más sube la perk: se revelan desafíos y, al completarlos, desbloqueas **mejoras** (y sus **desventajas** ligadas) que eliges en la pantalla **Research**.

### Las 6 perks

| Perk | Efecto base (el resto de la raid) |
|---|---|
| **Speed Cola** | Recargas ×1.5 más rápidas |
| **Stamin-Up** | Estamina de piernas ×1.5 |
| **Juggernog** | −33 % de daño recibido |
| **PHD Flopper** | Inmune al daño de caídas y explosiones |
| **Widow's Wine** | Tus granadas atrapan a los enemigos en telarañas (−60 % de velocidad) |
| **Deadshot Daiquiri** | Apuntas un 15 % más rápido y el balanceo baja un 25 % |

Cada perk sube del nivel 1 al 5 con el XP de las raids en las que la llevas (+25 %). Cada nivel revela desafíos; cada desafío desbloquea una mejora y su desventaja. Equipas 1 mejora mayor + 1 menor (2 con la Ranura extra) y **cada mejora necesita su desventaja equipada** (mayor con mayor, menor con menor).

### Modos (interruptor en Research)
- **Moderno**: Research completo, todas las perks que quieras.
  - **Eternal Perk**: si bebes una perk que ya llevas, se vuelve **eterna** (contorno azul): se queda contigo entre raids, pierde una desventaja y, si mueres, su botella vuelve por correo. Pestaña **ETERNAS** en Research.
- **Clásico**: sin Research ni desventajas. Hasta 4 perks + 1 **modificador** (efecto reducido). Cada perk trae 1 mejora menor al azar y **se quedan contigo hasta que mueras**. Cada raid trae un desafío opcional que da una 2.ª mejora.

### Perk aleatoria
Al matar hay un 4 % de que caiga una **botella flotante** (como mucho una por raid, 60 s). Recógela: perk al azar, o eterna si ya la llevabas. Con su voz de locutor.

### Sinergias
15 sinergias, una por cada pareja de perks: un bono pequeño cuando llevas las dos a la vez. Se desbloquean con su desafío y **solo se lleva una** equipada (pestaña **SINERGIA**).

### Historia: los Embotelladores (requiere VisitAPI)
10 misiones de **Skier** y **Therapist** (desde el nivel 12):
- **3 de introducción**: desbloquean las **recetas** de las perks en la **Unidad de nutrición** del refugio.
- **6, una por perk**: desbloquean la **venta** de esa perk en Therapist. Los guardias llevan la perk de la misión de verdad (y su icono sobre la cabeza).
- **Final**: el **Barman**, con todas las perks (4 en modo Clásico).

Con VisitAPI instalado, **las perks no se venden en Therapist hasta completar su misión** (se siguen encontrando en raid y fabricando). Sin VisitAPI no hay misiones y todo se vende desde el principio. Opción de depuración en F12: *Perks a la venta sin misiones*.

### Logros
**100 logros nativos** de EFT (Perfil > Logros), con recompensas por correo.

### Cómo se usa
1. Consigue una perk (Therapist, raid, refugio).
2. Menú principal: botón **RESEARCH** (barra inferior) o **F9** para ver el progreso, equipar mejoras, sinergias y cambiar de modo.
3. Bébela en raid. Con **ESC** en raid ves tu nivel y tus desafíos.

### Instalación
Extrae el zip **encima de la carpeta de SPT** (donde está `EscapeFromTarkov.exe`). Crea:
- `BepInEx/plugins/TarkovaCola/` (cliente)
- `SPT_Runtime/user/mods/TarkovaCola/` (servidor)
- `SPT_Runtime/user/mods/VisitAPI-Server/packs/TarkovaCola/` (misiones; solo hacen algo si tienes VisitAPI)

El progreso se guarda por perfil en `user/mods/TarkovaCola/profiles/` (no lo borres al actualizar). **Actualizando desde la 1.x:** tu progreso se conserva.

### Configuración (F12)
Idioma, tecla de Research, HUD (iconos abajo en el centro), volumen de canciones y voces, efectos visuales de PHD y de la telaraña, probabilidad de la perk aleatoria, "abandonar la raid no cuenta como muerte", opciones de depuración (guardias siempre, perks a la venta sin misiones, registro detallado) y tecla F10 para copiar coordenadas.

### Desinstalación
Borra las carpetas `TarkovaCola` (y el pack de VisitAPI). Si te quedan perks en el alijo, SPT permite eliminar los items rotos.

---

## English

### What's new in 2.1
- **F12 menu in English** (or Spanish): option names follow the mod language (Auto = game language). Your saved settings are kept.
- **Option to hide the perk icons** at the bottom of the screen (F12 > HUD > Show perk icons).
- **Perk carrier alert:** a notice when you get close to an enemy carrying perks (range in F12).
- **Perks show up in more places:** wooden crates, ammo boxes, grenade boxes (Widow's Wine / PHD Flopper), ration crates (Juggernog), PMC bodies and safes (almost always). **Bosses always carry 2 perks**; some PMCs, Rogues and (rarely) Scavs carry one. Chances are in `user/mods/TarkovaCola/loot.json`.
- **Fix:** perks were added to the wrong containers (dead Scavs and jackets instead of ammo boxes and safes).
- Quest items now look like what they are (notebook, envelope, sample...).

### What it is
Drinks you find, buy or craft that grant an in-raid ability. The more you use them, the more the perk levels up: challenges are revealed and completing them unlocks **augments** (and their linked **drawbacks**) that you equip in the **Research** screen.

### The 6 perks

| Perk | Base effect (rest of the raid) |
|---|---|
| **Speed Cola** | Reloads ×1.5 faster |
| **Stamin-Up** | Leg stamina ×1.5 |
| **Juggernog** | −33% damage taken |
| **PHD Flopper** | Immune to fall and explosion damage |
| **Widow's Wine** | Your grenades web enemies (−60% movement speed) |
| **Deadshot Daiquiri** | Aim 15% faster and 25% less weapon sway |

Each perk levels 1 → 5 with the XP of the raids you carry it in (+25%). Each level reveals challenges; each challenge unlocks an augment and its drawback. You equip 1 major + 1 minor augment (2 with Extra Slot) and **every augment needs its drawback equipped** (major with major, minor with minor).

### Modes (switch in Research)
- **Modern**: full Research, as many perks as you like.
  - **Eternal Perk**: drink a perk you already have and it becomes **eternal** (blue outline): it stays with you between raids, loses one drawback and, if you die, its bottle comes back by mail. **ETERNAL** tab in Research.
- **Classic**: no Research, no drawbacks. Up to 4 perks + 1 **modifier** (reduced effect). Each perk rolls 1 random minor augment and **they stay with you until you die**. Every raid brings an optional challenge that grants a 2nd augment.

### Random perk
Kills have a 4% chance to drop a **floating bottle** (at most one per raid, 60 s). Grab it: a random perk, or an eternal one if you already had it. With announcer voice.

### Synergies
15 synergies, one per pair of perks: a small bonus when you carry both. Unlocked with their own challenge, **only one equipped** at a time (**SYNERGY** tab).

### Storyline: the Bottlers (requires VisitAPI)
10 quests from **Skier** and **Therapist** (from level 12):
- **3 intro quests**: unlock the perk **recipes** in the hideout **Nutrition Unit**.
- **6, one per perk**: unlock that perk's **sale** at Therapist. The guards really carry the quest's perk (with its icon over their heads).
- **Final**: **the Bartender**, with every perk (4 in Classic mode).

With VisitAPI installed, **perks are not sold by Therapist until you finish their quest** (they can still be found in raid and crafted). Without VisitAPI there are no quests and everything is sold from the start. Debug option in F12: *Perks for sale without quests*.

### Achievements
**100 native EFT achievements** (Profile > Achievements), rewards by mail.

### How to use
1. Get a perk (Therapist, raid, hideout).
2. Main menu: **RESEARCH** button (bottom bar) or **F9** to see progress, equip augments and synergies, and switch modes.
3. Drink it in raid. Press **ESC** in raid to see your level and challenges.

### Installation
Extract the zip **over your SPT folder** (the one with `EscapeFromTarkov.exe`). It creates:
- `BepInEx/plugins/TarkovaCola/` (client)
- `SPT_Runtime/user/mods/TarkovaCola/` (server)
- `SPT_Runtime/user/mods/VisitAPI-Server/packs/TarkovaCola/` (quests; only active with VisitAPI)

Progress is saved per profile in `user/mods/TarkovaCola/profiles/` (keep it when updating). **Updating from 1.x:** your progress is kept.

### Configuration (F12)
Language, Research hotkey, HUD (icons bottom-center), jingle and voice volume, PHD and web visual effects, random perk chance, "leaving the raid does not count as dying", debug options (guards always, perks for sale without quests, verbose log) and F10 to copy coordinates.

### Uninstall
Delete the `TarkovaCola` folders (and the VisitAPI pack). If you still hold perks in your stash, SPT lets you remove broken items.

---

MIT License (código / code)

**Créditos / Credits**
- Speed Cola 3D model: "Speedcola CoD Zombies" by **albertjuli** ([Sketchfab](https://skfb.ly/6VRDU)), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Converted to a Unity asset bundle, textures re-prepared, rescaled and rotated.
- Other bottles (Stamin-Up, Juggernog, PHD Flopper, Widow's Wine, Deadshot Daiquiri): "cod zombies perks" by **Unknown Dev** ([Sketchfab](https://sketchfab.com/3d-models/cod-zombies-perks-77e9cb71adaf44b5a974c584f96cfc6c)), Sketchfab Free Standard license. Only the compiled asset bundles are included, not the original model files.
- Icons, jingles and announcer voice lines: Call of Duty, property of their owners (not MIT).
