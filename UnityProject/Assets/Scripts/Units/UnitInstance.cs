using System;
using UnityEngine;
using System.Collections.Generic;
using IsoRPG.Core;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Units
{
    /// <summary>
    /// Computed stats after applying job modifiers and equipment.
    /// Recalculated when job or equipment changes.
    /// </summary>
    [Serializable]
    public struct ComputedStats
    {
        public int MaxHP, MaxMP;
        public int PhysicalAttack, MagicAttack;
        public int Speed, Move, Jump;
        public int Defense, MagicDefense;

        /// <summary>
        /// Placeholder stat generation from level. Will be replaced by job+equipment calculation in System 9.
        /// </summary>
        public static ComputedStats FromBase(int level, int brave, int faith)
        {
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

    /// <summary>
    /// Pure C# data model for a battle unit. No MonoBehaviour dependency.
    /// All state mutations go through explicit methods that fire events.
    ///
    /// External code should never set properties directly — use mutation methods
    /// (SetPosition, ApplyDamage, etc.) so events fire correctly for views/UI.
    /// </summary>
    public class UnitInstance
    {
        // --- Identity (immutable after construction) ---

        /// <summary>Globally unique identifier. Survives serialization.</summary>
        public EntityId Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>Team affiliation. 0=player, 1=enemy, 2=neutral.</summary>
        public int Team { get; }

        // --- Position ---

        /// <summary>Current grid position. Changed via <see cref="SetPosition"/>.</summary>
        public Vector2Int GridPosition { get; private set; }

        /// <summary>Current facing direction. Changed via <see cref="SetFacing"/>.</summary>
        public Direction Facing { get; private set; }

        // --- Level & Stats ---

        /// <summary>Character level.</summary>
        public int Level { get; private set; }

        /// <summary>Computed stats (base + job + equipment). Recalculated on job/equip change.</summary>
        public ComputedStats Stats { get; private set; }

        /// <summary>
        /// Update computed stats. Called when job, level, or equipment changes.
        /// </summary>
        /// <param name="stats">New computed stats.</param>
        public void SetStats(ComputedStats stats)
        {
            Stats = stats;
        }

        /// <summary>Brave stat (0-100). Affects physical reactions and certain weapons.</summary>
        public int Brave { get; private set; }

        /// <summary>Faith stat (0-100). Multiplier for magic offense and defense.</summary>
        public int Faith { get; private set; }

        // --- HP / MP ---

        /// <summary>Current hit points. 0 = dead.</summary>
        public int CurrentHP { get; private set; }

        /// <summary>Current magic points.</summary>
        public int CurrentMP { get; private set; }

        /// <summary>Whether the unit is alive (HP > 0).</summary>
        public bool IsAlive => CurrentHP > 0;

        // --- CT ---

        /// <summary>
        /// Charge Time counter. Public set for CTSystem direct manipulation.
        /// When CT >= 100, unit gets an active turn.
        /// </summary>
        public int CT { get; set; }

        // --- Job (placeholder until System 9) ---

        /// <summary>Current job/class.</summary>
        public JobId CurrentJob { get; private set; }

        /// <summary>Job levels per job.</summary>
        public Dictionary<JobId, int> JobLevels { get; } = new();

        /// <summary>Accumulated job points per job.</summary>
        public Dictionary<JobId, int> JobPoints { get; } = new();

        /// <summary>Set of permanently learned ability IDs.</summary>
        public HashSet<int> LearnedAbilities { get; } = new();

        // --- Events ---

        /// <summary>Fired when HP changes. Args: (oldHP, newHP).</summary>
        public event Action<int, int> OnHPChanged;

        /// <summary>Fired when position changes. Args: (from, to).</summary>
        public event Action<Vector2Int, Vector2Int> OnPositionChanged;

        /// <summary>Fired when the unit dies (HP reaches 0).</summary>
        public event Action OnDied;

        /// <summary>Fired when facing direction changes.</summary>
        public event Action<Direction> OnFacingChanged;

        // --- Constructor ---

        /// <summary>
        /// Create a new unit instance with generated EntityId.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="team">Team (0=player, 1=enemy, 2=neutral).</param>
        /// <param name="level">Starting level.</param>
        /// <param name="position">Starting grid position.</param>
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

        // --- Mutation Methods ---

        /// <summary>
        /// Set grid position and fire OnPositionChanged.
        /// Used by MoveCommand — do not call directly from game logic.
        /// </summary>
        /// <param name="newPos">New grid position.</param>
        public void SetPosition(Vector2Int newPos)
        {
            var oldPos = GridPosition;
            GridPosition = newPos;
            OnPositionChanged?.Invoke(oldPos, newPos);
        }

        /// <summary>
        /// Set facing direction and fire OnFacingChanged.
        /// </summary>
        /// <param name="dir">New facing direction.</param>
        public void SetFacing(Direction dir)
        {
            Facing = dir;
            OnFacingChanged?.Invoke(dir);
        }

        /// <summary>
        /// Apply damage to the unit. Clamps HP to 0.
        /// Fires OnHPChanged. Fires OnDied if HP reaches 0.
        /// </summary>
        /// <param name="amount">Damage amount (positive integer).</param>
        public void ApplyDamage(int amount)
        {
            if (amount <= 0) return;
            int oldHP = CurrentHP;
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnHPChanged?.Invoke(oldHP, CurrentHP);

            if (CurrentHP == 0 && oldHP > 0)
                OnDied?.Invoke();
        }

        /// <summary>
        /// Apply healing to the unit. Clamps HP to MaxHP.
        /// Fires OnHPChanged.
        /// </summary>
        /// <param name="amount">Heal amount (positive integer).</param>
        public void ApplyHealing(int amount)
        {
            if (amount <= 0) return;
            int oldHP = CurrentHP;
            CurrentHP = Mathf.Min(Stats.MaxHP, CurrentHP + amount);
            OnHPChanged?.Invoke(oldHP, CurrentHP);
        }

        /// <summary>
        /// Directly set HP to a specific value. Used by command Undo.
        /// Fires OnHPChanged. Fires OnDied if new HP is 0 and old was > 0.
        /// </summary>
        /// <param name="hp">Target HP value.</param>
        public void SetHP(int hp)
        {
            int oldHP = CurrentHP;
            CurrentHP = Mathf.Clamp(hp, 0, Stats.MaxHP);
            if (CurrentHP != oldHP)
            {
                OnHPChanged?.Invoke(oldHP, CurrentHP);
                if (CurrentHP == 0 && oldHP > 0)
                    OnDied?.Invoke();
            }
        }

        /// <summary>
        /// Directly set MP to a specific value. Used by command Undo.
        /// </summary>
        /// <param name="mp">Target MP value.</param>
        public void SetMP(int mp)
        {
            CurrentMP = Mathf.Clamp(mp, 0, Stats.MaxMP);
        }

        public override string ToString()
        {
            return $"{Name} [Team {Team}] HP:{CurrentHP}/{Stats.MaxHP} CT:{CT} @({GridPosition.x},{GridPosition.y})";
        }
    }
}
