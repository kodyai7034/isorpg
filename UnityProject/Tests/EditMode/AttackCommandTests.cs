using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Tests
{
    public class AttackCommandTests
    {
        private BattleMapData _map;
        private IGameRng _rng;

        [SetUp]
        public void SetUp()
        {
            _map = MapGenerator.CreateFlatMap(8, 8);
            _rng = new GameRng(42);
        }

        private UnitInstance CreateUnit(string name, int team, Vector2Int pos, int pa = 10, int ma = 10)
        {
            var unit = new UnitInstance(name, team, 1, pos);
            unit.SetStats(new ComputedStats
            {
                MaxHP = 100, MaxMP = 50,
                PhysicalAttack = pa, MagicAttack = ma,
                Speed = 7, Move = 4, Jump = 3,
                Defense = 5, MagicDefense = 5
            });
            unit.SetHP(100);
            return unit;
        }

        private AbilityData CreateAbility(string name = "Attack", DamageType type = DamageType.Physical,
            int power = 10, int accuracy = 100, int mpCost = 0, bool healing = false,
            bool appliesStatus = false, StatusType status = StatusType.Poison, int statusChance = 100)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.AbilityName = name;
            ability.DamageType = type;
            ability.Power = power;
            ability.Accuracy = accuracy;
            ability.MPCost = mpCost;
            ability.IsHealing = healing;
            ability.AppliesStatus = appliesStatus;
            ability.AppliedStatus = status;
            ability.StatusChance = statusChance;
            ability.StatusDuration = 3;
            ability.Range = 1;
            ability.RequiresLineOfSight = true;
            return ability;
        }

        [Test]
        public void Execute_DealsDamage()
        {
            var attacker = CreateUnit("Attacker", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Target", 1, new Vector2Int(1, 0));
            var ability = CreateAbility(accuracy: 100);

            // Use a guaranteed-hit RNG
            var cmd = new AttackCommand(attacker, target, ability, _map, new GameRng(1));
            cmd.Execute();

            Assert.IsTrue(cmd.DidHit);
            Assert.Greater(cmd.DamageDealt, 0);
            Assert.Less(target.CurrentHP, 100);
        }

        [Test]
        public void Undo_RestoresHP()
        {
            var attacker = CreateUnit("Attacker", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Target", 1, new Vector2Int(1, 0));
            var ability = CreateAbility(accuracy: 100);

            var cmd = new AttackCommand(attacker, target, ability, _map, new GameRng(1));
            cmd.Execute();
            int hpAfterAttack = target.CurrentHP;
            Assert.Less(hpAfterAttack, 100);

            cmd.Undo();
            Assert.AreEqual(100, target.CurrentHP);
        }

        [Test]
        public void Execute_ConsumesMP()
        {
            var attacker = CreateUnit("Attacker", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Target", 1, new Vector2Int(1, 0));
            var ability = CreateAbility(mpCost: 8);

            var cmd = new AttackCommand(attacker, target, ability, _map, new GameRng(1));
            cmd.Execute();

            Assert.AreEqual(42, attacker.CurrentMP); // 50 - 8
        }

        [Test]
        public void Undo_RestoresMP()
        {
            var attacker = CreateUnit("Attacker", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Target", 1, new Vector2Int(1, 0));
            var ability = CreateAbility(mpCost: 8);

            var cmd = new AttackCommand(attacker, target, ability, _map, new GameRng(1));
            cmd.Execute();
            cmd.Undo();

            Assert.AreEqual(50, attacker.CurrentMP);
        }

        [Test]
        public void Healing_RestoresHP()
        {
            var caster = CreateUnit("Healer", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Wounded", 0, new Vector2Int(1, 0));
            target.ApplyDamage(50); // HP = 50

            var ability = CreateAbility("Cure", healing: true, power: 10);
            var cmd = new AttackCommand(caster, target, ability, _map, _rng);
            cmd.Execute();

            Assert.IsTrue(cmd.DidHit);
            Assert.Greater(cmd.HealingDone, 0);
            Assert.Greater(target.CurrentHP, 50);
        }

        [Test]
        public void StatusEffect_Applied()
        {
            var attacker = CreateUnit("Attacker", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Target", 1, new Vector2Int(1, 0));
            var ability = CreateAbility("Poison Strike",
                appliesStatus: true, status: StatusType.Poison, statusChance: 100);

            var cmd = new AttackCommand(attacker, target, ability, _map, new GameRng(1));
            cmd.Execute();

            if (cmd.DidHit)
            {
                Assert.IsTrue(target.HasStatus(StatusType.Poison));
            }
        }

        [Test]
        public void Undo_RemovesAppliedStatus()
        {
            var attacker = CreateUnit("Attacker", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Target", 1, new Vector2Int(1, 0));
            var ability = CreateAbility("Poison Strike",
                appliesStatus: true, status: StatusType.Poison, statusChance: 100);

            var cmd = new AttackCommand(attacker, target, ability, _map, new GameRng(1));
            cmd.Execute();
            cmd.Undo();

            Assert.IsFalse(target.HasStatus(StatusType.Poison));
        }

        [Test]
        public void Execute_FiresDamageEvent()
        {
            var attacker = CreateUnit("Attacker", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Target", 1, new Vector2Int(1, 0));
            var ability = CreateAbility(accuracy: 100);

            DamageDealtArgs received = default;
            GameEvents.DamageDealt.Subscribe(args => received = args);

            try
            {
                var cmd = new AttackCommand(attacker, target, ability, _map, new GameRng(1));
                cmd.Execute();

                if (cmd.DidHit)
                {
                    Assert.AreEqual(attacker.Id, received.AttackerId);
                    Assert.AreEqual(target.Id, received.TargetId);
                    Assert.Greater(received.Amount, 0);
                }
            }
            finally
            {
                GameEvents.DamageDealt.Clear();
            }
        }

        [Test]
        public void Execute_ViaCommandHistory_UndoWorks()
        {
            var history = new CommandHistory();
            var attacker = CreateUnit("Attacker", 0, new Vector2Int(0, 0));
            var target = CreateUnit("Target", 1, new Vector2Int(1, 0));
            var ability = CreateAbility(accuracy: 100);

            history.ExecuteCommand(new AttackCommand(attacker, target, ability, _map, new GameRng(1)));
            int hpAfter = target.CurrentHP;

            history.Undo();
            Assert.AreEqual(100, target.CurrentHP);
        }
    }

    public class StatusEffectTests
    {
        [Test]
        public void Poison_DealsDamageOnTick()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.SetStats(new ComputedStats { MaxHP = 100, Speed = 7, Move = 4, Jump = 3 });
            unit.SetHP(100);

            var poison = new StatusEffectInstance(StatusType.Poison, 3);
            unit.AddStatus(poison);

            unit.TickStatuses();

            Assert.Less(unit.CurrentHP, 100);
            Assert.AreEqual(2, unit.StatusEffects[0].RemainingDuration);
        }

        [Test]
        public void Regen_HealsOnTick()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.SetStats(new ComputedStats { MaxHP = 100, Speed = 7, Move = 4, Jump = 3 });
            unit.SetHP(50);

            var regen = new StatusEffectInstance(StatusType.Regen, 3);
            unit.AddStatus(regen);

            unit.TickStatuses();

            Assert.Greater(unit.CurrentHP, 50);
        }

        [Test]
        public void Status_ExpiresAfterDuration()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.SetStats(new ComputedStats { MaxHP = 100, Speed = 7, Move = 4, Jump = 3 });
            unit.SetHP(100);

            var poison = new StatusEffectInstance(StatusType.Poison, 2);
            unit.AddStatus(poison);

            unit.TickStatuses(); // duration 2 → 1
            Assert.AreEqual(1, unit.StatusEffects.Count);

            unit.TickStatuses(); // duration 1 → 0, removed
            Assert.AreEqual(0, unit.StatusEffects.Count);
        }

        [Test]
        public void HasStatus_DetectsActive()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            Assert.IsFalse(unit.HasStatus(StatusType.Haste));

            unit.AddStatus(new StatusEffectInstance(StatusType.Haste, 3));
            Assert.IsTrue(unit.HasStatus(StatusType.Haste));
        }

        [Test]
        public void AddStatus_SameType_Refreshes()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            unit.AddStatus(new StatusEffectInstance(StatusType.Poison, 1));
            unit.AddStatus(new StatusEffectInstance(StatusType.Poison, 5));

            Assert.AreEqual(1, unit.StatusEffects.Count);
            Assert.AreEqual(5, unit.StatusEffects[0].RemainingDuration);
        }

        [Test]
        public void RemoveStatus_ById_Works()
        {
            var unit = new UnitInstance("Test", 0, 1, Vector2Int.zero);
            var status = new StatusEffectInstance(StatusType.Protect, 3);
            unit.AddStatus(status);

            unit.RemoveStatus(status.Id);
            Assert.AreEqual(0, unit.StatusEffects.Count);
        }
    }
}
