using System;
using System.Collections.Generic;

namespace IsoRPG.Data
{
    /// <summary>
    /// Top-level save file data. Contains party state, progression, and current position.
    /// Serialized to JSON via Unity's JsonUtility (requires all fields to be serializable).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Display name for the save slot.</summary>
        public string SaveName;
        /// <summary>ISO timestamp of when the save was created.</summary>
        public string Timestamp;
        /// <summary>Index of the current/next battle (for linear progression).</summary>
        public int CurrentBattleIndex;
        /// <summary>Total battles completed.</summary>
        public int BattlesCompleted;
        /// <summary>All party unit data.</summary>
        public List<UnitSaveData> PartyUnits = new();
    }

    /// <summary>
    /// Serializable data for a single unit's persistent state.
    /// </summary>
    [Serializable]
    public class UnitSaveData
    {
        public string Name;
        public int Team;
        public int Level;
        public int CurrentJob;
        public int Brave;
        public int Faith;
        public int CurrentHP;
        public int CurrentMP;

        /// <summary>Job levels: serialized as parallel arrays (JsonUtility can't serialize Dictionary).</summary>
        public List<int> JobLevelKeys = new();
        public List<int> JobLevelValues = new();

        /// <summary>Job points: serialized as parallel arrays.</summary>
        public List<int> JobPointKeys = new();
        public List<int> JobPointValues = new();

        /// <summary>Learned ability instance IDs.</summary>
        public List<int> LearnedAbilities = new();

        /// <summary>Equipment names (resolved by name on load — asset GUIDs in V1).</summary>
        public string WeaponName;
        public string ArmorName;
        public string AccessoryName;
    }
}
