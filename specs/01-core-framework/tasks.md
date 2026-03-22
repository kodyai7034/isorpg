# System 1: Core Framework — Tasks

## Enums, Constants & Identity
- [ ] 1.1 Rewrite `GameEnums.cs` with XML docs on every value
- [ ] 1.2 Create `GameConstants.cs` — centralize all magic numbers
- [ ] 1.3 Create `EntityId.cs` — GUID wrapper, `None` sentinel, `IEquatable`, `ToString`
- [ ] 1.4 Write tests: `EntityIdTests.cs`

## Isometric Math
- [ ] 1.5 Rewrite `IsoMath.cs` — full XML docs, add `GetDirection()`, `RotateGrid()`
- [ ] 1.6 Write tests: `IsoMathTests.cs` (expand existing — round-trips, direction, rotation)

## Event System
- [ ] 1.7 Create `GameEvent.cs` — `GameEvent<T>` and parameterless `GameEvent`
- [ ] 1.8 Create `GameEventArgs.cs` — readonly struct args for all events
- [ ] 1.9 Create `GameEvents.cs` — static registry of all event channels
- [ ] 1.10 Write tests: `GameEventTests.cs`

## Command Pattern
- [ ] 1.11 Create `ICommand.cs` — interface with `Execute()`, `Undo()`, `Description`
- [ ] 1.12 Create `ICommandMeta.cs` — optional metadata (ActorId, RngSeedBefore)
- [ ] 1.13 Create `CommandHistory.cs` — stack with max capacity, eviction, events
- [ ] 1.14 Write tests: `CommandHistoryTests.cs`

## State Machine
- [ ] 1.15 Rewrite `StateMachine.cs` — `IState<T>` receives `IStateMachine<T>`, transition guard
- [ ] 1.16 Write tests: `StateMachineTests.cs`

## Deterministic RNG
- [ ] 1.17 Create `IGameRng.cs` — interface
- [ ] 1.18 Create `GameRng.cs` — `System.Random` wrapper with seed capture
- [ ] 1.19 Write tests: `GameRngTests.cs`

## Cleanup & Verification
- [ ] 1.20 Update `IsoRPG.Core.asmdef` — verify zero external dependencies
- [ ] 1.21 Remove any MonoBehaviour usage from Core scripts
- [ ] 1.22 Verify all public APIs have XML doc comments
- [ ] 1.23 Run all EditMode tests — zero failures, zero warnings
- [ ] 1.24 Commit and create PR `system/core-framework` → `main`
