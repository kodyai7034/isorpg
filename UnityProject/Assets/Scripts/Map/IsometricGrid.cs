using System;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// Manages the 3D isometric tile grid. Places cubes in direct 3D space,
    /// handles Physics.Raycast mouse picking, and provides overlay API for
    /// movement/attack range display.
    ///
    /// Blocks are positioned at (gridX, elevation * BlockHeight, gridY) in world space.
    /// No isometric projection math — the orthographic camera at an isometric angle
    /// handles the visual projection. Camera rotation orbits the 3D scene.
    /// </summary>
    public class IsometricGrid : MonoBehaviour
    {
        [SerializeField] private BattleMapData mapData;
        [SerializeField] private GameObject blockPrefab;
        [SerializeField] private Material defaultMaterial;

        private Dictionary<Vector2Int, TileView3D> _tileViews = new();
        private Vector2Int _hoveredTile = new(-1, -1);
        private TileView3D _hoveredView;

        /// <summary>The loaded map data.</summary>
        public BattleMapData MapData => mapData;

        /// <summary>Currently hovered grid position. (-1,-1) if none.</summary>
        public Vector2Int HoveredTile => _hoveredTile;

        // --- Instance Events ---

        /// <summary>Fired when mouse hovers over a new tile.</summary>
        public event Action<Vector2Int> OnTileHovered;

        /// <summary>Fired when a tile is clicked.</summary>
        public event Action<Vector2Int> OnTileClicked;

        /// <summary>Load and render a battle map as 3D blocks.</summary>
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

        private void BuildMap()
        {
            if (mapData == null || mapData.Tiles == null) return;

            _tileViews.Clear();

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    if (!mapData.TryGetTile(x, y, out var tileData))
                        continue;

                    // Direct 3D positioning
                    var worldPos = GridToWorld(x, y, tileData.Elevation);

                    GameObject blockObj;
                    if (blockPrefab != null)
                    {
                        blockObj = Instantiate(blockPrefab, worldPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        blockObj.transform.SetParent(transform);
                        blockObj.transform.position = worldPos;
                    }

                    // Half-height block
                    blockObj.transform.localScale = new Vector3(
                        GridConstants.BlockWidth,
                        GridConstants.BlockHeight,
                        GridConstants.BlockWidth);

                    // Ensure components
                    var view = blockObj.GetComponent<TileView3D>();
                    if (view == null)
                        view = blockObj.AddComponent<TileView3D>();

                    if (blockObj.GetComponent<BoxCollider>() == null)
                        blockObj.AddComponent<BoxCollider>();

                    // Terrain color via MaterialPropertyBlock (placeholder until System 14)
                    var renderer = blockObj.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        if (defaultMaterial != null)
                            renderer.sharedMaterial = defaultMaterial;

                        var terrainColor = GetTerrainColor(tileData.Terrain);
                        var propBlock = new MaterialPropertyBlock();
                        propBlock.SetColor("_Color", terrainColor);      // built-in shader
                        propBlock.SetColor("_BaseColor", terrainColor);  // URP shader
                        renderer.SetPropertyBlock(propBlock);
                    }

                    view.Initialize(tileData);
                    _tileViews[new Vector2Int(x, y)] = view;
                }
            }

            Debug.Log($"[IsometricGrid] Loaded '{mapData.MapName}' ({mapData.Width}x{mapData.Height}, {_tileViews.Count} blocks)");
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

        /// <summary>Get the TileView3D at a grid position.</summary>
        public TileView3D GetTileView(Vector2Int pos)
        {
            _tileViews.TryGetValue(pos, out var view);
            return view;
        }

        /// <summary>Try to get a TileView3D at the given position.</summary>
        public bool TryGetTileView(Vector2Int pos, out TileView3D view)
        {
            return _tileViews.TryGetValue(pos, out view);
        }

        // --- Overlay API ---

        /// <summary>Set visual state on a collection of tiles.</summary>
        public void SetTileOverlay(IEnumerable<Vector2Int> tiles, TileVisualState state)
        {
            if (tiles == null) return;
            foreach (var pos in tiles)
            {
                if (_tileViews.TryGetValue(pos, out var view))
                    view.SetVisualState(state);
            }
        }

        /// <summary>Clear all tile overlays.</summary>
        public void ClearAllOverlays()
        {
            foreach (var kvp in _tileViews)
                kvp.Value.ResetVisualState();
        }

        // --- Mouse Picking (3D Raycast) ---

        private void HandleMouseHover()
        {
            if (mapData == null || Camera.main == null) return;

            var gridPos = ScreenToGrid(Input.mousePosition);

            if (gridPos != _hoveredTile)
            {
                if (_hoveredView != null && _hoveredView.CurrentState == TileVisualState.Highlighted)
                    _hoveredView.ResetVisualState();

                _hoveredTile = gridPos;

                if (_tileViews.TryGetValue(gridPos, out var view))
                {
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
            if (mapData == null || Camera.main == null) return;

            // Direct raycast on click — don't rely on hover state
            var clickGrid = ScreenToGrid(Input.mousePosition);
            if (!mapData.InBounds(clickGrid)) return;

            _hoveredTile = clickGrid;
            OnTileClicked?.Invoke(clickGrid);
        }

        /// <summary>Convert screen position to grid coordinates via Physics.Raycast.</summary>
        public Vector2Int ScreenToGrid(Vector3 screenPos)
        {
            if (Camera.main == null) return new Vector2Int(-1, -1);

            var ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 200f))
            {
                var view = hit.collider.GetComponent<TileView3D>();
                if (view != null)
                    return view.GridPosition;
            }

            return new Vector2Int(-1, -1);
        }

        // --- Coordinate Helpers ---

        /// <summary>Convert grid position to 3D world position (center of block).</summary>
        public static Vector3 GridToWorld(int gridX, int gridY, int elevation)
        {
            return new Vector3(
                gridX * GridConstants.BlockWidth,
                elevation * GridConstants.BlockHeight,
                gridY * GridConstants.BlockWidth);
        }

        /// <summary>Convert grid position to 3D world position.</summary>
        public static Vector3 GridToWorld(Vector2Int grid, int elevation)
        {
            return GridToWorld(grid.x, grid.y, elevation);
        }

        /// <summary>Get world position of block top surface (for placing units on top).</summary>
        public static Vector3 GetBlockTopPosition(Vector2Int grid, int elevation)
        {
            return new Vector3(
                grid.x * GridConstants.BlockWidth,
                elevation * GridConstants.BlockHeight + GridConstants.BlockHeight * 0.5f,
                grid.y * GridConstants.BlockWidth);
        }

        /// <summary>Get center of map in world space (for camera targeting).</summary>
        public Vector3 GetMapCenter()
        {
            if (mapData == null) return Vector3.zero;
            float cx = (mapData.Width - 1) * 0.5f * GridConstants.BlockWidth;
            float cz = (mapData.Height - 1) * 0.5f * GridConstants.BlockWidth;
            return new Vector3(cx, 0, cz);
        }

        // --- Terrain Colors (placeholder until System 14) ---

        private static Color GetTerrainColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Grass => new Color(0.35f, 0.65f, 0.28f),
                TerrainType.Stone => new Color(0.55f, 0.53f, 0.5f),
                TerrainType.Water => new Color(0.2f, 0.45f, 0.75f),
                TerrainType.Sand => new Color(0.85f, 0.78f, 0.55f),
                TerrainType.Lava => new Color(0.85f, 0.25f, 0.1f),
                TerrainType.Forest => new Color(0.2f, 0.45f, 0.18f),
                _ => Color.grey,
            };
        }
    }
}
