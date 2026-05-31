using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public event System.Action onLanded;
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


    void Start()
    {
        _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        _trail    = GetComponentInChildren<TrailRenderer>();

        ParticleSystem ps = GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
            ps.gameObject.SetActive(false);

        if (GridManager.Instance == null)
            return;

        _currentCell = GridManager.Instance.WorldToCell(transform.position);
        _startingCell = _currentCell;
        transform.position = GridManager.Instance.CellToWorld(_currentCell) + pivotOffset;

        if (GridManager.Instance.HasTileAt(_currentCell))
            GridManager.Instance.MarkVisited(_currentCell, visitedTileVisualEnabled);
    }

    void Update()
    {
        if (_isDead || _inDialogue || _cutsceneLocked) return;

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

    void TryMove(Vector3Int direction)
    {
        Vector3Int targetCell = _currentCell + direction;

        if (!GridManager.Instance.HasTileAt(targetCell))
        {
            StartCoroutine(Die(targetCell));
            return;
        }

        if (visitedTileDeathEnabled && targetCell != GridManager.Instance.goalCell && GridManager.Instance.IsVisited(targetCell))
        {
            StartCoroutine(Die(targetCell));
            return;
        }

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

    public void ResumeFromDialogue()
    {
        if (_cutsceneLocked) return;
        _inDialogue = false;
    }

    public void LockInput()
    {
        _cutsceneLocked = true;
        _inDialogue     = true;
    }

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

        GridManager.Instance.FlashCell(cell);

        if (cell == _startingCell)
        {
            GridManager.Instance.MarkVisited(cell, visitedTileVisualEnabled);
            return;
        }

        SceneExitTile exit = GetExitTileAtCell(cell);
        if (exit != null)
        {
            StartCoroutine(LoadExitScene(exit));
            return;
        }

        WinCollectible collectible = GetCollectibleAtCell(cell);
        if (collectible != null)
        {
            StartCoroutine(WinWithCollectible(collectible));
            return;
        }

        if (cell == GridManager.Instance.goalCell)
        {
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
            yield return StartCoroutine(PlayDeathThenReload());
            yield break;
        }

        collectible.Collect();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();

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

        if (collectible.Dialogue != null && DialogueManager.Instance != null)
        {
            bool dialogueDone = false;
            DialogueManager.Instance.onClosed += () => dialogueDone = true;
            _inDialogue = true;
            DialogueManager.Instance.StartDialogue(collectible.Dialogue);
            yield return new WaitUntil(() => dialogueDone);
        }

        int target = collectible.nextSceneIndex >= 0
            ? collectible.nextSceneIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        if (target < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(target);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
    }

    IEnumerator Win()
    {
        _isDead = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
        if (CameraFollow.Instance != null) CameraFollow.Instance.Freeze();

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
    }

    IEnumerator DieNotAllVisited()
    {
        _isDead = true;
        yield return StartCoroutine(PlayDeathThenReload());
    }

    IEnumerator DieFromCamera()
    {
        _isDead   = true;
        _isMoving = false;
        StopAllCoroutines();
        ClearTrail();
        StartCoroutine(PlayDeathThenReload());
        yield break;
    }

    IEnumerator Die(Vector3Int fellIntoCell)
    {
        _isDead = true;
        ClearTrail();

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

    IEnumerator PlayDeathThenReload()
    {
        ClearTrail();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeath();
        if (CameraFollow.Instance != null) CameraFollow.Instance.Freeze();

        if (_animator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            _animator.enabled = true;
            _animator.SetTrigger(deathTrigger);
            float waited = 0f;
            while (waited < deathDelay + 2f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
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

    public void OnDeathAnimationEnd()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    public Vector3Int CurrentCell => _currentCell;
}
