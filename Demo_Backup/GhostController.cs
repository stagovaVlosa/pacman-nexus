using System.Collections.Generic;
using UnityEngine;

// A ghost moves cell by cell. At every cell it picks its next direction:
//  Random = picks randomly (never straight back)
//  Chase  = follows Pac-Man along the shortest path (BFS), but gives up for a few seconds
//           every now and then (wanders), so level 1 stays fair
public class GhostController : MonoBehaviour
{
    public enum Mode { Random, Chase }

    public Mode mode = Mode.Chase;
    public float speed = 3f;          // Pac-Man is 5, so the player can always outrun a ghost
    public float releaseDelay = 3f;   // seconds before the ghost leaves its start cell
    public float chaseTime = 8f;      // chasing for this long...
    public float wanderTime = 5f;     // ...then wandering for this long

    PacManController pacman;
    Vector2Int startCell, cell, targetCell, dir;
    bool moving;
    float delayTimer;
    float cycleTimer;

    static readonly Vector3 lift = Vector3.up * 0.5f;

    public void Init(Vector2Int start, Color color, Mode m, PacManController target, float delay)
    {
        startCell = start;
        mode = m;
        pacman = target;
        releaseDelay = delay;
        GetComponent<Renderer>().material.color = color;
        transform.localScale = Vector3.one * 0.8f * MazeBuilder.Instance.cellSize;
        ResetToStart();
    }

    public void ResetToStart()
    {
        cell = startCell;
        targetCell = cell;
        dir = Vector2Int.zero;
        moving = false;
        delayTimer = releaseDelay;
        cycleTimer = 0f;
        transform.position = MazeBuilder.Instance.CellToWorld(cell.x, cell.y) + lift;
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        MazeBuilder maze = MazeBuilder.Instance;

        // Wait at the start for a moment
        if (delayTimer > 0f) { delayTimer -= Time.deltaTime; return; }

        cycleTimer += Time.deltaTime;
        if (cycleTimer > chaseTime + wanderTime) cycleTimer = 0f;

        if (!moving && !ChooseNext()) return;

        Vector3 targetPos = maze.CellToWorld(targetCell.x, targetCell.y) + lift;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * maze.cellSize * Time.deltaTime);

        if (transform.position == targetPos)
        {
            cell = targetCell;
            moving = false;
        }
    }

    bool ChooseNext()
    {
        MazeBuilder maze = MazeBuilder.Instance;
        Vector2Int next = cell;
        bool found = false;

        bool chasing = mode == Mode.Chase && pacman != null && cycleTimer < chaseTime;
        if (chasing)
            found = maze.TryNextStep(cell, pacman.Cell, out next);

        if (!found)
        {
            // Pick randomly, but avoid turning straight back if there is another way
            List<Vector2Int> options = maze.OpenNeighbors(cell);
            if (options.Count == 0) return false;
            if (options.Count > 1) options.Remove(cell - dir);
            next = options[Random.Range(0, options.Count)];
        }

        dir = next - cell;
        targetCell = next;
        moving = true;
        return true;
    }
}
