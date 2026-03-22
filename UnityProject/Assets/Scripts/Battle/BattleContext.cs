using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;
using IsoRPG.Units;
using EntityId = IsoRPG.Core.EntityId;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Shared context passed to all battle states. Holds references to all
    /// battle subsystems so states can access them without direct coupling.
    /// </summary>
    public class BattleContext
    {
        // --- Map & Grid ---

        /// <summary>Static map data.</summary>
        public BattleMapData Map { get; set; }

        /// <summary>Grid view for overlays and tile interaction.</summary>
        public IsometricGrid Grid { get; set; }

        // --- Units ---

        /// <summary>Centralized unit lookup by ID, position, team.</summary>
        public UnitRegistry Registry { get; set; }

        /// <summary>All units in the battle (including dead).</summary>
        public List<UnitInstance> AllUnits { get; set; } = new();

        /// <summary>Map from EntityId to UnitView for animation coordination.</summary>
        public Dictionary<EntityId, UnitView> UnitViews { get; set; } = new();

        // --- Turn State ---

        /// <summary>The unit currently taking their turn.</summary>
        public UnitInstance ActiveUnit { get; set; }

        /// <summary>Total turns elapsed in this battle.</summary>
        public int TurnNumber { get; set; }

        /// <summary>Whether the active unit has moved this turn.</summary>
        public bool ActiveUnitMoved { get; set; }

        /// <summary>Whether the active unit has acted this turn.</summary>
        public bool ActiveUnitActed { get; set; }

        /// <summary>Number of commands executed during the current turn (for undo tracking).</summary>
        public int TurnCommandCount { get; set; }

        // --- Systems ---

        /// <summary>Command history for undo/rewind.</summary>
        public CommandHistory CommandHistory { get; set; }

        /// <summary>Movement controller for pathfinding UI.</summary>
        public MovementController MovementController { get; set; }

        /// <summary>Deterministic RNG.</summary>
        public IGameRng Rng { get; set; }

        /// <summary>Rewind system for CHARIOT-style multi-turn undo.</summary>
        public RewindSystem RewindSystem { get; set; }

        /// <summary>
        /// Default abilities available to all units. Placeholder until Job System (System 9)
        /// provides per-unit ability lists. Set by BattleManager at battle start.
        /// </summary>
        public AbilityData[] DefaultAbilities { get; set; } = System.Array.Empty<AbilityData>();

        // --- Queries ---

        /// <summary>Get the UnitView for a unit, or null if not found.</summary>
        public UnitView GetUnitView(EntityId id)
        {
            UnitViews.TryGetValue(id, out var view);
            return view;
        }

        /// <summary>Check if all units on a team are dead.</summary>
        public bool IsTeamDefeated(int team)
        {
            var teamUnits = Registry.GetTeam(team);
            return teamUnits.Count == 0;
        }
    }
}
