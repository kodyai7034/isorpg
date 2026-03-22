using NUnit.Framework;
using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Tests
{
    public class IsoMathTests
    {
        // --- GridToWorld / WorldToGrid ---

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
        public void WorldToGrid_RoundTrip_NoElevation()
        {
            var original = new Vector2Int(3, 5);
            var world = IsoMath.GridToWorld(original);
            var result = IsoMath.WorldToGrid(world);
            Assert.AreEqual(original, result);
        }

        [Test]
        public void WorldToGrid_RoundTrip_WithElevation()
        {
            var original = new Vector2Int(4, 2);
            int elevation = 3;
            var world = IsoMath.GridToWorld(original, elevation);
            var result = IsoMath.WorldToGrid(world, elevation);
            Assert.AreEqual(original, result);
        }

        [Test]
        public void WorldToGrid_WrongElevation_GivesWrongResult()
        {
            var original = new Vector2Int(4, 2);
            var world = IsoMath.GridToWorld(original, 5);
            var result = IsoMath.WorldToGrid(world, 0); // wrong elevation
            Assert.AreNotEqual(original, result);
        }

        // --- Sorting Order ---

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

        [Test]
        public void SortingOrder_SameElevation_DifferentDepths_NoOverlap()
        {
            // Max depth of elevation 0 should be less than min of elevation 1
            int maxElev0 = IsoMath.CalculateSortingOrder(99, 99, 0, 200);
            int minElev1 = IsoMath.CalculateSortingOrder(0, 0, 1, 200);
            Assert.Less(maxElev0, minElev1);
        }

        // --- Manhattan Distance ---

        [Test]
        public void ManhattanDistance_SamePoint_ReturnsZero()
        {
            Assert.AreEqual(0, IsoMath.ManhattanDistance(new Vector2Int(3, 3), new Vector2Int(3, 3)));
        }

        [Test]
        public void ManhattanDistance_KnownValues()
        {
            Assert.AreEqual(2, IsoMath.ManhattanDistance(new Vector2Int(0, 0), new Vector2Int(1, 1)));
            Assert.AreEqual(5, IsoMath.ManhattanDistance(new Vector2Int(1, 2), new Vector2Int(4, 4)));
        }

        [Test]
        public void ManhattanDistance_IsSymmetric()
        {
            var a = new Vector2Int(1, 3);
            var b = new Vector2Int(5, 7);
            Assert.AreEqual(IsoMath.ManhattanDistance(a, b), IsoMath.ManhattanDistance(b, a));
        }

        // --- GetDirection ---

        [Test]
        public void GetDirection_SamePosition_ReturnsSouth()
        {
            Assert.AreEqual(Direction.South, IsoMath.GetDirection(Vector2Int.zero, Vector2Int.zero));
        }

        [Test]
        public void GetDirection_CardinalDirections()
        {
            var origin = new Vector2Int(5, 5);
            Assert.AreEqual(Direction.East, IsoMath.GetDirection(origin, new Vector2Int(8, 5)));
            Assert.AreEqual(Direction.West, IsoMath.GetDirection(origin, new Vector2Int(2, 5)));
            Assert.AreEqual(Direction.North, IsoMath.GetDirection(origin, new Vector2Int(5, 8)));
            Assert.AreEqual(Direction.South, IsoMath.GetDirection(origin, new Vector2Int(5, 2)));
        }

        [Test]
        public void GetDirection_DiagonalDirections()
        {
            var origin = new Vector2Int(5, 5);
            Assert.AreEqual(Direction.NorthEast, IsoMath.GetDirection(origin, new Vector2Int(8, 8)));
            Assert.AreEqual(Direction.SouthWest, IsoMath.GetDirection(origin, new Vector2Int(2, 2)));
        }

        // --- RotateGrid ---

        [Test]
        public void RotateGrid_ZeroRotation_ReturnsSame()
        {
            var pos = new Vector2Int(2, 3);
            Assert.AreEqual(pos, IsoMath.RotateGrid(pos, 0, 8));
        }

        [Test]
        public void RotateGrid_FullRotation_ReturnsSame()
        {
            var pos = new Vector2Int(2, 3);
            Assert.AreEqual(pos, IsoMath.RotateGrid(pos, 4, 8));
        }

        [Test]
        public void RotateGrid_90CW_CorrectTransform()
        {
            // (0,0) in 8x8 map rotated 90° CW → (7,0)
            Assert.AreEqual(new Vector2Int(7, 0), IsoMath.RotateGrid(new Vector2Int(0, 0), 1, 8));
        }

        [Test]
        public void RotateGrid_180_CorrectTransform()
        {
            // (0,0) in 8x8 map rotated 180° → (7,7)
            Assert.AreEqual(new Vector2Int(7, 7), IsoMath.RotateGrid(new Vector2Int(0, 0), 2, 8));
        }

        [Test]
        public void RotateGrid_NegativeIndex_HandledCorrectly()
        {
            var pos = new Vector2Int(2, 3);
            var rotated = IsoMath.RotateGrid(pos, -1, 8);
            var expected = IsoMath.RotateGrid(pos, 3, 8); // -1 == 3 (mod 4)
            Assert.AreEqual(expected, rotated);
        }

        // --- GetDirectionVector ---

        [Test]
        public void GetDirectionVector_East_IsPositiveX()
        {
            var vec = IsoMath.GetDirectionVector(Direction.East);
            Assert.AreEqual(1, vec.x);
            Assert.AreEqual(0, vec.y);
        }

        [Test]
        public void GetDirectionVector_AllDirections_NonZero()
        {
            // Every direction should have a non-zero vector
            for (int i = 0; i < 8; i++)
            {
                var vec = IsoMath.GetDirectionVector((Direction)i);
                Assert.IsTrue(vec.x != 0 || vec.y != 0, $"Direction {(Direction)i} has zero vector");
            }
        }
    }
}
