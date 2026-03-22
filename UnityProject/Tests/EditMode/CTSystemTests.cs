using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Battle;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class CTSystemTests
    {
        private List<UnitInstance> CreateTestUnits()
        {
            var fast = new UnitInstance("Fast", 0, 1, Vector2Int.zero);
            var slow = new UnitInstance("Slow", 1, 1, new Vector2Int(1, 0));

            // Override stats for deterministic testing
            fast.SetStats(new ComputedStats { Speed = 10, MaxHP = 100, Move = 4, Jump = 3 });
            fast.SetHP(100);
            slow.SetStats(new ComputedStats { Speed = 5, MaxHP = 100, Move = 4, Jump = 3 });
            slow.SetHP(100);

            return new List<UnitInstance> { fast, slow };
        }

        [Test]
        public void AdvanceTick_FasterUnit_GoesFirst()
        {
            var units = CreateTestUnits();
            var active = CTSystem.AdvanceTick(units);
            Assert.AreEqual("Fast", active.Name);
        }

        [Test]
        public void AdvanceTick_CTReachesThreshold()
        {
            var units = CreateTestUnits();
            var active = CTSystem.AdvanceTick(units);
            Assert.GreaterOrEqual(active.CT, CTSystem.TurnThreshold);
        }

        [Test]
        public void ResolveTurn_MoveAndAct_Costs100()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.CT = 120;
            CTSystem.ResolveTurn(unit, moved: true, acted: true);
            Assert.AreEqual(20, unit.CT);
        }

        [Test]
        public void ResolveTurn_MoveOnly_Costs80()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.CT = 120;
            CTSystem.ResolveTurn(unit, moved: true, acted: false);
            Assert.AreEqual(40, unit.CT);
        }

        [Test]
        public void ResolveTurn_Wait_Costs60()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.CT = 120;
            CTSystem.ResolveTurn(unit, moved: false, acted: false);
            Assert.AreEqual(60, unit.CT);
        }

        [Test]
        public void ResolveTurn_CapsAtMax()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.CT = 200;
            CTSystem.ResolveTurn(unit, moved: false, acted: false);
            Assert.AreEqual(CTSystem.MaxCTAfterAction, unit.CT);
        }

        [Test]
        public void PreviewTurnOrder_ReturnsCorrectCount()
        {
            var units = CreateTestUnits();
            var preview = CTSystem.PreviewTurnOrder(units, 5);
            Assert.AreEqual(5, preview.Count);
        }

        [Test]
        public void PreviewTurnOrder_DoesNotMutateState()
        {
            var units = CreateTestUnits();
            int ct0 = units[0].CT;
            int ct1 = units[1].CT;
            CTSystem.PreviewTurnOrder(units, 10);
            Assert.AreEqual(ct0, units[0].CT);
            Assert.AreEqual(ct1, units[1].CT);
        }

        [Test]
        public void AdvanceTick_DeadUnits_Skipped()
        {
            var units = CreateTestUnits();
            units[0].SetHP(0); // Kill the fast unit
            var active = CTSystem.AdvanceTick(units);
            Assert.AreEqual("Slow", active.Name);
        }
    }
}
