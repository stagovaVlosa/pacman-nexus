using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// HUD drawn with OnGUI (no Canvas setup needed): dark panels with a cyan frame.
    /// Top left: level, Nexus Cores x/10, score, lives. Bottom center: energy bar 0-100 and the Energy Blast button.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }
        /// <summary>Main UI colour of the current level (set by LevelBuilder from its theme).</summary>
        public static Color Accent = Visuals.Cyan;

        GUIStyle label, big, small, tiny, gameOver, value, tinyLeft, tinyRight;
        Texture2D lifeIcon, blastIcon, arrowIcon;
        Texture2D lifeCell, lifeFrame, lifeGlow;
        int lastLives = -1;
        float lifeLostAt = -10f;
        int lifeLostIndex = -1;
        EnergyBlast blast;
        string message;
        float messageUntil;
        Texture2D edgeGlow;

        class PopupText { public Vector3 pos; public string text; public Color color; public float start, life; }
        readonly System.Collections.Generic.List<PopupText> popups = new System.Collections.Generic.List<PopupText>();
        GUIStyle popupStyle, powerName;

        /// <summary>Floating text at a world position that rises and fades (points for eaten Bugs, power-up names).</summary>
        public void Popup(Vector3 worldPos, string text, Color color, float seconds)
        {
            int recent = 0;
            foreach (var q in popups) if (Time.time - q.start < 0.5f) recent++;
            worldPos += Vector3.up * 0.4f * recent;   // several at once: stack them instead of overlapping
            popups.Add(new PopupText { pos = worldPos, text = text, color = color, start = Time.time, life = seconds });
        }

        void Awake()
        {
            Instance = this;
            useGUILayout = false;   // FPS: no GUILayout here, so skip IMGUI's extra Layout pass
            if (!TryGetComponent(out Minimap _)) gameObject.AddComponent<Minimap>();
        }

        /// <summary>Shows a short message in the middle of the screen for a few seconds.</summary>
        public void ShowMessage(string text, float seconds)
        {
            message = text;
            messageUntil = Time.time + seconds;
        }

        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || IntroSequence.Active) return;
            if (label == null) CreateStyles();

            // Status panel (top left): level, Nexus Cores, score, lives
            var panel = new Rect(16f, 16f, 250f, 124f);
            // Panel border flashes red for a moment when a life is lost
            float lostFlash = Mathf.Clamp01(1f - (Time.time - lifeLostAt) / 0.6f);
            DrawPanel(panel);
            if (lostFlash > 0f) DrawFrame(panel, new Color(1f, 0.2f, 0.25f, lostFlash));
            GUI.Label(new Rect(panel.x + 14f, panel.y + 8f, 230f, 26f), gm.levelName.ToUpper(), label);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 38f, 230f, 24f), $"NEXUS CORES  {gm.CoresCollected}/{gm.CoresRequired}", value);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 64f, 230f, 24f), $"SCORE  {gm.Score}", value);
            DrawLifeCores(gm, new Vector2(panel.x + 14f, panel.y + 92f));

            // Energy panel (bottom center): bar 0-100 with the number
            var ePanel = new Rect((Screen.width - 340f) / 2f, Screen.height - 70f, 340f, 54f);
            DrawPanel(ePanel);
            GUI.Label(new Rect(ePanel.x + 14f, ePanel.y + 6f, 150f, 20f), "ENERGY", tinyLeft);
            GUI.Label(new Rect(ePanel.xMax - 164f, ePanel.y + 6f, 150f, 20f), $"{Mathf.FloorToInt(gm.Energy)} / {gm.maxEnergy:0}", tinyRight);
            var bar = new Rect(ePanel.x + 14f, ePanel.y + 30f, ePanel.width - 28f, 12f);
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Accent;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(gm.Energy / gm.maxEnergy), bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (gm.IsPlaying)
            {
                DrawDevourEdge();
                DrawPowerLegend(new Vector2(panel.x, panel.yMax + 8f));
            }
            DrawPopups();
            DrawBugArrows();
            DrawBlastIcon(new Rect(ePanel.xMax + 18f, Screen.height - 74f, 56f, 56f), gm);

            if (gm.GameOver)
            {
                GUI.color = new Color(0.03f, 0.02f, 0.08f, 0.75f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 70), "GAME OVER", gameOver);
                GUI.Label(new Rect(0, Screen.height / 2f + 15, Screen.width, 30), $"Score: {gm.Score}", small);
                GUI.Label(new Rect(0, Screen.height / 2f + 50, Screen.width, 30), "Press SPACE to restart", small);
            }
            else if (gm.LevelComplete)
            {
                GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 60), "REGION RESTORED", big);
                GUI.Label(new Rect(0, Screen.height / 2f + 20, Screen.width, 30), $"Final score: {gm.Score}", small);
                GUI.Label(new Rect(0, Screen.height / 2f + 55, Screen.width, 30),
                    gm.HasNextLevel ? "Press ENTER for the next region" : "More regions coming soon", small);
            }
            else
            {
                // Short message (e.g. reaching the Generator too early)
                if (message != null && Time.time < messageUntil)
                {
                    var box = new Rect((Screen.width - 460f) / 2f, Screen.height * 0.3f, 460f, 44f);
                    GUI.color = new Color(0.03f, 0.05f, 0.11f, 0.85f);
                    GUI.DrawTexture(box, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(box, message, small);
                }
            }
        }

        void CreateStyles()
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            label.normal.textColor = Accent;
            small = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            small.normal.textColor = Visuals.NeonYellow;
            big = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Accent;
            gameOver = new GUIStyle(big) { fontSize = 64 };
            tiny = new GUIStyle(small) { fontSize = 12, fontStyle = FontStyle.Bold };
            tiny.normal.textColor = Accent;
            gameOver.normal.textColor = Visuals.CorruptedRed;
            label.fontSize = 18;
            value = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            value.normal.textColor = Visuals.NeonYellow;
            tinyLeft = new GUIStyle(tiny) { alignment = TextAnchor.MiddleLeft };
            tinyRight = new GUIStyle(tiny) { alignment = TextAnchor.MiddleRight };
            lifeIcon = MakeLifeIcon();
            blastIcon = MakeBlastIcon();
            arrowIcon = MakeArrowIcon();
        }

        /// <summary>Dark HUD panel with a cyan frame (palette: Midnight Navy + Electric Cyan).</summary>
        public static void DrawPanel(Rect r, float alpha = 1f)
        {
            var old = GUI.color;
            GUI.color = new Color(0.03f, 0.05f, 0.11f, 0.85f * alpha);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            var c = Accent;
            c.a = alpha;
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, 2f, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - 2f, r.y, 2f, r.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>An arrow (in the Bug's color) at the screen edge for every Bug that is off screen, pointing toward it.</summary>
        void DrawBugArrows()
        {
            var cam = Camera.main;
            if (cam == null) return;
            const float margin = 0.04f;
            foreach (var bug in BugAI.All)
            {
                if (bug.IsEaten) continue;
                Vector3 vp = cam.WorldToViewportPoint(bug.transform.position);
                bool behind = vp.z < 0f;
                if (!behind && vp.x > margin && vp.x < 1f - margin && vp.y > margin && vp.y < 1f - margin) continue;

                // Direction from the screen center, pushed out to the edge
                var dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                if (behind) dir = -dir;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector2.down;
                float scale = (0.5f - margin) / Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
                var edge = new Vector2(0.5f, 0.5f) + dir * scale;
                var center = new Vector2(edge.x * Screen.width, (1f - edge.y) * Screen.height); // GUI y goes down
                float angle = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;

                var old = GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, center);
                var tint = bug.BodyColor;
                tint.a = 0.7f + Mathf.Sin(Time.time * 8f) * 0.3f;
                GUI.color = tint;
                GUI.DrawTexture(new Rect(center.x - 22f, center.y - 22f, 44f, 44f), arrowIcon);
                GUI.matrix = old;
                GUI.color = Color.white;
            }
        }

        /// <summary>Devour Mode: soft magenta glow around the screen edge (blinks when the time is almost over).</summary>
        void DrawDevourEdge()
        {
            var powers = PlayerPowers.Instance;
            if (powers == null || !powers.DevourActive) return;
            if (edgeGlow == null) edgeGlow = MakeEdgeGlow();
            // Calm (16 Nov, "overstimulating"): faint and steady, only a slow fade-pulse in the last seconds
            float a = 0.16f;
            if (powers.DevourLeft < powers.devourWarning) a *= 0.6f + 0.4f * Mathf.Sin(Time.time * 5f);
            var c = PowerUp.ColorFor(PowerUpType.Devour);
            GUI.color = new Color(c.r, c.g, c.b, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), edgeGlow);
            GUI.color = Color.white;
        }

        /// <summary>Transparent in the middle, white at the borders (tinted when drawn).</summary>
        static Texture2D MakeEdgeGlow()
        {
            const int W = 128, H = 72;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = Mathf.Min(x, W - 1 - x) / (W * 0.08f), dy = Mathf.Min(y, H - 1 - y) / (H * 0.1f);
                    float a = 1f - Mathf.Clamp01(Mathf.Min(dx, dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return tex;
        }

        Texture2D dotTex;
        GUIStyle legendDim;

        /// <summary>
        /// Fruit legend under the status panel (16 Nov): every power-up fruit with what it does.
        /// Inactive rows are dim; an active one lights up and shows its time left (Plum shows READY until used).
        /// </summary>
        void DrawPowerLegend(Vector2 origin)
        {
            var powers = PlayerPowers.Instance;
            if (powers == null) return;
            if (powerName == null)
                powerName = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            if (legendDim == null)
                legendDim = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
            if (dotTex == null) dotTex = MakeDot();

            const float rowH = 22f;
            var types = new[] { PowerUpType.Devour, PowerUpType.SpeedBurst, PowerUpType.Shield, PowerUpType.Magnet };
            var panel = new Rect(origin.x, origin.y, 250f, 24f + types.Length * rowH + 6f);
            DrawPanel(panel, 0.8f);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 4f, 200f, 18f), "POWER FRUITS", tinyLeft);

            float y = panel.y + 24f;
            foreach (var type in types)
            {
                float left = 0f, total = 0f;
                bool active;
                switch (type)
                {
                    case PowerUpType.Devour: left = powers.DevourLeft; total = powers.devourTime; active = left > 0f; break;
                    case PowerUpType.SpeedBurst: left = powers.SpeedLeft; total = powers.speedTime; active = left > 0f; break;
                    case PowerUpType.Magnet: left = powers.MagnetLeft; total = powers.magnetTime; active = left > 0f; break;
                    default: active = powers.HasShield; break;
                }
                var c = PowerUp.ColorFor(type);
                float a = active ? 1f : 0.55f;
                bool ending = active && total > 0f && left < 2f;
                if (ending) a = 0.55f + 0.45f * Mathf.Sin(Time.time * 6f);

                GUI.color = new Color(c.r, c.g, c.b, a);
                GUI.DrawTexture(new Rect(panel.x + 12f, y + 3f, 12f, 12f), dotTex);
                powerName.normal.textColor = new Color(c.r, c.g, c.b, a);
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.x + 30f, y, 70f, 18f), PowerUp.FruitFor(type), powerName);
                legendDim.normal.textColor = new Color(0.85f, 0.9f, 1f, active ? 0.95f : 0.5f);
                GUI.Label(new Rect(panel.x + 98f, y, 100f, 18f), PowerUp.EffectFor(type), legendDim);

                if (active)
                {
                    string right = total > 0f ? left.ToString("0.0") + "s" : "READY";
                    GUI.Label(new Rect(panel.xMax - 52f, y, 46f, 18f), right, powerName);
                    if (total > 0f)
                    {
                        GUI.color = new Color(c.r, c.g, c.b, 0.8f);
                        GUI.DrawTexture(new Rect(panel.x + 30f, y + 18f, (panel.width - 42f) * Mathf.Clamp01(left / total), 2f), Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                }
                y += rowH;
            }
            GUI.color = Color.white;
        }

        static Texture2D MakeDot()
        {
            const int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).magnitude;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(size / 2f - r)));
                }
            tex.Apply();
            return tex;
        }

        void DrawPopups()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (popupStyle == null)
                popupStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                float k = (Time.time - p.start) / p.life;
                if (k >= 1f) { popups.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(p.pos + Vector3.up * k * 0.8f);
                if (sp.z < 0f) continue;
                var rect = new Rect(sp.x - 120f, Screen.height - sp.y - 16f, 240f, 32f);
                float a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                popupStyle.normal.textColor = new Color(0f, 0f, 0f, a * 0.8f);
                GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), p.text, popupStyle);
                popupStyle.normal.textColor = new Color(p.color.r, p.color.g, p.color.b, a);
                GUI.Label(rect, p.text, popupStyle);
            }
        }

        /// <summary>White triangle pointing right (tinted when drawn), with a soft edge.</summary>
        static Texture2D MakeArrowIcon()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var c = Color.white;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = Mathf.Abs((y + 0.5f) / size - 0.5f) * 2f; // v: 0 middle, 1 top/bottom
                    // Inside when the half-height at this x (shrinks toward the tip) is larger than v
                    float halfHeight = 1f - u;
                    float a = Mathf.Clamp01((halfHeight - v) * size * 0.5f) * Mathf.Clamp01(u * size * 0.25f);
                    tex.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Energy Blast button: dark while cooling down (fills from the bottom), dim without energy.</summary>
        void DrawBlastIcon(Rect r, GameManager gm)
        {
            if (blast == null && PlayerMovement.Instance != null) blast = PlayerMovement.Instance.GetComponent<EnergyBlast>();
            if (blast == null) return;

            GUI.color = new Color(0.03f, 0.05f, 0.11f, 0.85f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            bool enoughEnergy = gm.Energy >= blast.cost;
            GUI.color = blast.CanUse ? Color.white : new Color(1f, 1f, 1f, enoughEnergy ? 0.6f : 0.3f);
            GUI.DrawTexture(r, blastIcon);

            // Cooldown: a dark curtain that shrinks as the blast recharges
            float left = 1f - blast.CooldownProgress;
            if (left > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * left), Texture2D.whiteTexture);
            }

            // Cyan frame, brighter when ready
            GUI.color = blast.CanUse ? Accent : new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, 2f, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - 2f, r.y, 2f, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string text = blast.CooldownLeft > 0f ? blast.CooldownLeft.ToString("0.0") : (enoughEnergy ? "SPACE" : $"{blast.cost:0} EN");
            GUI.Label(new Rect(r.x - 20f, r.yMax + 1f, r.width + 40f, 18f), text, tiny);
        }

        /// <summary>Concentric cyan rings (the blast wave) drawn into a small texture.</summary>
        static Texture2D MakeBlastIcon()
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var c = Accent;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).magnitude;
                    float a = Mathf.Max(
                        Mathf.Clamp01(1.6f - Mathf.Abs(r - 18f)),           // outer ring
                        Mathf.Clamp01(1.4f - Mathf.Abs(r - 11f)) * 0.8f);   // inner ring
                    if (r < 5f) a = 1f;                                       // center
                    tex.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Yellow disc with a wedge cut out (Pac-Man seen from above), drawn into a small texture.</summary>
        /// <summary>
        /// Lives as "Life Cores": hexagonal energy cells with Pac-Man's silhouette inside.
        /// Remaining cells glow yellow (Pac-Man's colour) and pulse; the frame uses the level accent.
        /// A lost cell flashes red, cracks and fades out, leaving a dim empty frame. The last cell blinks red.
        /// </summary>
        void DrawLifeCores(GameManager gm, Vector2 origin)
        {
            if (lifeCell == null) { lifeCell = MakeHex(true); lifeFrame = MakeHex(false); lifeGlow = Visuals.GradientTexture(true); }
            if (lastLives < 0) lastLives = gm.Lives;
            if (gm.Lives < lastLives) { lifeLostAt = Time.time; lifeLostIndex = gm.Lives; }
            lastLives = gm.Lives;

            int slots = Mathf.Max(gm.startLives, gm.Lives);
            const float w = 18f, h = 21f, gap = 5f;   // small (16 Nov: were 26x30)
            float t = Time.time;
            var old = GUI.color;
            for (int i = 0; i < slots; i++)
            {
                var r = new Rect(origin.x + i * (w + gap), origin.y, w, h);
                bool alive = i < gm.Lives;
                bool justLost = i == lifeLostIndex && t - lifeLostAt < 0.8f;

                // empty slot frame
                GUI.color = new Color(Accent.r, Accent.g, Accent.b, alive ? 0.9f : 0.3f);
                GUI.DrawTexture(r, lifeFrame);

                if (alive)
                {
                    bool warning = gm.Lives == 1;
                    float pulse = 0.82f + 0.18f * Mathf.Sin(t * (warning ? 7f : 2.5f) + i * 0.9f);
                    Color c = warning ? Color.Lerp(Visuals.NeonYellow, Visuals.CorruptedRed, 0.5f + 0.5f * Mathf.Sin(t * 7f)) : Color.Lerp(Visuals.NeonYellow, Color.white, 0.2f);
                    var glowRect = new Rect(r.center.x - w, r.center.y - h, w * 2f, h * 2f);
                    GUI.color = new Color(c.r, c.g, c.b, 0.5f * pulse);
                    GUI.DrawTexture(glowRect, lifeGlow);
                    GUI.color = new Color(c.r, c.g, c.b, pulse);
                    GUI.DrawTexture(r, lifeCell);
                }
                else if (justLost)
                {
                    // flash red, swell a little and fade out
                    float k = (t - lifeLostAt) / 0.8f;
                    float grow = 1f + k * 0.5f;
                    var rr = new Rect(r.center.x - w * grow / 2f, r.center.y - h * grow / 2f, w * grow, h * grow);
                    GUI.color = new Color(1f, 0.25f, 0.3f, 1f - k);
                    GUI.DrawTexture(rr, lifeCell);
                    GUI.color = new Color(1f, 1f, 1f, (1f - k) * 0.8f);
                    GUI.DrawTexture(new Rect(r.center.x - 1f, r.y + 2f, 2f, h - 4f), Texture2D.whiteTexture);   // crack
                }
            }
            GUI.color = new Color(Accent.r, Accent.g, Accent.b, 0.8f);
            GUI.Label(new Rect(origin.x + slots * (w + gap) + 4f, origin.y, 120f, h), "LIFE CORES", tinyLeft);
            GUI.color = old;
        }

        static void DrawFrame(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 3f, r.width, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, 3f, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - 3f, r.y, 3f, r.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>White pointy-top hexagon (tinted when drawn): filled with Pac-Man's silhouette, or only its outline.</summary>
        static Texture2D MakeHex(bool filled)
        {
            const int W = 52, H = 60;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float R = W / 2f - 1.5f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float px = Mathf.Abs(x - W / 2f + 0.5f), py = Mathf.Abs(y - H / 2f + 0.5f);
                    // distance to a pointy-top hexagon edge (inradius R horizontally)
                    float d = Mathf.Max(px, px * 0.5f + py * 0.866f);
                    Color c;
                    if (filled)
                    {
                        float a = Mathf.Clamp01(R - 2f - d);
                        float shade = 0.92f + 0.08f * (1f - (y / (float)H));
                        // Pac-Man silhouette in the middle (darker), mouth to the right
                        float cx = x - W / 2f + 0.5f, cy = y - H / 2f + 0.5f;
                        float pr = Mathf.Sqrt(cx * cx + cy * cy);
                        float ang = Mathf.Abs(Mathf.Atan2(cy, cx)) * Mathf.Rad2Deg;
                        bool pac = pr < R * 0.5f && !(ang < 35f && cx > 0f);
                        if (pac) shade *= 0.28f;
                        c = new Color(shade, shade, shade, a);
                    }
                    else
                    {
                        float a = Mathf.Clamp01(1.6f - Mathf.Abs(d - (R - 0.5f)));
                        c = new Color(1f, 1f, 1f, a);
                    }
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        static Texture2D MakeLifeIcon()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var color = Visuals.NeonYellow;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f + 0.5f, dy = y - size / 2f + 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Abs(Mathf.Atan2(dy, dx)) * Mathf.Rad2Deg;
                    float a = Mathf.Clamp01(size / 2f - r);          // soft edge
                    if (angle < 32f && dx > 0f) a = 0f;               // mouth facing right
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a));
                }
            tex.Apply();
            return tex;
        }
    }
}
