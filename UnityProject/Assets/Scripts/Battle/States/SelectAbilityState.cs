using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player selects which ability to use. For MVP, uses a hardcoded ability list
    /// and keyboard input. Full UI in System 8.
    ///
    /// Temporary: press 1-4 to select ability, Escape to cancel back to SelectAction.
    /// </summary>
    public class SelectAbilityState : IState<BattleContext>
    {
        private AbilityData[] _availableAbilities;

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // MVP: hardcoded ability list until Job System (System 9)
            _availableAbilities = Resources.FindObjectsOfTypeAll<AbilityData>();

            if (_availableAbilities.Length == 0)
            {
                Debug.LogWarning("[SelectAbility] No abilities found. Returning to action selection.");
                machine.ChangeState(new SelectActionState());
                return;
            }

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
            // Cancel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                machine.ChangeState(new SelectActionState());
                return;
            }

            // Select ability by number key
            for (int i = 0; i < _availableAbilities.Length && i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    var ability = _availableAbilities[i];

                    // Check MP
                    if (ctx.ActiveUnit.CurrentMP < ability.MPCost)
                    {
                        Debug.Log($"[SelectAbility] Not enough MP for {ability.AbilityName} (need {ability.MPCost}, have {ctx.ActiveUnit.CurrentMP})");
                        continue;
                    }

                    machine.ChangeState(new ActionTargetState(ability));
                    return;
                }
            }
        }

        public void Exit(BattleContext ctx) { }
    }
}
