using UnityEngine;

namespace IsoRPG.Core
{
    /// <summary>
    /// Isometric math utilities for grid-to-world and world-to-grid conversions.
    /// Tile ratio: 2:1 (e.g., 64x32 pixels).
    /// </summary>
    public static class IsoMath
    {
        public const float TileWidth = 1f;
        public const float TileHeight = 0.5f;
        public const float TileWidthHalf = TileWidth / 2f;
        public const float TileHeightHalf = TileHeight / 2f;
        public const float ElevationHeight = 0.25f;

        public static Vector3 GridToWorld(int gridX, int gridY, int elevation = 0)
        {
            float x = (gridX - gridY) * TileWidthHalf;
            float y = (gridX + gridY) * TileHeightHalf + elevation * ElevationHeight;
            return new Vector3(x, y, 0f);
        }

        public static Vector3 GridToWorld(Vector2Int grid, int elevation = 0)
        {
            return GridToWorld(grid.x, grid.y, elevation);
        }

        public static Vector2Int WorldToGrid(Vector3 worldPos, int assumedElevation = 0)
        {
            float adjustedY = worldPos.y - assumedElevation * ElevationHeight;
            float gx = (worldPos.x / TileWidthHalf + adjustedY / TileHeightHalf) / 2f;
            float gy = (adjustedY / TileHeightHalf - worldPos.x / TileWidthHalf) / 2f;
            return new Vector2Int(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy));
        }

        public static int CalculateSortingOrder(int gridX, int gridY, int elevation, int maxDepth = 100)
        {
            return elevation * maxDepth + (gridX + gridY);
        }

        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
