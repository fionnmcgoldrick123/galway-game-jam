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

    [Header("Audio")]
    private AudioSource _audioSource;

    // ── state ────────────────────────────────────────────────────────────────
    private DialogueSequence _sequence;
    private int              _lineIndex;
    private bool             _isTyping;
    private bool             _skipRequested;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (continuePrompt) continuePrompt.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
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

        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (continuePrompt) continuePrompt.SetActive(false);

        ShowLine(_sequence.lines[0]);
    }

    // ── internals ─────────────────────────────────────────────────────────────

    void ShowLine(DialogueLine line)
    {
        if (speakerText != null)
            speakerText.text = _sequence.speakerName;
        else
            Debug.LogWarning("[DialogueManager] Speaker Text is not assigned in Inspector!");

        if (line.sound != null)
        {
            _audioSource.volume = line.volume;
            _audioSource.PlayOneShot(line.sound);
        }

        StartCoroutine(TypeLine(line));
    }

    IEnumerator TypeLine(DialogueLine line)
    {
        _isTyping      = true;
        _skipRequested = false;

        if (continuePrompt) continuePrompt.SetActive(false);

        if (bodyText == null)
        {
            Debug.LogError("[DialogueManager] Body Text is null! Cannot display dialogue.");
            yield break;
        }

        bodyText.text = string.Empty;
        float delay = line.textSpeed > 0 ? 1f / line.textSpeed : 0f;

        foreach (char c in line.text)
        {
            if (_skipRequested)
            {
                bodyText.text = line.text;
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
            ShowLine(_sequence.lines[_lineIndex]);
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
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (continuePrompt) continuePrompt.SetActive(false);

        // Resume camera and player
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.ResumeFromDialogue();
    }
}
