using System;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// Manages isometric tile rendering, mouse picking, and tile overlays.
    /// Loads a <see cref="BattleMapData"/> and instantiates <see cref="TileView"/> GameObjects.
    ///
    /// Provides O(1) tile lookup via dictionary and elevation-aware mouse-to-grid conversion.
    /// </summary>
    public class IsometricGrid : MonoBehaviour
    {
        [SerializeField] private BattleMapData mapData;
        [SerializeField] private GameObject tilePrefab;

        private Dictionary<Vector2Int, TileView> _tileViews = new();
        private Vector2Int _hoveredTile = new(-1, -1);
        private TileView _hoveredView;
        private int _rotationIndex;

        /// <summary>The loaded map data.</summary>
        public BattleMapData MapData => mapData;

        /// <summary>Currently hovered grid position. (-1,-1) if none.</summary>
        public Vector2Int HoveredTile => _hoveredTile;

        /// <summary>Current camera rotation index (0-3).</summary>
        public int RotationIndex => _rotationIndex;

        // --- Instance Events (scene-specific, not global GameEvents) ---

        /// <summary>Fired when mouse hovers over a new tile.</summary>
        public event Action<Vector2Int> OnTileHovered;

        /// <summary>Fired when a tile is clicked.</summary>
        public event Action<Vector2Int> OnTileClicked;

        /// <summary>
        /// Load and render a battle map. Destroys any previously loaded map.
        /// </summary>
        /// <param name="data">Map data to load.</param>
        public void LoadMap(BattleMapData data)
        {
            if (data == null)
            {
                Debug.LogError("[IsometricGrid] Cannot load null map data.");
                return;
            }

            mapData = data;
            ClearMap();
            BuildMap();
        }

        private void Start()
        {
            if (mapData != null && _tileViews.Count == 0)
                BuildMap();
        }

        private void Update()
        {
            HandleMouseHover();
            HandleMouseClick();
        }

        private Sprite _fallbackSprite;

        private void BuildMap()
        {
            if (mapData == null || mapData.Tiles == null) return;

            // Create fallback sprite if no prefab or prefab has no sprite
            if (_fallbackSprite == null)
                _fallbackSprite = CreateFallbackDiamondSprite();

            _tileViews.Clear();

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    if (!mapData.TryGetTile(x, y, out var tileData))
                        continue;

                    GameObject tileObj;
                    if (tilePrefab != null)
                    {
                        var worldPos = IsoMath.GridToWorld(x, y, tileData.Elevation);
                        tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        tileObj = new GameObject();
                        tileObj.AddComponent<SpriteRenderer>();
                        tileObj.transform.SetParent(transform);
                    }

                    var tileView = tileObj.GetComponent<TileView>();
                    if (tileView == null)
                        tileView = tileObj.AddComponent<TileView>();

                    // Ensure sprite exists — use fallback if prefab sprite is null
                    var sr = tileObj.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite == null)
                        sr.sprite = _fallbackSprite;

                    tileView.Initialize(tileData);
                    _tileViews[new Vector2Int(x, y)] = tileView;
                }
            }

            Debug.Log($"[IsometricGrid] Loaded '{mapData.MapName}' ({mapData.Width}x{mapData.Height}, {_tileViews.Count} tiles)");
        }

        /// <summary>
        /// Creates a simple diamond sprite at runtime as fallback when no tile sprite asset exists.
        /// </summary>
        private static Sprite CreateFallbackDiamondSprite()
        {
            int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[s * s];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            var fill = new Color32(200, 200, 200, 255);
            var edge = new Color32(100, 100, 100, 255);

            // Draw isometric diamond (top face)
            int cx = 32, cy = 48;
            for (int row = 0; row <= 15; row++)
            {
                int w = row * 2;
                for (int dx = -w; dx <= w; dx++)
                {
                    SetPx(px, s, cx + dx, cy - row, fill);
                    if (row < 15) SetPx(px, s, cx + dx, cy - 16 - row + 16, fill);
                }
                SetPx(px, s, cx - w, cy - row, edge);
                SetPx(px, s, cx + w, cy - row, edge);
            }

            // Draw side faces
            for (int row = 0; row < 16; row++)
            {
                int y = 32 - row;
                int half = 30 - row * 2;
                if (half < 0) half = 0;
                for (int dx = -half; dx < 0; dx++)
                    SetPx(px, s, cx + dx, y, new Color32(160, 160, 160, 255));
                for (int dx = 0; dx <= half; dx++)
                    SetPx(px, s, cx + dx, y, new Color32(130, 130, 130, 255));
            }

            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.75f), 64);
        }

        private static void SetPx(Color32[] px, int s, int x, int y, Color32 c)
        {
            if (x >= 0 && x < s && y >= 0 && y < s) px[y * s + x] = c;
        }

        private void ClearMap()
        {
            foreach (var kvp in _tileViews)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _tileViews.Clear();
            _hoveredTile = new Vector2Int(-1, -1);
            _hoveredView = null;
        }

        // --- Tile Lookup ---

        /// <summary>
        /// Get the TileView at a grid position.
        /// </summary>
        /// <param name="pos">Grid position.</param>
        /// <returns>TileView if found, null otherwise.</returns>
        public TileView GetTileView(Vector2Int pos)
        {
            _tileViews.TryGetValue(pos, out var view);
            return view;
        }

        /// <summary>
        /// Try to get a TileView at the given position.
        /// </summary>
        public bool TryGetTileView(Vector2Int pos, out TileView view)
        {
            return _tileViews.TryGetValue(pos, out view);
        }

        // --- Overlay API ---

        /// <summary>
        /// Set visual state on a collection of tiles for movement/attack range display.
        /// </summary>
        /// <param name="tiles">Grid positions to highlight.</param>
        /// <param name="state">Visual state to apply.</param>
        public void SetTileOverlay(IEnumerable<Vector2Int> tiles, TileVisualState state)
        {
            if (tiles == null) return;

            foreach (var pos in tiles)
            {
                if (_tileViews.TryGetValue(pos, out var view))
                    view.SetVisualState(state);
            }
        }

        /// <summary>
        /// Clear all tile overlays, resetting every tile to default visual state.
        /// </summary>
        public void ClearAllOverlays()
        {
            foreach (var kvp in _tileViews)
            {
                kvp.Value.ResetVisualState();
            }
        }

        // --- Mouse Picking ---

        private void HandleMouseHover()
        {
            if (mapData == null || Camera.main == null) return;

            var gridPos = ScreenToGrid(Input.mousePosition);

            if (gridPos != _hoveredTile)
            {
                // Unhighlight previous (only if it's still in Default/Highlighted state)
                if (_hoveredView != null && _hoveredView.CurrentState == TileVisualState.Highlighted)
                    _hoveredView.ResetVisualState();

                _hoveredTile = gridPos;

                if (_tileViews.TryGetValue(gridPos, out var view))
                {
                    // Only highlight if tile is in default state (don't override overlays)
                    if (view.CurrentState == TileVisualState.Default)
                        view.SetVisualState(TileVisualState.Highlighted);

                    _hoveredView = view;
                    OnTileHovered?.Invoke(gridPos);
                }
                else
                {
                    _hoveredView = null;
                }
            }
        }

        private void HandleMouseClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (mapData == null || !mapData.InBounds(_hoveredTile)) return;

            OnTileClicked?.Invoke(_hoveredTile);
        }

        /// <summary>
        /// Convert screen position to grid coordinates, accounting for elevation.
        /// Checks from highest elevation down, returning the first valid tile.
        /// </summary>
        /// <param name="screenPos">Screen-space position (e.g., Input.mousePosition).</param>
        /// <returns>Grid position of the tile under the cursor, or (-1,-1) if none.</returns>
        public Vector2Int ScreenToGrid(Vector3 screenPos)
        {
            if (Camera.main == null) return new Vector2Int(-1, -1);

            var worldPos = Camera.main.ScreenToWorldPoint(screenPos);

            // Check from highest elevation down — first match wins
            for (int elev = GameConstants.MaxElevation; elev >= 0; elev--)
            {
                var gridPos = IsoMath.WorldToGrid(worldPos, elev);

                // Apply inverse rotation if camera is rotated
                if (_rotationIndex != 0)
                {
                    int inverseRot = (4 - _rotationIndex) % 4;
                    gridPos = IsoMath.RotateGrid(gridPos, inverseRot, mapData.Width);
                }

                if (mapData.TryGetTile(gridPos, out var tile) && tile.Elevation == elev)
                    return gridPos;
            }

            // Fallback: try elevation 0
            var fallback = IsoMath.WorldToGrid(worldPos, 0);
            if (_rotationIndex != 0)
            {
                int inverseRot = (4 - _rotationIndex) % 4;
                fallback = IsoMath.RotateGrid(fallback, inverseRot, mapData.Width);
            }
            return mapData.InBounds(fallback) ? fallback : new Vector2Int(-1, -1);
        }

        // --- Camera Rotation Support ---

        /// <summary>
        /// Apply a new camera rotation. Recalculates all tile positions and sorting orders.
        /// </summary>
        /// <param name="rotationIndex">Rotation step (0-3).</param>
        public void ApplyRotation(int rotationIndex)
        {
            _rotationIndex = ((rotationIndex % 4) + 4) % 4;

            foreach (var kvp in _tileViews)
            {
                var originalPos = kvp.Key;
                var view = kvp.Value;

                if (!mapData.TryGetTile(originalPos, out var tileData))
                    continue;

                var rotated = IsoMath.RotateGrid(originalPos, _rotationIndex, mapData.Width);
                var worldPos = IsoMath.GridToWorld(rotated, tileData.Elevation);
                view.transform.position = worldPos;
                view.UpdateSortingOrder(rotated.x, rotated.y);
            }
        }
    }
}
