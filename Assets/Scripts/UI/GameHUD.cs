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

        GUIStyle label, big, small, tiny, gameOver, value, tinyLeft, tinyRight;
        Texture2D lifeIcon, blastIcon, arrowIcon;
        EnergyBlast blast;
        string message;
        float messageUntil;

        void Awake()
        {
            Instance = this;
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
            if (gm == null) return;
            if (label == null) CreateStyles();

            // Status panel (top left): level, Nexus Cores, score, lives
            var panel = new Rect(16f, 16f, 250f, 142f);
            DrawPanel(panel);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 8f, 230f, 26f), gm.levelName.ToUpper(), label);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 38f, 230f, 24f), $"NEXUS CORES  {gm.CoresCollected}/{gm.CoresRequired}", value);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 64f, 230f, 24f), $"SCORE  {gm.Score}", value);
            for (int i = 0; i < gm.Lives; i++)
                GUI.DrawTexture(new Rect(panel.x + 14f + i * 34f, panel.y + 98f, 28f, 28f), lifeIcon);

            // Energy panel (bottom center): bar 0-100 with the number
            var ePanel = new Rect((Screen.width - 340f) / 2f, Screen.height - 70f, 340f, 54f);
            DrawPanel(ePanel);
            GUI.Label(new Rect(ePanel.x + 14f, ePanel.y + 6f, 150f, 20f), "ENERGY", tinyLeft);
            GUI.Label(new Rect(ePanel.xMax - 164f, ePanel.y + 6f, 150f, 20f), $"{Mathf.FloorToInt(gm.Energy)} / {gm.maxEnergy:0}", tinyRight);
            var bar = new Rect(ePanel.x + 14f, ePanel.y + 30f, ePanel.width - 28f, 12f);
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Visuals.Cyan;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(gm.Energy / gm.maxEnergy), bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

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
            label.normal.textColor = Visuals.Cyan;
            small = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            small.normal.textColor = Visuals.NeonYellow;
            big = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Visuals.Cyan;
            gameOver = new GUIStyle(big) { fontSize = 64 };
            tiny = new GUIStyle(small) { fontSize = 12, fontStyle = FontStyle.Bold };
            tiny.normal.textColor = Visuals.Cyan;
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
            var c = Visuals.Cyan;
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
            GUI.color = blast.CanUse ? Visuals.Cyan : new Color(0f, 0.85f, 1f, 0.35f);
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
            var c = Visuals.Cyan;
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
