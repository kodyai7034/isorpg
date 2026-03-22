using NUnit.Framework;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;

namespace IsoRPG.Tests
{
    public class BattleMapDataTests
    {
        private BattleMapData CreateMap(int width = 4, int height = 4)
        {
            return MapGenerator.CreateFlatMap(width, height);
        }

        [Test]
        public void InBounds_ValidPositions_ReturnTrue()
        {
            var map = CreateMap();
            Assert.IsTrue(map.InBounds(new Vector2Int(0, 0)));
            Assert.IsTrue(map.InBounds(new Vector2Int(3, 3)));
            Assert.IsTrue(map.InBounds(new Vector2Int(2, 1)));
        }

        [Test]
        public void InBounds_InvalidPositions_ReturnFalse()
        {
            var map = CreateMap();
            Assert.IsFalse(map.InBounds(new Vector2Int(-1, 0)));
            Assert.IsFalse(map.InBounds(new Vector2Int(0, -1)));
            Assert.IsFalse(map.InBounds(new Vector2Int(4, 0)));
            Assert.IsFalse(map.InBounds(new Vector2Int(0, 4)));
            Assert.IsFalse(map.InBounds(new Vector2Int(100, 100)));
        }

        [Test]
        public void TryGetTile_ValidPosition_ReturnsTrueWithData()
        {
            var map = CreateMap();
            bool found = map.TryGetTile(new Vector2Int(1, 2), out var tile);
            Assert.IsTrue(found);
            Assert.AreEqual(new Vector2Int(1, 2), tile.Position);
            Assert.AreEqual(TerrainType.Grass, tile.Terrain);
        }

        [Test]
        public void TryGetTile_OutOfBounds_ReturnsFalse()
        {
            var map = CreateMap();
            bool found = map.TryGetTile(new Vector2Int(-1, 0), out _);
            Assert.IsFalse(found);

            found = map.TryGetTile(new Vector2Int(99, 99), out _);
            Assert.IsFalse(found);
        }

        [Test]
        public void TryGetTile_NullTiles_ReturnsFalse()
        {
            var map = ScriptableObject.CreateInstance<BattleMapData>();
            map.Width = 4;
            map.Height = 4;
            map.Tiles = null;

            bool found = map.TryGetTile(new Vector2Int(0, 0), out _);
            Assert.IsFalse(found);
        }

        [Test]
        public void GetElevation_ValidPosition_ReturnsCorrect()
        {
            var map = MapGenerator.CreateTestMap();
            // Center of test map should have elevation > 0
            int centerElev = map.GetElevation(new Vector2Int(map.Width / 2, map.Height / 2));
            Assert.Greater(centerElev, 0);
        }

        [Test]
        public void GetElevation_OutOfBounds_ReturnsZero()
        {
            var map = CreateMap();
            Assert.AreEqual(0, map.GetElevation(new Vector2Int(-1, -1)));
        }
    }

    public class MapGeneratorTests
    {
        [Test]
        public void CreateTestMap_CorrectDimensions()
        {
            var map = MapGenerator.CreateTestMap(10, 12);
            Assert.AreEqual(10, map.Width);
            Assert.AreEqual(12, map.Height);
            Assert.AreEqual(120, map.Tiles.Length);
        }

        [Test]
        public void CreateTestMap_HasSpawnZones()
        {
            var map = MapGenerator.CreateTestMap();
            Assert.IsNotNull(map.SpawnZones);
            Assert.AreEqual(2, map.SpawnZones.Length);
            Assert.AreEqual(0, map.SpawnZones[0].Team);
            Assert.AreEqual(1, map.SpawnZones[1].Team);
            Assert.Greater(map.SpawnZones[0].Tiles.Length, 0);
            Assert.Greater(map.SpawnZones[1].Tiles.Length, 0);
        }

        [Test]
        public void CreateTestMap_HasElevation()
        {
            var map = MapGenerator.CreateTestMap();
            bool hasElevation = false;
            foreach (var tile in map.Tiles)
            {
                if (tile.Elevation > 0)
                {
                    hasElevation = true;
                    break;
                }
            }
            Assert.IsTrue(hasElevation, "Test map should have tiles with elevation > 0");
        }

        [Test]
        public void CreateTestMap_HasTerrainVariety()
        {
            var map = MapGenerator.CreateTestMap();
            var terrains = new System.Collections.Generic.HashSet<TerrainType>();
            foreach (var tile in map.Tiles)
                terrains.Add(tile.Terrain);

            Assert.Greater(terrains.Count, 1, "Test map should have multiple terrain types");
        }

        [Test]
        public void CreateFlatMap_AllTilesUniform()
        {
            var map = MapGenerator.CreateFlatMap(5, 5, TerrainType.Stone);
            Assert.AreEqual(25, map.Tiles.Length);
            foreach (var tile in map.Tiles)
            {
                Assert.AreEqual(TerrainType.Stone, tile.Terrain);
                Assert.AreEqual(0, tile.Elevation);
            }
        }

        [Test]
        public void CreateFlatMap_AllTilesHaveCorrectPositions()
        {
            var map = MapGenerator.CreateFlatMap(3, 3);
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Assert.IsTrue(map.TryGetTile(x, y, out var tile));
                    Assert.AreEqual(new Vector2Int(x, y), tile.Position);
                }
            }
        }
    }
}
