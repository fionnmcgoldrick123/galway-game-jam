using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent singleton that manages which collectible item appears each level.
/// Items are shuffled randomly at startup. No item repeats until all have been seen,
/// then the list reshuffles and repeats.
///
/// Setup:
///   1. Create an empty GameObject in your first scene, attach this component.
///   2. Tick "Don't Destroy On Load" (handled automatically).
///   3. Populate the Items array with your CollectibleItemData assets.
/// </summary>
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

    /// <summary>Returns the next item in the shuffled deck. Never repeats until all are used.</summary>
    public CollectibleItemData GetNext()
    {
        if (_deck.Count == 0)
            BuildDeck(); // All used — reshuffle.

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
        {
            Debug.LogWarning("[CollectibleManager] No items assigned!");
            return;
        }

        foreach (var item in items)
            if (item != null) _deck.Add(item);

        // Fisher-Yates shuffle.
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }
}
