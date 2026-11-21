# Plan: armas de BO7 "de verdad" (última actualización)

Pedido del usuario (2026-11-11): port completo y bien hecho de las armas de BO7 — animaciones, sonidos, descripción — que
parezcan armas vanilla, no un clon con otra piel. Rehacer también la MK35 ISR y la VMP con el juego completo de animaciones.

## Armas y bases

| Arma | BO7 | Base EFT (controlador, calibre) | Estado del material |
|---|---|---|---|
| Peacekeeper Mk1 | `ar_condor` | AK-12 5.45 `6499849fc93611967b034949` | modelo base, 202 anim, sonidos |
| Voyak KT-3 | `ar_heron` | AK-74M 5.45 `5ac4cd105acfc40016339859` | modelo base, 208 anim, sonidos |
| MXR-17 | `ar_eagle` | SCAR-L Mk16 5.56 `6184055050224f204c1da540` | solo skins (s1blk completa), 184 anim, sonidos |
| M8A1 | `dm_scotia` | M4A1 5.56 `5447a9cd4bdc2dbd208b4567` (+ráfaga) | modelo base, 160 anim, **sin sonidos** |
| VST | `sm_bear` | MP9 9x19 `5e00903ae9dc277128008b87` | modelo base, 161 anim, **sin sonidos** |
| Hawker HX | `sn_aden` | AXMC .338 | skin s3blk completa (base en `wpn_sat_sn_aden_*`), 105 anim, sonidos |
| Escopeta | `sh_corso` | M590A1 12g `5e870397991fd70db46995c8` | modelo base, 230 anim, sonidos |
| Cuchillo balístico | `me_ballisticknife` | cuchillo de EFT | skin merc, 95 anim, sonidos |
| MK35 ISR (rehacer) | `ar_finch` | HK416A5 | todo |
| VMP (rehacer) | `sm_civet` | MP5 Navy | todo |

Material: `Downloads/saluki/exported_files/bo7/{models,animations,animpkgs,sounds,sndbanks}`.

## Arquitectura (decidida)

Se mantiene lo que ya funciona en el juego (arma = clon de un arma de EFT; piezas de BO7 = accesorios propios; animaciones =
`AnimatorOverrideController` sobre el controlador del arma base) y se completa. El controlador propio estilo WTT se descartó: la
plantilla del SDK solo trae la capa de la mano izquierda y cada tipo de arma (bomba, cerrojo) necesitaría su telaraña de estados;
con overrides el controlador del arma base ya trae todos los estados y solo se cambian los clips.

Mejoras respecto a la MK35/VMP de la 3.0:

1. **Todos los estados** con clip de BO7 cuando BO7 tiene material: reposo, disparo, recargas (táctica, vacía, rápida), sacar,
   guardar, inspeccionar, selector, comprobar cargador/recámara, encasquillamientos (con trozos de la recarga en vacío).
2. **Tiempo natural**: en vez de deformar la animación de BO7 para que coincida con los eventos de EFT, se clona el `StaticData`
   del arma en el cliente y se mueven los eventos (`AnimationEventsStateBehaviour.EventsData`, `FullName` = estado) al momento
   en que ocurren en BO7.
3. **Piezas móviles**: cerrojo, asa de carga, bomba, selector y balas del cargador como nodos animables dentro de los accesorios
   (malla partida por hueso), con curvas en los clips.
4. **Override puesto antes del primer fotograma** (no cada 0,25 s en reposo).
5. **Armas con identidad**: estadísticas, descripción (wiki), sonidos, iconos y ranuras propias; todo desde una tabla por arma.

## Fases

