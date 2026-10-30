using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Global rules: score, lives, being caught by a ghost, title / game over screens, HUD.
// Screens use OnGUI (no Canvas needed) so everything works right away; later we move to real UI.
// All screens are drawn in a virtual 720-pixel-high space, so they scale to any Game view size.
public class GameManager : MonoBehaviour
{
    public enum State { Title, Ready, Playing, GameOver, LevelWon }

    public static GameManager Instance { get; private set; }

    public int startLives = 3;
    public float catchDistance = 0.6f; // in cell units

    public State state = State.Title;
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int Level { get; private set; } = 1;
    public bool IsPlaying => state == State.Playing;

    PacManController pacman;
    readonly List<GhostController> ghosts = new List<GhostController>();
    GUIStyle hudStyle, titleStyle, headingStyle, textStyle, bodyStyle, buttonStyle;

    void Awake()
    {
        Instance = this;
        Lives = startLives;
        state = GameBootstrap.SkipTitle ? State.Ready : State.Title;
    }

    public void Register(PacManController p) { pacman = p; }
    public void Register(GhostController g) { ghosts.Add(g); }

    public void AddScore(int points) { Score += points; }

    void Update()
    {
        MazeBuilder maze = MazeBuilder.Instance;

        switch (state)
        {
            case State.Title:
                if (AnyKeyPressed()) state = State.Ready;
                return;

            case State.Ready:
                // Nothing moves until the player presses an arrow key or WASD
                if (MovementKeyPressed()) state = State.Playing;
                return;

            case State.GameOver:
            case State.LevelWon:
                if (RestartPressed()) GameBootstrap.Rebuild();
                return;

        }

        if (pacman == null) return;

        // Caught by a ghost?
        float limit = catchDistance * maze.cellSize;
        foreach (GhostController g in ghosts)
        {
            Vector3 d = g.transform.position - pacman.transform.position;
            d.y = 0f;
            if (d.magnitude < limit) { LoseLife(); return; }
        }

        // All dots eaten?
        if (maze.DotsRemaining == 0) state = State.LevelWon;
    }

    static bool AnyKeyPressed()
    {
        bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return key || click;
    }

    static bool MovementKeyPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;
        return kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
               kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
               kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ||
               kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
    }

    static bool RestartPressed()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame);
    }

    void LoseLife()
    {
        Lives--;
        if (Lives <= 0)
        {
            Lives = 0;
            state = State.GameOver;
            return;
        }
        pacman.ResetToStart();
        foreach (GhostController g in ghosts) g.ResetToStart();
        state = State.Ready; // wait for the player before anything moves again
    }

    void MakeStyles()
    {
        hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        hudStyle.normal.textColor = Color.white;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = new Color(1f, 0.9f, 0f);
        headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        headingStyle.normal.textColor = new Color(0.5f, 0.8f, 1f);
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        textStyle.normal.textColor = Color.white;
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft, wordWrap = true };
        bodyStyle.normal.textColor = Color.white;
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 26 };
    }

    void OnGUI()
    {
        if (hudStyle == null) MakeStyles();

        // Draw everything in a virtual space that is 720 high and as wide as the window needs
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float vw = Screen.width / s;
        float vh = 720f;
        float cx = vw / 2f;

        if (state == State.Title)
        {
            Dim(vw, vh);
            GUI.Label(new Rect(cx - 500, 20, 1000, 80), "PAC-MAN 3D", titleStyle);
            GUI.Label(new Rect(cx - 500, 100, 1000, 40), "Eat all the dots, but don't get eaten!", textStyle);

            GUI.Label(new Rect(cx - 500, 165, 1000, 40), "HOW TO PLAY", headingStyle);
            float x = cx - 330, y = 215, w = 660, h = 70;
            GUI.Label(new Rect(x, y,        w, h), "MOVE  -  Arrow keys or WASD. Pac-Man keeps moving until he hits a wall; press a direction early and he turns at the next opening.", bodyStyle);
            GUI.Label(new Rect(x, y + 75,   w, h), "GOAL  -  Eat every dot in the maze to finish the level. Big dots are worth 50 points.", bodyStyle);
            GUI.Label(new Rect(x, y + 125,  w, h), "DANGER  -  Touching a ghost costs one life. You have 3 lives. Ghosts are slower than you, so keep moving!", bodyStyle);
            GUI.Label(new Rect(x, y + 195,  w, h), "TUNNEL  -  The opening in the middle of the maze takes you straight to the other side.", bodyStyle);

            GUI.Label(new Rect(cx - 500, 560, 1000, 50), "Press any key to start", textStyle);
            return;
        }

        GUI.Label(new Rect(20, 10, 900, 40), "Score: " + Score + "     Lives: " + Lives + "     Level: " + Level, hudStyle);

        if (state == State.Ready)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 520, vw, 150), Texture2D.whiteTexture);
            GUI.color = old;
            string line = Lives < startLives ? "You were caught!  Lives left: " + Lives : "GET READY!";
            GUI.Label(new Rect(cx - 500, 530, 1000, 50), line, headingStyle);
            GUI.Label(new Rect(cx - 500, 590, 1000, 50), "Press an arrow key or WASD to move", textStyle);
        }

        if (state == State.GameOver || state == State.LevelWon)
        {
            Dim(vw, vh);
            string title = state == State.GameOver ? "GAME OVER" : "LEVEL COMPLETE!";
            GUI.Label(new Rect(cx - 500, 200, 1000, 90), title, titleStyle);
            GUI.Label(new Rect(cx - 500, 300, 1000, 50), "Score: " + Score, textStyle);
            GUI.Label(new Rect(cx - 500, 360, 1000, 50), "Press SPACE to play again", textStyle);
            if (GUI.Button(new Rect(cx - 120, 430, 240, 60), "Play again", buttonStyle)) GameBootstrap.Rebuild();
        }
    }

    static void Dim(float w, float h)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
