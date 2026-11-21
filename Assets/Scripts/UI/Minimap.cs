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
        Texture2D mapTex, dotTex, diamondTex, discTex, ringTex, pacOpenTex, pacHalfTex, smallRingTex;
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
            // Pac-Man icon (17 Nov): real Pac-Man shape with a mouth facing +x, so he never looks like a core or a fruit dot
            pacOpenTex = MakePacMan(64, 38f);
            pacHalfTex = MakePacMan(64, 12f);
            smallRingTex = MakeShape(64, (x, y) => 0.09f - Mathf.Abs(new Vector2(x, y).magnitude - 0.85f));
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
                    // Realistic look (21 Nov): muted grey streets on near-black blocks instead of cyan on blue
                    var street = RealisticCity.Active ? new Color32(0x9A, 0x9E, 0xA6, 255) : Street;
                    var block = RealisticCity.Active ? new Color32(0x12, 0x14, 0x19, 255) : Building;
                    var col = !discovered[x, y] ? Hidden : level.IsWalkable(c) ? street : block;
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

            // Realistic look (21 Nov, Blur-inspired HUD): smaller map, thin white frame instead of a thick neon ring
            bool clean = RealisticCity.Active;
            float diameter = clean ? Mathf.Clamp(Screen.height * 0.22f, 110f, 280f) : Mathf.Clamp(Screen.height * sizeOfScreen, 150f, 360f);
            float m = clean ? 28f * GameHUD.UiScale : margin;
            var circle = new Rect(Screen.width - diameter - m, m, diameter, diameter);

            GUI.color = clean ? new Color(0.01f, 0.012f, 0.02f, 0.72f) : new Color(0.03f, 0.05f, 0.11f, 0.88f);
            GUI.DrawTexture(circle, discTex);
            GUI.color = clean ? new Color(1f, 1f, 1f, 0.3f) : GameHUD.Accent;
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
            if (clean)
            {
                // 21 Nov: Generator = square (cores are diamonds), dark outline
                GUI.color = Color.black;
                DrawIcon(map, cellPx, level.GeneratorCell, 3.0f, Texture2D.whiteTexture);
                GUI.color = genColor;
                DrawIcon(map, cellPx, level.GeneratorCell, 2.2f, Texture2D.whiteTexture);
            }
            else
            {
                GUI.color = genColor;
                DrawIcon(map, cellPx, level.GeneratorCell, 1.7f, diamondTex);
            }

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
                    var glow = clean ? new Color(1f, 0.15f, 0.2f, 0.45f + Mathf.Sin(Time.time * 8f) * 0.2f) : bug.BodyColor;
                    if (!clean) glow.a = 0.35f + Mathf.Sin(Time.time * 8f) * 0.15f;
                    GUI.color = glow;
                    DrawIcon(map, cellPx, bp, 3.2f, dotTex);
                    GUI.color = Color.black;
                    DrawIcon(map, cellPx, bp, 2.3f, dotTex);
                    GUI.color = bug.BodyColor;
                    DrawIcon(map, cellPx, bp, 1.9f, dotTex);
                }

                // Power fruits (17 Nov): small fruit pictures (apple, lime, plum, grapes) in their own colour,
                // with a dark outline, leaf and stem, so they read as fruits and not as dots. The apple (Devour)
                // blinks like the classic power pellet.
                foreach (var pair in level.PowerUps)
                {
                    if (pair.Value == null || !discovered[pair.Key.x, pair.Key.y]) continue;
                    var type = pair.Value.type;
                    bool blink = type == PowerUpType.Devour && Mathf.Repeat(Time.time * 2.5f, 1f) < 0.35f;
                    GUI.color = blink ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
                    DrawIcon(map, cellPx, new Vector2(pair.Key.x, pair.Key.y), clean ? 3.6f : 2.8f, FruitIcon(type));
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

                // Pac-Man on top (17 Nov): Pac-Man shape facing his direction, chomping, white outline
                // and a pulsing white ring, clearly bigger than cores and fruits
                var p = WorldToGrid(player.transform.position);
                float ringPulse = Mathf.Repeat(Time.time * 1.2f, 1f);
                GUI.color = new Color(1f, 1f, 1f, 0.8f * (1f - ringPulse));
                DrawIcon(map, cellPx, p, 3.2f + ringPulse * 3.0f, smallRingTex);

                var d = player.Direction;
                float angle = d == Vector2Int.zero ? 0f : Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; // grid y down = screen down
                var mouthTex = Mathf.Repeat(Time.time * 6f, 1f) < 0.5f ? pacOpenTex : pacHalfTex;
                float size = Mathf.Max(4f, cellPx * 3.4f);
                Vector2 center = new Vector2(map.x + (p.x + 0.5f) * cellPx, map.y + (p.y + 0.5f) * cellPx);
                if (clean)
                {
                    // 21 Nov: "you" = white round badge with a dark edge and Pac-Man inside, clearly not a core or fruit
                    float b = size * 1.5f;
                    GUI.color = Color.black;
                    GUI.DrawTexture(new Rect(center.x - b * 0.58f, center.y - b * 0.58f, b * 1.16f, b * 1.16f), dotTex);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(center.x - b / 2f, center.y - b / 2f, b, b), dotTex);
                    size *= 0.8f;
                }
                var saved = GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, center);
                GUI.color = clean ? Color.black : Color.white;   // outline
                GUI.DrawTexture(new Rect(center.x - size * 0.58f, center.y - size * 0.58f, size * 1.16f, size * 1.16f), mouthTex);
                GUI.color = Visuals.NeonYellow;
                GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), mouthTex);
                GUI.matrix = saved;
            }
            GUI.color = Color.white;
            if (clean) DrawLegend(circle);
        }

        GUIStyle legendStyle;

        /// <summary>Tiny legend under the minimap: YOU / CORE / POWER / BUG / GENERATOR with their map symbols.</summary>
        void DrawLegend(Rect circle)
        {
            if (legendStyle == null)
            {
                legendStyle = new GUIStyle(GUI.skin.label) { wordWrap = false, fontSize = Mathf.RoundToInt(10f * Mathf.Max(1f, GameHUD.UiScale)), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Overflow };
                if (GameHUD.HudFont != null) legendStyle.font = GameHUD.HudFont;
                legendStyle.normal.textColor = new Color(0.78f, 0.81f, 0.86f, 0.95f);
            }
            float s = 9f * Mathf.Max(1f, GameHUD.UiScale);
            float rowH = s + 6f;
            var items = new (string label, System.Action<Rect> icon)[]
            {
                ("YOU", r => { GUI.color = Color.white; GUI.DrawTexture(r, dotTex); }),
                ("CORE", r => { GUI.color = Visuals.NeonYellow; GUI.DrawTexture(r, diamondTex); }),
                ("POWER", r => { GUI.color = Color.white; GUI.DrawTexture(r, FruitIcon(PowerUpType.Devour)); }),
                ("BUG", r => { GUI.color = new Color(1f, 0.25f, 0.3f); GUI.DrawTexture(r, dotTex); }),
                ("GENERATOR", r => { GUI.color = Visuals.CorruptedRed; GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), Texture2D.whiteTexture); }),
            };
            // one column under the map, right-aligned with it (dark strip behind so it reads over the city)
            float w = 96f * Mathf.Max(1f, GameHUD.UiScale);
            float y = circle.yMax + 8f;
            float x0 = circle.xMax - w + 8f;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(circle.xMax - w, y - 4f, w, rowH * items.Length + 6f), Texture2D.whiteTexture);
            for (int i = 0; i < items.Length; i++)
            {
                int row = i, col = 0; float colW = w;
                var r = new Rect(x0 + col * colW, y + row * rowH, s, s);
                items[i].icon(r);
                GUI.color = Color.white;
                GUI.Label(new Rect(r.xMax + 4f, r.y - 3f, colW, s + 6f), items[i].label, legendStyle);
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

        // ---------- Fruit icons (17 Nov) ----------
        static readonly System.Collections.Generic.Dictionary<PowerUpType, Texture2D> fruitIcons =
            new System.Collections.Generic.Dictionary<PowerUpType, Texture2D>();

        /// <summary>Coloured fruit picture for a power-up type (cached). Public so the HUD legend can use it too.</summary>
        public static Texture2D FruitIcon(PowerUpType type)
        {
            if (fruitIcons.TryGetValue(type, out var tex) && tex != null) return tex;
            tex = MakeFruit(64, type);
            fruitIcons[type] = tex;
            return tex;
        }

        // Signed "inside" distances (positive inside), coordinates from -1 to 1, y up
        static float Circle(float u, float v, float cx, float cy, float r) => r - new Vector2(u - cx, v - cy).magnitude;
        static float Ellipse(float u, float v, float cx, float cy, float rx, float ry)
        {
            float dx = (u - cx) / rx, dy = (v - cy) / ry;
            return (1f - Mathf.Sqrt(dx * dx + dy * dy)) * Mathf.Min(rx, ry);
        }
        static float Box(float u, float v, float cx, float cy, float hx, float hy) =>
            Mathf.Min(hx - Mathf.Abs(u - cx), hy - Mathf.Abs(v - cy));

        static Texture2D MakeFruit(int size, PowerUpType type)
        {
            Color body = PowerUp.ColorFor(type);
            Color leaf = Visuals.Hex("#3DFF8C"), stem = Visuals.Hex("#7A4A2A");
            Color outline = new Color(0.02f, 0.03f, 0.08f, 1f);
            const float outlineW = 0.14f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    float fB, fL = -9f, fS = -9f;
                    float hx, hy;   // highlight spot
                    switch (type)
                    {
                        case PowerUpType.Devour:      // apple: two round lobes, stem, leaf to the right
                            fB = Mathf.Max(Circle(u, v, -0.2f, -0.12f, 0.55f), Circle(u, v, 0.2f, -0.12f, 0.55f));
                            fS = Box(u, v, 0f, 0.5f, 0.06f, 0.16f);
                            fL = Ellipse(u, v, 0.28f, 0.6f, 0.24f, 0.11f);
                            hx = -0.3f; hy = 0.05f;
                            break;
                        case PowerUpType.SpeedBurst:  // lime: wide oval with small pointy tips
                            fB = Mathf.Max(Ellipse(u, v, 0f, 0f, 0.72f, 0.52f),
                                 Mathf.Max(Circle(u, v, -0.74f, 0f, 0.12f), Circle(u, v, 0.74f, 0f, 0.12f)));
                            hx = -0.25f; hy = 0.2f;
                            break;
                        case PowerUpType.Shield:      // plum: tall oval, short stem, leaf to the left
                            fB = Ellipse(u, v, 0f, -0.12f, 0.58f, 0.68f);
                            fS = Box(u, v, 0f, 0.6f, 0.05f, 0.12f);
                            fL = Ellipse(u, v, -0.26f, 0.66f, 0.22f, 0.1f);
                            hx = -0.22f; hy = 0.1f;
                            break;
                        default:                      // grapes: berries in a triangle, stem and leaf on top
                        {
                            fB = -9f;
                            float r = 0.23f;
                            fB = Mathf.Max(fB, Circle(u, v, -0.42f, 0.28f, r));
                            fB = Mathf.Max(fB, Circle(u, v, 0f, 0.28f, r));
                            fB = Mathf.Max(fB, Circle(u, v, 0.42f, 0.28f, r));
                            fB = Mathf.Max(fB, Circle(u, v, -0.21f, -0.08f, r));
                            fB = Mathf.Max(fB, Circle(u, v, 0.21f, -0.08f, r));
                            fB = Mathf.Max(fB, Circle(u, v, 0f, -0.44f, r));
                            fS = Box(u, v, 0f, 0.6f, 0.05f, 0.12f);
                            fL = Ellipse(u, v, 0.24f, 0.68f, 0.2f, 0.09f);
                            hx = -9f; hy = -9f;   // no single highlight (berries get their own below)
                            break;
                        }
                    }

                    float all = Mathf.Max(fB, Mathf.Max(fL, fS));
                    Color c;
                    if (fB > 0f)
                    {
                        c = body;
                        // small white shine
                        if (Circle(u, v, hx, hy, 0.13f) > 0f) c = Color.Lerp(body, Color.white, 0.75f);
                        if (type == PowerUpType.Magnet && fB < 0.06f) c = Color.Lerp(body, outline, 0.6f); // berry seams
                    }
                    else if (fL > 0f) c = leaf;
                    else if (fS > 0f) c = stem;
                    else c = outline;
                    // Soft outer edge of the outline
                    c.a = Mathf.Clamp01((all + outlineW) * size * 0.5f);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        /// <summary>White Pac-Man disc with a wedge mouth facing +x (halfMouthDeg = half the mouth opening).</summary>
        static Texture2D MakePacMan(int size, float halfMouthDeg)
        {
            float half = halfMouthDeg * Mathf.Deg2Rad;
            return MakeShape(size, (x, y) =>
            {
                float r = new Vector2(x, y).magnitude;
                float edge = 1f - r;
                float mouth = (Mathf.Abs(Mathf.Atan2(y, x)) - half) * Mathf.Max(r, 0.05f); // distance to the mouth edge
                return Mathf.Min(edge, mouth + 0.02f);
            });
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
