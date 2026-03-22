# IsoRPG — Core Architecture Specification

## Overview

An isometric tactics RPG inspired by Final Fantasy Tactics, built in Unity 6 with C#. The game features grid-based tactical combat with elevation, a job/class system, CT-based turn order, and AI-generated pixel art assets.

---

## 1. Game Loop & State Machine

The game operates as a hierarchical state machine managed by a `GameManager` singleton:

```
GameManager (persistent across scenes)
├── MainMenuState
├── WorldMapState (future - story mode)
├── BattleState
│   ├── DeploymentPhase
│   ├── BattleLoop
│   │   ├── CTAdvance (tick all units' CT by Speed)
│   │   ├── ActiveTurn
│   │   │   ├── SelectAction (Move / Act / Wait)
│   │   │   ├── MoveTarget (pathfinding overlay, confirm)
│   │   │   ├── ActionTarget (ability range overlay, confirm)
│   │   │   ├── PerformAction (animation, damage calc, resolution)
│   │   │   └── EndTurn (CT reset, status tick, death check)
│   │   └── AITurn
│   │       ├── EvaluateOptions (utility scoring)
│   │       ├── ExecuteAction (move + act)
│   │       └── EndTurn
│   ├── VictoryState
│   └── DefeatState
└── PartyManagementState
    ├── JobChange
    ├── AbilityEquip
    └── Equipment
```

State machine implemented as a pure C# class (not MonoBehaviour) with `IState` interface:

```csharp
public interface IBattleState
{
    void Enter(BattleContext context);
    void Execute(BattleContext context);
    void Exit(BattleContext context);
}
```

---

## 2. Map System

### 2.1 Data Model

```csharp
[System.Serializable]
public struct TileData
{
    public Vector2Int position;    // grid coords
    public int elevation;          // height (0-15)
    public TerrainType terrain;    // enum: Grass, Stone, Water, Sand, Lava, Forest
    public bool walkable;
    public int moveCost;           // 1 = normal, 2 = difficult, 999 = impassable
    public int cover;              // 0-2 defense bonus
    public HazardType hazard;      // None, Poison, Fire, Heal
}

[CreateAssetMenu(fileName = "NewMap", menuName = "IsoRPG/BattleMap")]
public class BattleMapData : ScriptableObject
{
    public int width;
    public int height;
    public TileData[] tiles;       // flattened 2D array
    public SpawnZone[] spawnZones;
    public Objective[] objectives;
}
```

### 2.2 Isometric Projection

Using Unity's built-in Isometric Tilemap or custom sprite placement with a 2:1 tile ratio (64x32 pixels). Elevation offset: 16px per level.

**Grid-to-World conversion:**
```csharp
public static Vector3 GridToWorld(Vector2Int grid, int elevation)
{
    float x = (grid.x - grid.y) * TileWidthHalf;
    float y = (grid.x + grid.y) * TileHeightHalf + elevation * ElevationHeight;
    return new Vector3(x, y, 0f);
}
```

**World-to-Grid (mouse picking):**
Uses Physics2D.Raycast against tile colliders, or inverse math with elevation sampling.

### 2.3 Sorting Order

Unity's Sorting Order per sprite:
```csharp
sortingOrder = (elevation * MaxDepth) + (gridX + gridY);
```

Alternatively use Unity's custom Transparency Sort Axis for isometric: `Camera.main.transparencySortAxis = new Vector3(0, 1, 0);`

### 2.4 Map Sizes

Compact arena-style maps, FFT-inspired: 10x10 to 16x16 tiles. Maps authored via:
- Unity Tilemap editor with custom brushes
- ScriptableObject JSON import
- Future: custom level editor tool

---

## 3. Unit System

### 3.1 Unit Data Model

