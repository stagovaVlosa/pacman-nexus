using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Hunter (red bug, GDD 4.1): moves cell to cell like Pac-Man.
    /// - If the route to Pac-Man is at most detectRange cells, it follows the shortest path (BFS).
    /// - Otherwise it wanders: at each cell it picks a random open direction, avoiding turning back.
    /// No damage yet, only chasing.
    /// </summary>
    public class HunterAI : MonoBehaviour
    {
        public float speed = 3.8f;   // a bit slower than Pac-Man (4.5)
        public float floatHeight = 0.55f;
        public float turnSpeed = 540f;
        [Tooltip("Chase Pac-Man when the route to him through the maze is at most this many cells")]
        public int detectRange = 8;

        public Vector2Int CurrentCell => cell;
        public bool IsChasing { get; private set; }

        LevelBuilder level;
        Vector2Int cell, target, dir;
        Transform body;

        public void Init(LevelBuilder builder, Vector2Int start)
        {
            level = builder;
            cell = target = start;
            dir = Vector2Int.zero;
            transform.position = level.CellToWorld(start, floatHeight);
            body = transform.Find("Body");
        }

        void Update()
        {
            if (level == null) return;
            if (GameManager.Instance != null && GameManager.Instance.LevelComplete) return;

            if (target == cell) PickNextTarget();

            // Same movement as the player: carry leftover distance across cell centers
            float remaining = speed * Time.deltaTime;
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
            var player = PlayerMovement.Instance;
            if (player != null)
            {
                if (player.CurrentCell == cell)
                {
                    // Already on Pac-Man's cell: wait here until he moves
                    IsChasing = true;
                    target = cell;
                    return;
                }
                if (GridPath.TryFirstStep(level, cell, player.CurrentCell, detectRange, out var step, out _))
                {
                    IsChasing = true;
                    dir = step - cell;
                    target = step;
                    return;
                }
            }
            IsChasing = false;
            Wander();
        }

        void Wander()
        {
            // Collect open directions, skipping the way we came from
            var options = new Vector2Int[4];
            int count = 0;
            foreach (var d in GridPath.Dirs)
                if (d != -dir && level.IsWalkable(cell + d)) options[count++] = d;

            if (count == 0)
            {
                // Dead end: turn back (or stay if completely boxed in)
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
            body.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.5f + 1f) * 0.06f, 0f);
            if (dir != Vector2Int.zero)
            {
                var look = new Vector3(dir.x, 0f, -dir.y);
                var rot = Quaternion.LookRotation(look, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed * Time.deltaTime);
            }
        }
    }
}
