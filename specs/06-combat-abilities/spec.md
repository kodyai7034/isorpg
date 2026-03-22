# System 6: Combat & Abilities — Specification

## Overview

This system adds the ability to attack, cast spells, take damage, and die. It introduces AbilityData (ScriptableObject), DamageCalculator (pure logic), AttackCommand (ICommand), LineOfSight, status effects, and integrates the "Act" option into the battle state machine.

---

## 1. AbilityData (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "NewAbility", menuName = "IsoRPG/Ability")]
public class AbilityData : ScriptableObject
{
    public string AbilityName;
    public string Description;
    public AbilitySlotType SlotType;   // Action, Reaction, Support, Movement
    public AbilityTargetType Targeting; // Single, Self, Area, Line

    // Range & AoE
    public int Range;              // 0 = self only, 1+ = tiles
    public int AoERadius;          // 0 = single target, 1+ = area radius
    public bool RequiresLineOfSight;

    // Damage
    public DamageType DamageType;
    public int Power;              // base power for damage formula
    public int Accuracy;           // base hit chance (0-100)
    public bool IsHealing;         // if true, Power heals instead of damages

    // Cost
    public int MPCost;

    // Status effect (optional)
    public StatusType? AppliedStatus;
    public int StatusDuration;     // turns
    public int StatusChance;       // % chance to apply (0-100)

    // Animation
    public string AnimationKey;    // for future VFX lookup
}

public enum AbilitySlotType
{
    Action,
    Reaction,
    Support,
    Movement
}
```

---

## 2. DamageCalculator (Pure Static)

All formulas from the core architecture spec, fully testable:

```csharp
public static class DamageCalculator
{
    /// <summary>Calculate raw damage before defense.</summary>
    public static int CalculateRawDamage(AbilityData ability, ComputedStats attacker,
        int resolve, int attunement);

    /// <summary>Apply defense reduction.</summary>
    public static int ApplyDefense(int rawDamage, DamageType type, ComputedStats defender);

    /// <summary>Calculate final damage (raw - defense, clamped to 1 minimum).</summary>
    public static int CalculateFinalDamage(AbilityData ability, ComputedStats attacker,
        ComputedStats defender, int attackerResolve, int attackerAttunement,
        int defenderAttunement, int heightAdvantage);

    /// <summary>Calculate hit chance (clamped to MinHitChance-MaxHitChance).</summary>
    public static int CalculateHitChance(AbilityData ability, ComputedStats attacker,
        ComputedStats defender, int heightAdvantage);

    /// <summary>Calculate healing amount.</summary>
    public static int CalculateHealing(AbilityData ability, ComputedStats caster,
        int casterAttunement);
}
```

### Formulas

**Physical Damage:**
```
rawDamage = attacker.PhysicalAttack * ability.Power / 10
modifier = resolve / 100
finalDamage = max(1, floor(rawDamage * modifier) - defender.Defense + heightAdvantage * 5)
```

**Magical Damage:**
```
rawDamage = attacker.MagicAttack * ability.Power / 10
modifier = (attackerAttunement / 100) * (defenderAttunement / 100)
finalDamage = max(1, floor(rawDamage * modifier) - defender.MagicDefense + heightAdvantage * 5)
```

**Hit Chance:**
```
hitChance = ability.Accuracy + attacker.Speed - defender.Speed + heightAdvantage * 5
clamped to [MinHitChance, MaxHitChance]
```

**Healing:**
```
healAmount = caster.MagicAttack * ability.Power / 10 * (casterAttunement / 100)
```

---

## 3. LineOfSight

```csharp
public static class LineOfSight
{
    /// <summary>
    /// Check if a unit at 'from' can see 'to' on the given map.
    /// Uses Bresenham line between grid positions. LoS is blocked if any
    /// intermediate tile's elevation exceeds both the source and target elevation.
    /// </summary>
    public static bool HasLineOfSight(BattleMapData map, Vector2Int from, Vector2Int to);

