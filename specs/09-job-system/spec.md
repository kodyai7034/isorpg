# System 9: Job System & Progression — Specification

## Overview

FFT-inspired 5-slot ability system with 8 MVP jobs, JP earning/spending, job unlock tree, equipment, and stat recalculation. See `class-system-design.md` for full design rationale.

Key decisions: character-intrinsic stat growth (no Ninja exploit), classes apply temporary multipliers, JP spillover, Resolve/Attunement replacing Brave/Faith.

---

## 1. JobData (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "NewJob", menuName = "IsoRPG/Job")]
public class JobData : ScriptableObject
{
    public JobId Id;
    public string JobName;
    public Sprite Icon;

    [Header("Stat Multipliers (percentage, 100 = no change)")]
    public int HPMultiplier = 100;
    public int MPMultiplier = 100;
    public int PAMultiplier = 100;
    public int MAMultiplier = 100;
    public int SpeedMultiplier = 100;

    [Header("Equipment")]
    public WeaponType[] AllowedWeapons;
    public ArmorType[] AllowedArmor;

    [Header("Abilities")]
    public JobAbilityEntry[] Abilities;  // all learnable abilities + JP costs

    [Header("Unlock Requirements")]
    public JobRequirement[] UnlockRequirements;  // e.g., Squire Lv 2
}
```

---

## 2. Job Ability Entry

```csharp
[System.Serializable]
public struct JobAbilityEntry
{
    public AbilityData Ability;
    public int JPCost;           // 50-800
    public int RequiredJobLevel; // minimum job level to learn (0 = available immediately)
}
```

---

## 3. Job Requirement

```csharp
[System.Serializable]
public struct JobRequirement
{
    public JobId Job;
    public int RequiredLevel;
}
```

---

## 4. Equipment Types

```csharp
public enum WeaponType { Sword, Bow, Rod, Staff, Fists, Lance }
public enum ArmorType { Heavy, Light, Robes }

[CreateAssetMenu(fileName = "NewEquipment", menuName = "IsoRPG/Equipment")]
public class EquipmentData : ScriptableObject
{
    public string ItemName;
    public EquipmentSlot Slot;     // Weapon, Armor, Accessory
    public WeaponType WeaponType;  // only if Slot == Weapon
    public ArmorType ArmorType;    // only if Slot == Armor
    public int AttackBonus;
    public int DefenseBonus;
    public int MagicAttackBonus;
    public int MagicDefenseBonus;
    public int SpeedBonus;
    public int HPBonus;
}

public enum EquipmentSlot { Weapon, Armor, Accessory }
```

---

## 5. JobSystem (Pure C# Logic)

```csharp
public class JobSystem
{
    /// <summary>Check if a unit meets unlock requirements for a job.</summary>
    public bool CanUnlockJob(UnitInstance unit, JobData job);

    /// <summary>Change a unit's job. Recalculates ComputedStats.</summary>
    public void ChangeJob(UnitInstance unit, JobData job);

    /// <summary>Award JP to a unit for their current job.</summary>
    public void AwardJP(UnitInstance unit, int amount);

    /// <summary>Award spillover JP to allies in matching jobs.</summary>
    public void AwardSpilloverJP(UnitInstance actor, List<UnitInstance> allies, int baseJP);

    /// <summary>Learn an ability if the unit has enough JP.</summary>
    public bool LearnAbility(UnitInstance unit, JobData job, int abilityIndex);

    /// <summary>Recalculate ComputedStats from base stats + job multipliers + equipment.</summary>
    public ComputedStats CalculateStats(UnitInstance unit, JobData currentJob,
        EquipmentData weapon, EquipmentData armor, EquipmentData accessory);
}
```

---

## 6. Unit Ability Slots

Expand UnitInstance with equipped ability tracking:

```csharp
// On UnitInstance (new fields):
public JobId? SecondaryAbilitySet { get; set; }     // action set from another job
public AbilityData EquippedReaction { get; set; }
public AbilityData EquippedSupport { get; set; }
public AbilityData EquippedMovement { get; set; }

// Equipment
public EquipmentData EquippedWeapon { get; set; }
public EquipmentData EquippedArmor { get; set; }
public EquipmentData EquippedAccessory { get; set; }
```

---

## 7. JP System

### Earning
- Base JP per action: `10 + (JobLevel * 2)`
- Kill bonus: +20 JP
- Spillover: allies on battlefield with same active job earn 25%

### Spending
- Each ability has a JP cost (50-800)
- Once learned, ability is permanently available from any job

### Job Level
- Job level increases when total JP earned in that job reaches thresholds
- Thresholds: Lv1=0, Lv2=100, Lv3=300, Lv4=600, Lv5=1000

---

## 8. Stat Recalculation

```
ComputedStat = floor(BaseCharacterStat * (JobMultiplier / 100)) + EquipmentBonus
```

- BaseCharacterStat: grows on level-up based on CHARACTER growth rates (not job)
- JobMultiplier: from current JobData (e.g., Knight HP = 120%)
- EquipmentBonus: flat adds from weapon + armor + accessory

---

## 9. MVP Jobs (8)

Per class-system-design.md. Each job ScriptableObject created with 7 abilities.

---

## 10. Test Coverage

| Class | Tests |
|-------|-------|
| `JobSystem` | CanUnlockJob with met/unmet requirements, ChangeJob recalculates stats, AwardJP increments correctly, spillover at 25%, LearnAbility deducts JP, job level thresholds |
| `ComputedStats` | Calculation with multipliers, equipment bonuses, edge cases |
