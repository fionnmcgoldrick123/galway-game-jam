using UnityEngine;

/// <summary>
/// A single line of NPC dialogue.
/// Create via: Right-click Project → Create → Dialogue → Dialogue Line
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueLine", menuName = "Dialogue/Dialogue Line")]
public class DialogueLine : ScriptableObject
{
    [Header("Text")]
    [TextArea(2, 6)]
    [Tooltip("The text displayed on screen.")]
    public string text = "Hello, traveller!";

    [Tooltip("Characters per second for the typewriter effect. Higher = faster.")]
    [Range(1f, 100f)]
    public float textSpeed = 30f;

    [Header("Audio")]
    [Tooltip("Optional sound clip to play when this line starts.")]
    public AudioClip sound;

    [Range(0f, 1f)]
    [Tooltip("Volume of the sound clip.")]
    public float volume = 1f;
}
