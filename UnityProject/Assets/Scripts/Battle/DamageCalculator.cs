using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Units;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Pure static damage/healing/hit-chance formulas. No side effects, no state.
    /// All formulas are deterministic given the same inputs.
    ///
    /// Physical: scales with PA and Resolve (Brave equivalent).
    /// Magical: scales with MA and Attunement (Faith equivalent) for both caster and target.
    /// Pure: ignores all defenses.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Calculate raw damage before defense reduction.
        /// </summary>
        /// <param name="ability">The ability being used.</param>
        /// <param name="attacker">Attacker's computed stats.</param>
        /// <param name="resolve">Attacker's Resolve (0-100). Affects physical damage.</param>
        /// <param name="attunement">Attacker's Attunement (0-100). Affects magical damage.</param>
        /// <returns>Raw damage value (before defense).</returns>
        public static int CalculateRawDamage(AbilityData ability, ComputedStats attacker,
            int resolve, int attunement)
        {
            float baseDamage;

            switch (ability.DamageType)
            {
                case DamageType.Physical:
                    baseDamage = attacker.PhysicalAttack * ability.Power / 10f;
                    baseDamage *= resolve / 100f;
                    break;

                case DamageType.Magical:
                    baseDamage = attacker.MagicAttack * ability.Power / 10f;
                    baseDamage *= attunement / 100f;
                    break;

                case DamageType.Pure:
                    baseDamage = ability.Power;
                    break;

                default:
                    baseDamage = ability.Power;
                    break;
            }

            return Mathf.Max(1, Mathf.FloorToInt(baseDamage));
        }

        /// <summary>
        /// Apply defense reduction to raw damage.
        /// </summary>
        /// <param name="rawDamage">Damage before defense.</param>
        /// <param name="type">Damage type (determines which defense stat applies).</param>
        /// <param name="defender">Defender's computed stats.</param>
        /// <param name="defenderAttunement">Defender's Attunement. Magical damage is further scaled by this.</param>
        /// <returns>Damage after defense (minimum 1).</returns>
        public static int ApplyDefense(int rawDamage, DamageType type, ComputedStats defender,
            int defenderAttunement = 70)
        {
            float reduced;

            switch (type)
            {
                case DamageType.Physical:
                    reduced = rawDamage - defender.Defense;
                    break;

                case DamageType.Magical:
                    // Magical damage further scaled by target's Attunement
                    // High Attunement = take MORE magic damage (same as FFT's Faith)
                    reduced = rawDamage * (defenderAttunement / 100f) - defender.MagicDefense;
                    break;

                case DamageType.Pure:
                    reduced = rawDamage; // Pure ignores defense
                    break;

                default:
                    reduced = rawDamage;
                    break;
            }

            return Mathf.Max(1, Mathf.FloorToInt(reduced));
        }

        /// <summary>
        /// Calculate final damage from ability, attacker stats, defender stats, and height advantage.
        /// </summary>
        /// <param name="ability">The ability being used.</param>
        /// <param name="attacker">Attacker's computed stats.</param>
        /// <param name="defender">Defender's computed stats.</param>
        /// <param name="attackerResolve">Attacker's Resolve (0-100).</param>
        /// <param name="attackerAttunement">Attacker's Attunement (0-100).</param>
        /// <param name="defenderAttunement">Defender's Attunement (0-100).</param>
        /// <param name="heightAdvantage">Elevation difference (positive = attacking downhill).</param>
        /// <returns>Final damage (minimum 1).</returns>
        public static int CalculateFinalDamage(AbilityData ability, ComputedStats attacker,
            ComputedStats defender, int attackerResolve, int attackerAttunement,
            int defenderAttunement, int heightAdvantage = 0)
        {
            int raw = CalculateRawDamage(ability, attacker, attackerResolve, attackerAttunement);
            int afterDefense = ApplyDefense(raw, ability.DamageType, defender, defenderAttunement);
            int heightBonus = heightAdvantage * GameConstants.HeightAdvantagePerLevel;

            return Mathf.Max(1, afterDefense + heightBonus);
        }

        /// <summary>
        /// Calculate hit chance as a percentage, clamped to [MinHitChance, MaxHitChance].
        /// </summary>
        /// <param name="ability">The ability being used.</param>
        /// <param name="attacker">Attacker's computed stats (Speed contributes to accuracy).</param>
        /// <param name="defender">Defender's computed stats (Speed contributes to evasion).</param>
        /// <param name="heightAdvantage">Elevation difference (positive = attacking downhill).</param>
        /// <returns>Hit chance percentage, clamped.</returns>
        public static int CalculateHitChance(AbilityData ability, ComputedStats attacker,
            ComputedStats defender, int heightAdvantage = 0)
        {
            int hitChance = ability.Accuracy
                + attacker.Speed - defender.Speed
                + heightAdvantage * GameConstants.HeightAdvantagePerLevel;

            return Mathf.Clamp(hitChance, GameConstants.MinHitChance, GameConstants.MaxHitChance);
        }

        /// <summary>
        /// Calculate healing amount for a healing ability.
        /// </summary>
        /// <param name="ability">The healing ability.</param>
        /// <param name="caster">Caster's computed stats.</param>
        /// <param name="casterAttunement">Caster's Attunement (0-100).</param>
        /// <returns>Healing amount (minimum 1).</returns>
        public static int CalculateHealing(AbilityData ability, ComputedStats caster,
            int casterAttunement)
        {
            float heal = caster.MagicAttack * ability.Power / 10f;
            heal *= casterAttunement / 100f;
            return Mathf.Max(1, Mathf.FloorToInt(heal));
        }
    }
}
