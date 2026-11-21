# Tarkova-Cola 4.0.0

## The Last Round
This is the last big update of Tarkova-Cola, and the biggest by far. It adds **6 new perks** (15 in total), **6 more Black Ops 7 weapons** (8 in total), Black Ops drinking and melee animations, machines on **every map**, a perk case, 45 new achievements, a balance pass on every perk and compatibility with many popular mods. All the texts can now also be **translated**.

> **Updating from 3.x:** extract the zip over your SPT folder and overwrite. Your progress, Eternal perks and Classic perks are kept.

### At a glance
| | |
|---|---|
| **New perks** | Quick Revive, Mule Kick, Electric Cherry, Death Perception, Tombstone Soda and Double Tap Root Beer, each with its full Research tree, APEX, jingle, bottle and injector |
| **Black Ops 7 weapons** | Peacekeeper Mk1, Voyak KT-3, MXR-17, VST, M10 Breacher and Hawker HX, with BO7 animations and sounds. MK35 ISR and VMP also get them |
| **Animations** | Black Ops 7 drinking animation for every perk, and Black Ops 7 melee animations for all 23 EFT melee weapons |
| **Machines** | Machines, Mystery Box, TEDD and the power switch on **every map**, placed automatically. A machine editor lets you place your own |
| **Mystery Box** | Can give the Ray Gun, Ray Gun Mark II and Wunderwaffe DG-2 when those mods are installed |
| **Perk case** | A case that only holds perks and APEX cores |
| **Achievements** | 45 new, 127 in total |
| **Classic** | The modifier perk now gives 50% of its effect plus a fixed signature augment |
| **Compatibility** | SAM (Advanced Movement), Dynamic Maps, SAIN, QuestingBots, K9 Companion, PIT Fireteam, TacticalReload, Weapon Camo and Stickers, and Kenner's wonder weapons |
| **Translations** | Every text can be translated with JSON files (see `TRANSLATING.md`) |

---

### The 6 new perks
| Perk | Base effect |
|---|---|
| **Quick Revive** | After 8 s without taking damage, you regenerate 1 HP per second in one injured body part at a time (head and thorax first), even while sprinting. The health is drawn from one of your medkits (never the one in your hands): no medkit, no regeneration. |
| **Mule Kick** | A **3rd weapon slot** that takes any primary. Reach it with the mouse wheel. Swapping to or from it is 50% faster. |
| **Electric Cherry** | When a reload finishes you get an electric **sprint speed boost** for 1 s. The emptier the magazine was, the bigger the boost (up to +150%). Works with every weapon: magazines, shotgun tubes, revolvers and bolt-actions. |
| **Death Perception** | You see better in the dark, and enemies you see within 20 m get an **orange outline** until you lose sight of them. |
| **Tombstone Soda** | When a hit leaves you below 25% of your total health, you drop a **decoy tombstone** and become a **specter** for 4 s. Bots lose you and shoot the tombstone, and you move 30% faster but can't shoot. At most once every 3 minutes. |
| **Double Tap Root Beer** | Fire rate ×1.10, and every consecutive hit within 0.4 s raises it by another 3% (up to +15%). Missing or stopping resets the rhythm. |

**How to get them:** Therapist (loyalty level 1, 7 GP Coins each), raid loot (the same places as the other perks), the **Der Wunderfizz** machine, TEDD's trials and the random perk drop. They are not locked behind the storyline.

Like every perk, each one levels 1 → 5, reveals challenges and unlocks **3 major augments, 4 minor augments and an Extra Slot**, each with its drawback (3 major, 4 minor).

