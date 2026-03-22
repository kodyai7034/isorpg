using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player chooses an action: Move, Act (stub), Wait, or Undo.
    /// Temporary keyboard input until UI System (System 8) is built.
    /// </summary>
    public class SelectActionState : IState<BattleContext>
    {
        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            var unit = ctx.ActiveUnit;
            string options = "";
            if (!ctx.ActiveUnitMoved) options += "[M]ove ";
            if (!ctx.ActiveUnitActed) options += "[A]ct(stub) ";
            options += "[W]ait ";
            if (ctx.TurnCommandCount > 0) options += "[U]ndo ";

            Debug.Log($"[Select] {unit.Name}: {options}");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Move
            if (!ctx.ActiveUnitMoved && Input.GetKeyDown(KeyCode.M))
            {
                machine.ChangeState(new MoveTargetState());
                return;
            }

            // Act
            if (!ctx.ActiveUnitActed && Input.GetKeyDown(KeyCode.A))
            {
                machine.ChangeState(new SelectAbilityState());
                return;
            }

            // Wait
            if (Input.GetKeyDown(KeyCode.W))
            {
                var waitCmd = new WaitCommand(ctx.ActiveUnit, ctx.Rng?.Seed ?? 0);
                ctx.CommandHistory.ExecuteCommand(waitCmd);
                ctx.TurnCommandCount++;
                machine.ChangeState(new EndTurnState());
                return;
            }

            // Undo
            if (ctx.TurnCommandCount > 0 && Input.GetKeyDown(KeyCode.U))
            {
                var undone = ctx.CommandHistory.Undo();
                ctx.TurnCommandCount--;

                // Reset action flags based on undone command type
                switch (undone)
                {
                    case MoveCommand:
                        ctx.ActiveUnitMoved = false;
                        break;
                    case AttackCommand:
                        ctx.ActiveUnitActed = false;
                        break;
                }

                Debug.Log($"[Undo] Reverted: {undone?.Description}");
                // Stay in SelectActionState — re-enter to refresh prompt
                machine.ChangeState(new SelectActionState());
                return;
            }
        }

        public void Exit(BattleContext ctx) { }
    }
}
