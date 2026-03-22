using NUnit.Framework;
using System.Collections.Generic;
using IsoRPG.Core;

namespace IsoRPG.Tests
{
    /// <summary>Test command that increments/decrements a shared counter.</summary>
    public class TestCommand : ICommand
    {
        private readonly List<int> _log;
        private readonly int _value;

        public string Description => $"Add {_value}";

        public TestCommand(List<int> log, int value)
        {
            _log = log;
            _value = value;
        }

        public void Execute() => _log.Add(_value);
        public void Undo()
        {
            if (_log.Count > 0)
                _log.RemoveAt(_log.Count - 1);
        }
    }

    public class CommandHistoryTests
    {
        [Test]
        public void ExecuteCommand_ExecutesAndRecords()
        {
            var log = new List<int>();
            var history = new CommandHistory();
            history.ExecuteCommand(new TestCommand(log, 10));

            Assert.AreEqual(1, log.Count);
            Assert.AreEqual(10, log[0]);
            Assert.AreEqual(1, history.Count);
        }

        [Test]
        public void Undo_ReversesLastCommand()
        {
            var log = new List<int>();
            var history = new CommandHistory();
            history.ExecuteCommand(new TestCommand(log, 10));
            history.ExecuteCommand(new TestCommand(log, 20));

            Assert.AreEqual(2, log.Count);

            var undone = history.Undo();

            Assert.AreEqual(1, log.Count);
            Assert.AreEqual(10, log[0]);
            Assert.AreEqual("Add 20", undone.Description);
            Assert.AreEqual(1, history.Count);
        }

        [Test]
        public void Undo_EmptyHistory_ReturnsNull()
        {
            var history = new CommandHistory();
            Assert.IsNull(history.Undo());
        }

        [Test]
        public void UndoMultiple_UndoesCorrectCount()
        {
            var log = new List<int>();
            var history = new CommandHistory();
            history.ExecuteCommand(new TestCommand(log, 1));
            history.ExecuteCommand(new TestCommand(log, 2));
            history.ExecuteCommand(new TestCommand(log, 3));

            history.UndoMultiple(2);

            Assert.AreEqual(1, log.Count);
            Assert.AreEqual(1, log[0]);
            Assert.AreEqual(1, history.Count);
        }

        [Test]
        public void UndoMultiple_MoreThanAvailable_UndoesAll()
        {
            var log = new List<int>();
            var history = new CommandHistory();
            history.ExecuteCommand(new TestCommand(log, 1));

            history.UndoMultiple(100);

            Assert.AreEqual(0, log.Count);
            Assert.AreEqual(0, history.Count);
        }

        [Test]
        public void MaxHistory_EvictsOldest()
        {
            var log = new List<int>();
            var history = new CommandHistory(maxHistory: 3);

            history.ExecuteCommand(new TestCommand(log, 1));
            history.ExecuteCommand(new TestCommand(log, 2));
            history.ExecuteCommand(new TestCommand(log, 3));
            history.ExecuteCommand(new TestCommand(log, 4)); // evicts command 1

            Assert.AreEqual(3, history.Count);
            Assert.AreEqual("Add 2", history.History[0].Description);
            Assert.AreEqual("Add 4", history.History[2].Description);
        }

        [Test]
        public void Clear_RemovesAllHistory()
        {
            var log = new List<int>();
            var history = new CommandHistory();
            history.ExecuteCommand(new TestCommand(log, 1));
            history.ExecuteCommand(new TestCommand(log, 2));

            history.Clear();

            Assert.AreEqual(0, history.Count);
            Assert.IsNull(history.Undo());
        }

        [Test]
        public void Events_FireOnExecuteAndUndo()
        {
            var log = new List<int>();
            var history = new CommandHistory();
            ICommand executedCmd = null;
            ICommand undoneCmd = null;

            history.OnCommandExecuted += cmd => executedCmd = cmd;
            history.OnCommandUndone += cmd => undoneCmd = cmd;

            var command = new TestCommand(log, 42);
            history.ExecuteCommand(command);
            Assert.AreSame(command, executedCmd);

            history.Undo();
            Assert.AreSame(command, undoneCmd);
        }

        [Test]
        public void Constructor_MaxHistoryLessThanOne_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CommandHistory(0));
        }

        [Test]
        public void ExecuteCommand_Null_Throws()
        {
            var history = new CommandHistory();
            Assert.Throws<System.ArgumentNullException>(() => history.ExecuteCommand(null));
        }

        [Test]
        public void History_ReturnsReadOnlyList()
        {
            var log = new List<int>();
            var history = new CommandHistory();
            history.ExecuteCommand(new TestCommand(log, 1));

            var readOnly = history.History;
            Assert.AreEqual(1, readOnly.Count);
        }
    }
}