#### Quick Revive
| Type | Augment | Effect |
|---|---|---|
| Major | **Revive** | Needs a stimulant in your inventory (it uses one). A lethal hit leaves you **Downed** for 10 s at 1 HP: your weapon is taken, you only have a pistol and you crawl. Kill an enemy to get back up with 30% health and keep your perks; otherwise you die. Once per raid. |
| Major | **Transfusion** | The regeneration restores destroyed body parts (back to 1 HP), but gives them no more health. From there only medicine heals them. |
| Major | **Combat Medic** | Taking damage no longer stops the regeneration: in combat you heal 0.3 HP/s. |
| Minor | **Patience** | Regeneration starts after 5 s without damage (instead of 8). |
| Minor | **Priority** | Head and thorax regenerate 50% faster. |
| Minor | **Clotting** | While you regenerate, light bleeds stop. |
| Minor | **Reserve** | When you take damage, you heal 2 HP/s for 3 s; then it stops until the normal wait is over. |
| Major drawback | Thin Blood · Costly Metabolism · Startle | Waits 14 s · each HP costs 0.5 energy and 0.5 hydration · your own shots reset the wait |
| Minor drawback | Lukewarm Regen · Scars · No Rush · Weak Mending | −25% healing · heals only up to 80% · stops while sprinting · +10% damage taken while regenerating |
| APEX | | Revive recharges every 7 PMC kills (it still needs a stimulant). |

#### Mule Kick
| Type | Augment | Effect |
|---|---|---|
| Major | **Mule's Kick** | The first shot within 2 s of swapping weapons deals 50% more damage. |
| Major | **Mule Bandolier** | The Mule Kick weapon reloads itself while stowed (1 round every 1.5 s, from the loose ammo you carry). |
| Major | **Quick Change** | Swapping weapons with an empty magazine is instant. |
| Minor | **Light Strap** | The Mule Kick weapon weighs half. |
| Minor | **Chamber Ready** | The Mule Kick weapon always comes out chambered. |
| Minor | **Balance** | For 2 s after drawing the Mule Kick weapon, sway drops by 15%. |
| Minor | **Holster** | Swapping between your other weapons is 30% faster too. |
| Major drawback | Mule Load · Loose Strap · Clumsy Swap | the weapon weighs 50% more · a hit of 30+ damage has a 5% chance to drop it · swapping to or from it is 30% slower |
| Minor drawback | Empty Chamber · Safety On · Clatter · Tangled | comes out unchambered · first shot delayed 0.3 s · drawing it makes noise (15 m) · can't reload it while sprinting |
| APEX | | When you swap weapons, the one you draw gets a chambered round and 5 rounds in its magazine. |

- The slot only accepts weapons while the perk is active. If the perk ends with a weapon still in it, the slot stays visible so you can take it out.
- The mouse wheel can be turned off in F12 (*Mule Kick: mouse wheel swaps weapons*).
- Works alongside DualWieldLab's extra holster.

#### Electric Cherry
| Type | Augment | Effect |
|---|---|---|
| Major | **Discharge** | Reloading also releases an electric burst around you: it stuns enemies for up to 1.5 s within 2-5 m, depending on how empty the magazine was. |
| Major | **Overcharge** | Reloading during the boost stacks it (up to +225%) and renews its duration. |
| Major | **Lightning** | During the boost, sprinting uses no stamina and you can reload while sprinting. |
| Minor | **Battery** | The boost lasts 2 s (instead of 1). |
| Minor | **Voltage** | Any reload gives at least +45% speed. |
| Minor | **Feedback** | A kill during the boost brings it back to its starting value. |
| Minor | **Steady Current** | The boost doesn't fade: it stays at full and cuts out at the end. |
| Major drawback | Leak · Polarity · Backlash | lasts 0.5 s · only reloads with the magazine at least half empty count · 15% slower for 1.5 s when it ends |
| Minor drawback | Short Circuit · Visible Spark · Tingling · Low Ceiling | taking damage cancels it · bots hear you crackle (20 m) · +15% sway · max boost +105% |
| APEX | | Every reload gives the maximum boost and releases the electric burst. |

