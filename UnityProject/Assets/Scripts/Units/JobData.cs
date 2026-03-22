using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Units
{
    /// <summary>
    /// An ability learnable within a job, with its JP cost and level requirement.
    /// </summary>
    [System.Serializable]
    public struct JobAbilityEntry
    {
        /// <summary>The ability to learn.</summary>
        public AbilityData Ability;
        /// <summary>JP cost to learn (50-800).</summary>
        public int JPCost;
        /// <summary>Minimum job level required to start learning (0 = immediate).</summary>
        public int RequiredJobLevel;
    }

    /// <summary>
    /// Prerequisite for unlocking a job (e.g., Squire Lv 2).
    /// </summary>
    [System.Serializable]
    public struct JobRequirement
    {
        /// <summary>Required job.</summary>
        public JobId Job;
        /// <summary>Minimum level in that job.</summary>
        public int RequiredLevel;
    }

    /// <summary>
    /// ScriptableObject defining a job/class: stat multipliers, equipment restrictions,
    /// learnable abilities, and unlock prerequisites.
    ///
    /// Create via Assets → Create → IsoRPG → Job.
    /// </summary>
    [CreateAssetMenu(fileName = "NewJob", menuName = "IsoRPG/Job")]
    public class JobData : ScriptableObject
    {
        /// <summary>Enum identifier for this job.</summary>
        public JobId Id;
        /// <summary>Display name.</summary>
        public string JobName;
        /// <summary>Job icon for UI.</summary>
        public Sprite Icon;

        [Header("Stat Multipliers (100 = no change)")]
        /// <summary>HP multiplier percentage. Knight=120, Black Mage=80.</summary>
        public int HPMultiplier = 100;
        /// <summary>MP multiplier percentage.</summary>
        public int MPMultiplier = 100;
        /// <summary>Physical Attack multiplier.</summary>
        public int PAMultiplier = 100;
        /// <summary>Magic Attack multiplier.</summary>
        public int MAMultiplier = 100;
        /// <summary>Speed multiplier.</summary>
        public int SpeedMultiplier = 100;

        [Header("Equipment Restrictions")]
        /// <summary>Weapon types this job can equip.</summary>
        public WeaponType[] AllowedWeapons;
        /// <summary>Armor types this job can equip.</summary>
        public ArmorType[] AllowedArmor;

        [Header("Abilities")]
        /// <summary>All abilities learnable in this job with their JP costs.</summary>
        public JobAbilityEntry[] Abilities;

        [Header("Unlock Requirements")]
        /// <summary>Jobs and levels required to unlock this job. Empty = always available.</summary>
        public JobRequirement[] UnlockRequirements;
    }
}
