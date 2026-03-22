using UnityEngine;

namespace IsoRPG.Units
{
    /// <summary>Weapon categories. Determines which jobs can equip.</summary>
    public enum WeaponType { Sword, Bow, Rod, Staff, Fists, Lance }

    /// <summary>Armor categories. Determines which jobs can equip.</summary>
    public enum ArmorType { Heavy, Light, Robes }

    /// <summary>Which slot this equipment occupies.</summary>
    public enum EquipmentSlot { Weapon, Armor, Accessory }

    /// <summary>
    /// ScriptableObject defining a piece of equipment's stats and restrictions.
    /// Create via Assets → Create → IsoRPG → Equipment.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEquipment", menuName = "IsoRPG/Equipment")]
    public class EquipmentData : ScriptableObject
    {
        /// <summary>Display name.</summary>
        public string ItemName;

        /// <summary>Which slot this goes in.</summary>
        public EquipmentSlot Slot;

        /// <summary>Weapon type (only relevant if Slot == Weapon).</summary>
        public WeaponType WeaponType;

        /// <summary>Armor type (only relevant if Slot == Armor).</summary>
        public ArmorType ArmorType;

        [Header("Stat Bonuses")]
        public int AttackBonus;
        public int DefenseBonus;
        public int MagicAttackBonus;
        public int MagicDefenseBonus;
        public int SpeedBonus;
        public int HPBonus;
        public int MPBonus;
    }
}
