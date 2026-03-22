using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Coordinates between pathfinding logic, grid overlays, and command creation.
    /// Pure C# class — no MonoBehaviour. Does not execute commands or trigger animations.
    ///
    /// Separation of concerns:
    /// - MovementController: calculates and displays options
    /// - Battle state machine: decides when to execute commands
    /// - UnitView: handles animation after command execution
    /// </summary>
    public class MovementController
    {
        private PathfindingResult _currentResult;
        private List<Vector2Int> _currentPathPreview;

        /// <summary>Whether a movement range is currently being displayed.</summary>
        public bool IsShowingRange => _currentResult != null;

        /// <summary>The current pathfinding result (null if not showing range).</summary>
        public PathfindingResult CurrentResult => _currentResult;

        /// <summary>
        /// Calculate and display movement range for a unit.
        /// Highlights all reachable tiles on the grid.
        /// </summary>
        /// <param name="unit">The unit to calculate movement for.</param>
        /// <param name="map">Battle map data.</param>
        /// <param name="allUnits">All units on the battlefield.</param>
        /// <param name="grid">Grid view for overlay display.</param>
        /// <param name="moveParams">Optional override for movement params. If null, uses unit stats.</param>
        /// <returns>Pathfinding result for subsequent path queries.</returns>
        public PathfindingResult ShowMovementRange(
            UnitInstance unit, BattleMapData map, List<UnitInstance> allUnits,
            IsometricGrid grid, MovementParams? moveParams = null)
        {
            var resolvedParams = moveParams ?? MovementParams.FromUnit(unit);

            _currentResult = Pathfinder.GetReachableTiles(
                map, unit.GridPosition, resolvedParams, unit.Team, allUnits);

            // Display overlay on stoppable tiles (excluding the unit's current position)
            var overlayTiles = new List<Vector2Int>();
            foreach (var pos in _currentResult.StoppableTiles.Keys)
            {
                if (pos != unit.GridPosition)
                    overlayTiles.Add(pos);
            }
            grid.SetTileOverlay(overlayTiles, TileVisualState.MoveRange);

            return _currentResult;
        }

        /// <summary>
        /// Preview the path to a specific tile. Highlights path tiles
        /// with PathPreview state while keeping MoveRange overlay on other tiles.
        /// </summary>
        /// <param name="grid">Grid view for overlay display.</param>
        /// <param name="start">Unit's current position.</param>
        /// <param name="target">Destination tile to preview path to.</param>
        /// <returns>The path, or null if target is not reachable.</returns>
        public List<Vector2Int> PreviewPathTo(
            IsometricGrid grid, Vector2Int start, Vector2Int target)
        {
            if (_currentResult == null || !_currentResult.CanMoveTo(target))
                return null;

            // Clear previous path preview (restore to MoveRange)
            ClearPathPreview(grid);

            // Calculate path
            var path = Pathfinder.ReconstructPath(_currentResult, start, target);
            if (path == null) return null;

            // Show path preview
            grid.SetTileOverlay(path, TileVisualState.PathPreview);
            _currentPathPreview = new List<Vector2Int>(path);

            return path;
        }

        /// <summary>
        /// Create a MoveCommand for the given destination.
        /// Does NOT execute the command — the caller (battle state machine) decides when
        /// to execute it via CommandHistory.
        /// </summary>
        /// <param name="unit">Unit to move.</param>
        /// <param name="destination">Target tile (must be in StoppableTiles).</param>
        /// <param name="rng">RNG for seed capture (rewind support).</param>
        /// <returns>MoveCommand ready for execution, or null if destination is invalid.</returns>
        public MoveCommand CreateMoveCommand(
            UnitInstance unit, Vector2Int destination, IGameRng rng)
        {
            if (_currentResult == null || !_currentResult.CanMoveTo(destination))
            {
                Debug.LogWarning($"[MovementController] Cannot create move to {destination} — not reachable.");
                return null;
            }

            var path = Pathfinder.ReconstructPath(_currentResult, unit.GridPosition, destination);
            if (path == null)
            {
                Debug.LogWarning($"[MovementController] Path reconstruction failed to {destination}.");
                return null;
            }

            int rngSeed = rng?.Seed ?? 0;
            return new MoveCommand(unit, destination, path, rngSeed);
        }

        /// <summary>
        /// Clear all movement overlays and reset internal state.
        /// </summary>
        /// <param name="grid">Grid view to clear overlays from.</param>
        public void ClearOverlays(IsometricGrid grid)
        {
            ClearPathPreview(grid);
            grid.ClearAllOverlays();
            _currentResult = null;
            _currentPathPreview = null;
        }

        /// <summary>
        /// Clear only the path preview, restoring those tiles to MoveRange state.
        /// Keeps the movement range overlay visible.
        /// </summary>
        private void ClearPathPreview(IsometricGrid grid)
        {
            if (_currentPathPreview == null || _currentPathPreview.Count == 0)
                return;

            // Restore path tiles to MoveRange (they were previously highlighted)
            grid.SetTileOverlay(_currentPathPreview, TileVisualState.MoveRange);
            _currentPathPreview = null;
        }

        /// <summary>
        /// Get the movement cost to reach a specific tile.
        /// Returns -1 if the tile is not reachable.
        /// </summary>
        /// <param name="pos">Grid position to query.</param>
        /// <returns>Movement cost, or -1 if unreachable.</returns>
        public int GetMoveCostTo(Vector2Int pos)
        {
            if (_currentResult == null) return -1;
            if (_currentResult.StoppableTiles.TryGetValue(pos, out var node))
                return node.CostSoFar;
            return -1;
        }
    }
}
