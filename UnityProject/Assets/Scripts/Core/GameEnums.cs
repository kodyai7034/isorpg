namespace IsoRPG.Core
{
    /// <summary>
    /// Terrain surface type. Determines movement cost, walkability, and visual tile.
    /// </summary>
    public enum TerrainType
    {
        /// <summary>Standard terrain. Move cost 1.</summary>
        Grass,
        /// <summary>Hard surface. Move cost 1.</summary>
        Stone,
        /// <summary>Impassable liquid. Not walkable.</summary>
        Water,
        /// <summary>Loose ground. Move cost 1.</summary>
        Sand,
        /// <summary>Hazardous terrain. Deals fire damage on entry. Not walkable by default.</summary>
        Lava,
        /// <summary>Dense foliage. Move cost 2. Grants cover.</summary>
        Forest
    }

    /// <summary>
    /// Environmental hazard applied to a tile. Triggers effects on units occupying the tile.
    /// </summary>
    public enum HazardType
    {
        /// <summary>No hazard.</summary>
        None,
        /// <summary>Deals poison damage at turn start.</summary>
        Poison,
        /// <summary>Deals fire damage on entry.</summary>
        Fire,
        /// <summary>Restores HP at turn start.</summary>
        Heal
    }

    /// <summary>
    /// Cardinal and intercardinal facing directions.
    /// Ordered clockwise from South for consistent rotation math.
    /// Cast to int (0-7) for rotation arithmetic: rotated = (direction + 2 * rotationIndex) % 8.
    /// </summary>
    public enum Direction
    {
        /// <summary>Toward the camera (default facing).</summary>
        South = 0,
        SouthWest = 1,
        West = 2,
        NorthWest = 3,
        /// <summary>Away from the camera.</summary>
        North = 4,
        NorthEast = 5,
        East = 6,
        SouthEast = 7
    }

    /// <summary>
    /// Character job/class identifier. Determines stat modifiers, equipment, and learnable abilities.
    /// </summary>
    public enum JobId
    {
        /// <summary>Starter class. Balanced stats, basic support abilities.</summary>
        Squire,
        /// <summary>Heavy melee. High HP/PA. Break abilities.</summary>
        Knight,
        /// <summary>Offensive caster. High MA. Elemental AoE spells.</summary>
        BlackMage,
        /// <summary>Healer/support. High MA. Healing and status removal.</summary>
        WhiteMage,
        /// <summary>Ranged physical. High PA/Speed. Height advantage bonus.</summary>
        Archer,
        /// <summary>Utility/speed. Highest Speed. Steal abilities, high evasion.</summary>
        Thief
    }

    /// <summary>
    /// How an ability selects its target(s).
    /// </summary>
    public enum AbilityTargetType
    {
        /// <summary>Targets one unit at a specific tile.</summary>
        Single,
        /// <summary>Targets the caster only.</summary>
        Self,
        /// <summary>Targets all units within an area shape around a tile.</summary>
        Area,
        /// <summary>Targets all units in a line from the caster.</summary>
        Line
    }

    /// <summary>
    /// Damage element type. Determines which defense stat applies.
    /// </summary>
    public enum DamageType
    {
        /// <summary>Reduced by Defense. Scaled by PA and Brave.</summary>
        Physical,
        /// <summary>Reduced by MagicDefense. Scaled by MA and Faith.</summary>
        Magical,
        /// <summary>Ignores all defenses. Fixed damage.</summary>
        Pure
    }

    /// <summary>
    /// Status effect type. Each has unique tick behavior and duration rules.
    /// </summary>
    public enum StatusType
    {
        /// <summary>Deals damage at turn start. Duration-based.</summary>
        Poison,
        /// <summary>Increases Speed by 50%. Duration-based.</summary>
        Haste,
        /// <summary>Decreases Speed by 50%. Duration-based.</summary>
        Slow,
        /// <summary>Reduces physical damage taken by 33%. Duration-based.</summary>
        Protect,
        /// <summary>Reduces magical damage taken by 33%. Duration-based.</summary>
        Shell,
        /// <summary>Restores HP at turn start. Duration-based.</summary>
        Regen
    }

    /// <summary>
    /// Outcome of a completed battle.
    /// </summary>
    public enum BattleResult
    {
        /// <summary>All objectives met. Player wins.</summary>
        Victory,
        /// <summary>All player units defeated. Player loses.</summary>
        Defeat,
        /// <summary>Player chose to withdraw. No rewards.</summary>
        Retreat
    }
}
