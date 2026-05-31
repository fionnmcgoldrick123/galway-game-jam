using System.Collections;
using UnityEngine;

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

    public IEnumerator Pop()
    {
        Vector3 bigScale = _originalScale * popScale;
        float elapsed = 0f;

        while (elapsed < popUpDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(_originalScale, bigScale, elapsed / popUpDuration);
            yield return null;
        }
        transform.localScale = bigScale;

        elapsed = 0f;

        while (elapsed < popDownDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(bigScale, _originalScale, elapsed / popDownDuration);
            yield return null;
        }
        transform.localScale = _originalScale;
    }
}
