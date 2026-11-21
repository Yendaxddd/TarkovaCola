# Investigación: animaciones y modelos 3D en mods de SPT 4.1.6

Fecha: 2026-10-30. Fuentes:
- Los 293 DLL de `BepInEx/plugins`, revisados todos por cadenas de la API de animación.
- 10 mods decompilados con ilspycmd: WatchAnims, CSGas, WTT-ClientCommonLib, WTT-ContentBackport, WTT-Armory, FoldThatStock, PaPHideoutModule, LoadAmmoAnim, Smoothie, Mortar.
- Inventario de REA de ManimalIcebreaker (evidencia `ev_6fb52045…`).
- La wiki oficial (github.com/sp-tarkov/wiki).
- La guía "How to SDK Creating Custom Weapon".
- Los bundles de EFT de la HK416 y la MP5, abiertos con UnityPy.

---

## 1. Qué hace cada mod y cómo (lo que nos sirve)

| Mod | Qué hace | Técnica |
|---|---|---|
| **Manimal.WatchAnims** (Manimal, 22 KB) | Animación nueva de mirar el reloj | **AnimatorOverrideController** sobre el controlador de los brazos (ver 2.A). |
| **CSGas** (Manimal) | Animación de toser en el gas | La misma técnica que WatchAnims: el clip de tos sustituye a `hand_nv_on`. |
| **FoldThatStock** | Animación de plegar la culata | **PlayableGraph**: aplica sobre los brazos un clip "donante" de otra arma de EFT y lo avanza a mano (`Evaluate`) cada frame (ver 2.B). |
| **LoadAmmoAnim** (Manimal) | Animación de cargar balas | Un objeto propio con su **Animator** dentro de un bundle (`BundleAnimator.Play("OUT TO USE S")`). |
| **WTT-Armory / ClientCommonLib** | Armas nuevas completas | Cada arma es un **contenedor** con su controlador de animación y sus `StaticData` (ver 2.C). Los DLL solo cargan recursos; la animación va dentro del bundle. |
| **WTT-ContentBackport** | Maniquíes del refugio | Carga un `RuntimeAnimatorController` de un bundle y se lo pone a un Animator. |
| **ManimalIcebreaker** (10 MB) | Mapa (puertas, tormenta, paneles) | Usa `AnimationSystem.Types` de EFT y sonido PCM incrustado como recurso. |
| **PaPHideoutModule** (Kennerz) | **Pack-a-Punch en el refugio**: camuflaje, brillo, bonificaciones, gestos | Tiene API pública (`PaPApi`). **Posible integración con Tarkova-Cola.** |
| Mod de cuerpo a cuerpo con animaciones de MW | En desarrollo, no publicado | Demuestra que la técnica 2.A/2.C funciona con animaciones de CoD. |

Ninguno de estos mods mueve huesos a mano después de que EFT anima, que es lo que hace nuestro `CodDrink.cs`. **Todos hacen que el Animator de EFT reproduzca clips de verdad.** Por eso a ellos les queda bien y a nosotros no.

## 2. Las tres técnicas

### A. AnimatorOverrideController (WatchAnims, CSGas)
1. Los clips (`AnimationClip`) se hacen en Unity **sobre el esqueleto de brazos de EFT** (rutas `Base HumanL…`, `Camera_animated`, `Weapon_root…`) y van en un bundle propio.
2. En el juego: `animator = player.HandsController.FirearmsAnimator.Animator`.
3. Se envuelve `animator.runtimeAnimatorController` en un `new AnimatorOverrideController(base)` y se guarda por animator.
4. `GetOverrides`/`ApplyOverrides`: el clip de un estado que ya existe (`hand_nv_on`) se cambia por el nuestro.
5. `animator.Play("hand_nv_on", -1, 0)` y una corrutina sube y baja el peso de la capa (fundido de entrada y salida). Al terminar se quita el override.
6. Extra: un parche a `ProceduralWeaponAnimation.ApplyComplexRotation` lleva `WeaponRootAnim` a la rotación neutra durante el fundido, para que el balanceo no estropee la pose.

**Ventaja:** se mantiene toda la lógica de EFT (estados, transiciones, eventos, red, IK). Solo cambia el movimiento.

