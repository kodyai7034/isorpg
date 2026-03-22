using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player selects a target for the chosen ability.
    /// Shows ability range overlay and SelectionContextUI with Cancel button.
    /// Left-click valid target confirms. Right-click or Cancel button cancels.
    /// </summary>
    public class ActionTargetState : IState<BattleContext>
    {
        private readonly AbilityData _ability;
        private List<Vector2Int> _targetableTiles;
        private IStateMachine<BattleContext> _machine;
        private BattleContext _ctx;
        private bool _actionTaken;

        public ActionTargetState(AbilityData ability)
        {
            _ability = ability;
        }

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _ctx = ctx;
            _machine = machine;
            _actionTaken = false;

            _targetableTiles = LineOfSight.GetTargetableTiles(
                ctx.Map, ctx.ActiveUnit.GridPosition,
                _ability.Range, _ability.RequiresLineOfSight);

            ctx.Grid.SetTileOverlay(_targetableTiles, TileVisualState.AttackRange);

            // Show selection context
            var mode = _ability.IsHealing ? SelectionMode.Heal : SelectionMode.Attack;
            string label = _ability.IsHealing
                ? $"{_ability.AbilityName}: select ally"
                : $"{_ability.AbilityName}: select target";

            GameEvents.HideCombatMenu.Raise();
            GameEvents.HideAbilityMenu.Raise();
            GameEvents.ShowSelectionContext.Raise(new SelectionContextArgs(
                label, "Right-click or Cancel to go back", mode));

            GameEvents.SelectionCancelled.Subscribe(OnCancelled);

            Debug.Log($"[Target] {_ability.AbilityName}: select target ({_targetableTiles.Count} tiles in range)");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            if (_actionTaken) return;

            // Right-click cancel
            if (Input.GetMouseButtonDown(1))
            {
                OnCancelled();
                return;
            }

            // Select target
            if (Input.GetMouseButtonDown(0))
            {
                var hovered = ctx.Grid.HoveredTile;
                if (hovered.x < 0) return;
                if (!_targetableTiles.Contains(hovered)) return;

                var target = ctx.Registry.GetAtPosition(hovered);

                if (_ability.IsHealing)
                {
                    if (target == null || target.Team != ctx.ActiveUnit.Team) return;
                }
                else
                {
                    if (target == null || target.Team == ctx.ActiveUnit.Team) return;
                }

                _actionTaken = true;
                SFXManager.Instance?.PlayConfirm();
                machine.ChangeState(new PerformActionState(_ability, target));
            }
        }

        public void Exit(BattleContext ctx)
        {
            GameEvents.SelectionCancelled.Unsubscribe(OnCancelled);
            GameEvents.HideSelectionContext.Raise();
            ctx.Grid.ClearAllOverlays();
        }

        private void OnCancelled()
        {
            if (_actionTaken) return;
            _actionTaken = true;
            SFXManager.Instance?.PlayCancel();
            _machine.ChangeState(new CombatMenuState());
        }
    }
}
