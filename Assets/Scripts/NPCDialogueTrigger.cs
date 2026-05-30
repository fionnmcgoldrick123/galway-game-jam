using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to any NPC GameObject.
/// Tag the NPC with "NPC" and give it a Collider2D set to Is Trigger.
/// Assign a DialogueSequence ScriptableObject in the Inspector.
/// </summary>
public class NPCDialogueTrigger : MonoBehaviour
{
    [Tooltip("The dialogue to play when the player tries to enter this NPC's tile.")]
    public DialogueSequence dialogue;

    [Header("Pop Animation")]
    [Tooltip("How much the NPC scales up during the pop.")]
    public float popScale = 1.4f;

    [Tooltip("Duration of the scale-up phase in seconds.")]
    public float popUpDuration = 0.1f;

    [Tooltip("Duration of the scale-back-down phase in seconds.")]
    public float popDownDuration = 0.12f;

    private Vector3 _originalScale;

    void Awake()
    {
        _originalScale = transform.localScale;
    }

    /// <summary>Scales up then back down. Awaitable by PlayerController.</summary>
    public IEnumerator Pop()
    {
        Vector3 bigScale = _originalScale * popScale;
        float elapsed = 0f;

        // Scale up
        while (elapsed < popUpDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(_originalScale, bigScale, elapsed / popUpDuration);
            yield return null;
        }
        transform.localScale = bigScale;

        elapsed = 0f;

        // Scale back down
        while (elapsed < popDownDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(bigScale, _originalScale, elapsed / popDownDuration);
            yield return null;
        }
        transform.localScale = _originalScale;
    }
}
