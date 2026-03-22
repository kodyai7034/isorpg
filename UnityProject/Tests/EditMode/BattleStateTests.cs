using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Battle.States;
using IsoRPG.Map;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Tests
{
    public class BattleStateTests
    {
        private BattleContext CreateTestContext()
        {
            var map = MapGenerator.CreateFlatMap(8, 8);
            var ctx = new BattleContext
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
            return ctx;
        }

        private UnitInstance AddUnit(BattleContext ctx, string name, int team, Vector2Int pos, int speed = 7)
        {
            var unit = new UnitInstance(name, team, 1, pos);
            unit.SetStats(new ComputedStats
            {
                MaxHP = 100, MaxMP = 50,
                PhysicalAttack = 10, MagicAttack = 10,
                Speed = speed, Move = 4, Jump = 3,
                Defense = 5, MagicDefense = 5
            });
            unit.SetHP(100);
            ctx.AllUnits.Add(unit);
            ctx.Registry.Register(unit);
            return unit;
        }

        // --- CTAdvanceState Tests ---

        [Test]
        public void CTAdvance_SelectsFastestUnit()
        {
            var ctx = CreateTestContext();
            var fast = AddUnit(ctx, "Fast", 0, new Vector2Int(0, 0), speed: 10);
            var slow = AddUnit(ctx, "Slow", 1, new Vector2Int(5, 5), speed: 5);

            // Manually tick CT to find who goes first
            var active = CTSystem.AdvanceTick(ctx.AllUnits);
            Assert.AreEqual("Fast", active.Name);
        }

        [Test]
        public void CTAdvance_SetsContextCorrectly()
        {
            var ctx = CreateTestContext();
            AddUnit(ctx, "Player", 0, new Vector2Int(0, 0), speed: 10);
            AddUnit(ctx, "Enemy", 1, new Vector2Int(5, 5), speed: 5);

            var active = CTSystem.AdvanceTick(ctx.AllUnits);
            ctx.ActiveUnit = active;
            ctx.ActiveUnitMoved = false;
            ctx.ActiveUnitActed = false;
            ctx.TurnNumber++;

            Assert.IsNotNull(ctx.ActiveUnit);
            Assert.IsFalse(ctx.ActiveUnitMoved);
            Assert.IsFalse(ctx.ActiveUnitActed);
            Assert.AreEqual(1, ctx.TurnNumber);
        }

        // --- EndTurnState Tests ---

        [Test]
        public void EndTurn_ResolvesCT_MoveAndAct()
        {
            var ctx = CreateTestContext();
            var unit = AddUnit(ctx, "Test", 0, Vector2Int.zero, speed: 10);

            // Simulate a turn where unit moved and acted
            CTSystem.AdvanceTick(ctx.AllUnits);
            int ctBefore = unit.CT;

            ctx.ActiveUnit = unit;
            ctx.ActiveUnitMoved = true;
            ctx.ActiveUnitActed = true;

            CTSystem.ResolveTurn(unit, true, true);

            Assert.AreEqual(Mathf.Max(0, ctBefore - GameConstants.CTCostMoveAndAct), unit.CT);
        }

        [Test]
        public void EndTurn_ResolvesCT_WaitOnly()
        {
            var ctx = CreateTestContext();
            var unit = AddUnit(ctx, "Test", 0, Vector2Int.zero, speed: 10);

            CTSystem.AdvanceTick(ctx.AllUnits);
            int ctBefore = unit.CT;

            CTSystem.ResolveTurn(unit, false, false);

            int expected = Mathf.Min(ctBefore - GameConstants.CTCostWait, GameConstants.MaxCTAfterAction);
            Assert.AreEqual(Mathf.Max(0, expected), unit.CT);
        }

        // --- Victory/Defeat Detection ---

        [Test]
        public void IsTeamDefeated_AllDead_ReturnsTrue()
        {
            var ctx = CreateTestContext();
            var enemy = AddUnit(ctx, "Enemy", 1, new Vector2Int(5, 5));
            enemy.ApplyDamage(999); // kill

            Assert.IsTrue(ctx.IsTeamDefeated(1));
        }

        [Test]
        public void IsTeamDefeated_SomeAlive_ReturnsFalse()
        {
            var ctx = CreateTestContext();
            AddUnit(ctx, "Enemy1", 1, new Vector2Int(5, 5));
            var enemy2 = AddUnit(ctx, "Enemy2", 1, new Vector2Int(6, 6));
            enemy2.ApplyDamage(999);

            Assert.IsFalse(ctx.IsTeamDefeated(1));
        }

        [Test]
        public void IsTeamDefeated_NoUnitsOnTeam_ReturnsTrue()
        {
            var ctx = CreateTestContext();
            AddUnit(ctx, "Player", 0, Vector2Int.zero);

            Assert.IsTrue(ctx.IsTeamDefeated(1)); // no team 1 units
        }

        // --- Command Undo in Battle ---

        [Test]
        public void Undo_MoveCommand_RestoresPosition()
        {
            var ctx = CreateTestContext();
            var unit = AddUnit(ctx, "Test", 0, new Vector2Int(2, 2));

            var path = new List<Vector2Int> { new(3, 2), new(4, 2) };
            var cmd = new MoveCommand(unit, new Vector2Int(4, 2), path, ctx.Rng.Seed);

            ctx.CommandHistory.ExecuteCommand(cmd);
            Assert.AreEqual(new Vector2Int(4, 2), unit.GridPosition);

            ctx.CommandHistory.Undo();
            Assert.AreEqual(new Vector2Int(2, 2), unit.GridPosition);
        }

        // --- Turn Events ---

        [Test]
        public void TurnStarted_EventFires()
        {
            var ctx = CreateTestContext();
            var unit = AddUnit(ctx, "Test", 0, Vector2Int.zero, speed: 10);

            TurnStartedArgs received = default;
            GameEvents.TurnStarted.Subscribe(args => received = args);

            try
            {
                var active = CTSystem.AdvanceTick(ctx.AllUnits);
                ctx.ActiveUnit = active;
                ctx.TurnNumber = 1;
                GameEvents.TurnStarted.Raise(new TurnStartedArgs(active.Id, 1));

                Assert.AreEqual(unit.Id, received.UnitId);
                Assert.AreEqual(1, received.TurnNumber);
            }
            finally
            {
                GameEvents.TurnStarted.Clear();
            }
        }

        [Test]
        public void BattleEnded_Victory_EventFires()
        {
            BattleEndedArgs received = default;
            GameEvents.BattleEnded.Subscribe(args => received = args);

            try
            {
                GameEvents.BattleEnded.Raise(new BattleEndedArgs(BattleResult.Victory, 10));
                Assert.AreEqual(BattleResult.Victory, received.Result);
                Assert.AreEqual(10, received.TurnsElapsed);
            }
            finally
            {
                GameEvents.BattleEnded.Clear();
            }
        }
    }
}
