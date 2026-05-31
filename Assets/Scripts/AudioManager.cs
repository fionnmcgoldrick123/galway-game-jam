using System.Collections;
using UnityEngine;

/// <summary>
/// Central singleton for all game sound effects and music.
///
/// SFX volume  : 0.65  (hardcoded)
/// Music volume: 0.35  (hardcoded)
///
/// Setup — Inspector only:
///   SFX Clips : voice.wav | Hit63.wav | death.wav | pickup.wav
///   Songs[0-2]: drag your three .mp3 files in order
/// Everything else (volumes, seamless transitions) is handled in code.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── volumes (no Inspector tweaking needed) ───────────────────────────────
    const float SFX_VOLUME   = 0.65f;
    const float MUSIC_VOLUME = 0.35f;

    // ── SFX ──────────────────────────────────────────────────────────────────
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

    // ── Music ─────────────────────────────────────────────────────────────────
    [Header("Music Playlist")]
    [Tooltip("Drag your three songs here in play order. They loop seamlessly.")]
    public AudioClip[] songs;

    // ── private ───────────────────────────────────────────────────────────────
    private AudioSource _sfxSource;
    private AudioSource _musicA;
    private AudioSource _musicB;

    // ── lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSource         = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.volume  = SFX_VOLUME;

        _musicA            = CreateMusicSource();
        _musicB            = CreateMusicSource();
    }

    void Start()
    {
        if (songs != null && songs.Length > 0)
            StartCoroutine(RunPlaylist());
    }

    AudioSource CreateMusicSource()
    {
        var src         = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop        = false;
        src.volume      = MUSIC_VOLUME;
        return src;
    }

    // ── seamless music playlist ───────────────────────────────────────────────
    // Uses dual AudioSources + AudioSource.PlayScheduled so there is zero gap
    // between tracks. Each track is scheduled 1 second before the current one
    // ends using exact DSP time derived from the clip's sample count.

    IEnumerator RunPlaylist()
    {
        AudioSource[] srcs   = { _musicA, _musicB };
        int           srcIdx = 0;
        int           songIdx = 0;

        // Schedule the first song to start in 0.1 s.
        double nextStart = AudioSettings.dspTime + 0.1;

        srcs[srcIdx].clip = songs[songIdx];
        srcs[srcIdx].PlayScheduled(nextStart);
        nextStart += ClipDspLength(songs[songIdx]);
        songIdx    = (songIdx + 1) % songs.Length;
        srcIdx     = 1 - srcIdx;

        // Keep scheduling the next song 1 s before the current one ends.
        while (true)
        {
            float waitTime = (float)(nextStart - AudioSettings.dspTime - 1.0);
            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);

            srcs[srcIdx].clip = songs[songIdx];
            srcs[srcIdx].PlayScheduled(nextStart);
            nextStart += ClipDspLength(songs[songIdx]);
            songIdx    = (songIdx + 1) % songs.Length;
            srcIdx     = 1 - srcIdx;
        }
    }

    // Exact DSP length avoids the tiny rounding error in AudioClip.length.
    static double ClipDspLength(AudioClip clip) =>
        clip.samples / (double)clip.frequency;

    // ── public SFX API ────────────────────────────────────────────────────────

    /// <summary>Played on each letter revealed during dialogue typewriter.</summary>
    public void PlayVoice()  => Play(voiceClip);

    /// <summary>Played when the player lands on a tile.</summary>
    public void PlayLand()   => Play(landClip);

    /// <summary>Played when the player dies.</summary>
    public void PlayDeath()  => Play(deathClip);

    /// <summary>Played when the player picks up the win item or enters any win / scene-exit state.</summary>
    public void PlayPickup() => Play(pickupClip);

    /// <summary>Mutes or unmutes the music sources without stopping the playlist.</summary>
    public void SetMusicMuted(bool muted)
    {
        float vol = muted ? 0f : MUSIC_VOLUME;
        if (_musicA != null) _musicA.volume = vol;
        if (_musicB != null) _musicB.volume = vol;
    }

    // ── internal ──────────────────────────────────────────────────────────────

    void Play(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        _sfxSource.PlayOneShot(clip);
    }
}
