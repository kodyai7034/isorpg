# IsoRPG — Implementation Plan

## Phased Approach

Development is split into 5 phases, each producing a playable milestone.

---

## Phase 1: Foundation (Weeks 1-2)

**Goal**: Unity project scaffolding, isometric grid rendering, and a unit that can be placed on the map.

1. Create Unity 6 project with URP 2D
2. Set up Assembly Definitions for module boundaries
3. Implement `IsometricGrid` — data model + grid-to-world math
4. Create tile prefabs and render a simple 8x8 map with elevation
5. Implement sprite sorting (z-order by elevation + depth)
6. Add `BattleCameraController` — pan, zoom, 4-way rotation
7. Place a single unit sprite on the grid with correct sorting
8. Create `BattleMapData` ScriptableObject and author a test map
9. Mouse picking — click tile to highlight it

**Deliverable**: A rendered isometric map with elevation, camera controls, and tile selection.

---

## Phase 2: Movement & Turns (Weeks 3-4)

**Goal**: Units move on the grid with pathfinding. CT system drives turn order.

1. Implement `UnitInstance` data model (stats, position, CT)
2. Implement `CTSystem` — tick loop, turn resolution, tie-breaking
3. Implement `Pathfinder` — A* with terrain cost, elevation/jump constraints
4. Implement `BattleStateMachine` with states: CTAdvance, SelectAction, MoveTarget, EndTurn
5. Movement range overlay (BFS flood fill, highlight reachable tiles)
6. Unit movement animation (tween along path)
7. Turn order UI bar showing upcoming unit order
8. Deploy 3-4 units and cycle through turns
9. Unit tests for CTSystem and Pathfinder

**Deliverable**: Multiple units taking turns, moving around the map with pathfinding.

---

## Phase 3: Combat (Weeks 5-7)

**Goal**: Units can attack, cast spells, and die. Basic AI opponents.

1. Implement `AbilityData` ScriptableObject — range, AoE shape, damage type, cost
2. Implement `DamageCalculator` — physical/magic formulas, hit rate, elevation bonus
3. Implement `ActionTarget` state — ability range overlay, target selection
4. Implement `PerformAction` state — animation, damage numbers, HP update
5. Implement `LineOfSight` — elevation-based LoS blocking
6. Status effects system (Poison, Haste, Protect — 3-4 for MVP)
7. Unit death and removal
8. Basic `AIController` with utility scoring — move toward enemies, attack best target
9. Victory/Defeat conditions (all enemies dead / all allies dead)
10. Action menu UI (Move / Act / Wait, ability sub-menu)
11. Unit info panel (HP/MP bar, stats on hover)
12. Damage number popups
13. Unit tests for DamageCalculator, LineOfSight

**Deliverable**: A playable battle — player vs AI enemies with abilities, damage, and win/lose.

---

## Phase 4: Job System & Progression (Weeks 8-10)

**Goal**: Characters have jobs, learn abilities, and level up.

1. Implement `JobData` ScriptableObject — stat modifiers, equipment slots, ability list
2. Create 6 job definitions (Squire, Knight, Black Mage, White Mage, Archer, Thief)
3. Implement JP earning — actions in battle grant JP for active job
4. Implement ability learning — spend JP to permanently learn abilities
5. Implement job switching and unlock requirements
6. Implement ability equip slots (secondary, reaction, support, movement)
7. Implement equipment system — weapons, armor, accessories with stat modifiers
8. Party management scene — job change, ability equip, equipment screens
9. Brave/Faith system
10. Stat growth on level-up (varies by current job)
11. Create 20-30 abilities across the 6 jobs
12. Balance pass on stats and formulas

**Deliverable**: Full job system with class switching, ability learning, and equipment.

---

## Phase 5: Content & Polish (Weeks 11-14)

**Goal**: Multiple battles, story flow, generated assets, save/load.

1. Author 5-8 battle maps with varying terrain and objectives
2. Generate character sprites via `/asset` pipeline + PixelLab MCP
3. Generate terrain tiles via PixelLab isometric tile tools
4. Import and configure all generated assets
5. Implement save/load system (JSON to `persistentDataPath`)
6. World map or battle select screen (linear progression)
7. Story dialogue system (simple text boxes between battles)
8. Sound effects and music integration
9. Screen transitions and polish
10. Tutorial battle with guided prompts
11. Full playtest and balance tuning

**Deliverable**: Complete MVP — a polished single-player tactics game with story, progression, and AI-generated art.

---

## Risk Mitigations

| Risk | Mitigation |
|------|-----------|
| Unity isometric sorting bugs | Use custom sorting script early; fallback to SpriteRenderer.sortingOrder |
| AI too dumb or too hard | Tunable AIProfile ScriptableObjects; playtest early |
| Asset pipeline generates inconsistent art | Establish style guide prompts; curate and regenerate |
| Scope creep | Strict phase gates; MVP before any V1 features |
| Solo dev burnout | Each phase has a playable milestone; visible progress |

---

## Dependencies

- Unity 6 LTS
- Newtonsoft JSON (via Unity Package Manager)
- TextMeshPro (bundled with Unity)
- Cinemachine (optional, for camera)
- PixelLab API subscription (for asset generation)
- Gemini API key (for keyframe generation)
