# Tarkova-Cola 3.0.0

The biggest update so far: 3 new perks, perk machines on the map, the Mystery Box, TEDD, APEX, two Black Ops 7 weapons and perks for enemies and teammates.

> **Install:** extract the zip over your SPT folder (the one with `BepInEx` and `SPT_Runtime`) and overwrite files. Your progress is kept.
> The Black Ops 6/7 models are shipped **compiled only**, for playing. The originals are not included and are not meant for development.

---

## What's new

### 3 new perks
| Perk | Base effect | How to get it |
|---|---|---|
| **Bandolier Bandit** | Magazines in your rig and pockets refill 1 round every 2 s from loose ammo you carry, while you are not shooting. | Therapist (7 GP), raid loot and the Wunderfizz. |
| **Vulture Aid Elixir** | Unsearched containers and bodies with valuables glow through walls (15 m). | Same as Bandolier. |
| **Melee Macchiato** | Melee damage ×2 and melee attacks 30% faster. | Same as Bandolier. |

Each one has its full **Research** tree, with augments, drawbacks, challenges and its own jingle. Vulture Aid has the **Re-roll** augment: look at a container up close, press **J** (configurable in F12) and its loot is rolled again.

### Perk machines on the map (Factory for now, day and night)
- Perk-a-Cola machines are **on the map**. Hold **F** in front of one to buy its perk for **30,000 ₽**. If you already have it, make it **Eternal** for **15,000 ₽**.
- **Turn on the power:** machines don't work until you flip the map's power switch. Can be turned off in F12.
- **Wunderfizz:** a random perk for **40,000 ₽**, with its roulette.

### Mystery Box
- Spawns at **one** of its spots on the map each raid. Costs **1,000 ₽**.
- **Hold F** for a **random weapon** (built, with its preset). **Hold Y** (configurable in F12) for a **random attachment**.
- Just like Zombies: the lid opens, models cycle and the prize floats. Grab it with EFT's F before it sinks back.
- After several uses in the same spot the **teddy bear** may show up: you get your money back and the box flies off to another spot.
- 15% chance to give one of the new Black Ops 7 weapons.

### TEDD
- Shows up in **some raids** (15% by default, configurable in F12) at one of his spots. He glows green and is marked on screen when you are close.
- Talk to him (**hold F**) and he gives you **one random trial**, with 5 minutes to complete it:
  - **Item:** an item spawns far away; bring it to him.
  - **Takedown:** a PMC spawns, marked on screen; it has to die.
  - **Hold out:** 90 s near TEDD while waves of infected come in.
  - **Money:** pay him the amount he asks for.
- **Reward:** a random perk bottle.

### APEX
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

### Black Ops 7 weapons
- **MK35 ISR** (assault rifle, based on the HK416) and **VMP** (SMG, based on the MP5).
- Their own models, usable iron sights, and they take **their base weapon's attachments** and camos.
- **How to get them:** Mechanic (level 1), already built, or the Mystery Box.

### Perks on enemies and teammates
- **Enemies:** those carrying a perk **get its effect**. Juggernog takes twice the damage, Speed Cola fires faster, Deadshot knows where you are through walls, Widow's Wine slows you when it hits, etc. Enemies with Bandolier or Vulture Aid spawn with extra ammo or a valuable item.
- **PIT Fireteam teammates:** when you open their inventory to give or swap gear, a panel with **4 perk slots** shows up at the bottom.
  - Press **+** and pick a perk you carry: it gets equipped and the bottle is used up when you close the inventory.
  - Press an equipped perk to take it back: the bottle returns to your gear.
  - Perks stay between raids until the teammate **dies**.
- Everything can be turned off in F12: *Perk effects on enemies and teammates*.

### Other
- **Perks as injectors** (F12, off by default): perks become injectors with the injection animation instead of cans. Requires restarting the server and the game.
- **Melee styles:** combos depending on the weapon. Light ones do horizontal, vertical and stab; medium ones horizontal, backhand and vertical; heavy ones a single weighty swing. Knives sit closer to the screen center.
- **Refreshed UI** in Research and the raid panel: animated background, bubbles, corner brackets, hover shine and a color that follows the selected perk.
- **New mod icon.**
- **Developer options** (machine editor, clip recording, debugging) hidden in F12. Turn on *Advanced settings* to see them.

---

## What changed
- **Drawback revision:** drawbacks that were generic penalties (hunger, thirst, sprinting, drawing your weapon) now punish in the same area their perk helps with. Whatever you had equipped stays equipped; only the effect changes. PHD Flopper gains the **Explosive Drop** augment and the **Powder Junkie** drawback.
- **Black Ops drinking animation:** now **off** and moved to F12 > Debug as experimental. It mixed with EFT's (black hands and a floating can), so now only EFT's plays.
- PIT teammates' perks are no longer read from their backpack: they are equipped in their 4 slots.

---

## Coming next
- **Machines, box and TEDD on more maps** (only Factory for now). They are placed with the F12 machine editor.
- New quests: APEX (3), Mystery Box (1) and TEDD (2).
- Synergies, achievements and quests for Bandolier, Vulture Aid and Melee Macchiato.
- Fix the Black Ops drinking animation.
- Mystery Box sounds.
- Master of Movement (PHD slide) and a stats tab.
