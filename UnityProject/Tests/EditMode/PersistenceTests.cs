using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using IsoRPG.Data;

namespace IsoRPG.Tests
{
    public class PersistenceTests
    {
        private const string TestSlot = "test_save_unit_test";

        [TearDown]
        public void TearDown()
        {
            Persistence.DeleteSave(TestSlot);
        }

        private SaveData CreateTestSaveData()
        {
            return new SaveData
            {
                SaveName = "Test Save",
                CurrentBattleIndex = 3,
                BattlesCompleted = 2,
                PartyUnits = new List<UnitSaveData>
                {
                    new UnitSaveData
                    {
                        Name = "Ramza",
                        Team = 0,
                        Level = 5,
                        CurrentJob = 0,
                        Brave = 70,
                        Faith = 65,
                        CurrentHP = 80,
                        CurrentMP = 30,
                        JobLevelKeys = new List<int> { 0, 1 },
                        JobLevelValues = new List<int> { 3, 2 },
                        JobPointKeys = new List<int> { 0, 1 },
                        JobPointValues = new List<int> { 350, 150 },
                        LearnedAbilities = new List<int> { 100, 200, 300 },
                        WeaponName = "Iron Sword",
                        ArmorName = "Leather Armor",
                        AccessoryName = null
                    }
                }
            };
        }

        [Test]
        public void Save_CreatesFile()
        {
            var data = CreateTestSaveData();
            Persistence.Save(data, TestSlot);

            Assert.IsTrue(Persistence.SaveExists(TestSlot));
        }

        [Test]
        public void Load_ReturnsData()
        {
            var original = CreateTestSaveData();
            Persistence.Save(original, TestSlot);

            var loaded = Persistence.Load(TestSlot);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Test Save", loaded.SaveName);
            Assert.AreEqual(3, loaded.CurrentBattleIndex);
            Assert.AreEqual(2, loaded.BattlesCompleted);
        }

        [Test]
        public void Load_PreservesUnitData()
        {
            var original = CreateTestSaveData();
            Persistence.Save(original, TestSlot);

            var loaded = Persistence.Load(TestSlot);

            Assert.AreEqual(1, loaded.PartyUnits.Count);
            var unit = loaded.PartyUnits[0];
            Assert.AreEqual("Ramza", unit.Name);
            Assert.AreEqual(5, unit.Level);
            Assert.AreEqual(70, unit.Brave);
            Assert.AreEqual(80, unit.CurrentHP);
            Assert.AreEqual("Iron Sword", unit.WeaponName);
        }

        [Test]
        public void Load_PreservesJobData()
        {
            var original = CreateTestSaveData();
            Persistence.Save(original, TestSlot);

            var loaded = Persistence.Load(TestSlot);
            var unit = loaded.PartyUnits[0];

            Assert.AreEqual(2, unit.JobLevelKeys.Count);
            Assert.AreEqual(0, unit.JobLevelKeys[0]);
            Assert.AreEqual(3, unit.JobLevelValues[0]);
            Assert.AreEqual(3, unit.LearnedAbilities.Count);
        }

        [Test]
        public void Load_MissingFile_ReturnsNull()
        {
            var loaded = Persistence.Load("nonexistent_slot");
            Assert.IsNull(loaded);
        }

        [Test]
        public void SaveExists_NoFile_ReturnsFalse()
        {
            Assert.IsFalse(Persistence.SaveExists("nonexistent_slot"));
        }

        [Test]
        public void DeleteSave_RemovesFile()
        {
            Persistence.Save(CreateTestSaveData(), TestSlot);
            Assert.IsTrue(Persistence.SaveExists(TestSlot));

            bool deleted = Persistence.DeleteSave(TestSlot);
            Assert.IsTrue(deleted);
            Assert.IsFalse(Persistence.SaveExists(TestSlot));
        }

        [Test]
        public void DeleteSave_MissingFile_ReturnsFalse()
        {
            Assert.IsFalse(Persistence.DeleteSave("nonexistent_slot"));
        }

        [Test]
        public void Save_SetsTimestamp()
        {
            var data = CreateTestSaveData();
            Persistence.Save(data, TestSlot);

            Assert.IsNotNull(data.Timestamp);
            Assert.IsNotEmpty(data.Timestamp);
        }

        [Test]
        public void GetSaveSlots_ReturnsExistingSlots()
        {
            Persistence.Save(CreateTestSaveData(), TestSlot);
            var slots = Persistence.GetSaveSlots();

            Assert.IsTrue(slots.Length > 0);
            Assert.Contains(TestSlot, slots);
        }
    }
}