#### Death Perception
| Type | Augment | Effect |
|---|---|---|
| Major | **Sixth Sense** | You see through walls: enemies in range are marked even when you can't see them. |
| Major | **Hunter's Eye** | Marks have no distance limit (instead of 20 m). |
| Major | **Killer Instinct** | Enemies are no longer marked. Instead, a warning shows when an enemy looks at you from behind, and every kill leaves a **UAV pulse** on the body for 30 s that reveals enemies within 15 m of it through walls. |
| Minor | **Persistence** | The mark stays 3 s after you lose sight of the enemy. |
| Minor | **Cat's Eyes** | Stronger dark vision, and flashbangs blind you half as much. |
| Minor | **Shrapnel Sense** | Enemy grenades are marked with their trajectory and danger radius. |
| Minor | **Reading** | The outline's color shows the enemy's health. |
| Major drawback | Interference · Short Sight · Paranoia | outlines flicker · marks reach only 12 m · fake marks appear now and then |
| Minor drawback | Lag · Glare · Migraine · Overload | marks update once a second · flashbangs blind you 30% more · each mark costs 2 energy · +15% sway with 3+ marks |
| APEX | | Marks stay 5 s without line of sight and show the enemy's health. |

- The outline is **edge only**, thin and translucent, so it doesn't cover the enemy. Its opacity is in F12 (*Outline opacity*).
- Dark vision raises brightness when the scene is dark, without a night-vision tint.

#### Tombstone Soda
| Type | Augment | Effect |
|---|---|---|
| Major | **Offering** | If you die, what you carry in your pockets and rig comes back by mail (once per raid). |
| Major | **Poltergeist** | The tombstone bursts when the specter ends and stuns enemies within 5 m for 2 s. |
| Major | **Haunting** | You can shoot as a specter: the first shot deals double damage and ends the specter. |
| Minor | **Long Haunt** | The specter lasts 6 s (instead of 4). |
| Minor | **Restless** | Tombstone recharges in 2 minutes (instead of 3). |
| Minor | **Ectoplasm** | Becoming a specter stops bleeds and heals 15 HP. |
| Minor | **Cold Trail** | When the specter ends, bots take 3 s longer to find you. |
| Major drawback | Short Haunt · Heavy Soul · Marked | lasts 2 s · recharges in 5 minutes · the closest bot knows where you are when it ends |
| Minor drawback | Cold · Wail · Slow Ghost · Fragile Tombstone | no stamina when it ends · a 40 m noise · no speed bonus · the tombstone draws fire for only 1.5 s |
| APEX | | Every PMC kill recharges Tombstone, and the specter lasts 6 s. |

#### Double Tap Root Beer
| Type | Augment | Effect |
|---|---|---|
| Major | **Double Shot** | At max rhythm, each hit has a 15% chance to count as two bullets (×2 damage) without using an extra round. |
| Major | **Perforation** | The rhythm also raises penetration (up to +8 at max rhythm), only against armor of class 4 or lower. Great for SMGs and pistols. |
| Major | **Momentum** | Missing no longer resets the rhythm; only stopping fire for 1 s does. |
| Minor | **Light Trigger** | Semi-automatic weapons also build rhythm with every hit. |
| Minor | **Cold Barrel** | The extra fire rate doesn't heat up your weapon. |
| Minor | **Feed** | The extra fire rate doesn't raise malfunctions. |
| Minor | **Metronome** | The rhythm goes up to +25% (instead of +15%). |
| Major drawback | Hungry Mag · Out of Step · Jam | at max rhythm 5% of shots use 2 rounds · missing slows you 10% for 1 s · +50% malfunctions at max rhythm |
| Minor drawback | Slow Start · Kick · Wear · Irregular | first 3 shots 10% slower · +10% recoil at max rhythm · +20% weapon wear · the bonus varies ±10% |
| APEX | | The rhythm always starts at max. |

---

### Perks on enemies and teammates
Enemies carrying a perk get its effect, and PIT Fireteam teammates get the perks you equip in their 4 slots. A notice warns you when an enemy perk triggers, and the icons of their perks float over their heads (in line of sight, within 60 m; F12 > *Perk icons over enemies*).

