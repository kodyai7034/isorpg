using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Command to move a unit from its current position to a destination tile.
    /// Captures pre-move state for undo. Fires UnitMoved event on execute.
    ///
    /// Does NOT validate the move — the caller (battle state machine or AI) must
    /// verify the destination is reachable via <see cref="Pathfinder"/> before creating this command.
    /// </summary>
    public class MoveCommand : ICommand, ICommandMeta
    {
        private readonly UnitInstance _unit;
        private readonly Vector2Int _destination;
        private readonly List<Vector2Int> _path;
        private readonly int _rngSeedBefore;

        // Captured pre-state for undo
        private Vector2Int _previousPosition;
        private Direction _previousFacing;

        /// <inheritdoc/>
        public string Description => $"{_unit.Name} moves to ({_destination.x},{_destination.y})";

        /// <inheritdoc/>
        public EntityId ActorId => _unit.Id;

        /// <inheritdoc/>
        public int RngSeedBefore => _rngSeedBefore;

        /// <summary>The path taken (for animation).</summary>
        public IReadOnlyList<Vector2Int> Path => _path.AsReadOnly();

        /// <summary>The unit being moved.</summary>
        public UnitInstance Unit => _unit;

        /// <summary>
        /// Create a move command.
        /// </summary>
        /// <param name="unit">Unit to move.</param>
        /// <param name="destination">Target grid position.</param>
        /// <param name="path">Path of waypoints from start (exclusive) to destination (inclusive).</param>
        /// <param name="rngSeedBefore">RNG seed state before this command (for rewind).</param>
        public MoveCommand(UnitInstance unit, Vector2Int destination, List<Vector2Int> path, int rngSeedBefore = 0)
        {
            _unit = unit;
            _destination = destination;
            _path = path != null ? new List<Vector2Int>(path) : new List<Vector2Int> { destination };
            _rngSeedBefore = rngSeedBefore;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            // Capture pre-state
            _previousPosition = _unit.GridPosition;
            _previousFacing = _unit.Facing;

            // Apply move
            _unit.SetPosition(_destination);

            // Update facing based on the last leg of the path (not overall direction)
            if (_path.Count >= 2)
                _unit.SetFacing(IsoMath.GetDirection(_path[_path.Count - 2], _path[_path.Count - 1]));
            else if (_previousPosition != _destination)
                _unit.SetFacing(IsoMath.GetDirection(_previousPosition, _destination));

            // Fire global event
            GameEvents.UnitMoved.Raise(new UnitMovedArgs(
                _unit.Id, _previousPosition, _destination, _path.ToArray()));
        }

        /// <inheritdoc/>
        public void Undo()
        {
            _unit.SetPosition(_previousPosition);
            _unit.SetFacing(_previousFacing);
        }
    }
}
