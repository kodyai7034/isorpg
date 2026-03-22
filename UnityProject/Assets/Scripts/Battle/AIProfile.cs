using UnityEngine;

namespace IsoRPG.Battle
{
    /// <summary>
    /// ScriptableObject defining tunable weights for AI decision-making.
    /// Each enemy unit references an AIProfile that shapes their behavior
    /// (aggressive, defensive, support, coward).
    ///
    /// Create via Assets → Create → IsoRPG → AIProfile.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAIProfile", menuName = "IsoRPG/AIProfile")]
    public class AIProfile : ScriptableObject
    {
        /// <summary>Display name for debugging.</summary>
        public string ProfileName = "Default";

        [Header("Offensive")]
        /// <summary>Score points per point of damage dealt.</summary>
        public float DamageWeight = 3f;
        /// <summary>Bonus score if the attack would kill the target.</summary>
        public float KillBonus = 50f;
        /// <summary>Multiplier for target threat level (PA+MA). Prioritizes dangerous units.</summary>
        public float TargetThreatWeight = 2f;

        [Header("Defensive")]
        /// <summary>Score penalty per enemy that can counter-attack from the candidate position.</summary>
        public float SelfPreservationWeight = 5f;
        /// <summary>HP fraction below which the AI becomes more cautious.</summary>
        [Range(0f, 1f)]
        public float HealthThresholdForCaution = 0.3f;

        [Header("Support")]
        /// <summary>Score points per point of healing done to an ally.</summary>
        public float HealingWeight = 2f;
        /// <summary>Only consider healing allies below this HP fraction.</summary>
        [Range(0f, 1f)]
        public float HealAllyThreshold = 0.5f;

        [Header("Position")]
        /// <summary>Score bonus per elevation level of the candidate tile.</summary>
        public float HighGroundBonus = 10f;
        /// <summary>Score adjustment per tile distance from nearest enemy. Positive = prefer far (defensive), negative = prefer close (aggressive).</summary>
        public float DistanceFromEnemyPenalty = 1f;

        /// <summary>Create a default Aggressive profile at runtime (for units without a ScriptableObject).</summary>
        public static AIProfile CreateAggressive()
        {
            var p = CreateInstance<AIProfile>();
            p.ProfileName = "Aggressive";
            p.DamageWeight = 4f;
            p.KillBonus = 60f;
            p.TargetThreatWeight = 2f;
            p.SelfPreservationWeight = 2f;
            p.HealthThresholdForCaution = 0.2f;
            p.HealingWeight = 1f;
            p.HealAllyThreshold = 0.3f;
            p.HighGroundBonus = 5f;
            p.DistanceFromEnemyPenalty = -2f; // wants to close distance
            return p;
        }

        /// <summary>Create a default Defensive profile at runtime.</summary>
        public static AIProfile CreateDefensive()
        {
            var p = CreateInstance<AIProfile>();
            p.ProfileName = "Defensive";
            p.DamageWeight = 2f;
            p.KillBonus = 40f;
            p.TargetThreatWeight = 1f;
            p.SelfPreservationWeight = 8f;
            p.HealthThresholdForCaution = 0.5f;
            p.HealingWeight = 3f;
            p.HealAllyThreshold = 0.6f;
            p.HighGroundBonus = 15f;
            p.DistanceFromEnemyPenalty = 3f; // stays back
            return p;
        }
    }
}
