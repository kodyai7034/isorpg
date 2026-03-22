namespace IsoRPG.Battle
{
    /// <summary>
    /// Movement parameters for pathfinding. Extracted from unit stats and modified
    /// by movement abilities (Move+1, Ignore Height, Teleport, etc.) before
    /// being passed to the Pathfinder.
    ///
    /// Separating this from UnitInstance allows the caller to apply ability modifiers
    /// without mutating the unit's base stats.
    /// </summary>
    public struct MovementParams
    {
        /// <summary>Maximum tiles the unit can traverse (after ability modifiers).</summary>
        public int MoveRange;

        /// <summary>Maximum elevation change per step (after ability modifiers).</summary>
        public int JumpHeight;

        /// <summary>If true, elevation differences are ignored for traversal.</summary>
        public bool IgnoreHeight;

        /// <summary>If true, terrain movement cost is ignored (all tiles cost 1).</summary>
        public bool CanFly;

        /// <summary>If true, obstacles and units are ignored for pathing (teleport to any tile in range).</summary>
        public bool CanTeleport;

        /// <summary>
        /// Create movement params from a unit's current stats.
        /// Apply ability modifiers after construction.
        /// </summary>
        public static MovementParams FromUnit(IsoRPG.Units.UnitInstance unit)
        {
            return new MovementParams
            {
                MoveRange = unit.Stats.Move,
                JumpHeight = unit.Stats.Jump,
                IgnoreHeight = false,
                CanFly = false,
                CanTeleport = false
            };
        }
    }
}