### B. PlayableGraph con clip donante (FoldThatStock)
`PlayableGraph.Create` → `AnimationClipPlayable.Create(graph, clip)` → `AnimationPlayableOutput.Create(graph, …, animator)` → `SetTime(t)` + `graph.Evaluate(0)` cada frame. Así aplica un clip que no está en el controlador del arma actual; puede ser un clip de EFT de otra arma, encontrado con `Resources.FindObjectsOfTypeAll<AnimationClip>()` o en su bundle. Sirve para acciones sueltas sin estado propio.

### C. Arma con su propio controlador (WTT, guía del SDK)
- `WeaponPrefab._originalAnimatorController` (más las variantes `_simple` y `_spirit`) es el controlador de primera persona del arma.
- Capas: Base, **Hands**, **LActions** (acciones de la mano izquierda), Firemode, Stock, Hammer y Malfunction.
- `AnimatorControllerStaticData`: eventos por estado (`Sound`, `UseProp`, cargador fuera/dentro, ...) en tiempo normalizado (78,1 % → 0.781). Cada estado con eventos lleva un `AnimationEventsStateBehaviour`.
- La guía recomienda **no hacer el controlador desde cero** ("una telaraña de estados"): sacarlo con **AssetRipper** de un arma de EFT y modificarlo.
- Animar en **Blender** sobre el rig de primera persona de EFT. Exportar FBX con *CustomBlenderFBXExporter*, que quita el prefijo "Armature". En Unity, importar como Generic, crear el avatar a partir del modelo y sin resample.
- Plantilla en el SDK: `UnityWeaponsSdk/Assets/Animation & StatiData template` (`Template Animator.controller`, `Template StaticData.asset`, `Left_hand_actions.mask`, `weapon container template.prefab`).

## 3. Cómo anima EFT un arma (bundles de la HK416 y la MP5)
- El contenedor de la HK416 lleva **1 AnimatorController** (`weapon_hk_416a5_556x45`) y 2 overrides generados (`_simple`, `_spirit`).
- Los clips de la MP5 (102) están en `mp5/client_assets.bundle`.
- **La recarga no es un clip: son piezas.** `mag_out_0/1/2`, `mag_out_all`, `mag_in_0/1/2`, `reload_catch_0/2`, `reload_charge`, `bolt_open`, `catch_on`.
- El resto de estados:
  - Sacar y guardar: `out_to_idle_arm` / `idle_to_out`, `hands_in/out`.
  - Inspeccionar: `look`.
  - Comprobar cargador: `check_all`, `check_chamber`.
  - Disparo: `fire`, `fire_dry`.
  - Encasquillamientos: `jam_*`, `misfire`.
  - Modos de fuego: `firemode_*`.
  - Patrulla: `patrol_*`.
  - Reposo: `idle_armed`, `too_idle_*`.
- **Estado actual:** la MK35 y la VMP usan tal cual el contenedor de la HK416 y la MP5. Se ven sus animaciones, no las de BO7.

## 4. Material de BO7 que ya tenemos (exportado por el usuario)
`Downloads/saluki/exported_files/bo7/animations` (formato `.cast`, 332 en total):
- **MK35 (`vm_ar_finch_*`, 177):**
  - `reload`, `reload_empty`, con variantes fast/ext/flipmag y ads.
  - `inspect`, `inspect_empty`.
  - `raise`, `raise_first`, `drop`.
  - `fire`, `fire_ads`, `fire_last`.
  - `idle`, `sprint_*`, `walk_*`, `melee_*`, `selectsemi_on/off`.
- **VMP (`vm_sm_civet_*`, 90):** lo mismo.
- **Cuerpo a cuerpo:**
  - Puños: `vm_p55_me_bgloves_l/r_melee_hit/miss/heavy_01-03`.
  - Cuchillo: `viewmodel_knife_melee_stab_*`.
  - Golpes con arma: `vm_p56_ar_malima_melee_*`, `vm_p55_pi_mike2011_melee_*`.
- **Perks:** `vm_gesture_zm_perk_can_drink` (+ `_rh`).
- Ya tenemos lector de `.cast` (`tools/cast2json.py`), cinemática directa de CoD (`tools/cod_fk.py`), esqueleto de los brazos de CoD (`Assets/Anim/viewhands_skel.json`) y el retarget al rig de EFT funcionando en el laboratorio (`UnityBundle/Assets/Editor/EftCod.cs`).

