using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Line of sight calculations on the isometric grid.
    /// Uses Bresenham's line algorithm to trace between grid positions.
    /// LoS is blocked if any intermediate tile's elevation exceeds both
    /// the source and target elevation.
    /// </summary>
    public static class LineOfSight
    {
        /// <summary>
        /// Check if a unit at 'from' can see 'to' on the given map.
        /// Adjacent tiles (Manhattan distance 1) always have LoS.
        /// Same tile always has LoS.
        /// </summary>
        /// <param name="map">Battle map data.</param>
        /// <param name="from">Source grid position.</param>
        /// <param name="to">Target grid position.</param>
        /// <returns>True if line of sight exists.</returns>
        public static bool HasLineOfSight(BattleMapData map, Vector2Int from, Vector2Int to)
        {
            if (from == to) return true;
            if (IsoMath.ManhattanDistance(from, to) <= 1) return true;

            int fromElev = map.GetElevation(from);
            int toElev = map.GetElevation(to);
            int maxElev = Mathf.Max(fromElev, toElev);

            // Bresenham line between from and to
            var line = GetBresenhamLine(from, to);

            // Check intermediate tiles (skip first and last)
            for (int i = 1; i < line.Count - 1; i++)
            {
                var pos = line[i];
                if (!map.TryGetTile(pos, out var tile))
                    continue;

                // If intermediate tile is higher than both source and target, LoS blocked
                if (tile.Elevation > maxElev)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get all tiles within range that have line of sight from the caster.
        /// </summary>
        /// <param name="map">Battle map data.</param>
        /// <param name="casterPos">Caster's grid position.</param>
        /// <param name="range">Maximum range in tiles (Manhattan distance).</param>
        /// <param name="requiresLoS">Whether to filter by line of sight.</param>
        /// <returns>List of targetable grid positions.</returns>
        public static List<Vector2Int> GetTargetableTiles(BattleMapData map,
            Vector2Int casterPos, int range, bool requiresLoS)
        {
            var result = new List<Vector2Int>();

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (IsoMath.ManhattanDistance(casterPos, pos) > range)
                        continue;

                    if (requiresLoS && !HasLineOfSight(map, casterPos, pos))
                        continue;

                    result.Add(pos);
                }
            }

            return result;
        }

        /// <summary>
        /// Bresenham's line algorithm on integer grid.
        /// Returns all grid positions along the line from start to end (inclusive).
        /// </summary>
        public static List<Vector2Int> GetBresenhamLine(Vector2Int start, Vector2Int end)
        {
            var line = new List<Vector2Int>();

            int x0 = start.x, y0 = start.y;
            int x1 = end.x, y1 = end.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int safety = 1000;
            while (safety-- > 0)
            {
                line.Add(new Vector2Int(x0, y0));

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return line;
        }
    }
}
