# Tarkova-Cola 1.0.0
*Call of Duty Zombies Perk-a-Colas for SPT*

Compatible con / Compatible with: **SPT 4.1.x** (single player, No Fika)

---

### What it is
Drinks you find or buy that grant an in-raid ability. All similar to Modern Style Black ops zombies, The more you use them, the more the perk levels up, unlocking challenges and, with them, **augments** (and linked **drawbacks**) that you pick to change your raids!.

There is one perk for now: **Speed Cola**.

### Speed Cola
- Sold by **Therapist** (loyalty level 1, 8,000 ₽). Takes 1×2 slots.
- Drink it in raid: **reloads 1.5x faster** for the rest of the raid.
- Levels 1 → 7 with the XP you earn in raid (+25% while the perk is active). Max level is 7.
- Each level reveals challenges; completing one unlocks an **augment** and its linked **drawback** (see the table above).

You equip 1 major + 1 minor augment (2 minors with Extra Slot) and **1 major + 1 minor drawback**. You can't carry a major augment without a major drawback.

### How to use
1. Buy Speed Cola from Therapist.
2. In the main menu press **RESEARCH** (bottom bar, next to Handbook) or **F9** to see progress and equip augments.
3. Drink it inside a raid. Open the pause menu (ESC) in raid to see your level and pending challenges.
4. Sugar rush.

### Installation
Extract the zip **over your SPT folder** (the one with `EscapeFromTarkov.exe`). It creates:
- `BepInEx/plugins/TarkovaCola/` (client)
- `SPT/user/mods/TarkovaCola/` (server)

If your install keeps the server in another folder (e.g. `SPT_Runtime/user/mods`), move the server's `TarkovaCola` folder there.
Progress is saved per profile in `user/mods/TarkovaCola/profiles/` (keep it when updating).

### Configuration (F12, ConfigurationManager)
Language (Auto / Español / English), Research hotkey, icon position, hand-model size, debug logging, etc.

### Uninstall
Delete both `TarkovaCola` folders. If you still hold Speed Cola in your stash, SPT lets you remove broken items from the inventory.

---

MIT License (código / code)

**Credits**
- 3D model: "Speedcola CoD Zombies" by **albertjuli** ([Sketchfab](https://skfb.ly/6VRDU)), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Changes: converted to an Unity asset bundle, textures re-prepared (metal/smoothness adjusted), rescaled and rotated for in-game use.
- Icons: Call of Duty, property of their owners (not MIT).


---

## Building from source

**Requirements:** .NET SDK 9, SPT 4.1.x, Unity **2022.3.43f1**, Python 3 + Pillow.

This folder must live **inside your SPT folder** (`<SPT>/TarkovaCola-src`): the `.csproj` files look for `EscapeFromTarkov_Data`, `BepInEx` and `SPT_Runtime` one level up.

```
dotnet build Client -c Release     # plugin de BepInEx -> BepInEx/plugins/TarkovaCola
dotnet build Server -c Release     # mod de servidor  -> SPT_Runtime/user/mods/TarkovaCola
python package.py                  # genera dist/TarkovaCola-<version>.zip
```

Every build deploys into the game folder.

### Third-party material included

The repo bundles, for convenience, the bottle 3D model (Sketchfab), its textures and the Call of Duty icons. They are **not** covered by the MIT license and remain the property of their owners; if you are an owner and want them removed, open an issue (see `LICENSE`).

###/ Layout
- `Client/` BepInEx Plugin (UI, HUD, Harmony patches, Native screen, Perk effects on screen.)
- `Server/` Spt server-side mod (item, Store, Saved in profile progress)

