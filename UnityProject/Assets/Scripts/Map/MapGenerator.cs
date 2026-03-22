using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// Generates test <see cref="BattleMapData"/> instances at runtime for prototyping
    /// without requiring the Unity editor's ScriptableObject workflow.
    /// </summary>
    public static class MapGenerator
    {
        /// <summary>
        /// Create a test map with elevation, mixed terrain, and spawn zones.
        /// Features: center raised platform, steps, water corners, stone path, forest edges.
        /// </summary>
        /// <param name="width">Map width in tiles.</param>
        /// <param name="height">Map height in tiles.</param>
        /// <returns>A populated BattleMapData ScriptableObject instance.</returns>
        public static BattleMapData CreateTestMap(int width = 8, int height = 8)
        {
            var map = ScriptableObject.CreateInstance<BattleMapData>();
            map.MapName = "Test Arena";
            map.Width = width;
            map.Height = height;
            map.Tiles = new TileData[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int elevation = 0;
                    var terrain = TerrainType.Grass;

                    // Center raised platform (2x2)
                    if (x >= width / 2 - 1 && x <= width / 2 && y >= height / 2 - 1 && y <= height / 2)
                        elevation = 2;
                    // Steps around center (4x4)
                    else if (x >= width / 2 - 2 && x <= width / 2 + 1 && y >= height / 2 - 2 && y <= height / 2 + 1)
                        elevation = 1;

                    // Water in opposite corners
                    if ((x == 0 && y == 0) || (x == width - 1 && y == height - 1))
                        terrain = TerrainType.Water;

                    // Stone path through middle columns
                    if ((x == width / 2 - 1 || x == width / 2) && elevation == 0)
                        terrain = TerrainType.Stone;

                    // Forest on left and right edges
                    if ((x == 0 || x == width - 1) && terrain == TerrainType.Grass)
                        terrain = TerrainType.Forest;

                    map.Tiles[y * width + x] = TileData.Create(x, y, elevation, terrain);
                }
            }

            map.SpawnZones = new[]
            {
                new SpawnZone
                {
                    Team = 0,
                    Tiles = new[]
                    {
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
                    }
                },
                new SpawnZone
                {
                    Team = 1,
                    Tiles = new[]
                    {
                        new Vector2Int(width - 2, height - 1),
                        new Vector2Int(width - 1, height - 2),
                        new Vector2Int(width - 2, height - 2)
                    }
                }
            };

            return map;
        }

        /// <summary>
        /// Create a flat uniform map. Useful for unit tests where terrain variety is not needed.
        /// </summary>
        /// <param name="width">Map width.</param>
        /// <param name="height">Map height.</param>
        /// <param name="terrain">Uniform terrain type for all tiles.</param>
        /// <returns>A flat BattleMapData instance.</returns>
        public static BattleMapData CreateFlatMap(int width, int height,
            TerrainType terrain = TerrainType.Grass)
        {
            var map = ScriptableObject.CreateInstance<BattleMapData>();
            map.MapName = "Flat Map";
            map.Width = width;
            map.Height = height;
            map.Tiles = new TileData[width * height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map.Tiles[y * width + x] = TileData.Create(x, y, 0, terrain);

            map.SpawnZones = System.Array.Empty<SpawnZone>();
            return map;
        }
    }
}
