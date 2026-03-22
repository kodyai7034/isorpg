using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace IsoRPG.Data
{
    /// <summary>
    /// Static utility for saving and loading game state to JSON files.
    /// Files stored at <see cref="Application.persistentDataPath"/>/saves/.
    ///
    /// Uses JsonUtility for Unity-native serialization (no third-party dependencies).
    /// </summary>
    public static class Persistence
    {
        private const string SaveDirectory = "saves";
        private const string FileExtension = ".json";

        /// <summary>
        /// Save game state to a named slot.
        /// </summary>
        /// <param name="data">Save data to persist.</param>
        /// <param name="slotName">Save slot identifier (default: "save1").</param>
        public static void Save(SaveData data, string slotName = "save1")
        {
            if (data == null)
            {
                Debug.LogError("[Persistence] Cannot save null data.");
                return;
            }

            data.Timestamp = DateTime.UtcNow.ToString("o");

            string dir = GetSaveDirectory();
            Directory.CreateDirectory(dir);

            string path = GetSavePath(slotName);
            string json = JsonUtility.ToJson(data, prettyPrint: true);

            File.WriteAllText(path, json);
            Debug.Log($"[Persistence] Saved to {path}");
        }

        /// <summary>
        /// Load game state from a named slot.
        /// </summary>
        /// <param name="slotName">Save slot identifier.</param>
        /// <returns>Loaded save data, or null if file doesn't exist or is corrupt.</returns>
        public static SaveData Load(string slotName = "save1")
        {
            string path = GetSavePath(slotName);

            if (!File.Exists(path))
            {
                Debug.Log($"[Persistence] No save file at {path}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[Persistence] Loaded from {path}");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Persistence] Failed to load {path}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if a save file exists for the given slot.
        /// </summary>
        public static bool SaveExists(string slotName = "save1")
        {
            return File.Exists(GetSavePath(slotName));
        }

        /// <summary>
        /// Delete a save file.
        /// </summary>
        /// <param name="slotName">Save slot to delete.</param>
        /// <returns>True if file was deleted, false if it didn't exist.</returns>
        public static bool DeleteSave(string slotName = "save1")
        {
            string path = GetSavePath(slotName);
            if (!File.Exists(path)) return false;

            File.Delete(path);
            Debug.Log($"[Persistence] Deleted {path}");
            return true;
        }

        /// <summary>
        /// Get all existing save slot names.
        /// </summary>
        /// <returns>Array of slot names (without extension).</returns>
        public static string[] GetSaveSlots()
        {
            string dir = GetSaveDirectory();
            if (!Directory.Exists(dir))
                return Array.Empty<string>();

            return Directory.GetFiles(dir, $"*{FileExtension}")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(s => s)
                .ToArray();
        }

        private static string GetSaveDirectory()
        {
            return Path.Combine(Application.persistentDataPath, SaveDirectory);
        }

        private static string GetSavePath(string slotName)
        {
            return Path.Combine(GetSaveDirectory(), slotName + FileExtension);
        }
    }
}
