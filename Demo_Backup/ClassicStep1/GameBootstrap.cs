using System.Collections.Generic;
using UnityEngine;

// Ndërton gjithë lojën kur shtyp Play, pa pasur nevojë të vendosësh asgjë me dorë në skenë.
// Rifillimi i fshin të gjitha dhe i ndërton përsëri.
// HAPI 1: labirinti, Pac-Man, kamera dhe drita. Fantazmat shtohen në hapin 2.
public static class GameBootstrap
{
    static readonly List<GameObject> spawned = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        spawned.Clear();
        SetupCameraAndLight();
        Build();
    }

    public static void Rebuild()
    {
        foreach (GameObject go in spawned)
            if (go != null) Object.Destroy(go);
        spawned.Clear();
        Build();
    }

    static void Build()
    {
        Track(new GameObject("GameManager")).AddComponent<GameManager>();

        // Labirinti (Awake e ndërton menjëherë)
        Track(new GameObject("Maze")).AddComponent<MazeBuilder>();

        // Pac-Man: sferë e verdhë që shkëlqen pak
        GameObject player = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
        player.name = "PacMan";
        Object.Destroy(player.GetComponent<Collider>());
        player.GetComponent<Renderer>().sharedMaterial = MazeBuilder.MakeMaterial(new Color(1f, 0.9f, 0f), 0.4f);
        player.AddComponent<PacManController>();

        // Kamera ndjek Pac-Man
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.SnapToTarget();
        }
    }

    static void SetupCameraAndLight()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            // Sfond i zi si në arcade
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        // Nëse skena nuk ka dritë, shtojmë një
        if (Object.FindAnyObjectByType<Light>() == null)
        {
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    static GameObject Track(GameObject go)
    {
        spawned.Add(go);
        return go;
    }
}
