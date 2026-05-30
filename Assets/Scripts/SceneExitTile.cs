using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Place this on any GameObject with a Collider2D to create a special tile
/// that sends the player to the next scene when they land on it.
///
/// Setup:
///   1. Create an empty GameObject positioned at the tile centre.
///   2. Add a Collider2D (e.g. BoxCollider2D, set Is Trigger = true).
///   3. Attach this component.
///   4. Optionally set Next Scene Index (-1 = auto next in build order).
/// </summary>
public class SceneExitTile : MonoBehaviour
{
    [Tooltip("Scene to load. -1 = auto-load the next scene index in build order.")]
    public int nextSceneIndex = -1;
}
