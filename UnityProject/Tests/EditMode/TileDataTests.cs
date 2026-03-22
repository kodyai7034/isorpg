using NUnit.Framework;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;

namespace IsoRPG.Tests
{
    public class TileDataTests
    {
        [Test]
        public void Create_Grass_DefaultsCorrect()
        {
            var tile = TileData.Create(3, 5, 0, TerrainType.Grass);
            Assert.AreEqual(new Vector2Int(3, 5), tile.Position);
            Assert.AreEqual(0, tile.Elevation);
            Assert.AreEqual(TerrainType.Grass, tile.Terrain);
            Assert.IsTrue(tile.Walkable);
            Assert.AreEqual(1, tile.MoveCost);
            Assert.AreEqual(0, tile.Cover);
            Assert.AreEqual(HazardType.None, tile.Hazard);
        }

        [Test]
        public void Create_Water_IsImpassable()
        {
            var tile = TileData.Create(0, 0, 0, TerrainType.Water);
            Assert.IsFalse(tile.Walkable);
            Assert.AreEqual(GameConstants.ImpassableMoveCost, tile.MoveCost);
        }

        [Test]
        public void Create_Lava_HasFireHazard()
        {
            var tile = TileData.Create(0, 0, 0, TerrainType.Lava);
            Assert.IsFalse(tile.Walkable);
            Assert.AreEqual(HazardType.Fire, tile.Hazard);
        }

        [Test]
        public void Create_Forest_HasIncreasedMoveCostAndCover()
        {
            var tile = TileData.Create(0, 0, 0, TerrainType.Forest);
            Assert.IsTrue(tile.Walkable);
            Assert.AreEqual(2, tile.MoveCost);
            Assert.AreEqual(1, tile.Cover);
        }

        [Test]
        public void Create_ElevationClamped()
        {
            var high = TileData.Create(0, 0, 999);
            Assert.AreEqual(GameConstants.MaxElevation, high.Elevation);

            var low = TileData.Create(0, 0, -5);
            Assert.AreEqual(0, low.Elevation);
        }

        [Test]
        public void Create_Stone_Walkable()
        {
            var tile = TileData.Create(0, 0, 0, TerrainType.Stone);
            Assert.IsTrue(tile.Walkable);
            Assert.AreEqual(1, tile.MoveCost);
        }

        [Test]
        public void Create_Sand_Walkable()
        {
            var tile = TileData.Create(0, 0, 0, TerrainType.Sand);
            Assert.IsTrue(tile.Walkable);
            Assert.AreEqual(1, tile.MoveCost);
        }
    }
}
