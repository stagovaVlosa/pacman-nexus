using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    public enum BugType { Chaser, Patroller, Blocker }

    /// <summary>
    /// A Bug (corrupted drone enemy). Moves cell to cell like Pac-Man, always slower than him (4.5).
    /// - Chaser    ('H'):  follows the shortest maze route to Pac-Man (BFS), wherever he is.
    /// - Patroller ('1'-'9', two cells with the same digit): walks back and forth between them.
    /// - Blocker   ('B'):  guards the area around its post; chases only when Pac-Man comes within
    ///                     alertRange cells, gives up when he leaves the territory and walks back.
    /// Touching a Bug costs a life (handled by PlayerLives).
    /// </summary>
    public class BugAI : MonoBehaviour
    {
        public BugType type;

        [Header("Speed (Pac-Man = 4.5)")]
        public float speed = 3.6f;
        [Tooltip("Blocker only: speed while chasing")]
        public float chaseSpeed = 3.9f;
        public float floatHeight = 0.55f;
        public float turnSpeed = 540f;

        [Header("Blocker")]
        [Tooltip("Starts chasing when the route to Pac-Man is at most this many cells")]
        public int alertRange = 4;
        [Tooltip("Walks around within this many cells of its post while guarding")]
        public int guardRadius = 3;
        [Tooltip("Stops chasing when Pac-Man is more than this many cells from the post")]
        public int territoryRadius = 7;

        /// <summary>All Bugs in the scene (PlayerLives checks them for collisions).</summary>
        public static readonly List<BugAI> All = new List<BugAI>();

        public Vector2Int CurrentCell => cell;
        public Vector2Int SpawnCell => spawn;
        public bool IsChasing { get; private set; }
        /// <summary>True while stunned by an Energy Blast (does not move).</summary>
        public bool IsFrozen => frozenLeft > 0f;
        public Color BodyColor => ColorFor(type);

        LevelBuilder level;
        Vector2Int cell, target, dir, spawn, routeEnd, patrolGoal;
        HashSet<Vector2Int> guardArea, territory;
        Transform body;
        float frozenLeft;
        Vector3 knockFrom;
        float knockT = 1f;
        const float KnockDuration = 0.2f;

        int NoLimit => level.Width * level.Height;

        public static Color ColorFor(BugType t)
        {
            switch (t)
            {
                case BugType.Patroller: return Visuals.Purple;
                case BugType.Blocker: return Visuals.Hex("#FF2BD6");   // magenta
                default: return Visuals.Hex("#FF1030");                 // corrupted red
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>routeEnd is only used by the Patroller (the other end of its route).</summary>
        public void Init(LevelBuilder builder, BugType bugType, Vector2Int start, Vector2Int routeEnd)
        {
            level = builder;
            type = bugType;
            spawn = start;
            this.routeEnd = routeEnd;
            switch (type)
            {
                case BugType.Chaser: speed = 3.6f; break;
                case BugType.Patroller: speed = 3.2f; break;
                case BugType.Blocker: speed = 2.8f; chaseSpeed = 3.9f; break;
            }
            if (type == BugType.Blocker)
            {
                guardArea = new HashSet<Vector2Int>(GridPath.CellsWithin(level, spawn, guardRadius)) { spawn };
                territory = new HashSet<Vector2Int>(GridPath.CellsWithin(level, spawn, territoryRadius)) { spawn };
            }
            body = transform.Find("Body");
            PlaceAt(start);
        }

        /// <summary>Back to the spawn cell after Pac-Man loses a life.</summary>
        public void ResetToSpawn() => PlaceAt(spawn);

        /// <summary>Puts the Bug on a cell, standing still and not frozen.</summary>
        public void PlaceAt(Vector2Int c)
        {
            cell = target = c;
            dir = Vector2Int.zero;
            IsChasing = false;
            frozenLeft = 0f;
            knockT = 1f;
            patrolGoal = routeEnd;
            transform.position = level.CellToWorld(c, floatHeight);
        }

        /// <summary>
        /// Energy Blast hit: slide to the open cell (up to 'push' steps away through the maze) that is
        /// farthest from 'from', then stay frozen for 'freeze' seconds.
        /// </summary>
        public void Knockback(Vector2Int from, int push, float freeze)
        {
            var best = cell;
            float bestDist = (cell - from).sqrMagnitude;
            foreach (var c in GridPath.CellsWithin(level, cell, push))
            {
                float d = (c - from).sqrMagnitude;
                if (d > bestDist) { best = c; bestDist = d; }
            }
            knockFrom = transform.position;
            knockT = 0f;
            cell = target = best;
            dir = Vector2Int.zero;
            IsChasing = false;
            frozenLeft = freeze;
        }

        void Update()
        {
            if (level == null) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            if (knockT < 1f)
            {
                // Slide to the knockback cell
                knockT = Mathf.Min(1f, knockT + Time.deltaTime / KnockDuration);
                transform.position = Vector3.Lerp(knockFrom, level.CellToWorld(cell, floatHeight), 1f - (1f - knockT) * (1f - knockT));
            }
            if (frozenLeft > 0f)
            {
                frozenLeft -= Time.deltaTime;
                return;
            }

            if (target == cell) PickNextTarget();

            // Same movement as the player: carry leftover distance across cell centers
            float remaining = (type == BugType.Blocker && IsChasing ? chaseSpeed : speed) * Time.deltaTime;
            int safety = 4;
            while (remaining > 0f && target != cell && safety-- > 0)
            {
                Vector3 goal = level.CellToWorld(target, floatHeight);
                float dist = Vector3.Distance(transform.position, goal);
                if (dist > remaining)
                {
                    transform.position = Vector3.MoveTowards(transform.position, goal, remaining);
                    remaining = 0f;
                }
                else
                {
                    transform.position = goal;
                    remaining -= dist;
                    cell = target;
                    PickNextTarget();
                }
            }

            AnimateBody();
        }

        void PickNextTarget()
        {
            switch (type)
            {
                case BugType.Chaser: ChaserStep(); break;
                case BugType.Patroller: PatrollerStep(); break;
                case BugType.Blocker: BlockerStep(); break;
            }
        }

        // ---------- Behaviours ----------

        void ChaserStep()
        {
            var player = PlayerMovement.Instance;
            if (player != null && TryChase(player.CurrentCell, NoLimit)) return;
            IsChasing = false;
            Wander(null);
        }

        void PatrollerStep()
        {
            IsChasing = false;
            if (cell == patrolGoal) patrolGoal = patrolGoal == spawn ? routeEnd : spawn;
            // BFS also brings it back onto the route after an Energy Blast knockback
            if (!StepToward(patrolGoal)) Wander(null);
        }

        void BlockerStep()
        {
            var player = PlayerMovement.Instance;
            if (player != null && territory.Contains(player.CurrentCell))
            {
                // Start chasing when Pac-Man is close; keep chasing while he stays in the territory
                int range = IsChasing ? territoryRadius * 2 : alertRange;
                if (TryChase(player.CurrentCell, range)) return;
            }
            IsChasing = false;

            if (guardArea.Contains(cell)) Wander(guardArea);
            else if (!StepToward(spawn)) Wander(null); // walk back to the post
        }

        // ---------- Helpers ----------

        /// <summary>Moves one step along the shortest route to Pac-Man if it is at most maxDistance cells.</summary>
        bool TryChase(Vector2Int playerCell, int maxDistance)
        {
            if (playerCell == cell)
            {
                // Already on Pac-Man's cell: wait here until he moves
                IsChasing = true;
                target = cell;
                return true;
            }
            if (GridPath.TryFirstStep(level, cell, playerCell, maxDistance, out var step, out _))
            {
                IsChasing = true;
                dir = step - cell;
                target = step;
                return true;
            }
            return false;
        }

        bool StepToward(Vector2Int goal)
        {
            if (!GridPath.TryFirstStep(level, cell, goal, NoLimit, out var step, out _)) return false;
            dir = step - cell;
            target = step;
            return true;
        }

        /// <summary>Random open direction, avoiding turning back. 'area' (optional) limits where it may go.</summary>
        void Wander(HashSet<Vector2Int> area)
        {
            var options = new Vector2Int[4];
            int count = 0;
            foreach (var d in GridPath.Dirs)
            {
                var n = cell + d;
                if (d != -dir && level.IsWalkable(n) && (area == null || area.Contains(n))) options[count++] = d;
            }

            if (count == 0)
            {
                // Dead end (or edge of the area): turn back, or stay if completely boxed in
                if (dir != Vector2Int.zero && level.IsWalkable(cell - dir)) dir = -dir;
                else { target = cell; return; }
            }
            else
            {
                dir = options[Random.Range(0, count)];
            }
            target = cell + dir;
        }

        void AnimateBody()
        {
            if (body == null) return;
            // Float up and down (offset from Pac-Man so they don't bob in sync)
            body.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.5f + spawn.x) * 0.06f, 0f);
            if (dir != Vector2Int.zero)
            {
                var look = new Vector3(dir.x, 0f, -dir.y);
                var rot = Quaternion.LookRotation(look, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed * Time.deltaTime);
            }
        }
    }
}
