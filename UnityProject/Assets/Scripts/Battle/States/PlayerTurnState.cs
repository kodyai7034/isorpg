using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Entry point for a player-controlled unit's turn.
    /// Focuses camera on the unit and transitions to action selection.
    /// </summary>
    public class PlayerTurnState : IState<BattleContext>
    {
        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Focus camera on active unit
            var unitView = ctx.GetUnitView(ctx.ActiveUnit.Id);
            if (unitView != null)
            {
                var cam = Camera.main?.GetComponent<Map.BattleCameraController>();
                cam?.FocusOn(unitView.transform.position);
            }

            machine.ChangeState(new SelectActionState());
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine) { }
        public void Exit(BattleContext ctx) { }
    }
}
