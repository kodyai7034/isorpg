namespace IsoRPG.Core
{
    public enum TerrainType
    {
        Grass,
        Stone,
        Water,
        Sand,
        Lava,
        Forest
    }

    public enum HazardType
    {
        None,
        Poison,
        Fire,
        Heal
    }

    public enum Direction
    {
        South,
        SouthWest,
        West,
        NorthWest,
        North,
        NorthEast,
        East,
        SouthEast
    }

    public enum JobId
    {
        Squire,
        Knight,
        BlackMage,
        WhiteMage,
        Archer,
        Thief
    }

    public enum AbilityTargetType
    {
        Single,
        Self,
        Area,
        Line
    }

    public enum DamageType
    {
        Physical,
        Magical,
        Pure
    }

    public enum StatusType
    {
        Poison,
        Haste,
        Slow,
        Protect,
        Shell,
        Regen
    }
}
