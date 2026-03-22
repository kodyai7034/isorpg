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

            // Debug: log world positions of tiles and units
            var corner0 = IsoMath.GridToWorld(0, 0, 0);
            var corner1 = IsoMath.GridToWorld(ctx.Map.Width - 1, ctx.Map.Height - 1, 0);
            var centerGrid = new Vector2Int(ctx.Map.Width / 2, ctx.Map.Height / 2);
            var centerWorld = IsoMath.GridToWorld(centerGrid, ctx.Map.GetElevation(centerGrid));
            Debug.Log($"[Deploy] Grid corners: (0,0)={corner0} to ({ctx.Map.Width-1},{ctx.Map.Height-1})={corner1}, center={centerWorld}");
            Debug.Log($"[Deploy] Camera at: {Camera.main?.transform.position}, orthoSize={Camera.main?.orthographicSize}");

            // Center camera on map
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(centerWorld.x, centerWorld.y, -10);
                Camera.main.orthographicSize = 4f;
                Debug.Log($"[Deploy] Camera moved to: {Camera.main.transform.position}");
            }

            machine.ChangeState(new CTAdvanceState());
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine) { }
        public void Exit(BattleContext ctx) { }
    }
}
