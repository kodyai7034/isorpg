# System 13: 3D Rendering Foundation — Specification

## Overview

Replace the 2D sprite-based isometric rendering with real 3D cubes in an orthographic camera. This gives us proper isometric blocks with visible top and side faces, real camera rotation (orbit around the scene), and automatic depth sorting via Unity's 3D depth buffer.

This system handles the rendering foundation only — untextured or default-material blocks. Texture assignment (System 14) and unit integration (System 15) come after.

---

## 1. Why This Change

| Problem | Current (2D) | After (3D) |
|---------|-------------|-----------|
| Tiles show only top face | Flat diamond sprites | Cubes with top + 4 side faces |
| Camera rotation is faked | Re-project all tile positions on rotate | Camera orbits 3D scene — rotation is free |
| Elevation looks flat | Y offset in 2D, no depth | Cubes stack vertically with real depth |
| Sorting is manual | Calculate sortingOrder per tile | Unity 3D depth buffer — automatic |
| Can't add slopes/ramps | Sprite-only | Mesh-based, extensible to any geometry |

---

## 2. URP Installation

### 2.1 Package
Add to `Packages/manifest.json`:
```json
"com.unity.render-pipelines.universal": "17.0.3"
```

### 2.2 Pipeline Asset
QuickSetup creates a `UniversalRenderPipelineAsset` at runtime and assigns it to `GraphicsSettings.defaultRenderPipeline`. Settings:
- Renderer: Forward
- HDR: off (pixel art doesn't need it)
- Anti-aliasing: off (pixel art needs hard edges)
- Shadows: soft shadows enabled (gives blocks depth)

### 2.3 Lighting
QuickSetup adds a Directional Light to the scene:
- Rotation: (50, -30, 0) — illuminates top and one side face
- Color: warm white (1, 0.96, 0.9)
- Intensity: 1.0
- Shadows: soft

---

## 3. Block Geometry

### 3.1 Cube Dimensions
Each terrain block is a Unity cube primitive:
- Scale: `(1, 0.5, 1)` — half-height cube (wider than tall, like FFT blocks)
- This means each grid cell is 1x1 world units, elevation adds 0.5 per level

### 3.2 Block Positioning
Direct 3D grid — no isometric projection math needed:
```csharp
Vector3 worldPos = new Vector3(gridX, elevation * BlockHeight, gridY);
// Where BlockHeight = 0.5f
```

### 3.3 Block Prefab
Created by QuickSetup at editor time:
- `MeshFilter` → Unity's built-in Cube mesh
- `MeshRenderer` → default URP Lit material (grey, replaced per-terrain in System 14)
- `BoxCollider` → for Physics.Raycast mouse picking
- `TileView3D` component → visual state management

---

## 4. TileView3D (Replaces TileView)

### 4.1 Responsibilities
- Holds reference to `MeshRenderer` for the block
- Manages visual states via `MaterialPropertyBlock` (no material copies)
- Stores `TileData` for game logic queries
- `BoxCollider` enables raycasting

### 4.2 Visual States (same as TileView)
| State | Color Tint |
|-------|-----------|
| Default | White (1,1,1) — material renders normally |
| Highlighted | Yellow tint (1, 1, 0.6) |
| Selected | Bright yellow (1, 1, 0.3) |
| MoveRange | Blue tint (0.5, 0.7, 1) |
| AttackRange | Red tint (1, 0.4, 0.4) |
| HazardWarning | Orange tint (1, 0.6, 0.2) |
| PathPreview | Light blue (0.4, 0.8, 1) |

Applied via `MaterialPropertyBlock.SetColor("_BaseColor", tint)` — URP Lit shader's base color property.

### 4.3 Interface
```csharp
public class TileView3D : MonoBehaviour
{
    public TileData Data { get; }
    public Vector2Int GridPosition { get; }
    public TileVisualState CurrentState { get; }

    public void Initialize(TileData data, Material material = null);
    public void SetVisualState(TileVisualState state);
    public void ResetVisualState();
}
```

---

## 5. IsometricGrid Rework

### 5.1 BuildMap Changes
```
Before (2D):
  worldPos = IsoMath.GridToWorld(x, y, elevation)  // 2D isometric projection
  Instantiate tilePrefab → SpriteRenderer
  sr.sortingOrder = IsoMath.CalculateSortingOrder(...)

After (3D):
  worldPos = new Vector3(x, elevation * 0.5f, y)   // direct 3D position
  Instantiate blockPrefab → MeshRenderer
  // No sorting needed — depth buffer
```

### 5.2 Tile Lookup
Keep `Dictionary<Vector2Int, TileView3D>` for game logic (pathfinding, unit queries, overlay API). Interface stays the same — only the rendering changes.

### 5.3 Overlay API (unchanged interface)
```csharp
public void SetTileOverlay(IEnumerable<Vector2Int> tiles, TileVisualState state);
public void ClearAllOverlays();
public TileView3D GetTileView(Vector2Int pos);
public bool TryGetTileView(Vector2Int pos, out TileView3D view);
```

### 5.4 Mouse Picking (3D Raycast)
```csharp
public Vector2Int ScreenToGrid(Vector3 screenPos)
{
    var ray = Camera.main.ScreenPointToRay(screenPos);
    if (Physics.Raycast(ray, out var hit))
    {
        var view = hit.collider.GetComponent<TileView3D>();
        if (view != null)
            return view.GridPosition;
    }
    return new Vector2Int(-1, -1);
}
```

### 5.5 Hover/Click Handling
Same event pattern — `OnTileHovered(Vector2Int)`, `OnTileClicked(Vector2Int)`. Implementation changes from screen-to-grid math to Physics.Raycast, but the events stay identical so battle states don't need changes.

---

## 6. Camera Rework (3D Orbit)

### 6.1 Isometric Angle
```csharp
// Standard isometric: 30° pitch, 45° yaw
Quaternion targetRotation = Quaternion.Euler(30, 45 + rotationIndex * 90, 0);
```

### 6.2 Rotation (Q/E)
- Q: `rotationIndex = (rotationIndex + 1) % 4`
- E: `rotationIndex = (rotationIndex + 3) % 4`
- Camera smoothly lerps to new rotation over ~0.3s
- No grid re-projection — the camera literally orbits

### 6.3 Pan (WASD)
Pan relative to camera's current facing:
```csharp
Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
Vector3 move = right * horizontal + forward * vertical;
```

### 6.4 Zoom (Scroll)
Adjust `camera.orthographicSize` — same as before.

### 6.5 Focus/Snap
```csharp
public void FocusOn(Vector3 worldPos);  // smooth lerp to position
public void SnapTo(Vector3 worldPos);   // instant jump
```

Camera maintains a `_lookTarget` (center of the map) and positions itself relative to that point based on rotation + distance.

---

## 7. Grid Constants

```csharp
public static class GridConstants
{
    public const float BlockWidth = 1f;      // X and Z size
    public const float BlockHeight = 0.5f;   // Y size per elevation level
    public const float IsoPitch = 30f;       // camera pitch angle
    public const float IsoYaw = 45f;         // base camera yaw
}
```

---

## 8. What Stays the Same

These systems are UNCHANGED by this rework:
- `BattleMapData`, `TileData`, `MapGenerator` — data layer untouched
- `BattleContext`, `BattleStateMachine`, all battle states — they interact via grid position (Vector2Int), not rendering
- `CTSystem`, `Pathfinder`, `DamageCalculator`, `LineOfSight` — pure logic, no rendering
- `CommandHistory`, `GameEvents`, `GameRng` — core infrastructure
- All UI (ActionMenuUI, CombatMenuUI, etc.) — Screen Space Overlay, unaffected by 3D
- `UnitInstance`, `UnitRegistry`, `JobSystem` — pure data

---

## 9. What Gets Deprecated

- `TileView.cs` — replaced by `TileView3D.cs`
- `IsoMath.GridToWorld` / `WorldToGrid` — no longer used for tile placement (kept for game logic utilities like ManhattanDistance)
- `IsoMath.CalculateSortingOrder` — no longer needed (3D depth buffer)
- `IsoMath.RotateGrid` — no longer needed (camera orbits instead)
- `IsometricGrid.CreateFallbackDiamondSprite` — replaced by cube primitive
- `IsometricGrid.ApplyRotation` — replaced by camera orbit

---

## 10. QuickSetup Changes

QuickSetup must:
1. Create URP pipeline asset and assign to graphics settings
2. Add directional light
3. Set camera to orthographic at isometric angle (30° pitch, 45° yaw)
4. Create block prefab (cube with MeshFilter + MeshRenderer + BoxCollider + TileView3D)
5. Create default material (grey URP Lit)
6. Wire IsometricGrid with block prefab
7. Remove old tile sprite prefab creation

---

## 11. Tasks

- [ ] 13.1 Add URP package to manifest.json
- [ ] 13.2 Create `GridConstants.cs` with block dimensions and camera angles
- [ ] 13.3 Create `TileView3D.cs` — MeshRenderer, MaterialPropertyBlock, BoxCollider
- [ ] 13.4 Rework `IsometricGrid.cs` — 3D cube instantiation, Physics.Raycast picking, remove 2D projection
- [ ] 13.5 Rework `BattleCameraController.cs` — 3D orbit rotation, direction-relative pan, smooth rotation lerp
- [ ] 13.6 Update `QuickSetup.cs` — URP setup, directional light, 3D block prefab, camera angle
- [ ] 13.7 Keep `IsoMath.cs` utility functions (ManhattanDistance, GetDirection) but deprecate rendering functions
- [ ] 13.8 Verify: blocks render with top + side faces visible
- [ ] 13.9 Verify: Q/E rotates camera, blocks correct from all angles
- [ ] 13.10 Verify: click block highlights, movement overlay works
- [ ] 13.11 Verify: existing battle states still function (Move, Act, Wait)
- [ ] 13.12 Verify: UI menus unaffected
- [ ] 13.13 Commit, PR, merge

---

## 12. Exit Criteria

A playable battle on an 8x8 grid of 3D grey cubes with:
- Visible top and side faces
- Elevation as stacked block height
- Camera rotation via Q/E (smooth 90° orbit)
- WASD pans relative to camera direction
- Click highlights blocks (blue movement range, red attack range)
- All existing battle gameplay works (Move, Act, Wait, Undo, AI turns)
- UI menus render correctly on top of 3D scene
