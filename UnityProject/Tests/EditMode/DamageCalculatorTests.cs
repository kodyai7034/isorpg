using NUnit.Framework;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class DamageCalculatorTests
    {
        private AbilityData CreateAbility(DamageType type = DamageType.Physical,
            int power = 10, int accuracy = 90, bool healing = false)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.DamageType = type;
            ability.Power = power;
            ability.Accuracy = accuracy;
            ability.IsHealing = healing;
            return ability;
        }

        private ComputedStats CreateStats(int pa = 10, int ma = 10, int speed = 7,
            int defense = 5, int magDef = 5)
        {
            return new ComputedStats
            {
                MaxHP = 100, MaxMP = 50,
                PhysicalAttack = pa, MagicAttack = ma,
                Speed = speed, Move = 4, Jump = 3,
                Defense = defense, MagicDefense = magDef
            };
        }

        [Test]
        public void RawDamage_Physical_ScalesWithPAAndResolve()
        {
            var ability = CreateAbility(DamageType.Physical, power: 10);
            var stats = CreateStats(pa: 20);

            int fullResolve = DamageCalculator.CalculateRawDamage(ability, stats, 100, 70);
            int halfResolve = DamageCalculator.CalculateRawDamage(ability, stats, 50, 70);

            Assert.Greater(fullResolve, halfResolve);
            Assert.AreEqual(20, fullResolve); // 20 * 10 / 10 * 1.0 = 20
            Assert.AreEqual(10, halfResolve); // 20 * 10 / 10 * 0.5 = 10
        }

        [Test]
        public void RawDamage_Magical_ScalesWithMAAndAttunement()
        {
            var ability = CreateAbility(DamageType.Magical, power: 10);
            var stats = CreateStats(ma: 20);

            int full = DamageCalculator.CalculateRawDamage(ability, stats, 70, 100);
            int half = DamageCalculator.CalculateRawDamage(ability, stats, 70, 50);

            Assert.Greater(full, half);
        }

        [Test]
        public void RawDamage_Pure_IgnoresStats()
        {
            var ability = CreateAbility(DamageType.Pure, power: 25);
            var weakStats = CreateStats(pa: 1, ma: 1);

            int damage = DamageCalculator.CalculateRawDamage(ability, weakStats, 10, 10);
            Assert.AreEqual(25, damage); // Pure = just ability power
        }

        [Test]
        public void ApplyDefense_Physical_ReducesByDefense()
        {
            var stats = CreateStats(defense: 5);
            int result = DamageCalculator.ApplyDefense(20, DamageType.Physical, stats);
            Assert.AreEqual(15, result); // 20 - 5
        }

        [Test]
        public void ApplyDefense_Magical_ScaledByTargetAttunement()
        {
            var stats = CreateStats(magDef: 5);
            int highFaith = DamageCalculator.ApplyDefense(20, DamageType.Magical, stats, 100);
            int lowFaith = DamageCalculator.ApplyDefense(20, DamageType.Magical, stats, 50);

            Assert.Greater(highFaith, lowFaith); // high attunement = take MORE magic damage
        }

        [Test]
        public void ApplyDefense_Pure_IgnoresDefense()
        {
            var stats = CreateStats(defense: 99, magDef: 99);
            int result = DamageCalculator.ApplyDefense(20, DamageType.Pure, stats);
            Assert.AreEqual(20, result);
        }

        [Test]
        public void FinalDamage_MinimumIsOne()
        {
            var ability = CreateAbility(DamageType.Physical, power: 1);
            var attacker = CreateStats(pa: 1);
            var defender = CreateStats(defense: 999);

            int damage = DamageCalculator.CalculateFinalDamage(
                ability, attacker, defender, 1, 70, 70, 0);
            Assert.AreEqual(1, damage);
        }

        [Test]
        public void FinalDamage_HeightAdvantage_AddsDamage()
        {
            var ability = CreateAbility(DamageType.Physical, power: 10);
            var attacker = CreateStats(pa: 10);
            var defender = CreateStats(defense: 5);

            int flat = DamageCalculator.CalculateFinalDamage(
                ability, attacker, defender, 70, 70, 70, 0);
            int downhill = DamageCalculator.CalculateFinalDamage(
                ability, attacker, defender, 70, 70, 70, 2);

            Assert.Greater(downhill, flat);
            Assert.AreEqual(flat + 2 * GameConstants.HeightAdvantagePerLevel, downhill);
        }

        [Test]
        public void HitChance_ClampsToMinMax()
        {
            var ability = CreateAbility(accuracy: 200);
            var stats = CreateStats(speed: 100);
            var defender = CreateStats(speed: 0);

            int hit = DamageCalculator.CalculateHitChance(ability, stats, defender);
            Assert.AreEqual(GameConstants.MaxHitChance, hit);

            var badAbility = CreateAbility(accuracy: 0);
            var slowAttacker = CreateStats(speed: 0);
            var fastDefender = CreateStats(speed: 100);

            int miss = DamageCalculator.CalculateHitChance(badAbility, slowAttacker, fastDefender);
            Assert.AreEqual(GameConstants.MinHitChance, miss);
        }

        [Test]
        public void HitChance_SpeedDifference_Matters()
        {
            var ability = CreateAbility(accuracy: 80);
            var fast = CreateStats(speed: 15);
            var slow = CreateStats(speed: 5);

            int fastVsSlow = DamageCalculator.CalculateHitChance(ability, fast, slow);
            int slowVsFast = DamageCalculator.CalculateHitChance(ability, slow, fast);

            Assert.Greater(fastVsSlow, slowVsFast);
        }

        [Test]
        public void Healing_ScalesWithMAAndAttunement()
        {
            var ability = CreateAbility(power: 10, healing: true);
            var caster = CreateStats(ma: 20);

            int fullAtt = DamageCalculator.CalculateHealing(ability, caster, 100);
            int halfAtt = DamageCalculator.CalculateHealing(ability, caster, 50);

            Assert.Greater(fullAtt, halfAtt);
            Assert.AreEqual(20, fullAtt); // 20 * 10 / 10 * 1.0
        }

        [Test]
        public void Healing_MinimumIsOne()
        {
            var ability = CreateAbility(power: 1, healing: true);
            var caster = CreateStats(ma: 1);

            int heal = DamageCalculator.CalculateHealing(ability, caster, 1);
            Assert.AreEqual(1, heal);
        }
    }
}
