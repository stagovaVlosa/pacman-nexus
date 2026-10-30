// DISABLED: classic Pac-Man prototype (Step 1). Replaced by PAC-MAN: NEXUS in Assets/Scripts/Core, Player, World, UI.
// A copy is in Demo_Backup/ClassicStep1/. To turn it back on, add PACMAN_CLASSIC to Scripting Define Symbols.
#if PACMAN_CLASSIC
using UnityEngine;
using UnityEngine.InputSystem;

// HAPI 1: Rregullat bazë.
// Gjendjet: Ready (pret shigjetën e parë) -> Playing -> LevelWon (të gjitha pikat u ngrënë).
// P ose ESC: pauzë. Jetët, fantazmat dhe Game Over vijnë në hapin 2.
// Ekranet me OnGUI për tani; UI e vërtetë vjen më vonë.
public class GameManager : MonoBehaviour
{
    public enum State { Ready, Playing, Paused, LevelWon }

    public static GameManager Instance { get; private set; }

    public State state = State.Ready;
    public int Score { get; private set; }
    public int Level { get; private set; } = 1;
    public bool IsPlaying => state == State.Playing;

    float levelTime;
    GUIStyle hudStyle, bigStyle, textStyle, buttonStyle;

    void Awake()
    {
        Instance = this;
    }

    public void AddScore(int points) { Score += points; }

    void Update()
    {
        Keyboard kb = Keyboard.current;

        switch (state)
        {
            case State.Ready:
                // Asgjë nuk lëviz derisa lojtari të shtypë shigjetë ose WASD
                if (MovementKeyPressed(kb)) state = State.Playing;
                break;

            case State.Playing:
                levelTime += Time.deltaTime;
                if (PausePressed(kb)) state = State.Paused;
                else if (MazeBuilder.Instance.DotsRemaining == 0) state = State.LevelWon;
                break;

            case State.Paused:
                if (PausePressed(kb)) state = State.Playing;
                break;

            case State.LevelWon:
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    GameBootstrap.Rebuild();
                break;
        }
    }

    static bool MovementKeyPressed(Keyboard kb)
    {
        if (kb == null) return false;
        return kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
               kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
               kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ||
               kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
    }

    static bool PausePressed(Keyboard kb)
    {
        return kb != null && (kb.pKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame);
    }

    // ---------------- Ekrani ----------------

    void MakeStyles()
    {
        hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        hudStyle.normal.textColor = Color.white;
        bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 60, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bigStyle.normal.textColor = new Color(1f, 0.9f, 0f);
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        textStyle.normal.textColor = Color.white;
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 26 };
    }

    void OnGUI()
    {
        if (hudStyle == null) MakeStyles();

        // Gjithçka vizatohet në një hapësirë virtuale 720 piksel të lartë, që të shkallëzohet në çdo ekran
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float vw = Screen.width / s;
        float cx = vw / 2f;

        MazeBuilder maze = MazeBuilder.Instance;
        int eaten = maze.TotalDots - maze.DotsRemaining;
        GUI.Label(new Rect(20, 10, 1200, 40),
            "SCORE  " + Score + "      LEVEL  " + Level + "      DOTS  " + eaten + " / " + maze.TotalDots, hudStyle);

        switch (state)
        {
            case State.Ready:
                Band(vw, 520, 150);
                GUI.Label(new Rect(cx - 500, 530, 1000, 60), "READY!", bigStyle);
                GUI.Label(new Rect(cx - 500, 600, 1000, 50), "Press an arrow key or WASD to start", textStyle);
                break;

            case State.Paused:
                Band(vw, 280, 160);
                GUI.Label(new Rect(cx - 500, 290, 1000, 70), "PAUSED", bigStyle);
                GUI.Label(new Rect(cx - 500, 370, 1000, 50), "Press P or ESC to continue", textStyle);
                break;

            case State.LevelWon:
                Band(vw, 0, 720);
                GUI.Label(new Rect(cx - 500, 190, 1000, 90), "LEVEL COMPLETE!", bigStyle);
                GUI.Label(new Rect(cx - 500, 290, 1000, 50), "Score: " + Score, textStyle);
                GUI.Label(new Rect(cx - 500, 335, 1000, 50), "Time: " + Mathf.FloorToInt(levelTime / 60f) + ":" + (Mathf.FloorToInt(levelTime) % 60).ToString("00"), textStyle);
                GUI.Label(new Rect(cx - 500, 390, 1000, 50), "Press SPACE to play again", textStyle);
                if (GUI.Button(new Rect(cx - 120, 450, 240, 60), "Play again", buttonStyle)) GameBootstrap.Rebuild();
                break;
        }
    }

    static void Band(float width, float y, float height)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(0, y, width, height), Texture2D.whiteTexture);
        GUI.color = old;
    }
}

#endif
