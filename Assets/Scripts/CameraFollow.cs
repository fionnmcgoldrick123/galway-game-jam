using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Tooltip("The player to follow and check against.")]
    public Transform target;

    [Tooltip("Offset from the target's starting X. Camera only scrolls upward — X stays centered on player.")]
    public Vector3 offset = Vector3.zero;

    [Header("Chase")]
    [Tooltip("Untick to disable the scrolling chase — camera stays fixed on player.")]
    public bool chaseEnabled = true;

    [Tooltip("Units per second the camera moves upward. Set per level.")]
    public float chaseSpeed = 1.5f;

    [Tooltip("How many units below the camera bottom edge the player must be to trigger death.")]
    public float killMargin = 0.5f;

    [Tooltip("Half the camera\u2019s vertical size in world units. Match your Camera\u2019s Size field (default 5).")]
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

        if (chaseEnabled)
        {
            float chaseY = pos.y + chaseSpeed * Time.deltaTime;
            float playerLeadY = target.position.y + lookAhead;
            pos.y = Mathf.Max(chaseY, playerLeadY);
        }
        else
        {
            pos.y = target.position.y + lookAhead;
        }

        pos.x = target.position.x + offset.x;
        pos.z = -10f;

        transform.position = pos;
    }

    public bool IsBelowCamera(Vector3 playerPos)
    {
        // Never kill from camera if chase is disabled
        if (!chaseEnabled) return false;
        return playerPos.y < BottomEdge - killMargin;
    }

    public void Freeze()
    {
        chaseEnabled = false;
        enabled      = false;
    }
}
