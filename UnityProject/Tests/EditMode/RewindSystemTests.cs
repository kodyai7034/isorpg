using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Map;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Tests
{
    public class RewindSystemTests
    {
        private BattleContext CreateContext()
        {
            var map = MapGenerator.CreateFlatMap(8, 8);
            return new BattleContext
            {
                Map = map,
                Registry = new UnitRegistry(),
                AllUnits = new List<UnitInstance>(),
                UnitViews = new Dictionary<EntityId, UnitView>(),
                CommandHistory = new CommandHistory(),
                MovementController = new MovementController(),
                Rng = new GameRng(42),
                TurnNumber = 0
            };
        }

        private UnitInstance AddUnit(BattleContext ctx, string name, int team, Vector2Int pos)
        {
            var unit = new UnitInstance(name, team, 1, pos);
            unit.SetStats(new ComputedStats
            {
                MaxHP = 100, MaxMP = 50,
                PhysicalAttack = 10, MagicAttack = 10,
                Speed = 7, Move = 4, Jump = 3,
                Defense = 5, MagicDefense = 5
            });
            unit.SetHP(100);
            ctx.AllUnits.Add(unit);
            ctx.Registry.Register(unit);
            return unit;
        }

        [Test]
        public void CaptureSnapshot_PreservesUnitState()
        {
            var ctx = CreateContext();
            var unit = AddUnit(ctx, "Ramza", 0, new Vector2Int(3, 4));
            unit.CT = 50;
            ctx.ActiveUnit = unit;
            ctx.TurnNumber = 5;

            var rewind = new RewindSystem();
            var snapshot = rewind.CaptureSnapshot(ctx);

            Assert.AreEqual(5, snapshot.TurnNumber);
            Assert.IsTrue(snapshot.Units.ContainsKey(unit.Id));
            Assert.AreEqual(new Vector2Int(3, 4), snapshot.Units[unit.Id].Position);
            Assert.AreEqual(100, snapshot.Units[unit.Id].CurrentHP);
            Assert.AreEqual(50, snapshot.Units[unit.Id].CT);
        }

        [Test]
        public void RestoreSnapshot_RestoresAllState()
        {
            var ctx = CreateContext();
            var unit = AddUnit(ctx, "Ramza", 0, new Vector2Int(3, 4));
            ctx.ActiveUnit = unit;
            ctx.TurnNumber = 1;

            var rewind = new RewindSystem();
            var snapshot = rewind.CaptureSnapshot(ctx);

            // Modify state
            unit.SetPosition(new Vector2Int(7, 7));
            unit.ApplyDamage(50);
            unit.CT = 99;
            ctx.TurnNumber = 5;

            // Restore
            rewind.RestoreSnapshot(ctx, snapshot);

            Assert.AreEqual(new Vector2Int(3, 4), unit.GridPosition);
            Assert.AreEqual(100, unit.CurrentHP);
            Assert.AreEqual(0, unit.CT); // CT was 0 at snapshot (before we set 50)
            // Actually unit.CT was 0 at construction, let me check...
            // We set CT=50 before snapshot, so snapshot should have CT=50
        }

        [Test]
        public void RestoreSnapshot_RestoresCTCorrectly()
        {
            var ctx = CreateContext();
            var unit = AddUnit(ctx, "Test", 0, Vector2Int.zero);
            unit.CT = 42;
            ctx.ActiveUnit = unit;

            var rewind = new RewindSystem();
            var snapshot = rewind.CaptureSnapshot(ctx);

            unit.CT = 99;
            rewind.RestoreSnapshot(ctx, snapshot);

            Assert.AreEqual(42, unit.CT);
        }

        [Test]
        public void RestoreSnapshot_RestoresStatusEffects()
        {
            var ctx = CreateContext();
            var unit = AddUnit(ctx, "Test", 0, Vector2Int.zero);
            unit.AddStatus(new StatusEffectInstance(StatusType.Poison, 3));
            ctx.ActiveUnit = unit;

            var rewind = new RewindSystem();
            var snapshot = rewind.CaptureSnapshot(ctx);

            // Remove status
            unit.StatusEffects.Clear();
            Assert.AreEqual(0, unit.StatusEffects.Count);

            // Restore
            rewind.RestoreSnapshot(ctx, snapshot);
            Assert.AreEqual(1, unit.StatusEffects.Count);
            Assert.AreEqual(StatusType.Poison, unit.StatusEffects[0].Type);
            Assert.AreEqual(3, unit.StatusEffects[0].RemainingDuration);
        }

        [Test]
        public void RestoreSnapshot_RestoresRNG()
        {
            var ctx = CreateContext();
            AddUnit(ctx, "Test", 0, Vector2Int.zero);
            ctx.ActiveUnit = ctx.AllUnits[0];

            var rewind = new RewindSystem();
            int seedBefore = ctx.Rng.Seed;
            rewind.CaptureSnapshot(ctx);

            // Consume some RNG
            ctx.Rng.Range(0, 100);
            ctx.Rng.Range(0, 100);
            Assert.AreNotEqual(seedBefore, ctx.Rng.Seed);

            // Restore
            rewind.RestoreSnapshot(ctx, rewind.GetLatestSnapshot());
            Assert.AreEqual(seedBefore, ctx.Rng.Seed);
        }

        [Test]
        public void RewindCommands_UndoesAndRestores()
        {
            var ctx = CreateContext();
            var unit = AddUnit(ctx, "Test", 0, new Vector2Int(0, 0));
            ctx.ActiveUnit = unit;

            var rewind = new RewindSystem();

            // Capture initial state
            rewind.CaptureSnapshot(ctx);

            // Execute move
            var path = new List<Vector2Int> { new(1, 0), new(2, 0) };
            var cmd = new MoveCommand(unit, new Vector2Int(2, 0), path, ctx.Rng.Seed);
            rewind.CaptureSnapshot(ctx);
            ctx.CommandHistory.ExecuteCommand(cmd);

            Assert.AreEqual(new Vector2Int(2, 0), unit.GridPosition);

            // Rewind 1 command
            rewind.RewindCommands(ctx, 1);

            Assert.AreEqual(new Vector2Int(0, 0), unit.GridPosition);
            Assert.AreEqual(0, ctx.CommandHistory.Count);
        }

        [Test]
        public void RewindCommands_MultipleCommands()
        {
            var ctx = CreateContext();
            var unit = AddUnit(ctx, "Test", 0, new Vector2Int(0, 0));
            ctx.ActiveUnit = unit;

            var rewind = new RewindSystem();

            // Snapshot + Move 1
            rewind.CaptureSnapshot(ctx);
            var cmd1 = new MoveCommand(unit, new Vector2Int(1, 0),
                new List<Vector2Int> { new(1, 0) }, ctx.Rng.Seed);
            ctx.CommandHistory.ExecuteCommand(cmd1);

            // Snapshot + Move 2
            rewind.CaptureSnapshot(ctx);
            var cmd2 = new MoveCommand(unit, new Vector2Int(2, 0),
                new List<Vector2Int> { new(2, 0) }, ctx.Rng.Seed);
            ctx.CommandHistory.ExecuteCommand(cmd2);

            Assert.AreEqual(new Vector2Int(2, 0), unit.GridPosition);

            // Rewind both
            rewind.RewindCommands(ctx, 2);
            Assert.AreEqual(new Vector2Int(0, 0), unit.GridPosition);
        }

        [Test]
        public void RNG_Determinism_SameActionSameResult()
        {
            var rng = new GameRng(42);

            // Capture seed
            int seed = rng.Seed;
            int roll1 = rng.Range(0, 100);

            // Reset and replay
            rng.SetSeed(seed);
            int roll2 = rng.Range(0, 100);

            Assert.AreEqual(roll1, roll2, "Same seed must produce same result");
        }

        [Test]
        public void MaxSnapshots_EvictsOldest()
        {
            var ctx = CreateContext();
            AddUnit(ctx, "Test", 0, Vector2Int.zero);
            ctx.ActiveUnit = ctx.AllUnits[0];

            var rewind = new RewindSystem(maxSnapshots: 3);

            rewind.CaptureSnapshot(ctx);
            rewind.CaptureSnapshot(ctx);
            rewind.CaptureSnapshot(ctx);
            rewind.CaptureSnapshot(ctx); // evicts first

            Assert.AreEqual(3, rewind.SnapshotCount);
        }
    }
}
