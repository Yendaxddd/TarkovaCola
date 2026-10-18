# Tarkova-Cola 2.1.0
*Call of Duty Zombies Perk-a-Colas for SPT*

Compatible with: **SPT 4.1.x** (single player, Not guaranteed to work with Fika, Report issues on SPT Page/Here). Optional: **VisitAPI 1.3.5+** for the storyline.

> El README del zip (Package/README.md) está en español e inglés.

---

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


---

## Building from source

**Requirements:** .NET SDK 9, SPT 4.1.x, Unity **2022.3.43f1**, Python 3 + Pillow (+ `miniaudio` to convert audio).

This folder must live **inside your SPT folder** (`<SPT>/TarkovaCola-src`): the `.csproj` files look for `EscapeFromTarkov_Data`, `BepInEx` and `SPT_Runtime` one level up.

```
dotnet build Client -c Release     # BepInEx plugin -> BepInEx/plugins/TarkovaCola
dotnet build Server -c Release     # server mod    -> SPT_Runtime/user/mods/TarkovaCola (+ VisitAPI pack if installed)
python make_quest_pack.py          # storyline: VisitAPI pack + Server/ModFiles/story.json + guards.json
python make_achievements.py        # 2.0 achievements (server/client data + badge list)
python make_achievement_icons.py   # achievement badges
python package.py                  # dist/TarkovaCola-<version>.zip
```

Every build deploys into the game folder. `build_bundle.ps1` rebuilds the bottle asset bundles (the original Sketchfab bottle pack must be downloaded separately into `UnityBundle/Assets/Models/perks/`).

### Third-party material
The repo bundles, for convenience, the compiled bottle models, the Speed Cola model textures and the Call of Duty icons, jingles and voice lines. They are **not** covered by the MIT license and remain the property of their owners; if you are an owner and want them removed, open an issue (see `LICENSE`).

### Layout
- `Client/` BepInEx plugin (Research screen, HUD, Harmony patches, perk effects, quest guards, achievements).
- `Server/` SPT server mod (items, Therapist offers, loot, recipes, sale locks, achievements, saved progress, VisitAPI pack).
