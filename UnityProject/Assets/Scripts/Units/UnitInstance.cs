using UnityEngine;
using System.Collections.Generic;
using IsoRPG.Core;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Units
{
    [System.Serializable]
    public struct ComputedStats
    {
        public int MaxHP, MaxMP;
        public int PhysicalAttack, MagicAttack;
        public int Speed, Move, Jump;
        public int Defense, MagicDefense;

        public static ComputedStats FromBase(int level, int brave, int faith)
        {
            // Placeholder — will be driven by job modifiers + equipment
            return new ComputedStats
            {
                MaxHP = 80 + level * 10,
                MaxMP = 30 + level * 5,
                PhysicalAttack = 5 + level * 2,
                MagicAttack = 5 + level * 2,
                Speed = 6 + level,
                Move = 4,
                Jump = 3,
                Defense = 2 + level,
                MagicDefense = 2 + level
            };
        }
    }

    public class UnitInstance
    {
        public EntityId Id { get; }
        public string Name { get; set; }
        public int Team { get; set; }
        public int Level { get; set; }

        public Vector2Int GridPosition { get; set; }
        public Direction Facing { get; set; }

        public int CurrentHP { get; set; }
        public int CurrentMP { get; set; }
        public ComputedStats Stats { get; set; }

        public int Brave { get; set; }
        public int Faith { get; set; }

        public int CT { get; set; }

        public JobId CurrentJob { get; set; }
        public Dictionary<JobId, int> JobLevels { get; } = new();
        public Dictionary<JobId, int> JobPoints { get; } = new();
        public HashSet<int> LearnedAbilities { get; } = new();

        public bool IsAlive => CurrentHP > 0;

        public UnitInstance(string name, int team, int level, Vector2Int position)
        {
            Id = EntityId.New();
            Name = name;
            Team = team;
            Level = level;
            GridPosition = position;
            Facing = Direction.South;
            Brave = 70;
            Faith = 70;
            CurrentJob = JobId.Squire;
            CT = 0;

            Stats = ComputedStats.FromBase(level, Brave, Faith);
            CurrentHP = Stats.MaxHP;
            CurrentMP = Stats.MaxMP;
        }

        public void TakeDamage(int amount)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
        }

        public void Heal(int amount)
        {
            CurrentHP = Mathf.Min(Stats.MaxHP, CurrentHP + amount);
        }

        public override string ToString()
        {
            return $"{Name} (Team {Team}) HP:{CurrentHP}/{Stats.MaxHP} CT:{CT} Pos:{GridPosition}";
        }
    }
}
