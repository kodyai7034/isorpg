# IsoRPG — Task Breakdown

## Phase 1: Foundation

- [ ] 1.1 Create Unity 6 project with URP 2D renderer
- [ ] 1.2 Set up folder structure per spec (Scripts/, Data/, Sprites/, etc.)
- [ ] 1.3 Create Assembly Definitions (Core, Battle, Units, Map, UI, Data)
- [ ] 1.4 Implement `IsoMath` utility — `GridToWorld()`, `WorldToGrid()`, elevation offset
- [ ] 1.5 Implement `TileData` struct and `BattleMapData` ScriptableObject
- [ ] 1.6 Author test map (8x8, 3 elevation levels, mixed terrain)
- [ ] 1.7 Implement `IsometricGrid` — load map data, instantiate tile sprites
- [ ] 1.8 Implement sprite sorting — `sortingOrder = elevation * maxDepth + depth`
- [ ] 1.9 Implement `BattleCameraController` — pan (WASD/drag), zoom (scroll), rotation (Q/E)
- [ ] 1.10 Implement mouse-to-grid picking — highlight hovered tile
- [ ] 1.11 Place a unit sprite on the grid at a specific tile position
- [ ] 1.12 Generate initial terrain tiles via PixelLab (`create_isometric_tile`)

## Phase 2: Movement & Turns

- [ ] 2.1 Implement `UnitInstance` data model (stats, position, CT, facing)
- [ ] 2.2 Implement `UnitView` MonoBehaviour (sprite rendering, animation hooks)
- [ ] 2.3 Implement `CTSystem` — tick all units, determine active turn, tie-break
- [ ] 2.4 Write unit tests for `CTSystem`
- [ ] 2.5 Implement `Pathfinder` — A* with terrain cost, elevation/jump, collision
- [ ] 2.6 Write unit tests for `Pathfinder`
- [ ] 2.7 Implement movement range overlay (BFS flood fill, colored tile highlights)
- [ ] 2.8 Implement `BattleStateMachine` with `IBattleState` interface
- [ ] 2.9 Implement states: `CTAdvanceState`, `SelectActionState`, `MoveTargetState`, `EndTurnState`
- [ ] 2.10 Implement unit movement animation (tween along path tiles)
- [ ] 2.11 Implement `TurnOrderBarUI` — show next 8-10 units in turn order
- [ ] 2.12 Deploy 4 units (2 player, 2 enemy) and verify turn cycling
- [ ] 2.13 Generate initial character sprites via PixelLab (`create_character`)

## Phase 3: Combat

- [ ] 3.1 Implement `AbilityData` ScriptableObject (range, AoE, damage, cost, targeting)
- [ ] 3.2 Create basic abilities: Attack, Fire, Cure, Potion
- [ ] 3.3 Implement `DamageCalculator` — physical/magic formulas, hit rate
- [ ] 3.4 Write unit tests for `DamageCalculator`
- [ ] 3.5 Implement `ActionTargetState` — show ability range, select target
- [ ] 3.6 Implement `PerformActionState` — play animation, apply damage, show numbers
- [ ] 3.7 Implement `LineOfSight` — elevation-based LoS check
- [ ] 3.8 Write unit tests for `LineOfSight`
- [ ] 3.9 Implement `ActionMenuUI` — Move / Act / Wait, ability sub-menu
- [ ] 3.10 Implement `UnitInfoPanelUI` — HP/MP bars, stats on hover/select
- [ ] 3.11 Implement `DamageNumberUI` — floating damage/heal popups
- [ ] 3.12 Implement unit death — play death animation, remove from battle
- [ ] 3.13 Implement basic status effects: Poison, Haste, Protect
- [ ] 3.14 Implement `StatusEffects` tick on turn end
- [ ] 3.15 Implement `AIController` — utility scoring, evaluate all move+action combos
- [ ] 3.16 Create `AIProfile` ScriptableObject with Aggressive preset
- [ ] 3.17 Implement `VictoryState` / `DefeatState` with results screen
- [ ] 3.18 Full integration test: complete player vs AI battle

## Phase 4: Job System & Progression

- [ ] 4.1 Implement `JobData` ScriptableObject (stat mods, equip slots, abilities, JP costs)
- [ ] 4.2 Create Squire job definition + 4 abilities
- [ ] 4.3 Create Knight job definition + 4 abilities
- [ ] 4.4 Create Black Mage job definition + 4 abilities
- [ ] 4.5 Create White Mage job definition + 4 abilities
- [ ] 4.6 Create Archer job definition + 4 abilities
- [ ] 4.7 Create Thief job definition + 4 abilities
- [ ] 4.8 Implement JP earning — grant JP on ability use in battle
- [ ] 4.9 Implement ability learning — spend JP, add to `LearnedAbilities`
- [ ] 4.10 Implement job switching with unlock requirements
- [ ] 4.11 Implement ability equip slots (secondary, reaction, support, movement)
- [ ] 4.12 Implement reaction abilities (e.g., Counter — auto-attack on hit)
- [ ] 4.13 Implement support abilities (e.g., Equip Heavy Armor)
- [ ] 4.14 Implement movement abilities (e.g., Move+1, Jump+1)
- [ ] 4.15 Implement equipment system — weapons, armor, accessories
- [ ] 4.16 Implement Brave/Faith stats and their effect on formulas
- [ ] 4.17 Implement stat growth on level-up (varies by current job)
- [ ] 4.18 Build Party Management scene — job change UI
- [ ] 4.19 Build ability equip UI
- [ ] 4.20 Build equipment UI
- [ ] 4.21 Balance pass — stat curves, JP costs, ability power

## Phase 5: Content & Polish

- [ ] 5.1 Author Battle Map 1: Plains (tutorial, flat terrain)
- [ ] 5.2 Author Battle Map 2: Castle walls (elevation focus)
- [ ] 5.3 Author Battle Map 3: Forest (difficult terrain, cover)
- [ ] 5.4 Author Battle Map 4: Volcano (hazard tiles, narrow paths)
- [ ] 5.5 Author Battle Map 5: Fortress (complex multi-level)
- [ ] 5.6 Generate all character sprites (6 job visuals x 4 directions x animations)
- [ ] 5.7 Generate all terrain tile sets (grass, stone, water, sand, lava, forest)
- [ ] 5.8 Generate spell/ability VFX sprites
- [ ] 5.9 Import and configure all generated assets in Unity
- [ ] 5.10 Implement save/load system (JSON serialization)
- [ ] 5.11 Build battle select / world map screen
- [ ] 5.12 Implement story dialogue system (text boxes between battles)
- [ ] 5.13 Add sound effects (attack, spell, hit, UI clicks)
- [ ] 5.14 Add background music (menu, battle, victory, defeat)
- [ ] 5.15 Screen transitions and loading screens
- [ ] 5.16 Tutorial battle with guided prompts
- [ ] 5.17 Create additional AI profiles (Defensive, Support, Coward)
- [ ] 5.18 Full playtest and balance tuning
- [ ] 5.19 Bug fixing and polish pass
- [ ] 5.20 Build for Windows and test
