using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Executes a MoveCommand and plays the movement animation.
    /// Waits for animation to complete before transitioning to SelectActionState.
    /// </summary>
    public class PerformMoveState : IState<BattleContext>
    {
        private readonly MoveCommand _command;
        private bool _animationComplete;

        public PerformMoveState(MoveCommand command)
        {
            _command = command;
        }

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Execute the command (updates data model)
            ctx.CommandHistory.ExecuteCommand(_command);
            ctx.TurnCommandCount++;
            ctx.ActiveUnitMoved = true;

            // Animate the movement (updates view)
            var unitView = ctx.GetUnitView(ctx.ActiveUnit.Id);
            if (unitView != null)
            {
                var path = new List<Vector2Int>(_command.Path);
                _animationComplete = false;

                var coroutine = unitView.AnimateMovement(path, ctx.Map, 4f);

                // Subscribe to animation completion
                // For now, use a simple frame-delay approach since UnitView.AnimateMovement
                // updates position via the coroutine. We check distance to destination.
                unitView.StartCoroutine(WaitForAnimation(unitView, ctx));
            }
            else
            {
                // No view — skip animation
                _animationComplete = true;
            }
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            if (_animationComplete)
            {
                machine.ChangeState(new SelectActionState());
            }
        }

        public void Exit(BattleContext ctx) { }

        private System.Collections.IEnumerator WaitForAnimation(
            Units.UnitView view, BattleContext ctx)
        {
            var destWorld = IsoMath.GridToWorld(
                _command.Path[_command.Path.Count - 1],
                ctx.Map.GetElevation(_command.Path[_command.Path.Count - 1]));
            destWorld.y += IsoMath.TileHeightHalf * 0.5f;

            // Guard against destroyed view (e.g., unit killed by trap/reaction mid-move)
            while (view != null && Vector3.Distance(view.transform.position, destWorld) > 0.05f)
            {
                yield return null;
            }

            _animationComplete = true;
        }
    }
}
