using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player selects a destination tile for movement.
    /// Shows movement range overlay and path preview on hover.
    /// Left-click confirms, right-click/Escape cancels.
    /// </summary>
    public class MoveTargetState : IState<BattleContext>
    {
        private PathfindingResult _result;

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _result = ctx.MovementController.ShowMovementRange(
                ctx.ActiveUnit, ctx.Map, ctx.AllUnits, ctx.Grid);

            Debug.Log($"[Move] {ctx.ActiveUnit.Name}: select destination (click tile, Esc to cancel)");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Path preview on hover
            var hovered = ctx.Grid.HoveredTile;
            if (hovered.x >= 0)
            {
                ctx.MovementController.PreviewPathTo(ctx.Grid, ctx.ActiveUnit.GridPosition, hovered);
            }

            // Cancel
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                ctx.MovementController.ClearOverlays(ctx.Grid);
                machine.ChangeState(new SelectActionState());
                return;
            }

            // Confirm destination
            if (Input.GetMouseButtonDown(0) && hovered.x >= 0)
            {
                if (_result != null && _result.CanMoveTo(hovered) && hovered != ctx.ActiveUnit.GridPosition)
                {
                    var cmd = ctx.MovementController.CreateMoveCommand(
                        ctx.ActiveUnit, hovered, ctx.Rng);

                    if (cmd != null)
                    {
                        ctx.MovementController.ClearOverlays(ctx.Grid);
                        machine.ChangeState(new PerformMoveState(cmd));
                        return;
                    }
                }
            }
        }

        public void Exit(BattleContext ctx)
        {
            ctx.MovementController.ClearOverlays(ctx.Grid);
        }
    }
}
