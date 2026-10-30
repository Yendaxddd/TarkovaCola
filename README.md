# Tarkova-Cola 3.0.1
*Call of Duty Zombies Perk-a-Colas for SPT*

Compatible with: **SPT 4.1.x** (single player, Not guaranteed to work with Fika, Report issues on SPT Page/Here). Optional: **VisitAPI 1.3.5+** for the storyline.

> El README del zip (Package/README.md) está en español e inglés.

---

### Hotfix 3.0.1
- **APEX Core:** dragging it in the inventory right after picking it up broke the UI. It is now absorbed when you close the inventory.
- **Perks as injectors:** the "Use" button was greyed out. Fixed by Kaganari (PR #1, thanks!).
- **Akula push dagger** (and infected hands): no longer use the melee styles; the weapon no longer floats in the air.
- **Eternal and Classic perks:** at raid start they are checked to still be active, and Deadshot is reapplied to your weapon.

### What's new in 3.0
The biggest update so far: 3 new perks, perk machines on the map, the Mystery Box, TEDD, APEX, two Black Ops 7 weapons and perks for enemies and teammates. The Black Ops 6/7 models are shipped compiled only, for playing.

#### What's new

##### 3 new perks
| Perk | Base effect | How to get it |
|---|---|---|
| **Bandolier Bandit** | Magazines in your rig and pockets refill 1 round every 2 s from loose ammo you carry, while you are not shooting. | Therapist (7 GP), raid loot and the Wunderfizz. |
| **Vulture Aid Elixir** | Unsearched containers and bodies with valuables glow through walls (15 m). | Same as Bandolier. |
| **Melee Macchiato** | Melee damage ×2 and melee attacks 30% faster. | Same as Bandolier. |

Each one has its full **Research** tree, with augments, drawbacks, challenges and its own jingle. Vulture Aid has the **Re-roll** augment: look at a container up close, press **J** (configurable in F12) and its loot is rolled again.

##### Perk machines on the map (Factory for now, day and night)
- Perk-a-Cola machines are **on the map**. Hold **F** in front of one to buy its perk for **30,000 ₽**. If you already have it, make it **Eternal** for **15,000 ₽**.
- **Turn on the power:** machines don't work until you flip the map's power switch. Can be turned off in F12.
- **Wunderfizz:** a random perk for **40,000 ₽**, with its roulette.

##### Mystery Box
- Spawns at **one** of its spots on the map each raid. Costs **1,000 ₽**.
- **Hold F** for a **random weapon** (built, with its preset). **Hold Y** (configurable in F12) for a **random attachment**.
- Just like Zombies: the lid opens, models cycle and the prize floats. Grab it with EFT's F before it sinks back.
- After several uses in the same spot the **teddy bear** may show up: you get your money back and the box flies off to another spot.
- 15% chance to give one of the new Black Ops 7 weapons.

##### TEDD
- Shows up in **some raids** (15% by default, configurable in F12) at one of his spots. He glows green and is marked on screen when you are close.
- Talk to him (**hold F**) and he gives you **one random trial**, with 5 minutes to complete it:
  - **Item:** an item spawns far away; bring it to him.
  - **Takedown:** a PMC spawns, marked on screen; it has to die.
  - **Hold out:** 90 s near TEDD while waves of infected come in.
  - **Money:** pay him the amount he asks for.
- **Reward:** a random perk bottle.

##### APEX
A **very strong** effect for each perk. You can only have **one** equipped (Modern and Classic) and it works while its perk is active.
1. Get an **APEX Core**: rarely found in safes, caches, weapon boxes and PMC bodies. Mechanic (level 1) also sells it for 5,000,000 ₽, and it shows up in the flea market search. To count, pick it up or **carry it into a raid** and **extract alive**.
2. In **Research > APEX**, spend a core to start an APEX challenge. It only progresses while its perk is active.
3. Beat the challenge and equip it.

| APEX | Effect |
|---|---|
| Juggernog | Immune to melee and no bleeding |
| Speed Cola | Your weapon never jams |
| Stamin-Up | Infinite stamina and +10% sprint speed |
| PHD Flopper | Every kill sets off an explosion (it doesn't hurt you) |
| Widow's Wine | Your explosives go off on their own when an enemy walks close (3 m) |
| Deadshot | +10 penetration (one armor class higher) |
| Bandolier | A kill refills your magazine |
| Vulture Aid | 50% of enemies carry a valuable or quest item |
| Melee Macchiato | You take no damage while swinging |

##### Black Ops 7 weapons
- **MK35 ISR** (assault rifle, based on the HK416) and **VMP** (SMG, based on the MP5).
- Their own models, usable iron sights, and they take **their base weapon's attachments** and camos.
- **How to get them:** Mechanic (level 1), already built, or the Mystery Box.

##### Perks on enemies and teammates
- **Enemies:** those carrying a perk **get its effect**. Juggernog takes twice the damage, Speed Cola fires faster, Deadshot knows where you are through walls, Widow's Wine slows you when it hits, etc. Enemies with Bandolier or Vulture Aid spawn with extra ammo or a valuable item.
- **PIT Fireteam teammates:** when you open their inventory to give or swap gear, a panel with **4 perk slots** shows up at the bottom.
  - Press **+** and pick a perk you carry: it gets equipped and the bottle is used up when you close the inventory.
  - Press an equipped perk to take it back: the bottle returns to your gear.
  - Perks stay between raids until the teammate **dies**.
- Everything can be turned off in F12: *Perk effects on enemies and teammates*.

##### Other
- **Perks as injectors** (F12, off by default): perks become injectors with the injection animation instead of cans. Requires restarting the server and the game.
- **Melee styles:** combos depending on the weapon. Light ones do horizontal, vertical and stab; medium ones horizontal, backhand and vertical; heavy ones a single weighty swing. Knives sit closer to the screen center.
- **Refreshed UI** in Research and the raid panel: animated background, bubbles, corner brackets, hover shine and a color that follows the selected perk.
- **New mod icon.**
- **Developer options** (machine editor, clip recording, debugging) hidden in F12. Turn on *Advanced settings* to see them.


#### What changed
- **Drawback revision:** drawbacks that were generic penalties (hunger, thirst, sprinting, drawing your weapon) now punish in the same area their perk helps with. Whatever you had equipped stays equipped; only the effect changes. PHD Flopper gains the **Explosive Drop** augment and the **Powder Junkie** drawback.
- **Black Ops drinking animation:** now **off** and moved to F12 > Debug as experimental. It mixed with EFT's (black hands and a floating can), so now only EFT's plays.
- PIT teammates' perks are no longer read from their backpack: they are equipped in their 4 slots.


#### Coming next
- **Machines, box and TEDD on more maps** (only Factory for now). They are placed with the F12 machine editor.
- New quests: APEX (3), Mystery Box (1) and TEDD (2).
- Synergies, achievements and quests for Bandolier, Vulture Aid and Melee Macchiato.
- Fix the Black Ops drinking animation.
- Mystery Box sounds.
- Master of Movement (PHD slide) and a stats tab.

### Hotfix 2.2.1
- Compatibility with mods that restore your equipment on death (e.g. Fair Equipment Restoration): bottle refunds (Eternal / Classic / Widow's Bite) and achievement rewards are now sent after the raid ends, never during it.
- Achievements unlocked when you die are now handled correctly.

### What's new in 2.2
- **Fix (important):** reaching level 5 on a perk could show a blank error at raid start and break other mods' raid start (HUD mods, loot counters). Raid start is now fully isolated from errors, and achievements are checked once your character is loaded.
- **Arena Mode compatibility:** its Zombies mode drops a random perk every 5 waves and perks gain slow progress. Arena matches never count for your SPT profile (Classic and Eternal perks are kept). Options in F12 > Arena.
- **Many more perks in the world:** medical cases, weapon boxes, duffle bags, dead Scavs, buried caches, drawers and jackets, and as **loose loot** wherever drinks can spawn. Higher chances everywhere (PMCs 10%, Scavs 3%, Rogues 12%). Your old `loot.json` is replaced (backup in `loot.json.old`).
- **Augment review:** augments that cancelled their own drawback, duplicated challenges and drawbacks that didn't stack were reworked (Deadshot, Juggernog, Speed Cola, Stamin-Up, PHD Flopper, Widow's Wine). Changed challenges restart their progress if they weren't done.
- **Dead Aim** now keeps your weapon locked to the center: no sway while moving, turning or aiming (recoil still applies).
- **Quest images** for all 10 storyline quests (they were missing).
- ???

### What's new in 2.1
- **F12 menu in English** (or Spanish): option names follow the mod language (Auto = game language). Your saved settings are kept.
- **Option to hide the perk icons** at the bottom of the screen (F12 > HUD > Show perk icons).
- **Perk carrier alert:** a notice when you get close to an enemy carrying perks (range in F12).
- **Perks show up in more places:** wooden crates, ammo boxes, grenade boxes (Widow's Wine / PHD Flopper), ration crates (Juggernog), PMC bodies and safes (almost always). **Bosses always carry 2 perks**; some PMCs, Rogues and (rarely) Scavs carry one. Chances are in `user/mods/TarkovaCola/loot.json`.
- **Fix:** perks were added to the wrong containers (dead Scavs and jackets instead of ammo boxes and safes).
- Quest items now look like what they are (notebook, envelope, sample...).

### What it is
Drinks you find, buy or craft that grant an in-raid ability. The more you use them, the more the perk levels up: challenges are revealed and completing them unlocks **augments** (and their linked **drawbacks**) that you equip in the **Research** screen.

### The perks

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
- "le creatura": fan-made M4 SOPMOD II Jr. model (Girls' Frontline, © MICA Team / Sunborn); throw sounds supplied by the mod author. Only the compiled asset bundle is included.
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
