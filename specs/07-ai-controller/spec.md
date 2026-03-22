# System 7: AI Controller — Specification

## Overview

Utility-based AI that evaluates all possible (move, action) combinations for an enemy unit, scores each option, and picks the best. Uses the same Command pattern as the player — AI produces MoveCommands and AttackCommands executed through CommandHistory.

The AI's "thinking" is invisible to game state: it uses hypothetical execute/undo to evaluate options without permanent side effects.

---

## 1. AIProfile (ScriptableObject)

Tunable weights that define an AI personality:

```csharp
[CreateAssetMenu(fileName = "NewAIProfile", menuName = "IsoRPG/AIProfile")]
public class AIProfile : ScriptableObject
{
    public string ProfileName;

    [Header("Offensive")]
    public float DamageWeight = 3f;      // points per damage dealt
    public float KillBonus = 50f;        // bonus if target would die
    public float TargetThreatWeight = 2f; // prioritize high-threat units

    [Header("Defensive")]
    public float SelfPreservationWeight = 5f; // penalty per damage risked
    public float HealthThresholdForCaution = 0.3f; // below this HP%, increase caution

    [Header("Support")]
    public float HealingWeight = 2f;     // points per HP healed
    public float HealAllyThreshold = 0.5f; // only heal allies below this HP%

    [Header("Position")]
    public float HighGroundBonus = 10f;  // prefer elevated tiles
    public float DistanceFromEnemyPenalty = 1f; // penalty per tile away from enemies (aggressive) or bonus (defensive)
}
```

### Presets

| Profile | Behavior |
|---------|----------|
| **Aggressive** | High DamageWeight, KillBonus. Low SelfPreservation. Negative DistanceFromEnemyPenalty (wants to close distance). |
| **Defensive** | High SelfPreservation, HealingWeight. Positive DistanceFromEnemyPenalty (stays back). |
| **Support** | High HealingWeight, low HealAllyThreshold. Moderate SelfPreservation. |
| **Coward** | Very high SelfPreservation. Only attacks when safe. Positive DistanceFromEnemyPenalty. |

---

## 2. AIOption

```csharp
public struct AIOption
{
    public Vector2Int MoveTo;
    public AbilityData Ability;     // null = no action (wait)
    public UnitInstance Target;     // null if no action or self-target
    public float Score;
    public int EstimatedDamage;
    public bool WouldKill;
}
```

---

## 3. AIController

```csharp
public class AIController
{
    /// <summary>
    /// Evaluate all options for the given unit and return the best one.
    /// Uses hypothetical command execution (execute → score → undo) to
    /// evaluate damage without permanent side effects.
    /// </summary>
    public AIOption EvaluateBestOption(
        UnitInstance unit, BattleContext ctx, AIProfile profile, AbilityData[] abilities);
}
```

### 3.1 Evaluation Flow

1. Get reachable tiles via `Pathfinder.GetReachableTiles`
2. For each reachable tile:
   a. Score the position (elevation, distance from enemies/allies)
   b. For each ability the unit has:
      - Get targetable enemies/allies from that position
      - For each valid target:
        - Create hypothetical AttackCommand
        - Execute it (captures RNG seed first)
        - Score the result (damage dealt, kill, healing)
        - Undo immediately
        - Record as AIOption with composite score
   c. Also score "wait at this tile" (no action)
3. Sort all options by score
4. Return the best option
5. Performance guard: if total combos > 500, prune low-value positions first

### 3.2 Scoring Formula

```
score = 0
score += estimatedDamage * profile.DamageWeight
score += wouldKill ? profile.KillBonus : 0
score += targetThreat * profile.TargetThreatWeight  // threat = target's PA+MA
score -= riskToSelf * profile.SelfPreservationWeight // risk = enemies that can reach this tile
score += heightAdvantage * profile.HighGroundBonus
score -= distanceToNearestEnemy * profile.DistanceFromEnemyPenalty
score += healingDone * profile.HealingWeight  // if healing ally below threshold
```

### 3.3 Threat Assessment

A unit's threat level = `PhysicalAttack + MagicAttack`. High-threat units are prioritized as targets.

### 3.4 Risk Assessment

For a candidate tile, risk = number of enemy units that could reach and attack this tile on their next turn. Computed via reverse pathfinding or simple range check.

---

## 4. AITurnState (Rewrite)

Replace the stub with full AI:

```csharp
public class AITurnState : IState<BattleContext>
{
    private enum Phase { Thinking, Moving, Acting, Done }
    private Phase _phase;
    private AIOption _chosen;
    private float _timer;

    public void Enter(...)
    {
        _chosen = aiController.EvaluateBestOption(unit, ctx, profile, abilities);
        _phase = Phase.Moving;
    }

    public void Execute(...)
    {
        // Phase.Moving: execute MoveCommand, animate, wait
        // Phase.Acting: execute AttackCommand, wait
        // Phase.Done: transition to EndTurnState
    }
}
```

Brief delays between phases so the player can see what the AI did.

---

## 5. Default Enemy Setup

Until the Job System (System 9), enemies use a hardcoded ability list:
- All enemies know "Attack" (melee, range 1)
- Assigned an AIProfile ScriptableObject (default: Aggressive)

---

## 6. Test Coverage

| Class | Tests |
|-------|-------|
| `AIController` | EvaluateBestOption picks highest-damage target, avoids suicide, prefers kill, heals wounded ally, profile weights change behavior |
| `AIOption` scoring | DamageWeight scales correctly, KillBonus applied, SelfPreservation reduces risky moves |
| `AIProfile` | Default weights produce reasonable scores |
