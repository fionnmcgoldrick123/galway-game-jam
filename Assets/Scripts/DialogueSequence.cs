using UnityEngine;

/// <summary>
/// A collection of dialogue lines for an NPC.
/// Create via: Right-click Project → Create → Dialogue → Dialogue Sequence
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [Tooltip("NPC display name shown above the text box. Leave empty to hide.")]
    public string speakerName = "???";

    [Tooltip("The lines of dialogue spoken in order.")]
    public DialogueLine[] lines;
}
