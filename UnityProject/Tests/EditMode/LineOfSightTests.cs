using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Map;

namespace IsoRPG.Tests
{
    public class LineOfSightTests
    {
        [Test]
        public void SameTile_AlwaysHasLoS()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
            Assert.IsTrue(LineOfSight.HasLineOfSight(map, new Vector2Int(2, 2), new Vector2Int(2, 2)));
        }

        [Test]
        public void AdjacentTiles_AlwaysHaveLoS()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
            // Even with a wall between, adjacent tiles always have LoS
            map.Tiles[2 * 5 + 2] = TileData.Create(2, 2, 10, TerrainType.Stone);

            Assert.IsTrue(LineOfSight.HasLineOfSight(map, new Vector2Int(1, 2), new Vector2Int(2, 2)));
            Assert.IsTrue(LineOfSight.HasLineOfSight(map, new Vector2Int(2, 2), new Vector2Int(3, 2)));
        }

        [Test]
        public void FlatMap_AlwaysHasLoS()
        {
            var map = MapGenerator.CreateFlatMap(10, 10);
            Assert.IsTrue(LineOfSight.HasLineOfSight(map, new Vector2Int(0, 0), new Vector2Int(9, 9)));
            Assert.IsTrue(LineOfSight.HasLineOfSight(map, new Vector2Int(0, 5), new Vector2Int(9, 5)));
        }

        [Test]
        public void HighWall_BlocksLoS()
        {
            var map = MapGenerator.CreateFlatMap(7, 7);
            // Create a wall at y=3 with elevation 5
            for (int x = 0; x < 7; x++)
                map.Tiles[3 * 7 + x] = TileData.Create(x, 3, 5, TerrainType.Stone);

            // Units on flat ground (elev 0) on either side
            Assert.IsFalse(LineOfSight.HasLineOfSight(map, new Vector2Int(3, 0), new Vector2Int(3, 6)));
        }

        [Test]
        public void HighToHigh_OverWall_HasLoS()
        {
            var map = MapGenerator.CreateFlatMap(7, 7);
            // Wall at y=3, elevation 3
            for (int x = 0; x < 7; x++)
                map.Tiles[3 * 7 + x] = TileData.Create(x, 3, 3, TerrainType.Stone);

            // Source at elevation 5, target at elevation 5 — wall at 3 doesn't block
            map.Tiles[0 * 7 + 3] = TileData.Create(3, 0, 5, TerrainType.Stone);
            map.Tiles[6 * 7 + 3] = TileData.Create(3, 6, 5, TerrainType.Stone);

            Assert.IsTrue(LineOfSight.HasLineOfSight(map, new Vector2Int(3, 0), new Vector2Int(3, 6)));
        }

        [Test]
        public void GetTargetableTiles_RespectsRange()
        {
            var map = MapGenerator.CreateFlatMap(10, 10);
            var tiles = LineOfSight.GetTargetableTiles(map, new Vector2Int(5, 5), 2, false);

            // All tiles within Manhattan distance 2
            foreach (var tile in tiles)
            {
                Assert.LessOrEqual(
                    IsoMath.ManhattanDistance(new Vector2Int(5, 5), tile), 2);
            }

            // Count: center + 4*1 + 4*2 = 1 + 4 + 8 = 13
            Assert.AreEqual(13, tiles.Count);
        }

        [Test]
        public void GetTargetableTiles_WithLoS_ExcludesBlocked()
        {
            var map = MapGenerator.CreateFlatMap(10, 10);
            // Wall blocking some tiles
            map.Tiles[5 * 10 + 6] = TileData.Create(6, 5, 10, TerrainType.Stone);

            var withLoS = LineOfSight.GetTargetableTiles(map, new Vector2Int(5, 5), 3, true);
            var withoutLoS = LineOfSight.GetTargetableTiles(map, new Vector2Int(5, 5), 3, false);

            Assert.LessOrEqual(withLoS.Count, withoutLoS.Count);
        }

        [Test]
        public void BresenhamLine_StraightHorizontal()
        {
            var line = LineOfSight.GetBresenhamLine(new Vector2Int(0, 0), new Vector2Int(4, 0));
            Assert.AreEqual(5, line.Count); // 0,1,2,3,4
            Assert.AreEqual(new Vector2Int(0, 0), line[0]);
            Assert.AreEqual(new Vector2Int(4, 0), line[4]);
        }

        [Test]
        public void BresenhamLine_Diagonal()
        {
            var line = LineOfSight.GetBresenhamLine(new Vector2Int(0, 0), new Vector2Int(3, 3));
            Assert.AreEqual(new Vector2Int(0, 0), line[0]);
            Assert.AreEqual(new Vector2Int(3, 3), line[line.Count - 1]);
        }
    }
}
