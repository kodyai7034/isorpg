# IsoRPG — Class/Job System Design

## Research Summary

Analyzed 8 major tactics RPGs. Key findings:
- FFT's 5-slot equip system is the gold standard for build depth
- 20-25 classes is the sweet spot for a full game; 8-12 for MVP
- Branching unlock trees with multi-class prerequisites create the best progression
- Permanent stat shaping (FFT-style) adds depth but creates exploitation — use character-intrinsic growth instead
- JP spillover is an underappreciated anti-grind mechanic
- Advanced classes should be specialists, not straight upgrades

---

## Our Design: "Ivalice-Inspired, Modern-Refined"

### Core Philosophy

Take FFT's 5-slot system and branching tree as our foundation, but:
1. **Remove permanent stat shaping** — stats grow based on character, not class. No Ninja speed exploit.
2. **Keep JP and ability persistence** — the best part of FFT
3. **Add JP spillover** — allies earn 25% JP in matching jobs
4. **Keep Brave/Faith** — but simplify to "Resolve" (physical affinity) and "Attunement" (magical affinity) with clearer mechanics
5. **Design advanced classes as specialists, not upgrades** — every class has a unique niche

---

## Job Roster

### MVP (8 jobs — System 9)

#### Tier 0: Starter
| Job | Role | Niche | Equips |
|-----|------|-------|--------|
| **Squire** | Starter/Support | Party buffs, JP boost, versatile | Swords, Light Armor |

#### Tier 1: Base Classes (unlock: Squire Lv 2)
| Job | Role | Niche | Equips |
|-----|------|-------|--------|
| **Knight** | Tank/Disabler | Break abilities (destroy enemy gear/stats) | Swords, Heavy Armor, Shields |
| **Archer** | Ranged Physical | Long range, height bonus, pinning shots | Bows, Light Armor |
| **Black Mage** | Offensive Caster | Elemental AoE, weakness exploitation | Rods, Robes |
| **White Mage** | Healer/Support | Healing, status cure, protective buffs | Staves, Robes |

#### Tier 2: Advanced (unlock: 2 Tier 1 jobs at Lv 3)
| Job | Role | Niche | Unlock Requires |
|-----|------|-------|-----------------|
| **Thief** | Utility/Disrupt | Steal, high evasion, Haste self | Knight 3 + Archer 3 |
| **Monk** | Hybrid Melee/Heal | Unarmed combat, self-heal, AoE physical | Knight 3 + White Mage 3 |
| **Red Mage** | Hybrid Caster | Both Black + White magic (weaker), Dualcast | Black Mage 3 + White Mage 3 |

### V1 Expansion (+6 jobs = 14 total)

#### Tier 2 Additions
| Job | Role | Niche | Unlock |
|-----|------|-------|--------|
| **Dragoon** | Mobile Striker | Jump attacks (ignore height), lance mastery | Knight 3 + Monk 3 |
| **Time Mage** | Battlefield Control | Haste/Slow, CT manipulation, gravity spells | Black Mage 3 + Archer 3 |
| **Geomancer** | Terrain Caster | Free spells based on terrain type, no MP cost | Monk 3 + Thief 3 |

#### Tier 3: Pinnacle (unlock: 3+ jobs at Lv 4)
| Job | Role | Niche | Unlock |
|-----|------|-------|--------|
| **Samurai** | AoE Physical | Blade arts (draw out), high damage, medium range | Knight 4 + Monk 4 + Dragoon 3 |
| **Ninja** | Speed DPS | Dual wield, throw items, highest Speed | Thief 4 + Archer 4 + Geomancer 3 |
| **Sage** | Ultimate Caster | Access to all magic schools, Arithmeticks-lite | Black Mage 4 + White Mage 4 + Time Mage 3 |

### V2 Expansion (+6 jobs = 20 total)
Dark Knight, Dancer, Bard, Summoner, Orator, Mime — designed later based on player feedback.

---

## Unlock Tree Visualization

```
                        Squire (Lv 0, default)
                       /    |    \         \
                Knight  Archer  Black Mage  White Mage
                  |  \    |  \     |  \        |  \
                  |   +---+   |    |   +-------+   |
                  |   Thief   |    |   Red Mage    |
                  |     |     |    |               |
                  +-----+    |    +---------+     |
                  Monk  |    |    Time Mage  |    |
                   |    |    |       |       |    |
                   +----+   |       +-------+    |
                   Dragoon  |       Geomancer    |
                     |      |          |         |
                     +------+----------+---------+
                     |            |            |
                   Samurai      Ninja        Sage
```

**Key rule**: Advanced classes require investment in MULTIPLE lower-tier classes. This prevents rushing and ensures players have a well-rounded party.

---

## 5-Slot Ability Equip System (FFT Model)

