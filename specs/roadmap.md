# IsoRPG — System Roadmap

Each system is a self-contained speckit spec → implement → review → PR → merge cycle.

---

## System 1: Core Framework
**Branch**: `system/core-framework`
**Spec**: `specs/01-core-framework/`

Foundation that all other systems depend on.

- `ICommand` interface with `Execute()` / `Undo()`
- `CommandHistory` (stack-based, supports rewind N steps)
- `StateMachine<T>` with proper state transitions (states receive machine reference)
- `GameEvent<T>` lightweight event bus
- `IsoMath` (grid↔world, sorting order, Manhattan distance)
- `GameEnums` (TerrainType, Direction, JobId, etc.)
- GUID-based entity IDs
- Assembly Definitions (Core, Map, Units, Battle, UI, Data)
- EditMode tests for all pure logic

**Exit Criteria**: All core utilities tested, no MonoBehaviour dependencies.

---

## System 2: Map & Grid
**Branch**: `system/map-grid`
**Spec**: `specs/02-map-grid/`

Isometric map rendering with elevation, camera, and tile picking.

- `TileData` struct, `BattleMapData` ScriptableObject
- `IsometricGrid` MonoBehaviour — loads map, instantiates tile views
- `TileView` MonoBehaviour — sprite, sorting order, highlight state
- Safe out-of-bounds handling (sentinel tile, `TryGetTile` pattern)
- `BattleCameraController` — pan, zoom, 4-way rotation with coordinate transform
- Mouse-to-grid picking with elevation awareness
- `MapGenerator` for test maps
- Generate initial terrain tiles via PixelLab
- EditMode tests for IsoMath round-trips, bounds checking

**Exit Criteria**: Rendered 8x8 map with elevation, camera controls, tile hover highlight.

---

## System 3: Units & CT Turn System
**Branch**: `system/units-ct`
**Spec**: `specs/03-units-ct/`

Unit data model and FFT's Charge Time turn order.

- `UnitInstance` — pure C# data with GUID ID, stats, CT, events
- `UnitView` MonoBehaviour — sprite rendering, subscribes to `UnitInstance` events
- `ComputedStats` — calculated from base + job + equipment
- `CTSystem` — tick loop with infinite-loop guard, turn resolution, preview
- `MoveCommand : ICommand` — move unit along path, undoable
- `WaitCommand : ICommand` — end turn with CT cost
- Unit spawn on map at spawn zone positions
- Turn order preview (next 10 turns)
- Events: `OnTurnStarted(UnitInstance)`, `OnUnitMoved(UnitInstance, path)`
- EditMode tests for CT, commands (execute + undo), turn order preview

**Exit Criteria**: Units on map, CT-driven turns cycling, movement via Command with undo working.

---

## System 4: Pathfinding & Movement
**Branch**: `system/pathfinding`
**Spec**: `specs/04-pathfinding/`

A* movement with terrain, elevation, and collision.

- `Pathfinder` — A* with terrain cost, jump height, enemy blocking
- Fix ally pass-through (traversable but not stoppable, no broken path reconstruction)
- Movement range overlay (BFS flood fill → highlight reachable tiles)
- `MoveCommand` integration — animate unit along path, update grid position
- Movement animation (tween along path waypoints)
- Facing direction update after movement
- Events: `OnMovementStarted`, `OnMovementCompleted`
- EditMode tests for pathfinding edge cases (water, cliffs, enemies, allies)

**Exit Criteria**: Click unit → see movement range → click tile → unit walks there → undo returns them.

---

## System 5: Battle State Machine
**Branch**: `system/battle-states`
**Spec**: `specs/05-battle-states/`

Full battle flow from deployment to victory/defeat.

- `BattleStateMachine` with proper transition mechanism
- States: `DeploymentState`, `CTAdvanceState`, `SelectUnitState`, `SelectActionState`, `MoveTargetState`, `ActionTargetState`, `PerformActionState`, `EndTurnState`, `AITurnState`, `VictoryState`, `DefeatState`
- Player input handling in relevant states (click to select, click to move/target)
- AI stub (random valid action for now)
- Victory/defeat condition checks (all enemies dead / all allies dead)
- `BattleManager` MonoBehaviour — bootstraps context, runs state machine
- Events: `OnBattleStarted`, `OnBattleEnded(result)`
- Integration test: full battle loop player vs dummy AI

**Exit Criteria**: Complete player-controlled battle loop with state transitions, win/lose.

---

## System 6: Combat & Abilities
**Branch**: `system/combat-abilities`
**Spec**: `specs/06-combat-abilities/`

Damage formulas, abilities, targeting, and status effects.

