namespace IsoRPG.Core
{
    /// <summary>
    /// Centralized game constants. No magic numbers in game logic — reference these instead.
    /// </summary>
    public static class GameConstants
    {
        // --- Command History ---

        /// <summary>Maximum commands retained in rewind history.</summary>
        public const int MaxCommandHistory = 50;

        // --- CT (Charge Time) System ---

        /// <summary>CT value at which a unit gains an active turn.</summary>
        public const int CTThreshold = 100;

        /// <summary>Maximum CT a unit can retain after resolving their turn.</summary>
        public const int MaxCTAfterAction = 60;

        /// <summary>CT cost when a unit both moves and acts.</summary>
        public const int CTCostMoveAndAct = 100;

        /// <summary>CT cost when a unit only moves or only acts.</summary>
        public const int CTCostSingleAction = 80;

        /// <summary>CT cost when a unit waits (neither moves nor acts).</summary>
        public const int CTCostWait = 60;

        /// <summary>Safety limit for CT tick loop to prevent infinite loops (e.g., all Speed=0).</summary>
        public const int CTTickSafetyLimit = 10000;

        // --- Map & Grid ---

        /// <summary>Maximum tile elevation value.</summary>
        public const int MaxElevation = 15;

        /// <summary>Movement cost for impassable tiles.</summary>
        public const int ImpassableMoveCost = 999;

        /// <summary>Default max depth for sorting order calculation.</summary>
        public const int DefaultMaxSortDepth = 100;

        // --- Unit Stats ---

        /// <summary>Maximum Brave or Faith stat value.</summary>
        public const int MaxBraveOrFaith = 100;

        /// <summary>Minimum Brave or Faith stat value.</summary>
        public const int MinBraveOrFaith = 0;

        /// <summary>Brave threshold below which a unit permanently deserts.</summary>
        public const int BraveDesertThreshold = 5;

        /// <summary>Faith threshold above which a unit permanently deserts.</summary>
        public const int FaithDesertThreshold = 95;

        // --- Combat ---

        /// <summary>Height advantage bonus per elevation level when attacking downhill.</summary>
        public const int HeightAdvantagePerLevel = 5;

        /// <summary>Minimum hit chance (%). Attacks always have at least this chance to land.</summary>
        public const int MinHitChance = 5;

        /// <summary>Maximum hit chance (%). Attacks never exceed this.</summary>
        public const int MaxHitChance = 95;
    }
}