```csharp
[CreateAssetMenu(fileName = "NewUnit", menuName = "IsoRPG/UnitTemplate")]
public class UnitTemplate : ScriptableObject
{
    public string unitName;
    public Sprite portrait;
    public RuntimeAnimatorController animatorController;
    public JobId startingJob;
    public BaseStats baseStats;
}

[System.Serializable]
public struct BaseStats
{
    public int maxHP, maxMP;
    public int physicalAttack, magicAttack;
    public int speed, move, jump;
    public int brave;   // 0-100
    public int faith;   // 0-100
}

public class UnitInstance
{
    public string Id { get; }
    public UnitTemplate Template { get; }
    public int Team { get; set; }           // 0=player, 1=enemy, 2=neutral
    public int Level { get; set; }
    public Vector2Int GridPosition { get; set; }
    public Direction Facing { get; set; }

    // Runtime stats (base + job modifier + equipment)
    public int CurrentHP { get; set; }
    public int CurrentMP { get; set; }
    public ComputedStats Stats { get; }     // recalculated on job/equip change

    // CT System
    public int CT { get; set; }

    // Job & Abilities
    public JobId CurrentJob { get; set; }
    public Dictionary<JobId, int> JobLevels { get; }
    public Dictionary<JobId, int> JobPoints { get; }
    public HashSet<AbilityId> LearnedAbilities { get; }

    // Equipped slots
    public JobId? SecondaryAbilitySet { get; set; }
    public AbilityId? ReactionAbility { get; set; }
    public AbilityId? SupportAbility { get; set; }
    public AbilityId? MovementAbility { get; set; }

    // Equipment
    public EquipmentLoadout Equipment { get; }

    // Status
    public List<StatusEffectInstance> StatusEffects { get; }
}
```

### 3.2 CT (Charge Time) Turn System

Per FFT:
1. All units start CT = 0
2. Each tick: `unit.CT += unit.Stats.Speed` for all living units
3. When `unit.CT >= 100`: that unit gets an Active Turn
4. If multiple units hit 100 same tick: highest CT first, then highest Speed
5. After acting:
   - Move + Act: `CT -= 100`
   - Move only or Act only: `CT -= 80`
   - Wait (neither): `CT -= 60`
   - Cap resulting CT at 60 max

Managed by `CTSystem.cs` — pure logic, no MonoBehaviour.

### 3.3 Stats & Formulas

**Physical Damage:**
```
rawDamage = physicalAttack * weaponPower
modifier = zodiacCompat * braveModifier * elementalModifier
finalDamage = Mathf.FloorToInt(rawDamage * modifier) - targetDefense
```

**Magic Damage:**
```
rawDamage = spellPower * magicAttack
modifier = (casterFaith / 100f) * (targetFaith / 100f) * zodiacCompat * elementalModifier
finalDamage = Mathf.FloorToInt(rawDamage * modifier) - targetMagicDefense
```

**Hit Rate:**
```
hitChance = baseAccuracy + attackerSpeed - targetSpeed + (heightAdvantage * 5)
```

All formulas in `DamageCalculator.cs` — static pure functions, fully unit-testable.

---

## 4. Job System

### 4.1 Initial Jobs (MVP)

| Job | Role | Stat Focus | Unique Mechanic |
|-----|------|-----------|-----------------|
| Squire | Starter | Balanced | Basic support abilities |
| Knight | Tank/Physical | HP, PA | Break abilities (destroy enemy stats/equipment) |
| Black Mage | Offensive Magic | MA | Elemental AoE spells |
| White Mage | Healing/Support | MA | Heal, status removal, buffs |
| Archer | Ranged Physical | PA, Speed | Long range, height advantage bonus |
| Thief | Utility/Speed | Speed | Steal, high evasion |

### 4.2 Data-Driven Job Definitions

```csharp
[CreateAssetMenu(fileName = "NewJob", menuName = "IsoRPG/Job")]
public class JobData : ScriptableObject
{
    public JobId id;
    public string jobName;
    public Sprite icon;
    public StatModifiers statModifiers;         // multipliers applied to base stats
    public EquipmentSlotConfig equipmentSlots;   // what this job can equip
    public AbilityData[] abilities;              // learnable abilities + JP costs
    public JobRequirement[] unlockRequirements;  // e.g., Squire Lv 2
}
```

### 4.3 Job Unlock Tree (MVP)

```
Squire (default)
├── Knight (Squire Lv 2)
├── Archer (Squire Lv 2)
├── Black Mage (Squire Lv 2)
├── White Mage (Squire Lv 2)
└── Thief (Squire Lv 3)
```

### 4.4 Ability Learning

- Actions in battle earn JP for active job
- JP spent to learn abilities within job's skill tree
- Learned abilities persist across job changes
- Each unit equips: 1 primary set (current job), 1 secondary set, 1 reaction, 1 support, 1 movement

---

## 5. Pathfinding & Movement

### 5.1 Algorithm

A* on 2D grid with constraints:
- Movement range: BFS flood fill from unit position, up to `unit.Stats.Move` tiles
- Terrain cost: each tile's `moveCost` consumed from movement budget
- Elevation: can traverse if `Mathf.Abs(targetElevation - currentElevation) <= unit.Stats.Jump`
- Collision: cannot move through enemy units; can move through allied units