- `AbilityData` ScriptableObject — range, AoE shape, damage type, cost, targeting
- `AttackCommand : ICommand` — resolve damage, undoable (restore HP on undo)
- `DamageCalculator` — physical/magic formulas, hit rate, elevation bonus, Brave/Faith
- `LineOfSight` — elevation-based LoS blocking
- `ActionTargetState` — show ability range, select target, preview damage
- `PerformActionState` — play animation, show damage numbers, apply effects
- Basic abilities: Attack, Fire, Cure, Potion (4 total for MVP)
- Status effects: Poison, Haste, Protect (3 for MVP)
- Status tick on turn end
- Unit death and removal
- Events: `OnAbilityUsed`, `OnDamageDealt`, `OnUnitDied`, `OnStatusApplied`
- EditMode tests for DamageCalculator, LineOfSight, status tick, command undo

**Exit Criteria**: Units attack each other with damage, abilities have range/AoE, undo reverses damage.

---

## System 7: AI Controller
**Branch**: `system/ai-controller`
**Spec**: `specs/07-ai-controller/`

Utility-based AI that evaluates all options and picks the best.

- `AIController` — evaluate all (move, action) combos using utility scoring
- `AIProfile` ScriptableObject — tunable weights (damage, kill, heal, survival, position)
- 4 presets: Aggressive, Defensive, Support, Coward
- AI uses Command pattern (same `MoveCommand`, `AttackCommand` as player)
- AI hypothetical evaluation: execute commands, score, undo, pick best
- `AITurnState` integration — AI picks and executes best option
- Performance guard: limit evaluation to top-N candidates if too many combos
- EditMode tests for scoring, profile weights

**Exit Criteria**: AI enemies make reasonable tactical decisions, use abilities, avoid suicide.

---

## System 8: UI Layer
**Branch**: `system/ui-layer`
**Spec**: `specs/08-ui-layer/`

All battle HUD elements, subscribing to game events.

- `TurnOrderBarUI` — show next 8-10 turns, update on CT changes
- `ActionMenuUI` — Move / Act / Wait / Undo, ability sub-menu
- `UnitInfoPanelUI` — HP/MP bars, stats on hover/select
- `DamageNumberUI` — floating popups, animated
- `AbilityPreviewUI` — show predicted damage/hit% before confirming
- All UI subscribes to events — zero polling
- Events consumed: `OnTurnStarted`, `OnUnitDamaged`, `OnAbilityUsed`, etc.

**Exit Criteria**: Full HUD for tactical battle, all elements update reactively.

---

## System 9: Job System & Progression
**Branch**: `system/job-system`
**Spec**: `specs/09-job-system/`

FFT-style job/class system with ability learning.

- `JobData` ScriptableObject — stat modifiers, equipment slots, abilities, JP costs
- 6 jobs: Squire, Knight, Black Mage, White Mage, Archer, Thief
- 4 abilities per job (24 total)
- JP earning on ability use
- Ability learning (spend JP)
- Job switching with unlock requirements
- Ability equip slots: primary, secondary, reaction, support, movement
- Stat growth on level-up (varies by current job)
- `ComputedStats` recalculation on job/equip change
- Equipment system — weapons, armor, accessories
- Party management scene
- EditMode tests for JP, unlocks, stat computation

**Exit Criteria**: Characters can change jobs, learn abilities, equip cross-class skills.

---

## System 10: Undo/Rewind (CHARIOT)
**Branch**: `system/rewind`
**Spec**: `specs/10-rewind/`

Tactics Ogre-style rewind system.

- `BattleSnapshot` — serializable snapshot of full battle state
- Snapshot captured before each command execution
- `CommandHistory` extended with state snapshots + RNG seed capture
- Rewind UI — scroll through past N actions, select rewind point
- RNG determinism: same action at same point = same result
- Rewind limit: 50 actions (configurable)
- Integration with existing Command system (should "just work" if commands are correct)

**Exit Criteria**: Player can rewind any number of turns, RNG is deterministic on replay.

---

## System 11: Save/Load & Polish
**Branch**: `system/save-load`
**Spec**: `specs/11-save-load/`

Persistence, content, and ship readiness.

- Save/load game state to `Application.persistentDataPath` (JSON)
- Battle select / world map screen
- 5 battle maps authored with varying terrain
- Asset generation pass (all character sprites, terrain tiles, VFX)
- Import pipeline: PixelLab → Unity Sprites
- Sound effects and music
- Story dialogue (simple text between battles)
- Tutorial battle
- Addressables setup for asset loading
- Build for Windows, playtest, polish

**Exit Criteria**: Shippable MVP — complete single-player experience with save/load.

---

## Branch & PR Workflow

```
main (protected)
  └── system/core-framework     → PR #1 → merge
  └── system/map-grid           → PR #2 → merge
  └── system/units-ct           → PR #3 → merge
  └── system/pathfinding        → PR #4 → merge
  └── system/battle-states      → PR #5 → merge
  └── system/combat-abilities   → PR #6 → merge
  └── system/ai-controller      → PR #7 → merge
  └── system/ui-layer           → PR #8 → merge
  └── system/job-system         → PR #9 → merge
  └── system/rewind             → PR #10 → merge
  └── system/save-load          → PR #11 → merge
```

Each PR includes:
- Speckit spec in `specs/<NN>-<system>/`
- Implementation code
- EditMode unit tests
- Updated `specs/roadmap.md` (check off completed system)
