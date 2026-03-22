# System 1: Core Framework — Specification

## Overview

The core framework provides the foundational abstractions that every other system depends on. Nothing in this module has gameplay semantics — it is pure infrastructure: commands, events, state machines, math utilities, and identity.

This system must be rock-solid. Every bug here cascades into every other system.

---

## 1. Command Pattern (`ICommand`)

### 1.1 Interface

```csharp
/// <summary>
/// Represents a reversible game action.
/// All mutations to battle state flow through commands.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Execute the action, mutating game state.
    /// Must capture all state needed for Undo before mutating.
    /// </summary>
    void Execute();

    /// <summary>
    /// Reverse the action, restoring game state to pre-Execute.
    /// Calling Undo after Execute must produce identical state to before Execute.
    /// </summary>
    void Undo();

    /// <summary>
    /// Human-readable description for UI/logging (e.g., "Ramza moves to (3,4)").
    /// </summary>
    string Description { get; }
}
```

### 1.2 Design Rules

- Commands capture **pre-mutation state** in their constructor or at the start of `Execute()`, before any changes.
- `Execute()` followed by `Undo()` must leave the world **byte-identical** to before `Execute()`.
- Commands are **immutable after construction** — all parameters set in constructor, no public setters.
- Commands do **not** own view/animation logic. They mutate data; views react via events.
- Commands must be **serializable** (for replay/network). All fields should be primitive types, enums, or GUIDs — no object references.

### 1.3 Command Metadata

```csharp
/// <summary>
/// Optional metadata for commands that affect the rewind timeline.
/// </summary>
public interface ICommandMeta
{
    /// <summary>Unique ID of the unit that performed this action.</summary>
    System.Guid ActorId { get; }

    /// <summary>The RNG seed state before this command executed.</summary>
    int RngSeedBefore { get; }
}
```

---

## 2. Command History

### 2.1 Interface

```csharp
/// <summary>
/// Stack-based history of executed commands. Supports undo and replay.
/// </summary>
public interface ICommandHistory
{
    /// <summary>Execute a command and push it onto the history stack.</summary>
    void ExecuteCommand(ICommand command);

    /// <summary>Undo the most recent command.</summary>
    /// <returns>The undone command, or null if history is empty.</returns>
    ICommand Undo();

    /// <summary>Undo the last N commands.</summary>
    void UndoMultiple(int count);

    /// <summary>All commands in execution order (oldest first).</summary>
    IReadOnlyList<ICommand> History { get; }

    /// <summary>Number of commands that can be undone.</summary>
    int Count { get; }

    /// <summary>Maximum commands retained. Oldest are discarded when exceeded.</summary>
    int MaxHistory { get; }

    /// <summary>Remove all history.</summary>
    void Clear();

    /// <summary>Fired after any command is executed.</summary>
    event System.Action<ICommand> OnCommandExecuted;

    /// <summary>Fired after any command is undone.</summary>
    event System.Action<ICommand> OnCommandUndone;
}
```

### 2.2 Implementation: `CommandHistory`

- Backed by a `List<ICommand>` with a configurable max size (default 50).
- When max is exceeded, the oldest command is removed (FIFO eviction).
- `ExecuteCommand` calls `command.Execute()` then appends to history.
- `Undo` calls `command.Undo()` then removes from history.
- Thread-safe is **not** required (single-threaded game loop).
- Fires events after execute/undo for UI and logging subscribers.

---

## 3. Event System (`GameEvent<T>`)

### 3.1 Design

A lightweight, type-safe event channel. Each event type is a static singleton channel that any system can raise or subscribe to.

```csharp
/// <summary>
/// Type-safe event channel. Raise from any system, subscribe from any system.
/// No allocation on raise if no listeners.
/// </summary>
public class GameEvent<T>
{
    private event System.Action<T> _listeners;

    public void Subscribe(System.Action<T> listener);
    public void Unsubscribe(System.Action<T> listener);
    public void Raise(T args);
}

/// <summary>
/// Parameterless event channel for simple signals.
/// </summary>
public class GameEvent
{
    private event System.Action _listeners;

    public void Subscribe(System.Action listener);
    public void Unsubscribe(System.Action listener);
    public void Raise();
}
```

