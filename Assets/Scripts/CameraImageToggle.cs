using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any GameObject in the scene (e.g. the Camera itself).
/// Drag the image that lives on the camera into the Inspector slot,
/// then tick/untick "Show Image" per scene.
///
/// Works with any Unity UI Graphic: Image, RawImage, or SpriteRenderer.
/// </summary>
public class CameraImageToggle : MonoBehaviour
{
    [Tooltip("The image component attached to (or parented under) the camera. " +
             "Accepts Image, RawImage, or SpriteRenderer.")]
    [SerializeField] private GameObject cameraImage;

    [Tooltip("Whether the image should be visible in this scene.")]
    [SerializeField] private bool showImage = true;

    void Start()
    {
        Apply();
    }

    // Lets you flip it in the Inspector at runtime and see the change instantly.
    void OnValidate()
    {
        if (cameraImage != null)
            Apply();
    }

    private void Apply()
    {
        cameraImage.SetActive(showImage);
    }
}
