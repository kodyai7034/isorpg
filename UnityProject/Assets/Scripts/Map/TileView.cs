using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// Visual states for tile highlighting. Each state has a distinct color tint.
    /// </summary>
    public enum TileVisualState
    {
        /// <summary>Normal terrain-colored appearance.</summary>
        Default,
        /// <summary>Mouse hover — light yellow tint.</summary>
        Highlighted,
        /// <summary>Active selection — bright yellow.</summary>
        Selected,
        /// <summary>Reachable by unit movement — blue tint.</summary>
        MoveRange,
        /// <summary>Targetable by ability — red tint.</summary>
        AttackRange,
        /// <summary>Hazardous tile — pulsing orange/red.</summary>
        HazardWarning,
        /// <summary>Path preview — bright blue trail showing planned movement path.</summary>
        PathPreview
    }

    /// <summary>
    /// MonoBehaviour for a single tile's visual representation.
    /// Manages sprite rendering, sorting order, and visual state (highlight, overlay).
    ///
    /// Contains NO game logic — purely visual. Reads data from <see cref="TileData"/>.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TileView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private TileData _data;
        private TileVisualState _currentState = TileVisualState.Default;
        private Color _baseColor;

        /// <summary>The tile data this view represents.</summary>
        public TileData Data => _data;

        /// <summary>Grid position shortcut.</summary>
        public Vector2Int GridPosition => _data.Position;

        /// <summary>Current visual state.</summary>
        public TileVisualState CurrentState => _currentState;

        /// <summary>
        /// Initialize the tile view with data and sprite.
        /// Called by <see cref="IsometricGrid"/> during map construction.
        /// </summary>
        /// <param name="data">Tile data (position, elevation, terrain).</param>
        /// <param name="sprite">Sprite to render. Null uses existing sprite.</param>
        public void Initialize(TileData data, Sprite sprite = null)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _data = data;

            if (sprite != null)
                _renderer.sprite = sprite;

            _baseColor = GetTerrainColor(data.Terrain);
            _renderer.color = _baseColor;
            _renderer.sortingOrder = IsoMath.CalculateSortingOrder(
                data.Position.x, data.Position.y, data.Elevation);

            transform.position = IsoMath.GridToWorld(data.Position, data.Elevation);
            gameObject.name = $"Tile_{data.Position.x}_{data.Position.y}_E{data.Elevation}";
        }

        /// <summary>
        /// Set the visual state, applying the corresponding color tint.
        /// </summary>
        /// <param name="state">The visual state to apply.</param>
        public void SetVisualState(TileVisualState state)
        {
            _currentState = state;
            if (_renderer == null) return;

            _renderer.color = state switch
            {
                TileVisualState.Default => _baseColor,
                TileVisualState.Highlighted => new Color(1f, 1f, 0.6f, 0.9f),
                TileVisualState.Selected => new Color(1f, 1f, 0.2f, 1f),
                TileVisualState.MoveRange => new Color(0.4f, 0.6f, 1f, 0.7f),
                TileVisualState.AttackRange => new Color(1f, 0.3f, 0.3f, 0.7f),
                TileVisualState.HazardWarning => new Color(1f, 0.5f, 0.1f, 0.8f),
                TileVisualState.PathPreview => new Color(0.3f, 0.7f, 1f, 0.9f),
                _ => _baseColor,
            };
        }

        /// <summary>
        /// Reset to default terrain color.
        /// </summary>
        public void ResetVisualState()
        {
            SetVisualState(TileVisualState.Default);
        }

        /// <summary>
        /// Update sorting order (called after camera rotation).
        /// </summary>
        /// <param name="rotatedX">Rotated grid X.</param>
        /// <param name="rotatedY">Rotated grid Y.</param>
        public void UpdateSortingOrder(int rotatedX, int rotatedY)
        {
            if (_renderer != null)
                _renderer.sortingOrder = IsoMath.CalculateSortingOrder(
                    rotatedX, rotatedY, _data.Elevation);
        }

        /// <summary>
        /// Get the default color for a terrain type.
        /// Used as placeholder until PixelLab-generated tile sprites are available.
        /// </summary>
        public static Color GetTerrainColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Grass => new Color(0.4f, 0.72f, 0.32f),
                TerrainType.Stone => new Color(0.62f, 0.6f, 0.58f),
                TerrainType.Water => new Color(0.25f, 0.5f, 0.82f),
                TerrainType.Sand => new Color(0.92f, 0.85f, 0.6f),
                TerrainType.Lava => new Color(0.92f, 0.28f, 0.12f),
                TerrainType.Forest => new Color(0.22f, 0.52f, 0.22f),
                _ => Color.white,
            };
        }
    }
}
