namespace IsoRPG.Core
{
    /// <summary>
    /// Static registry of all game event channels.
    /// </summary>
    public static class GameEvents
    {
        // --- Battle Flow ---
        public static readonly GameEvent<BattleStartedArgs> BattleStarted = new();
        public static readonly GameEvent<BattleEndedArgs> BattleEnded = new();
        public static readonly GameEvent<TurnStartedArgs> TurnStarted = new();
        public static readonly GameEvent<TurnEndedArgs> TurnEnded = new();

        // --- Unit Actions ---
        public static readonly GameEvent<UnitMovedArgs> UnitMoved = new();
        public static readonly GameEvent<DamageDealtArgs> DamageDealt = new();
        public static readonly GameEvent<HealingDealtArgs> HealingDealt = new();
        public static readonly GameEvent<UnitDiedArgs> UnitDied = new();
        public static readonly GameEvent<AbilityUsedArgs> AbilityUsed = new();
        public static readonly GameEvent<StatusAppliedArgs> StatusApplied = new();

        // --- UI Requests (battle states → UI) ---
        public static readonly GameEvent<ActionMenuRequestArgs> ShowActionMenu = new();
        public static readonly GameEvent HideActionMenu = new();
        public static readonly GameEvent<CombatMenuRequestArgs> ShowCombatMenu = new();
        public static readonly GameEvent HideCombatMenu = new();
        public static readonly GameEvent<AbilityMenuRequestArgs> ShowAbilityMenu = new();
        public static readonly GameEvent HideAbilityMenu = new();

        // --- UI Responses (UI → battle states) ---
        // Action menu
        public static readonly GameEvent ActionMoveSelected = new();
        public static readonly GameEvent ActionActSelected = new();
        public static readonly GameEvent ActionWaitSelected = new();
        public static readonly GameEvent ActionUndoSelected = new();
        // Combat menu (Attack / Skills / Skip action)
        public static readonly GameEvent CombatAttackSelected = new();
        public static readonly GameEvent CombatSkillsSelected = new();
        public static readonly GameEvent CombatSkipSelected = new();
        public static readonly GameEvent CombatCancelled = new();
        // Skills menu
        public static readonly GameEvent<AbilityData> AbilitySelected = new();
        public static readonly GameEvent AbilitySelectionCancelled = new();
        // Selection context (tile selection states)
        public static readonly GameEvent<SelectionContextArgs> ShowSelectionContext = new();
        public static readonly GameEvent HideSelectionContext = new();
        public static readonly GameEvent SelectionCancelled = new();

        // --- Commands ---
        public static readonly GameEvent<ICommand> CommandExecuted = new();
        public static readonly GameEvent<ICommand> CommandUndone = new();

        public static void ClearAll()
        {
            BattleStarted.Clear(); BattleEnded.Clear();
            TurnStarted.Clear(); TurnEnded.Clear();
            UnitMoved.Clear(); DamageDealt.Clear(); HealingDealt.Clear();
            UnitDied.Clear(); AbilityUsed.Clear(); StatusApplied.Clear();
            ShowActionMenu.Clear(); HideActionMenu.Clear();
            ShowCombatMenu.Clear(); HideCombatMenu.Clear();
            ShowAbilityMenu.Clear(); HideAbilityMenu.Clear();
            ActionMoveSelected.Clear(); ActionActSelected.Clear();
            ActionWaitSelected.Clear(); ActionUndoSelected.Clear();
            CombatAttackSelected.Clear(); CombatSkillsSelected.Clear();
            CombatSkipSelected.Clear(); CombatCancelled.Clear();
            AbilitySelected.Clear(); AbilitySelectionCancelled.Clear();
            ShowSelectionContext.Clear(); HideSelectionContext.Clear();
            SelectionCancelled.Clear();
            CommandExecuted.Clear(); CommandUndone.Clear();
        }
    }
}
