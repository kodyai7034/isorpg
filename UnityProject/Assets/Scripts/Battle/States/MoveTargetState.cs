using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player selects a destination tile for movement.
    /// Shows movement range overlay, path preview on hover, and SelectionContextUI.
    /// Left-click confirms. Right-click or Cancel button cancels.
    /// </summary>
    public class MoveTargetState : IState<BattleContext>
    {
        private PathfindingResult _result;
        private IStateMachine<BattleContext> _machine;
        private BattleContext _ctx;
        private bool _actionTaken;

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _ctx = ctx;
            _machine = machine;
            _actionTaken = false;

            _result = ctx.MovementController.ShowMovementRange(
                ctx.ActiveUnit, ctx.Map, ctx.AllUnits, ctx.Grid);

            // Show selection context UI
            GameEvents.HideActionMenu.Raise();
            GameEvents.ShowSelectionContext.Raise(new SelectionContextArgs(
                "Select move destination",
                "Right-click or Cancel to go back",
                SelectionMode.Move));

            GameEvents.SelectionCancelled.Subscribe(OnCancelled);

            Debug.Log($"[Move] {ctx.ActiveUnit.Name}: select destination");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            if (_actionTaken) return;

            // Path preview on hover
            var hovered = ctx.Grid.HoveredTile;
            if (hovered.x >= 0)
            {
                ctx.MovementController.PreviewPathTo(ctx.Grid, ctx.ActiveUnit.GridPosition, hovered);
            }

            // Right-click cancel
            if (Input.GetMouseButtonDown(1))
            {
                OnCancelled();
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
                        _actionTaken = true;
                        // SFX handled by UI layer via events
                        machine.ChangeState(new PerformMoveState(cmd));
                    }
                }
            }
        }

        public void Exit(BattleContext ctx)
        {
            GameEvents.SelectionCancelled.Unsubscribe(OnCancelled);
            GameEvents.HideSelectionContext.Raise();
            ctx.MovementController.ClearOverlays(ctx.Grid);
        }

        private void OnCancelled()
        {
            if (_actionTaken) return;
            _actionTaken = true;
            _machine.ChangeState(new SelectActionState());
        }
    }
}
