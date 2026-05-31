using System.Collections.Generic;
using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Tooltip("All collectible items available in the game. Order doesn't matter — they are shuffled randomly.")]
    public CollectibleItemData[] items;

    private List<CollectibleItemData> _deck  = new List<CollectibleItemData>();
    private List<CollectibleItemData> _used  = new List<CollectibleItemData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDeck();
    }

    public CollectibleItemData GetNext()
    {
        if (_deck.Count == 0)
            BuildDeck();

        int idx  = Random.Range(0, _deck.Count);
        CollectibleItemData item = _deck[idx];
        _deck.RemoveAt(idx);
        _used.Add(item);
        return item;
    }

    void BuildDeck()
    {
        _deck.Clear();
        _used.Clear();

        if (items == null || items.Length == 0)
            return;

        foreach (var item in items)
            if (item != null) _deck.Add(item);

        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }
}
