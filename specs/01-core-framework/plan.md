# System 1: Core Framework — Implementation Plan

## Approach

Build bottom-up: utilities with zero dependencies first, then abstractions that compose them. Every file is tested before moving to the next.

---

## Step 1: Enums, Constants, and EntityId

Create the foundational types that everything else references.

- `GameEnums.cs` — all enums (replace existing file)
- `GameConstants.cs` — all magic numbers centralized
- `EntityId.cs` — GUID wrapper with `None` sentinel, equality, serialization

**Test**: EntityId uniqueness, None validity, equality operators.

## Step 2: IsoMath

Rewrite `IsoMath.cs` with full XML docs, add `GetDirection` and `RotateGrid`.

**Test**: Round-trip conversions, sorting order invariants, direction calculation, rotation.

## Step 3: Event System

- `GameEvent.cs` — generic `GameEvent<T>` and parameterless `GameEvent`
- `GameEvents.cs` — static registry of all event channels
- `GameEventArgs.cs` — readonly struct args for each event

**Test**: Subscribe/raise/unsubscribe lifecycle, no-listener raise, double-subscribe safety.

## Step 4: Command Pattern

- `ICommand.cs` — interface with Execute/Undo/Description
- `ICommandMeta.cs` — optional metadata interface
- `CommandHistory.cs` — stack with max history, eviction, events

**Test**: Execute/undo symmetry, max eviction, UndoMultiple, events fire correctly.

## Step 5: State Machine

Rewrite `StateMachine.cs` — states receive `IStateMachine<T>` for transitions, transition guard against recursion.

**Test**: State transitions, Enter/Exit callbacks, transition-from-within-Enter queuing.

## Step 6: Deterministic RNG

- `IGameRng.cs` — interface
- `GameRng.cs` — `System.Random` wrapper with seed capture

**Test**: Determinism (same seed = same sequence), range bounds, Check edge cases.

## Step 7: Assembly Definition Cleanup

Update `IsoRPG.Core.asmdef` — verify no external dependencies. Remove any MonoBehaviour usage from Core scripts.

## Step 8: Integration Verification

- All tests pass in Unity Test Runner (EditMode)
- No compiler warnings
- All public APIs have XML doc comments