| Perk | On an enemy |
|---|---|
| Juggernog | Takes half damage |
| Speed Cola | Reloads ×1.5 faster |
| Stamin-Up | Infinite stamina and immune to pain |
| PHD Flopper | Immune to explosions and falls |
| Widow's Wine | Its shots slow you; its grenades web you (8 m) |
| Deadshot | Knows where its enemies are through walls (80 m) and +30% penetration |
| Bandolier Bandit | Spawns with extra ammo |
| Vulture Aid | Spawns with a valuable item |
| Melee Macchiato | Melee ×2. Takes 80% less damage until you hit it in melee, and a head or thorax shot won't drop it before the rest of its body |
| **Quick Revive** | Regenerates. A lethal hit can leave it **downed** instead of dead: finish it with a headshot, or it gets back up later with full health and comes for you |
| **Mule Kick** | Spawns with a 3rd weapon, and swaps weapons instead of reloading |
| **Electric Cherry** | Reloading releases a discharge (3 m) that hurts and stuns you |
| **Death Perception** | Hunts only you and roughly knows where you are |
| **Tombstone Soda** | Drops a live grenade when it dies (3 s to get away) |
| **Double Tap** | Fires 25% faster |

Everything can be turned off in F12 > *Perk effects on enemies and teammates*.

---

### Black Ops 7 weapons
Six new weapons, ported with their **own models, first-person animations and sounds** from Black Ops 7, built on an EFT weapon so they take its attachments and camos. **MK35 ISR** and **VMP** now also have their BO7 animations and sounds.

| Weapon | Type | Built on | Price |
|---|---|---|---|
| **MK35 ISR** | Assault rifle | HK416A5 (5.56) | |
| **VMP** | SMG | MP5 Navy (9×19) | |
| **Peacekeeper Mk1** | Assault rifle | AK-12 (5.45) | 120,000 ₽ |
| **Voyak KT-3** | Assault rifle | AK-74M (5.45) | 105,000 ₽ |
| **MXR-17** | Assault rifle | SCAR-L (5.56) | 115,000 ₽ |
| **VST** | SMG | MP9 (9×19) | 85,000 ₽ |
| **M10 Breacher** | Pump shotgun | M590 (12ga) | |
| **Hawker HX** | Bolt-action sniper | AXMC (.338 LM), with a S&B scope | |

- **How to get them:** Mechanic (loyalty level 1), already built. MK35 ISR and VMP also come out of the Mystery Box.
- Usable iron sights, correct icons and the same size as their real counterparts.
- Compatible with **Weapon Camo and Stickers**.

### Black Ops animations
- **Drinking:** every perk is drunk with the Black Ops 7 animation (the can in your hand, the pull tab, the gulp). On by default; F12 > *Black Ops drinking animation*. The can size in your hand is also in F12.
- **Melee:** all 23 EFT melee weapons use Black Ops 7 animations (knives, machetes, axes, crowbars, sledgehammers...). F12 > *Black Ops melee animations*; turn it off to go back to the melee styles.

---

### Machines on every map
- Perk machines, Der Wunderfizz, the Mystery Box spots, TEDD's spots and the **power switch** now appear on **every map**. Maps without hand-placed spots get them **automatically**: next to walls, close to loot, on the navmesh, spread out.
  - F12 > Machines > *Automatic machine placement*: **Fixed** (found once and kept), **Every raid** (new spots every raid) or **Off**.
  - F12 > Machines > *Redo automatic machines*: the next raid throws away that map's automatic spots and finds new ones.
- **Machine editor** (F12 > Machines, or **End** in raid): place your own pieces anywhere. Hand-placed pieces always win over automatic ones.

  | Key | Action |
  |---|---|
  | PageUp / PageDown | Next / previous piece (perk machines, Der Wunderfizz, Mystery Box spot, TEDD spot, power switch) |
  | [ / ] | Rotate |
  | \\ | Snap the rotation to 45° |
  | Insert | Place |
  | Delete | Delete the nearest piece |
  | F11 | Screenshot |

