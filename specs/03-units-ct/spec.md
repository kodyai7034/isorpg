# System 3: Units & CT Turn System — Specification

## Overview

The unit system models all character state — stats, position, CT, job, abilities — as pure C# data. The CT system drives FFT-style interleaved turn order. MoveCommand and WaitCommand are the first concrete `ICommand` implementations, establishing the pattern all future actions follow.

---

## 1. UnitInstance (Rewrite)

### 1.1 Requirements

- GUID-based identity via `EntityId`
- Events for state changes (HP, position, death)
- Computed stats derived from base + job + equipment (placeholder until System 9)
- No MonoBehaviour dependency — pure C# class
- Serializable state for save/load

### 1.2 Data Model

```csharp
public class UnitInstance
{
    // Identity
    public EntityId Id { get; }
    public string Name { get; }
    public int Team { get; }

    // Position & facing
    public Vector2Int GridPosition { get; private set; }
    public Direction Facing { get; private set; }

    // Level & stats
    public int Level { get; private set; }
    public ComputedStats Stats { get; private set; }
    public int Brave { get; private set; }
    public int Faith { get; private set; }

    // HP/MP (mutable via commands only)
    public int CurrentHP { get; private set; }
    public int CurrentMP { get; private set; }
    public bool IsAlive => CurrentHP > 0;

    // CT
    public int CT { get; set; }  // set by CTSystem directly

    // Job (placeholder until System 9)
    public JobId CurrentJob { get; private set; }

    // Events
    public event Action<int, int> OnHPChanged;        // (oldHP, newHP)
    public event Action<Vector2Int, Vector2Int> OnPositionChanged;  // (from, to)
    public event Action OnDied;
}
```

### 1.3 Mutation Methods

All state changes go through explicit methods that fire events:

```csharp
/// <summary>Set position and fire OnPositionChanged. Used by MoveCommand.</summary>
public void SetPosition(Vector2Int newPos);

/// <summary>Set facing direction.</summary>
public void SetFacing(Direction dir);

/// <summary>Apply damage. Fires OnHPChanged and OnDied if HP reaches 0.</summary>
public void ApplyDamage(int amount);

/// <summary>Apply healing. Fires OnHPChanged. Clamps to MaxHP.</summary>
public void ApplyHealing(int amount);

/// <summary>Directly set HP (for undo). Fires OnHPChanged.</summary>
public void SetHP(int hp);

/// <summary>Directly set MP (for undo).</summary>
public void SetMP(int mp);
```

### 1.4 Design Rules

