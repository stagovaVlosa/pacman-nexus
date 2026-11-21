using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Classic Pac-Man style grid movement:
    /// - moves from cell center to cell center, never through walls
    /// - remembers the next direction you press and turns at the next opening (turn buffer, see turnBufferSeconds);
    ///   the most recently pressed key always wins, even while another arrow is still held
    /// - can reverse direction instantly
    /// Controls: WASD or arrow keys.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        public static PlayerMovement Instance { get; private set; }

        public float speed = 4.5f;
        public float floatHeight = 0.55f;
        public float turnSpeed = 720f;
        [Tooltip("How long a pressed turn is remembered before the next opening (seconds)")]
        public float turnBufferSeconds = 2.5f;
        // Lane change (17 Nov): a sideways press on a 2-wide street steps into the other lane AND the turn stays
        // remembered for the next real junction. Always on (no inspector toggle, so an old scene value cannot switch it off).

        public Vector2Int CurrentCell => cell;
        public Vector2Int Direction => dir;
        public bool IsMoving => target != cell;
        /// <summary>The cell Pac-Man is moving into (equals CurrentCell when standing still).</summary>
        public Vector2Int TargetCell => target;
        /// <summary>False until the player presses a direction (at the start and after losing a life). Bugs wait for it.</summary>
        public bool HasStarted { get; private set; }

        LevelBuilder level;
        Vector2Int cell, target, dir, queued, start;
        Vector2Int laneReturn;   // direction to continue in after a one-cell lane-change step (zero = none)
        float queuedAt;          // Time.time when the queued turn was pressed
        bool queuedNoExpiry;     // set by SetQueuedDirection (Autopilot test): never expires

        static readonly Vector2Int Up = new Vector2Int(0, -1), Down = new Vector2Int(0, 1),
                                   Left = new Vector2Int(-1, 0), Right = new Vector2Int(1, 0);
        Transform body;

        void Awake()
        {
            Instance = this;
        }

        public void Init(LevelBuilder builder, Vector2Int start)
        {
            level = builder;
            this.start = start;
            cell = target = start;
            dir = queued = laneReturn = Vector2Int.zero;
            transform.position = level.CellToWorld(start, floatHeight);
            body = transform.Find("Body");
        }

        /// <summary>Same as pressing a direction key (used by the Autopilot test).</summary>
        public void SetQueuedDirection(Vector2Int d)
        {
            queued = d;
            queuedNoExpiry = true;
            if (d != Vector2Int.zero) HasStarted = true;
        }

        /// <summary>Back to the start cell ('P'), standing still (after losing a life).</summary>
        public void Respawn()
        {
            cell = target = start;
            dir = queued = laneReturn = Vector2Int.zero;
            queuedNoExpiry = false;
            HasStarted = false;
            transform.position = level.CellToWorld(start, floatHeight);
        }

        void Update()
        {
            if (level == null) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

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
            // Grid y grows downward (row 0 = top), so "up" is -1.
            // 1) A key pressed this frame always wins (so you can press the turn while still holding the old arrow).
            Vector2Int pressed = Vector2Int.zero;
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) pressed = Up;
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) pressed = Down;
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) pressed = Left;
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) pressed = Right;
            if (pressed != Vector2Int.zero)
            {
                queued = pressed;
                queuedAt = Time.time;
                queuedNoExpiry = false;
            }
            else if (queued != Vector2Int.zero && IsHeld(kb, queued))
            {
                // 2) Holding the queued key keeps the turn waiting as long as you hold it
                queuedAt = Time.time;
            }
            else if (queued == Vector2Int.zero)
            {
                // 3) Nothing queued yet: a held key counts too (e.g. held before the level started)
                var held = HeldDirection(kb);
                if (held != Vector2Int.zero) { queued = held; queuedAt = Time.time; }
            }

            // 4) Turn buffer: a turn that could not be taken within turnBufferSeconds is forgotten,
            //    so Pac-Man does not turn unexpectedly much later.
            if (!queuedNoExpiry && queued != Vector2Int.zero && queued != dir && dir != Vector2Int.zero
                && Time.time - queuedAt > turnBufferSeconds)
                queued = dir;

            if (queued != Vector2Int.zero) HasStarted = true;
        }

        static bool IsHeld(Keyboard kb, Vector2Int d)
        {
            if (d == Up) return kb.wKey.isPressed || kb.upArrowKey.isPressed;
            if (d == Down) return kb.sKey.isPressed || kb.downArrowKey.isPressed;
            if (d == Left) return kb.aKey.isPressed || kb.leftArrowKey.isPressed;
            if (d == Right) return kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            return false;
        }

        static Vector2Int HeldDirection(Keyboard kb)
        {
            if (IsHeld(kb, Up)) return Up;
            if (IsHeld(kb, Down)) return Down;
            if (IsHeld(kb, Left)) return Left;
            if (IsHeld(kb, Right)) return Right;
            return Vector2Int.zero;
        }

        /// <summary>The turn waiting for the next opening (zero or equal to Direction when none).</summary>
        public Vector2Int QueuedDirection => queued;

        void PickNextTarget()
        {
            // Just finished a lane-change step: continue the way he was going before.
            // The sideways turn stays queued, so it is taken at the next real junction (within the turn buffer).
            if (laneReturn != Vector2Int.zero)
            {
                if (level.IsWalkable(cell + laneReturn) && laneReturn != -dir) dir = laneReturn;
                laneReturn = Vector2Int.zero;
            }

            if (queued != Vector2Int.zero && queued != dir && level.IsWalkable(cell + queued) && ShouldTakeTurn())
            {
                var previous = dir;
                bool laneChange = !queuedNoExpiry && IsLaneChange();
                dir = queued;
                // Lane change on a 2-wide street: step across one cell, then keep going the way he was going
                if (laneChange) laneReturn = previous;
            }
            if (dir != Vector2Int.zero && level.IsWalkable(cell + dir)) target = cell + dir;
            else target = cell; // blocked: stop and wait for a new direction
        }

        /// <summary>
        /// Smart turn: while Pac-Man can keep going straight, a pre-pressed turn is only taken into a real
        /// street (at least 2 open cells in the new direction), never into a 1-cell nook beside the road.
        /// With a wall ahead (or standing still) any opening is taken.
        /// A step sideways into the other lane of a 2-wide street is allowed too (see IsLaneChange).
        /// </summary>
        bool ShouldTakeTurn()
        {
            if (queuedNoExpiry) return true;                                    // Autopilot: exact control
            if (dir == Vector2Int.zero || queued == dir || queued == -dir) return true;
            if (!level.IsWalkable(cell + dir)) return true;                     // wall ahead: take any opening
            return level.IsWalkable(cell + queued * 2) || IsLaneChange();
        }

        /// <summary>
        /// True when the turn only crosses to the parallel lane of a wide street: the cell beside Pac-Man is open,
        /// the street does not continue further sideways, but that lane continues in his current direction.
        /// (A 1-cell nook beside the road does not continue forward, so it is still ignored.)
        /// </summary>
        bool IsLaneChange()
        {
            // 16 Nov this was switched off because the hop made Pac-Man forget the turn. Since 17 Nov the turn stays
            // queued after the hop (see laneReturn in PickNextTarget), so both work: lane change + turn at the junction.
            if (dir == Vector2Int.zero || queued == dir || queued == -dir || queued == Vector2Int.zero) return false;
            var side = cell + queued;
            return level.IsWalkable(side) && !level.IsWalkable(side + queued) && level.IsWalkable(side + dir);
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
