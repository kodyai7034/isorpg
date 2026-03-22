using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IsoRPG.Core;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Units
{
    /// <summary>
    /// Centralized registry for unit lookup by ID, position, or team.
    /// Maintains internal indices for O(1) access. Subscribes to unit position
    /// change events to keep the position index current.
    ///
    /// Create one per battle. Call <see cref="Register"/> for each unit at battle start.
    /// </summary>
    public class UnitRegistry
    {
        private readonly Dictionary<EntityId, UnitInstance> _byId = new();
        private readonly Dictionary<Vector2Int, UnitInstance> _byPosition = new();
        private readonly List<UnitInstance> _all = new();

        /// <summary>Total registered units (including dead).</summary>
        public int Count => _all.Count;

        /// <summary>All currently living units.</summary>
        public IReadOnlyList<UnitInstance> AllLiving =>
            _all.Where(u => u.IsAlive).ToList().AsReadOnly();

        /// <summary>
        /// Register a unit at battle start. Subscribes to position change events
        /// to keep the position index current.
        /// </summary>
        /// <param name="unit">Unit to register.</param>
        /// <exception cref="ArgumentException">If a unit with the same ID is already registered.</exception>
        public void Register(UnitInstance unit)
        {
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));

            if (_byId.ContainsKey(unit.Id))
                throw new ArgumentException($"Unit with ID {unit.Id} is already registered.", nameof(unit));

            _byId[unit.Id] = unit;
            _byPosition[unit.GridPosition] = unit;
            _all.Add(unit);

            unit.OnPositionChanged += (from, to) => OnUnitMoved(unit, from, to);
        }

        /// <summary>
        /// Get unit by EntityId.
        /// </summary>
        /// <param name="id">Unit ID.</param>
        /// <returns>The unit, or null if not found.</returns>
        public UnitInstance GetById(EntityId id)
        {
            _byId.TryGetValue(id, out var unit);
            return unit;
        }

        /// <summary>
        /// Try to get unit by EntityId.
        /// </summary>
        public bool TryGetById(EntityId id, out UnitInstance unit)
        {
            return _byId.TryGetValue(id, out unit);
        }

        /// <summary>
        /// Get the living unit occupying a grid position.
        /// </summary>
        /// <param name="pos">Grid position.</param>
        /// <returns>The unit at that position, or null if empty or unit is dead.</returns>
        public UnitInstance GetAtPosition(Vector2Int pos)
        {
            if (_byPosition.TryGetValue(pos, out var unit) && unit.IsAlive)
                return unit;
            return null;
        }

        /// <summary>
        /// Get all living units on a team.
        /// </summary>
        /// <param name="team">Team index (0=player, 1=enemy, 2=neutral).</param>
        /// <returns>Read-only list of living units on that team.</returns>
        public IReadOnlyList<UnitInstance> GetTeam(int team)
        {
            return _all.Where(u => u.Team == team && u.IsAlive).ToList().AsReadOnly();
        }

        /// <summary>
        /// Check if a position is occupied by a living unit.
        /// </summary>
        public bool IsOccupied(Vector2Int pos)
        {
            return GetAtPosition(pos) != null;
        }

        private void OnUnitMoved(UnitInstance unit, Vector2Int from, Vector2Int to)
        {
            // Remove old position mapping (only if this unit is still at that position)
            if (_byPosition.TryGetValue(from, out var occupant) && occupant == unit)
                _byPosition.Remove(from);

            // Set new position mapping
            _byPosition[to] = unit;
        }
    }
}
