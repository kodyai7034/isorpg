using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// 3D block tile view. Uses MeshRenderer with MaterialPropertyBlock for
    /// per-instance color tinting. BoxCollider enables Physics.Raycast picking.
    ///
    /// Visual states (hover, movement range, attack range) multiply a tint
    /// with the base terrain color so highlighting preserves terrain identity.
    /// Supports both built-in shader (_Color) and URP Lit shader (_BaseColor).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(BoxCollider))]
    public class TileView3D : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private TileData _data;
        private TileVisualState _currentState = TileVisualState.Default;
        private Color _baseTerrainColor = Color.white;

        // Support both built-in and URP shader property names
        private static readonly int ColorPropBuiltIn = Shader.PropertyToID("_Color");
        private static readonly int ColorPropURP = Shader.PropertyToID("_BaseColor");

        /// <summary>The tile data this view represents.</summary>
        public TileData Data => _data;

        /// <summary>Grid position shortcut.</summary>
        public Vector2Int GridPosition => _data.Position;

        /// <summary>Current visual state.</summary>
        public TileVisualState CurrentState => _currentState;

        /// <summary>
        /// Initialize the tile view with data and terrain color.
        /// </summary>
        /// <param name="data">Tile data (position, elevation, terrain).</param>
        /// <param name="material">Material to apply. Null keeps existing.</param>
        public void Initialize(TileData data, Material material = null)
        {
            _renderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
            _data = data;

            if (material != null)
                _renderer.sharedMaterial = material;

            // Capture the terrain color that was set by IsometricGrid
            _renderer.GetPropertyBlock(_propBlock);
            _baseTerrainColor = _propBlock.GetColor(ColorPropBuiltIn);
            if (_baseTerrainColor == Color.clear)
                _baseTerrainColor = _propBlock.GetColor(ColorPropURP);
            if (_baseTerrainColor == Color.clear)
                _baseTerrainColor = Color.grey;

            gameObject.name = $"Block_{data.Position.x}_{data.Position.y}_E{data.Elevation}";
        }

        /// <summary>
        /// Set the visual state. Multiplies tint with base terrain color
        /// so highlighting preserves terrain identity.
        /// </summary>
        public void SetVisualState(TileVisualState state)
        {
            _currentState = state;
            if (_renderer == null) return;

            Color tint = state switch
            {
                TileVisualState.Default => Color.white,
                TileVisualState.Highlighted => new Color(1.3f, 1.3f, 0.8f, 1f),
                TileVisualState.Selected => new Color(1.5f, 1.5f, 0.5f, 1f),
                TileVisualState.MoveRange => new Color(0.6f, 0.8f, 1.5f, 1f),
                TileVisualState.AttackRange => new Color(1.5f, 0.5f, 0.5f, 1f),
                TileVisualState.HazardWarning => new Color(1.5f, 0.8f, 0.3f, 1f),
                TileVisualState.PathPreview => new Color(0.5f, 1f, 1.5f, 1f),
                _ => Color.white,
            };

            // Multiply terrain color by tint — preserves terrain identity while showing state
            Color finalColor = _baseTerrainColor * tint;
            finalColor.a = 1f;

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorPropBuiltIn, finalColor);
            _propBlock.SetColor(ColorPropURP, finalColor);
            _renderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>Reset to base terrain color.</summary>
        public void ResetVisualState()
        {
            SetVisualState(TileVisualState.Default);
        }
    }
}
