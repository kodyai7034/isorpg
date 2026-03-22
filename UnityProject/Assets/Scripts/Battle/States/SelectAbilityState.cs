using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player selects which ability to use.
    /// Supports both UI (AbilityMenuUI) and keyboard shortcuts (1-4, Escape).
    /// </summary>
    public class SelectAbilityState : IState<BattleContext>
    {
        private AbilityData[] _availableAbilities;
        private IStateMachine<BattleContext> _machine;
        private bool _actionTaken;

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _machine = machine;
            _actionTaken = false;

            // Use context's default abilities until Job System provides per-unit lists
            _availableAbilities = ctx.DefaultAbilities;

            if (_availableAbilities == null || _availableAbilities.Length == 0)
            {
                Debug.LogWarning("[SelectAbility] No abilities available.");
                machine.ChangeState(new SelectActionState());
                return;
            }

            // Request UI to show ability menu via event
            GameEvents.ShowAbilityMenu.Raise(new AbilityMenuRequestArgs(
                _availableAbilities, ctx.ActiveUnit.CurrentMP));

            // Subscribe to UI response events
            GameEvents.AbilitySelected.Subscribe(OnAbilitySelected);
            GameEvents.AbilitySelectionCancelled.Subscribe(OnCancelled);

            // Log for keyboard fallback
            string list = "[Abilities] ";
            for (int i = 0; i < _availableAbilities.Length && i < 4; i++)
            {
                var a = _availableAbilities[i];
                list += $"[{i + 1}]{a.AbilityName}(MP:{a.MPCost}) ";
            }
            list += "[Esc]Cancel";
            Debug.Log(list);
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            if (_actionTaken) return;

            // Keyboard cancel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancelled();
                return;
            }

            // Keyboard ability selection (1-4)
            for (int i = 0; i < _availableAbilities.Length && i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    var ability = _availableAbilities[i];
                    if (ctx.ActiveUnit.CurrentMP >= ability.MPCost)
                        OnAbilitySelected(ability);
                    else
                        Debug.Log($"[SelectAbility] Not enough MP for {ability.AbilityName}");
                    return;
                }
            }
        }

        public void Exit(BattleContext ctx)
        {
            GameEvents.AbilitySelected.Unsubscribe(OnAbilitySelected);
            GameEvents.AbilitySelectionCancelled.Unsubscribe(OnCancelled);
            GameEvents.HideAbilityMenu.Raise();
        }

        private void OnAbilitySelected(AbilityData ability)
        {
            if (_actionTaken) return;
            _actionTaken = true;
            _machine.ChangeState(new ActionTargetState(ability));
        }

        private void OnCancelled()
        {
            if (_actionTaken) return;
            _actionTaken = true;
            _machine.ChangeState(new SelectActionState());
        }
    }
}
