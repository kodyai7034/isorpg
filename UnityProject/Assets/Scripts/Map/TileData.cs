using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// Immutable data describing a single grid tile's properties.
    /// Created via <see cref="Create"/> factory method which applies terrain defaults.
    /// </summary>
    [System.Serializable]
    public struct TileData
    {
        /// <summary>Grid position (column, row).</summary>
        public Vector2Int Position;

        /// <summary>Height level. Range: 0 to <see cref="GameConstants.MaxElevation"/>.</summary>
        public int Elevation;

        /// <summary>Surface type. Determines default walkability, move cost, and visuals.</summary>
        public TerrainType Terrain;

        /// <summary>Whether units can occupy this tile.</summary>
        public bool Walkable;

        /// <summary>Movement points consumed when traversing. 1=normal, 2=difficult, 999=impassable.</summary>
        public int MoveCost;

        /// <summary>Defense bonus granted to occupying unit (0-2).</summary>
        public int Cover;

        /// <summary>Environmental hazard applied to occupying units.</summary>
        public HazardType Hazard;

        /// <summary>
        /// Create a tile with terrain-appropriate defaults applied.
        /// </summary>
        /// <param name="x">Grid column.</param>
        /// <param name="y">Grid row.</param>
        /// <param name="elevation">Height level (clamped to 0-MaxElevation).</param>
        /// <param name="terrain">Surface type — determines walkability, move cost, and hazard defaults.</param>
        /// <returns>A fully initialized TileData with terrain defaults.</returns>
        public static TileData Create(int x, int y, int elevation = 0,
            TerrainType terrain = TerrainType.Grass)
        {
            int clampedElevation = Mathf.Clamp(elevation, 0, GameConstants.MaxElevation);

            return terrain switch
            {
                TerrainType.Grass => new TileData
                {
                    Position = new Vector2Int(x, y), Elevation = clampedElevation,
                    Terrain = terrain, Walkable = true, MoveCost = 1, Cover = 0, Hazard = HazardType.None
                },
                TerrainType.Stone => new TileData
                {
                    Position = new Vector2Int(x, y), Elevation = clampedElevation,
                    Terrain = terrain, Walkable = true, MoveCost = 1, Cover = 0, Hazard = HazardType.None
                },
                TerrainType.Water => new TileData
                {
                    Position = new Vector2Int(x, y), Elevation = clampedElevation,
                    Terrain = terrain, Walkable = false, MoveCost = GameConstants.ImpassableMoveCost,
                    Cover = 0, Hazard = HazardType.None
                },
                TerrainType.Sand => new TileData
                {
                    Position = new Vector2Int(x, y), Elevation = clampedElevation,
                    Terrain = terrain, Walkable = true, MoveCost = 1, Cover = 0, Hazard = HazardType.None
                },
                TerrainType.Lava => new TileData
                {
                    Position = new Vector2Int(x, y), Elevation = clampedElevation,
                    Terrain = terrain, Walkable = false, MoveCost = GameConstants.ImpassableMoveCost,
                    Cover = 0, Hazard = HazardType.Fire
                },
                TerrainType.Forest => new TileData
                {
                    Position = new Vector2Int(x, y), Elevation = clampedElevation,
                    Terrain = terrain, Walkable = true, MoveCost = 2, Cover = 1, Hazard = HazardType.None
                },
                _ => new TileData
                {
                    Position = new Vector2Int(x, y), Elevation = clampedElevation,
                    Terrain = terrain, Walkable = true, MoveCost = 1, Cover = 0, Hazard = HazardType.None
                },
            };
        }
    }
}
