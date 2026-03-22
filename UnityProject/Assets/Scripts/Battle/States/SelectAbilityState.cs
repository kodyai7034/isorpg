using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player selects an ability via on-screen UI menu.
    /// No keyboard input — all actions flow through GameEvents from AbilityMenuUI.
    /// </summary>
    public class SelectAbilityState : IState<BattleContext>
    {
        private IStateMachine<BattleContext> _machine;
        private bool _actionTaken;

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _machine = machine;
            _actionTaken = false;

            var abilities = ctx.DefaultAbilities;
            if (abilities == null || abilities.Length == 0)
            {
                Debug.LogWarning("[SelectAbility] No abilities available.");
                machine.ChangeState(new SelectActionState());
                return;
            }

            GameEvents.ShowAbilityMenu.Raise(new AbilityMenuRequestArgs(abilities, ctx.ActiveUnit.CurrentMP));
            GameEvents.AbilitySelected.Subscribe(OnAbilitySelected);
            GameEvents.AbilitySelectionCancelled.Subscribe(OnCancelled);

            Debug.Log("[SelectAbility] Waiting for ability selection...");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Empty — all input is event-driven from UI
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
