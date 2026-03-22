using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// Player selects a target for the chosen ability.
    /// Shows ability range overlay (red tiles). Click to confirm, Escape to cancel.
    /// </summary>
    public class ActionTargetState : IState<BattleContext>
    {
        private readonly AbilityData _ability;
        private List<Vector2Int> _targetableTiles;

        public ActionTargetState(AbilityData ability)
        {
            _ability = ability;
        }

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Calculate targetable tiles
            _targetableTiles = LineOfSight.GetTargetableTiles(
                ctx.Map, ctx.ActiveUnit.GridPosition,
                _ability.Range, _ability.RequiresLineOfSight);

            // Show attack range overlay
            ctx.Grid.SetTileOverlay(_targetableTiles, TileVisualState.AttackRange);

            Debug.Log($"[Target] {_ability.AbilityName}: select target ({_targetableTiles.Count} tiles in range)");
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            // Cancel
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                ctx.Grid.ClearAllOverlays();
                machine.ChangeState(new SelectAbilityState());
                return;
            }

            // Select target
            if (Input.GetMouseButtonDown(0))
            {
                var hovered = ctx.Grid.HoveredTile;
                if (hovered.x < 0) return;

                // Check if tile is in range
                if (!_targetableTiles.Contains(hovered)) return;

                // Find target unit at tile
                var target = ctx.Registry.GetAtPosition(hovered);

                if (_ability.IsHealing)
                {
                    // Healing: target must be a living ally
                    if (target == null || target.Team != ctx.ActiveUnit.Team) return;
                }
                else
                {
                    // Damage: target must be a living enemy
                    if (target == null || target.Team == ctx.ActiveUnit.Team) return;
                }

                // Create and execute command
                ctx.Grid.ClearAllOverlays();
                machine.ChangeState(new PerformActionState(_ability, target));
            }
        }

        public void Exit(BattleContext ctx)
        {
            ctx.Grid.ClearAllOverlays();
        }
    }
}
