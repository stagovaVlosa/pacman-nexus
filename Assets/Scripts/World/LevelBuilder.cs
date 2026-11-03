using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nexus
{
    /// <summary>
    /// Reads a text layout and builds the maze.
    ///   #  wall           .  energy orb       C  Nexus Core
    ///   P  player start   G  generator        -  enemy door     (space) empty floor
    ///   Bugs (an orb is placed under B and the digits too):
    ///   H  Chaser spawn   B  Blocker post     1-9  Patroller route: two cells with the same digit
    /// Row 0 of the text is the far (top) side of the maze.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        public static LevelBuilder Instance { get; private set; }

        [Header("Layout")]
        public TextAsset layout;
        public float cellSize = 1f;
        public float wallHeight = 1.2f;
        [Tooltip("How many rows of background buildings surround the maze")]
        public int skylineBand = 3;

        [Header("Optional prefabs (leave empty = placeholder shapes)")]
        public GameObject wallPrefab;
        public GameObject orbPrefab;
        public GameObject corePrefab;
        public GameObject generatorPrefab;
        public GameObject playerPrefab;
        [FormerlySerializedAs("hunterPrefab")] public GameObject bugPrefab;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CoreCount { get; private set; }
        public Vector2Int PlayerStart { get; private set; }
        public Vector2Int GeneratorCell { get; private set; }
        /// <summary>Every Bug of the level, Chasers first.</summary>
        public readonly List<BugSpawn> BugSpawns = new List<BugSpawn>();

        public struct BugSpawn
        {
            public BugType type;
            public Vector2Int cell;
            public Vector2Int routeEnd;   // Patroller only
        }

        /// <summary>Cells that started with a Nexus Core (use HasCore to see if it is still there).</summary>
        public readonly List<Vector2Int> CoreCells = new List<Vector2Int>();

        bool[,] walls;
        readonly Dictionary<Vector2Int, Collectible> collectibles = new Dictionary<Vector2Int, Collectible>();

        void Awake()
        {
            Instance = this;
            if (layout == null)
            {
                Debug.LogError("LevelBuilder: no layout TextAsset assigned.");
                return;
            }
            Build(layout.text);
        }

        // ---------- Grid helpers (used by player, enemies, minimap) ----------

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

        public bool IsWalkable(Vector2Int c) => InBounds(c) && !walls[c.x, c.y];

        public Vector3 CellToWorld(Vector2Int c, float y = 0f) =>
            new Vector3(c.x * cellSize, y, (Height - 1 - c.y) * cellSize);

        public Vector2Int WorldToCell(Vector3 p) =>
            new Vector2Int(Mathf.RoundToInt(p.x / cellSize), Height - 1 - Mathf.RoundToInt(p.z / cellSize));

        /// <summary>True while the Nexus Core on this cell has not been collected yet.</summary>
        public bool HasCore(Vector2Int c) =>
            collectibles.TryGetValue(c, out var item) && item != null && item.kind == Collectible.Kind.NexusCore;

        /// <summary>Called by the player every time it reaches the center of a cell.</summary>
        public void OnPlayerEnteredCell(Vector2Int cell)
        {
            if (collectibles.TryGetValue(cell, out var item))
            {
                collectibles.Remove(cell);
                item.Collect();
            }
        }

        // ---------- Building ----------

        void Build(string text)
        {
            var rows = new List<string>();
            foreach (var line in text.Replace("\r", "").Split('\n'))
                if (line.Length > 0) rows.Add(line);

            Height = rows.Count;
            Width = 0;
            foreach (var r in rows) Width = Mathf.Max(Width, r.Length);
            walls = new bool[Width, Height];

            var wallsRoot = new GameObject("Walls").transform;
            var itemsRoot = new GameObject("Collectibles").transform;
            wallsRoot.SetParent(transform, false);
            CityVisuals.BeginCity(transform);
            itemsRoot.SetParent(transform, false);

            // Pass 1: read the grid (walls must be known before buildings look at their neighbours)
            var walkableItems = new List<(Vector2Int cell, char ch)>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char ch = x < rows[y].Length ? rows[y][x] : '#';
                    var cell = new Vector2Int(x, y);
                    if (ch == '#') walls[x, y] = true;
                    else walkableItems.Add((cell, ch));
                }
            }

            // Pass 2: build
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (walls[x, y]) BuildWall(new Vector2Int(x, y), CellToWorld(new Vector2Int(x, y)), wallsRoot);

            var blockers = new List<Vector2Int>();
            var routes = new SortedDictionary<char, List<Vector2Int>>();
            foreach (var (cell, ch) in walkableItems)
            {
                if (ch >= '1' && ch <= '9')
                {
                    if (!routes.TryGetValue(ch, out var route)) routes[ch] = route = new List<Vector2Int>();
                    route.Add(cell);
                    AddCollectible(cell, Collectible.Kind.Orb, itemsRoot);
                    continue;
                }
                switch (ch)
                {
                    case '.':
                        AddCollectible(cell, Collectible.Kind.Orb, itemsRoot);
                        break;
                    case 'C':
                        AddCollectible(cell, Collectible.Kind.NexusCore, itemsRoot);
                        CoreCount++;
                        CoreCells.Add(cell);
                        break;
                    case 'P':
                        PlayerStart = cell;
                        break;
                    case 'G':
                        GeneratorCell = cell;
                        break;
                    case 'H':
                        BugSpawns.Add(new BugSpawn { type = BugType.Chaser, cell = cell });
                        break;
                    case 'B':
                        blockers.Add(cell);
                        AddCollectible(cell, Collectible.Kind.Orb, itemsRoot);
                        break;
                }
            }
            foreach (var pair in routes)
            {
                if (pair.Value.Count != 2)
                {
                    Debug.LogWarning($"LevelBuilder: Patroller route '{pair.Key}' needs exactly 2 cells (has {pair.Value.Count}).");
                    continue;
                }
                BugSpawns.Add(new BugSpawn { type = BugType.Patroller, cell = pair.Value[0], routeEnd = pair.Value[1] });
            }
            foreach (var b in blockers)
                BugSpawns.Add(new BugSpawn { type = BugType.Blocker, cell = b });

            // Buildings between the camera and Pac-Man turn semi-transparent
            wallsRoot.gameObject.AddComponent<BuildingFader>();

            BuildFloor();
            BuildGenerator();
            BuildPlayer();
            BuildBugs();
        }

        void BuildWall(Vector2Int cell, Vector3 pos, Transform parent)
        {
            if (wallPrefab != null)
            {
                Instantiate(wallPrefab, pos, Quaternion.identity, parent);
                return;
            }
            CityVisuals.BuildBuilding(this, cell, parent, false);
        }

        void BuildFloor()
        {
            var city = new GameObject("City").transform;
            city.SetParent(transform, false);
            CityVisuals.BuildGround(this, city, skylineBand);
            CityVisuals.BuildStreetLines(this, city);
            CityVisuals.BuildSkyline(this, city, skylineBand);
            CityVisuals.ApplyAtmosphere();
            CityVisuals.AddReflectionProbe(this, city, skylineBand);
        }

        void AddCollectible(Vector2Int cell, Collectible.Kind kind, Transform parent)
        {
            GameObject go;
            var prefab = kind == Collectible.Kind.Orb ? orbPrefab : corePrefab;
            if (prefab != null)
            {
                go = Instantiate(prefab, CellToWorld(cell), Quaternion.identity, parent);
            }
            else
            {
                go = new GameObject(kind == Collectible.Kind.Orb ? "EnergyOrb" : "NexusCore");
                go.transform.SetParent(parent, false);
                go.transform.position = CellToWorld(cell);
                if (kind == Collectible.Kind.Orb)
                {
                    // Shared material (one for all orbs), bright enough for bloom to make it glow
                    Visuals.Shape(PrimitiveType.Sphere, "Visual", go.transform, new Vector3(0, 0.45f, 0),
                        Vector3.one * 0.18f, Visuals.GetMaterial(Visuals.Cyan, Visuals.Cyan, 2.8f));
                }
                else
                {
                    Visuals.Shape(PrimitiveType.Cylinder, "Platform", go.transform, new Vector3(0, 0.03f, 0),
                        new Vector3(0.7f, 0.03f, 0.7f), Visuals.GetMaterial(Visuals.DarkBlue, Visuals.Cyan, 1f));
                    // Own materials per core so each one can pulse (Collectible animates them)
                    var visual = Visuals.Shape(PrimitiveType.Sphere, "Visual", go.transform, new Vector3(0, 0.6f, 0),
                        Vector3.one * 0.45f, Visuals.Lit(Visuals.NeonYellow, 0f, 0.5f, Visuals.NeonYellow, 4f));
                    // Soft yellow aura around the core (rim glow, follows the core's bob)
                    var halo = Visuals.Shape(PrimitiveType.Sphere, "Halo", visual.transform, Vector3.zero,
                        Vector3.one * 1.7f, Visuals.Glow(Visuals.NeonYellow, 1.2f, 2f));
                    halo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var light = new GameObject("Glow").AddComponent<Light>();
                    light.transform.SetParent(go.transform, false);
                    light.transform.localPosition = new Vector3(0, 0.8f, 0);
                    light.type = LightType.Point;
                    light.color = Visuals.NeonYellow;
                    light.range = 3f;
                    light.intensity = 2.5f;
                    light.shadows = LightShadows.None;
                }
            }
            if (!go.TryGetComponent(out Collectible c)) c = go.AddComponent<Collectible>();
            c.kind = kind;
            collectibles[cell] = c;
        }

        void BuildGenerator()
        {
            GameObject go;
            if (generatorPrefab != null)
            {
                go = Instantiate(generatorPrefab, CellToWorld(GeneratorCell), Quaternion.identity, transform);
            }
            else
            {
                go = new GameObject("Generator");
                go.transform.SetParent(transform, false);
                go.transform.position = CellToWorld(GeneratorCell);
                Visuals.Shape(PrimitiveType.Cylinder, "Base", go.transform, new Vector3(0, 0.05f, 0),
                    new Vector3(0.95f, 0.05f, 0.95f), Visuals.GetMaterial(Visuals.DarkBlue, Color.black, 0f));
                Visuals.Shape(PrimitiveType.Cylinder, "Core", go.transform, new Vector3(0, 0.6f, 0),
                    new Vector3(0.45f, 0.5f, 0.45f), Visuals.GetMaterial(Visuals.CorruptedRed, Visuals.CorruptedRed, 2f));
                // Energy beam going up into the sky, visible from far away
                Visuals.Shape(PrimitiveType.Cylinder, "Beam", go.transform, new Vector3(0, 3.6f, 0),
                    new Vector3(0.12f, 2.5f, 0.12f), Visuals.GetMaterial(Visuals.CorruptedRed, Visuals.CorruptedRed, 2.5f));
            }
            if (!go.TryGetComponent(out Generator gen)) gen = go.AddComponent<Generator>();
            gen.cell = GeneratorCell;
        }

        void BuildPlayer()
        {
            GameObject go;
            if (playerPrefab != null)
            {
                go = Instantiate(playerPrefab);
            }
            else
            {
                go = new GameObject("PacMan");
                var body = new GameObject("Body").transform;
                body.SetParent(go.transform, false);
                PacManModel.Create(body, 0.44f);
                var light = new GameObject("Glow").AddComponent<Light>();
                light.transform.SetParent(go.transform, false);
                light.type = LightType.Point;
                light.color = Visuals.NeonYellow;
                light.range = 3.5f;
                light.intensity = 2.5f;
            }
            // Yellow ring on the street so Pac-Man is always easy to find
            GroundRing.Create(go.transform, Visuals.NeonYellow, 0.55f, 2.2f);
            if (!go.TryGetComponent(out PlayerMovement mover)) mover = go.AddComponent<PlayerMovement>();
            mover.Init(this, PlayerStart);
            if (!go.TryGetComponent(out PlayerLives _)) go.AddComponent<PlayerLives>();
            if (!go.TryGetComponent(out EnergyBlast _)) go.AddComponent<EnergyBlast>();

            // BYTE, the hologram companion (its own object, so it can lag behind smoothly)
            ByteCompanion.Create(go.transform);
        }

        void BuildBugs()
        {
            var root = new GameObject("Bugs").transform;
            root.SetParent(transform, false);
            foreach (var spawn in BugSpawns)
            {
                var color = BugAI.ColorFor(spawn.type);
                GameObject go;
                if (bugPrefab != null)
                {
                    go = Instantiate(bugPrefab, root);
                    go.name = spawn.type + "Bug";
                }
                else
                {
                    go = new GameObject(spawn.type + "Bug");
                    go.transform.SetParent(root, false);
                    var body = new GameObject("Body").transform;
                    body.SetParent(go.transform, false);
                    BugModel.Create(body, 0.38f, color);
                    var light = new GameObject("Glow").AddComponent<Light>();
                    light.transform.SetParent(go.transform, false);
                    light.type = LightType.Point;
                    light.color = color;
                    light.range = 4f;
                    light.intensity = 4f;
                }
                // Colored ring on the street, visible even behind buildings
                GroundRing.Create(go.transform, color, 0.5f, 2.5f);
                if (!go.TryGetComponent(out BugAI bug)) bug = go.AddComponent<BugAI>();
                bug.Init(this, spawn.type, spawn.cell, spawn.routeEnd);
            }
        }
    }
}
