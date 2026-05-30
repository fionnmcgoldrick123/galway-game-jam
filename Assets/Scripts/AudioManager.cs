using UnityEngine;

/// <summary>
/// Central singleton for all game sound effects.
/// All clips play through a single AudioSource with a randomised pitch per call.
///
/// Setup:
///   1. Create an empty GameObject in your persistent/bootstrap scene, name it "AudioManager".
///   2. Attach this script to it.
///   3. Drag each .wav from Assets/Audio/SFX into the matching slot in the Inspector:
///        Voice Clip  -> voice.wav
///        Land Clip   -> Hit63.wav
///        Death Clip  -> death.wav
///        Pickup Clip -> pickup.wav  (or pickup2.wav, whichever you prefer)
///   4. The object survives scene loads automatically (DontDestroyOnLoad).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    [Tooltip("Played on each letter revealed in dialogue typewriter.")]
    public AudioClip voiceClip;

    [Tooltip("Played when the player lands on a tile.")]
    public AudioClip landClip;

    [Tooltip("Played when the player dies.")]
    public AudioClip deathClip;

    [Tooltip("Played when the player picks up the win item or triggers any win/exit.")]
    public AudioClip pickupClip;

    [Header("Pitch Variance")]
    [Tooltip("Minimum random pitch multiplier applied to every sound.")]
    public float pitchMin = 0.9f;

    [Tooltip("Maximum random pitch multiplier applied to every sound.")]
    public float pitchMax = 1.1f;

    private AudioSource _source;

    // ── lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source             = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    // ── public API ────────────────────────────────────────────────────────────

    /// <summary>Played on each letter revealed during dialogue typewriter.</summary>
    public void PlayVoice()  => Play(voiceClip);

    /// <summary>Played when the player lands on a tile.</summary>
    public void PlayLand()   => Play(landClip);

    /// <summary>Played when the player dies.</summary>
    public void PlayDeath()  => Play(deathClip);

    /// <summary>Played when the player picks up the win item or enters any win / scene-exit state.</summary>
    public void PlayPickup() => Play(pickupClip);

    // ── internal ─────────────────────────────────────────────────────────────

    void Play(AudioClip clip)
    {
        if (clip == null || _source == null) return;
        _source.pitch = Random.Range(pitchMin, pitchMax);
        _source.PlayOneShot(clip);
    }
}
