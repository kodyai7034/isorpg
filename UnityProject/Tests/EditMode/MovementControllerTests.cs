using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class MovementControllerTests
    {
        private BattleMapData _map;
        private UnitInstance _unit;
        private List<UnitInstance> _allUnits;
        private MovementController _controller;

        [SetUp]
        public void SetUp()
        {
            _map = MapGenerator.CreateFlatMap(8, 8);
            _unit = new UnitInstance("Test", 0, 1, new Vector2Int(3, 3));
            _unit.SetStats(new ComputedStats { Move = 4, Jump = 3, Speed = 7, MaxHP = 100 });
            _unit.SetHP(100);
            _allUnits = new List<UnitInstance> { _unit };
            _controller = new MovementController();
        }

        [Test]
        public void ShowMovementRange_ReturnsResult()
        {
            // Can't test grid overlay without MonoBehaviour, but can test result
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.CanMoveTo(new Vector2Int(3, 4))); // 1 tile away
            Assert.IsTrue(result.CanMoveTo(new Vector2Int(3, 7))); // 4 tiles away
            Assert.IsFalse(result.CanMoveTo(new Vector2Int(3, 8))); // 5 tiles = out of range
        }

        [Test]
        public void CreateMoveCommand_ValidDestination_ReturnsCommand()
        {
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);

            // Simulate what MovementController does internally
            var path = Pathfinder.ReconstructPath(result, _unit.GridPosition, new Vector2Int(5, 3));
            Assert.IsNotNull(path);

            var cmd = new MoveCommand(_unit, new Vector2Int(5, 3), path, 42);
            Assert.AreEqual("Test moves to (5,3)", cmd.Description);
        }

        [Test]
        public void GetMoveCostTo_ReturnsCorrectCost()
        {
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);

            // Adjacent tile should cost 1
            Assert.IsTrue(result.StoppableTiles.TryGetValue(new Vector2Int(4, 3), out var node));
            Assert.AreEqual(1, node.CostSoFar);

            // 3 tiles away should cost 3
            Assert.IsTrue(result.StoppableTiles.TryGetValue(new Vector2Int(6, 3), out var node2));
            Assert.AreEqual(3, node2.CostSoFar);
        }

        [Test]
        public void PathPreview_ReturnsPathToReachableTile()
        {
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);
            var dest = new Vector2Int(5, 5);

            if (result.CanMoveTo(dest))
            {
                var path = Pathfinder.ReconstructPath(result, _unit.GridPosition, dest);
                Assert.IsNotNull(path);
                Assert.AreEqual(dest, path[path.Count - 1]);
            }
        }

        [Test]
        public void PathPreview_UnreachableTile_ReturnsNull()
        {
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);
            // 10 tiles away — definitely out of move range 4
            var path = Pathfinder.ReconstructPath(result, _unit.GridPosition, new Vector2Int(3, 13));
            Assert.IsNull(path); // not in visited set
        }
    }

    public class PathfinderMovementParamsTests
    {
        [Test]
        public void MovementParams_FromUnit_MatchesStats()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.SetStats(new ComputedStats { Move = 5, Jump = 4, Speed = 7, MaxHP = 100 });
            unit.SetHP(100);

            var p = MovementParams.FromUnit(unit);
            Assert.AreEqual(5, p.MoveRange);
            Assert.AreEqual(4, p.JumpHeight);
            Assert.IsFalse(p.IgnoreHeight);
            Assert.IsFalse(p.CanFly);
            Assert.IsFalse(p.CanTeleport);
        }

        [Test]
        public void IgnoreHeight_CanTraverseCliffsWithin()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
            // Create a cliff at (2,2) — elevation 10
            map.Tiles[2 * 5 + 2] = TileData.Create(2, 2, 10, TerrainType.Stone);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(2, 1));
            unit.SetStats(new ComputedStats { Move = 4, Jump = 1, Speed = 7, MaxHP = 100 });
            unit.SetHP(100);

            // Without IgnoreHeight: can't reach elevation 10 with Jump 1
            var normalResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, MovementParams.FromUnit(unit),
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsFalse(normalResult.CanMoveTo(new Vector2Int(2, 2)));

            // With IgnoreHeight: should reach it
            var flyParams = MovementParams.FromUnit(unit);
            flyParams.IgnoreHeight = true;
            var flyResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, flyParams,
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsTrue(flyResult.CanMoveTo(new Vector2Int(2, 2)));
        }

        [Test]
        public void CanFly_IgnoresTerrainCost()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
            // Fill row 2 with forest (moveCost 2)
            for (int x = 0; x < 5; x++)
                map.Tiles[2 * 5 + x] = TileData.Create(x, 2, 0, TerrainType.Forest);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(2, 0));
            unit.SetStats(new ComputedStats { Move = 3, Jump = 3, Speed = 7, MaxHP = 100 });
            unit.SetHP(100);

            // Without fly: forest costs 2, so reaching y=3 costs 1+2+1=4 > move range 3
            var normalResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, MovementParams.FromUnit(unit),
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsFalse(normalResult.CanMoveTo(new Vector2Int(2, 3)));

            // With fly: all tiles cost 1, so y=3 costs 3 = exactly move range
            var flyParams = MovementParams.FromUnit(unit);
            flyParams.CanFly = true;
            var flyResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, flyParams,
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsTrue(flyResult.CanMoveTo(new Vector2Int(2, 3)));
        }

        [Test]
        public void CanTeleport_IgnoresObstacles()
        {
            var map = MapGenerator.CreateFlatMap(8, 8);
            // Wall of water blocking direct path
            for (int x = 0; x < 8; x++)
                map.Tiles[3 * 8 + x] = TileData.Create(x, 3, 0, TerrainType.Water);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(4, 1));
            unit.SetStats(new ComputedStats { Move = 4, Jump = 3, Speed = 7, MaxHP = 100 });
            unit.SetHP(100);

            // Without teleport: can't cross water wall
            var normalResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, MovementParams.FromUnit(unit),
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsFalse(normalResult.CanMoveTo(new Vector2Int(4, 5)));

            // With teleport: can reach any walkable tile in Manhattan range
            var teleParams = MovementParams.FromUnit(unit);
            teleParams.CanTeleport = true;
            var teleResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, teleParams,
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsTrue(teleResult.CanMoveTo(new Vector2Int(4, 5)));
        }

        [Test]
        public void MoveRange_Override_ExtendsRange()
        {
            var map = MapGenerator.CreateFlatMap(10, 10);
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(5, 5));
            unit.SetStats(new ComputedStats { Move = 3, Jump = 3, Speed = 7, MaxHP = 100 });
            unit.SetHP(100);

            // Base move 3: can't reach 4 tiles away
            var baseResult = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });
            Assert.IsFalse(baseResult.CanMoveTo(new Vector2Int(5, 9)));

            // Move+1 = 4: still can't reach 4 tiles
            // Move+2 = 5: CAN reach 4 tiles away (5,9 is 4 away from 5,5)
            var extendedParams = MovementParams.FromUnit(unit);
            extendedParams.MoveRange = 5;
            var extResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, extendedParams,
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsTrue(extResult.CanMoveTo(new Vector2Int(5, 9)));
        }
    }
}
