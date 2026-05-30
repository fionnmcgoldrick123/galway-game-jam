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

    [Header("Win")]
    [Tooltip("Scene to load on win. -1 = auto-load the next scene index.")]
    public int nextSceneIndex = -1;

    [Header("Feature Toggles")]
    [Tooltip("Untick to allow the player to freely revisit tiles without dying.")]
    public bool visitedTileDeathEnabled = true;

    [Tooltip("Untick to stop tiles changing to the visited sprite.")]
    public bool visitedTileVisualEnabled = true;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // ── events ──────────────────────────────────────────────────────────────
    /// <summary>Fired every time the player successfully lands on a new tile.</summary>
    public event System.Action onLanded;

    /// <summary>Fired the moment the player begins moving toward a new tile.</summary>
    public event System.Action onMoveStarted;

    // ── internal state ──────────────────────────────────────────────────────
    private Vector3Int _currentCell;
    private bool       _isMoving;
    private bool       _isDead;
    private bool       _inDialogue;

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

        // Mark starting tile as visited immediately (with full validation).
        if (GridManager.Instance.HasTileAt(_currentCell))
        {
            GridManager.Instance.MarkVisited(_currentCell, visitedTileVisualEnabled);
            Debug.Log($"[Player] Starting cell {_currentCell} marked as visited.");
        }
        else
        {
            Debug.LogWarning($"[Player] WARNING: No tile exists at starting position {_currentCell}. Check LevelGenerator or manual tile placement.");
        }

        if (showDebugInfo)
        {
            Vector3 cellCenter = GridManager.Instance.CellToWorld(_currentCell);
            Debug.Log($"[Player] Started at cell {_currentCell}, world pos {cellCenter}, final pos {transform.position}, pivot offset {pivotOffset}");
        }
    }

    void Update()
    {
        if (_isDead || _inDialogue) return;

        // Check if the camera has caught up to the player
        if (CameraFollow.Instance != null && CameraFollow.Instance.IsBelowCamera(transform.position))
        {
            StartCoroutine(DieFromCamera());
            return;
        }

        if (_isMoving) return;

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

        if (!GridManager.Instance.HasTileAt(targetCell))
        {
            // No tile — fall and die.
            StartCoroutine(Die(targetCell));
            return;
        }

        // Revisiting a tile (that isn't the goal) kills the player.
        if (visitedTileDeathEnabled && targetCell != GridManager.Instance.goalCell && GridManager.Instance.IsVisited(targetCell))
        {
            StartCoroutine(Die(targetCell));
            return;
        }

        // Check for an NPC on the target tile — block movement, pop NPC, start dialogue.
        NPCDialogueTrigger npc = GetNPCAtCell(targetCell);
        if (npc != null)
        {
            StartCoroutine(TriggerNPCDialogue(npc));
            return;
        }

        StartCoroutine(MoveToCell(targetCell));
    }

    IEnumerator TriggerNPCDialogue(NPCDialogueTrigger npc)
    {
        _inDialogue = true;
        yield return npc.Pop();
        if (npc.dialogue != null)
            DialogueManager.Instance.StartDialogue(npc.dialogue);
    }

    /// <summary>Called by DialogueManager when dialogue closes.</summary>
    public void ResumeFromDialogue()
    {
        _inDialogue = false;
    }

    NPCDialogueTrigger GetNPCAtCell(Vector3Int cell)
    {
        Vector3 worldPos = GridManager.Instance.CellToWorld(cell);
        float radius = GridManager.Instance.tilemap.cellSize.x * 0.4f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("NPC"))
                return hit.GetComponent<NPCDialogueTrigger>();
        }
        return null;
    }

    IEnumerator MoveToCell(Vector3Int targetCell)
    {
        _isMoving = true;
        onMoveStarted?.Invoke();

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

        OnLanded(targetCell);
    }

    void OnLanded(Vector3Int cell)
    {
        onLanded?.Invoke();

        if (cell == GridManager.Instance.goalCell)
        {
            if (GridManager.Instance.HasVisitedAll())
                StartCoroutine(Win());
            else
                StartCoroutine(DieNotAllVisited());
        }
        else
        {
            GridManager.Instance.MarkVisited(cell, visitedTileVisualEnabled);
        }
    }

    IEnumerator Win()
    {
        _isDead = true;
        Debug.Log("[Player] Level complete — loading next scene!");
        yield return new WaitForSeconds(deathDelay);
        int target = nextSceneIndex >= 0
            ? nextSceneIndex
            : SceneManager.GetActiveScene().buildIndex + 1;
        if (target < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(target);
        else
            Debug.Log("[Player] You finished the game!");
    }

    IEnumerator DieNotAllVisited()
    {
        _isDead = true;
        Debug.Log("[Player] Reached goal but missed some tiles — restarting.");
        yield return new WaitForSeconds(deathDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator DieFromCamera()
    {
        _isDead = true;
        Debug.Log("[Player] Caught by camera — restarting.");
        yield return new WaitForSeconds(deathDelay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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
