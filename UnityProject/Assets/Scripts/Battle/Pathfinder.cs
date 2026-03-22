using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Battle
{
    /// <summary>
    /// Intermediate node for pathfinding. Tracks cost and path reconstruction.
    /// </summary>
    public struct PathNode
    {
        /// <summary>Grid position of this node.</summary>
        public Vector2Int Position;
        /// <summary>Total movement cost from start to this node.</summary>
        public int CostSoFar;
        /// <summary>Grid position of the previous node in the path.</summary>
        public Vector2Int CameFrom;
        /// <summary>Whether a unit can stop on this tile (false if occupied by ally).</summary>
        public bool CanStop;
    }

    /// <summary>
    /// Grid pathfinding with terrain cost, elevation/jump constraints, and unit collision.
    /// Pure logic — no MonoBehaviour dependency.
    ///
    /// Movement rules:
    /// - Cannot enter unwalkable tiles (water, lava)
    /// - Cannot enter tiles occupied by enemy units
    /// - Can pass through allied units but cannot stop on them
    /// - Cannot traverse elevation differences greater than JumpHeight (unless IgnoreHeight)
    /// - Forest costs 2 movement points (unless CanFly)
    /// </summary>
    public static class Pathfinder
    {
        private static readonly Vector2Int[] Neighbors =
        {
            new(0, 1), new(0, -1), new(1, 0), new(-1, 0)
        };

        /// <summary>
        /// Calculate all tiles reachable by a unit within their movement range.
        /// </summary>
        /// <param name="map">The battle map data.</param>
        /// <param name="unit">The unit to calculate movement for.</param>
        /// <param name="allUnits">All units on the battlefield (for collision).</param>
        /// <returns>PathfindingResult with stoppable tiles and full visited set.</returns>
        public static PathfindingResult GetReachableTiles(
            BattleMapData map, UnitInstance unit, List<UnitInstance> allUnits)
        {
            return GetReachableTiles(map, unit.GridPosition, MovementParams.FromUnit(unit),
                unit.Team, allUnits);
        }

        /// <summary>
        /// Calculate all tiles reachable with explicit movement parameters.
        /// Use this overload to apply movement ability modifiers (Move+1, Ignore Height, etc.).
        /// </summary>
        /// <param name="map">The battle map data.</param>
        /// <param name="start">Starting grid position.</param>
        /// <param name="moveParams">Movement parameters (potentially modified by abilities).</param>
        /// <param name="unitTeam">Team of the moving unit (for collision rules).</param>
        /// <param name="allUnits">All units on the battlefield.</param>
        /// <returns>PathfindingResult with stoppable tiles and full visited set.</returns>
        public static PathfindingResult GetReachableTiles(
            BattleMapData map, Vector2Int start, MovementParams moveParams,
            int unitTeam, List<UnitInstance> allUnits)
        {
            // Pre-build position sets for O(1) collision checks
            var enemyPositions = new HashSet<Vector2Int>();
            var allyPositions = new HashSet<Vector2Int>();
            foreach (var u in allUnits)
            {
                if (!u.IsAlive || u.GridPosition == start) continue;
                if (u.Team != unitTeam)
                    enemyPositions.Add(u.GridPosition);
                else
                    allyPositions.Add(u.GridPosition);
            }

            var visited = new Dictionary<Vector2Int, PathNode>();
            var frontier = new PriorityQueue<Vector2Int, int>();

            var startNode = new PathNode
            {
                Position = start,
                CostSoFar = 0,
                CameFrom = start,
                CanStop = true
            };
            visited[start] = startNode;
            frontier.Enqueue(start, 0);

            // Teleport: all tiles in Manhattan range are reachable, ignore obstacles
            if (moveParams.CanTeleport)
            {
                return GetTeleportReachable(map, start, moveParams.MoveRange,
                    enemyPositions, allyPositions);
            }

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                var currentNode = visited[current];

                foreach (var offset in Neighbors)
                {
                    var next = current + offset;

                    if (!map.TryGetTile(next, out var nextTile))
                        continue;

                    if (!nextTile.Walkable)
                        continue;

                    // Elevation check
                    if (!moveParams.IgnoreHeight)
                    {
                        if (!map.TryGetTile(current, out var currentTile))
                            continue;

                        if (Mathf.Abs(nextTile.Elevation - currentTile.Elevation) > moveParams.JumpHeight)
                            continue;
                    }

                    // Enemy collision — cannot enter enemy tiles
                    if (enemyPositions.Contains(next))
                        continue;

                    // Movement cost
                    int tileCost = moveParams.CanFly ? 1 : nextTile.MoveCost;
                    int newCost = currentNode.CostSoFar + tileCost;
                    if (newCost > moveParams.MoveRange)
                        continue;

                    if (!visited.ContainsKey(next) || visited[next].CostSoFar > newCost)
                    {
                        bool canStop = !allyPositions.Contains(next);

                        var node = new PathNode
                        {
                            Position = next,
                            CostSoFar = newCost,
                            CameFrom = current,
                            CanStop = canStop
                        };
                        visited[next] = node;
                        frontier.Enqueue(next, newCost);
                    }
                }
            }

            return new PathfindingResult(visited);
        }

        /// <summary>
        /// Reconstruct the path from start to target.
        /// Uses the full visited set (including non-stoppable ally tiles) for reconstruction.
        /// </summary>
        public static List<Vector2Int> ReconstructPath(
            PathfindingResult result, Vector2Int start, Vector2Int target)
        {
            return ReconstructPath(result.AllVisited, start, target);
        }

        /// <summary>
        /// Reconstruct path from a raw visited dictionary.
        /// </summary>
        public static List<Vector2Int> ReconstructPath(
            Dictionary<Vector2Int, PathNode> visited, Vector2Int start, Vector2Int target)
        {
            if (!visited.ContainsKey(target))
                return null;

            var path = new List<Vector2Int>();
            var current = target;

            int safety = 1000;
            while (current != start && safety-- > 0)
            {
                path.Add(current);
                current = visited[current].CameFrom;
            }

            if (safety <= 0)
            {
                Debug.LogError("[Pathfinder] Path reconstruction safety limit reached.");
                return null;
            }

            path.Reverse();
            return path;
        }

        private static PathfindingResult GetTeleportReachable(
            BattleMapData map, Vector2Int start, int range,
            HashSet<Vector2Int> enemyPositions, HashSet<Vector2Int> allyPositions)
        {
            var visited = new Dictionary<Vector2Int, PathNode>();

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (IsoMath.ManhattanDistance(start, pos) > range)
                        continue;

                    if (!map.TryGetTile(pos, out var tile) || !tile.Walkable)
                        continue;

                    if (enemyPositions.Contains(pos))
                        continue;

                    bool canStop = !allyPositions.Contains(pos);

                    visited[pos] = new PathNode
                    {
                        Position = pos,
                        CostSoFar = IsoMath.ManhattanDistance(start, pos),
                        CameFrom = start, // teleport: direct from start
                        CanStop = canStop
                    };
                }
            }

            // Ensure start is in visited
            visited[start] = new PathNode
            {
                Position = start,
                CostSoFar = 0,
                CameFrom = start,
                CanStop = true
            };

            return new PathfindingResult(visited);
        }
    }

    /// <summary>
    /// Result of a pathfinding query. Contains both the full visited set
    /// (needed for path reconstruction through ally tiles) and the stoppable
    /// subset (tiles the unit can actually end their movement on).
    /// </summary>
    public class PathfindingResult
    {
        /// <summary>All visited tiles including non-stoppable ally-occupied tiles.</summary>
        public Dictionary<Vector2Int, PathNode> AllVisited { get; }

        /// <summary>Only tiles the unit can stop on (excludes ally-occupied tiles).</summary>
        public Dictionary<Vector2Int, PathNode> StoppableTiles { get; }

        public PathfindingResult(Dictionary<Vector2Int, PathNode> allVisited)
        {
            AllVisited = allVisited;
            StoppableTiles = new Dictionary<Vector2Int, PathNode>();
            foreach (var kvp in allVisited)
            {
                if (kvp.Value.CanStop)
                    StoppableTiles[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>Check if a tile is reachable and stoppable.</summary>
        public bool CanMoveTo(Vector2Int pos) => StoppableTiles.ContainsKey(pos);

        /// <summary>Check if a tile was visited during pathfinding (even if not stoppable).</summary>
        public bool WasVisited(Vector2Int pos) => AllVisited.ContainsKey(pos);
    }
}
