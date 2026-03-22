using UnityEngine;

namespace IsoRPG.Core
{
    /// <summary>
    /// Pure static utilities for isometric grid math.
    /// All methods are side-effect-free. Tile ratio is 2:1 (width:height).
    ///
    /// Coordinate systems:
    /// - Grid: integer (x, y) tile position + integer elevation
    /// - World: Unity world-space Vector3 used for rendering
    ///
    /// Example: GridToWorld(2, 3, elevation: 1) → world position offset from origin.
    /// </summary>
    public static class IsoMath
    {
        /// <summary>Full tile width in world units.</summary>
        public const float TileWidth = 1f;

        /// <summary>Full tile height in world units (2:1 ratio).</summary>
        public const float TileHeight = 0.5f;

        /// <summary>Half tile width. Used in projection formula.</summary>
        public const float TileWidthHalf = TileWidth / 2f;

        /// <summary>Half tile height. Used in projection formula.</summary>
        public const float TileHeightHalf = TileHeight / 2f;

        /// <summary>World-space Y offset per elevation level.</summary>
        public const float ElevationHeight = 0.25f;

        private static readonly Vector2Int[] DirectionVectors =
        {
            new(0, 1),   // South
            new(-1, 1),  // SouthWest
            new(-1, 0),  // West
            new(-1, -1), // NorthWest
            new(0, -1),  // North
            new(1, -1),  // NorthEast
            new(1, 0),   // East
            new(1, 1),   // SouthEast
        };

        /// <summary>
        /// Convert grid coordinates and elevation to a world-space position.
        /// </summary>
        /// <param name="gridX">Grid column.</param>
        /// <param name="gridY">Grid row.</param>
        /// <param name="elevation">Tile elevation (0 to <see cref="GameConstants.MaxElevation"/>).</param>
        /// <returns>World-space position for rendering.</returns>
        /// <example>GridToWorld(0, 0, 0) returns Vector3.zero.</example>
        public static Vector3 GridToWorld(int gridX, int gridY, int elevation = 0)
        {
            float x = (gridX - gridY) * TileWidthHalf;
            float y = (gridX + gridY) * TileHeightHalf + elevation * ElevationHeight;
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Convert grid coordinates and elevation to a world-space position.
        /// </summary>
        /// <param name="grid">Grid position.</param>
        /// <param name="elevation">Tile elevation.</param>
        /// <returns>World-space position for rendering.</returns>
        public static Vector3 GridToWorld(Vector2Int grid, int elevation = 0)
        {
            return GridToWorld(grid.x, grid.y, elevation);
        }

        /// <summary>
        /// Convert a world-space position back to grid coordinates.
        /// Requires knowing the elevation to produce correct results.
        /// </summary>
        /// <param name="worldPos">World-space position (e.g., from mouse click).</param>
        /// <param name="assumedElevation">The elevation to assume for the conversion.</param>
        /// <returns>Nearest grid position.</returns>
        public static Vector2Int WorldToGrid(Vector3 worldPos, int assumedElevation = 0)
        {
            float adjustedY = worldPos.y - assumedElevation * ElevationHeight;
            float gx = (worldPos.x / TileWidthHalf + adjustedY / TileHeightHalf) / 2f;
            float gy = (adjustedY / TileHeightHalf - worldPos.x / TileWidthHalf) / 2f;
            return new Vector2Int(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy));
        }

        /// <summary>
        /// Calculate sprite sorting order for correct isometric draw order.
        /// Higher elevation and deeper tiles (higher x+y) draw on top.
        /// </summary>
        /// <param name="gridX">Grid column.</param>
        /// <param name="gridY">Grid row.</param>
        /// <param name="elevation">Tile elevation.</param>
        /// <param name="maxDepth">Maximum depth value per elevation layer. Default 100.</param>
        /// <returns>Sorting order integer for Unity's SpriteRenderer.</returns>
        public static int CalculateSortingOrder(int gridX, int gridY, int elevation,
            int maxDepth = GameConstants.DefaultMaxSortDepth)
        {
            return elevation * maxDepth + (gridX + gridY);
        }

        /// <summary>
        /// Manhattan distance between two grid positions.
        /// Used for ability range and movement cost calculations.
        /// </summary>
        /// <param name="a">First grid position.</param>
        /// <param name="b">Second grid position.</param>
        /// <returns>Non-negative integer distance.</returns>
        /// <example>ManhattanDistance((0,0), (2,3)) returns 5.</example>
        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// Determine the facing direction from one grid position to another.
        /// Returns <see cref="Direction.South"/> if both positions are the same.
        /// </summary>
        /// <param name="from">Origin grid position.</param>
        /// <param name="to">Target grid position.</param>
        /// <returns>The closest cardinal or intercardinal direction.</returns>
        public static Direction GetDirection(Vector2Int from, Vector2Int to)
        {
            if (from == to)
                return Direction.South;

            float dx = to.x - from.x;
            float dy = to.y - from.y;

            // Angle in degrees, 0 = east, counter-clockwise
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Map to 8 directions: each sector is 45 degrees
            // Offset by 22.5 so sector boundaries fall between directions
            int sector = Mathf.RoundToInt(angle / 45f) % 8;

            // Map angle sectors to our Direction enum
            // Atan2 sectors (0°=East going CCW): E=0, NE=1, N=2, NW=3, W=4, SW=5, S=6, SE=7
            // Our Direction enum: S=0, SW=1, W=2, NW=3, N=4, NE=5, E=6, SE=7
            return sector switch
            {
                0 => Direction.East,
                1 => Direction.NorthEast,
                2 => Direction.North,
                3 => Direction.NorthWest,
                4 => Direction.West,
                5 => Direction.SouthWest,
                6 => Direction.South,
                7 => Direction.SouthEast,
                _ => Direction.South,
            };
        }

        /// <summary>
        /// Rotate a grid position for camera rotation.
        /// Rotates 90° clockwise per rotation index (0-3).
        /// </summary>
        /// <param name="grid">Original grid position.</param>
        /// <param name="rotationIndex">Rotation step (0 = no rotation, 1 = 90° CW, 2 = 180°, 3 = 270° CW).</param>
        /// <param name="mapSize">Map dimension (assumes square map). Used to keep coordinates positive.</param>
        /// <returns>Rotated grid position.</returns>
        public static Vector2Int RotateGrid(Vector2Int grid, int rotationIndex, int mapSize)
        {
            int normalized = ((rotationIndex % 4) + 4) % 4; // handle negatives
            int x = grid.x;
            int y = grid.y;
            int max = mapSize - 1;

            return normalized switch
            {
                0 => new Vector2Int(x, y),
                1 => new Vector2Int(max - y, x),
                2 => new Vector2Int(max - x, max - y),
                3 => new Vector2Int(y, max - x),
                _ => new Vector2Int(x, y),
            };
        }

        /// <summary>
        /// Get the grid offset vector for a given direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>A Vector2Int offset to add to a grid position.</returns>
        public static Vector2Int GetDirectionVector(Direction direction)
        {
            return DirectionVectors[(int)direction];
        }
    }
}
