using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Round minimap in the top-right corner, built from the LevelBuilder grid.
    /// revealAll (default, user decision 10 Nov): whole city, all cores and all Bugs visible from the start.
    /// With revealAll off it works as fog of war:
    /// - Every cell starts hidden. Cells within revealRadius of Pac-Man become discovered and stay discovered.
    /// - Shows discovered streets and buildings, Pac-Man, Nexus Cores in discovered cells (gone once collected),
    ///   Bugs only on discovered cells within sightRadius of Pac-Man right now, and the Generator always.
    /// The maze is drawn into a small texture that is redrawn only when new cells are discovered;
    /// the icons are drawn on top every frame. Added automatically by GameHUD.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        // FPS: no GUILayout here, so skip IMGUI's extra Layout pass
        void Awake() => useGUILayout = false;

        [Tooltip("Show the whole city, all Nexus Cores and all Bugs from the start (no fog of war)")]
        public bool revealAll = true;
        [Tooltip("Cells closer than this to Pac-Man become discovered")]
        public float revealRadius = 3f;
        [Tooltip("Bugs are shown when they are on a discovered cell closer than this to Pac-Man")]
        public float sightRadius = 10f;
        [Tooltip("Minimap diameter as a fraction of the screen height")]
        public float sizeOfScreen = 0.3f;
        public float margin = 16f;

        const int PixelsPerCell = 6;

        static readonly Color32 Hidden = new Color32(0, 0, 0, 0);
        static readonly Color32 Street = new Color32(0x1F, 0x86, 0xA8, 255);   // dim cyan road
        static readonly Color32 Building = new Color32(0x18, 0x22, 0x3A, 255); // dark blue block

        LevelBuilder level;
        bool[,] discovered;
        Texture2D mapTex, dotTex, diamondTex, discTex, ringTex;
        Color32[] pixels;
        bool dirty;
        Vector2Int lastPlayerCell = new Vector2Int(int.MinValue, int.MinValue);

        public bool IsDiscovered(Vector2Int c) =>
            discovered != null && level.InBounds(c) && discovered[c.x, c.y];

        void Update()
        {
            if (level == null)
            {
                level = LevelBuilder.Instance;
                if (level == null || level.Width == 0) return;
                Setup();
            }

            var player = PlayerMovement.Instance;
            if (player != null && player.CurrentCell != lastPlayerCell)
            {
                lastPlayerCell = player.CurrentCell;
                Reveal(lastPlayerCell);
            }
            if (dirty) Redraw();
        }

        void Setup()
        {
            discovered = new bool[level.Width, level.Height];
            mapTex = new Texture2D(level.Width * PixelsPerCell, level.Height * PixelsPerCell, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color32[mapTex.width * mapTex.height];
            dotTex = MakeShape(32, (x, y) => 1f - new Vector2(x, y).magnitude);                 // filled circle
            diamondTex = MakeShape(32, (x, y) => 1f - (Mathf.Abs(x) + Mathf.Abs(y)));          // diamond
            discTex = MakeShape(128, (x, y) => 1f - new Vector2(x, y).magnitude);              // background
            ringTex = MakeShape(128, (x, y) => 0.03f - Mathf.Abs(new Vector2(x, y).magnitude - 0.96f)); // border
            if (revealAll)
                for (int y = 0; y < level.Height; y++)
                    for (int x = 0; x < level.Width; x++)
                        discovered[x, y] = true;
            dirty = true;
        }

        /// <summary>Marks every cell (street or building) closer than revealRadius as discovered.</summary>
        void Reveal(Vector2Int center)
        {
            int r = Mathf.CeilToInt(revealRadius);
            for (int y = center.y - r; y <= center.y + r; y++)
                for (int x = center.x - r; x <= center.x + r; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!level.InBounds(c) || discovered[x, y]) continue;
                    if ((c - center).sqrMagnitude > revealRadius * revealRadius) continue;
                    discovered[x, y] = true;
                    dirty = true;
                }
        }

        void Redraw()
        {
            dirty = false;
            int w = mapTex.width;
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    var col = !discovered[x, y] ? Hidden : level.IsWalkable(c) ? Street : Building;
                    // Texture row 0 is the bottom; grid row 0 is the top of the maze
                    int py0 = (level.Height - 1 - y) * PixelsPerCell, px0 = x * PixelsPerCell;
                    for (int py = 0; py < PixelsPerCell; py++)
                        for (int px = 0; px < PixelsPerCell; px++)
                            pixels[(py0 + py) * w + px0 + px] = col;
                }
            mapTex.SetPixels32(pixels);
            mapTex.Apply();
        }

        void OnGUI()
        {
            if (level == null || mapTex == null || IntroSequence.Active) return;

            float diameter = Mathf.Clamp(Screen.height * sizeOfScreen, 150f, 360f);
            var circle = new Rect(Screen.width - diameter - margin, margin, diameter, diameter);

            // Dark round panel with a cyan frame
            GUI.color = new Color(0.03f, 0.05f, 0.11f, 0.88f);
            GUI.DrawTexture(circle, discTex);
            GUI.color = GameHUD.Accent;
            GUI.DrawTexture(circle, ringTex);

            // The square maze fits inside the circle
            float side = diameter * 0.68f;
            float cellPx = side / Mathf.Max(level.Width, level.Height);
            var map = new Rect(circle.center.x - cellPx * level.Width / 2f, circle.center.y - cellPx * level.Height / 2f,
                               cellPx * level.Width, cellPx * level.Height);
            GUI.color = Color.white;
            GUI.DrawTexture(map, mapTex);

            // Generator: always shown; blinks once all cores are collected (go back there)
            var gm = GameManager.Instance;
            bool ready = gm != null && gm.AllCoresCollected;
            Color genColor = ready ? Visuals.Cyan : Visuals.CorruptedRed;
            if (ready) genColor.a = 0.55f + Mathf.Sin(Time.time * 6f) * 0.45f;
            GUI.color = genColor;
            DrawIcon(map, cellPx, level.GeneratorCell, 1.7f, diamondTex);

            // Bugs on discovered cells near Pac-Man
            var player = PlayerMovement.Instance;
            if (player != null)
            {
                foreach (var bug in BugAI.All)
                {
                    if (bug.IsEaten) continue;
                    var bc = bug.CurrentCell;
                    if (!revealAll && (!IsDiscovered(bc) || (bc - player.CurrentCell).sqrMagnitude > sightRadius * sightRadius)) continue;
                    // Big, pulsing icons so Bugs are easy to spot on the small map
                    var bp = WorldToGrid(bug.transform.position);
                    var glow = bug.BodyColor; glow.a = 0.35f + Mathf.Sin(Time.time * 8f) * 0.15f;
                    GUI.color = glow;
                    DrawIcon(map, cellPx, bp, 3.2f, dotTex);
                    GUI.color = Color.black;
                    DrawIcon(map, cellPx, bp, 2.3f, dotTex);
                    GUI.color = bug.BodyColor;
                    DrawIcon(map, cellPx, bp, 1.9f, dotTex);
                }

                // Power-ups: small dots in their own colour (Devour blinks like the classic pellet)
                foreach (var pair in level.PowerUps)
                {
                    if (pair.Value == null || !discovered[pair.Key.x, pair.Key.y]) continue;
                    var pc = pair.Value.type == PowerUpType.Devour && Mathf.Repeat(Time.time * 2.5f, 1f) < 0.4f
                        ? Color.white : PowerUp.ColorFor(pair.Value.type);
                    GUI.color = Color.black;
                    DrawIcon(map, cellPx, new Vector2(pair.Key.x, pair.Key.y), 2.1f, dotTex);
                    GUI.color = pc;
                    DrawIcon(map, cellPx, new Vector2(pair.Key.x, pair.Key.y), 1.5f, dotTex);
                }

                // Nexus Cores (discovered and not collected): big pulsing diamonds with a dark outline,
                // drawn above the Bugs so a Bug guarding a core never hides it
                float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.15f;
                foreach (var c in level.CoreCells)
                {
                    if (!discovered[c.x, c.y] || !level.HasCore(c)) continue;
                    var cp = new Vector2(c.x, c.y);
                    GUI.color = Color.black;
                    DrawIcon(map, cellPx, cp, 2.9f * pulse, diamondTex);
                    GUI.color = Visuals.NeonYellow;
                    DrawIcon(map, cellPx, cp, 2.2f * pulse, diamondTex);
                }

                // Pac-Man on top, with a dark outline so it is easy to spot
                var p = WorldToGrid(player.transform.position);
                GUI.color = Color.black;
                DrawIcon(map, cellPx, p, 1.8f, dotTex);
                GUI.color = Visuals.NeonYellow;
                DrawIcon(map, cellPx, p, 1.4f, dotTex);
            }
            GUI.color = Color.white;
        }

        /// <summary>World position to fractional grid coordinates (x right, y down), for smooth icons.</summary>
        Vector2 WorldToGrid(Vector3 p) =>
            new Vector2(p.x / level.cellSize, level.Height - 1 - p.z / level.cellSize);

        static void DrawIcon(Rect map, float cellPx, Vector2 grid, float sizeInCells, Texture2D tex)
        {
            float s = Mathf.Max(4f, cellPx * sizeInCells);
            float cx = map.x + (grid.x + 0.5f) * cellPx, cy = map.y + (grid.y + 0.5f) * cellPx;
            GUI.DrawTexture(new Rect(cx - s / 2f, cy - s / 2f, s, s), tex);
        }

        /// <summary>White texture whose alpha is f(x, y), with x and y from -1 to 1 (soft edge).</summary>
        static Texture2D MakeShape(int size, System.Func<float, float, float> f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(f(u, v) * size * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }
    }
}
