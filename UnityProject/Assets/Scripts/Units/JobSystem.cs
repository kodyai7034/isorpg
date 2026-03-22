using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Units
{
    /// <summary>
    /// Manages job switching, JP earning/spending, ability learning, and stat recalculation.
    /// Pure C# — no MonoBehaviour dependency.
    ///
    /// Job level thresholds: Lv1=0, Lv2=100, Lv3=300, Lv4=600, Lv5=1000
    /// JP per action: 10 + (JobLevel * 2)
    /// Spillover: allies earn 25% of actor's JP in matching jobs
    /// </summary>
    public class JobSystem
    {
        /// <summary>JP thresholds for job levels. Index = level (0-based), value = cumulative JP required.</summary>
        public static readonly int[] JobLevelThresholds = { 0, 100, 300, 600, 1000 };

        /// <summary>Spillover fraction (25%).</summary>
        public const float SpilloverFraction = 0.25f;

        /// <summary>
        /// Check if a unit meets all unlock requirements for a job.
        /// </summary>
        /// <param name="unit">Unit to check.</param>
        /// <param name="job">Job to unlock.</param>
        /// <returns>True if all requirements are met.</returns>
        public bool CanUnlockJob(UnitInstance unit, JobData job)
        {
            if (job.UnlockRequirements == null || job.UnlockRequirements.Length == 0)
                return true;

            foreach (var req in job.UnlockRequirements)
            {
                int currentLevel = GetJobLevel(unit, req.Job);
                if (currentLevel < req.RequiredLevel)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Change a unit's active job. Recalculates stats.
        /// </summary>
        /// <param name="unit">Unit to change.</param>
        /// <param name="job">Target job.</param>
        /// <param name="jobLookup">Lookup for job data (for stat recalculation).</param>
        public void ChangeJob(UnitInstance unit, JobData job)
        {
            unit.SetJob(job.Id);
            RecalculateStats(unit, job);
        }

        /// <summary>
        /// Award JP to a unit for their current job.
        /// </summary>
        /// <param name="unit">Unit earning JP.</param>
        /// <param name="baseJP">Base JP amount (before job level bonus).</param>
        /// <returns>Total JP awarded (after bonus).</returns>
        public int AwardJP(UnitInstance unit, int baseJP)
        {
            int jobLevel = GetJobLevel(unit, unit.CurrentJob);
            int totalJP = baseJP + jobLevel * 2;

            if (!unit.JobPoints.ContainsKey(unit.CurrentJob))
                unit.JobPoints[unit.CurrentJob] = 0;

            unit.JobPoints[unit.CurrentJob] += totalJP;
            UpdateJobLevel(unit, unit.CurrentJob);
            return totalJP;
        }

        /// <summary>
        /// Award spillover JP (25%) to allies with the same active job.
        /// </summary>
        /// <param name="actor">Unit that earned JP.</param>
        /// <param name="allies">All allied units on the battlefield.</param>
        /// <param name="baseJP">Base JP the actor earned.</param>
        public void AwardSpilloverJP(UnitInstance actor, List<UnitInstance> allies, int baseJP)
        {
            int spillover = Mathf.Max(1, Mathf.FloorToInt(baseJP * SpilloverFraction));

            foreach (var ally in allies)
            {
                if (ally == actor || !ally.IsAlive) continue;
                if (ally.CurrentJob != actor.CurrentJob) continue;

                if (!ally.JobPoints.ContainsKey(ally.CurrentJob))
                    ally.JobPoints[ally.CurrentJob] = 0;

                ally.JobPoints[ally.CurrentJob] += spillover;
                UpdateJobLevel(ally, ally.CurrentJob);
            }
        }

        /// <summary>
        /// Learn an ability if the unit has enough JP.
        /// </summary>
        /// <param name="unit">Unit learning the ability.</param>
        /// <param name="job">Job the ability belongs to.</param>
        /// <param name="abilityIndex">Index into job's Abilities array.</param>
        /// <returns>True if learned successfully.</returns>
        public bool LearnAbility(UnitInstance unit, JobData job, int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= job.Abilities.Length)
                return false;

            var entry = job.Abilities[abilityIndex];

            // Check job level requirement
            int currentLevel = GetJobLevel(unit, job.Id);
            if (currentLevel < entry.RequiredJobLevel)
                return false;

            // Check JP
            int currentJP = unit.JobPoints.ContainsKey(job.Id) ? unit.JobPoints[job.Id] : 0;
            if (currentJP < entry.JPCost)
                return false;

            // Check not already learned
            int abilityHash = entry.Ability.GetInstanceID();
            if (unit.LearnedAbilities.Contains(abilityHash))
                return false;

            // Spend JP and learn
            unit.JobPoints[job.Id] -= entry.JPCost;
            unit.LearnedAbilities.Add(abilityHash);
            return true;
        }

        /// <summary>
        /// Get a unit's level in a specific job.
        /// </summary>
        public int GetJobLevel(UnitInstance unit, JobId jobId)
        {
            if (!unit.JobPoints.ContainsKey(jobId))
                return 1;

            int totalJP = unit.JobPoints[jobId];
            for (int i = JobLevelThresholds.Length - 1; i >= 0; i--)
            {
                if (totalJP >= JobLevelThresholds[i])
                    return i + 1; // levels are 1-based
            }
            return 1;
        }

        /// <summary>
        /// Calculate base JP earned per action (before spillover).
        /// </summary>
        public int CalculateBaseJP(UnitInstance unit)
        {
            int jobLevel = GetJobLevel(unit, unit.CurrentJob);
            return 10 + jobLevel * 2;
        }

        /// <summary>
        /// Recalculate a unit's ComputedStats based on job multipliers and equipment.
        /// </summary>
        public void RecalculateStats(UnitInstance unit, JobData job)
        {
            var baseStats = ComputedStats.FromBase(unit.Level, unit.Brave, unit.Faith);

            int hp = ApplyMultiplier(baseStats.MaxHP, job.HPMultiplier);
            int mp = ApplyMultiplier(baseStats.MaxMP, job.MPMultiplier);
            int pa = ApplyMultiplier(baseStats.PhysicalAttack, job.PAMultiplier);
            int ma = ApplyMultiplier(baseStats.MagicAttack, job.MAMultiplier);
            int spd = ApplyMultiplier(baseStats.Speed, job.SpeedMultiplier);

            // Equipment bonuses
            if (unit.EquippedWeapon != null)
            {
                pa += unit.EquippedWeapon.AttackBonus;
                ma += unit.EquippedWeapon.MagicAttackBonus;
            }
            if (unit.EquippedArmor != null)
            {
                hp += unit.EquippedArmor.HPBonus;
                baseStats.Defense += unit.EquippedArmor.DefenseBonus;
                baseStats.MagicDefense += unit.EquippedArmor.MagicDefenseBonus;
            }
            if (unit.EquippedAccessory != null)
            {
                spd += unit.EquippedAccessory.SpeedBonus;
                hp += unit.EquippedAccessory.HPBonus;
                mp += unit.EquippedAccessory.MPBonus;
            }

            unit.SetStats(new ComputedStats
            {
                MaxHP = Mathf.Max(1, hp),
                MaxMP = Mathf.Max(0, mp),
                PhysicalAttack = Mathf.Max(1, pa),
                MagicAttack = Mathf.Max(1, ma),
                Speed = Mathf.Max(1, spd),
                Move = baseStats.Move,
                Jump = baseStats.Jump,
                Defense = Mathf.Max(0, baseStats.Defense),
                MagicDefense = Mathf.Max(0, baseStats.MagicDefense)
            });
        }

        /// <summary>
        /// Check if a unit can equip a specific piece of equipment given their current job.
        /// </summary>
        public bool CanEquip(UnitInstance unit, EquipmentData equipment, JobData currentJob)
        {
            if (equipment == null || currentJob == null) return false;

            switch (equipment.Slot)
            {
                case EquipmentSlot.Weapon:
                    return currentJob.AllowedWeapons != null &&
                           currentJob.AllowedWeapons.Contains(equipment.WeaponType);
                case EquipmentSlot.Armor:
                    return currentJob.AllowedArmor != null &&
                           currentJob.AllowedArmor.Contains(equipment.ArmorType);
                case EquipmentSlot.Accessory:
                    return true; // accessories are universal
                default:
                    return false;
            }
        }

        private void UpdateJobLevel(UnitInstance unit, JobId jobId)
        {
            int newLevel = GetJobLevel(unit, jobId);
            if (!unit.JobLevels.ContainsKey(jobId) || unit.JobLevels[jobId] < newLevel)
                unit.JobLevels[jobId] = newLevel;
        }

        private int ApplyMultiplier(int baseStat, int multiplierPercent)
        {
            return Mathf.FloorToInt(baseStat * multiplierPercent / 100f);
        }
    }
}
