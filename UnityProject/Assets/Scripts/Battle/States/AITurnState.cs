using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Stub AI turn: enemy waits. Full AI implementation in System 7.
    /// </summary>
    public class AITurnState : IState<BattleContext>
    {
        private float _delayTimer;
        private const float AIDelay = 0.5f; // Brief pause so player sees it's the AI's turn

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            Debug.Log($"[AI] {ctx.ActiveUnit.Name}'s turn (stub — waiting)");
            _delayTimer = AIDelay;
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer > 0) return;

            // Stub: enemy waits
            var waitCmd = new WaitCommand(ctx.ActiveUnit, ctx.Rng?.Seed ?? 0);
            ctx.CommandHistory.ExecuteCommand(waitCmd);
            ctx.TurnCommandCount++;

            machine.ChangeState(new EndTurnState());
        }

        public void Exit(BattleContext ctx) { }
    }
}
