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
    [Tooltip("Brief pause before the scene is reloaded after the player dies. Only used if no Animator or trigger name is set.")]
    public float deathDelay = 0.6f;

    [Tooltip("Animator Trigger parameter name for the death animation. Leave blank to skip.")]
    public string deathTrigger = "Die";

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
    private Vector3Int   _currentCell;
    private bool         _isMoving;
    private bool         _isDead;
    private bool         _inDialogue;
    private bool         _cutsceneLocked;
    private Animator     _animator;
    private TrailRenderer _trail;
    private Vector3Int   _startingCell;

    // ── Unity lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        _trail    = GetComponentInChildren<TrailRenderer>();

        if (GridManager.Instance == null)
        {
            Debug.LogError("[PlayerController] GridManager not found in scene!");
            return;
        }

        // Snap the player to the nearest tile centre.
        _currentCell = GridManager.Instance.WorldToCell(transform.position);
        _startingCell = _currentCell;  // Remember where we started
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
        if (_isDead || _inDialogue || _cutsceneLocked) return;

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

    /// <summary>Called by DialogueManager when dialogue closes. Ignored while a cutscene is running.</summary>
    public void ResumeFromDialogue()
    {
        if (_cutsceneLocked) return;
        _inDialogue = false;
    }

    /// <summary>Called by OpeningCutscene to hard-lock input for the full cutscene duration.</summary>
    public void LockInput()
    {
        _cutsceneLocked = true;
        _inDialogue     = true;
    }

    /// <summary>Called by OpeningCutscene when the cutscene finishes to restore full control.</summary>
    public void UnlockCutscene()
    {
        _cutsceneLocked = false;
        _inDialogue     = false;
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

    SceneExitTile GetExitTileAtCell(Vector3Int cell)
    {
        Vector3 worldPos = GridManager.Instance.CellToWorld(cell);
        float radius = GridManager.Instance.tilemap.cellSize.x * 0.4f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, radius);
        foreach (var hit in hits)
        {
            SceneExitTile exit = hit.GetComponent<SceneExitTile>();
            if (exit != null) return exit;
        }
        return null;
    }

    WinCollectible GetCollectibleAtCell(Vector3Int cell)
    {
        Vector3 worldPos = GridManager.Instance.CellToWorld(cell);
        float radius = GridManager.Instance.tilemap.cellSize.x * 0.4f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, radius);
        foreach (var hit in hits)
        {
            WinCollectible c = hit.GetComponent<WinCollectible>();
            if (c != null) return c;
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();

        // Flash the tile white on landing.
        GridManager.Instance.FlashCell(cell);

        // Never trigger exit/win logic on the starting cell.
        if (cell == _startingCell)
        {
            GridManager.Instance.MarkVisited(cell, visitedTileVisualEnabled);
            return;
        }

        // Check for a scene-exit tile first.
        SceneExitTile exit = GetExitTileAtCell(cell);
        if (exit != null)
        {
            StartCoroutine(LoadExitScene(exit));
            return;
        }

        // Check for a win collectible.
        WinCollectible collectible = GetCollectibleAtCell(cell);
        if (collectible != null)
        {
            StartCoroutine(WinWithCollectible(collectible));
            return;
        }

        if (cell == GridManager.Instance.goalCell)
        {
            // Only enforce "visit all tiles" rule when death is enabled.
            if (!visitedTileDeathEnabled || GridManager.Instance.HasVisitedAll())
                StartCoroutine(Win());
            else
                StartCoroutine(DieNotAllVisited());
        }
        else
        {
            GridManager.Instance.MarkVisited(cell, visitedTileVisualEnabled);
        }
    }

    IEnumerator WinWithCollectible(WinCollectible collectible)
    {
        _isDead = true;
        if (CameraFollow.Instance != null) CameraFollow.Instance.Freeze();

        // If the player hasn't visited all tiles yet, die instead of winning.
        if (visitedTileDeathEnabled && !GridManager.Instance.HasVisitedAll())
        {
            collectible.Collect();
            Debug.Log("[Player] Collected item before visiting all tiles — dying.");
            yield return StartCoroutine(PlayDeathThenReload());
            yield break;
        }

        // Collectible disappears immediately.
        collectible.Collect();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();

        // Play victory particles, wait for the burst duration, then stop emitting.
        // Using duration instead of IsAlive() so looping systems don't hang the coroutine.
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            ps.gameObject.SetActive(true);
            ps.Play();
            yield return new WaitForSeconds(ps.main.duration);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        else
        {
            yield return new WaitForSeconds(deathDelay);
        }

        // Play the win dialogue and wait for it to close.
        Debug.Log($"[PlayerController] WinWithCollectible: Dialogue={(collectible.Dialogue == null ? "NULL" : "OK")}, DialogueManager={(DialogueManager.Instance == null ? "NULL" : "OK")}");
        if (collectible.Dialogue != null && DialogueManager.Instance != null)
        {
            bool dialogueDone = false;
            DialogueManager.Instance.onClosed += () => dialogueDone = true;
            _inDialogue = true;
            DialogueManager.Instance.StartDialogue(collectible.Dialogue);
            yield return new WaitUntil(() => dialogueDone);
        }

        // Load next scene.
        int target = collectible.nextSceneIndex >= 0
            ? collectible.nextSceneIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        if (target < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(target);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // no next scene — repeat level
    }

    IEnumerator LoadExitScene(SceneExitTile exit)
    {
        _isDead = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
        if (CameraFollow.Instance != null) CameraFollow.Instance.Freeze();

        // Play victory particles if present.
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            ps.gameObject.SetActive(true);
            ps.Play();
            yield return new WaitWhile(() => ps.IsAlive(true));
        }
        else
        {
            yield return new WaitForSeconds(deathDelay);
        }

        int target = exit.nextSceneIndex >= 0
            ? exit.nextSceneIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        if (target < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(target);
        else
            Debug.Log("[Player] No next scene in build — exit tile reached the end.");
    }

    IEnumerator Win()
    {
        _isDead = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
        if (CameraFollow.Instance != null) CameraFollow.Instance.Freeze();
        Debug.Log("[Player] Level complete — loading next scene!");

        // Play victory particle system (child of player) and wait for it to finish.
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            ps.gameObject.SetActive(true);
            ps.Play();
            yield return new WaitWhile(() => ps.IsAlive(true));
        }
        else
        {
            yield return new WaitForSeconds(deathDelay);
        }

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
        yield return StartCoroutine(PlayDeathThenReload());
    }

    IEnumerator DieFromCamera()
    {
        _isDead   = true;
        _isMoving = false;
        StopAllCoroutines();
        ClearTrail();
        Debug.Log("[Player] Caught by camera — restarting.");
        StartCoroutine(PlayDeathThenReload());
        yield break;
    }

    IEnumerator Die(Vector3Int fellIntoCell)
    {
        _isDead = true;
        ClearTrail();

        // Slide the player toward the empty cell so it looks like they fell.
        Vector3 startPos   = transform.position;
        Vector3 fallTarget = GridManager.Instance.CellToWorld(fellIntoCell) + pivotOffset;
        float   elapsed    = 0f;
        float   slideTime  = deathDelay * 0.4f;

        while (elapsed < slideTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, fallTarget, elapsed / slideTime);
            yield return null;
        }

        yield return StartCoroutine(PlayDeathThenReload());
    }

    /// <summary>
    /// Freezes the camera, re-enables the Animator, and fires the death trigger.
    /// Scene reload is driven by the animation event on the last frame — see OnDeathAnimationEnd().
    /// Falls back to deathDelay seconds if no Animator / trigger is set.
    /// </summary>
    IEnumerator PlayDeathThenReload()
    {
        ClearTrail();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeath();
        if (CameraFollow.Instance != null) CameraFollow.Instance.Freeze();

        if (_animator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            // Re-enable in case PlayerDancing disabled it.
            _animator.enabled = true;
            _animator.SetTrigger(deathTrigger);
            // Wait for animation event, but cap with a timeout so we never hang.
            float waited = 0f;
            while (waited < deathDelay + 2f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            // If we reach here the animation event never fired — reload anyway.
        }
        else
        {
            yield return new WaitForSeconds(deathDelay);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ClearTrail()
    {
        if (_trail == null) return;
        _trail.emitting = false;
        _trail.Clear();
    }

    /// <summary>
    /// Called by the Animation Event on the last frame of the death clip.
    /// Reloads the current scene.
    /// </summary>
    public void OnDeathAnimationEnd()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── public helpers ──────────────────────────────────────────────────────

    /// <summary>The cell the player is currently standing on.</summary>
    public Vector3Int CurrentCell => _currentCell;
}
