using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Logo")]
    [SerializeField] private RectTransform logoUI;
    [SerializeField] private float breatheScaleMin = 0.95f;
    [SerializeField] private float breatheScaleMax = 1.05f;
    [SerializeField] private float breatheSpeed = 1.5f;

    [Header("Play Button")]
    [SerializeField] private RectTransform playButton;
    [SerializeField] private float buttonHoverScale = 1.15f;
    [SerializeField] private float buttonScaleSpeed = 10f;

    private Vector3 logoBaseScale;
    private Vector3 buttonBaseScale;
    private bool isHoveringButton = false;

    void Start()
    {
        if (logoUI != null)
            logoBaseScale = logoUI.localScale;

        if (playButton != null)
        {
            buttonBaseScale = playButton.localScale;

            EventTrigger trigger = playButton.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = playButton.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((_) => isHoveringButton = true);
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((_) => isHoveringButton = false);
            trigger.triggers.Add(exitEntry);
        }
    }

    void Update()
    {
        AnimateLogo();
        AnimateButton();
    }

    private void AnimateLogo()
    {
        if (logoUI == null) return;

        float t = (Mathf.Sin(Time.time * breatheSpeed) + 1f) / 2f;
        float scale = Mathf.Lerp(breatheScaleMin, breatheScaleMax, t);
        logoUI.localScale = logoBaseScale * scale;
    }

    private void AnimateButton()
    {
        if (playButton == null) return;

        Vector3 targetScale = isHoveringButton ? buttonBaseScale * buttonHoverScale : buttonBaseScale;
        playButton.localScale = Vector3.Lerp(playButton.localScale, targetScale, Time.deltaTime * buttonScaleSpeed);
    }

    public void OnPlayButtonPressed()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextIndex);
    }
}