| Slot | What It Does | Example |
|------|-------------|---------|
| **Primary Action** | Fixed: current job's action set | Knight → "Arts of War" |
| **Secondary Action** | Any LEARNED action set from another job | Equip "White Magicks" on a Knight |
| **Reaction** | Auto-triggers on condition (chance = Resolve%) | Counter, Auto-Potion, Absorb MP |
| **Support** | Always-active passive | Dual Wield, Equip Shield, Attack Boost |
| **Movement** | Mobility enhancement | Move+1, Jump+2, Teleport, Ignore Height |

**One slot per category** — forces meaningful tradeoffs. A Knight can't have Counter AND Auto-Potion.

---

## JP (Job Points) System

### Earning JP
- Performing any action in battle earns JP for your **active job**
- Base JP per action: `10 + (JobLevel * 2)`
- **JP Spillover**: all allied units on the battlefield with the same active job earn 25% of JP earned by the acting unit
- Bonus JP for: killing an enemy (+20), healing a critical ally (+10), first action of battle (+15)

### Spending JP
- Each ability in a job has a JP cost to learn permanently
- Costs range: 50 JP (basic) to 800 JP (ultimate)
- Once learned, an ability can be equipped in the appropriate slot from ANY job

### JP Cost Tiers
| Tier | Cost Range | Examples |
|------|-----------|---------|
| Basic | 50-100 JP | Basic attack upgrades, simple buffs |
| Standard | 150-300 JP | Core job abilities, useful passives |
| Advanced | 400-600 JP | Signature abilities, powerful reactions |
| Ultimate | 700-800 JP | Job-defining capstone ability |

---

## Stat System

### Character-Intrinsic Growth (NOT Class-Dependent)

Unlike FFT, stats grow based on the **character**, not the class:
- Each character has fixed growth rates per stat (HP, MP, PA, MA, Speed)
- Leveling up in ANY class applies the same growth
- **No Ninja speed exploit** — there is no stat benefit to leveling as one class vs another
- Classes still modify DISPLAYED stats via **multipliers** (Knight shows higher HP, Mage shows higher MA), but these are temporary and change when you switch jobs

### Stat Multipliers by Job

```
Job          HP    MP    PA    MA   Speed
Squire      100%  100%  100%  100%  100%
Knight      120%   80%  110%   80%   90%
Archer      100%   80%  105%   85%  110%
Black Mage   80%  120%   75%  120%   95%
White Mage   90%  130%   70%  110%   95%
Thief        90%   80%   95%   80%  130%
Monk        110%   90%  115%   90%  105%
Red Mage     95%  110%   90%  105%  100%
```

### Resolve & Attunement (Simplified Brave/Faith)

| Stat | Range | Effect |
|------|-------|--------|
| **Resolve** | 0-100 | Physical reaction trigger chance, unarmed damage, certain weapon scaling |
| **Attunement** | 0-100 | Magic power multiplier (offense AND defense), healing effectiveness |

