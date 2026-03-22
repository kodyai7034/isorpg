using NUnit.Framework;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class UnitRegistryTests
    {
        [Test]
        public void Register_And_GetById()
        {
            var registry = new UnitRegistry();
            var unit = new UnitInstance("Ramza", 0, 1, new Vector2Int(0, 0));
            registry.Register(unit);

            Assert.AreSame(unit, registry.GetById(unit.Id));
            Assert.AreEqual(1, registry.Count);
        }

        [Test]
        public void GetById_NotFound_ReturnsNull()
        {
            var registry = new UnitRegistry();
            Assert.IsNull(registry.GetById(EntityId.New()));
        }

        [Test]
        public void GetAtPosition_ReturnsUnit()
        {
            var registry = new UnitRegistry();
            var unit = new UnitInstance("Ramza", 0, 1, new Vector2Int(3, 4));
            registry.Register(unit);

            Assert.AreSame(unit, registry.GetAtPosition(new Vector2Int(3, 4)));
        }

        [Test]
        public void GetAtPosition_Empty_ReturnsNull()
        {
            var registry = new UnitRegistry();
            Assert.IsNull(registry.GetAtPosition(new Vector2Int(5, 5)));
        }

        [Test]
        public void GetAtPosition_UpdatesOnMove()
        {
            var registry = new UnitRegistry();
            var unit = new UnitInstance("Ramza", 0, 1, new Vector2Int(0, 0));
            registry.Register(unit);

            unit.SetPosition(new Vector2Int(3, 4));

            Assert.IsNull(registry.GetAtPosition(new Vector2Int(0, 0)));
            Assert.AreSame(unit, registry.GetAtPosition(new Vector2Int(3, 4)));
        }

        [Test]
        public void GetTeam_ReturnsCorrectUnits()
        {
            var registry = new UnitRegistry();
            var player1 = new UnitInstance("P1", 0, 1, new Vector2Int(0, 0));
            var player2 = new UnitInstance("P2", 0, 1, new Vector2Int(1, 0));
            var enemy1 = new UnitInstance("E1", 1, 1, new Vector2Int(5, 5));
            registry.Register(player1);
            registry.Register(player2);
            registry.Register(enemy1);

            var team0 = registry.GetTeam(0);
            var team1 = registry.GetTeam(1);

            Assert.AreEqual(2, team0.Count);
            Assert.AreEqual(1, team1.Count);
        }

        [Test]
        public void GetAtPosition_DeadUnit_ReturnsNull()
        {
            var registry = new UnitRegistry();
            var unit = new UnitInstance("Ramza", 0, 1, new Vector2Int(0, 0));
            unit.Stats = new ComputedStats { MaxHP = 10, Speed = 5, Move = 4, Jump = 3 };
            unit.SetHP(10);
            registry.Register(unit);

            unit.ApplyDamage(10); // kill

            Assert.IsNull(registry.GetAtPosition(new Vector2Int(0, 0)));
        }

        [Test]
        public void IsOccupied_CorrectResults()
        {
            var registry = new UnitRegistry();
            var unit = new UnitInstance("Ramza", 0, 1, new Vector2Int(2, 3));
            registry.Register(unit);

            Assert.IsTrue(registry.IsOccupied(new Vector2Int(2, 3)));
            Assert.IsFalse(registry.IsOccupied(new Vector2Int(0, 0)));
        }

        [Test]
        public void Register_Duplicate_Throws()
        {
            var registry = new UnitRegistry();
            var unit = new UnitInstance("Ramza", 0, 1, new Vector2Int(0, 0));
            registry.Register(unit);

            Assert.Throws<System.ArgumentException>(() => registry.Register(unit));
        }
    }
}
