using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    /// <summary>
    /// Generates a test BattleMapData ScriptableObject at runtime for prototyping.
    /// </summary>
    public static class MapGenerator
    {
        public static BattleMapData CreateTestMap(int width = 8, int height = 8)
        {
            var map = ScriptableObject.CreateInstance<BattleMapData>();
            map.mapName = "Test Arena";
            map.width = width;
            map.height = height;
            map.tiles = new TileData[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int elevation = 0;
                    var terrain = TerrainType.Grass;

                    // Center raised platform
                    if (x >= 3 && x <= 4 && y >= 3 && y <= 4)
                        elevation = 2;
                    // Steps around center
                    else if (x >= 2 && x <= 5 && y >= 2 && y <= 5)
                        elevation = 1;

                    // Water in corners
                    if ((x == 0 && y == 0) || (x == width - 1 && y == height - 1))
                        terrain = TerrainType.Water;

                    // Stone path
                    if ((x == 3 || x == 4) && elevation == 0)
                        terrain = TerrainType.Stone;

                    // Forest edges
                    if ((x == 0 || x == width - 1) && terrain != TerrainType.Water)
                        terrain = TerrainType.Forest;

                    map.tiles[y * width + x] = new TileData(x, y, elevation, terrain);
                }
            }

            map.spawnZones = new[]
            {
                new SpawnZone
                {
                    team = 0,
                    tiles = new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) }
                },
                new SpawnZone
                {
                    team = 1,
                    tiles = new[] { new Vector2Int(6, 6), new Vector2Int(7, 6), new Vector2Int(6, 7) }
                }
            };

            return map;
        }
    }
}
