using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Resolves CT cost for the active unit's turn, checks win/lose conditions,
    /// and advances to the next turn or ends the battle.
    /// </summary>
    public class EndTurnState : IState<BattleContext>
    {
        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            var unit = ctx.ActiveUnit;

            // Resolve CT cost
            CTSystem.ResolveTurn(unit, ctx.ActiveUnitMoved, ctx.ActiveUnitActed);

            // Fire turn ended event
            GameEvents.TurnEnded.Raise(new TurnEndedArgs(
                unit.Id, ctx.ActiveUnitMoved, ctx.ActiveUnitActed));

            Debug.Log($"[EndTurn] {unit.Name} — moved:{ctx.ActiveUnitMoved} acted:{ctx.ActiveUnitActed} CT→{unit.CT}");

            // Check victory/defeat
            if (ctx.IsTeamDefeated(1)) // all enemies dead
            {
                machine.ChangeState(new VictoryState());
                return;
            }

            if (ctx.IsTeamDefeated(0)) // all players dead
            {
                machine.ChangeState(new DefeatState());
                return;
            }

            // Next turn
            machine.ChangeState(new CTAdvanceState());
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine) { }
        public void Exit(BattleContext ctx) { }
    }
}