Implemented in `Pathfinder.cs` — pure C# class operating on `BattleMapData`.

### 5.2 Range Calculation

Manhattan distance: `distance = Mathf.Abs(x1-x2) + Mathf.Abs(y1-y2)`
Height bonus: +1 effective range per height level when attacking downhill.

### 5.3 Line of Sight

Raycast from attacker to target in grid space. Check each intermediate tile — if any tile's elevation exceeds both attacker and target elevation, LoS is blocked.

---

## 6. AI System

### 6.1 Architecture: Utility AI

Each AI unit evaluates all possible (move, action) combinations and scores them:

```csharp
public struct AIOption
{
    public Vector2Int moveTo;
    public AbilityId action;    // or "Wait"
    public UnitInstance target;
    public float score;
}
```

### 6.2 Scoring Factors

| Factor | Weight | Description |
|--------|--------|-------------|
| Damage dealt | +3 per point | Prioritize high damage |
| Kill potential | +50 | Bonus if target would die |
| Healing done | +2 per point | Heal wounded allies |
| Self-preservation | -5 per point damage risked | Avoid dying |
| Position safety | +10 | Prefer tiles with cover / high ground |
| Target threat | +20 | Focus high-damage enemies |

Weights stored in `AIProfile` ScriptableObjects — tunable per enemy unit.

### 6.3 AI Profiles

```csharp
[CreateAssetMenu(fileName = "NewAIProfile", menuName = "IsoRPG/AIProfile")]
public class AIProfile : ScriptableObject
{
    public float damageWeight = 3f;
    public float killBonus = 50f;
    public float healingWeight = 2f;
    public float selfPreservation = 5f;
    public float positionSafety = 10f;
    public float targetThreat = 20f;
}
```

Presets: Aggressive, Defensive, Support, Coward.

---

## 7. Rendering & Camera

### 7.1 Unity Scene Structure

```
Scenes/
├── Boot.unity           # Initialization, load persistent managers
├── MainMenu.unity
├── Battle.unity         # Isometric battle arena
└── PartyManagement.unity
```

Persistent across scenes via `DontDestroyOnLoad`:
- `GameManager` (state machine, save/load)
- `AudioManager`
- `AssetRegistry`

### 7.2 Battle Scene Hierarchy

```
BattleScene
├── Grid (parent for all tile sprites)
│   ├── Tile_0_0
│   ├── Tile_0_1 ...
├── Units (parent for all unit GameObjects)
│   ├── Unit_Player_0
│   ├── Unit_Enemy_0 ...
├── Overlays (movement range, ability range highlights)
├── Effects (VFX, particles)
├── BattleCamera
└── UI (Canvas)
    ├── TurnOrderBar
    ├── ActionMenu
    ├── UnitInfoPanel
    └── DamageNumbers
```

### 7.3 Camera

- Orthographic camera for pixel-perfect 2D
- 4-way rotation (90° snaps) — rotate grid visualization, re-sort sprites
- Zoom: 0.5x to 3.0x via `Camera.orthographicSize`
- Smooth follow active unit via `Cinemachine` or custom lerp
- Controlled by `BattleCameraController.cs` MonoBehaviour

### 7.4 Sprite Assets

- Characters: 64x64 pixel art, 4 or 8 directions, sprite sheets imported as Unity Sprites
- Tiles: 64x32 isometric tiles, Sprite Atlas for batching
- Effects: Unity Particle System + animated sprites
- Import settings: `Filter Mode = Point`, `Compression = None`, `Pixels Per Unit = 64`

---

## 8. Asset Pipeline Integration

### 8.1 Character Creation Flow

1. `/asset <character description>` — Gemini keyframes + PixelLab interpolation for custom animations
2. `mcp__pixellab__create_character` — PixelLab native for standard directional sprites
3. `mcp__pixellab__animate_character` — Template animations (walk, attack, etc.)
4. Output PNGs imported into Unity `Assets/Sprites/Characters/`
5. Sprite sheets sliced via Unity Sprite Editor or `TexturePacker`

### 8.2 Tile Creation

- `mcp__pixellab__create_isometric_tile` for terrain tiles
- Output to `Assets/Sprites/Tiles/`
- Configured as Isometric Tile Assets for Unity Tilemap

### 8.3 Asset Registry

All generated assets tracked in `assets/registry.json` (outside Unity project):
```json
{
  "characters": { "<id>": { "manifest": "path/to/manifest.json" } },
  "tiles": { "<id>": { "path": "...", "terrain": "grass" } },
  "effects": { "<id>": { ... } }
}
```

