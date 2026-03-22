# System 4: Pathfinding & Movement — Specification

## Overview

This system connects pathfinding to the battle state machine and visual layer. The Pathfinder logic exists from System 2, but it has no UI overlay, no movement animation integration, and no connection to the Command system's undo flow. This system makes movement actually playable.

---

## 1. Movement Range Overlay

### 1.1 Flow

When a player selects "Move" during their turn:
1. Calculate reachable tiles via `Pathfinder.GetReachableTiles`
2. Call `IsometricGrid.SetTileOverlay(result.StoppableTiles.Keys, TileVisualState.MoveRange)`
3. Player clicks a highlighted tile
4. Calculate path via `Pathfinder.ReconstructPath`
5. Create and execute `MoveCommand` via `CommandHistory`
6. Animate movement via `UnitView.AnimateMovement`
7. Clear overlay via `IsometricGrid.ClearAllOverlays`

### 1.2 Path Preview

While hovering over a reachable tile, show the path the unit would take:
- Highlight path tiles with a distinct color (brighter blue trail)
- Show movement cost at the destination tile
- Update in real-time as mouse moves

---

## 2. MovementController

Coordinates between pathfinding logic, the grid overlay, and the animation layer.

```csharp
public class MovementController
{
    /// <summary>
    /// Calculate and display movement range for a unit.
    /// Returns the pathfinding result for subsequent path queries.
    /// </summary>
    public PathfindingResult ShowMovementRange(
        UnitInstance unit, BattleMapData map, List<UnitInstance> allUnits, IsometricGrid grid);

    /// <summary>
    /// Preview the path to a specific tile (for hover display).
    /// Returns null if tile is not reachable.
    /// </summary>
    public List<Vector2Int> PreviewPathTo(
        PathfindingResult result, Vector2Int start, Vector2Int target);

    /// <summary>
    /// Create a MoveCommand for the given destination.
    /// Does not execute — caller decides when to execute via CommandHistory.
    /// </summary>
    public MoveCommand CreateMoveCommand(
        UnitInstance unit, Vector2Int destination, PathfindingResult result, IGameRng rng);

    /// <summary>
    /// Clear all movement overlays from the grid.
    /// </summary>
    public void ClearOverlays(IsometricGrid grid);
}
```

### 2.1 Design Rules

- `MovementController` is a pure C# class — no MonoBehaviour
- It does NOT execute commands — it creates them. The battle state machine executes via CommandHistory.
- It does NOT animate — it provides the path. The battle state machine triggers animation on UnitView.
- Separation: logic (MovementController) → decision (state machine) → presentation (UnitView)

---

## 3. Path Highlight

### 3.1 TileVisualState Addition

Add a new visual state for path preview:

```csharp
PathPreview  // bright blue trail showing the path the unit will take
```

### 3.2 Path Display

```csharp
/// <summary>
/// Highlight tiles along a path (brighter than MoveRange).
/// </summary>
public void ShowPathPreview(IsometricGrid grid, List<Vector2Int> path);

/// <summary>
/// Clear only the path preview, keeping MoveRange overlay intact.
/// </summary>
public void ClearPathPreview(IsometricGrid grid, IEnumerable<Vector2Int> previousPath);
```

When clearing the path preview, restore tiles to `MoveRange` state (not Default), since the range overlay should persist while selecting a destination.

---

## 4. Movement Animation Integration

### 4.1 Animation Flow

After `MoveCommand.Execute()` updates the data model:
1. The battle state machine retrieves the path from the command
2. Calls `UnitView.AnimateMovement(path, map, speed)`
3. Waits for the coroutine to complete
4. Transitions to the next battle state

### 4.2 Animation During Undo

When undoing a move:
1. `MoveCommand.Undo()` restores position instantly (data layer)
2. `UnitView` receives `OnPositionChanged` event and snaps to the old position
3. No reverse animation — undo is instant (matches Tactics Ogre CHARIOT behavior)

---

## 5. Pathfinder Improvements

### 5.1 Movement Type Support

Prepare for future movement abilities (Move+1, Teleport, Ignore Height):

```csharp
public struct MovementParams
{
    public int MoveRange;       // base from stats, can be modified by abilities
    public int JumpHeight;      // base from stats, can be modified by abilities
    public bool IgnoreHeight;   // Geomancer/Monk movement ability
    public bool CanFly;         // future: flying units ignore terrain cost
    public bool CanTeleport;    // future: teleport ignores obstacles
}
```

`GetReachableTiles` takes `MovementParams` instead of reading directly from `UnitInstance.Stats`. This allows the caller to apply movement ability modifiers before pathfinding.

### 5.2 Performance: Occupied Position HashSet

Replace LINQ `FirstOrDefault` on every neighbor with a pre-built HashSet:

```csharp
// Build once before pathfinding
var enemyPositions = new HashSet<Vector2Int>(
    allUnits.Where(u => u.IsAlive && u.Team != unit.Team)
            .Select(u => u.GridPosition));
var allyPositions = new HashSet<Vector2Int>(
    allUnits.Where(u => u.IsAlive && u.Team == unit.Team && u != unit)
            .Select(u => u.GridPosition));
```

O(1) collision checks instead of O(n) per neighbor.

---

## 6. Events

| Event | When |
|-------|------|
| `GameEvents.UnitMoved` | MoveCommand.Execute() (already implemented) |
| `UnitInstance.OnPositionChanged` | SetPosition called (already implemented) |
| `IsometricGrid.OnTileClicked` | Player clicks destination tile |

No new global events needed — this system wires existing events together.

---

## 7. Test Coverage

| Class | Tests |
|-------|-------|
| `MovementController` | ShowMovementRange returns correct result, CreateMoveCommand produces valid command, PreviewPathTo returns path or null |
| `Pathfinder` (expanded) | MovementParams with IgnoreHeight, performance with large maps, ally pass-through path reconstruction |
| `MoveCommand` | Already tested in System 3 |
