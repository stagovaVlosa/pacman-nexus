using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Test helper: plays a level by itself and logs PASS/FAIL to the Console.
    /// Goes to the nearest remaining Nexus Core, then to the Generator.
    /// With harmlessBugs = false it plays like a careful player: routes around cells near Bugs,
    /// runs away when a Bug is close, and fires the Energy Blast when a Bug is about to touch it.
    /// Start it in Play mode: new GameObject("Autopilot").AddComponent&lt;Nexus.Autopilot&gt;()
    /// </summary>
    public class Autopilot : MonoBehaviour
    {
        public bool harmlessBugs = false;
        public float timeLimit = 400f;
        [Tooltip("Cells closer than this to a Bug are avoided when planning a route")]
        public int dangerRadius = 2;
        [Tooltip("Energy Blast when a Bug is this close (world units)")]
        public float blastDistance = 1.6f;

        float startTime;
        int lastCores = -1, lastLives = -1, blasts;
        bool done;
        readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();
        readonly Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        void Start()
        {
            startTime = Time.time;
            if (harmlessBugs)
                foreach (var bug in BugAI.All.ToArray()) bug.gameObject.SetActive(false);
            Debug.Log("Autopilot: started" + (harmlessBugs ? " (Bugs off)" : " (Bugs ON)"));
        }

        void Update()
        {
            if (done) return;
            var level = LevelBuilder.Instance;
            var player = PlayerMovement.Instance;
            var gm = GameManager.Instance;
            if (level == null || player == null || gm == null) return;

            if (gm.CoresCollected != lastCores)
            {
                lastCores = gm.CoresCollected;
                Debug.Log($"Autopilot: cores {gm.CoresCollected}/{gm.CoresRequired} at {Time.time - startTime:0.0}s, score {gm.Score}, energy {gm.Energy:0}, lives {gm.Lives}");
            }
            if (gm.Lives != lastLives)
            {
                if (lastLives >= 0) Debug.Log($"Autopilot: lost a life at {Time.time - startTime:0.0}s (lives {gm.Lives})");
                lastLives = gm.Lives;
            }
            if (gm.LevelComplete) { Finish(true, "LEVEL COMPLETE"); return; }
            if (gm.GameOver) { Finish(false, "GAME OVER"); return; }
            if (Time.time - startTime > timeLimit) { Finish(false, "time limit reached"); return; }

            var from = player.TargetCell;

            // Danger: cells near active Bugs
            var danger = new HashSet<Vector2Int>();
            float nearest = float.MaxValue;
            foreach (var bug in BugAI.All)
            {
                if (!bug.isActiveAndEnabled || bug.IsFrozen) continue;
                nearest = Mathf.Min(nearest, Vector3.Distance(bug.transform.position, player.transform.position));
                var bc = bug.CurrentCell;
                for (int dy = -dangerRadius; dy <= dangerRadius; dy++)
                    for (int dx = -dangerRadius; dx <= dangerRadius; dx++)
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) <= dangerRadius) danger.Add(bc + new Vector2Int(dx, dy));
            }

            // Energy Blast when a Bug is about to touch Pac-Man
            if (nearest < blastDistance && player.TryGetComponent(out EnergyBlast blast) && blast.CanUse && blast.TryBlast())
            {
                blasts++;
                Debug.Log($"Autopilot: Energy Blast #{blasts} at {Time.time - startTime:0.0}s");
            }

            // Goals: remaining cores, or the Generator
            var goals = new HashSet<Vector2Int>();
            if (!gm.AllCoresCollected) { foreach (var c in level.CoreCells) if (level.HasCore(c)) goals.Add(c); }
            else goals.Add(level.GeneratorCell);
            if (goals.Contains(from)) { player.SetQueuedDirection(Vector2Int.zero); return; }

            // 1) safe route around danger, 2) any route; flee if the next step walks into a Bug
            if (!Route(level, from, goals, danger, out var step) && !Route(level, from, goals, null, out step))
            {
                Finish(false, "a goal cannot be reached");
                return;
            }
            if (danger.Contains(step) && !danger.Contains(from) && Flee(level, from, out var away)) step = away;
            player.SetQueuedDirection(step - from);
        }

        /// <summary>BFS to the nearest goal, never entering blocked cells (goals themselves are allowed).</summary>
        bool Route(LevelBuilder level, Vector2Int from, HashSet<Vector2Int> goals, HashSet<Vector2Int> blocked, out Vector2Int step)
        {
            step = from;
            queue.Clear();
            cameFrom.Clear();
            queue.Enqueue(from);
            cameFrom[from] = from;
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (goals.Contains(c) && c != from)
                {
                    var back = c;
                    while (cameFrom[back] != from) back = cameFrom[back];
                    step = back;
                    return true;
                }
                foreach (var d in GridPath.Dirs)
                {
                    var n = c + d;
                    if (!level.IsWalkable(n) || cameFrom.ContainsKey(n)) continue;
                    if (blocked != null && blocked.Contains(n) && !goals.Contains(n)) continue;
                    cameFrom[n] = c;
                    queue.Enqueue(n);
                }
            }
            return false;
        }

        /// <summary>Neighbour cell farthest from all active Bugs.</summary>
        bool Flee(LevelBuilder level, Vector2Int from, out Vector2Int best)
        {
            best = from;
            float bestScore = float.MinValue;
            foreach (var d in GridPath.Dirs)
            {
                var n = from + d;
                if (!level.IsWalkable(n)) continue;
                float score = float.MaxValue;
                foreach (var bug in BugAI.All)
                    if (bug.isActiveAndEnabled && !bug.IsFrozen) score = Mathf.Min(score, (bug.CurrentCell - n).sqrMagnitude);
                if (score > bestScore) { bestScore = score; best = n; }
            }
            return best != from;
        }

        void Finish(bool ok, string what)
        {
            done = true;
            var gm = GameManager.Instance;
            string msg = $"Autopilot: {(ok ? "PASS" : "FAIL")} - {what} after {Time.time - startTime:0.0}s " +
                         $"(cores {gm.CoresCollected}/{gm.CoresRequired}, score {gm.Score}, lives {gm.Lives}, blasts {blasts})";
            if (ok) Debug.Log(msg); else Debug.LogError(msg);
            if (harmlessBugs)
                foreach (var bug in Resources.FindObjectsOfTypeAll<BugAI>())
                    if (bug.gameObject.scene.IsValid()) bug.gameObject.SetActive(true);
        }
    }
}