### 3.2 Event Registry (`GameEvents`)

A static class holding all game event channels as public static fields:

```csharp
public static class GameEvents
{
    // Battle flow
    public static readonly GameEvent<BattleStartedArgs> BattleStarted = new();
    public static readonly GameEvent<BattleEndedArgs> BattleEnded = new();
    public static readonly GameEvent<TurnStartedArgs> TurnStarted = new();
    public static readonly GameEvent<TurnEndedArgs> TurnEnded = new();

    // Unit actions
    public static readonly GameEvent<UnitMovedArgs> UnitMoved = new();
    public static readonly GameEvent<DamageDealtArgs> DamageDealt = new();
    public static readonly GameEvent<UnitDiedArgs> UnitDied = new();
    public static readonly GameEvent<AbilityUsedArgs> AbilityUsed = new();
    public static readonly GameEvent<StatusAppliedArgs> StatusApplied = new();

    // Commands
    public static readonly GameEvent<ICommand> CommandExecuted = new();
    public static readonly GameEvent<ICommand> CommandUndone = new();
}
```

### 3.3 Event Args

Each event arg is a readonly struct to avoid GC allocation:

```csharp
public readonly struct TurnStartedArgs
{
    public readonly System.Guid UnitId;
    public readonly int TurnNumber;
    // constructor...
}

public readonly struct DamageDealtArgs
{
    public readonly System.Guid AttackerId;
    public readonly System.Guid TargetId;
    public readonly int Amount;
    public readonly DamageType Type;
    public readonly bool IsCritical;
    // constructor...
}
```

### 3.4 Design Rules

- Event args are **readonly structs** — no heap allocation, no mutation.
- Listeners must **not** throw exceptions. `Raise` should catch and log, not propagate.
- Listeners must **not** modify game state in response to events. Events are for observation (UI, audio, VFX), not control flow.
- Unsubscribe in `OnDestroy` / `OnDisable` to prevent dangling references.

---

## 4. State Machine

### 4.1 Interface

```csharp
/// <summary>
/// A state in a finite state machine. Receives the machine reference
/// for triggering transitions from within states.
/// </summary>
public interface IState<T>
{
    void Enter(T context, IStateMachine<T> machine);
    void Execute(T context, IStateMachine<T> machine);
    void Exit(T context);
}

/// <summary>
/// State machine that states can use to trigger transitions.
/// </summary>
public interface IStateMachine<T>
{
    IState<T> CurrentState { get; }
    void ChangeState(IState<T> newState);
}
```

### 4.2 Implementation: `StateMachine<T>`

```csharp
public class StateMachine<T> : IStateMachine<T>
{
    public IState<T> CurrentState { get; private set; }
    private readonly T _context;
    private bool _isTransitioning;

    public StateMachine(T context);
    public void ChangeState(IState<T> newState);
    public void Update();
}
```

- **Transition guard**: `ChangeState` sets a flag to prevent recursive transitions. If `ChangeState` is called during `Enter`, the transition is queued and applied after the current `Enter` completes.
- States receive `IStateMachine<T>` (not the concrete class) to call `ChangeState` from within `Execute` or `Enter`.
- `Update()` calls `CurrentState.Execute(context, this)`.

---

## 5. Isometric Math (`IsoMath`)

### 5.1 Constants

```csharp
public const float TileWidth = 1f;
public const float TileHeight = 0.5f;    // 2:1 ratio
public const float TileWidthHalf = 0.5f;
public const float TileHeightHalf = 0.25f;
public const float ElevationHeight = 0.25f;
```

### 5.2 Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `GridToWorld` | `(Vector2Int grid, int elevation) → Vector3` | Convert grid coords + elevation to world position |
| `WorldToGrid` | `(Vector3 world, int elevation) → Vector2Int` | Convert world position to grid coords (requires known elevation) |
| `CalculateSortingOrder` | `(int gridX, int gridY, int elevation, int maxDepth) → int` | Composite z-sort key |
| `ManhattanDistance` | `(Vector2Int a, Vector2Int b) → int` | Grid distance for range calculations |
| `GetDirection` | `(Vector2Int from, Vector2Int to) → Direction` | Facing direction between two tiles |
| `RotateGrid` | `(Vector2Int grid, int rotationIndex, int mapSize) → Vector2Int` | Transform grid coords for camera rotation |

