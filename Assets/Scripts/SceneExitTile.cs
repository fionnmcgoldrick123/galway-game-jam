using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneExitTile : MonoBehaviour
{
    [Tooltip("Scene to load. -1 = auto-load the next scene index in build order.")]
    public int nextSceneIndex = -1;
}
