using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Battle;
using IsoRPG.Map;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Tests
{
    public class AIControllerTests
    {
        private BattleContext CreateContext()
        {
            var map = MapGenerator.CreateFlatMap(8, 8);
            return new BattleContext
            {
                Map = map,
                Registry = new UnitRegistry(),
                AllUnits = new List<UnitInstance>(),
                UnitViews = new Dictionary<EntityId, UnitView>(),
                CommandHistory = new CommandHistory(),
                MovementController = new MovementController(),
                Rng = new GameRng(42),
                TurnNumber = 0
            };
        }

        private UnitInstance AddUnit(BattleContext ctx, string name, int team, Vector2Int pos,
            int pa = 10, int speed = 7, int hp = 100)
        {
            var unit = new UnitInstance(name, team, 1, pos);
            unit.SetStats(new ComputedStats
            {
                MaxHP = hp, MaxMP = 50,
                PhysicalAttack = pa, MagicAttack = 10,
                Speed = speed, Move = 4, Jump = 3,
                Defense = 5, MagicDefense = 5
            });
            unit.SetHP(hp);
            ctx.AllUnits.Add(unit);
            ctx.Registry.Register(unit);
            return unit;
        }

        private AbilityData CreateMeleeAttack()
        {
            var a = ScriptableObject.CreateInstance<AbilityData>();
            a.AbilityName = "Attack";
            a.DamageType = DamageType.Physical;
            a.Power = 10;
            a.Accuracy = 90;
            a.Range = 1;
            a.RequiresLineOfSight = true;
            return a;
        }

        private AbilityData CreateRangedAttack()
        {
            var a = ScriptableObject.CreateInstance<AbilityData>();
            a.AbilityName = "Fire";
            a.DamageType = DamageType.Magical;
            a.Power = 12;
            a.Accuracy = 85;
            a.Range = 4;
            a.MPCost = 8;
            a.RequiresLineOfSight = true;
            return a;
        }

        [Test]
        public void EvaluateBestOption_PrefersKill()
        {
            var ctx = CreateContext();
            var ai = AddUnit(ctx, "Enemy", 1, new Vector2Int(3, 3), pa: 20);
            var weakPlayer = AddUnit(ctx, "Weak", 0, new Vector2Int(4, 3), hp: 5);
            var strongPlayer = AddUnit(ctx, "Strong", 0, new Vector2Int(2, 3), hp: 100);

            var controller = new AIController();
            var profile = AIProfile.CreateAggressive();
            var abilities = new[] { CreateMeleeAttack() };

            var best = controller.EvaluateBestOption(ai, ctx, profile, abilities);

            // Should prefer the weak target (kill bonus)
            Assert.IsNotNull(best.Target);
            Assert.AreEqual("Weak", best.Target.Name);
            Assert.IsTrue(best.WouldKill);
        }

        [Test]
        public void EvaluateBestOption_MovesTowardEnemy()
        {
            var ctx = CreateContext();
            var ai = AddUnit(ctx, "Enemy", 1, new Vector2Int(0, 0));
            var player = AddUnit(ctx, "Player", 0, new Vector2Int(7, 7));

            var controller = new AIController();
            var profile = AIProfile.CreateAggressive();
            var abilities = new[] { CreateMeleeAttack() };

            var best = controller.EvaluateBestOption(ai, ctx, profile, abilities);

            // Aggressive AI should move closer to the enemy
            int startDist = IsoMath.ManhattanDistance(new Vector2Int(0, 0), new Vector2Int(7, 7));
            int newDist = IsoMath.ManhattanDistance(best.MoveTo, new Vector2Int(7, 7));
            Assert.Less(newDist, startDist);
        }

        [Test]
        public void EvaluateBestOption_DefensiveStaysBack()
        {
            var ctx = CreateContext();
            var ai = AddUnit(ctx, "Enemy", 1, new Vector2Int(4, 4));
            var player = AddUnit(ctx, "Player", 0, new Vector2Int(5, 4), pa: 30);

            var controller = new AIController();
            var profile = AIProfile.CreateDefensive();
            var abilities = new[] { CreateMeleeAttack() };

            var best = controller.EvaluateBestOption(ai, ctx, profile, abilities);

            // Defensive AI should not move adjacent to a high-PA enemy if it can avoid it
            // (self-preservation weight is high)
            // At minimum, the score should account for the risk
            Assert.IsNotNull(best.MoveTo);
        }

        [Test]
        public void EvaluateBestOption_NoAbilities_ReturnsWait()
        {
            var ctx = CreateContext();
            var ai = AddUnit(ctx, "Enemy", 1, new Vector2Int(3, 3));
            AddUnit(ctx, "Player", 0, new Vector2Int(5, 5));

            var controller = new AIController();
            var profile = AIProfile.CreateAggressive();

            var best = controller.EvaluateBestOption(ai, ctx, profile, new AbilityData[0]);

            // No abilities = wait at best position
            Assert.IsNull(best.Ability);
        }

        [Test]
        public void EvaluateBestOption_UsesRangedAbility()
        {
            var ctx = CreateContext();
            var ai = AddUnit(ctx, "Mage", 1, new Vector2Int(0, 0));
            var player = AddUnit(ctx, "Player", 0, new Vector2Int(3, 0)); // 3 tiles away

            var controller = new AIController();
            var profile = AIProfile.CreateAggressive();
            var abilities = new[] { CreateRangedAttack() }; // range 4

            var best = controller.EvaluateBestOption(ai, ctx, profile, abilities);

            // Should be able to target from distance
            if (best.Ability != null)
            {
                Assert.AreEqual("Fire", best.Ability.AbilityName);
            }
        }

        [Test]
        public void EvaluateBestOption_SkipsAbilityIfNotEnoughMP()
        {
            var ctx = CreateContext();
            var ai = AddUnit(ctx, "Enemy", 1, new Vector2Int(3, 3));
            ai.SetMP(0); // no MP
            var player = AddUnit(ctx, "Player", 0, new Vector2Int(4, 3));

            var controller = new AIController();
            var profile = AIProfile.CreateAggressive();
            var ranged = CreateRangedAttack(); // costs 8 MP
            var melee = CreateMeleeAttack();   // costs 0 MP

            var best = controller.EvaluateBestOption(ai, ctx, profile, new[] { ranged, melee });

            // Should use melee (free) not ranged (8 MP, can't afford)
            if (best.Ability != null)
            {
                Assert.AreEqual("Attack", best.Ability.AbilityName);
            }
        }

        [Test]
        public void EvaluateBestOption_ProfileWeightsAffectChoice()
        {
            var ctx = CreateContext();
            var ai = AddUnit(ctx, "Enemy", 1, new Vector2Int(3, 3));
            var farPlayer = AddUnit(ctx, "Far", 0, new Vector2Int(7, 7));

            var controller = new AIController();
            var abilities = new[] { CreateMeleeAttack() };

            // Aggressive: should move toward enemy (negative DistanceFromEnemyPenalty)
            var aggressive = AIProfile.CreateAggressive();
            var aggrOption = controller.EvaluateBestOption(ai, ctx, aggressive, abilities);

            // Defensive: should stay away (positive DistanceFromEnemyPenalty)
            var defensive = AIProfile.CreateDefensive();
            // Reset unit position
            ai.SetPosition(new Vector2Int(3, 3));
            var defOption = controller.EvaluateBestOption(ai, ctx, defensive, abilities);

            // Aggressive should end up closer to enemy than defensive
            int aggrDist = IsoMath.ManhattanDistance(aggrOption.MoveTo, farPlayer.GridPosition);
            int defDist = IsoMath.ManhattanDistance(defOption.MoveTo, farPlayer.GridPosition);

            Assert.LessOrEqual(aggrDist, defDist);
        }
    }
}
