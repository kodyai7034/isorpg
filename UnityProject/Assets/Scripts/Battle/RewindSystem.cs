using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Snapshot of a single unit's state at a point in time.
    /// </summary>
    [System.Serializable]
    public struct UnitSnapshot
    {
        public EntityId Id;
        public Vector2Int Position;
        public Direction Facing;
        public int CurrentHP;
        public int CurrentMP;
        public int CT;
        public List<StatusSnapshot> Statuses;
    }

    /// <summary>
    /// Snapshot of a status effect instance.
    /// </summary>
    [System.Serializable]
    public struct StatusSnapshot
    {
        public EntityId Id;
        public StatusType Type;
        public int RemainingDuration;
    }

    /// <summary>
    /// Full snapshot of the battle state at a point in time.
    /// Captured before each command for rewind support.
    /// </summary>
    [System.Serializable]
    public class BattleSnapshot
    {
        /// <summary>Turn number at time of snapshot.</summary>
        public int TurnNumber;
        /// <summary>RNG seed at time of snapshot.</summary>
        public int RngSeed;
        /// <summary>ID of the active unit at time of snapshot.</summary>
        public EntityId ActiveUnitId;
        /// <summary>Per-unit state snapshots.</summary>
        public Dictionary<EntityId, UnitSnapshot> Units;
        /// <summary>Whether the active unit had moved.</summary>
        public bool ActiveUnitMoved;
        /// <summary>Whether the active unit had acted.</summary>
        public bool ActiveUnitActed;
        /// <summary>Command count for the active turn.</summary>
        public int TurnCommandCount;
    }

    /// <summary>
    /// Captures and restores battle state snapshots for CHARIOT-style rewind.
    /// Works alongside CommandHistory — snapshots are the safety net,
    /// command Undo is the primary mechanism.
    ///
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class RewindSystem
    {
        private readonly List<BattleSnapshot> _snapshots = new();
        private readonly int _maxSnapshots;

        /// <summary>Number of stored snapshots.</summary>
        public int SnapshotCount => _snapshots.Count;

        /// <summary>
        /// Create a rewind system.
        /// </summary>
        /// <param name="maxSnapshots">Maximum snapshots to retain (matches CommandHistory max).</param>
        public RewindSystem(int maxSnapshots = GameConstants.MaxCommandHistory)
        {
            _maxSnapshots = maxSnapshots;
        }

        /// <summary>
        /// Capture a snapshot of the current battle state.
        /// Called before each command execution.
        /// </summary>
        /// <param name="ctx">Current battle context.</param>
        /// <returns>The captured snapshot.</returns>
        public BattleSnapshot CaptureSnapshot(BattleContext ctx)
        {
            var snapshot = new BattleSnapshot
            {
                TurnNumber = ctx.TurnNumber,
                RngSeed = ctx.Rng?.Seed ?? 0,
                ActiveUnitId = ctx.ActiveUnit?.Id ?? EntityId.None,
                ActiveUnitMoved = ctx.ActiveUnitMoved,
                ActiveUnitActed = ctx.ActiveUnitActed,
                TurnCommandCount = ctx.TurnCommandCount,
                Units = new Dictionary<EntityId, UnitSnapshot>()
            };

            foreach (var unit in ctx.AllUnits)
            {
                var unitSnap = new UnitSnapshot
                {
                    Id = unit.Id,
                    Position = unit.GridPosition,
                    Facing = unit.Facing,
                    CurrentHP = unit.CurrentHP,
                    CurrentMP = unit.CurrentMP,
                    CT = unit.CT,
                    Statuses = new List<StatusSnapshot>()
                };

                foreach (var status in unit.StatusEffects)
                {
                    unitSnap.Statuses.Add(new StatusSnapshot
                    {
                        Id = status.Id,
                        Type = status.Type,
                        RemainingDuration = status.RemainingDuration
                    });
                }

                snapshot.Units[unit.Id] = unitSnap;
            }

            _snapshots.Add(snapshot);

            // Evict oldest if over capacity
            while (_snapshots.Count > _maxSnapshots)
                _snapshots.RemoveAt(0);

            return snapshot;
        }

        /// <summary>
        /// Restore a snapshot, resetting all unit state to the captured values.
        /// </summary>
        /// <param name="ctx">Battle context to restore into.</param>
        /// <param name="snapshot">Snapshot to restore.</param>
        public void RestoreSnapshot(BattleContext ctx, BattleSnapshot snapshot)
        {
            // Restore context state
            ctx.TurnNumber = snapshot.TurnNumber;
            ctx.ActiveUnitMoved = snapshot.ActiveUnitMoved;
            ctx.ActiveUnitActed = snapshot.ActiveUnitActed;
            ctx.TurnCommandCount = snapshot.TurnCommandCount;

            // Restore RNG
            ctx.Rng?.SetSeed(snapshot.RngSeed);

            // Restore active unit reference
            if (snapshot.ActiveUnitId.IsValid)
                ctx.ActiveUnit = ctx.AllUnits.FirstOrDefault(u => u.Id == snapshot.ActiveUnitId);

            // Restore each unit's state
            foreach (var unit in ctx.AllUnits)
            {
                if (!snapshot.Units.TryGetValue(unit.Id, out var unitSnap))
                    continue;

                unit.SetPosition(unitSnap.Position);
                unit.SetFacing(unitSnap.Facing);
                unit.SetHP(unitSnap.CurrentHP);
                unit.SetMP(unitSnap.CurrentMP);
                unit.CT = unitSnap.CT;

                // Restore statuses
                unit.StatusEffects.Clear();
                foreach (var statusSnap in unitSnap.Statuses)
                {
                    unit.AddStatus(new StatusEffectInstance(
                        statusSnap.Id, statusSnap.Type, statusSnap.RemainingDuration));
                }
            }
        }

        /// <summary>
        /// Rewind N commands by restoring the snapshot from N commands ago.
        /// Also undoes commands via CommandHistory for consistency.
        /// </summary>
        /// <param name="ctx">Battle context.</param>
        /// <param name="count">Number of commands to rewind.</param>
        public void RewindCommands(BattleContext ctx, int count)
        {
            int targetIndex = _snapshots.Count - count;
            if (targetIndex < 0) targetIndex = 0;
            if (targetIndex >= _snapshots.Count) return;

            var targetSnapshot = _snapshots[targetIndex];

            // Undo commands via CommandHistory
            ctx.CommandHistory.UndoMultiple(count);

            // Restore snapshot as safety net
            RestoreSnapshot(ctx, targetSnapshot);

            // Remove snapshots after the target
            if (targetIndex + 1 < _snapshots.Count)
                _snapshots.RemoveRange(targetIndex + 1, _snapshots.Count - targetIndex - 1);
        }

        /// <summary>
        /// Get the most recent snapshot (for display in rewind UI).
        /// </summary>
        public BattleSnapshot GetLatestSnapshot()
        {
            return _snapshots.Count > 0 ? _snapshots[_snapshots.Count - 1] : null;
        }

        /// <summary>
        /// Clear all snapshots (e.g., on battle end).
        /// </summary>
        public void Clear()
        {
            _snapshots.Clear();
        }
    }
}
