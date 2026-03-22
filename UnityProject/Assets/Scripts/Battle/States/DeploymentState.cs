using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Initial battle state. Spawns units at spawn zones, centers camera, then starts battle.
    /// </summary>
    public class DeploymentState : IState<BattleContext>
    {
        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            Debug.Log($"[Battle] Deployment phase — {ctx.AllUnits.Count} units on '{ctx.Map.MapName}'");

            GameEvents.BattleStarted.Raise(new BattleStartedArgs(
                ctx.Map.MapName,
                ctx.Registry.GetTeam(0).Count,
                ctx.Registry.GetTeam(1).Count));

            // Center camera on map middle
            var centerGrid = new Vector2Int(ctx.Map.Width / 2, ctx.Map.Height / 2);
            var centerWorld = IsoMath.GridToWorld(centerGrid, ctx.Map.GetElevation(centerGrid));
            var cam = Camera.main?.GetComponent<Map.BattleCameraController>();
            cam?.SnapTo(centerWorld);

            machine.ChangeState(new CTAdvanceState());
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine) { }
        public void Exit(BattleContext ctx) { }
    }
}
