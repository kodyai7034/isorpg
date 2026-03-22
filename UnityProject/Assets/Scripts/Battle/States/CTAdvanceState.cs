using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Ticks CT for all units until one reaches the threshold.
    /// Routes to PlayerTurnState or AITurnState based on the active unit's team.
    /// </summary>
    public class CTAdvanceState : IState<BattleContext>
    {
        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            var activeUnit = CTSystem.AdvanceTick(ctx.AllUnits);

            if (activeUnit == null)
            {
                Debug.LogError("[CTAdvance] No unit gained a turn. Ending battle.");
                machine.ChangeState(new DefeatState());
                return;
            }

            ctx.ActiveUnit = activeUnit;
            ctx.ActiveUnitMoved = false;
            ctx.ActiveUnitActed = false;
            ctx.TurnCommandCount = 0;
            ctx.TurnNumber++;

            GameEvents.TurnStarted.Raise(new TurnStartedArgs(activeUnit.Id, ctx.TurnNumber));

            Debug.Log($"[Turn {ctx.TurnNumber}] {activeUnit.Name}'s turn (Team {activeUnit.Team}, CT={activeUnit.CT}, Speed={activeUnit.Stats.Speed})");

            if (activeUnit.Team == 0)
                machine.ChangeState(new PlayerTurnState());
            else
                machine.ChangeState(new AITurnState());
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine) { }
        public void Exit(BattleContext ctx) { }
    }
}
