using NUnit.Framework;
using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Tests
{
    public class IsoMathTests
    {
        [Test]
        public void GridToWorld_Origin_ReturnsZero()
        {
            var result = IsoMath.GridToWorld(0, 0);
            Assert.AreEqual(0f, result.x, 0.001f);
            Assert.AreEqual(0f, result.y, 0.001f);
        }

        [Test]
        public void GridToWorld_WithElevation_OffsetsY()
        {
            var noElev = IsoMath.GridToWorld(0, 0, 0);
            var withElev = IsoMath.GridToWorld(0, 0, 2);
            Assert.Greater(withElev.y, noElev.y);
            Assert.AreEqual(IsoMath.ElevationHeight * 2, withElev.y - noElev.y, 0.001f);
        }

        [Test]
        public void WorldToGrid_RoundTrip()
        {
            var original = new Vector2Int(3, 5);
            var world = IsoMath.GridToWorld(original);
            var result = IsoMath.WorldToGrid(world);
            Assert.AreEqual(original, result);
        }

        [Test]
        public void ManhattanDistance_CorrectValues()
        {
            Assert.AreEqual(0, IsoMath.ManhattanDistance(new Vector2Int(0, 0), new Vector2Int(0, 0)));
            Assert.AreEqual(2, IsoMath.ManhattanDistance(new Vector2Int(0, 0), new Vector2Int(1, 1)));
            Assert.AreEqual(5, IsoMath.ManhattanDistance(new Vector2Int(1, 2), new Vector2Int(4, 4)));
        }

        [Test]
        public void SortingOrder_HigherElevation_HigherOrder()
        {
            int low = IsoMath.CalculateSortingOrder(0, 0, 0);
            int high = IsoMath.CalculateSortingOrder(0, 0, 1);
            Assert.Greater(high, low);
        }

        [Test]
        public void SortingOrder_DeeperTile_HigherOrder()
        {
            int front = IsoMath.CalculateSortingOrder(0, 0, 0);
            int back = IsoMath.CalculateSortingOrder(1, 1, 0);
            Assert.Greater(back, front);
        }
    }
}
