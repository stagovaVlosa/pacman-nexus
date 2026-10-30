using UnityEngine;
using UnityEngine.InputSystem;

// HAPI 1: Pac-Man.
// Lëviz qelizë pas qelize. Vazhdon drejt derisa përplaset në mur.
// Kthesa që lojtari shtyp herët ruhet (buffer) dhe bëhet te hapja e parë.
// Kthimi prapa lejohet në çdo moment. Tuneli anësor e çon në anën tjetër.
public class PacManController : MonoBehaviour
{
    public float speed = 5f; // qeliza në sekondë

    Vector2Int cell;        // qeliza ku është (ose nga e cila po niset)
    Vector2Int targetCell;  // qeliza drejt së cilës po lëviz
    Vector2Int dir;         // drejtimi aktual
    Vector2Int wantedDir;   // drejtimi i kërkuar nga lojtari
    bool moving;

    public Vector2Int Cell => cell;
    public Vector2Int Direction => dir;

    static readonly Vector3 Lift = Vector3.up * 0.45f;

    void Start()
    {
        transform.localScale = Vector3.one * 0.8f * MazeBuilder.Instance.cellSize;
        ResetToStart();
    }

    // Kthen Pac-Man te fillimi (në start, dhe më vonë pas humbjes së një jete)
    public void ResetToStart()
    {
        MazeBuilder maze = MazeBuilder.Instance;
        cell = maze.PlayerStart;
        targetCell = cell;
        dir = Vector2Int.zero;
        wantedDir = Vector2Int.zero;
        moving = false;
        transform.position = maze.CellToWorld(cell) + Lift;
        transform.rotation = Quaternion.Euler(0f, 90f, 0f); // shikon djathtas
    }

    void Update()
    {
        // Inputi lexohet gjithmonë, që shigjeta që nis lojën të jetë edhe drejtimi i parë
        ReadInput();
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        Move();
        Face();
    }

    void ReadInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Rreshti 0 është lart, prandaj "lart" = rreshti - 1
        if (kb.upArrowKey.wasPressedThisFrame    || kb.wKey.wasPressedThisFrame) wantedDir = Vector2Int.down;  // (0,-1)
        if (kb.downArrowKey.wasPressedThisFrame  || kb.sKey.wasPressedThisFrame) wantedDir = Vector2Int.up;    // (0, 1)
        if (kb.leftArrowKey.wasPressedThisFrame  || kb.aKey.wasPressedThisFrame) wantedDir = Vector2Int.left;
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) wantedDir = Vector2Int.right;
    }

    void Move()
    {
        MazeBuilder maze = MazeBuilder.Instance;

        // Kthim prapa në mes të rrugës: menjëherë
        if (moving && dir != Vector2Int.zero && wantedDir == -dir)
        {
            (cell, targetCell) = (targetCell, cell);
            dir = wantedDir;
        }

        if (!moving)
        {
            // Provo së pari kthesën e kërkuar, pastaj vazhdo drejt
            if (TryStart(wantedDir)) dir = wantedDir;
            else if (!TryStart(dir)) return; // para murit: qëndron
        }

        float step = speed * maze.cellSize * Time.deltaTime;
        Vector3 targetPos = maze.CellToWorld(targetCell) + Lift;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        if (transform.position == targetPos)
        {
            cell = targetCell;
            moving = false;

            int points = maze.TryEatDot(cell);
            if (points > 0 && GameManager.Instance != null) GameManager.Instance.AddScore(points);
        }
    }

    // Provon të nisë lëvizjen drejt fqinjit në drejtimin d.
    bool TryStart(Vector2Int d)
    {
        if (d == Vector2Int.zero) return false;
        MazeBuilder maze = MazeBuilder.Instance;
        Vector2Int next = cell + d;

        // Tuneli: dalim nga njëra anë, shfaqemi në anën tjetër dhe vazhdojmë
        if (maze.TryWrap(next, out Vector2Int wrapped))
        {
            cell = wrapped;
            transform.position = maze.CellToWorld(cell) + Lift;
            next = cell + d;
            if (!maze.IsWalkable(next)) { targetCell = cell; moving = false; return false; }
        }

        if (!maze.IsWalkable(next)) return false;

        targetCell = next;
        moving = true;
        return true;
    }

    // Pac-Man kthehet nga drejtimi i lëvizjes
    void Face()
    {
        if (dir == Vector2Int.zero) return;
        Vector3 look = new Vector3(dir.x, 0f, -dir.y);
        Quaternion want = Quaternion.LookRotation(look, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 900f * Time.deltaTime);
    }
}
