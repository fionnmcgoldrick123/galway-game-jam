using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpeningCutscene : MonoBehaviour
{
    [Header("Cutscene Beats")]
    [Tooltip("Each beat plays dialogue, then hides/moves characters before the next beat.")]
    [SerializeField] private CutsceneBeat[] beats;

    [Header("Character Movement")]
    [Tooltip("Seconds it takes a character to slide right one grid cell.")]
    [SerializeField] private float characterMoveTime = 0.4f;

    [Tooltip("Seconds to wait after movement completes before the next dialogue starts.")]
    [SerializeField] private float pauseBetweenBeats = 0.3f;

    [Header("Character Hide")]
    [Tooltip("If true, the hidden character fades out instead of snapping off.")]
    [SerializeField] private bool fadeOutCharacter = true;

    [Tooltip("Duration of the fade-out in seconds.")]
    [SerializeField] private float fadeOutDuration = 0.4f;

    private PlayerController _player;

    void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicMuted(true);
        StartCoroutine(PlayCutscene());
    }

    // ── main sequence ────────────────────────────────────────────────────────

    private IEnumerator PlayCutscene()
    {
        if (_player != null)
            _player.LockInput();

        foreach (CutsceneBeat beat in beats)
        {
            if (beat.dialogue != null)
            {
                bool dialogueDone = false;
                DialogueManager.Instance.onClosed += () => dialogueDone = true;
                DialogueManager.Instance.StartDialogue(beat.dialogue);
                yield return new WaitUntil(() => dialogueDone);
            }

            if (beat.characterToHide != null)
            {
                if (fadeOutCharacter)
                    yield return StartCoroutine(FadeOutCharacter(beat.characterToHide, fadeOutDuration));
                else
                    beat.characterToHide.SetActive(false);
            }

            if (beat.charactersToMoveUp != null && beat.charactersToMoveUp.Length > 0)
            {
                float gridStep = GetGridCellWidth();
                Coroutine[] moves = new Coroutine[beat.charactersToMoveUp.Length];

                for (int i = 0; i < beat.charactersToMoveUp.Length; i++)
                {
                    if (beat.charactersToMoveUp[i] != null)
                        moves[i] = StartCoroutine(MoveRight(beat.charactersToMoveUp[i], gridStep));
                }

                foreach (Coroutine move in moves)
                    if (move != null)
                        yield return move;
            }

            if (pauseBetweenBeats > 0f)
                yield return new WaitForSeconds(pauseBetweenBeats);
        }

        if (_player != null)
            _player.UnlockCutscene();

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicMuted(false);

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextIndex);
    }


    private float GetGridCellWidth()
    {
        if (GridManager.Instance != null)
            return GridManager.Instance.tilemap.cellSize.x;

        return 1f;
    }

    private IEnumerator MoveRight(GameObject character, float distance)
    {
        Vector3 startPos = character.transform.position;
        Vector3 endPos   = startPos + Vector3.right * distance;
        float   elapsed  = 0f;

        while (elapsed < characterMoveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / characterMoveTime;
            character.transform.position = Vector3.Lerp(startPos, endPos, EaseOutQuad(t));
            yield return null;
        }

        character.transform.position = endPos;
    }

    private IEnumerator FadeOutCharacter(GameObject character, float duration)
    {
        SpriteRenderer sr = character.GetComponentInChildren<SpriteRenderer>();

        if (sr == null)
        {
            character.SetActive(false);
            yield break;
        }

        Color startColour = sr.color;
        Color endColour   = new Color(startColour.r, startColour.g, startColour.b, 0f);
        float elapsed     = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            sr.color  = Color.Lerp(startColour, endColour, elapsed / duration);
            yield return null;
        }

        character.SetActive(false);
        // Reset alpha so the object is clean if re-enabled later.
        sr.color = startColour;
    }

    private static float EaseOutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}

// ── data ──────────────────────────────────────────────────────────────────────

[System.Serializable]
public class CutsceneBeat
{
    [Tooltip("The conversation that plays during this beat. Leave null to skip straight to the character actions.")]
    public DialogueSequence dialogue;

    [Tooltip("This character disappears after the dialogue finishes. Leave null for no hide.")]
    public GameObject characterToHide;

    [Tooltip("These characters each slide right one grid cell after the dialogue finishes. Leave empty for none.")]
    public GameObject[] charactersToMoveUp;
}
