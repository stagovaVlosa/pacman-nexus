using System.Collections.Generic;
using UnityEngine;

// Builds the whole game at runtime, so nothing has to be placed by hand in the scene.
// Restart destroys everything and builds it again (no scene reload needed).
// (Later, when we move to real models, this is replaced by objects placed in the scene.)
public static class GameBootstrap
{
    // When true the next GameManager skips the title screen (used by "play again").
    public static bool SkipTitle;

    static readonly List<GameObject> spawned = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SkipTitle = false;
        spawned.Clear();
        Build();
    }

    public static void Rebuild()
    {
        foreach (GameObject go in spawned)
            if (go != null) Object.Destroy(go);
        spawned.Clear();

        SkipTitle = true;
        Build();
    }

    static void Build()
    {
        GameManager manager = Track(new GameObject("GameManager")).AddComponent<GameManager>();

        // Maze (Awake builds it immediately)
        MazeBuilder maze = Track(new GameObject("Maze")).AddComponent<MazeBuilder>();

        // Pac-Man
        GameObject player = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
        player.name = "PacMan";
        Object.Destroy(player.GetComponent<Collider>());
        player.GetComponent<Renderer>().material.color = new Color(1f, 0.9f, 0f);
        PacManController pacman = player.AddComponent<PacManController>();
        manager.Register(pacman);

        // Ghosts: level 1 has 2 ghosts. The red one chases, the pink one wanders (for testing).
        Color[] colors = { new Color(1f, 0.1f, 0.1f), new Color(1f, 0.5f, 0.8f) };
        GhostController.Mode[] modes = { GhostController.Mode.Chase, GhostController.Mode.Random };
        float[] delays = { 0f, 0f }; // seconds before each ghost leaves its start cell (0 = immediately)
        for (int i = 0; i < maze.GhostStarts.Count && i < colors.Length; i++)
        {
            GameObject go = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            go.name = "Ghost" + (i + 1);
            Object.Destroy(go.GetComponent<Collider>());
            GhostController ghost = go.AddComponent<GhostController>();
            ghost.Init(maze.GhostStarts[i], colors[i], modes[i], pacman, delays[i]);
            manager.Register(ghost);
        }

        // Camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow follow = cam.gameObject.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.SnapToTarget();
        }
    }

    static GameObject Track(GameObject go)
    {
        spawned.Add(go);
        return go;
    }
}
