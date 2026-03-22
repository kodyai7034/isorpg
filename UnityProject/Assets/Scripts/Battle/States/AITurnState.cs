using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Units;

namespace IsoRPG.Battle.States
{
    /// <summary>
    /// AI-controlled unit's turn. Evaluates options via AIController,
    /// then executes move and action with brief delays so the player
    /// can see what happened.
    /// </summary>
    public class AITurnState : IState<BattleContext>
    {
        private enum Phase { Evaluating, Moving, Acting, Done }

        private Phase _phase;
        private AIOption _chosen;
        private float _timer;
        private bool _animationComplete;
        private AIController _aiController;
        private AIProfile _profile;

        private const float PhaseDelay = 0.4f;

        public void Enter(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            _aiController = new AIController();
            _profile = AIProfile.CreateAggressive(); // default until per-unit profiles

            // Get abilities for this unit (MVP: just Attack)
            var abilities = Resources.FindObjectsOfTypeAll<AbilityData>();
            if (abilities.Length == 0)
            {
                // No abilities — just wait
                Debug.Log($"[AI] {ctx.ActiveUnit.Name} has no abilities — waiting");
                var waitCmd = new WaitCommand(ctx.ActiveUnit, ctx.Rng?.Seed ?? 0);
                ctx.CommandHistory.ExecuteCommand(waitCmd);
                ctx.TurnCommandCount++;
                machine.ChangeState(new EndTurnState());
                return;
            }

            _chosen = _aiController.EvaluateBestOption(ctx.ActiveUnit, ctx, _profile, abilities);

            Debug.Log($"[AI] {ctx.ActiveUnit.Name} chose: move to ({_chosen.MoveTo.x},{_chosen.MoveTo.y})" +
                      (_chosen.Ability != null ? $", use {_chosen.Ability.AbilityName} on {_chosen.Target?.Name}" : ", wait") +
                      $" (score: {_chosen.Score:F1})");

            // Start with moving (or skip if staying in place)
            if (_chosen.MoveTo != ctx.ActiveUnit.GridPosition)
            {
                _phase = Phase.Moving;
                ExecuteMove(ctx);
            }
            else if (_chosen.Ability != null)
            {
                _phase = Phase.Acting;
                _timer = PhaseDelay;
            }
            else
            {
                _phase = Phase.Done;
                ExecuteWait(ctx);
                _timer = PhaseDelay;
            }
        }

        public void Execute(BattleContext ctx, IStateMachine<BattleContext> machine)
        {
            switch (_phase)
            {
                case Phase.Moving:
                    if (_animationComplete)
                    {
                        _timer -= Time.deltaTime;
                        if (_timer <= 0)
                        {
                            if (_chosen.Ability != null)
                            {
                                _phase = Phase.Acting;
                                _timer = PhaseDelay;
                            }
                            else
                            {
                                _phase = Phase.Done;
                                _timer = PhaseDelay;
                            }
                        }
                    }
                    break;

                case Phase.Acting:
                    _timer -= Time.deltaTime;
                    if (_timer <= 0)
                    {
                        ExecuteAction(ctx);
                        _phase = Phase.Done;
                        _timer = PhaseDelay;
                    }
                    break;

                case Phase.Done:
                    _timer -= Time.deltaTime;
                    if (_timer <= 0)
                    {
                        machine.ChangeState(new EndTurnState());
                    }
                    break;
            }
        }

        public void Exit(BattleContext ctx) { }

        private void ExecuteMove(BattleContext ctx)
        {
            var pathResult = Pathfinder.GetReachableTiles(ctx.Map, ctx.ActiveUnit, ctx.AllUnits);
            var path = Pathfinder.ReconstructPath(pathResult, ctx.ActiveUnit.GridPosition, _chosen.MoveTo);

            if (path == null || path.Count == 0)
            {
                _animationComplete = true;
                _timer = PhaseDelay;
                return;
            }

            var moveCmd = new MoveCommand(ctx.ActiveUnit, _chosen.MoveTo, path, ctx.Rng?.Seed ?? 0);
            ctx.CommandHistory.ExecuteCommand(moveCmd);
            ctx.TurnCommandCount++;
            ctx.ActiveUnitMoved = true;

            // Animate
            var unitView = ctx.GetUnitView(ctx.ActiveUnit.Id);
            if (unitView != null)
            {
                _animationComplete = false;
                unitView.StartCoroutine(WaitForMoveAnimation(unitView, ctx));
            }
            else
            {
                _animationComplete = true;
                _timer = PhaseDelay;
            }
        }

        private void ExecuteAction(BattleContext ctx)
        {
            if (_chosen.Target == null || _chosen.Ability == null) return;
            if (!_chosen.Target.IsAlive) return;

            var attackCmd = new AttackCommand(
                ctx.ActiveUnit, _chosen.Target, _chosen.Ability, ctx.Map, ctx.Rng);
            ctx.CommandHistory.ExecuteCommand(attackCmd);
            ctx.TurnCommandCount++;
            ctx.ActiveUnitActed = true;

            Debug.Log($"[AI] {attackCmd.Description}");
        }

        private void ExecuteWait(BattleContext ctx)
        {
            var waitCmd = new WaitCommand(ctx.ActiveUnit, ctx.Rng?.Seed ?? 0);
            ctx.CommandHistory.ExecuteCommand(waitCmd);
            ctx.TurnCommandCount++;
        }

        private System.Collections.IEnumerator WaitForMoveAnimation(UnitView view, BattleContext ctx)
        {
            var destWorld = IsoMath.GridToWorld(
                _chosen.MoveTo, ctx.Map.GetElevation(_chosen.MoveTo));
            destWorld.y += IsoMath.TileHeightHalf * 0.5f;

            while (view != null && Vector3.Distance(view.transform.position, destWorld) > 0.05f)
            {
                yield return null;
            }

            _animationComplete = true;
            _timer = PhaseDelay;
        }
    }
}
