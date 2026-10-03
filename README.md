# Tarkova-Cola 1.2.0
*Call of Duty Zombies Perk-a-Colas for SPT*

Compatible con / Compatible with: **SPT 4.1.x** (single player, No Fika)

---

### What it is
Drinks you find or buy that grant an in-raid ability. All similar to Modern Style Black ops zombies, The more you use them, the more the perk levels up, unlocking challenges and, with them, **augments** (and linked **drawbacks**) that you pick to change your raids!.

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
| Sprint Shooter (major) | Weapon ready much faster right after sprinting | Find 15 Hot Rod / RatCola / Max Energy | Burnout: below 30% stamina you recover at half speed |
| Second Wind (major) | While your health is in the red: infinite stamina and pain immunity | Run out of stamina 5 times | Jelly Legs: weapon sway +25% after sprinting 5 s |
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
4. Sugar rush.

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

**Credits**
- 3D model: "Speedcola CoD Zombies" by **albertjuli** ([Sketchfab](https://skfb.ly/6VRDU)), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Changes: converted to an Unity asset bundle, textures re-prepared (metal/smoothness adjusted), rescaled and rotated for in-game use.
- Icons and jingles: Call of Duty, property of their owners (not MIT).
- Stamin-Up bottle: "cod zombies perks" by **Unknown Dev** ([Sketchfab](https://sketchfab.com/3d-models/cod-zombies-perks-77e9cb71adaf44b5a974c584f96cfc6c)), Sketchfab Free Standard license. Only the compiled, game-ready asset bundle is included; the original model files are **not** redistributed (download them from Sketchfab to rebuild the bundle).


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