Import script (`scripts/import-assets.py`) copies and configures assets into Unity project.

---

## 9. Data Storage & Persistence

### 9.1 Local (MVP)

- Save/load via `JsonUtility` or Newtonsoft JSON to `Application.persistentDataPath`
- Battle maps as `BattleMapData` ScriptableObjects
- Job/ability definitions as `JobData` / `AbilityData` ScriptableObjects
- All static game data lives in `Assets/Data/`

### 9.2 Supabase (V1+)

- User auth (Supabase Auth via REST)
- Cloud save sync (Supabase Database)
- Leaderboards / PvP matchmaking (future)
- Unity HTTP client via `UnityWebRequest` wrapper

---

## 10. Unity Project Structure

```
IsoRPG/
├── Assets/
│   ├── Scenes/
│   │   ├── Boot.unity
│   │   ├── MainMenu.unity
│   │   ├── Battle.unity
│   │   └── PartyManagement.unity
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   ├── StateMachine.cs
│   │   │   └── ServiceLocator.cs
│   │   ├── Battle/
│   │   │   ├── BattleManager.cs
│   │   │   ├── BattleStateMachine.cs
│   │   │   ├── CTSystem.cs
│   │   │   ├── DamageCalculator.cs
│   │   │   ├── Pathfinder.cs
│   │   │   ├── LineOfSight.cs
│   │   │   └── AIController.cs
│   │   ├── Map/
│   │   │   ├── IsometricGrid.cs
│   │   │   ├── TileRenderer.cs
│   │   │   ├── MapLoader.cs
│   │   │   └── BattleCameraController.cs
│   │   ├── Units/
│   │   │   ├── UnitInstance.cs
│   │   │   ├── UnitView.cs          # MonoBehaviour for sprite/animation
│   │   │   ├── JobSystem.cs
│   │   │   ├── AbilitySystem.cs
│   │   │   └── StatusEffects.cs
│   │   ├── UI/
│   │   │   ├── ActionMenuUI.cs
│   │   │   ├── TurnOrderBarUI.cs
│   │   │   ├── UnitInfoPanelUI.cs
│   │   │   └── DamageNumberUI.cs
│   │   └── Data/
│   │       ├── Persistence.cs
│   │       └── SupabaseClient.cs
│   ├── Data/
│   │   ├── Jobs/                    # JobData ScriptableObjects
│   │   ├── Abilities/               # AbilityData ScriptableObjects
│   │   ├── Maps/                    # BattleMapData ScriptableObjects
│   │   ├── AIProfiles/              # AIProfile ScriptableObjects
│   │   └── UnitTemplates/           # UnitTemplate ScriptableObjects
│   ├── Sprites/
│   │   ├── Characters/
│   │   ├── Tiles/
│   │   ├── Effects/
│   │   └── UI/
│   ├── Animations/
│   ├── Prefabs/
│   │   ├── Units/
│   │   ├── Tiles/
│   │   └── Effects/
│   ├── Materials/
│   └── Plugins/                     # Third-party (Newtonsoft JSON, etc.)
├── Packages/
├── ProjectSettings/
├── Tests/
│   ├── EditMode/                    # Pure logic unit tests
│   │   ├── CTSystemTests.cs
│   │   ├── DamageCalculatorTests.cs
│   │   ├── PathfinderTests.cs
│   │   └── JobSystemTests.cs
│   └── PlayMode/                    # Integration tests
├── .specify/                        # Speckit specs (outside Assets)
├── .claude/                         # Claude commands
├── assets/                          # AI-generated raw assets (pre-import)
└── scripts/                         # Asset pipeline scripts
```

---

## 11. Assembly Definitions

Module boundaries enforced via `.asmdef` files:

| Assembly | Contains | Dependencies |
|----------|----------|-------------|
| `IsoRPG.Core` | GameManager, StateMachine, ServiceLocator | None |
| `IsoRPG.Battle` | CTSystem, DamageCalculator, Pathfinder, AI | Core |
| `IsoRPG.Units` | UnitInstance, JobSystem, AbilitySystem | Core, Battle |
| `IsoRPG.Map` | IsometricGrid, MapLoader | Core |
| `IsoRPG.UI` | All UI scripts | Core, Battle, Units |
| `IsoRPG.Data` | Persistence, SupabaseClient | Core |
| `IsoRPG.Tests.EditMode` | Unit tests | Core, Battle, Units, Map |

This keeps compile times fast and enforces clean dependency flow.
