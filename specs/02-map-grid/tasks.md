# System 2: Map & Grid — Tasks

## Tile Data
- [ ] 2.1 Rewrite `TileData.cs` — factory method with terrain defaults, XML docs
- [ ] 2.2 Rewrite `BattleMapData.cs` — TryGetTile pattern, InBounds, XML docs
- [ ] 2.3 Write tests: `TileDataTests.cs`, `BattleMapDataTests.cs`

## Tile View
- [ ] 2.4 Create `TileVisualState` enum
- [ ] 2.5 Create `TileView.cs` MonoBehaviour — sprite, sorting, visual states, color tints
- [ ] 2.6 Create placeholder diamond tile sprite (64x32, procedural or asset)

## Isometric Grid
- [ ] 2.7 Rewrite `IsometricGrid.cs` — Dictionary-based tile lookup, TileView instantiation
- [ ] 2.8 Implement tile overlay API (SetTileOverlay, ClearAllOverlays)
- [ ] 2.9 Implement elevation-aware mouse picking
- [ ] 2.10 Add instance events: OnTileHovered, OnTileClicked

## Camera
- [ ] 2.11 Rewrite `BattleCameraController.cs` — pan, zoom, middle-mouse drag
- [ ] 2.12 Implement camera rotation with grid re-projection and re-sort
- [ ] 2.13 Implement FocusOn / SnapTo with smooth lerp
- [ ] 2.14 Add OnCameraRotated event

## Map Generator
- [ ] 2.15 Rewrite `MapGenerator.cs` — CreateTestMap with elevation, terrain variety, spawn zones
- [ ] 2.16 Add `CreateFlatMap` for test scenarios
- [ ] 2.17 Write tests: `MapGeneratorTests.cs`

## Integration
- [ ] 2.18 Generate terrain tiles via PixelLab (grass, stone, water, sand, lava, forest)
- [ ] 2.19 Verify rendered map in Unity editor with camera controls
- [ ] 2.20 Commit, code review, PR, merge
