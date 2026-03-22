namespace IsoRPG.Core
{
    /// <summary>
    /// Represents a reversible game action. All mutations to battle state must flow
    /// through commands to support undo/rewind, AI evaluation, replay, and multiplayer.
    ///
    /// Contract:
    /// - Execute() captures pre-mutation state, then applies the action.
    /// - Undo() restores state to exactly before Execute() was called.
    /// - Execute() followed by Undo() must leave the world byte-identical.
    /// - Commands are immutable after construction (no public setters).
    /// - Commands must not own view/animation logic. Views react via events.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the action, mutating game state.
        /// Must capture all state needed for Undo before mutating.
        /// </summary>
        void Execute();

        /// <summary>
        /// Reverse the action, restoring game state to pre-Execute.
        /// </summary>
        void Undo();

        /// <summary>
        /// Human-readable description for UI, logging, and rewind display.
        /// Example: "Ramza moves to (3,4)" or "Agrias attacks Goblin A for 42 damage".
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Optional metadata interface for commands that participate in the rewind timeline.
    /// Provides actor identity and RNG state for deterministic replay.
    /// </summary>
    public interface ICommandMeta
    {
        /// <summary>Unique ID of the unit that performed this action.</summary>
        EntityId ActorId { get; }

        /// <summary>
        /// The RNG seed state captured before this command executed.
        /// Used to restore deterministic RNG on rewind.
        /// </summary>
        int RngSeedBefore { get; }
    }
}
