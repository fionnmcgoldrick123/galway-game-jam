using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the dialogue UI and typewriter effect.
///
/// Setup:
///   1. Create a Canvas with a panel (the dialogue box).
///   2. Add a TextMeshProUGUI for the speaker name and one for the body text.
///   3. Attach this script to a persistent GameObject (e.g. DialogueManager).
///   4. Assign the references in the Inspector.
///   5. The panel's GameObject is toggled on/off automatically.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The root panel GameObject that holds the dialogue box.")]
    public GameObject dialoguePanel;

    [Tooltip("TextMeshPro component that shows the speaker name.")]
    public TextMeshProUGUI speakerText;

    [Tooltip("TextMeshPro component that shows the line body.")]
    public TextMeshProUGUI bodyText;

    [Tooltip("Optional indicator shown when player can press to advance.")]
    public GameObject continuePrompt;

    [Tooltip("Full-screen dark overlay panel. Activated only during dialogue.")]
    public GameObject darkOverlay;

    [Header("Pop Animation")]
    [Tooltip("Duration of the scale pop-in and pop-out.")]
    public float popDuration = 0.15f;

    [Header("Audio")]
    private AudioSource _audioSource;

    // ── state ────────────────────────────────────────────────────────────────
    private DialogueSequence _sequence;
    private int              _lineIndex;
    private bool             _isTyping;
    private bool             _skipRequested;
    private bool             _inputCooldown;

    public bool IsOpen { get; private set; }

    /// <summary>Invoked once when the dialogue panel finishes closing. Auto-cleared after each use.</summary>
    public event System.Action onClosed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (continuePrompt) continuePrompt.SetActive(false);
        if (darkOverlay) darkOverlay.SetActive(false);

        // Prevent TMP from wrapping text onto new lines.
        if (bodyText != null)
        {
            bodyText.overflowMode       = TextOverflowModes.Overflow;
            bodyText.enableWordWrapping = false;
        }
        if (speakerText != null)
        {
            speakerText.overflowMode       = TextOverflowModes.Overflow;
            speakerText.enableWordWrapping = false;
        }
    }

    void Update()
    {
        if (!IsOpen || _inputCooldown) return;

        if (Input.anyKeyDown)
        {
            if (_isTyping)
                _skipRequested = true;
            else
                AdvanceLine();
        }
    }

    // ── public API ────────────────────────────────────────────────────────────

    public void StartDialogue(DialogueSequence sequence)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
        {
            Debug.LogError("[DialogueManager] StartDialogue called with null or empty sequence!");
            return;
        }

        if (bodyText == null)
        {
            Debug.LogError("[DialogueManager] Body Text is not assigned! Assign it in the Inspector.");
            return;
        }

        _sequence    = sequence;
        _lineIndex   = 0;
        IsOpen       = true;

        if (darkOverlay) darkOverlay.SetActive(true);
        if (continuePrompt) continuePrompt.SetActive(false);

        if (sequence.sound != null)
        {
            _audioSource.volume = sequence.volume;
            _audioSource.PlayOneShot(sequence.sound);
        }

        StartCoroutine(PopIn());
        StartCoroutine(ShowLineWithCooldown(0));
    }

    // ── internals ──────────────────────────────────────────────────────────────

    IEnumerator PopIn()
    {
        if (dialoguePanel == null) yield break;
        dialoguePanel.SetActive(true);

        RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / popDuration;
            rt.localScale = Vector3.one * EaseOutBack(t);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator PopOut(System.Action onDone)
    {
        if (dialoguePanel == null) { onDone?.Invoke(); yield break; }

        RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
        if (rt == null) { onDone?.Invoke(); yield break; }

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / popDuration;
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0f, EaseInBack(t));
            yield return null;
        }
        rt.localScale = Vector3.one;
        dialoguePanel.SetActive(false);
        onDone?.Invoke();
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    static float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

    // Wait one frame so the key that triggered dialogue doesn't immediately advance it
    IEnumerator ShowLineWithCooldown(int index)
    {
        _inputCooldown = true;
        yield return null;
        _inputCooldown = false;
        ShowLine(index);
    }

    void ShowLine(int index)
    {
        if (speakerText != null)
            speakerText.text = _sequence.speakerName;

        StartCoroutine(TypeLine(_sequence.lines[index], _sequence.textSpeed));
    }

    IEnumerator TypeLine(string text, float speed)
    {
        _isTyping      = true;
        _skipRequested = false;

        if (continuePrompt) continuePrompt.SetActive(false);
        bodyText.text = string.Empty;

        float delay = speed > 0 ? 1f / speed : 0f;

        foreach (char c in text)
        {
            if (_skipRequested)
            {
                bodyText.text = text;
                break;
            }
            bodyText.text += c;
            yield return new WaitForSeconds(delay);
        }

        _isTyping = false;
        if (continuePrompt) continuePrompt.SetActive(true);
    }

    void AdvanceLine()
    {
        _lineIndex++;

        if (_lineIndex < _sequence.lines.Length)
        {
            ShowLine(_lineIndex);
        }
        else
        {
            CloseDialogue();
        }
    }

    void CloseDialogue()
    {
        StopAllCoroutines();
        IsOpen = false;
        if (continuePrompt) continuePrompt.SetActive(false);
        if (darkOverlay) darkOverlay.SetActive(false);

        // Capture and clear before invoking so re-entrant calls work cleanly.
        System.Action callback = onClosed;
        onClosed = null;

        StartCoroutine(PopOut(() =>
        {
            callback?.Invoke();
            StartCoroutine(ResumeNextFrame());
        }));
    }

    IEnumerator ResumeNextFrame()
    {
        yield return null;
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.ResumeFromDialogue();
    }
}
