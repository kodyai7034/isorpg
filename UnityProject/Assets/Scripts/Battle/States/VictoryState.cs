using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Battle won. Fires BattleEnded event and stops the battle loop.
    /// Full victory screen UI in System 8.
    /// </summary>
    public class VictoryState : IState<BattleContext>
    {
        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            GameEvents.BattleEnded.Raise(new BattleEndedArgs(BattleResult.Victory, ctx.TurnNumber));
            Debug.Log($"[Battle] VICTORY after {ctx.TurnNumber} turns!");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Battle loop stops — no transitions
        }

        public void Exit(BattleContext ctx) { }
    }
}
