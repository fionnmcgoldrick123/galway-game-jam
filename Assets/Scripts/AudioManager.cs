using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const float SFX_VOLUME   = 0.65f;
    const float MUSIC_VOLUME = 0.35f;

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

    [Header("Music Playlist")]
    [Tooltip("Drag your three songs here in play order. They loop seamlessly.")]
    public AudioClip[] songs;

    private AudioSource _sfxSource;
    private AudioSource _musicA;
    private AudioSource _musicB;

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

    IEnumerator RunPlaylist()
    {
        AudioSource[] srcs   = { _musicA, _musicB };
        int           srcIdx = 0;
        int           songIdx = 0;

        double nextStart = AudioSettings.dspTime + 0.1;

        srcs[srcIdx].clip = songs[songIdx];
        srcs[srcIdx].PlayScheduled(nextStart);
        nextStart += ClipDspLength(songs[songIdx]);
        songIdx    = (songIdx + 1) % songs.Length;
        srcIdx     = 1 - srcIdx;

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

    static double ClipDspLength(AudioClip clip) =>
        clip.samples / (double)clip.frequency;

    public void PlayVoice()  => Play(voiceClip);
    public void PlayLand()   => Play(landClip);
    public void PlayDeath()  => Play(deathClip);
    public void PlayPickup() => Play(pickupClip);

    public void SetMusicMuted(bool muted)
    {
        float vol = muted ? 0f : MUSIC_VOLUME;
        if (_musicA != null) _musicA.volume = vol;
        if (_musicB != null) _musicB.volume = vol;
    }

    void Play(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        _sfxSource.PlayOneShot(clip);
    }
}
