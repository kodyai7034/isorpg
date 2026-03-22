using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Battle;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class PathfinderTests
    {
        private BattleMapData CreateFlatMap(int size = 5)
        {
            var map = ScriptableObject.CreateInstance<BattleMapData>();
            map.width = size;
            map.height = size;
            map.tiles = new TileData[size * size];

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    map.tiles[y * size + x] = new TileData(x, y);

            return map;
        }

        [Test]
        public void GetReachableTiles_FlatMap_ReturnsCorrectRange()
        {
            var map = CreateFlatMap(10);
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(5, 5));
            unit.Stats = new ComputedStats { Move = 3, Jump = 3, Speed = 5, MaxHP = 100 };
            unit.CurrentHP = 100;

            var reachable = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });

            // Should include the starting tile
            Assert.IsTrue(reachable.ContainsKey(new Vector2Int(5, 5)));

            // Tiles at Manhattan distance <= 3 should be reachable
            Assert.IsTrue(reachable.ContainsKey(new Vector2Int(5, 6)));   // 1 away
            Assert.IsTrue(reachable.ContainsKey(new Vector2Int(5, 8)));   // 3 away
            Assert.IsFalse(reachable.ContainsKey(new Vector2Int(5, 9)));  // 4 away
        }

        [Test]
        public void GetReachableTiles_WaterBlocks()
        {
            var map = CreateFlatMap(5);
            // Place water at (2,2)
            map.tiles[2 * 5 + 2] = new TileData(2, 2, 0, IsoRPG.Core.TerrainType.Water);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(2, 0));
            unit.Stats = new ComputedStats { Move = 4, Jump = 3, Speed = 5, MaxHP = 100 };
            unit.CurrentHP = 100;

            var reachable = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });
            Assert.IsFalse(reachable.ContainsKey(new Vector2Int(2, 2)));
        }

        [Test]
        public void GetReachableTiles_ElevationRespected()
        {
            var map = CreateFlatMap(5);
            // Create a cliff at (2,2) — elevation 5
            map.tiles[2 * 5 + 2] = new TileData(2, 2, 5, IsoRPG.Core.TerrainType.Stone);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(2, 1));
            unit.Stats = new ComputedStats { Move = 4, Jump = 2, Speed = 5, MaxHP = 100 };
            unit.CurrentHP = 100;

            var reachable = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });
            // Jump=2, elevation diff=5, should NOT be reachable
            Assert.IsFalse(reachable.ContainsKey(new Vector2Int(2, 2)));
        }

        [Test]
        public void GetReachableTiles_EnemyBlocks()
        {
            var map = CreateFlatMap(5);
            var player = new UnitInstance("Player", 0, 1, new Vector2Int(0, 0));
            player.Stats = new ComputedStats { Move = 4, Jump = 3, Speed = 5, MaxHP = 100 };
            player.CurrentHP = 100;

            var enemy = new UnitInstance("Enemy", 1, 1, new Vector2Int(1, 0));
            enemy.Stats = new ComputedStats { Move = 4, Jump = 3, Speed = 5, MaxHP = 100 };
            enemy.CurrentHP = 100;

            var allUnits = new List<UnitInstance> { player, enemy };
            var reachable = Pathfinder.GetReachableTiles(map, player, allUnits);

            // Can't stop on enemy tile
            Assert.IsFalse(reachable.ContainsKey(new Vector2Int(1, 0)));
        }

        [Test]
        public void ReconstructPath_ValidPath_ReturnsCorrect()
        {
            var map = CreateFlatMap(5);
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(0, 0));
            unit.Stats = new ComputedStats { Move = 10, Jump = 3, Speed = 5, MaxHP = 100 };
            unit.CurrentHP = 100;

            var reachable = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });
            var path = Pathfinder.ReconstructPath(reachable, new Vector2Int(0, 0), new Vector2Int(2, 2));

            Assert.IsNotNull(path);
            Assert.AreEqual(new Vector2Int(2, 2), path[path.Count - 1]);
        }
    }
}
