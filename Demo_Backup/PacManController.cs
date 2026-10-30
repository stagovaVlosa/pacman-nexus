using UnityEngine;
using UnityEngine.InputSystem;

// Pac-Man lëviz qelizë pas qelize. Vazhdon në drejtimin aktual derisa përplaset
// në mur, dhe e ruan kthesën e radhës (buffer) deri sa të bëhet e mundur.
public class PacManController : MonoBehaviour
{
    public float speed = 5f; // qeliza në sekondë

    Vector2Int cell;         // qeliza aktuale (col, row)
    Vector2Int targetCell;   // qeliza drejt së cilës po lëviz
    Vector2Int dir;          // drejtimi aktual
    Vector2Int bufferedDir;  // drejtimi i kërkuar nga lojtari
    bool moving;

    public Vector2Int Cell => cell;

    static readonly Vector3 lift = Vector3.up * 0.5f;

    void Start()
    {
        MazeBuilder maze = MazeBuilder.Instance;
        transform.localScale = Vector3.one * 0.8f * maze.cellSize;
        ResetToStart();
    }

    // Kthen Pac-Man te fillimi (në start dhe pas humbjes së një jete)
    public void ResetToStart()
    {
        MazeBuilder maze = MazeBuilder.Instance;
        cell = maze.PlayerStart;
        targetCell = cell;
        dir = Vector2Int.zero;
        bufferedDir = Vector2Int.zero;
        moving = false;
        transform.position = maze.CellToWorld(cell.x, cell.y) + lift;
    }

    void Update()
    {
        // Always read input, so the arrow key that starts the round also sets the first direction
        ReadInput();
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        Move();
    }

    void ReadInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // row 0 është lart në listë, prandaj "lart" = row - 1
        if (kb.upArrowKey.wasPressedThisFrame    || kb.wKey.wasPressedThisFrame) bufferedDir = new Vector2Int(0, -1);
        if (kb.downArrowKey.wasPressedThisFrame  || kb.sKey.wasPressedThisFrame) bufferedDir = new Vector2Int(0, 1);
        if (kb.leftArrowKey.wasPressedThisFrame  || kb.aKey.wasPressedThisFrame) bufferedDir = new Vector2Int(-1, 0);
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) bufferedDir = new Vector2Int(1, 0);
    }

    void Move()
    {
        MazeBuilder maze = MazeBuilder.Instance;

        // Kthim prapa në mes të rrugës lejohet menjëherë
        if (moving && bufferedDir == -dir && dir != Vector2Int.zero)
        {
            Vector2Int tmp = cell; cell = targetCell; targetCell = tmp;
            dir = bufferedDir;
        }

        if (!moving)
        {
            // Së pari provo kthesën e ruajtur, pastaj vazhdo drejt
            if (TryStart(bufferedDir)) dir = bufferedDir;
            else if (!TryStart(dir)) return; // ngecur para murit
        }

        Vector3 targetPos = maze.CellToWorld(targetCell.x, targetCell.y) + lift;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * maze.cellSize * Time.deltaTime);

        if (transform.position == targetPos)
        {
            cell = targetCell;
            moving = false;

            int points = maze.TryEatDot(cell.x, cell.y);
            if (points > 0 && GameManager.Instance != null) GameManager.Instance.AddScore(points);
        }
    }

    // Provon të nisë lëvizjen në drejtimin d. Kthen true nëse ia del.
    bool TryStart(Vector2Int d)
    {
        if (d == Vector2Int.zero) return false;
        MazeBuilder maze = MazeBuilder.Instance;

        int nc = cell.x + d.x;
        int nr = cell.y + d.y;

        // Tuneli anësor: dalim nga njëra anë, shfaqemi menjëherë në tjetrën
        if (nc < 0 || nc >= maze.Width)
        {
            cell.x = (nc < 0) ? maze.Width - 1 : 0;
            transform.position = maze.CellToWorld(cell.x, cell.y) + lift;
            nc = cell.x + d.x;
        }

        if (maze.IsWall(nc, nr)) return false;

        targetCell = new Vector2Int(nc, nr);
        moving = true;
        return true;
    }
}
