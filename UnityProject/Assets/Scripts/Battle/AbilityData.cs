using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Which equip slot an ability occupies.
    /// </summary>
    public enum AbilitySlotType
    {
        /// <summary>Active skill used during the Act phase.</summary>
        Action,
        /// <summary>Auto-triggers on conditions (e.g., Counter on hit).</summary>
        Reaction,
        /// <summary>Always-active passive buff (e.g., Dual Wield).</summary>
        Support,
        /// <summary>Movement enhancement (e.g., Move+1, Teleport).</summary>
        Movement
    }

    /// <summary>
    /// ScriptableObject defining a single ability's properties.
    /// Abilities are shared data — instances are not modified at runtime.
    ///
    /// Create via Assets → Create → IsoRPG → Ability.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "IsoRPG/Ability")]
    public class AbilityData : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>Display name.</summary>
        public string AbilityName;
        /// <summary>Tooltip description.</summary>
        [TextArea(2, 4)]
        public string Description;

        [Header("Slot & Targeting")]
        /// <summary>Which equip slot this ability goes in.</summary>
        public AbilitySlotType SlotType = AbilitySlotType.Action;
        /// <summary>How this ability selects targets.</summary>
        public AbilityTargetType Targeting = AbilityTargetType.Single;

        [Header("Range")]
        /// <summary>Maximum range in tiles. 0 = self only.</summary>
        public int Range = 1;
        /// <summary>Area of effect radius. 0 = single target.</summary>
        public int AoERadius = 0;
        /// <summary>Whether line of sight is required to target.</summary>
        public bool RequiresLineOfSight = true;

        [Header("Damage / Healing")]
        /// <summary>Physical, Magical, or Pure damage type.</summary>
        public DamageType DamageType = DamageType.Physical;
        /// <summary>Base power value for damage/healing formula.</summary>
        public int Power = 10;
        /// <summary>Base accuracy percentage (0-100).</summary>
        public int Accuracy = 90;
        /// <summary>If true, Power heals instead of dealing damage.</summary>
        public bool IsHealing;

        [Header("Cost")]
        /// <summary>MP consumed on use. 0 = free.</summary>
        public int MPCost;

        [Header("Status Effect")]
        /// <summary>Status effect applied on hit. Null/None for no status.</summary>
        public StatusType AppliedStatus;
        /// <summary>Whether this ability applies a status effect.</summary>
        public bool AppliesStatus;
        /// <summary>Duration of applied status in turns.</summary>
        public int StatusDuration = 3;
        /// <summary>Chance to apply status (0-100).</summary>
        public int StatusChance = 100;

        [Header("Animation")]
        /// <summary>Key for VFX/animation lookup (future use).</summary>
        public string AnimationKey;
    }
}
