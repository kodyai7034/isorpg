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
    /// - Cannot traverse elevation differences greater than unit's Jump stat
    /// - Forest costs 2 movement points, all other walkable terrain costs 1
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
        /// <returns>Dictionary of reachable positions to their path nodes. Only includes tiles the unit can stop on.</returns>
        public static Dictionary<Vector2Int, PathNode> GetReachableTiles(
            BattleMapData map, UnitInstance unit, List<UnitInstance> allUnits)
        {
            var start = unit.GridPosition;
            var moveRange = unit.Stats.Move;
            var jumpHeight = unit.Stats.Jump;

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
                    if (!map.TryGetTile(current, out var currentTile))
                        continue;

                    if (Mathf.Abs(nextTile.Elevation - currentTile.Elevation) > jumpHeight)
                        continue;

                    // Enemy collision — cannot enter enemy tiles at all
                    var occupant = allUnits.FirstOrDefault(u =>
                        u.GridPosition == next && u.IsAlive && u != unit);
                    if (occupant != null && occupant.Team != unit.Team)
                        continue;

                    int newCost = currentNode.CostSoFar + nextTile.MoveCost;
                    if (newCost > moveRange)
                        continue;

                    if (!visited.ContainsKey(next) || visited[next].CostSoFar > newCost)
                    {
                        // Allied occupant: can traverse but not stop
                        bool canStop = occupant == null;

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

            // Return only tiles the unit can actually stop on
            var stoppable = new Dictionary<Vector2Int, PathNode>();
            foreach (var kvp in visited)
            {
                if (kvp.Value.CanStop)
                    stoppable[kvp.Key] = kvp.Value;
            }

            return stoppable;
        }

        /// <summary>
        /// Reconstruct the path from start to target using visited node data.
        /// Uses the full visited set (including non-stoppable tiles) for path reconstruction.
        /// </summary>
        /// <param name="visited">Visited nodes from GetReachableTiles (stoppable only).</param>
        /// <param name="start">Starting grid position.</param>
        /// <param name="target">Target grid position.</param>
        /// <returns>Ordered list of positions from start (exclusive) to target (inclusive), or null if unreachable.</returns>
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
                Debug.LogError("[Pathfinder] Path reconstruction safety limit reached — possible cycle.");
                return null;
            }

            path.Reverse();
            return path;
        }
    }
}