    /// <summary>
    /// Get all tiles within ability range that have line of sight from the caster.
    /// </summary>
    public static List<Vector2Int> GetTargetableTiles(BattleMapData map,
        Vector2Int casterPos, int range, bool requiresLoS);
}
```

---

## 4. AttackCommand

```csharp
public class AttackCommand : ICommand, ICommandMeta
{
    // Captures: target HP before, status before, RNG seed
    // Execute: roll hit, calculate damage, apply damage, apply status, fire events
    // Undo: restore target HP, remove applied status, restore RNG seed
}
```

Key design:
- Captures ALL mutable state before executing (target HP, target status list snapshot)
- Hit/miss determined by RNG during Execute — deterministic on replay
- Fires: `GameEvents.AbilityUsed`, `GameEvents.DamageDealt` or `GameEvents.HealingDealt`, `GameEvents.UnitDied`, `GameEvents.StatusApplied`
- On Undo: restores HP, removes status, but does NOT fire "reverse" events (undo is invisible to observers per System 3 review decision)

---

## 5. Status Effects

### 5.1 StatusEffectInstance

```csharp
public class StatusEffectInstance
{
    public EntityId Id { get; }
    public StatusType Type { get; }
    public int RemainingDuration { get; set; }

    /// <summary>Apply per-turn tick effect (poison damage, regen heal).</summary>
    public void Tick(UnitInstance unit);

    /// <summary>Whether this effect has expired.</summary>
    public bool IsExpired => RemainingDuration <= 0;
}
```

### 5.2 Status Tick Behavior

| Status | Tick Effect | Duration |
|--------|-----------|----------|
| Poison | 10% MaxHP damage at turn start | 3 turns |
| Regen | 10% MaxHP heal at turn start | 3 turns |
| Haste | +50% Speed (applied to ComputedStats) | 3 turns |
| Slow | -50% Speed (applied to ComputedStats) | 3 turns |
| Protect | -33% physical damage taken | 3 turns |
| Shell | -33% magical damage taken | 3 turns |

### 5.3 Status on UnitInstance

```csharp
// On UnitInstance:
public List<StatusEffectInstance> StatusEffects { get; }
public void AddStatus(StatusEffectInstance status);
public void RemoveStatus(EntityId statusId);
public bool HasStatus(StatusType type);
public void TickStatuses();  // called at turn start
public void RemoveExpiredStatuses();
```

---

## 6. Battle State Integration

### 6.1 SelectActionState Update

Add "Act" option (keyboard `A`):
- If not acted → transition to `SelectAbilityState`

### 6.2 New States

**SelectAbilityState**: Show available abilities list (from current job primary action set). Player picks one. Transitions to `ActionTargetState`.

**ActionTargetState**: Show ability range overlay (red tiles). Player clicks target. Transitions to `PerformActionState`.

**PerformActionState**: Execute AttackCommand via CommandHistory. Show damage number. Wait briefly. Transitions back to `SelectActionState`.

---

## 7. MVP Abilities (4 total)

| Ability | Type | Range | Power | MP | Targeting | Notes |
|---------|------|-------|-------|-----|-----------|-------|
| Attack | Physical | 1 | 10 | 0 | Single | Basic melee |
| Fire | Magical | 4 | 12 | 8 | Single | Ranged fire damage |
| Cure | Magical | 4 | 10 | 6 | Single | Healing spell |
| Poison Strike | Physical | 1 | 8 | 4 | Single | Damage + Poison status |

---

## 8. Test Coverage

| Class | Tests |
|-------|-------|
| `DamageCalculator` | Physical/magical damage, defense reduction, height advantage, min damage 1, hit chance clamping, healing |
| `LineOfSight` | Clear LoS, blocked by elevation, adjacent always visible, same-tile |
| `AttackCommand` | Execute deals damage, Undo restores HP, miss deals no damage, status application, events fire |
| `StatusEffectInstance` | Tick reduces duration, poison deals damage, regen heals, expiry |
