using System;
using System.Collections.Generic;

namespace IsoRPG.Core
{
    /// <summary>
    /// Stack-based history of executed commands with configurable max capacity.
    /// Supports undo, multi-undo, and fires events for UI/logging subscribers.
    ///
    /// When max capacity is exceeded, the oldest command is evicted (FIFO).
    /// Note: evicted commands cannot be undone.
    /// </summary>
    public class CommandHistory : ICommandHistory
    {
        private readonly List<ICommand> _history;

        /// <inheritdoc/>
        public int MaxHistory { get; }

        /// <inheritdoc/>
        public int Count => _history.Count;

        /// <inheritdoc/>
        public IReadOnlyList<ICommand> History => _history.AsReadOnly();

        /// <inheritdoc/>
        public event Action<ICommand> OnCommandExecuted;

        /// <inheritdoc/>
        public event Action<ICommand> OnCommandUndone;

        /// <summary>
        /// Create a command history with the specified max capacity.
        /// </summary>
        /// <param name="maxHistory">Maximum commands to retain. Oldest evicted when exceeded.</param>
        /// <exception cref="ArgumentOutOfRangeException">If maxHistory is less than 1.</exception>
        public CommandHistory(int maxHistory = GameConstants.MaxCommandHistory)
        {
            if (maxHistory < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHistory), "Max history must be at least 1.");

            MaxHistory = maxHistory;
            _history = new List<ICommand>(maxHistory);
        }

        /// <inheritdoc/>
        public void ExecuteCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Execute();

            _history.Add(command);

            // Evict oldest if over capacity
            while (_history.Count > MaxHistory)
            {
                _history.RemoveAt(0);
            }

            OnCommandExecuted?.Invoke(command);
            GameEvents.CommandExecuted.Raise(command);
        }

        /// <inheritdoc/>
        public ICommand Undo()
        {
            if (_history.Count == 0)
                return null;

            int lastIndex = _history.Count - 1;
            var command = _history[lastIndex];
            _history.RemoveAt(lastIndex);

            command.Undo();

            OnCommandUndone?.Invoke(command);
            GameEvents.CommandUndone.Raise(command);

            return command;
        }

        /// <inheritdoc/>
        public void UndoMultiple(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative.");

            int toUndo = Math.Min(count, _history.Count);
            for (int i = 0; i < toUndo; i++)
            {
                Undo();
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _history.Clear();
        }
    }

    /// <summary>
    /// Interface for command history to support testing and dependency injection.
    /// </summary>
    public interface ICommandHistory
    {
        /// <summary>Execute a command and push it onto the history stack.</summary>
        void ExecuteCommand(ICommand command);

        /// <summary>Undo the most recent command. Returns the undone command, or null if empty.</summary>
        ICommand Undo();

        /// <summary>Undo the last N commands. Stops if history runs out.</summary>
        void UndoMultiple(int count);

        /// <summary>All commands in execution order (oldest first).</summary>
        IReadOnlyList<ICommand> History { get; }

        /// <summary>Number of undoable commands.</summary>
        int Count { get; }

        /// <summary>Maximum commands retained before eviction.</summary>
        int MaxHistory { get; }

        /// <summary>Remove all history.</summary>
        void Clear();

        /// <summary>Fired after a command is executed.</summary>
        event Action<ICommand> OnCommandExecuted;

        /// <summary>Fired after a command is undone.</summary>
        event Action<ICommand> OnCommandUndone;
    }
}
