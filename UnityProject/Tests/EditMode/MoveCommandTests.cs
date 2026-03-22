using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class MoveCommandTests
    {
        [Test]
        public void Execute_ChangesPosition()
        {
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(0, 0));
            var dest = new Vector2Int(2, 3);
            var path = new List<Vector2Int> { new(1, 0), new(2, 0), new(2, 1), new(2, 2), new(2, 3) };

            var cmd = new MoveCommand(unit, dest, path);
            cmd.Execute();

            Assert.AreEqual(dest, unit.GridPosition);
        }

        [Test]
        public void Undo_RestoresPosition()
        {
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(1, 1));
            var cmd = new MoveCommand(unit, new Vector2Int(3, 4), new List<Vector2Int> { new(3, 4) });

            cmd.Execute();
            Assert.AreEqual(new Vector2Int(3, 4), unit.GridPosition);

            cmd.Undo();
            Assert.AreEqual(new Vector2Int(1, 1), unit.GridPosition);
        }

        [Test]
        public void Execute_UpdatesFacing()
        {
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(0, 0));
            unit.SetFacing(Direction.North);

            var cmd = new MoveCommand(unit, new Vector2Int(3, 0), new List<Vector2Int> { new(3, 0) });
            cmd.Execute();

            Assert.AreEqual(Direction.East, unit.Facing);
        }

        [Test]
        public void Undo_RestoresFacing()
        {
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(0, 0));
            unit.SetFacing(Direction.NorthWest);

            var cmd = new MoveCommand(unit, new Vector2Int(3, 0), new List<Vector2Int> { new(3, 0) });
            cmd.Execute();
            cmd.Undo();

            Assert.AreEqual(Direction.NorthWest, unit.Facing);
        }

        [Test]
        public void Execute_FiresUnitMovedEvent()
        {
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(0, 0));
            UnitMovedArgs received = default;
            GameEvents.UnitMoved.Subscribe(args => received = args);

            try
            {
                var cmd = new MoveCommand(unit, new Vector2Int(2, 2), new List<Vector2Int> { new(2, 2) });
                cmd.Execute();

                Assert.AreEqual(unit.Id, received.UnitId);
                Assert.AreEqual(new Vector2Int(0, 0), received.From);
                Assert.AreEqual(new Vector2Int(2, 2), received.To);
            }
            finally
            {
                GameEvents.UnitMoved.Clear();
            }
        }

        [Test]
        public void Description_IsCorrect()
        {
            var unit = new UnitInstance("Ramza", 0, 1, new Vector2Int(0, 0));
            var cmd = new MoveCommand(unit, new Vector2Int(3, 4), null);
            Assert.AreEqual("Ramza moves to (3,4)", cmd.Description);
        }

        [Test]
        public void ExecuteUndo_ViaCommandHistory()
        {
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(0, 0));
            var history = new CommandHistory();

            history.ExecuteCommand(new MoveCommand(unit, new Vector2Int(2, 2),
                new List<Vector2Int> { new(1, 1), new(2, 2) }));
            Assert.AreEqual(new Vector2Int(2, 2), unit.GridPosition);

            history.Undo();
            Assert.AreEqual(new Vector2Int(0, 0), unit.GridPosition);
        }
    }

    public class WaitCommandTests
    {
        [Test]
        public void Execute_DoesNothing()
        {
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(3, 4));
            var hp = unit.CurrentHP;
            var pos = unit.GridPosition;

            var cmd = new WaitCommand(unit);
            cmd.Execute();

            Assert.AreEqual(hp, unit.CurrentHP);
            Assert.AreEqual(pos, unit.GridPosition);
        }

        [Test]
        public void Undo_DoesNothing()
        {
            var unit = new UnitInstance("Test", 0, 1, new Vector2Int(3, 4));
            var cmd = new WaitCommand(unit);
            cmd.Execute();
            Assert.DoesNotThrow(() => cmd.Undo());
        }

        [Test]
        public void Description_IsCorrect()
        {
            var unit = new UnitInstance("Agrias", 0, 1, Vector2Int.zero);
            var cmd = new WaitCommand(unit);
            Assert.AreEqual("Agrias waits", cmd.Description);
        }
    }
}
