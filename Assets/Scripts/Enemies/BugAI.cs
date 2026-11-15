using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    public enum BugType { Chaser, Patroller, Blocker, Sentinel }

    /// <summary>
    /// A Bug (corrupted drone enemy). Moves cell to cell like Pac-Man, always slower than him (4.5).
    /// - Chaser    ('H'):  follows the shortest maze route to Pac-Man (BFS), wherever he is.
    /// - Patroller ('1'-'9', two cells with the same digit): walks back and forth between them.
    /// - Blocker   ('B'):  guards the area around its post; chases only when Pac-Man comes within
    ///                     alertRange cells, gives up when he leaves the territory and walks back.
    /// - Sentinel  ('S'):  GDD 4.2, purple guardian (Level 2+). Guards the Nexus Core nearest to its spawn by
    ///                     patrolling back and forth THROUGH it (between two cells ~4 steps away on opposite sides),
    ///                     so the core is free while it is away. Attacks when Pac-Man comes within alertRange
    ///                     and gives up once he leaves the territory around the core (never camps on the core).
    /// Touching a Bug costs a life (handled by PlayerLives).
    /// Devour Mode (PlayerPowers): every Bug turns blue (IsVulnerable), turns around, runs away from Pac-Man at about half
    /// speed and can be eaten; an eaten Bug disappears (IsEaten) and comes back at its spawn after respawnDelay.
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

        [Header("Chaser")]
        [Tooltip("Hunter chases only when the route to Pac-Man is at most this many cells")]
        public int chaseRange = 10;

        [Header("Blocker")]
        [Tooltip("Starts chasing when the route to Pac-Man is at most this many cells")]
        public int alertRange = 3;
        [Tooltip("Walks around within this many cells of its post while guarding")]
        public int guardRadius = 2;
        [Tooltip("Stops chasing when Pac-Man is more than this many cells from the post")]
        public int territoryRadius = 5;

        [Header("Devour Mode")]
        [Tooltip("Speed factor while blue (running away)")]
        public float frightenedSpeed = 0.55f;
        [Tooltip("Seconds an eaten Bug stays away before it comes back at its spawn")]
        public float respawnDelay = 5f;

        /// <summary>Blue and edible (Devour Mode).</summary>
        public bool IsVulnerable => vulnerableLeft > 0f && eatenLeft <= 0f;
        /// <summary>Eaten: invisible and harmless until it respawns.</summary>
        public bool IsEaten => eatenLeft > 0f;
        /// <summary>True during the last seconds of Devour Mode (the blue Bug blinks white).</summary>
        public bool VulnerableEnding => IsVulnerable && vulnerableLeft < vulnerableWarning;
        public static readonly Color FrightBlue = new Color(0.22f, 0.45f, 1f);

        /// <summary>All Bugs in the scene (PlayerLives checks them for collisions).</summary>
        public static readonly List<BugAI> All = new List<BugAI>();

        public Vector2Int CurrentCell => cell;
        public Vector2Int SpawnCell => spawn;
        public bool IsChasing { get; private set; }
        /// <summary>True while stunned by an Energy Blast (does not move).</summary>
        public bool IsFrozen => frozenLeft > 0f;
        public Color BodyColor => IsVulnerable
            ? (VulnerableEnding && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f ? Color.white : FrightBlue)
            : ColorFor(type);

        LevelBuilder level;
        Vector2Int cell, target, dir, spawn, routeEnd, patrolGoal, post, sentA, sentB, sentGoal;
        HashSet<Vector2Int> guardArea, territory;
        Transform body;
        float frozenLeft, vulnerableLeft, vulnerableWarning, eatenLeft;
        Renderer[] renderers;
        Light[] lights;
        Light glowLight;
        GroundRing groundRing;
        Transform scaredAura;
        Material scaredMat;
        bool showedScared;
        Vector3 knockFrom;
        float knockT = 1f;
        const float KnockDuration = 0.2f;

        int NoLimit => level.Width * level.Height;

        /// <summary>
        /// Bug colour, chosen per level so it stands out against that level's palette
        /// (user: the blue Patroller disappeared in the blue Neon District).
        /// Neon District (blue/pink night): red, lime, orange. Skyline Zone (orange sunset): crimson, cyan, magenta, violet.
        /// </summary>
        public static Color ColorFor(BugType t)
        {
            bool sky = CityVisuals.CurrentTheme == LevelTheme.SkylineZone;
            switch (t)
            {
                case BugType.Patroller: return sky ? Visuals.Hex("#1EF2FF") : Visuals.Hex("#9DFF3A");   // cyan / lime
                case BugType.Blocker: return sky ? Visuals.Hex("#FF2BD6") : Visuals.Hex("#FF7A1A");     // magenta / orange
                case BugType.Sentinel: return sky ? Visuals.Hex("#7B2CFF") : Visuals.Purple;             // GDD 4.2: purple
                default: return sky ? Visuals.Hex("#E8004F") : Visuals.Hex("#FF2A2A");                   // Hunter: crimson / red
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
                case BugType.Chaser: speed = 3.3f; break;
                case BugType.Patroller: speed = 3.2f; break;
                case BugType.Blocker: speed = 2.8f; chaseSpeed = 3.6f; break;
                case BugType.Sentinel: speed = 2.9f; chaseSpeed = 3.5f; break;
            }
            post = type == BugType.Sentinel ? NearestCore(spawn) : spawn;
            if (type == BugType.Sentinel) PickSentinelRoute();
            if (type == BugType.Blocker || type == BugType.Sentinel)
            {
                guardArea = new HashSet<Vector2Int>(GridPath.CellsWithin(level, post, guardRadius)) { post };
                territory = new HashSet<Vector2Int>(GridPath.CellsWithin(level, post, territoryRadius)) { post };
            }
            body = transform.Find("Body");
            renderers = GetComponentsInChildren<Renderer>(true);
            lights = GetComponentsInChildren<Light>(true);
            var g = transform.Find("Glow");
            if (g != null) glowLight = g.GetComponent<Light>();
            groundRing = GetComponentInChildren<GroundRing>(true);
            // Devour Mode look: a blue glowing shell around the drone (blinks white near the end)
            scaredMat = Visuals.Glow(FrightBlue, 2.2f, 1.6f);
            var aura = Visuals.Shape(PrimitiveType.Sphere, "ScaredAura", body != null ? body : transform, Vector3.zero, Vector3.one * 1.05f, scaredMat);
            aura.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            scaredAura = aura.transform;
            scaredAura.gameObject.SetActive(false);
            PlaceAt(start);
        }

        /// <summary>Two cells 4 steps from the guarded core, as far apart as possible (= on opposite sides).</summary>
        void PickSentinelRoute()
        {
            var dist = new Dictionary<Vector2Int, int> { [post] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(post);
            var ring = new List<Vector2Int>();
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (dist[c] == 4) { ring.Add(c); continue; }
                foreach (var d in GridPath.Dirs)
                {
                    var n = c + d;
                    if (level.IsWalkable(n) && !dist.ContainsKey(n)) { dist[n] = dist[c] + 1; queue.Enqueue(n); }
                }
            }
            sentA = spawn; sentB = post;
            int best = -1;
            for (int i = 0; i < ring.Count; i++)
                for (int j = i + 1; j < ring.Count; j++)
                {
                    int m = Mathf.Abs(ring[i].x - ring[j].x) + Mathf.Abs(ring[i].y - ring[j].y);
                    if (m > best) { best = m; sentA = ring[i]; sentB = ring[j]; }
                }
            sentGoal = sentA;
        }

        /// <summary>The Nexus Core closest to 'from' (Manhattan), or 'from' itself when the level has none.</summary>
        Vector2Int NearestCore(Vector2Int from)
        {
            var best = from;
            int bestD = int.MaxValue;
            foreach (var c in level.CoreCells)
            {
                int d = Mathf.Abs(c.x - from.x) + Mathf.Abs(c.y - from.y);
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
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
            sentGoal = sentA;
            vulnerableLeft = 0f;
            if (eatenLeft > 0f) { eatenLeft = 0f; SetVisible(true); }
            UpdateScaredLook();
            transform.position = level.CellToWorld(c, floatHeight);
        }

        /// <summary>Devour Mode starts: turn blue, turn around and run away for 'seconds'.</summary>
        public void MakeVulnerable(float seconds, float warning)
        {
            if (IsEaten) return;
            vulnerableLeft = seconds;
            vulnerableWarning = warning;
            IsChasing = false;
            if (target != cell) { (cell, target) = (target, cell); dir = -dir; }   // classic: reverse at once
        }

        public void EndVulnerable() { vulnerableLeft = 0f; UpdateScaredLook(); }

        /// <summary>Pac-Man ate this Bug: it vanishes (burst effect) and comes back at its spawn later.</summary>
        public void Eat()
        {
            vulnerableLeft = 0f;
            eatenLeft = respawnDelay;
            IsChasing = false;
            BugBurst.Spawn(transform.position, FrightBlue);
            UpdateScaredLook();
            SetVisible(false);
        }

        void UpdateScaredLook()
        {
            var c = BodyColor;
            if (glowLight != null) glowLight.color = c;
            bool scared = IsVulnerable;
            if (scared != showedScared)
            {
                showedScared = scared;
                if (scaredAura != null) scaredAura.gameObject.SetActive(scared);
                if (!scared && groundRing != null) groundRing.SetColor(ColorFor(type));
            }
            if (scared)
            {
                if (groundRing != null) groundRing.SetColor(c);
                if (scaredMat != null) scaredMat.SetColor("_Color", c);
                if (scaredAura != null) scaredAura.localScale = Vector3.one * (1.05f + Mathf.Sin(Time.time * 9f) * 0.06f);
            }
        }

        void SetVisible(bool on)
        {
            if (renderers != null) foreach (var r in renderers) if (r != null) r.enabled = on;
            if (lights != null) foreach (var l in lights) if (l != null) l.enabled = on;
        }

        /// <summary>
        /// Energy Blast hit: slide to the open cell (up to 'push' steps away through the maze) that is
        /// farthest from 'from', then stay frozen for 'freeze' seconds.
        /// </summary>
        public void Knockback(Vector2Int from, int push, float freeze)
        {
            if (IsEaten) return;
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
            // Nothing moves until the player presses a direction (start and after losing a life)
            if (PlayerMovement.Instance != null && !PlayerMovement.Instance.HasStarted) return;

            if (eatenLeft > 0f)
            {
                eatenLeft -= Time.deltaTime;
                if (eatenLeft <= 0f) PlaceAt(spawn);   // back home, normal colour again
                return;
            }
            if (vulnerableLeft > 0f) vulnerableLeft -= Time.deltaTime;
            UpdateScaredLook();

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
            float baseSpeed = (type == BugType.Blocker || type == BugType.Sentinel) && IsChasing ? chaseSpeed : speed;
            if (IsVulnerable) baseSpeed *= frightenedSpeed;
            float remaining = baseSpeed * Time.deltaTime;
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
            if (IsVulnerable) { FleeStep(); return; }
            switch (type)
            {
                case BugType.Chaser: ChaserStep(); break;
                case BugType.Patroller: PatrollerStep(); break;
                case BugType.Blocker: BlockerStep(); break;
                case BugType.Sentinel: SentinelStep(); break;
            }
        }

        // ---------- Behaviours ----------

        void ChaserStep()
        {
            var player = PlayerMovement.Instance;
            // Hunter (GDD 4.1): detects Pac-Man within a range and chases; outside it, it roams the streets
            if (player != null && TryChase(player.CurrentCell, chaseRange)) return;
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
            else if (!StepToward(post)) Wander(null); // walk back to the post
        }

        void SentinelStep()
        {
            var player = PlayerMovement.Instance;
            if (player != null && territory.Contains(player.CurrentCell))
            {
                // Short alert range, and it gives up sooner than the Blocker
                int range = IsChasing ? territoryRadius : alertRange;
                if (TryChase(player.CurrentCell, range)) return;
            }
            IsChasing = false;
            if (cell == sentGoal) sentGoal = sentGoal == sentA ? sentB : sentA;
            if (!StepToward(sentGoal)) Wander(null);
        }

        /// <summary>Devour Mode: at every cell take the open direction that leads furthest from Pac-Man (no turning back).</summary>
        void FleeStep()
        {
            IsChasing = false;
            var player = PlayerMovement.Instance;
            var from = player != null ? player.CurrentCell : cell;
            Vector2Int best = Vector2Int.zero;
            float bestScore = float.MinValue;
            foreach (var d in GridPath.Dirs)
            {
                var n = cell + d;
                if (!level.IsWalkable(n) || (d == -dir && dir != Vector2Int.zero)) continue;
                float score = Mathf.Abs(n.x - from.x) + Mathf.Abs(n.y - from.y) + Random.value * 0.9f;
                if (score > bestScore) { bestScore = score; best = d; }
            }
            if (best == Vector2Int.zero)
            {
                if (dir != Vector2Int.zero && level.IsWalkable(cell - dir)) best = -dir;   // dead end
                else { target = cell; return; }
            }
            dir = best;
            target = cell + dir;
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

    /// <summary>Short burst when a Bug is eaten: expanding ring + sparks in the Bug's colour.</summary>
    public class BugBurst : MonoBehaviour
    {
        const float Duration = 0.5f;
        float t;
        Material mat;
        Transform[] sparks;
        Vector3[] vel;

        public static void Spawn(Vector3 pos, Color color)
        {
            var go = new GameObject("BugBurst");
            go.transform.position = pos;
            var b = go.AddComponent<BugBurst>();
            b.mat = Visuals.Glow(Color.Lerp(color, Color.white, 0.4f), 3f, 0f);
            b.sparks = new Transform[10];
            b.vel = new Vector3[10];
            for (int i = 0; i < 10; i++)
            {
                var s = Visuals.Shape(PrimitiveType.Cube, "Spark", go.transform, Vector3.zero, Vector3.one * 0.09f, b.mat);
                s.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                b.sparks[i] = s.transform;
                float a = i * Mathf.PI * 2f / 10f;
                b.vel[i] = new Vector3(Mathf.Cos(a), 0.6f + Random.value, Mathf.Sin(a)) * 3.2f;
            }
        }

        void Update()
        {
            t += Time.deltaTime / Duration;
            if (t >= 1f) { Destroy(mat); Destroy(gameObject); return; }
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i].localPosition += vel[i] * Time.deltaTime;
                vel[i] += Vector3.down * 6f * Time.deltaTime;
                sparks[i].localScale = Vector3.one * 0.09f * (1f - t);
                sparks[i].Rotate(400f * Time.deltaTime, 300f * Time.deltaTime, 0f);
            }
            mat.SetFloat("_Intensity", 3f * (1f - t));
        }
    }
}
