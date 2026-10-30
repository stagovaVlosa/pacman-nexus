using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Classic Pac-Man style grid movement:
    /// - moves from cell center to cell center, never through walls
    /// - remembers the next direction you press and turns as soon as it can
    /// - can reverse direction instantly
    /// Controls: WASD or arrow keys.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        public static PlayerMovement Instance { get; private set; }

        public float speed = 4.5f;
        public float floatHeight = 0.55f;
        public float turnSpeed = 720f;

        public Vector2Int CurrentCell => cell;
        public Vector2Int Direction => dir;
        public bool IsMoving => target != cell;

        LevelBuilder level;
        Vector2Int cell, target, dir, queued;
        Transform body;

        void Awake()
        {
            Instance = this;
        }

        public void Init(LevelBuilder builder, Vector2Int start)
        {
            level = builder;
            cell = target = start;
            dir = queued = Vector2Int.zero;
            transform.position = level.CellToWorld(start, floatHeight);
            body = transform.Find("Body");
        }

        void Update()
        {
            if (level == null) return;
            if (GameManager.Instance != null && GameManager.Instance.LevelComplete) return;

            ReadInput();

            // Instant reverse while between two cells
            if (target != cell && queued != Vector2Int.zero && queued == -dir)
            {
                (cell, target) = (target, cell);
                dir = queued;
            }

            if (target == cell) PickNextTarget();

            // Move, carrying leftover distance across cell centers so movement stays smooth
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
                    level.OnPlayerEnteredCell(cell);
                    PickNextTarget();
                }
            }

            AnimateBody();
        }

        void ReadInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            // Grid y grows downward (row 0 = top), so "up" is -1
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) queued = new Vector2Int(0, -1);
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) queued = new Vector2Int(0, 1);
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) queued = new Vector2Int(-1, 0);
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) queued = new Vector2Int(1, 0);
        }

        void PickNextTarget()
        {
            if (queued != Vector2Int.zero && level.IsWalkable(cell + queued)) dir = queued;
            if (dir != Vector2Int.zero && level.IsWalkable(cell + dir)) target = cell + dir;
            else target = cell; // blocked: stop and wait for a new direction
        }

        void AnimateBody()
        {
            if (body == null) return;
            // Float up and down
            body.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.06f, 0f);
            // Face movement direction
            if (dir != Vector2Int.zero)
            {
                var look = new Vector3(dir.x, 0f, -dir.y);
                var rot = Quaternion.LookRotation(look, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed * Time.deltaTime);
            }
        }
    }
}
