## Tarkova-Cola {.tabset}

### Description

**Tarkova-Cola brings Call of Duty Zombies Perk-a-Colas to Tushonka!** (SPT 4.1.x, single player)

Drink a perk in raid and it stays active until the raid ends. Level each perk up with the XP you earn, complete challenges to unlock augments, and pair them with drawbacks. The build you equip changes what you chase in raid and which playstyle fits you.

**The 6 perks**

| Perk | Base effect (rest of the raid) |
| ---- | ------------------------------ |
| **Speed Cola** | Reloads x1.5 faster |
| **Stamin-Up** | Leg stamina x1.5 |
| **Juggernog** | -33% damage taken |
| **PHD Flopper** | Immune to fall and explosion damage |
| **Widow's Wine** | Your grenades web enemies (-60% movement speed) |
| **Deadshot Daiquiri** | Aim 15% faster and 25% less weapon sway |

**What's in 2.0**

- **Two modes**, switch them in Research:
  - **Modern**: full progression with augments and drawbacks. Drink a perk you already have and it becomes an **Eternal Perk** (blue outline): it stays with you between raids, loses a drawback and comes back by mail if you die.
  - **Classic**: no Research and no drawbacks. Up to 4 perks plus 1 **modifier** (reduced effect), each with a random minor augment. **Your perks stay with you until you die.** Every raid brings an optional challenge that grants a second augment.
- **Random Perk power-up**: kills can drop a floating bottle (4%, once per raid, 60 s to grab it), with announcer voice.
- **15 Synergies**, one for every pair of perks: a small bonus when you carry both. Unlock them with their own challenge, and equip one at a time.
- **The Bottlers storyline** (needs **VisitAPI**): 10 quests from Skier and Therapist about a crew of ex-PMCs who got their hands on the Perk-a-Colas. The guards really drink their perks, and you can see their icons over their heads. The final quest pits you against **the Bartender**, who carries every perk.
- **Crafting**: brew perks in the hideout **Nutrition Unit** (recipes unlocked through the storyline).
- **100 native EFT achievements** (Profile > Achievements), rewards by mail.
- Visual effects for PHD Flopper and Widow's Wine, and a new bottle for Deadshot Daiquiri.

**How to use**

1. Get a perk: buy it from **Therapist** (loyalty level 1, **7 GP Coin**), find it in raid (ammo crates, safes, Rogues and Killa) or craft it.
2. Drink it in raid.
3. Earn XP in raid (+25% while the perk is active) to level it up (max level 5).
4. Open **RESEARCH** in the main menu (bottom bar, next to Handbook, or press **F9**) to see your progress, equip augments and drawbacks, pick a synergy and switch modes.
5. Press **ESC** in raid to see your level and pending challenges.

**Requirements**

- **SPT 4.1.x**.
- **Optional: VisitAPI 1.3.5+** for the storyline. Without it there are no quests and every perk is sold by Therapist from the start; everything else works the same. **With** it, each perk's sale at Therapist unlocks with its quest (you can still find and craft them). Debug option in F12: *Perks for sale without quests*.

**Updating from 1.x:** extract over your SPT folder. Your progress is kept.

**About the use of AI.** The code of this mod was written with the help of an AI assistant, directed, tested and published by me. You can check the GitHub repository. The 3D models are **not** mine (see the License & Credits tab), and the Call of Duty icons, jingles and voice lines belong to their owners. I take full responsibility for any inconsistency or bug in the mod.

information
**Fika:** not guaranteed to work with Fika. Progress is saved on each player's own SPT server, so every server involved needs the server part of the mod installed. Please report issues here or on GitHub.

### License & Credits

**Code: MIT License**

Copyright (c) 2026 yendaxddd

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, subject to the conditions of the MIT License. The full text is in the LICENSE file of the GitHub repository.

**Third-party material (NOT covered by the MIT license)**

