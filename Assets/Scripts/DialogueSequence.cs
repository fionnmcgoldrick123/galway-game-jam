using UnityEngine;

/// <summary>
/// One ScriptableObject holds an entire conversation.
/// Create via: Right-click Project → Create → Dialogue → Dialogue Sequence
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [Tooltip("NPC display name shown above the text box. Leave empty to hide.")]
    public string speakerName = "???";

    [Tooltip("Each entry is one screen of text. Player presses any key to advance.")]
    [TextArea(2, 6)]
    public string[] lines;

    [Header("Typewriter")]
    [Tooltip("Characters per second. Higher = faster.")]
    [Range(1f, 100f)]
    public float textSpeed = 30f;

    [Header("Audio")]
    [Tooltip("Optional sound played when the first line starts.")]
    public AudioClip sound;

    [Range(0f, 1f)]
    public float volume = 1f;
}
