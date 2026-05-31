using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerDancing : MonoBehaviour
{
    [Header("Poses")]
    [Tooltip("Sprites to pick from each time the player lands on a tile.")]
    public Sprite[] poseSprites;

    [Header("Pop")]
    [Tooltip("How much the player scales up at the peak of the pop.")]
    public float popScaleMultiplier = 1.25f;

    [Tooltip("Total duration of the pop animation (scale up + scale down).")]
    public float popDuration = 0.18f;

    [Header("Idle")]
    [Tooltip("Seconds of no movement before the idle animator state is triggered.")]
    public float idleDelay = 2f;

    [Tooltip("Name of the Animator bool parameter to set when idle. Leave blank to ignore.")]
    public string idleAnimatorParam = "Idle";


    private SpriteRenderer   _sr;
    private Animator         _animator;
    private PlayerController _controller;

    private int       _lastPoseIndex = -1;
    private Vector3   _baseScale;
    private Coroutine _popCoroutine;
    private Coroutine _idleCoroutine;


    void Awake()
    {
        _sr         = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        _animator   = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        _controller = GetComponent<PlayerController>();
        _baseScale  = transform.localScale;
    }

    void OnEnable()
    {
        _controller.onLanded      += HandleLanded;
        _controller.onMoveStarted += HandleMoveStarted;
    }

    void OnDisable()
    {
        _controller.onLanded      -= HandleLanded;
        _controller.onMoveStarted -= HandleMoveStarted;
    }

    void HandleLanded()
    {
        if (_animator != null) _animator.enabled = false;

        SetRandomPose();
        TriggerPop();

        if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
        _idleCoroutine = StartCoroutine(IdleTimerCoroutine());
    }

    void HandleMoveStarted()
    {
        if (_idleCoroutine != null)
        {
            StopCoroutine(_idleCoroutine);
            _idleCoroutine = null;
        }
    }


    void SetRandomPose()
    {
        if (_sr == null || poseSprites == null || poseSprites.Length == 0) return;

        int newIndex;
        if (poseSprites.Length == 1)
        {
            newIndex = 0;
        }
        else
        {
            do { newIndex = Random.Range(0, poseSprites.Length); }
            while (newIndex == _lastPoseIndex);
        }

        _lastPoseIndex = newIndex;
        _sr.sprite     = poseSprites[newIndex];
    }


    void TriggerPop()
    {
        if (_popCoroutine != null) StopCoroutine(_popCoroutine);
        _popCoroutine = StartCoroutine(PopCoroutine());
    }

    IEnumerator PopCoroutine()
    {
        Vector3 bigScale = _baseScale * popScaleMultiplier;
        float   half     = popDuration * 0.5f;
        float   elapsed  = 0f;

        while (elapsed < half)
        {
            elapsed              += Time.deltaTime;
            transform.localScale  = Vector3.Lerp(_baseScale, bigScale, elapsed / half);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < half)
        {
            elapsed              += Time.deltaTime;
            transform.localScale  = Vector3.Lerp(bigScale, _baseScale, elapsed / half);
            yield return null;
        }

        transform.localScale = _baseScale;
        _popCoroutine        = null;
    }


    IEnumerator IdleTimerCoroutine()
    {
        yield return new WaitForSeconds(idleDelay);

        if (_animator != null)
        {
            _animator.enabled = true;
            if (!string.IsNullOrEmpty(idleAnimatorParam))
                _animator.SetBool(idleAnimatorParam, true);
        }

        _idleCoroutine = null;
    }
}
