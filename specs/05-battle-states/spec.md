# System 5: Battle State Machine — Specification

## Overview

The battle state machine orchestrates the full battle flow: deployment → CT advance → unit turns → victory/defeat. It connects all prior systems (grid, units, pathfinding, commands) into a playable battle loop where the player selects actions and AI takes turns.

---

## 1. State Flow

```
BattleManager.Start()
    → DeploymentState
        → CTAdvanceState (loop)
            → [if player unit] PlayerTurnState
                → SelectActionState
                    → [Move] MoveTargetState → PerformMoveState → SelectActionState
                    → [Act]  (stub — System 6)
                    → [Wait] EndTurnState
                    → [Undo] Undo last command, return to SelectActionState
                → EndTurnState
            → [if enemy unit] AITurnState (stub — System 7)
                → EndTurnState
            → [check win/lose] VictoryState | DefeatState
```

---

## 2. BattleContext (Expanded)

```csharp
public class BattleContext
{
    // Map & Grid
    public BattleMapData Map { get; set; }
    public IsometricGrid Grid { get; set; }

    // Units
    public UnitRegistry Registry { get; set; }
    public List<UnitInstance> AllUnits { get; set; }

    // Turn state
    public UnitInstance ActiveUnit { get; set; }
    public int TurnNumber { get; set; }
    public bool ActiveUnitMoved { get; set; }
    public bool ActiveUnitActed { get; set; }

    // Systems
    public CommandHistory CommandHistory { get; set; }
    public MovementController MovementController { get; set; }
    public IGameRng Rng { get; set; }

    // Unit views (for animation)
    public Dictionary<EntityId, UnitView> UnitViews { get; set; }
}
```

---

## 3. States

### 3.1 DeploymentState

- Places player and enemy units at spawn zone positions
- Instantiates UnitView GameObjects for each unit
- Registers all units in UnitRegistry
- Centers camera on the map
- Transitions → CTAdvanceState

### 3.2 CTAdvanceState

- Calls `CTSystem.AdvanceTick` to find the next active unit
- Increments TurnNumber
- Fires `GameEvents.TurnStarted`
- Resets `ActiveUnitMoved` / `ActiveUnitActed` flags
- If player unit → transition to `PlayerTurnState`
- If enemy unit → transition to `AITurnState`

### 3.3 PlayerTurnState

- Focuses camera on active unit
- Transitions immediately to `SelectActionState`

### 3.4 SelectActionState

- Displays action menu: **Move** (if not moved), **Act** (if not acted, stub), **Wait**, **Undo** (if commands exist this turn)
- Listens for player input:
  - Move selected → transition to `MoveTargetState`
  - Wait selected → transition to `EndTurnState`
  - Undo selected → undo last command, reset moved/acted flags, stay in SelectActionState

### 3.5 MoveTargetState

- Shows movement range overlay via `MovementController.ShowMovementRange`
- Shows path preview on tile hover via `MovementController.PreviewPathTo`
- On tile click:
  - If valid destination → create MoveCommand, transition to `PerformMoveState`
  - If invalid (right-click or escape) → cancel, clear overlays, return to `SelectActionState`

### 3.6 PerformMoveState

- Executes MoveCommand via CommandHistory
- Triggers UnitView movement animation
- Waits for animation to complete
- Sets `ActiveUnitMoved = true`
- Clears movement overlays
- Transitions → `SelectActionState` (unit can still Act or Wait)

### 3.7 AITurnState (Stub)

- For now: enemy waits (creates WaitCommand)
- Full AI implementation in System 7
- Transitions → `EndTurnState`

### 3.8 EndTurnState

- Resolves CT cost via `CTSystem.ResolveTurn(unit, moved, acted)`
- Fires `GameEvents.TurnEnded`
- Checks win/lose conditions:
  - All enemies dead → `VictoryState`
  - All player units dead → `DefeatState`
  - Otherwise → `CTAdvanceState`

### 3.9 VictoryState / DefeatState

- Fires `GameEvents.BattleEnded`
- Displays result (placeholder UI — full UI in System 8)
- Battle loop stops (no more state transitions)

---

## 4. Input Handling

### 4.1 Design

Input is handled ONLY by states that need it. Each state checks `Input` in its `Execute` method. No global input manager.

### 4.2 Key Bindings (MVP)

| Input | Context | Action |
|-------|---------|--------|
| Left click | MoveTargetState | Select destination tile |
| Right click / Escape | MoveTargetState | Cancel movement selection |
| Left click on menu | SelectActionState | Choose Move/Act/Wait/Undo |
| 1 / 2 / 3 / 4 | SelectActionState | Hotkeys for menu options |
| U | SelectActionState | Undo shortcut |

### 4.3 Temporary Input (Before UI System)

Until System 8 builds proper UI, use keyboard shortcuts:
- `M` = Move
- `W` = Wait
- `U` = Undo
- `Escape` = Cancel

---

## 5. Animation Coordination

### 5.1 Waiting for Animation

`PerformMoveState` needs to wait for `UnitView.AnimateMovement` (a coroutine) to complete before transitioning. Options:

```csharp
// PerformMoveState tracks animation status
private bool _animationComplete;

public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
{
    ctx.CommandHistory.ExecuteCommand(_moveCommand);

    var unitView = ctx.UnitViews[ctx.ActiveUnit.Id];
    unitView.AnimateMovement(_moveCommand.Path.ToList(), ctx.Map, 4f);

    // Start polling for animation completion
    _animationComplete = false;
    // Use a callback or coroutine wrapper
}

public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
{
    if (_animationComplete)
        machine.ChangeState(new SelectActionState());
}
```

### 5.2 Animation Completion Callback

Add to UnitView:
```csharp
public event Action OnAnimationComplete;
```

PerformMoveState subscribes to this event to know when to transition.

---

## 6. BattleManager (Rewrite)

```csharp
public class BattleManager : MonoBehaviour
{
    [SerializeField] private IsometricGrid grid;
    [SerializeField] private BattleMapData mapOverride;
    [SerializeField] private GameObject unitPrefab;

    private BattleContext _context;
    private StateMachine<BattleContext> _stateMachine;

    // Bootstraps everything and starts the battle
    private void Start();

    // Calls _stateMachine.Update() each frame
    private void Update();

    // Spawns units at spawn zones, creates UnitViews, registers in UnitRegistry
    private void SpawnUnits(BattleMapData map);
}
```

---

## 7. Test Coverage

| Class | Tests |
|-------|-------|
| `CTAdvanceState` | Correct unit selected, transitions to PlayerTurn or AITurn based on team |
| `EndTurnState` | CT resolved correctly, victory/defeat detection |
| `BattleContext` | Registry lookups, unit state queries |

Note: Most states require MonoBehaviour (input, animation), so they get PlayMode tests. EditMode tests focus on the pure logic paths (CT, win/lose checks).