- Default: 70/70 for both
- Modified by specific abilities in battle
- Changes are **partially permanent**: 1 point per 4 modified persists after battle
- **No desertion** (removed FFT's frustrating auto-leave mechanic)
- High Resolve + Low Attunement = physical specialist
- Low Resolve + High Attunement = magic specialist
- Both high = versatile but triggers reactions unpredictably

---

## Equipment System

### Equipment Slots
| Slot | Options |
|------|---------|
| **Weapon** | Determined by job (sword, bow, rod, staff, fists, lance) |
| **Off-Hand** | Shield (Knight, Squire) or empty (most) or dual weapon (Ninja support ability) |
| **Armor** | Heavy (Knight, Dragoon), Light (Archer, Thief, Squire), Robes (Mages, Monk) |
| **Accessory** | Universal — stat boosts, status immunity, special effects |

### Support Ability Equipment Override
Like FFT, Support abilities can override equipment restrictions:
- "Equip Heavy Armor" lets any class wear knight armor
- "Equip Swords" lets any class use swords
- This consumes the Support slot — opportunity cost against Dual Wield, Attack Boost, etc.

---

## Ability Design Per Job (MVP 8 Jobs)

### Squire (4 abilities + passives)
| Ability | Type | JP | Effect |
|---------|------|-----|--------|
| Focus | Action | 50 | +PA to self for 3 turns |
| Rally | Action | 100 | +Speed to adjacent allies for 3 turns |
| Tailwind | Action | 200 | Grant CT+20 to target ally |
| Cheer | Action | 300 | +Resolve to target ally |
| Counter Tackle | Reaction | 150 | Physical counter at 50% damage |
| JP Boost | Support | 250 | +50% JP earned |
| Move+1 | Movement | 200 | +1 movement range |

### Knight (4 abilities + passives)
| Ability | Type | JP | Effect |
|---------|------|-----|--------|
| Shield Bash | Action | 100 | Damage + chance to Stun |
| Armor Break | Action | 200 | Reduce target Defense for 3 turns |
| Weapon Break | Action | 300 | Reduce target PA for 3 turns |
| Sentinel | Action | 500 | Take hits for adjacent allies this turn |
| Parry | Reaction | 200 | Chance to negate physical attack |
| Equip Heavy Armor | Support | 300 | Any class can equip heavy armor |
| Move-1, Jump+1 | Movement | 150 | Trade mobility for verticality |

### Black Mage (4 abilities + passives)
| Ability | Type | JP | Effect |
|---------|------|-----|--------|
| Fire | Action | 50 | Single-target fire damage |
| Blizzard | Action | 50 | Single-target ice damage |
| Thunder | Action | 100 | Single-target lightning damage |
| Firaga | Action | 500 | AoE fire damage (2-tile radius) |
| Magic Counter | Reaction | 300 | Counter with equivalent spell |
| Magic Boost | Support | 350 | +25% magic damage |
| Ignore Height | Movement | 400 | Abilities ignore height for range calc |

### White Mage (4 abilities + passives)
| Ability | Type | JP | Effect |
|---------|------|-----|--------|
| Cure | Action | 50 | Heal single target |
| Protect | Action | 100 | Reduce physical damage for 3 turns |
| Esuna | Action | 200 | Remove negative status effects |
| Curaga | Action | 500 | AoE heal (2-tile radius) |
| Auto-Potion | Reaction | 150 | Auto-use potion when hit |
| Arcane Defense | Support | 300 | +25% magic resistance |
| Levitate | Movement | 350 | Ignore terrain movement costs |

### Archer (4 abilities + passives)
| Ability | Type | JP | Effect |
|---------|------|-----|--------|
| Aimed Shot | Action | 100 | High accuracy, ignores evasion |
| Pinning Shot | Action | 200 | Damage + Immobilize for 1 turn |
| Barrage | Action | 400 | 3 hits at 50% damage each |
| Snipe | Action | 600 | Double range, double damage, skip next turn |
| Arrow Guard | Reaction | 200 | Chance to dodge ranged attacks |
| Concentrate | Support | 250 | +25% hit rate |
| Move+1 | Movement | 200 | +1 movement range |

### Thief (4 abilities + passives)
| Ability | Type | JP | Effect |
|---------|------|-----|--------|
| Steal Gil | Action | 50 | Steal money from target |
| Steal Item | Action | 200 | Steal held item |
| Mug | Action | 400 | Attack + Steal in one action |
| Vanish | Action | 500 | Become invisible for 2 turns |
| Evade | Reaction | 250 | Chance to fully dodge any attack |
| Speed Boost | Support | 300 | +25% Speed |
| Move+2 | Movement | 500 | +2 movement range |

### Monk (4 abilities + passives)
| Ability | Type | JP | Effect |
|---------|------|-----|--------|
| Chakra | Action | 100 | Heal self + adjacent allies |
| Earth Slash | Action | 200 | Line AoE physical damage |
| Revive | Action | 400 | Restore fallen ally with 30% HP |
| Aura Blast | Action | 600 | Large AoE physical + ignore defense |
| Counter | Reaction | 100 | Physical counter at full damage |
| Martial Arts | Support | 300 | Unarmed damage equals weapon damage |
| Ignore Height | Movement | 400 | No Jump restriction |

### Red Mage (4 abilities + passives)
| Ability | Type | JP | Effect |
|---------|------|-----|--------|
| Fire/Cure hybrid | Action | 100 | Damage enemy OR heal ally (same ability, smart targeting) |
| Enfeeble | Action | 200 | Reduce target MA for 3 turns |
| Dualcast | Action | 800 | Cast two spells in one turn (ultimate) |
| Dispel | Action | 300 | Remove positive buffs from enemy |
| Absorb MP | Reaction | 250 | Recover MP when hit by magic |
| Equip Swords | Support | 200 | Can equip swords in any class |
| Teleport | Movement | 500 | Teleport to any tile in move range (ignore obstacles) |

---

## Balance Safeguards

### 1. No "One Best Class"
Every class has a unique mechanic no other class replicates:
- Knight: Break abilities
- Thief: Steal
- Monk: Revive (only non-White-Mage revival)
- Red Mage: Dualcast
- Archer: Snipe (extreme range)

### 2. Advanced Classes as Specialists
Tier 2-3 classes are NOT upgrades — they're narrower and more powerful in their niche:
- Ninja: highest speed + dual wield but lowest HP/defense
- Sage: all magic schools but worst physical stats
- Samurai: strongest AoE physical but slow

### 3. JP Costs Prevent Rushing
Even with JP Boost, learning a job's full kit takes 3-5 battles. Advanced job prerequisites require real time investment in lower jobs.

### 4. Equipment Tradeoffs
Support slot "Equip X" abilities consume the same slot as Dual Wield, Attack Boost, Speed Boost — you can't have everything.

---

## Implementation Notes for System 9

- All job data as `JobData` ScriptableObjects
- All abilities as `AbilityData` ScriptableObjects
- `JobSystem` class: manages unlock checks, JP, ability learning
- `ComputedStats` recalculation when job changes (apply multipliers to character base)
- Equipment validation: check current job's allowed equipment types
- Party management UI: job tree visualization, ability equip screen, equipment screen
