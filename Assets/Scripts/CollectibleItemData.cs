using UnityEngine;

[CreateAssetMenu(fileName = "NewCollectibleItem", menuName = "Collectibles/Collectible Item")]
public class CollectibleItemData : ScriptableObject
{
    [Tooltip("The sprite shown on the tile for this item.")]
    public Sprite sprite;

    [Tooltip("Dialogue the player says when they pick this item up.")]
    public DialogueSequence dialogue;
}
