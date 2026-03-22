using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Battle lost. Fires BattleEnded event and stops the battle loop.
    /// Full defeat screen UI in System 8.
    /// </summary>
    public class DefeatState : IState<BattleContext>
    {
        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            GameEvents.BattleEnded.Raise(new BattleEndedArgs(BattleResult.Defeat, ctx.TurnNumber));
            Debug.Log($"[Battle] DEFEAT after {ctx.TurnNumber} turns.");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Battle loop stops — no transitions
        }

        public void Exit(BattleContext ctx) { }
    }
}
