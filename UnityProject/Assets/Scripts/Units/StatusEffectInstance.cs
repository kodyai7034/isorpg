using UnityEngine;
using IsoRPG.Core;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Units
{
    /// <summary>
    /// Runtime instance of a status effect on a unit. Tracks remaining duration
    /// and applies per-turn tick effects (poison damage, regen healing, etc.).
    /// </summary>
    public class StatusEffectInstance
    {
        /// <summary>Unique ID for this status instance (for targeted removal on undo).</summary>
        public EntityId Id { get; }

        /// <summary>The type of status effect.</summary>
        public StatusType Type { get; }

        /// <summary>Turns remaining. Decremented each tick. Effect expires at 0.</summary>
        public int RemainingDuration { get; set; }

        /// <summary>Whether this effect has expired.</summary>
        public bool IsExpired => RemainingDuration <= 0;

        /// <summary>
        /// Create a new status effect instance.
        /// </summary>
        /// <param name="type">Status type.</param>
        /// <param name="duration">Duration in turns.</param>
        public StatusEffectInstance(StatusType type, int duration)
        {
            Id = EntityId.New();
            Type = type;
            RemainingDuration = duration;
        }

        /// <summary>
        /// Create a status effect with a specific ID (for undo reconstruction).
        /// </summary>
        public StatusEffectInstance(EntityId id, StatusType type, int duration)
        {
            Id = id;
            Type = type;
            RemainingDuration = duration;
        }

        /// <summary>
        /// Apply per-turn tick effect and decrement duration.
        /// Call at the start of the affected unit's turn.
        /// </summary>
        /// <param name="unit">The unit affected by this status.</param>
        public void Tick(UnitInstance unit)
        {
            switch (Type)
            {
                case StatusType.Poison:
                    int poisonDmg = Mathf.Max(1, unit.Stats.MaxHP / 10);
                    unit.ApplyDamage(poisonDmg);
                    break;

                case StatusType.Regen:
                    int regenHeal = Mathf.Max(1, unit.Stats.MaxHP / 10);
                    unit.ApplyHealing(regenHeal);
                    break;

                // Haste, Slow, Protect, Shell are passive modifiers —
                // they don't tick, they modify stats/damage while active.
                // Handled by DamageCalculator and CTSystem queries.
            }

            RemainingDuration--;
        }

        /// <summary>
        /// Get the stat modifier this status applies (for ComputedStats adjustment).
        /// Returns 1.0 for no modification.
        /// </summary>
        /// <param name="statType">Which stat to query.</param>
        /// <returns>Multiplier (e.g., 1.5 for Haste on Speed, 0.67 for Protect on physical damage).</returns>
        public float GetModifier(StatusModifierType statType)
        {
            return (Type, statType) switch
            {
                (StatusType.Haste, StatusModifierType.Speed) => 1.5f,
                (StatusType.Slow, StatusModifierType.Speed) => 0.5f,
                (StatusType.Protect, StatusModifierType.PhysicalDamageReduction) => 0.67f,
                (StatusType.Shell, StatusModifierType.MagicalDamageReduction) => 0.67f,
                _ => 1.0f,
            };
        }

        public override string ToString()
        {
            return $"{Type} ({RemainingDuration} turns)";
        }
    }

    /// <summary>
    /// Types of stat modifications that status effects can apply.
    /// </summary>
    public enum StatusModifierType
    {
        Speed,
        PhysicalDamageReduction,
        MagicalDamageReduction
    }
}