## 5. Wiki de SPT 4.1
- La wiki es corta. Lo útil:
  - `Client_Modding_Quick_Guide`: BepInEx + `SPT.Reflection.Patching.ModulePatch`.
  - `debug_dnSpy`: depurar el cliente.
  - `WTT_Vol1`: objetos estáticos.
  - `Class_Name_Mappings` (4.1): nombres de clases de EFT, que ahora vienen desofuscados.
  - `EnumExtensions`.
  - `Server_40_to_41`: cambios del servidor.
- La 4.1 trae el cliente **desofuscado** (nombres reales de clases y métodos) y *reverse patching* en cliente y servidor.
- Recursos enlazados:
  - Base de datos de objetos de SPT.
  - Documentación técnica del servidor (deepwiki sp-tarkov/server-csharp).
  - Ejemplos de mods de servidor.
  - Discord de WTT (armas).
  - Guía del SDK de armas (resumida en 2.C).

---

## 6. Plan propuesto

### Fase 0: herramientas (gratis)
- **AssetRipper**: saca el controlador de la HK416 y la MP5, sus `StaticData` y sus clips como assets editables de Unity, para ver en el editor los estados y en qué % cae cada evento.
- **Blender + addon Cast** (DTZxPorter): abre los `.cast` de BO7 con su esqueleto, para ver y retocar a mano. Es opcional: el retarget se puede automatizar en Unity.
- (Ya tenemos Unity 2022.3.43f1, el WTT SDK y el laboratorio EftLab.)

### Fase 1: hornear clips de BO7 sobre el rig de EFT (en Unity, por código)
- Pasar `EftCod.cs` de "mover huesos en tiempo real" a **hornear un AnimationClip** (`AnimationClip.SetCurve` sobre las rutas del contenedor de la HK416 y la MP5):
  - Brazos `Base HumanL/R*`.
  - `Camera_animated`.
  - `Weapon_root_anim`.
  - Piezas del arma: `tag_weapon` → `weapon`, `j_mag1` → cargador, `j_bolt` → cerrojo y asa de carga.
- Revisarlo en el laboratorio (hojas de fotogramas) **antes** de llevarlo al juego.

### Fase 2: overrides por arma (técnica A, sin rehacer el contenedor)
- Con la MK35 o la VMP en la mano: `AnimatorOverrideController` sobre el controlador de la HK416 o la MP5 que cambia `look` → `inspect`, `out_to_idle_arm` → `raise`, `idle_to_out` → `drop`, `fire` → `fire`, `idle_armed` → `idle`.
- **Recarga:** el `reload` de BO7 se **corta** en dos (`mag_out` / `mag_in`) en el fotograma en que sale el cargador viejo. Se deforma el tiempo para que el momento en que el cargador sale y entra caiga en el % en que EFT tiene sus eventos (`StaticData`). Así cuadran sonido, lógica y red.
- Lo que BO7 no tiene (encasquillamientos, comprobar cargador o recámara, modos de fuego) se queda con la animación de EFT.

### Fase 3: cuerpo a cuerpo y beber
- **Beber perks:** `vm_gesture_zm_perk_can_drink` horneado sobre el rig de la lata o inyector de EFT y puesto como override del clip de beber del `MedsController`. Sustituye a `CodDrink.cs`, sin manos negras ni doble animación.
- **Cuerpo a cuerpo:** los golpes `p55_me_bgloves` y `knife_melee_stab` como overrides de los estados de tajo y estocada del cuchillo. Sustituye a los giros de `MeleeStyle.cs`.

### Fase 4 (opcional): contenedor propio
Si los overrides se quedan cortos (por ejemplo, para recargas tácticas y en vacío distintas, o la recarga rápida de Speed Cola con su propia animación): contenedor propio con el controlador extraído por AssetRipper, como hace WTT.

## 7. Resultados de la fase 0 (AssetRipper, 2026-10-30)

