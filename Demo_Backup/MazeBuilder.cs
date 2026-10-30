using System.Collections.Generic;
using UnityEngine;

// Ndërton labirintin nga një listë tekstesh.
//  #=mur  .=pikë e vogël  o=pikë e madhe  P=fillimi i Pac-Man  G=fillimi i një fantazme  hapësirë=rrugë e zbrazët
// Rreshti i mesit (rreshti 4) është i hapur në të dy anët: ky është tuneli anësor.
public class MazeBuilder : MonoBehaviour
{
    public static MazeBuilder Instance { get; private set; }

    public float cellSize = 1f;
    public float wallHeight = 1f;
    public float outerThickness = 0.25f; // outer border thickness, in cells
    public float outerHeight = 0.5f;     // outer border height

    static readonly string[] layout =
    {
        "###########",
        "#o...#...o#",
        "#.##.#.##.#",
        "#...G.G...#",
        "...........",
        "#.##.#.##.#",
        "#o...#...o#",
        "#....P....#",
        "###########",
    };

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int DotsRemaining { get; private set; }
    public Vector2Int PlayerStart { get; private set; }
    public List<Vector2Int> GhostStarts { get; } = new List<Vector2Int>();
    public Vector3 Center => new Vector3((Width - 1) * cellSize / 2f, 0f, -(Height - 1) * cellSize / 2f);

    bool[,] walls;
    GameObject[,] dots;
    bool[,] isBig;

    static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0)
    };

    void Awake()
    {
        Instance = this;
        Build();
    }

    void Build()
    {
        Height = layout.Length;
        Width = layout[0].Length;
        walls = new bool[Width, Height];
        dots = new GameObject[Width, Height];
        isBig = new bool[Width, Height];

        // Dyshemeja (Plane është 10x10 njësi)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(transform);
        floor.transform.localScale = new Vector3(Width * cellSize / 10f, 1f, Height * cellSize / 10f);
        floor.transform.position = Center;
        Paint(floor, new Color(0.05f, 0.05f, 0.12f));

        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                char c = layout[row][col];
                Vector3 pos = CellToWorld(col, row);

                if (c == '#')
                {
                    walls[col, row] = true;
                    BuildWall(col, row, pos);
                }
                else if (c == '.' || c == 'o')
                {
                    bool big = (c == 'o');
                    GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    dot.name = big ? "PowerPellet" : "Dot";
                    dot.transform.SetParent(transform);
                    dot.transform.localScale = Vector3.one * (big ? 0.45f : 0.2f) * cellSize;
                    dot.transform.position = pos + Vector3.up * 0.5f;
                    Destroy(dot.GetComponent<Collider>()); // pikat kontrollohen me qeliza, jo me fizikë
                    Paint(dot, new Color(1f, 0.9f, 0.7f));
                    dots[col, row] = dot;
                    isBig[col, row] = big;
                    DotsRemaining++;
                }
                else if (c == 'P') PlayerStart = new Vector2Int(col, row);
                else if (c == 'G') GhostStarts.Add(new Vector2Int(col, row));
            }
        }
    }

    // Inner walls are full-size blocks. The outer border is a thin, low slab on the outside
    // edge of the border cells, so it does not hide the maze from the tilted camera.
    void BuildWall(int col, int row, Vector3 pos)
    {
        bool top = row == 0, bottom = row == Height - 1, left = col == 0, right = col == Width - 1;

        if (!(top || bottom || left || right))
        {
            MakeSlab(pos + Vector3.up * (wallHeight / 2f), new Vector3(cellSize, wallHeight, cellSize));
            return;
        }

        float t = outerThickness * cellSize;
        float h = outerHeight;
        float edge = cellSize / 2f - t / 2f;
        Vector3 up = Vector3.up * (h / 2f);

        if (top)    MakeSlab(pos + up + new Vector3(0, 0,  edge), new Vector3(cellSize, h, t));
        if (bottom) MakeSlab(pos + up + new Vector3(0, 0, -edge), new Vector3(cellSize, h, t));
        if (left)   MakeSlab(pos + up + new Vector3(-edge, 0, 0), new Vector3(t, h, cellSize));
        if (right)  MakeSlab(pos + up + new Vector3( edge, 0, 0), new Vector3(t, h, cellSize));
    }

    void MakeSlab(Vector3 center, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(transform);
        wall.transform.localScale = size;
        wall.transform.position = center;
        Paint(wall, new Color(0.1f, 0.2f, 0.9f));
    }

    static void Paint(GameObject go, Color color)
    {
        go.GetComponent<Renderer>().material.color = color;
    }

    public Vector3 CellToWorld(int col, int row)
    {
        return new Vector3(col * cellSize, 0f, -row * cellSize);
    }

    public bool IsWall(int col, int row)
    {
        if (row < 0 || row >= Height || col < 0 || col >= Width) return true;
        return walls[col, row];
    }

    // Ha pikën në qelizën (col,row) nëse ka. Kthen pikët (0 = nuk kishte pikë).
    public int TryEatDot(int col, int row)
    {
        if (col < 0 || col >= Width || row < 0 || row >= Height) return 0;
        if (dots[col, row] == null) return 0;

        Destroy(dots[col, row]);
        dots[col, row] = null;
        DotsRemaining--;
        return isBig[col, row] ? 50 : 10;
    }

    // Qelizat fqinje ku mund të shkohet (pa mure; tuneli nuk përdoret nga fantazmat).
    public List<Vector2Int> OpenNeighbors(Vector2Int cell)
    {
        var list = new List<Vector2Int>(4);
        foreach (Vector2Int d in Dirs)
        {
            Vector2Int n = cell + d;
            if (!IsWall(n.x, n.y)) list.Add(n);
        }
        return list;
    }

    // BFS: hapi i parë në rrugën më të shkurtër nga 'from' te 'to'. Kthen false nëse nuk ka rrugë.
    public bool TryNextStep(Vector2Int from, Vector2Int to, out Vector2Int step)
    {
        step = from;
        if (from == to) return false;

        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        cameFrom[from] = from;

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            if (cur == to) break;
            foreach (Vector2Int n in OpenNeighbors(cur))
            {
                if (cameFrom.ContainsKey(n)) continue;
                cameFrom[n] = cur;
                queue.Enqueue(n);
            }
        }

        if (!cameFrom.ContainsKey(to)) return false;

        // Ec mbrapsht nga 'to' deri te qeliza e parë pas 'from'
        Vector2Int node = to;
        while (cameFrom[node] != from) node = cameFrom[node];
        step = node;
        return true;
    }
}