- [x] A. Importar modelo y animaciones del piloto (`tools/bo6_import.py`, `tools/cast2json.py`, `tools/bo7_skel.py`). Falta el resto de armas.
- [x] B. Sonda de armas base: `EftProbe2.Run` -> `UnityWeaponsSdk/Preview/probe_<base>.txt` (hechas ak12, ak74m, mk16, m4a1, mp9, axmc, m590, hk416, mp5).
- [x] C. Canal generico: `BuildBo7Guns.V2.cs` (tabla `Guns`, `RunGun`, partes moviles, anclaje por la mano derecha), `Bo7Anim.V2.cs`
      (`Build2`/`Solver2`, `Run2` hoja de un clip, `BakeRun2` + `Jobs2`, `AddGripPoses`, `bo7_events.json`), cliente (`Bo7Guns.All`,
      `Bo7Skin.Movers`, `Bo7Anims.Retime`) y servidor (`AddPeacekeeper`).
- [ ] D. Piloto Peacekeeper Mk1 (`pk`, ids b70101-07): construido, horneado (17 clips) e instalado el 2026-11-11. **Falta la prueba del usuario en el juego.**
      Pendiente en el piloto: comprobar cargador/recamara, encasquillamientos, cargar/descargar una bala, primer sacado (el override entra al quedar en reposo).
- [ ] E. Resto de armas. Hecha (sin probar): Voyak KT-3 (`vk`). Receta corta para fusiles: `tools/bo7_gun_import.sh`, fila en `BuildBo7Guns.Guns`,
      `Bo7Anim.Rifle{...}.Jobs()` en `Jobs2`, `AddRifle` en el servidor, fila en `Client/Bo7Guns.All`, alias en `tools/bo7_sounds.py`.
      MXR-17 (`mx`, ar_eagle sobre SCAR-L, ids b70301-07, preset 7) hecha el 2026-11-12, sin probar en juego: modelos base
      `att_sat_vm_ar_eagle_*` importados como `sat_vm_ar_eagle_*` (`BO7_PREFIX=att_sat_vm_ tools/bo7_gun_import.sh ar_eagle rec bar mag pgrip stock` y renombrar),
      fila en `Guns`, `Jobs2["mx"]` (Rifle con `Catch`/`PullFrom` para armas con reten de cerrojo; tiempos medidos sobre los huesos, sin ver),
      cliente, servidor (AddRifle no crea el arma si falta `<key>_upper.bundle`) y sonidos. El controlador de la SCAR esta en
      `scar/weapon_fn_mk16_556x45_fde_container.bundle` (`EftBundles.Family`); la SCAR no tiene `fireport` en el arma (se usa `patron_in_weapon`).
      2026-11-12: sondeada (probe_mk16.txt), construida, horneada (18 clips) e instalada. **Falta la prueba del usuario.** Sin clip propio: comprobar cargador/recamara, encasquillamientos, culata plegable, cargar una bala.
- [ ] F. Sonidos de todas (faltan los de `dm_scotia` y `sm_bear`: pedirlos al usuario). `tools/bo7_sounds.py` (anadir el arma a GUNS).
- [ ] G. Cuchillo balistico.
- [ ] H. Limpieza: README/ModPage, CHANGELOG, empaquetado. Descripciones con la wiki.

## Receta por arma

1. `python tools/bo6_import.py <saluki models> UnityWeaponsSdk/Assets/TarkovaCola/Models/bo7 <piezas>`; convertir sus `.cast` con `cast2json.py` a
   `Assets/TarkovaCola/Anim`; `bo7_skel.py` -> `bo7_<cod>_skel.json`; `python tools/bo7_notes.py <Anim> sat_vm_<cod>_ <regex> -v` para ver notas.
2. Entrada en `BuildBo7Guns.Guns` + funcion `Build<Arma>` y `Mount`; `tools/u.sh BuildBo7Guns.RunGun BO7_GUN=<id>`.
3. `tools/u.sh Bo7Anim.Run2 BO7_GUN=<id> BO7_CLIP=reload BO7_FRAMES=0-end/6 LAB_VIEW=fps LAB_COLS=4 LAB_W=640 LAB_H=360` para ver clips.
4. `Jobs2[<id>]` con los clips de EFT de `probe_<base>.txt`; `tools/u.sh Bo7Anim.BakeRun2 BO7_GUN=<id> BO7_SHEET=<clips>`.
5. Sonidos (`bo7_sounds.py`), cliente (`Bo7Guns.All`), servidor (ids, piezas, preset, Mechanic), copiar bundles a `Server/ModFiles` + `bundles.json`.