- **Der Wunderfizz** is the machine for the 9 perks without their own machine (Bandolier, Vulture Aid, Melee Macchiato and the 6 new ones).
- Machines are easier to use: 1.6 m reach and more tolerance for height differences.
- **Dynamic Maps** shows the machines and the power switch on its map (see Compatibility).

### Mystery Box
- **Wonder weapons:** with Kenner's **Ray Gun**, **Ray Gun Mark II** or **Wunderwaffe DG-2** installed, the box has a 10% chance to give one of them, loaded the same way their wall buy hands them out.
- Black Ops 7 weapons: 15%.

### Perk case
- A case (2×2 outside, 5×4 inside) that only holds perk bottles, perk injectors and APEX cores.
- Therapist (loyalty level 1, 65,000 ₽) and the flea market.

### APEX
- The 6 new perks have their APEX (see each perk above).
- **APEX Stamin-Up:** stamina regenerates ×3 (it used to be infinite).
- **APEX Vulture Aid:** 15% of enemies (it used to be 50%), and never LEDX, graphics cards or Bitcoin.
- **APEX Bandolier:** its challenge needs 40 kills (it used to be 60).
- **APEX cores** stop being absorbed once there are no challenges left to start, so you can sell them.

### Classic mode
- The 5th perk (the **modifier**) now gives **50%** of its base effect, plus its fixed **signature minor augment**:

  | Perk | Signature | Perk | Signature |
  |---|---|---|---|
  | Speed Cola | Mag Juggler | Bandolier Bandit | Cold Barrel |
  | Stamin-Up | Light Feet | Vulture Aid | Sharp Eyes |
  | Juggernog | Thick Skin | Melee Macchiato | Quick Draw |
  | PHD Flopper | Soft Soles | Quick Revive | Patience |
  | Widow's Wine | Quick Spinner | Mule Kick | Chamber Ready |
  | Deadshot | Quick Scope | Electric Cherry | Battery |
  | Death Perception | Persistence | Tombstone Soda | Long Haunt |
  | Double Tap | Cold Barrel | | |

- The modifier can also earn a 2nd minor augment through the raid challenge.

### Achievements
- **45 new achievements**, 5 for each perk added since 3.0 (Bandolier, Vulture Aid, Melee Macchiato and the 6 new ones): first drink, total progress, level 5, all challenges and a secret one. **127** in total.
- New sound when you complete a challenge: the Black Ops 3 rank-up.

---

### Compatibility with other mods
Everything is optional and detected automatically: without these mods nothing changes.

