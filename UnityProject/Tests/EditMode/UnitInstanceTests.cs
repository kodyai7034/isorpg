using NUnit.Framework;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class UnitInstanceTests
    {
        private UnitInstance CreateUnit(int hp = 100)
        {
            var unit = new UnitInstance("TestUnit", 0, 1, new Vector2Int(3, 4));
            unit.Stats = new ComputedStats { MaxHP = hp, MaxMP = 50, Speed = 7, Move = 4, Jump = 3 };
            unit.SetHP(hp);
            return unit;
        }

        [Test]
        public void Constructor_SetsAllFields()
        {
            var unit = new UnitInstance("Ramza", 0, 5, new Vector2Int(2, 3));
            Assert.AreEqual("Ramza", unit.Name);
            Assert.AreEqual(0, unit.Team);
            Assert.AreEqual(5, unit.Level);
            Assert.AreEqual(new Vector2Int(2, 3), unit.GridPosition);
            Assert.IsTrue(unit.Id.IsValid);
            Assert.IsTrue(unit.IsAlive);
            Assert.AreEqual(0, unit.CT);
        }

        [Test]
        public void Id_IsUnique()
        {
            var a = new UnitInstance("A", 0, 1, Vector2Int.zero);
            var b = new UnitInstance("B", 0, 1, Vector2Int.zero);
            Assert.AreNotEqual(a.Id, b.Id);
        }

        [Test]
        public void SetPosition_ChangesPosition_FiresEvent()
        {
            var unit = CreateUnit();
            Vector2Int from = Vector2Int.zero, to = Vector2Int.zero;
            unit.OnPositionChanged += (f, t) => { from = f; to = t; };

            unit.SetPosition(new Vector2Int(5, 6));

            Assert.AreEqual(new Vector2Int(5, 6), unit.GridPosition);
            Assert.AreEqual(new Vector2Int(3, 4), from);
            Assert.AreEqual(new Vector2Int(5, 6), to);
        }

        [Test]
        public void SetFacing_ChangesFacing_FiresEvent()
        {
            var unit = CreateUnit();
            Direction received = Direction.South;
            unit.OnFacingChanged += d => received = d;

            unit.SetFacing(Direction.NorthEast);

            Assert.AreEqual(Direction.NorthEast, unit.Facing);
            Assert.AreEqual(Direction.NorthEast, received);
        }

        [Test]
        public void ApplyDamage_ReducesHP_FiresEvent()
        {
            var unit = CreateUnit(100);
            int oldHP = -1, newHP = -1;
            unit.OnHPChanged += (o, n) => { oldHP = o; newHP = n; };

            unit.ApplyDamage(30);

            Assert.AreEqual(70, unit.CurrentHP);
            Assert.AreEqual(100, oldHP);
            Assert.AreEqual(70, newHP);
        }

        [Test]
        public void ApplyDamage_ClampsToZero()
        {
            var unit = CreateUnit(50);
            unit.ApplyDamage(999);
            Assert.AreEqual(0, unit.CurrentHP);
            Assert.IsFalse(unit.IsAlive);
        }

        [Test]
        public void ApplyDamage_ZeroOrNegative_Ignored()
        {
            var unit = CreateUnit(100);
            bool eventFired = false;
            unit.OnHPChanged += (_, _) => eventFired = true;

            unit.ApplyDamage(0);
            Assert.AreEqual(100, unit.CurrentHP);
            Assert.IsFalse(eventFired);

            unit.ApplyDamage(-10);
            Assert.AreEqual(100, unit.CurrentHP);
        }

        [Test]
        public void ApplyDamage_KillsUnit_FiresOnDied()
        {
            var unit = CreateUnit(10);
            bool died = false;
            unit.OnDied += () => died = true;

            unit.ApplyDamage(10);

            Assert.IsFalse(unit.IsAlive);
            Assert.IsTrue(died);
        }

        [Test]
        public void ApplyHealing_IncreasesHP_ClampsToMax()
        {
            var unit = CreateUnit(100);
            unit.ApplyDamage(50); // HP = 50
            unit.ApplyHealing(30);
            Assert.AreEqual(80, unit.CurrentHP);

            unit.ApplyHealing(999);
            Assert.AreEqual(100, unit.CurrentHP); // clamped to MaxHP
        }

        [Test]
        public void ApplyHealing_ZeroOrNegative_Ignored()
        {
            var unit = CreateUnit(100);
            unit.ApplyDamage(50);
            bool eventFired = false;
            unit.OnHPChanged += (_, _) => eventFired = true;

            unit.ApplyHealing(0);
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void SetHP_DirectlyChangesHP_FiresEvent()
        {
            var unit = CreateUnit(100);
            int oldHP = -1, newHP = -1;
            unit.OnHPChanged += (o, n) => { oldHP = o; newHP = n; };

            unit.SetHP(42);

            Assert.AreEqual(42, unit.CurrentHP);
            Assert.AreEqual(100, oldHP);
            Assert.AreEqual(42, newHP);
        }

        [Test]
        public void SetHP_SameValue_NoEvent()
        {
            var unit = CreateUnit(100);
            bool eventFired = false;
            unit.OnHPChanged += (_, _) => eventFired = true;

            unit.SetHP(100);
            Assert.IsFalse(eventFired);
        }
    }
}