## Notas de trabajo

Ver `docs/Investigacion_animaciones.md` (técnicas, tabla de estados de MP5/M4A1) y la memoria `reference-eft-anim-lab`.

## 2026-11-16: VST, M10 Breacher y Hawker HX (instaladas, SIN probar en juego)
- **VST** (`vs`, MP9, ids b70401-07, preset 8): `BuildRifle` con `GripInUpper` (la MP9 no tiene ranura de empunadura); Jobs2 = `Rifle` con `CatchState = "RELOAD CATCH END IDLE"`.
- **M10 Breacher** (`br`, Mossberg 590A1, ids b70501/03/04/06/07, preset 9): `BuildShotgun`/`MountShotgun` (el canon de BO7 lleva receptor + canon + bomba `tag_guard_attach`; culata en mod_stock; tubo = pieza vacia en mod_magazine). Servidor `AddShotgun`: sin guardamanos ni montura de la 590, oferta en tiro a tiro. Jobs2 a mano: FIRE = 6 fotogramas quieto + rechamber entero; RELOAD FIRST/LOAD ONE = reload_empty_chamber_start; RELOAD IN (vel 1.2) = reload_loop; RELOAD START (m590_hands_out, compartido con abrir inventario) = reload_start.
- **Hawker HX** (`hw`, AXMC .338, ids b70601/04/05, preset a): en la AXMC todo cuelga de la empunadura: `BuildSniper` hace una pieza `hw_pgrip` (receptor, canon, empunadura, culata; ranuras mod_scope, mod_muzzle, mod_tactical_001) y `hw_mag`. El cerrojo de EFT (mod_charge, obligatorio) se queda y el cliente lo oculta. Preset con montura Nightforce 34 mm + S&B PM II 3-12x50. `axmc_fire_chambering` lo comparten FIRE RELOAD / RECHAMBER / MISFIRE_REPAIR / RELOAD CHARGE END MAG (un clip, eventos por estado). La carga de balas sueltas sigue con los clips de EFT.
- `AddGripPoses` mide las dos manos antes de guardar (con las dos en el mismo prefab, la segunda no encontraba la instancia).

### 2026-11-16 (tarde): Voyak/MXR/Breacher demasiado grandes y Breacher sin poder equiparse
- **Equipar la Breacher**: las ranuras de arma del inventario por defecto (`55d7217a4bdc2d86028b456d`) no admiten la clase escopeta sino
  tpls concretos (la M590 incluida). `AddShotgun` anade el tpl del clon a todo filtro de ranura que ya contiene la M590 (el log dice en
  cuantas). Los fusiles de cerrojo si van por clase, la Hawker no lo necesita.
- **Armas pegadas a la camara**: el reposo de la base de EFT las deja mas cerca que el encuadre propio de BO7 y al inspeccionar/recargar
  se metian en la camara. `GunDef.ViewPush` aleja arma y manos en todas las animaciones (vk 7 cm, mx 4, br 4) y `GunDef.LookPush` suma
  mas solo en el estado LOOK, con entrada/salida suave en el 15 % de los extremos (vk +7 cm). `Bo7Anim.ViewProbe` mide lo mas cerca
  que llegan arma y manos dentro de un cono de 30 grados y avisa si una mano no llega. Cobertura de pantalla al inspeccionar (p90):
  Voyak 0.62 -> 0.42, MXR 0.48, Breacher 0.44, VST 0.51.
- `AimDepth` quitado de Voyak y MXR: el empuje ya aleja la mira trasera del ojo al apuntar (~15 y ~17 cm).
