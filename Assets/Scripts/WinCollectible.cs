using UnityEngine;

public class WinCollectible : MonoBehaviour
{
    [Tooltip("Scene to load after dialogue ends. -1 = auto next in build order.")]
    public int nextSceneIndex = -1;

    private DialogueSequence _dialogue;
    private SpriteRenderer _sr;

    public DialogueSequence Dialogue => _dialogue;

    void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Apply(CollectibleItemData data)
    {
        if (data == null) return;
        _dialogue = data.dialogue;
        if (_sr != null && data.sprite != null)
            _sr.sprite = data.sprite;
    }

    public void Collect()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
    }
}