- Properties with `private set` — external code cannot mutate directly
- All mutations fire events for view/UI subscribers
- `CT` has public set because CTSystem needs direct access (it's a tight loop)
- No static counters — EntityId.New() for identity

---

## 2. UnitView (MonoBehaviour)

### 2.1 Responsibilities

- Subscribes to `UnitInstance` events (OnPositionChanged, OnHPChanged, OnDied)
- Updates sprite position, facing sprite, and animations
- Manages sprite sorting order (based on grid position + elevation)
- Provides reference back to UnitInstance for mouse picking

### 2.2 Interface

```csharp
public class UnitView : MonoBehaviour
{
    public UnitInstance Unit { get; private set; }

    /// <summary>Bind this view to a unit instance. Subscribes to events.</summary>
    public void Initialize(UnitInstance unit, BattleMapData map);

    /// <summary>Animate movement along a path (coroutine).</summary>
    public Coroutine AnimateMovement(List<Vector2Int> path, BattleMapData map, float speed);
}
```

---

## 3. UnitRegistry

Centralized lookup for units by ID, position, or team:

```csharp
public class UnitRegistry
{
    /// <summary>Register a unit. Called at battle start.</summary>
    public void Register(UnitInstance unit);

    /// <summary>Get unit by EntityId. Returns null if not found.</summary>
    public UnitInstance GetById(EntityId id);

    /// <summary>Get unit at grid position. Returns null if unoccupied.</summary>
    public UnitInstance GetAtPosition(Vector2Int pos);

    /// <summary>Get all living units on a team.</summary>
    public IReadOnlyList<UnitInstance> GetTeam(int team);

    /// <summary>Get all living units.</summary>
    public IReadOnlyList<UnitInstance> AllLiving { get; }

    /// <summary>Total unit count (including dead).</summary>
    public int Count { get; }
}
```

Uses internal dictionaries keyed by EntityId and position. Updates position index when units move (subscribes to OnPositionChanged).

---

## 4. CTSystem (Hardened)

### 4.1 Changes from System 1

- Use `GameConstants` for all thresholds
- Safety guard with `CTTickSafetyLimit`
- Fire `GameEvents.TurnStarted` when a unit gains a turn
- Fire `GameEvents.TurnEnded` when a turn resolves
- Turn counter tracking

### 4.2 CT Cost Constants (from GameConstants)

| Action | CT Cost |
|--------|---------|
| Move + Act | 100 |
| Move only | 80 |
| Act only | 80 |
| Wait (neither) | 60 |
| Max CT after action | 60 |

---

## 5. MoveCommand

First concrete `ICommand` implementation.

```csharp
public class MoveCommand : ICommand, ICommandMeta
{
    // Constructor params (immutable)
    private readonly UnitInstance _unit;
    private readonly Vector2Int _destination;
    private readonly List<Vector2Int> _path;

    // Captured pre-state for undo
    private Vector2Int _previousPosition;
    private Direction _previousFacing;

    public string Description => $"{_unit.Name} moves to ({_destination.x},{_destination.y})";
    public EntityId ActorId => _unit.Id;
    public int RngSeedBefore { get; }

    public void Execute()
    {
        _previousPosition = _unit.GridPosition;
        _previousFacing = _unit.Facing;

        _unit.SetPosition(_destination);
        _unit.SetFacing(IsoMath.GetDirection(_previousPosition, _destination));

        GameEvents.UnitMoved.Raise(new UnitMovedArgs(
            _unit.Id, _previousPosition, _destination, _path.ToArray()));
    }

    public void Undo()
    {
        _unit.SetPosition(_previousPosition);
        _unit.SetFacing(_previousFacing);
    }
}
```

---

## 6. WaitCommand

```csharp
public class WaitCommand : ICommand, ICommandMeta
{
    private readonly UnitInstance _unit;

    public string Description => $"{_unit.Name} waits";
    public EntityId ActorId => _unit.Id;
    public int RngSeedBefore { get; }

    public void Execute()
    {
        // Wait does nothing to game state — CT cost applied by battle state machine
    }

    public void Undo()
    {
        // Nothing to undo
    }
}
```

---

## 7. Events Fired

| Event | When |
|-------|------|
| `GameEvents.TurnStarted` | Unit's CT reaches 100, turn begins |
| `GameEvents.TurnEnded` | Unit's turn resolves (after move/act/wait) |
| `GameEvents.UnitMoved` | MoveCommand.Execute() |
| `GameEvents.CommandExecuted` | Via CommandHistory |
| `UnitInstance.OnPositionChanged` | SetPosition called |
| `UnitInstance.OnHPChanged` | ApplyDamage/ApplyHealing/SetHP called |
| `UnitInstance.OnDied` | HP reaches 0 |

---

## 8. Test Coverage

| Class | Tests |
|-------|-------|
| `UnitInstance` | Construction, SetPosition fires event, ApplyDamage/Heal, death event, HP clamping |
| `UnitRegistry` | Register, GetById, GetAtPosition, GetTeam, position update on move |
| `MoveCommand` | Execute changes position, Undo restores, facing updates, event fires |
| `WaitCommand` | Execute/Undo are no-ops, Description correct |
| `CTSystem` | Already tested in System 1 (expanded if needed) |
