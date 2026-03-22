# System 11: Save/Load & Polish — Specification

## Overview

Persistence layer for game state serialization, plus infrastructure for content and polish. This system makes the game saveable, loadable, and ready for content authoring.

---

## 1. SaveData Model

```csharp
[System.Serializable]
public class SaveData
{
    public string SaveName;
    public string Timestamp;
    public int CurrentBattleIndex;
    public List<UnitSaveData> PartyUnits;
}

[System.Serializable]
public struct UnitSaveData
{
    public string Name;
    public int Team;
    public int Level;
    public int CurrentJob;       // JobId as int
    public int Brave;
    public int Faith;
    public int CurrentHP, CurrentMP;
    public Dictionary<int, int> JobLevels;   // JobId → level
    public Dictionary<int, int> JobPoints;   // JobId → JP
    public List<int> LearnedAbilities;       // ability instance IDs
    // Equipment references (by name for now, asset GUID in V1)
    public string WeaponName;
    public string ArmorName;
    public string AccessoryName;
}
```

---

## 2. Persistence Manager

```csharp
public static class Persistence
{
    /// <summary>Save game state to a JSON file in Application.persistentDataPath.</summary>
    public static void Save(SaveData data, string slotName = "save1");

    /// <summary>Load game state from a JSON file.</summary>
    public static SaveData Load(string slotName = "save1");

    /// <summary>Check if a save file exists.</summary>
    public static bool SaveExists(string slotName = "save1");

    /// <summary>Delete a save file.</summary>
    public static void DeleteSave(string slotName = "save1");

    /// <summary>Get all save slot names.</summary>
    public static string[] GetSaveSlots();
}
```

Uses `JsonUtility` for Unity-native serialization. Files stored at `Application.persistentDataPath/saves/`.

---

## 3. Test Coverage

| Class | Tests |
|-------|-------|
| `Persistence` | Save/Load round-trip, SaveExists, DeleteSave, missing file returns null |
| `SaveData` | Serialization preserves all fields, unit data integrity |