| Mod | What Tarkova-Cola does with it |
|---|---|
| **SAM (Sage's Advanced Movement)** | With **PHD Flopper**, landing a dive sets off an explosion where you land (the classic dolphin dive; it never hurts you or your armor). The slide is **twice as fast** and goes 50% farther, with no slow tail, pink energy trail, sparks and sound. Crouching again ends it instantly, and you come out of it sprinting at full speed. |
| **Dynamic Maps** | Its map and minimap show the perk machines (with their perk icon), Der Wunderfizz, the Mystery Box, TEDD and the power switch, and the enemies marked by **Death Perception**, live. |
| **SAIN** | Stunned bots (Widow's Wine, Electric Cherry, Tombstone) stop shooting while the stun lasts. |
| **QuestingBots** | The storyline guards and bosses stay at their post instead of going off on quests. |
| **K9 Companion** | Your dog takes no damage from perk explosions (PHD dive, APEX). |
| **PIT Fireteam** | Teammates use the perks in their 4 slots, and perks never stun, mark or hurt them. |
| **Allies in general** | Any bot that isn't hostile to you (for example Scavs when you play as a Scav) is never stunned, marked or hurt by your perks. If it attacks you, it stops counting as an ally. |
| **Ray Gun, Ray Gun Mark II, Wunderwaffe DG-2** (Kenner) | They can come out of the Mystery Box. |
| **TacticalReload** | Speed Cola's 2 shells per insertion respect the tube capacity together with its multi-shell reloads (Benelli M3). |
| **MagRetention, Kirisame Mag Reload, StripperClips, Speedloader** | Speed Cola and Electric Cherry work with their reloads. |
| **Weapon Camo and Stickers** | Camos and stickers apply to the Black Ops 7 weapons. |
| **DualWieldLab** | The Mule Kick slot works alongside its extra holster. |
| **Arena Mode** | Supported since 2.2 (Zombies mode drops perks). |

### Translations
- **Every text** in the mod can be translated with plain JSON files: the Research, APEX and Classic screens, notices, perks, augments, machines, F12 options, items, achievements and quests.
- The mod writes up-to-date **templates** (`en.json` and `es.json`) every time it starts, in `BepInEx/plugins/TarkovaCola/locales/` and `SPT_Runtime/user/mods/TarkovaCola/locales/`.
- Copy `en.json` as your language code (`ch`, `ru`, `fr`, `de`, `jp`...), translate the values and restart. F12 > *Language* lists every installed translation.
- The full guide is in **`TRANSLATING.md`**. Send your translation and it will be bundled with credit to you.

### Other changes
- **Speed Cola on manual weapons:** pump shotguns, bolt-action rifles and revolvers reload **twice as fast** and cycle the bolt or pump twice as fast. Tube-fed shotguns load **2 shells per insertion**.
- **PHD Flopper:** explosions no longer wear your armor.
- **F12 > Audio:** separate volume for the perk jingles and for the sound effects.
- **F12 > Perks > Pause perk effects:** turns every perk effect off without losing your perks, to compare.
- **Bandolier and Widow's Wine** have their own cans, and the can icons in the inventory are no longer white.
- Perks can't be drunk in the Hideout.
- The **Research**, APEX, Eternal and ESC panels fit the 15 perks.

### Balance pass
**Challenges that could be cheated**
- PHD Flopper's absorb challenge only counts falls that would really hurt.
- Vulture Aid's search challenge needs you to open the container or body.
- Several challenges now need their perk active.

**Lower goals**

| Augment | New challenge |
|---|---|
| Stamin-Up: Rush Hour | 3 kills in under 3 minutes, 3 times |
| Widow's Wine: Nest | Web 3 enemies with one grenade, 5 times |
| Melee Macchiato: Flash Strike | 100 melee attacks |
| Melee Macchiato: Charge | Sprint 1 km with a melee weapon |
| Deadshot: To the Skies | Fire 800 rounds |
| Bandolier: Cold Barrel | Fire 600 rounds |
| Juggernog: Bone Plating | 40 hits on your armor |
| Juggernog: Skull Cap | Take 600 damage |
| Stamin-Up: Light Feet · PHD: Terrain Walker | 3 km |

**Effects**
- **Dead Aim:** sway ×0.35 instead of none.
- **Second Wind:** 90 s cooldown.
- **Iron Skull:** once per raid.
- **Web radius:** 8 m.

**Drawbacks that now affect their own perk**

| Perk | Drawback | Effect |
|---|---|---|
| Juggernog | **Cold Armor** | The damage reduction starts at 0 on the first hit of a fight and takes 3 s to reach full. |
| PHD Flopper | **Own Wave** | Explosions don't hurt you, but they shake you: +30% sway for 2 s. |
| Deadshot | **Tired Eye** | After aiming for 8 s straight, the sway reduction drops by half. |
| Vulture Aid | **Picky Vulture** | Only containers and bodies worth 100,000 ₽ or more are marked. |
| Melee Macchiato | **Fleeting Edge** | The ×2 damage only applies to the first hit on each enemy. |
| Speed Cola | **Noisy Reload** | Every reload with Speed Cola makes noise that bots within 15 m hear. |

**Removed**
- **Synergies.** Their achievements are hidden; your other achievements are untouched.
- The developer tools in F12 (copy position, record clip, melee debug), and the *Melee styles* and *Center light melee* options: the styles are always on when the Black Ops melee animations are off.
