# System 2: Map & Grid — Specification

## Overview

The map system manages isometric tile data, rendering, camera controls, and mouse interaction. It is the visual foundation of every battle — all units, effects, and UI overlays exist relative to the grid.

---

## 1. Tile Data

### 1.1 TileData Struct

```csharp
[System.Serializable]
public struct TileData
{
    public Vector2Int Position;
    public int Elevation;           // 0 to GameConstants.MaxElevation
    public TerrainType Terrain;
    public bool Walkable;
    public int MoveCost;            // 1 = normal, 2 = difficult, 999 = impassable
    public int Cover;               // 0-2 defense bonus
    public HazardType Hazard;
}
```

### 1.2 Terrain Defaults

| Terrain | Walkable | MoveCost | Cover | Default Hazard |
|---------|----------|----------|-------|----------------|
| Grass | true | 1 | 0 | None |
| Stone | true | 1 | 0 | None |
| Water | false | 999 | 0 | None |
| Sand | true | 1 | 0 | None |
| Lava | false | 999 | 0 | Fire |
| Forest | true | 2 | 1 | None |

### 1.3 Factory Method

```csharp
public static TileData Create(int x, int y, int elevation, TerrainType terrain)
```

Applies terrain defaults automatically. Custom overrides via optional parameters.

---

## 2. BattleMapData (ScriptableObject)

### 2.1 Data Model

```csharp
[CreateAssetMenu(fileName = "NewMap", menuName = "IsoRPG/BattleMap")]
public class BattleMapData : ScriptableObject
{
    public string MapName;
    public int Width;
    public int Height;
    public TileData[] Tiles;        // flattened [y * Width + x]
    public SpawnZone[] SpawnZones;
}
```

### 2.2 Safe Access

```csharp
/// <summary>
/// Try to get tile data at grid position. Returns false if out of bounds.
/// </summary>
public bool TryGetTile(Vector2Int pos, out TileData tile);
public bool TryGetTile(int x, int y, out TileData tile);

/// <summary>
/// Check if position is within map boundaries.
/// </summary>
public bool InBounds(Vector2Int pos);
```

Out-of-bounds access returns `false` — never returns default/sentinel structs that could be misinterpreted as valid tiles.

---

## 3. TileView (MonoBehaviour)

### 3.1 Responsibilities

- Holds reference to a `SpriteRenderer` for the tile sprite
- Manages sorting order via `IsoMath.CalculateSortingOrder`
- Supports visual states: Default, Highlighted (mouse hover), Selected, MovementRange, AttackRange, Hazard
- Applies color tint per state
- Does NOT hold game logic — purely visual

### 3.2 Visual States

```csharp
public enum TileVisualState
{
    Default,
    Highlighted,    // mouse hover — light yellow
    Selected,       // clicked/active — bright yellow
    MoveRange,      // reachable by movement — blue
    AttackRange,    // targetable by ability — red
    HazardWarning   // hazard tile — pulsing red
}
```

### 3.3 Interface

```csharp
public void Initialize(TileData data, Sprite sprite);
public void SetVisualState(TileVisualState state);
public void ResetVisualState();
public TileData Data { get; }
public Vector2Int GridPosition { get; }
```

---

## 4. IsometricGrid (MonoBehaviour)

### 4.1 Responsibilities

- Loads a `BattleMapData` and instantiates `TileView` GameObjects
- Maintains a `Dictionary<Vector2Int, TileView>` for O(1) tile lookup
- Handles mouse-to-grid picking with elevation awareness
- Provides public API for overlays (set visual state on tile ranges)
- Fires events: `OnTileHovered(Vector2Int)`, `OnTileClicked(Vector2Int)`

### 4.2 Tile Lookup

```csharp
/// <summary>
/// Get the TileView at a grid position. Returns null if out of bounds.
/// </summary>
public TileView GetTileView(Vector2Int pos);

/// <summary>
/// Try to get TileView. Preferred over GetTileView for safety.
/// </summary>
public bool TryGetTileView(Vector2Int pos, out TileView view);
```

### 4.3 Overlay API

```csharp
/// <summary>
/// Set visual state on a collection of tiles. Used for movement range, attack range, etc.
/// </summary>
public void SetTileOverlay(IEnumerable<Vector2Int> tiles, TileVisualState state);

/// <summary>
/// Clear all tile overlays, resetting to default visual state.
/// </summary>
public void ClearAllOverlays();
```

### 4.4 Mouse Picking

Grid picking must account for elevation:
1. Raycast from camera through mouse position
2. Convert world position to grid coords at each elevation level (high to low)
3. Check if a tile exists at that grid position with that elevation
4. Return the first match (highest elevation tile under cursor)

---

## 5. BattleCameraController (MonoBehaviour)

### 5.1 Controls

| Input | Action |
|-------|--------|
| WASD / Arrow keys | Pan camera |
| Mouse wheel | Zoom (orthographic size) |
| Q / E | Rotate view 90° CW / CCW |
| Middle mouse drag | Pan camera (alternative) |

### 5.2 Configuration (SerializeField)

```csharp
float PanSpeed = 5f;
float ZoomSpeed = 1f;
float MinZoom = 2f;
float MaxZoom = 10f;
float FollowSpeed = 5f;
```

### 5.3 Camera Rotation

Rotation changes the visual projection but NOT the grid data. When rotating:
1. Increment `RotationIndex` (0-3)
2. Recalculate all tile world positions using `IsoMath.RotateGrid`
3. Re-sort all sprite sorting orders
4. Notify listeners via event `OnCameraRotated(int rotationIndex)`

### 5.4 Follow Mode

```csharp
/// <summary>Center camera on world position with smooth lerp.</summary>
public void FocusOn(Vector3 worldPosition);

/// <summary>Snap camera immediately to position.</summary>
public void SnapTo(Vector3 worldPosition);
```

---

## 6. MapGenerator (Static Utility)

Creates test maps for prototyping without the Unity editor:

```csharp
public static BattleMapData CreateTestMap(int width = 8, int height = 8);
public static BattleMapData CreateFlatMap(int width, int height, TerrainType terrain = TerrainType.Grass);
```

Test map features:
- Center raised platform (elevation 2)
- Steps around center (elevation 1)
- Water in corners
- Stone path through middle
- Forest on edges
- Spawn zones for 2 teams

---

## 7. Placeholder Tile Sprite

Until PixelLab-generated tiles are ready, use a procedural diamond sprite:
- 64x32 pixel diamond shape
- Colored by terrain type
- Generated at runtime via `Texture2D` or included as a simple asset
- Filter Mode: Point, Compression: None, Pixels Per Unit: 64

---

## 8. Events

| Event | Args | When |
|-------|------|------|
| `OnTileHovered` | `Vector2Int` | Mouse moves over a new tile |
| `OnTileClicked` | `Vector2Int` | Mouse clicks a tile |
| `OnCameraRotated` | `int rotationIndex` | Camera rotation changes |

These are instance events on `IsometricGrid` / `BattleCameraController`, not global `GameEvents` (since they are scene-specific).

---

## 9. Test Coverage

| Class | Tests |
|-------|-------|
| `TileData` | Factory method applies terrain defaults correctly |
| `BattleMapData` | TryGetTile in-bounds/out-of-bounds, InBounds edge cases |
| `IsoMath` | Already covered in System 1 (round-trips, rotation) |
| `MapGenerator` | CreateTestMap produces valid map with correct dimensions, spawn zones |
