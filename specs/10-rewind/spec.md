# System 10: Undo/Rewind (CHARIOT) — Specification

## Overview

Tactics Ogre-style rewind: player can undo any number of past actions (up to 50). RNG state is captured per command so replaying the same action produces identical results. The CommandHistory and ICommand infrastructure from System 1 does the heavy lifting — this system adds the RNG determinism guarantee and the rewind UI.

---

## 1. How It Works

The system is already 90% built:
- `CommandHistory` stores up to 50 commands with Execute/Undo
- Every `ICommand` captures `RngSeedBefore` via `ICommandMeta`
- `GameRng` (xorshift32) has fully extractable/restorable state
- `MoveCommand.Undo` and `AttackCommand.Undo` restore all state

What's missing:
- Multi-turn rewind (currently undo is scoped to current turn)
- RNG seed restoration on undo
- Rewind UI showing action history
- CT state restoration on multi-turn undo

---

## 2. BattleSnapshot

```csharp
public class BattleSnapshot
{
    public int TurnNumber;
    public int RngSeed;
    public Dictionary<EntityId, UnitSnapshot> Units;
}

public struct UnitSnapshot
{
    public EntityId Id;
    public Vector2Int Position;
    public Direction Facing;
    public int CurrentHP, CurrentMP;
    public int CT;
    public List<StatusSnapshot> Statuses;
}
```

Snapshots are taken BEFORE each command executes. On rewind, the snapshot is restored and all subsequent commands are discarded.

---

## 3. RewindSystem

```csharp
public class RewindSystem
{
    /// <summary>Take a snapshot of the current battle state.</summary>
    public BattleSnapshot CaptureSnapshot(BattleContext ctx);

    /// <summary>Restore a snapshot, resetting all unit state.</summary>
    public void RestoreSnapshot(BattleContext ctx, BattleSnapshot snapshot);

    /// <summary>Rewind N commands, restoring state and RNG.</summary>
    public void RewindCommands(BattleContext ctx, int count);
}
```

---

## 4. Integration

- `CommandHistory.ExecuteCommand` now also captures a snapshot before each command
- Undo restores the snapshot instead of relying solely on command Undo (belt-and-suspenders)
- The battle state machine returns to CTAdvanceState after multi-turn rewind
- Rewind is available from SelectActionState (extends the existing Undo functionality)

---

## 5. RNG Determinism

On undo: `_rng.SetSeed(command.RngSeedBefore)` restores the RNG to its pre-command state. If the player replays the same action, the same random sequence produces the same hit/miss/damage results. Different actions produce different results (because the RNG is consumed differently).

---

## 6. Test Coverage

| Class | Tests |
|-------|-------|
| `RewindSystem` | CaptureSnapshot preserves all state, RestoreSnapshot restores exactly, multi-command rewind, RNG determinism after rewind |
