using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Units;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Executes an AttackCommand for the selected ability and target.
    /// Shows a brief pause for the player to see the result, then transitions
    /// back to SelectActionState.
    /// </summary>
    public class PerformActionState : IState<BattleContext>
    {
        private readonly AbilityData _ability;
        private readonly UnitInstance _target;
        private float _displayTimer;
        private const float DisplayDuration = 0.8f;

        public PerformActionState(AbilityData ability, UnitInstance target)
        {
            _ability = ability;
            _target = target;
        }

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            var cmd = new AttackCommand(
                ctx.ActiveUnit, _target, _ability, ctx.Map, ctx.Rng);

            ctx.CommandHistory.ExecuteCommand(cmd);
            ctx.TurnCommandCount++;
            ctx.ActiveUnitActed = true;

            Debug.Log($"[Action] {cmd.Description}");

            _displayTimer = DisplayDuration;
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _displayTimer -= Time.deltaTime;
            if (_displayTimer <= 0)
            {
                // Check victory/defeat immediately after a kill
                if (ctx.IsTeamDefeated(1))
                {
                    machine.ChangeState(new VictoryState());
                    return;
                }
                if (ctx.IsTeamDefeated(0))
                {
                    machine.ChangeState(new DefeatState());
                    return;
                }

                machine.ChangeState(new SelectActionState());
            }
        }

        public void Exit(BattleContext ctx) { }
    }
}
