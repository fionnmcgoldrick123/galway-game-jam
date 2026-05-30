using UnityEngine;

/// <summary>
/// Camera that scrolls upward at a set speed and kills the player if it catches them.
///
/// Setup:
///   1. Attach to Main Camera.
///   2. Drag the Player into Target.
///   3. Set Chase Speed per level in the Inspector.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Tooltip("The player to follow and check against.")]
    public Transform target;

    [Tooltip("Offset from the target's starting X. Camera only scrolls upward — X stays centered on player.")]
    public Vector3 offset = Vector3.zero;

    [Header("Chase")]
    [Tooltip("Units per second the camera moves upward. Set per level.")]
    public float chaseSpeed = 1.5f;

    [Tooltip("How many units below the camera bottom edge the player must be to trigger death.")]
    public float killMargin = 0.5f;

    [Tooltip("Half the camera's vertical size in world units. Match your Camera's Size field (default 5).")]
    public float cameraHalfHeight = 5f;

    [Tooltip("Units ahead of the player the camera sits when the player outpaces the chase speed. Lets the player see upcoming tiles.")]
    public float lookAhead = 2f;

    // The bottom edge of the camera in world Y
    public float BottomEdge => transform.position.y - cameraHalfHeight;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = transform.position;

        // Base chase: scroll upward at chase speed
        float chaseY = pos.y + chaseSpeed * Time.deltaTime;

        // Player-lead: if the player is ahead, push camera to keep them in view with look-ahead
        float playerLeadY = target.position.y + lookAhead;

        // Camera Y is whichever is higher — chase floor or player lead
        pos.y = Mathf.Max(chaseY, playerLeadY);

        // Follow player on X axis
        pos.x = target.position.x + offset.x;

        // Keep Z fixed
        pos.z = -10f;

        transform.position = pos;
    }

    /// <summary>Returns true if the player has fallen below the camera's kill line.</summary>
    public bool IsBelowCamera(Vector3 playerPos)
    {
        return playerPos.y < BottomEdge - killMargin;
    }
}
