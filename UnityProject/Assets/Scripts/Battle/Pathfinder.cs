using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IsoRPG.Map;
using IsoRPG.Units;

namespace IsoRPG.Battle
{
    public struct PathNode
    {
        public Vector2Int Position;
        public int CostSoFar;
        public Vector2Int CameFrom;
    }

    /// <summary>
    /// Grid pathfinding with terrain cost, elevation/jump, and collision.
    /// Pure logic — no MonoBehaviour.
    /// </summary>
    public static class Pathfinder
    {
        private static readonly Vector2Int[] Neighbors = {
            new(0, 1), new(0, -1), new(1, 0), new(-1, 0)
        };

        /// <summary>
        /// Returns all reachable tiles within the unit's move range.
        /// </summary>
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
                CameFrom = start
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

                    if (!map.InBounds(next)) continue;

                    var tile = map.GetTile(next);
                    if (!tile.walkable) continue;

                    // Elevation check
                    var currentTile = map.GetTile(current);
                    if (Mathf.Abs(tile.elevation - currentTile.elevation) > jumpHeight)
                        continue;

                    // Collision — can't end on enemy, can pass through allies
                    var occupant = allUnits.FirstOrDefault(u => u.GridPosition == next && u.IsAlive);
                    if (occupant != null && occupant.Team != unit.Team)
                        continue;

                    int newCost = currentNode.CostSoFar + tile.moveCost;
                    if (newCost > moveRange) continue;

                    if (!visited.ContainsKey(next) || visited[next].CostSoFar > newCost)
                    {
                        var node = new PathNode
                        {
                            Position = next,
                            CostSoFar = newCost,
                            CameFrom = current
                        };
                        visited[next] = node;
                        frontier.Enqueue(next, newCost);
                    }
                }
            }

            // Remove tiles occupied by other units (can pass through allies but not stop on them)
            var blocked = new List<Vector2Int>();
            foreach (var pos in visited.Keys)
            {
                if (pos == start) continue;
                var occupant = allUnits.FirstOrDefault(u => u.GridPosition == pos && u.IsAlive && u != unit);
                if (occupant != null)
                    blocked.Add(pos);
            }
            foreach (var pos in blocked)
                visited.Remove(pos);

            return visited;
        }

        /// <summary>
        /// Reconstruct path from start to target using the visited nodes.
        /// </summary>
        public static List<Vector2Int> ReconstructPath(
            Dictionary<Vector2Int, PathNode> visited, Vector2Int start, Vector2Int target)
        {
            if (!visited.ContainsKey(target))
                return null;

            var path = new List<Vector2Int>();
            var current = target;

            while (current != start)
            {
                path.Add(current);
                current = visited[current].CameFrom;
            }

            path.Reverse();
            return path;
        }
    }
}