Herramientas en `D:\ModTools`: AssetRipper 2.0.0 (`--headless --port 5123`, API `POST /LoadFolder` y `POST /Export/UnityProject`), Blender 5.2.3 portable con el addon Cast 2.01. Exportado en `D:\ModTools\rip_out\ExportedProject` (controladores, `StaticData`, 195 clips de la MP5 y la M4A1).

- La HK416 usa los clips de la **M4A1** (`weapons/m4a1/client_assets.bundle`). Las dependencias de cada bundle están en `StreamingAssets/Windows/Windows.json`.
- En `StaticData` los eventos no traen el estado: se reconocen por su contenido (tabla abajo).
- Capas de la MP5: Base Layer, Hands, Catch, Selector, LActions, Malfunction.

| Estado (capa Hands) | MP5 | HK416 (M4A1) | Eventos (tiempo normalizado) |
|---|---|---|---|
| OUT TO IDLE ARM (sacar) | `mp5_out_to_idle_arm` 1,33 s | `m4a1_out_to_idle_arm_0/1/2` | Draw 0,05, BoltOut 0,30, AddAmmoInChamber 0,58, WeapIn 1,0 |
| OUT TO IDLE | `mp5_out_to_idle` 1,0 s | `m4a1_out_to_idle` | WeapIn 1,0 |
| IDLE TO OUT (guardar) | `mp5_idle_to_out` 0,83 s | `m4a1_idle_to_out` | WeapOut 0,97 |
| RELOAD START | `mp5_hands_out_half_1` 0,33 s | `m4a1_hands_out_half_1` | — |
| RELOAD OUT (árbol `RelTypeOld`) | `mp5_mag_out_0/1/2` 1,1 s | `m4a1_mag_out_0..6` | **MagOut 0,462**, MagHide 0,85 |
| RELOAD IN (árbol `RelTypeNew`) | `mp5_mag_in_0/1/2` 1,67 s | `m4a1_mag_in_0..6` | MagShow 0,20 (M4: 0,05), **MagIn 0,70** |
| RELOAD END | `mp5_hands_in_half_2` 0,33 s | `m4a1_hands_in_half_2` | — |
| RELOAD CHARGE END (en vacío) | `mp5_reload_charge` 0,9 s | `m4a1_reload_charge_0` | OffBoltCatch 0,42 (M4: 0,21), AddAmmoInChamber 0,54 |
| RELOAD OUT ALL | `mp5_mag_out_all` 1,0 s | `m4a1_mag_out_all_0/2` | MagOut 0,49, MagHide 0,60 |
| RELOAD ADMIN (solo HK416) | — | `m4a1_reload_admin` | recarga entera en un clip: MagOut 0,355, MagIn 0,600 |
| LOOK (inspeccionar) | `mp5_look` 3,33 s | `m4a1_look` | StartUtilityOperation 0, HandOn 0,93 |
| FIRE | `mp5_fire` (velocidad 13,3) | `m4a1_fire` (14) | FiringBullet 0, FireEnd 0,85 |
| IDLE | `mp5_idle_armed` | `m4a1_grip_0_armed` | — |

- Los clips de EFT animan, además de los brazos y `Camera_animated`, `Weapon_root/Weapon_root_anim/weapon` y sus hijos: `mod_magazine`, `mod_charge`, `weapon_bolt` y los marcadores de IK (`weapon_L/R_hand_marker`, `weapon_L/R_IK_marker`, `Bend_Goal_Left/Right`, `weapon_L/RCollarbone_marker`).
- BO7: el arma (`j_gun`) cuelga de la muñeca derecha y coincide con `tag_weapon`. En la recarga se anima `j_mag1`; la nota `show_full_magazine` marca el cambio de cargador. Las traslaciones son relativas al reposo y los giros absolutos.
- Horneador: `UnityWeaponsSdk/Assets/Editor/TarkovaCola/Bo7Anim.cs`, con el esqueleto combinado de `tools/bo7_skel.py`. Sin probar todavía: Unity pide renovar la licencia.

### Riesgos y dudas
- Las proporciones de los brazos de CoD y EFT no son iguales: en el laboratorio cuadraron muñecas y hombros, pero hay que revisar cada clip.
- La ruta exacta de cada pieza del arma en el contenedor de la HK416 y la MP5 sale con `EftDump`.
- Cómo se comportan los overrides con Fika o con otros jugadores: sin probar.