### 5.3 Design Rules

- All methods are `static` and `pure` — no side effects, no state.
- All methods have XML doc comments with examples.
- Edge cases (same tile, out-of-bounds) documented and tested.

---

## 6. Entity Identity

### 6.1 GUID-Based IDs

All game entities (units, abilities, status effect instances) use `System.Guid` for identity:

```csharp
public readonly struct EntityId : System.IEquatable<EntityId>
{
    public readonly System.Guid Value;

    public EntityId(System.Guid value);
    public static EntityId New() => new(System.Guid.NewGuid());
    public static readonly EntityId None = new(System.Guid.Empty);

    public bool IsValid => Value != System.Guid.Empty;

    // IEquatable, GetHashCode, ToString, operators
}
```

### 6.2 Design Rules

- No static counters. IDs survive serialization, deserialization, and scene reloads.
- `EntityId.None` is the null sentinel — never use `default(EntityId)`.
- All dictionaries keyed by entity use `EntityId`, not `int` or `string`.

---

## 7. Deterministic RNG

### 7.1 Interface

```csharp
/// <summary>
/// Seedable RNG for deterministic replay and rewind.
/// </summary>
public interface IGameRng
{
    /// <summary>Current seed state (capture for rewind snapshots).</summary>
    int Seed { get; }

    /// <summary>Set seed for deterministic replay.</summary>
    void SetSeed(int seed);

    /// <summary>Random int in [min, max) range.</summary>
    int Range(int min, int max);

    /// <summary>Random float in [0, 1) range.</summary>
    float Value();

    /// <summary>Hit check: returns true if random roll < chance (0-100).</summary>
    bool Check(int chancePercent);
}
```

### 7.2 Implementation

Wraps `System.Random` with seed capture. The seed is saved/restored by the rewind system to ensure deterministic replay — same seed + same action = same result.

---

## 8. Enums & Constants

### 8.1 Game Enums

```csharp
public enum TerrainType { Grass, Stone, Water, Sand, Lava, Forest }
public enum HazardType { None, Poison, Fire, Heal }
public enum Direction { South, SouthWest, West, NorthWest, North, NorthEast, East, SouthEast }
public enum JobId { Squire, Knight, BlackMage, WhiteMage, Archer, Thief }
public enum AbilityTargetType { Single, Self, Area, Line }
public enum DamageType { Physical, Magical, Pure }
public enum StatusType { Poison, Haste, Slow, Protect, Shell, Regen }
public enum BattleResult { Victory, Defeat, Retreat }
```

### 8.2 Constants

```csharp
public static class GameConstants
{
    public const int MaxCommandHistory = 50;
    public const int CTThreshold = 100;
    public const int MaxCTAfterAction = 60;
    public const int MaxElevation = 15;
    public const int MaxBraveOrFaith = 100;
    public const int MinBraveOrFaith = 0;
    public const int CTTickSafetyLimit = 10000;
}
```

---

## 9. Assembly Definition

`IsoRPG.Core.asmdef`:
- **No dependencies** on any other assembly.
- **No MonoBehaviour** references — `noEngineReferences: false` only because we need `UnityEngine.Vector2Int`, `Vector3`, `Mathf`. No `MonoBehaviour`, `Component`, or `GameObject`.
- All other assemblies depend on Core. Core depends on nothing.

---

## 10. Test Coverage

Every public method and interface in this system must have EditMode tests:

| Class | Tests Required |
|-------|---------------|
| `CommandHistory` | Execute, Undo, UndoMultiple, max history eviction, Clear, events fire |
| `GameEvent<T>` | Subscribe, Unsubscribe, Raise with/without listeners, raise after unsubscribe |
| `StateMachine<T>` | ChangeState transitions, Enter/Exit called, Execute called each Update, transition from within Enter |
| `IsoMath` | GridToWorld/WorldToGrid round-trips, sorting order comparisons, ManhattanDistance, GetDirection, RotateGrid |
| `EntityId` | New generates unique, None is invalid, equality, serialization round-trip |
| `GameRng` | Same seed produces same sequence, different seeds diverge, Range bounds, Check edge cases (0%, 100%) |
