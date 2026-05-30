using UnityEngine;

/// <summary>
/// Follows the player with a fixed Z position.
///
/// Setup:
///   1. Add this component to the Main Camera.
///   2. Drag the Player GameObject into the Target field in the Inspector.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("The player or object to follow.")]
    public Transform target;

    [Tooltip("Offset from the target position (e.g., slightly above or to the side).")]
    public Vector3 offset = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // Follow the target on X and Y, keep Z fixed
        Vector3 newPos = target.position + offset;
        newPos.z = -10f; // Standard 2D camera Z position
        transform.position = newPos;
    }
}
