using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Breadth-first search on the LevelBuilder grid.
    /// Shared by all enemies that need the shortest route through the maze.
    /// </summary>
    public static class GridPath
    {
        // Grid directions: up (row - 1), right, down, left
        public static readonly Vector2Int[] Dirs =
            { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

        static readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();
        static readonly Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        static readonly Dictionary<Vector2Int, int> depth = new Dictionary<Vector2Int, int>();

        /// <summary>All walkable cells reachable from 'from' in at most 'steps' moves (not including 'from').</summary>
        public static List<Vector2Int> CellsWithin(LevelBuilder level, Vector2Int from, int steps)
        {
            var result = new List<Vector2Int>();
            var seen = new Dictionary<Vector2Int, int> { [from] = 0 };
            var q = new Queue<Vector2Int>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (seen[c] >= steps) continue;
                foreach (var dir in Dirs)
                {
                    var n = c + dir;
                    if (!level.IsWalkable(n) || seen.ContainsKey(n)) continue;
                    seen[n] = seen[c] + 1;
                    result.Add(n);
                    q.Enqueue(n);
                }
            }
            return result;
        }

        /// <summary>
        /// Finds the shortest path from 'from' to 'to' (at most maxDistance cells long).
        /// Returns false if there is no such path or from == to.
        /// 'step' is the first cell to move into, 'distance' the path length in cells.
        /// </summary>
        public static bool TryFirstStep(LevelBuilder level, Vector2Int from, Vector2Int to, int maxDistance,
                                        out Vector2Int step, out int distance)
        {
            step = from;
            distance = 0;
            if (from == to || !level.IsWalkable(to)) return false;

            queue.Clear();
            cameFrom.Clear();
            depth.Clear();
            depth[from] = 0;
            queue.Enqueue(from);
            cameFrom[from] = from;

            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                int d = depth[c];
                if (d >= maxDistance) continue;

                foreach (var dir in Dirs)
                {
                    var n = c + dir;
                    if (!level.IsWalkable(n) || cameFrom.ContainsKey(n)) continue;
                    cameFrom[n] = c;
                    depth[n] = d + 1;
                    if (n == to)
                    {
                        distance = d + 1;
                        // Walk back until the cell right after 'from'
                        var back = n;
                        while (cameFrom[back] != from) back = cameFrom[back];
                        step = back;
                        return true;
                    }
                    queue.Enqueue(n);
                }
            }
            return false;
        }
    }
}
