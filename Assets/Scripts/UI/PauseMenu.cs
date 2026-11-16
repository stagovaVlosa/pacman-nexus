using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>
    /// ESC pauses the game: Resume / Restart region / Quit, plus the controls.
    /// Time stops while paused (Time.timeScale = 0) and GameManager.IsPlaying is false,
    /// so Pac-Man, the Bugs and the power-up timers all wait.
    /// Created automatically in every scene; no setup needed.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        // FPS: no GUILayout here, so skip IMGUI's extra Layout pass
        void Awake() => useGUILayout = false;

        public static bool Paused { get; private set; }

        static readonly string[] Items = { "RESUME", "RESTART REGION", "QUIT GAME" };
        static readonly string[] Controls =
        {
            "ARROWS / WASD    move",
            "SPACE    Energy Blast (30 energy)",
            "ESC    pause",
            "ENTER    next region / skip dialogue",
        };

        int selected;
        GUIStyle titleStyle, itemStyle, hintStyle, controlStyle;
        readonly Rect[] itemRects = new Rect[3];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<PauseMenu>() != null) return;
            var go = new GameObject("PauseMenu");
            DontDestroyOnLoad(go);
            go.AddComponent<PauseMenu>();
            SceneManager.sceneLoaded += (scene, mode) => SetPaused(false);
        }

        static void SetPaused(bool on)
        {
            Paused = on;
            Time.timeScale = on ? 0f : 1f;
            AudioListener.pause = on;
        }

        void OnDestroy() { if (Paused) SetPaused(false); }

        static bool CanPause
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && !gm.LevelComplete && !gm.GameOver && !IntroSequence.Active;
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (!Paused)
            {
                if (kb.escapeKey.wasPressedThisFrame && CanPause) { SetPaused(true); selected = 0; }
                return;
            }

            if (kb.escapeKey.wasPressedThisFrame) { SetPaused(false); return; }
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                selected = (selected + Items.Length - 1) % Items.Length;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                selected = (selected + 1) % Items.Length;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                Choose(selected);
        }

        void Choose(int i)
        {
            switch (i)
            {
                case 0:
                    SetPaused(false);
                    break;
                case 1:
                    SetPaused(false);
                    if (GameManager.Instance != null) GameManager.Instance.Restart();
                    break;
                case 2:
                    SetPaused(false);
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        void OnGUI()
        {
            if (!Paused) return;
            GUI.depth = -100;
            if (titleStyle == null) CreateStyles();

            float W = Screen.width, H = Screen.height;
            var accent = GameHUD.Accent;

            // Dark veil over the frozen game
            GUI.color = new Color(0.02f, 0.02f, 0.07f, 0.78f);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);

            // Panel
            var panel = new Rect((W - 420f) / 2f, H * 0.5f - 210f, 420f, 420f);
            GUI.color = new Color(0.03f, 0.05f, 0.12f, 0.95f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = accent;
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.yMax - 2f, panel.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.y, 2f, panel.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panel.xMax - 2f, panel.y, 2f, panel.height), Texture2D.whiteTexture);

            GUI.color = Color.white;
            titleStyle.normal.textColor = accent;
            GUI.Label(new Rect(panel.x, panel.y + 18f, panel.width, 50f), "PAUSED", titleStyle);

            // Menu items (keyboard or mouse)
            var e = Event.current;
            for (int i = 0; i < Items.Length; i++)
            {
                var r = new Rect(panel.x + 50f, panel.y + 90f + i * 52f, panel.width - 100f, 42f);
                itemRects[i] = r;
                if (e.type == EventType.MouseMove && r.Contains(e.mousePosition)) selected = i;
                bool sel = i == selected;
                if (sel)
                {
                    GUI.color = new Color(accent.r, accent.g, accent.b, 0.22f);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = accent;
                    GUI.DrawTexture(new Rect(r.x, r.y, 4f, r.height), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
                itemStyle.normal.textColor = sel ? Visuals.NeonYellow : new Color(0.85f, 0.9f, 1f, 0.85f);
                GUI.Label(r, Items[i], itemStyle);
                if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
                {
                    e.Use();
                    Choose(i);
                    return;
                }
            }

            // Controls
            float cy = panel.y + 260f;
            hintStyle.normal.textColor = accent;
            GUI.Label(new Rect(panel.x, cy, panel.width, 22f), "CONTROLS", hintStyle);
            for (int i = 0; i < Controls.Length; i++)
                GUI.Label(new Rect(panel.x, cy + 26f + i * 22f, panel.width, 22f), Controls[i], controlStyle);

            GUI.color = Color.white;
        }

        void CreateStyles()
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            itemStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            controlStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            controlStyle.normal.textColor = new Color(0.8f, 0.86f, 1f, 0.8f);
        }
    }
}
