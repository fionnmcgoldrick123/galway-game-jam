using UnityEngine;

/// <summary>
/// Defines one collectible item: its sprite and the dialogue the player says on pickup.
/// Create via: Right-click Project → Create → Collectibles → Collectible Item
/// </summary>
[CreateAssetMenu(fileName = "NewCollectibleItem", menuName = "Collectibles/Collectible Item")]
public class CollectibleItemData : ScriptableObject
{
    [Tooltip("The sprite shown on the tile for this item.")]
    public Sprite sprite;

    [Tooltip("Dialogue the player says when they pick this item up.")]
    public DialogueSequence dialogue;
}
