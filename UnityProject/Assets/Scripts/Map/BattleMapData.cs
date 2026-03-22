using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// Spawn zone defining where a team's units can be placed at battle start.
    /// </summary>
    [System.Serializable]
    public struct SpawnZone
    {
        /// <summary>Team index (0=player, 1=enemy, 2=neutral).</summary>
        public int Team;
        /// <summary>Grid positions available for unit placement.</summary>
        public Vector2Int[] Tiles;
    }

    /// <summary>
    /// ScriptableObject holding all static data for a battle map.
    /// Tiles are stored in a flattened array indexed by [y * Width + x].
    ///
    /// Always use <see cref="TryGetTile"/> for safe access — never index the array directly.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMap", menuName = "IsoRPG/BattleMap")]
    public class BattleMapData : ScriptableObject
    {
        /// <summary>Display name of the map.</summary>
        public string MapName;

        /// <summary>Grid width (columns).</summary>
        public int Width = 8;

        /// <summary>Grid height (rows).</summary>
        public int Height = 8;

        /// <summary>
        /// Flattened tile array. Index: [y * Width + x].
        /// Use <see cref="TryGetTile"/> instead of direct access.
        /// </summary>
        public TileData[] Tiles;

        /// <summary>Spawn zones for each team.</summary>
        public SpawnZone[] SpawnZones;

        /// <summary>
        /// Check if a grid position is within map boundaries.
        /// </summary>
        /// <param name="pos">Grid position to check.</param>
        /// <returns>True if within [0, Width) x [0, Height).</returns>
        public bool InBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }

        /// <summary>
        /// Check if grid coordinates are within map boundaries.
        /// </summary>
        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Try to get tile data at the given grid position.
        /// Returns false if out of bounds — never throws, never returns invalid data.
        /// </summary>
        /// <param name="pos">Grid position.</param>
        /// <param name="tile">The tile data if found.</param>
        /// <returns>True if the position is valid and tile data was returned.</returns>
        public bool TryGetTile(Vector2Int pos, out TileData tile)
        {
            return TryGetTile(pos.x, pos.y, out tile);
        }

        /// <summary>
        /// Try to get tile data at the given grid coordinates.
        /// Returns false if out of bounds.
        /// </summary>
        /// <param name="x">Grid column.</param>
        /// <param name="y">Grid row.</param>
        /// <param name="tile">The tile data if found.</param>
        /// <returns>True if the position is valid and tile data was returned.</returns>
        public bool TryGetTile(int x, int y, out TileData tile)
        {
            if (!InBounds(x, y) || Tiles == null)
            {
                tile = default;
                return false;
            }

            int index = y * Width + x;
            if (index >= Tiles.Length)
            {
                tile = default;
                return false;
            }

            tile = Tiles[index];
            return true;
        }

        /// <summary>
        /// Get the elevation at a grid position. Returns 0 if out of bounds.
        /// </summary>
        public int GetElevation(Vector2Int pos)
        {
            return TryGetTile(pos, out var tile) ? tile.Elevation : 0;
        }
    }
}
