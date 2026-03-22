namespace IsoRPG.Core
{
    /// <summary>
    /// Static registry of all game event channels.
    /// Systems raise events here; UI, audio, and VFX subscribe.
    ///
    /// Usage:
    /// <code>
    /// // Raise:
    /// GameEvents.DamageDealt.Raise(new DamageDealtArgs(attackerId, targetId, 42, DamageType.Physical, false));
    ///
    /// // Subscribe (in OnEnable/Start):
    /// GameEvents.DamageDealt.Subscribe(OnDamageDealt);
    ///
    /// // Unsubscribe (in OnDisable/OnDestroy):
    /// GameEvents.DamageDealt.Unsubscribe(OnDamageDealt);
    /// </code>
    /// </summary>
    public static class GameEvents
    {
        // --- Battle Flow ---

        /// <summary>Raised when a battle begins.</summary>
        public static readonly GameEvent<BattleStartedArgs> BattleStarted = new();

        /// <summary>Raised when a battle ends (victory, defeat, or retreat).</summary>
        public static readonly GameEvent<BattleEndedArgs> BattleEnded = new();

        /// <summary>Raised when a unit's active turn begins.</summary>
        public static readonly GameEvent<TurnStartedArgs> TurnStarted = new();

        /// <summary>Raised when a unit's active turn ends.</summary>
        public static readonly GameEvent<TurnEndedArgs> TurnEnded = new();

        // --- Unit Actions ---

        /// <summary>Raised when a unit moves to a new tile.</summary>
        public static readonly GameEvent<UnitMovedArgs> UnitMoved = new();

        /// <summary>Raised when damage is dealt to a unit.</summary>
        public static readonly GameEvent<DamageDealtArgs> DamageDealt = new();

        /// <summary>Raised when a unit is healed.</summary>
        public static readonly GameEvent<HealingDealtArgs> HealingDealt = new();

        /// <summary>Raised when a unit dies.</summary>
        public static readonly GameEvent<UnitDiedArgs> UnitDied = new();

        /// <summary>Raised when an ability is used.</summary>
        public static readonly GameEvent<AbilityUsedArgs> AbilityUsed = new();

        /// <summary>Raised when a status effect is applied.</summary>
        public static readonly GameEvent<StatusAppliedArgs> StatusApplied = new();

        // --- Commands (for UI history / rewind display) ---

        /// <summary>Raised after any command is executed.</summary>
        public static readonly GameEvent<ICommand> CommandExecuted = new();

        /// <summary>Raised after any command is undone.</summary>
        public static readonly GameEvent<ICommand> CommandUndone = new();

        /// <summary>
        /// Clear all event subscribers. Call during scene teardown to prevent
        /// stale listener references surviving scene transitions.
        /// </summary>
        public static void ClearAll()
        {
            BattleStarted.Clear();
            BattleEnded.Clear();
            TurnStarted.Clear();
            TurnEnded.Clear();
            UnitMoved.Clear();
            DamageDealt.Clear();
            HealingDealt.Clear();
            UnitDied.Clear();
            AbilityUsed.Clear();
            StatusApplied.Clear();
            CommandExecuted.Clear();
            CommandUndone.Clear();
        }
    }
}
