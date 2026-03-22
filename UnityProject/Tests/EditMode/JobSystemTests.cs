using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class JobSystemTests
    {
        private JobSystem _jobSystem;
        private JobData _squire;
        private JobData _knight;
        private AbilityData _testAbility;

        [SetUp]
        public void SetUp()
        {
            _jobSystem = new JobSystem();

            _squire = ScriptableObject.CreateInstance<JobData>();
            _squire.Id = JobId.Squire;
            _squire.JobName = "Squire";
            _squire.HPMultiplier = 100;
            _squire.MPMultiplier = 100;
            _squire.PAMultiplier = 100;
            _squire.MAMultiplier = 100;
            _squire.SpeedMultiplier = 100;
            _squire.AllowedWeapons = new[] { WeaponType.Sword };
            _squire.AllowedArmor = new[] { ArmorType.Light };
            _squire.UnlockRequirements = new JobRequirement[0];

            _testAbility = ScriptableObject.CreateInstance<AbilityData>();
            _testAbility.AbilityName = "Focus";
            _testAbility.SlotType = AbilitySlotType.Action;

            _squire.Abilities = new[]
            {
                new JobAbilityEntry { Ability = _testAbility, JPCost = 100, RequiredJobLevel = 0 }
            };

            _knight = ScriptableObject.CreateInstance<JobData>();
            _knight.Id = JobId.Knight;
            _knight.JobName = "Knight";
            _knight.HPMultiplier = 120;
            _knight.MPMultiplier = 80;
            _knight.PAMultiplier = 110;
            _knight.MAMultiplier = 80;
            _knight.SpeedMultiplier = 90;
            _knight.AllowedWeapons = new[] { WeaponType.Sword };
            _knight.AllowedArmor = new[] { ArmorType.Heavy, ArmorType.Light };
            _knight.UnlockRequirements = new[]
            {
                new JobRequirement { Job = JobId.Squire, RequiredLevel = 2 }
            };
            _knight.Abilities = new JobAbilityEntry[0];
        }

        private UnitInstance CreateUnit()
        {
            var unit = new UnitInstance("Test", 0, 5, Vector2Int.zero);
            return unit;
        }

        // --- CanUnlockJob ---

        [Test]
        public void CanUnlockJob_NoRequirements_ReturnsTrue()
        {
            var unit = CreateUnit();
            Assert.IsTrue(_jobSystem.CanUnlockJob(unit, _squire));
        }

        [Test]
        public void CanUnlockJob_RequirementsNotMet_ReturnsFalse()
        {
            var unit = CreateUnit();
            // Knight requires Squire Lv 2, unit has no JP
            Assert.IsFalse(_jobSystem.CanUnlockJob(unit, _knight));
        }

        [Test]
        public void CanUnlockJob_RequirementsMet_ReturnsTrue()
        {
            var unit = CreateUnit();
            // Give enough JP for Squire Lv 2 (threshold: 100 JP)
            unit.JobPoints[JobId.Squire] = 150;
            unit.JobLevels[JobId.Squire] = 2;

            Assert.IsTrue(_jobSystem.CanUnlockJob(unit, _knight));
        }

        // --- AwardJP ---

        [Test]
        public void AwardJP_IncreasesJP()
        {
            var unit = CreateUnit();
            int awarded = _jobSystem.AwardJP(unit, 10);

            Assert.Greater(awarded, 0);
            Assert.Greater(unit.JobPoints[unit.CurrentJob], 0);
        }

        [Test]
        public void AwardJP_IncludesJobLevelBonus()
        {
            var unit = CreateUnit();
            // At job level 1: base 10 + (1 * 2) = 12
            int awarded = _jobSystem.AwardJP(unit, 10);
            Assert.AreEqual(12, awarded);
        }

        [Test]
        public void AwardJP_UpdatesJobLevel()
        {
            var unit = CreateUnit();
            // Award enough for Lv 2 (100 JP threshold)
            // Each award gives 10 + 2*level, need to accumulate 100+
            for (int i = 0; i < 10; i++)
                _jobSystem.AwardJP(unit, 10);

            int level = _jobSystem.GetJobLevel(unit, unit.CurrentJob);
            Assert.GreaterOrEqual(level, 2);
        }

        // --- Spillover ---

        [Test]
        public void SpilloverJP_AlliesInSameJob_ReceiveJP()
        {
            var actor = CreateUnit();
            var ally = new UnitInstance("Ally", 0, 1, new Vector2Int(1, 0));

            // Both are Squire by default
            var allies = new List<UnitInstance> { actor, ally };

            _jobSystem.AwardSpilloverJP(actor, allies, 20);

            Assert.Greater(ally.JobPoints.GetValueOrDefault(JobId.Squire), 0);
        }

        [Test]
        public void SpilloverJP_DifferentJob_NoJP()
        {
            var actor = CreateUnit();
            var ally = new UnitInstance("Ally", 0, 1, new Vector2Int(1, 0));
            ally.SetJob(JobId.Knight); // different job

            var allies = new List<UnitInstance> { actor, ally };

            _jobSystem.AwardSpilloverJP(actor, allies, 20);

            Assert.AreEqual(0, ally.JobPoints.GetValueOrDefault(JobId.Knight));
        }

        [Test]
        public void SpilloverJP_Is25Percent()
        {
            var actor = CreateUnit();
            var ally = new UnitInstance("Ally", 0, 1, new Vector2Int(1, 0));

            var allies = new List<UnitInstance> { actor, ally };
            _jobSystem.AwardSpilloverJP(actor, allies, 100);

            Assert.AreEqual(25, ally.JobPoints.GetValueOrDefault(JobId.Squire));
        }

        // --- LearnAbility ---

        [Test]
        public void LearnAbility_EnoughJP_Succeeds()
        {
            var unit = CreateUnit();
            unit.JobPoints[JobId.Squire] = 200;

            bool learned = _jobSystem.LearnAbility(unit, _squire, 0);

            Assert.IsTrue(learned);
            Assert.IsTrue(unit.LearnedAbilities.Contains(_testAbility.GetInstanceID()));
            Assert.AreEqual(100, unit.JobPoints[JobId.Squire]); // 200 - 100 cost
        }

        [Test]
        public void LearnAbility_NotEnoughJP_Fails()
        {
            var unit = CreateUnit();
            unit.JobPoints[JobId.Squire] = 50;

            bool learned = _jobSystem.LearnAbility(unit, _squire, 0);

            Assert.IsFalse(learned);
        }

        [Test]
        public void LearnAbility_AlreadyLearned_Fails()
        {
            var unit = CreateUnit();
            unit.JobPoints[JobId.Squire] = 500;

            _jobSystem.LearnAbility(unit, _squire, 0);
            bool learnedAgain = _jobSystem.LearnAbility(unit, _squire, 0);

            Assert.IsFalse(learnedAgain);
        }

        // --- ChangeJob ---

        [Test]
        public void ChangeJob_UpdatesCurrentJob()
        {
            var unit = CreateUnit();
            _jobSystem.ChangeJob(unit, _knight);
            Assert.AreEqual(JobId.Knight, unit.CurrentJob);
        }

        [Test]
        public void ChangeJob_RecalculatesStats()
        {
            var unit = CreateUnit();
            var squireHP = unit.Stats.MaxHP;

            _jobSystem.ChangeJob(unit, _knight); // HP multiplier 120%
            Assert.Greater(unit.Stats.MaxHP, squireHP);
        }

        // --- GetJobLevel ---

        [Test]
        public void GetJobLevel_NoJP_ReturnsOne()
        {
            var unit = CreateUnit();
            Assert.AreEqual(1, _jobSystem.GetJobLevel(unit, JobId.Squire));
        }

        [Test]
        public void GetJobLevel_ThresholdsMet()
        {
            var unit = CreateUnit();
            unit.JobPoints[JobId.Squire] = 0;
            Assert.AreEqual(1, _jobSystem.GetJobLevel(unit, JobId.Squire));

            unit.JobPoints[JobId.Squire] = 100;
            Assert.AreEqual(2, _jobSystem.GetJobLevel(unit, JobId.Squire));

            unit.JobPoints[JobId.Squire] = 300;
            Assert.AreEqual(3, _jobSystem.GetJobLevel(unit, JobId.Squire));

            unit.JobPoints[JobId.Squire] = 600;
            Assert.AreEqual(4, _jobSystem.GetJobLevel(unit, JobId.Squire));

            unit.JobPoints[JobId.Squire] = 1000;
            Assert.AreEqual(5, _jobSystem.GetJobLevel(unit, JobId.Squire));
        }

        // --- CanEquip ---

        [Test]
        public void CanEquip_AllowedWeapon_ReturnsTrue()
        {
            var unit = CreateUnit();
            var sword = ScriptableObject.CreateInstance<EquipmentData>();
            sword.Slot = EquipmentSlot.Weapon;
            sword.WeaponType = WeaponType.Sword;

            Assert.IsTrue(_jobSystem.CanEquip(unit, sword, _knight));
        }

        [Test]
        public void CanEquip_DisallowedWeapon_ReturnsFalse()
        {
            var unit = CreateUnit();
            var bow = ScriptableObject.CreateInstance<EquipmentData>();
            bow.Slot = EquipmentSlot.Weapon;
            bow.WeaponType = WeaponType.Bow;

            Assert.IsFalse(_jobSystem.CanEquip(unit, bow, _knight));
        }

        [Test]
        public void CanEquip_Accessory_AlwaysTrue()
        {
            var unit = CreateUnit();
            var ring = ScriptableObject.CreateInstance<EquipmentData>();
            ring.Slot = EquipmentSlot.Accessory;

            Assert.IsTrue(_jobSystem.CanEquip(unit, ring, _knight));
        }

        // --- Stat Calculation ---

        [Test]
        public void RecalculateStats_AppliesMultipliers()
        {
            var unit = CreateUnit();
            var baseHP = unit.Stats.MaxHP;

            _jobSystem.RecalculateStats(unit, _knight);

            // Knight has 120% HP multiplier
            Assert.Greater(unit.Stats.MaxHP, baseHP * 1.1f); // should be ~120% of base
        }

        [Test]
        public void RecalculateStats_IncludesEquipment()
        {
            var unit = CreateUnit();
            var sword = ScriptableObject.CreateInstance<EquipmentData>();
            sword.Slot = EquipmentSlot.Weapon;
            sword.AttackBonus = 10;
            unit.EquippedWeapon = sword;

            _jobSystem.RecalculateStats(unit, _squire);
            int withSword = unit.Stats.PhysicalAttack;

            unit.EquippedWeapon = null;
            _jobSystem.RecalculateStats(unit, _squire);
            int withoutSword = unit.Stats.PhysicalAttack;

            Assert.AreEqual(10, withSword - withoutSword);
        }
    }
}
