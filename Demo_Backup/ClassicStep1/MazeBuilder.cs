using System.Collections.Generic;
using UnityEngine;

// HAPI 1: Labirinti.
// Ndërtohet nga një listë tekstesh. Simbolet:
//   #  mur
//   .  pikë e vogël (10 pikë)
//   o  pikë e madhe / power pellet (50 pikë)
//   ' ' rrugë pa pikë
//   P  fillimi i Pac-Man
//   G  vend fantazme brenda shtëpisë (përdoret në hapin 2)
//   -  dera e shtëpisë së fantazmave (Pac-Man nuk kalon)
// Një rresht që është i hapur në të dy skajet është tunel anësor.
// Rreshti 0 është lart. Kolona rritet djathtas (x+), rreshti rritet poshtë (z-).
public class MazeBuilder : MonoBehaviour
{
    public static MazeBuilder Instance { get; private set; }

    public float cellSize = 1f;
    public float wallHeight = 0.6f;
    public float wallThickness = 0.3f;

    // Niveli 1: 19 kolona x 21 rreshta, simetrik, me tunel në rreshtin 9.
    public static readonly string[] Level1 =
    {
        "###################",
        "#........#........#",
        "#o##.###.#.###.##o#",
        "#.................#",
        "#.##.#.#####.#.##.#",
        "#....#...#...#....#",
        "####.### # ###.####",
        "####.#       #.####",
        "####.# ##-## #.####",
        "    .  #GGG#  .    ",
        "####.# ##### #.####",
        "####.#       #.####",
        "####.# ##### #.####",
        "#........#........#",
        "#.##.###.#.###.##.#",
        "#o.#.....P.....#.o#",
        "##.#.#.#####.#.#.##",
        "#....#...#...#....#",
        "#.######.#.######.#",
        "#.................#",
        "###################",
    };

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int TotalDots { get; private set; }
    public int DotsRemaining { get; private set; }
    public Vector2Int PlayerStart { get; private set; }
    public List<Vector2Int> GhostStarts { get; } = new List<Vector2Int>();
    public Vector3 Center => new Vector3((Width - 1) * cellSize / 2f, 0f, -(Height - 1) * cellSize / 2f);

    char[,] grid;
    GameObject[,] dots;
    readonly List<Transform> pellets = new List<Transform>();

    Material wallMat, doorMat, dotMat, pelletMat, floorMat;

    void Awake()
    {
        Instance = this;
        wallMat   = MakeMaterial(new Color(0.12f, 0.25f, 1f), 1.2f);
        doorMat   = MakeMaterial(new Color(1f, 0.6f, 0.85f), 0.8f);
        dotMat    = MakeMaterial(new Color(1f, 0.85f, 0.7f), 0.6f);
        pelletMat = MakeMaterial(new Color(1f, 0.85f, 0.7f), 1.5f);
        floorMat  = MakeMaterial(new Color(0.02f, 0.02f, 0.06f), 0f);
        Build(Level1);
    }

