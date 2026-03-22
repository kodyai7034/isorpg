using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player chose Act → shows Combat Menu: Attack, Skills, Wait (skip action).
    /// Attack → target selection for basic melee.
    /// Skills → opens skill list (SelectAbilityState).
    /// Wait → skip action phase, return to action menu with Act greyed.
    /// Cancel → return to action menu.
    /// </summary>
    public class CombatMenuState : IState<BattleContext>
    {
        private IStateMachine<BattleContext> _machine;
        private BattleContext _ctx;
        private bool _actionTaken;

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _ctx = ctx;
            _machine = machine;
            _actionTaken = false;

            // Find if unit has skills (non-Attack abilities)
            bool hasSkills = false;
            if (ctx.DefaultAbilities != null)
            {
                foreach (var a in ctx.DefaultAbilities)
                {
                    if (a != null && a.AbilityName != "Attack" && ctx.ActiveUnit.CurrentMP >= a.MPCost)
                    {
                        hasSkills = true;
                        break;
                    }
                }
            }

            GameEvents.ShowCombatMenu.Raise(new CombatMenuRequestArgs(hasSkills, ctx.ActiveUnit.CurrentMP));

            GameEvents.CombatAttackSelected.Subscribe(OnAttackSelected);
            GameEvents.CombatSkillsSelected.Subscribe(OnSkillsSelected);
            GameEvents.CombatSkipSelected.Subscribe(OnSkipSelected);
            GameEvents.CombatCancelled.Subscribe(OnCancelled);

            Debug.Log("[Combat] Attack / Skills / Skip action");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Event-driven — empty
        }

        public void Exit(BattleContext ctx)
        {
            GameEvents.CombatAttackSelected.Unsubscribe(OnAttackSelected);
            GameEvents.CombatSkillsSelected.Unsubscribe(OnSkillsSelected);
            GameEvents.CombatSkipSelected.Unsubscribe(OnSkipSelected);
            GameEvents.CombatCancelled.Unsubscribe(OnCancelled);
            GameEvents.HideCombatMenu.Raise();
        }

        private void OnAttackSelected()
        {
            if (_actionTaken) return;
            _actionTaken = true;

            // Find the basic Attack ability
            AbilityData attackAbility = null;
            if (_ctx.DefaultAbilities != null)
            {
                foreach (var a in _ctx.DefaultAbilities)
                {
                    if (a != null && a.AbilityName == "Attack")
                    {
                        attackAbility = a;
                        break;
                    }
                }
            }

            if (attackAbility != null)
                _machine.ChangeState(new ActionTargetState(attackAbility));
            else
                Debug.LogWarning("[Combat] No Attack ability found!");
        }

        private void OnSkillsSelected()
        {
            if (_actionTaken) return;
            _actionTaken = true;
            _machine.ChangeState(new SelectAbilityState());
        }

        private void OnSkipSelected()
        {
            if (_actionTaken) return;
            _actionTaken = true;

            // Skip the action phase — mark as acted but don't execute anything
            _ctx.ActiveUnitActed = true;
            Debug.Log("[Combat] Skipped action phase");
            _machine.ChangeState(new SelectActionState());
        }

        private void OnCancelled()
        {
            if (_actionTaken) return;
            _actionTaken = true;
            _machine.ChangeState(new SelectActionState());
        }
    }
}
