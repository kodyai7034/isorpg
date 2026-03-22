using UnityEngine;
using IsoRPG.Core;
using IsoRPG.UI;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player chooses an action: Move, Act, Wait, or Undo.
    /// Supports both UI button clicks (via ActionMenuUI events) and keyboard shortcuts.
    /// </summary>
    public class SelectActionState : IState<BattleContext>
    {
        private IStateMachine<BattleContext> _machine;
        private BattleContext _ctx;
        private bool _actionTaken;

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _ctx = ctx;
            _machine = machine;
            _actionTaken = false;

            // Show UI menu if available
            var ui = UIManager.Instance;
            if (ui != null && ui.ActionMenu != null)
            {
                ui.ActionMenu.Show(
                    canMove: !ctx.ActiveUnitMoved,
                    canAct: !ctx.ActiveUnitActed,
                    canUndo: ctx.TurnCommandCount > 0);

                ui.ActionMenu.OnMoveSelected += OnMoveSelected;
                ui.ActionMenu.OnActSelected += OnActSelected;
                ui.ActionMenu.OnWaitSelected += OnWaitSelected;
                ui.ActionMenu.OnUndoSelected += OnUndoSelected;
            }

            // Show unit info
            if (ui != null && ui.UnitInfoPanel != null)
                ui.UnitInfoPanel.ShowUnit(ctx.ActiveUnit);

            Debug.Log($"[Select] {ctx.ActiveUnit.Name}: " +
                (!ctx.ActiveUnitMoved ? "[M]ove " : "") +
                (!ctx.ActiveUnitActed ? "[A]ct " : "") +
                "[W]ait " +
                (ctx.TurnCommandCount > 0 ? "[U]ndo " : ""));
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            if (_actionTaken) return;

            // Keyboard shortcuts (retained alongside UI buttons)
            if (!ctx.ActiveUnitMoved && Input.GetKeyDown(KeyCode.M))
                OnMoveSelected();
            else if (!ctx.ActiveUnitActed && Input.GetKeyDown(KeyCode.A))
                OnActSelected();
            else if (Input.GetKeyDown(KeyCode.W))
                OnWaitSelected();
            else if (ctx.TurnCommandCount > 0 && Input.GetKeyDown(KeyCode.U))
                OnUndoSelected();
        }

        public void Exit(BattleContext ctx)
        {
            // Unsubscribe from UI
            var ui = UIManager.Instance;
            if (ui != null && ui.ActionMenu != null)
            {
                ui.ActionMenu.OnMoveSelected -= OnMoveSelected;
                ui.ActionMenu.OnActSelected -= OnActSelected;
                ui.ActionMenu.OnWaitSelected -= OnWaitSelected;
                ui.ActionMenu.OnUndoSelected -= OnUndoSelected;
                ui.ActionMenu.Hide();
            }
        }

        private void OnMoveSelected()
        {
            if (_actionTaken || _ctx.ActiveUnitMoved) return;
            _actionTaken = true;
            _machine.ChangeState(new MoveTargetState());
        }

        private void OnActSelected()
        {
            if (_actionTaken || _ctx.ActiveUnitActed) return;
            _actionTaken = true;
            _machine.ChangeState(new SelectAbilityState());
        }

        private void OnWaitSelected()
        {
            if (_actionTaken) return;
            _actionTaken = true;

            var waitCmd = new WaitCommand(_ctx.ActiveUnit, _ctx.Rng?.Seed ?? 0);
            _ctx.CommandHistory.ExecuteCommand(waitCmd);
            _ctx.TurnCommandCount++;
            _machine.ChangeState(new EndTurnState());
        }

        private void OnUndoSelected()
        {
            if (_actionTaken || _ctx.TurnCommandCount <= 0) return;
            _actionTaken = true;

            var undone = _ctx.CommandHistory.Undo();
            _ctx.TurnCommandCount--;

            switch (undone)
            {
                case MoveCommand:
                    _ctx.ActiveUnitMoved = false;
                    break;
                case AttackCommand:
                    _ctx.ActiveUnitActed = false;
                    break;
            }

            Debug.Log($"[Undo] Reverted: {undone?.Description}");
            _machine.ChangeState(new SelectActionState());
        }
    }
}