    void Build(string[] layout)
    {
        Height = layout.Length;
        Width = layout[0].Length;
        grid = new char[Width, Height];
        dots = new GameObject[Width, Height];

        for (int row = 0; row < Height; row++)
        {
            if (layout[row].Length != Width)
                Debug.LogError("Maze row " + row + " has " + layout[row].Length + " characters, expected " + Width);
            for (int col = 0; col < Width; col++)
                grid[col, row] = col < layout[row].Length ? layout[row][col] : '#';
        }

        // Dyshemeja (Plane është 10x10 njësi)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(transform);
        floor.transform.localScale = new Vector3(Width * cellSize / 10f, 1f, Height * cellSize / 10f);
        floor.transform.position = Center;
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        Transform wallParent = new GameObject("Walls").transform;
        wallParent.SetParent(transform);
        Transform dotParent = new GameObject("Dots").transform;
        dotParent.SetParent(transform);

        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                char c = grid[col, row];
                Vector3 pos = CellToWorld(new Vector2Int(col, row));

                switch (c)
                {
                    case '#': BuildWall(col, row, pos, wallParent); break;
                    case '-':
                        MakeBlock(pos + Vector3.up * 0.15f, new Vector3(cellSize, 0.1f, cellSize * 0.2f), doorMat, wallParent, "Door");
                        break;
                    case '.':
                    case 'o':
                        BuildDot(col, row, pos, c == 'o', dotParent);
                        break;
                    case 'P': PlayerStart = new Vector2Int(col, row); break;
                    case 'G': GhostStarts.Add(new Vector2Int(col, row)); break;
                }
            }
        }
        DotsRemaining = TotalDots;
    }

    // Muret vizatohen si vija të holla: një shtyllë në qendër të qelizës dhe "krahë" drejt
    // qelizave fqinje që janë gjithashtu mur. Blloqet 2x2 mbushen, që zonat e trasha të duken plot.
    void BuildWall(int col, int row, Vector3 pos, Transform parent)
    {
        float t = wallThickness * cellSize;
        float h = wallHeight;
        Vector3 up = Vector3.up * (h / 2f);

        MakeBlock(pos + up, new Vector3(t, h, t), wallMat, parent, "Wall");

        bool right = IsWallChar(col + 1, row);
        bool down  = IsWallChar(col, row + 1);

        if (right) MakeBlock(pos + up + new Vector3(cellSize / 2f, 0f, 0f), new Vector3(cellSize, h, t), wallMat, parent, "Wall");
        if (down)  MakeBlock(pos + up + new Vector3(0f, 0f, -cellSize / 2f), new Vector3(t, h, cellSize), wallMat, parent, "Wall");
        if (right && down && IsWallChar(col + 1, row + 1))
            MakeBlock(pos + up + new Vector3(cellSize / 2f, 0f, -cellSize / 2f), new Vector3(cellSize, h, cellSize), wallMat, parent, "WallFill");
    }

    void BuildDot(int col, int row, Vector3 pos, bool big, Transform parent)
    {
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = big ? "PowerPellet" : "Dot";
        dot.transform.SetParent(parent);
        dot.transform.localScale = Vector3.one * (big ? 0.45f : 0.18f) * cellSize;
        dot.transform.position = pos + Vector3.up * 0.4f;
        Destroy(dot.GetComponent<Collider>()); // pikat kontrollohen me qeliza, jo me fizikë
        dot.GetComponent<Renderer>().sharedMaterial = big ? pelletMat : dotMat;
        dots[col, row] = dot;
        if (big) pellets.Add(dot.transform);
        TotalDots++;
    }

    void Update()
    {
        // Pikat e mëdha pulsojnë që të dallohen
        float s = (0.45f + 0.08f * Mathf.Sin(Time.time * 6f)) * cellSize;
        foreach (Transform p in pellets)
            if (p != null) p.localScale = Vector3.one * s;
    }

    static void MakeBlock(Vector3 center, Vector3 size, Material mat, Transform parent, string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localScale = size;
        go.transform.position = center;
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    public static Material MakeMaterial(Color color, float glow)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (glow > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * glow);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        return m;
    }

    // ---------- Pyetje për rrjetën (përdoren nga Pac-Man dhe më vonë nga fantazmat) ----------

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, 0f, -cell.y * cellSize);
    }

    bool InRange(int col, int row) => col >= 0 && col < Width && row >= 0 && row < Height;

    bool IsWallChar(int col, int row) => InRange(col, row) && grid[col, row] == '#';

    // A mund të ecë Pac-Man në këtë qelizë? (muret dhe dera e shtëpisë ndalojnë)
    public bool IsWalkable(Vector2Int cell)
    {
        if (!InRange(cell.x, cell.y)) return false;
        char c = grid[cell.x, cell.y];
        return c != '#' && c != '-';
    }

    // Nëse qeliza del jashtë majtas/djathtas në një rresht tuneli, kthen qelizën në anën tjetër.
    public bool TryWrap(Vector2Int cell, out Vector2Int wrapped)
    {
        wrapped = cell;
        if (cell.y < 0 || cell.y >= Height) return false;
        if (cell.x < 0) wrapped.x = Width - 1;
        else if (cell.x >= Width) wrapped.x = 0;
        else return false;
        return IsWalkable(wrapped);
    }

    // Ha pikën në qelizë nëse ka. Kthen pikët (0 = nuk kishte pikë).
    public int TryEatDot(Vector2Int cell)
    {
        if (!InRange(cell.x, cell.y) || dots[cell.x, cell.y] == null) return 0;

        bool big = grid[cell.x, cell.y] == 'o';
        if (big) pellets.Remove(dots[cell.x, cell.y].transform);
        Destroy(dots[cell.x, cell.y]);
        dots[cell.x, cell.y] = null;
        DotsRemaining--;
        return big ? 50 : 10;
    }
}
