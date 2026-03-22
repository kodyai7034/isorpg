using IsoRPG.Core;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Command for a unit that chooses to wait (neither move nor act).
    /// This is a no-op on game state — the CT cost is applied by the battle state machine,
    /// not by this command. Exists for command history logging and undo consistency.
    /// </summary>
    public class WaitCommand : ICommand, ICommandMeta
    {
        private readonly UnitInstance _unit;
        private readonly int _rngSeedBefore;

        /// <inheritdoc/>
        public string Description => $"{_unit.Name} waits";

        /// <inheritdoc/>
        public EntityId ActorId => _unit.Id;

        /// <inheritdoc/>
        public int RngSeedBefore => _rngSeedBefore;

        /// <summary>The unit that waited.</summary>
        public UnitInstance Unit => _unit;

        /// <summary>
        /// Create a wait command.
        /// </summary>
        /// <param name="unit">The unit choosing to wait.</param>
        /// <param name="rngSeedBefore">RNG seed state before this command.</param>
        public WaitCommand(UnitInstance unit, int rngSeedBefore = 0)
        {
            _unit = unit;
            _rngSeedBefore = rngSeedBefore;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            // Wait is a no-op. CT cost handled by battle state machine.
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // Nothing to undo.
        }
    }
}