- **Speed Cola 3D model:** "Speedcola CoD Zombies" by **albertjuli** ([Sketchfab](https://skfb.ly/6VRDU)), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Changes: converted to a Unity asset bundle, textures re-prepared (metal/smoothness adjusted), rescaled and rotated for in-game use.
- **Other bottles** (Stamin-Up, Juggernog, PHD Flopper, Widow's Wine, Deadshot Daiquiri): "cod zombies perks" by **Unknown Dev** ([Sketchfab](https://sketchfab.com/3d-models/cod-zombies-perks-77e9cb71adaf44b5a974c584f96cfc6c)), Sketchfab Free Standard license. Only the compiled, game-ready asset bundles are included, not the original model files.
- **Icons, jingles and announcer voice lines:** Call of Duty, property of their respective owners.

If you are the owner of any of this material and want it removed, please open an issue on GitHub and it will be taken down.

### Challenges, Augments & Drawbacks

Complete a challenge to unlock an **augment** and its linked **drawback**. You can equip 1 major + 1 minor augment (2 minors with Extra Slot), and **every augment needs its drawback equipped** (major with major, minor with minor).

**Levels (max 5)**

| Level | XP to next level | Total XP |
| ----- | ---------------- | -------- |
| 1 to 2 | 4,050 | 4,050 |
| 2 to 3 | 5,400 | 9,450 |
| 3 to 4 | 6,750 | 16,200 |
| 4 to 5 | 8,100 | 24,300 |

#### Speed Cola

| Lvl | Augment | Challenge | Linked drawback |
| --- | ------- | --------- | --------------- |
| 1 | **Quick Hands** (Major): Drawing and swapping weapons is 50% faster. | Kill 3 enemies shortly after swapping weapons, with Speed Cola active. | **Jittery Hands**: If an arm has an unhealthy effect (fracture, destroyed, light/heavy bleed), weapon sway rises by 25%. |
| 1 | **Steady Fingers** (Minor): Your arms take 30% less damage. | Get your arms broken 4 times with Speed Cola active. | **Dry Mouth**: Hydration drains 20% faster. |
| 2 | **Bolt Runner** (Major): If your weapon jams, it clears itself when you put it away. | Clear a weapon jam 5 times with Speed Cola active. | **Overpressure**: Every 4th magazine loses all its rounds when reloaded (they return to your inventory). |
| 2 | **Mag Juggler** (Minor): Magazines for your primary weapon load 50% faster in your inventory. | Load 20 magazines in raid with Speed Cola active. | **Sticky Mags**: +10% jam chance. |
| 3 | **Chamber Check** (Minor): Checking magazines in your inventory is 75% faster. | Check your weapon's magazine 20 times in raid. | **Loud Slurp**: Drinking it makes noise that nearby bots can hear. |
| 3 | **Round Counter** (Minor): Magazines show their exact ammo count, checked or not. | Check magazines in your inventory 15 times. | **Sweet Tooth**: Eating or drinking anything else takes twice as long. |
| 4 | **Sugar Rush** (Major): Speed Cola's reload bonus rises from x1.5 to x2.0. | Kill 4 PMCs. | **Sugar Crash**: Every 2 min without killing an enemy, your thirst multiplier rises (max x2). A kill resets it. |
| 5 | **Extra Slot** (Special): You can carry 2 minor augments (1 by default). | Kill 1 boss with Speed Cola active. | — |

#### Stamin-Up

| Lvl | Augment | Challenge | Linked drawback |
| --- | ------- | --------- | --------------- |
| 1 | **Rush Hour** (Major): After a kill, your leg stamina refills completely. | Get 3 kills in under 3 minutes with Stamin-Up active. | **Loud Boots**: Sprinting is noisy: bots can hear you from 25 m. |
| 1 | **Light Feet** (Minor): You carry 10% less weight. | Sprint 5 km in total with Stamin-Up active. | **Hungry Legs**: You burn 50% more energy while sprinting. |
| 2 | **Quick Recovery** (Minor): Stamina recovers 25% faster. | Sprint 10 km in total with Stamin-Up active. | **Cramps**: After you stop sprinting, stamina takes 2 s longer to start recovering. |
| 2 | **Soft Landing** (Minor): Fall damage is reduced by 30%. | Kill 3 enemies who are at least 2 m below you, with Stamin-Up active. | **Sweaty**: You burn 50% more hydration while sprinting. |
| 3 | **Second Wind** (Major): Every time your health is in the red: 5 s of infinite stamina and pain immunity, no cooldown. You can still take damage. | Run out of stamina 5 times with Stamin-Up active. | **Jelly Legs**: After sprinting 5 s in a row, weapon sway rises by 25% for 6 s. |
| 3 | **Iron Lungs** (Minor): You hold your breath 60% longer and recover it faster. | Hold your breath 20 times with Stamin-Up active. | **Tight Chest**: Below 30% stamina, weapon sway rises by 10%. |
| 4 | **Sprint Shooter** (Major): Weapon animations always run 1.75x faster (reloads are not affected). | Find 15 Hot Rod, RatCola or Max Energy in raid. | **Burnout**: Below 30% stamina, you recover at half speed. |
| 5 | **Extra Slot** (Special): You can carry 2 minor augments (1 by default). | Kill 1 boss with Stamin-Up active. | — |

#### Juggernog

| Lvl | Augment | Challenge | Linked drawback |
| --- | ------- | --------- | --------------- |
| 1 | **Skull Cap** (Minor): Higher chance that bullets ricochet off your helmet. | Take 1,500 damage with Juggernog active. | **Dead Weight**: Sprinting burns 20% more stamina. |
| 1 | **Thick Skin** (Minor): You bleed less, but your bones break more easily. | Fix 5 heavy bleeds with Juggernog active. | **Sluggish Reflexes**: Drawing and swapping weapons is 20% slower. |
| 2 | **Quick Patch** (Major): Healing animations are twice as fast. | Use 10 medical items with Juggernog active. | **Clanky Plates**: Your footsteps are audible from 12 m when walking. |
| 2 | **Heavy Bones** (Minor): Your bones break less, but you bleed more easily. | Fix 5 fractures with Juggernog active. | **Slow Metabolism**: Hunger and thirst drop 20% faster. |
| 3 | **Bone Plating** (Major): Your armor plates take twice the damage. | Take 60 hits on your armor with Juggernog active. | **Heavy Plates**: You carry 10% more weight. |
| 3 | **Keep Going** (Minor): Hunger and thirst drain your health much more slowly. | Eat or drink 15 times with Juggernog active. | **Slow Chewer**: Eating and drinking takes 30% longer. |
| 4 | **Last Bastion** (Major): Once per raid, a lethal hit to the head or thorax leaves you at 1 HP. | Extract alive 5 times with Juggernog active. | **Heavy Hands**: Weapon sway rises by 20%. |
| 5 | **Extra Slot** (Special): You can carry 2 minor augments (1 by default). | Kill 1 boss with Juggernog active. | — |

#### PHD Flopper

| Lvl | Augment | Challenge | Linked drawback |
| --- | ------- | --------- | --------------- |
| 1 | **Blast Absorber** (Major): You heal for 25% of the explosion damage you deal. | Deal 500 explosion damage with PHD Flopper active. | **Concussed**: After a nearby explosion, weapon sway rises by 40% for 8 s. |
| 1 | **Soft Soles** (Minor): Landing after a fall doesn't slow you down. | Travel 2 km with PHD Flopper active. | **Stagger**: Landing from a fall of 3 m or more costs you 40 stamina. |
| 2 | **Pit Stop** (Minor): Standing still recovers stamina twice as fast. | Stand still for 5 minutes in total with PHD Flopper active. | **Lazy Legs**: Leg stamina is reduced by 15%. |
| 2 | **Ear Plugs** (Minor): You are immune to contusion. | Suffer 5 contusions with PHD Flopper active. | **Heavy Pockets**: You carry 10% more weight. |
| 3 | **Terrain Walker** (Minor): You are immune to barbed wire and mud. | Travel 5 km with PHD Flopper active. | **Hard Stomp**: Your footsteps are audible from 15 m. |
| 4 | **Shockwave** (Major): Your grenades have double the radius and half the fuse time. | Kill 5 enemies with grenades with PHD Flopper active. | **Heavy Throw**: Throwing a grenade costs you 25 stamina. |
| 5 | **Extra Slot** (Special): You can carry 2 minor augments (1 by default). | Kill 1 boss with PHD Flopper active. | — |

#### Widow's Wine

| Lvl | Augment | Challenge | Linked drawback |
| --- | ------- | --------- | --------------- |
| 1 | **Venomous Bite** (Minor): Enemies caught in the web are also stunned. | Kill 5 webbed enemies with Widow's Wine active. | **Entangled**: You slow down by 40% inside your own webs. |
| 1 | **Quick Spinner** (Minor): You ready and throw grenades 40% faster. | Throw 10 grenades with Widow's Wine active. | **Greasy Fingers**: Drawing and swapping weapons is 20% slower. |
| 2 | **Sticky Floor** (Minor): You can throw grenades 35% farther. | Web 15 enemies with Widow's Wine active. | **Hungry Spider**: Energy drops 20% faster. |
| 3 | **Spinneret** (Major): The web has double the radius and lasts 2 s longer. | Web 25 enemies with Widow's Wine active. | **Loud Throw**: Throwing a grenade makes noise (20 m). |
| 3 | **Safe Fuse** (Major): Explosions don't damage your armor. | Web 3 enemies with a single grenade. | **Brittle Plates**: Your armor takes 25% more damage from bullets. |
| 4 | **Web Burst** (Major): If you take 100 damage in under 3 s, a grenade from your inventory is spent: nearby enemies are webbed and stunned (once every 5 min). | Kill 3 enemies with grenades with Widow's Wine active. | **Bitter Wine**: Sprinting burns 15% more stamina. |
| 4 | **Widow's Bite** (Minor): If one of your grenades kills an enemy, it is returned to you (it arrives by mail after the raid). | Kill 10 enemies with grenades with Widow's Wine active. | **Dry Throat**: Hydration drops 20% faster. |
| 5 | **Extra Slot** (Special): You can carry 2 minor augments (1 by default). | Kill 1 boss with Widow's Wine active. | — |

#### Deadshot Daiquiri

| Lvl | Augment | Challenge | Linked drawback |
| --- | ------- | --------- | --------------- |
| 1 | **Quick Scope** (Minor): You aim down sights 20% faster. | Kill 10 enemies while aiming, with Deadshot active. | **Eye Strain**: You aim down sights 10% slower. |
| 1 | **Steady Pulse** (Minor): You hold your breath 40% longer. | Hold your breath while aiming 25 times, with Deadshot active. | **Bitter Lime**: Thirst drops 20% faster. |
| 2 | **Dead Aim** (Major): Your weapon does not sway while you move. | Kill 20 enemies while moving, with Deadshot active. | **Tunnel Vision**: Drawing and swapping weapons is 15% slower. |
| 2 | **Headhunter** (Minor): A headshot kill restores 20% of your stamina. | Kill 15 enemies with headshots, with Deadshot active. | **Sugar Rim**: Hunger drops 20% faster. |
| 3 | **Incredible** (Major): Your bullets have 30% more armor penetration. | Kill 15 armored enemies with Deadshot active. | **Heavy Breathing**: Sprinting burns 15% more stamina. |
| 3 | **Stillness** (Minor): Crouched or prone, sway drops by another 25%. | Kill 10 enemies while crouched or prone, with Deadshot active. | **Squeaky Boots**: Your footsteps are audible from 12 m when walking. |
| 4 | **To The Skies** (Major): Vertical and horizontal recoil drop by 50%. | Fire 1,500 rounds with Deadshot active. | **Shaky Hands**: When not aiming, weapon sway rises by 20%. |
| 5 | **Extra Slot** (Special): You can carry 2 minor augments (1 by default). | Kill 1 boss with Deadshot active. | — |


### Synergies

Carry both perks of a pair to activate its synergy. Unlock each one with its challenge (it only advances with both perks active) and equip **one** at a time in the **SYNERGY** tab of Research. In Classic mode, if one of the two perks is your modifier, the bonus is smaller.

| Perks | Synergy | Bonus | Unlock challenge |
| ----- | ------- | ----- | ---------------- |
| Speed Cola + Stamin-Up | **Cardio** | You draw and swap weapons 12% faster and sprinting uses 5% less stamina. | Sprint 3 km with Speed Cola and Stamin-Up active. |
| Speed Cola + Juggernog | **Combat Medic** | Healing with medical items is 15% faster. | Use 10 medical items with Speed Cola and Juggernog active. |
| Speed Cola + PHD Flopper | **Blast Reload** | When PHD Flopper cancels an explosion or a fall, you reload 20% faster for 6 s. | Absorb 5 explosions or falls with Speed Cola and PHD Flopper active. |
| Speed Cola + Widow's Wine | **Quick Pin** | You throw grenades 15% faster. | Throw 10 grenades with Speed Cola and Widow's Wine active. |
| Speed Cola + Deadshot | **Hair Trigger** | You aim down sights 8% faster. | Kill 15 enemies while aiming, with Speed Cola and Deadshot active. |
| Stamin-Up + Juggernog | **Tank** | You carry 5% less weight. | Sprint 2 km with Stamin-Up and Juggernog active. |
| Stamin-Up + PHD Flopper | **Bounce Back** | When PHD Flopper cancels an explosion or a fall, you recover 15% stamina. | Absorb 8 explosions or falls with Stamin-Up and PHD Flopper active. |
| Stamin-Up + Widow's Wine | **Getaway** | Throwing a grenade restores 10% stamina. | Throw 10 grenades with Stamin-Up and Widow's Wine active. |
| Stamin-Up + Deadshot | **Runner's Breath** | You hold your breath 15% longer. | Hold your breath while aiming 20 times, with Stamin-Up and Deadshot active. |
| Juggernog + PHD Flopper | **Shock Shield** | When PHD Flopper cancels an explosion or a fall, you take 10% less damage for 5 s. | Absorb 5 explosions or falls with Juggernog and PHD Flopper active. |
| Juggernog + Widow's Wine | **Steel Web** | You take 3% less damage. | Kill 5 enemies caught in your web with Juggernog and Widow's Wine active. |
| Juggernog + Deadshot | **Iron Sights** | While aiming you take 5% less damage. | Kill 10 armored enemies with Juggernog and Deadshot active. |
| PHD Flopper + Widow's Wine | **Blast Web** | Your webs cover a 20% larger radius. | Web 15 enemies with PHD Flopper and Widow's Wine active. |
| PHD Flopper + Deadshot | **Death From Above** | When PHD Flopper cancels an explosion or a fall, weapon sway drops 40% for 5 s. | Kill 3 enemies within 10 s of absorbing an explosion or fall, with PHD Flopper and Deadshot active. |
| Widow's Wine + Deadshot | **Spider Eye** | You deal 10% more damage to enemies caught in your web. | Kill 8 enemies caught in your web, with Widow's Wine and Deadshot active. |

### Storyline

*Requires VisitAPI 1.3.5+.* Available from PMC level 12.

| # | Quest | Given by | Map | Unlocks |
| - | ----- | -------- | --- | ------- |
| 1 | Cold Cargo | Skier | Reserve | Speed Cola and Stamin-Up recipes |
| 2 | The Formula | Therapist | Shoreline | Juggernog and PHD Flopper recipes |
| 3 | Bottling Line | Therapist | Customs | Widow's Wine and Deadshot Daiquiri recipes |
| 4 | Fast Hands | Therapist | Interchange | Speed Cola sale |
| 5 | Long Haul | Therapist | Woods | Stamin-Up sale |
| 6 | Meat Wall | Therapist | Streets of Tarkov | Juggernog sale |
| 7 | Zero Gravity | Therapist | Lighthouse | PHD Flopper sale |
| 8 | Black Widow | Therapist | Shoreline | Widow's Wine sale |
| 9 | Hawkeye | Therapist | Customs | Deadshot Daiquiri sale |
| 10 | Last Call | Skier | Reserve | The end of the Bottlers |

Every quest sends you to a Bottlers post: the guards drink the quest's perk and it really works on them (Juggernog guards take less damage, PHD guards ignore your grenades, Widow's Wine guards slow you down...). One of them carries the item you need. Recipes are crafted in the **Nutrition Unit**.

### Gallery

![Speed Cola in the trader](https://i.imgur.com/rGoP6Qe.png) Speed cola in the trader screen

![Speed Cola in hand](https://i.imgur.com/oEexJnv.png) Speed cola model in hand

![Research Button](https://i.imgur.com/m58EOf0.png) Research button

![Research screen](https://i.imgur.com/qdO1B1d.png) Research screen

![Challenges tracker](https://i.imgur.com/VVfT3pG.png) Challenges tracker

![Achievements](https://i.imgur.com/CMonIIe.png) Achievements

{.endtabset}
