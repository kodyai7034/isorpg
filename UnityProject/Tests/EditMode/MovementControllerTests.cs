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
        public void IsShowingRange_FalseBeforeShow()
        {
            Assert.IsFalse(_controller.IsShowingRange);
            Assert.IsNull(_controller.CurrentResult);
        }

        [Test]
        public void CreateMoveCommand_WithoutShowingRange_ReturnsNull()
        {
            // Controller has no result — should return null
            var cmd = _controller.CreateMoveCommand(_unit, new Vector2Int(4, 3), null);
            Assert.IsNull(cmd);
        }

        [Test]
        public void GetMoveCostTo_WithoutRange_ReturnsNegative()
        {
            Assert.AreEqual(-1, _controller.GetMoveCostTo(new Vector2Int(4, 3)));
        }

        [Test]
        public void CreateMoveCommand_AfterManualResult_ProducesValidCommand()
        {
            // Simulate what ShowMovementRange does (without MonoBehaviour grid)
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);

            // Use reflection-free approach: directly test the Pathfinder + MoveCommand integration
            var dest = new Vector2Int(5, 3);
            Assert.IsTrue(result.CanMoveTo(dest));

            var path = Pathfinder.ReconstructPath(result, _unit.GridPosition, dest);
            Assert.IsNotNull(path);

            var rng = new GameRng(42);
            var cmd = new MoveCommand(_unit, dest, path, rng.Seed);

            Assert.AreEqual("Test moves to (5,3)", cmd.Description);
            Assert.AreEqual(_unit.Id, cmd.ActorId);

            // Execute and verify
            cmd.Execute();
            Assert.AreEqual(dest, _unit.GridPosition);

            // Undo and verify
            cmd.Undo();
            Assert.AreEqual(new Vector2Int(3, 3), _unit.GridPosition);
        }

        [Test]
        public void PathfindingResult_CostAccuracy()
        {
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);

            // Adjacent tile: cost 1
            Assert.IsTrue(result.StoppableTiles.TryGetValue(new Vector2Int(4, 3), out var adj));
            Assert.AreEqual(1, adj.CostSoFar);

            // 3 tiles away: cost 3
            Assert.IsTrue(result.StoppableTiles.TryGetValue(new Vector2Int(6, 3), out var far));
            Assert.AreEqual(3, far.CostSoFar);

            // 4 tiles away: cost 4 (at boundary)
            Assert.IsTrue(result.StoppableTiles.TryGetValue(new Vector2Int(7, 3), out var edge));
            Assert.AreEqual(4, edge.CostSoFar);

            // 5 tiles away: out of range
            Assert.IsFalse(result.CanMoveTo(new Vector2Int(3, 8)));
        }

        [Test]
        public void PathPreview_ReachableTile_ReturnsPath()
        {
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);
            var dest = new Vector2Int(5, 5);

            // (5,5) is Manhattan distance 4 from (3,3) — exactly at move range
            Assert.IsTrue(result.CanMoveTo(dest),
                "Destination should be reachable on flat 8x8 map with Move=4");

            var path = Pathfinder.ReconstructPath(result, _unit.GridPosition, dest);
            Assert.IsNotNull(path);
            Assert.AreEqual(dest, path[path.Count - 1]);
            Assert.Greater(path.Count, 0);
        }

        [Test]
        public void PathPreview_UnreachableTile_ReturnsNull()
        {
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);
            var path = Pathfinder.ReconstructPath(result, _unit.GridPosition, new Vector2Int(0, 7));
            // (0,7) is Manhattan distance 7 from (3,3) — out of Move=4 range
            Assert.IsNull(path);
        }

        [Test]
        public void AllyPassThrough_PathReconstructsCorrectly()
        {
            // Place ally blocking the direct path
            var ally = new UnitInstance("Ally", 0, 1, new Vector2Int(4, 3));
            ally.SetStats(new ComputedStats { Move = 4, Jump = 3, Speed = 5, MaxHP = 100 });
            ally.SetHP(100);
            var units = new List<UnitInstance> { _unit, ally };

            var result = Pathfinder.GetReachableTiles(_map, _unit, units);

            // Can't stop on ally's tile
            Assert.IsFalse(result.CanMoveTo(new Vector2Int(4, 3)));

            // CAN reach tile beyond ally (path goes through ally)
            Assert.IsTrue(result.CanMoveTo(new Vector2Int(5, 3)));

            // Path reconstruction should work through the ally tile
            var path = Pathfinder.ReconstructPath(result, _unit.GridPosition, new Vector2Int(5, 3));
            Assert.IsNotNull(path, "Path through ally-occupied tile should reconstruct successfully");
            Assert.AreEqual(new Vector2Int(5, 3), path[path.Count - 1]);
        }

        [Test]
        public void MoveCommand_ViaCommandHistory_FullUndoFlow()
        {
            var history = new CommandHistory();
            var result = Pathfinder.GetReachableTiles(_map, _unit, _allUnits);
            var dest = new Vector2Int(5, 5);
            var path = Pathfinder.ReconstructPath(result, _unit.GridPosition, dest);

            var cmd = new MoveCommand(_unit, dest, path, 0);
            history.ExecuteCommand(cmd);
            Assert.AreEqual(dest, _unit.GridPosition);

            history.Undo();
            Assert.AreEqual(new Vector2Int(3, 3), _unit.GridPosition);
            Assert.AreEqual(0, history.Count);
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
        public void IgnoreHeight_CanTraverseCliffs()
        {
            var map = MapGenerator.CreateFlatMap(5, 5);
            map.Tiles[2 * 5 + 2] = TileData.Create(2, 2, 10, TerrainType.Stone);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(2, 1));
            unit.SetStats(new ComputedStats { Move = 4, Jump = 1, Speed = 7, MaxHP = 100 });
            unit.SetHP(100);

            // Without IgnoreHeight: can't reach elevation 10 with Jump 1
            var normalResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, MovementParams.FromUnit(unit),
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsFalse(normalResult.CanMoveTo(new Vector2Int(2, 2)));

            // With IgnoreHeight
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
            for (int x = 0; x < 5; x++)
                map.Tiles[2 * 5 + x] = TileData.Create(x, 2, 0, TerrainType.Forest);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(2, 0));
            unit.SetStats(new ComputedStats { Move = 3, Jump = 3, Speed = 7, MaxHP = 100 });
            unit.SetHP(100);

            var normalResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, MovementParams.FromUnit(unit),
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsFalse(normalResult.CanMoveTo(new Vector2Int(2, 3)));

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
            for (int x = 0; x < 8; x++)
                map.Tiles[3 * 8 + x] = TileData.Create(x, 3, 0, TerrainType.Water);

            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(4, 1));
            unit.SetStats(new ComputedStats { Move = 4, Jump = 3, Speed = 7, MaxHP = 100 });
            unit.SetHP(100);

            var normalResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, MovementParams.FromUnit(unit),
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsFalse(normalResult.CanMoveTo(new Vector2Int(4, 5)));

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

            var baseResult = Pathfinder.GetReachableTiles(map, unit, new List<UnitInstance> { unit });
            Assert.IsFalse(baseResult.CanMoveTo(new Vector2Int(5, 9)));

            var extendedParams = MovementParams.FromUnit(unit);
            extendedParams.MoveRange = 5;
            var extResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, extendedParams,
                unit.Team, new List<UnitInstance> { unit });
            Assert.IsTrue(extResult.CanMoveTo(new Vector2Int(5, 9)));
        }
    }
}
