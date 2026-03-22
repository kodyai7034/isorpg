using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Represents a scored AI option (move + optional action).
    /// </summary>
    public struct AIOption
    {
        /// <summary>Tile to move to (may be current position for "don't move").</summary>
        public Vector2Int MoveTo;
        /// <summary>Ability to use after moving. Null = wait.</summary>
        public AbilityData Ability;
        /// <summary>Target for the ability. Null if waiting or self-target.</summary>
        public UnitInstance Target;
        /// <summary>Composite utility score. Higher = better.</summary>
        public float Score;
        /// <summary>Estimated damage this option would deal.</summary>
        public int EstimatedDamage;
        /// <summary>Whether this option would kill the target.</summary>
        public bool WouldKill;
    }

    /// <summary>
    /// Utility-based AI that evaluates all possible (move, action) combinations
    /// and picks the best one. Uses hypothetical command execution (execute → score → undo)
    /// to evaluate outcomes without permanent side effects.
    ///
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class AIController
    {
        private const int MaxCombosToEvaluate = 500;

        /// <summary>
        /// Evaluate all options for the given unit and return the best one.
        /// </summary>
        /// <param name="unit">The AI unit taking its turn.</param>
        /// <param name="ctx">Battle context with map, units, RNG.</param>
        /// <param name="profile">AI personality weights.</param>
        /// <param name="abilities">Abilities available to this unit.</param>
        /// <returns>The highest-scoring option.</returns>
        public AIOption EvaluateBestOption(
            UnitInstance unit, BattleContext ctx, AIProfile profile, AbilityData[] abilities)
        {
            var options = new List<AIOption>();

            // Get reachable tiles
            var pathResult = Pathfinder.GetReachableTiles(ctx.Map, unit, ctx.AllUnits);
            var reachableTiles = pathResult.StoppableTiles.Keys.ToList();

            // Pre-compute enemy and ally lists
            var enemies = ctx.AllUnits.Where(u => u.IsAlive && u.Team != unit.Team).ToList();
            var allies = ctx.AllUnits.Where(u => u.IsAlive && u.Team == unit.Team && u != unit).ToList();

            int combosEvaluated = 0;

            foreach (var tile in reachableTiles)
            {
                if (combosEvaluated >= MaxCombosToEvaluate) break;

                float positionScore = ScorePosition(tile, ctx.Map, enemies, profile);

                // Option: move here and wait
                options.Add(new AIOption
                {
                    MoveTo = tile,
                    Ability = null,
                    Target = null,
                    Score = positionScore,
                    EstimatedDamage = 0,
                    WouldKill = false
                });
                combosEvaluated++;

                // Evaluate each ability from this position
                foreach (var ability in abilities)
                {
                    if (combosEvaluated >= MaxCombosToEvaluate) break;
                    if (ability == null) continue;
                    if (ability.MPCost > unit.CurrentMP) continue;

                    if (ability.IsHealing)
                    {
                        // Healing: target wounded allies
                        foreach (var ally in allies)
                        {
                            if (combosEvaluated >= MaxCombosToEvaluate) break;

                            float hpRatio = (float)ally.CurrentHP / ally.Stats.MaxHP;
                            if (hpRatio > profile.HealAllyThreshold) continue;

                            int distance = IsoMath.ManhattanDistance(tile, ally.GridPosition);
                            if (distance > ability.Range) continue;

                            int healAmount = DamageCalculator.CalculateHealing(
                                ability, unit.Stats, unit.Faith);
                            float healScore = healAmount * profile.HealingWeight;

                            options.Add(new AIOption
                            {
                                MoveTo = tile,
                                Ability = ability,
                                Target = ally,
                                Score = positionScore + healScore,
                                EstimatedDamage = 0,
                                WouldKill = false
                            });
                            combosEvaluated++;
                        }
                    }
                    else
                    {
                        // Damage: target enemies
                        foreach (var enemy in enemies)
                        {
                            if (combosEvaluated >= MaxCombosToEvaluate) break;

                            int distance = IsoMath.ManhattanDistance(tile, enemy.GridPosition);
                            if (distance > ability.Range) continue;

                            // LoS check
                            if (ability.RequiresLineOfSight &&
                                !LineOfSight.HasLineOfSight(ctx.Map, tile, enemy.GridPosition))
                                continue;

                            // Estimate damage
                            int heightAdv = ctx.Map.GetElevation(tile) - ctx.Map.GetElevation(enemy.GridPosition);
                            int damage = DamageCalculator.CalculateFinalDamage(
                                ability, unit.Stats, enemy.Stats,
                                unit.Brave, unit.Faith, enemy.Faith, heightAdv);

                            bool wouldKill = damage >= enemy.CurrentHP;

                            // Score
                            float actionScore = damage * profile.DamageWeight;
                            if (wouldKill) actionScore += profile.KillBonus;

                            float threatLevel = enemy.Stats.PhysicalAttack + enemy.Stats.MagicAttack;
                            actionScore += threatLevel * profile.TargetThreatWeight * 0.1f;

                            options.Add(new AIOption
                            {
                                MoveTo = tile,
                                Ability = ability,
                                Target = enemy,
                                Score = positionScore + actionScore,
                                EstimatedDamage = damage,
                                WouldKill = wouldKill
                            });
                            combosEvaluated++;
                        }
                    }
                }
            }

            if (options.Count == 0)
            {
                // Fallback: wait in place
                return new AIOption
                {
                    MoveTo = unit.GridPosition,
                    Score = 0
                };
            }

            // Sort by score descending and return best
            options.Sort((a, b) => b.Score.CompareTo(a.Score));
            return options[0];
        }

        /// <summary>
        /// Score a position based on elevation, distance to enemies, and risk.
        /// </summary>
        private float ScorePosition(Vector2Int tile, BattleMapData map,
            List<UnitInstance> enemies, AIProfile profile)
        {
            float score = 0;

            // High ground bonus
            int elevation = map.GetElevation(tile);
            score += elevation * profile.HighGroundBonus;

            // Distance from enemies
            if (enemies.Count > 0)
            {
                int minDist = int.MaxValue;
                int enemiesInRange = 0;

                foreach (var enemy in enemies)
                {
                    int dist = IsoMath.ManhattanDistance(tile, enemy.GridPosition);
                    if (dist < minDist) minDist = dist;
                    if (dist <= enemy.Stats.Move + 1) enemiesInRange++; // can they reach us?
                }

                score -= minDist * profile.DistanceFromEnemyPenalty;
                score -= enemiesInRange * profile.SelfPreservationWeight;
            }

            return score;
        }
    }
}
