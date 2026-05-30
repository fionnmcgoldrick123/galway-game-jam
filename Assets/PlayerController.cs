using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Grid-based player controller.
///
/// Setup:
///   1. Add this component to your Player GameObject.
///   2. In the Inspector assign the SpriteRenderer (or leave it and the script
///      will find it on the same GameObject).
///   3. Make sure GridManager exists in the scene with its Tilemap assigned.
///
/// Movement:
///   Arrow keys / WASD move the player one cell at a time.
///   The player animates (lerps) to the centre of the target cell.
///   If no tile exists at the target cell the player "falls" and the scene restarts.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Time in seconds it takes to slide between two cells.")]
    public float moveTime = 0.15f;

    [Tooltip("Offset to apply to the player's position on each tile (compensates for sprite pivot).")]
    public Vector3 pivotOffset = Vector3.zero;

    [Header("Death")]
    [Tooltip("Brief pause before the scene is reloaded after the player dies.")]
    public float deathDelay = 0.6f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // ── internal state ──────────────────────────────────────────────────────
    private Vector3Int _currentCell;
    private bool       _isMoving;
    private bool       _isDead;

    // ── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("[PlayerController] GridManager not found in scene!");
            return;
        }

        // Snap the player to the nearest tile centre.
        _currentCell = GridManager.Instance.WorldToCell(transform.position);
        transform.position = GridManager.Instance.CellToWorld(_currentCell) + pivotOffset;

        if (showDebugInfo)
        {
            Vector3 cellCenter = GridManager.Instance.CellToWorld(_currentCell);
            Debug.Log($"[Player] Started at cell {_currentCell}, world pos {cellCenter}, final pos {transform.position}, pivot offset {pivotOffset}");
        }
    }

    void Update()
    {
        if (_isMoving || _isDead) return;

        Vector3Int direction = Vector3Int.zero;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            direction = Vector3Int.right;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A))
            direction = Vector3Int.left;
        else if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W))
            direction = Vector3Int.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S))
            direction = Vector3Int.down;

        if (direction != Vector3Int.zero)
            TryMove(direction);
    }

    // ── movement ────────────────────────────────────────────────────────────

    void TryMove(Vector3Int direction)
    {
        Vector3Int targetCell = _currentCell + direction;

        if (showDebugInfo)
        {
            Debug.Log($"[Player] Current: {_currentCell}, Target: {targetCell}, Has Tile: {GridManager.Instance.HasTileAt(targetCell)}");
        }

        if (GridManager.Instance.HasTileAt(targetCell))
        {
            // Valid tile – move there.
            StartCoroutine(MoveToCell(targetCell));
        }
        else
        {
            // No tile – player dies.
            StartCoroutine(Die(targetCell));
        }
    }

    IEnumerator MoveToCell(Vector3Int targetCell)
    {
        _isMoving = true;

        Vector3 startPos = transform.position;
        Vector3 endPos   = GridManager.Instance.CellToWorld(targetCell) + pivotOffset;
        float   elapsed  = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / moveTime);
            yield return null;
        }

        transform.position = endPos;
        _currentCell = targetCell;
        _isMoving    = false;
    }

    IEnumerator Die(Vector3Int fellIntoCell)
    {
        _isDead = true;

        // Move the player toward the empty cell so it looks like they fell.
        Vector3 startPos  = transform.position;
        Vector3 fallTarget = GridManager.Instance.CellToWorld(fellIntoCell) + pivotOffset;
        float   elapsed   = 0f;

        while (elapsed < deathDelay * 0.5f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, fallTarget, elapsed / (deathDelay * 0.5f));
            yield return null;
        }

        yield return new WaitForSeconds(deathDelay * 0.5f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── public helpers ──────────────────────────────────────────────────────

    /// <summary>The cell the player is currently standing on.</summary>
    public Vector3Int CurrentCell => _currentCell;
}
