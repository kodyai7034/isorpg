using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Battle;
using IsoRPG.Map;
using IsoRPG.Units;
using IsoRPG.Core;

namespace IsoRPG.Tests
{
    public class PathfinderTests
    {
        private BattleMapData CreateFlatMap(int size = 5)
        {
            return MapGenerator.CreateFlatMap(size, size);
        }

        [Test]
        public void GetReachableTiles_FlatMap_ReturnsCorrectRange()
        {
            var map = MapGenerator.CreateFlatMap(10, 10);
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(5, 5));
            unit.Stats = new ComputedStats { Move = 3, Jump = 3, Speed = 5, MaxHP = 100 };
            unit.CurrentHP = 100;

            var reachable = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });

            Assert.IsTrue(reachable.ContainsKey(new Vector2Int(5, 5)));   // start
            Assert.IsTrue(reachable.ContainsKey(new Vector2Int(5, 6)));   // 1 away
            Assert.IsTrue(reachable.ContainsKey(new Vector2Int(5, 8)));   // 3 away
            Assert.IsFalse(reachable.ContainsKey(new Vector2Int(5, 9)));  // 4 away
        }

        [Test]
        public void GetReachableTiles_WaterBlocks()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
            map.Tiles[2 * 5 + 2] = TileData.Create(2, 2, 0, TerrainType.Water);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(2, 0));
            unit.Stats = new ComputedStats { Move = 4, Jump = 3, Speed = 5, MaxHP = 100 };
            unit.CurrentHP = 100;

            var reachable = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });
            Assert.IsFalse(reachable.ContainsKey(new Vector2Int(2, 2)));
        }

        [Test]
        public void GetReachableTiles_ElevationRespected()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
            map.Tiles[2 * 5 + 2] = TileData.Create(2, 2, 5, TerrainType.Stone);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(2, 1));
            unit.Stats = new ComputedStats { Move = 4, Jump = 2, Speed = 5, MaxHP = 100 };
            unit.CurrentHP = 100;

            var reachable = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });
            Assert.IsFalse(reachable.ContainsKey(new Vector2Int(2, 2)));
        }

        [Test]
        public void GetReachableTiles_EnemyBlocks()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
            var player = new UnitInstance("Player", 0, 1, new Vector2Int(0, 0));
            player.Stats = new ComputedStats { Move = 4, Jump = 3, Speed = 5, MaxHP = 100 };
            player.CurrentHP = 100;

            var enemy = new UnitInstance("Enemy", 1, 1, new Vector2Int(1, 0));
            enemy.Stats = new ComputedStats { Move = 4, Jump = 3, Speed = 5, MaxHP = 100 };
            enemy.CurrentHP = 100;

            var allUnits = new List<UnitInstance> { player, enemy };
            var reachable = Pathfinder.GetReachableTiles(map, player, allUnits);

            Assert.IsFalse(reachable.ContainsKey(new Vector2Int(1, 0)));
        }

        [Test]
        public void ReconstructPath_ValidPath_ReturnsCorrect()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
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
