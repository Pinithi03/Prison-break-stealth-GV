using UnityEngine;

/// <summary>
/// Central audio manager for Prison Break: Silent Escape.
/// Attach to a GameObject named "AudioManager" in the scene.
/// Drag your audio clips into each slot in the Inspector.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Player Sounds")]
    public AudioClip footstepClip;

    [Header("Item Sounds")]
    public AudioClip pickupClip;

    [Header("Door Sounds")]
    public AudioClip doorOpenClip;
    public AudioClip doorLockedClip;

    [Header("Guard Sounds")]
    public AudioClip guardAlertClip;

    [Header("Objective & UI")]
    public AudioClip objectiveClip;
    public AudioClip buttonClickClip;

    [Header("Game State")]
    public AudioClip victoryClip;
    public AudioClip gameOverClip;

    [Header("Ambience")]
    public AudioClip ambientClip;
    [Range(0f, 1f)] public float ambientVolume = 0.25f;

    // ── Audio Sources ─────────────────────────────────────────────────────
    private AudioSource sfxSource;       // for one-shot SFX
    private AudioSource footstepSource;  // separate so footsteps don't cut other SFX
    private AudioSource ambientSource;   // looping ambient background

    // ── Footstep timing ───────────────────────────────────────────────────
    private float footstepTimer = 0f;
    public float footstepInterval = 0.42f;   // seconds between footstep sounds

    // ── Guard alert cooldown (prevent spam) ───────────────────────────────
    private float guardAlertCooldown = 0f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create audio sources
        sfxSource       = gameObject.AddComponent<AudioSource>();
        footstepSource  = gameObject.AddComponent<AudioSource>();
        ambientSource   = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake      = false;
        footstepSource.playOnAwake = false;

        // Ambient loops forever quietly in background
        ambientSource.clip      = ambientClip;
        ambientSource.loop      = true;
        ambientSource.volume    = ambientVolume;
        ambientSource.playOnAwake = false;

        if (ambientClip != null)
            ambientSource.Play();
    }

    void Update()
    {
        // Tick down guard alert cooldown
        if (guardAlertCooldown > 0f)
            guardAlertCooldown -= Time.deltaTime;
    }

    // ═════════════════════════════════════════════════════════════════════
    // Public API — called by other scripts
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Call every frame while the player is moving. Internally timed.</summary>
    public void TickFootstep(bool isMoving)
    {
        if (!isMoving)
        {
            // Stop footstep immediately — no lingering
            footstepTimer = footstepInterval; // so next move plays instantly
            if (footstepSource.isPlaying)
                footstepSource.Stop();
            return;
        }

        footstepTimer += Time.deltaTime;
        if (footstepTimer >= footstepInterval)
        {
            footstepTimer = 0f;
            if (footstepClip != null)
            {
                footstepSource.pitch  = Random.Range(0.9f, 1.1f);
                footstepSource.volume = 0.55f;
                footstepSource.clip   = footstepClip;
                footstepSource.Play();
            }
        }
    }

    public void PlayPickup()            => PlaySFX(pickupClip,     0.9f);
    public void PlayDoorOpen()          => PlaySFX(doorOpenClip,   0.8f);
    public void PlayDoorLocked()        => PlaySFX(doorLockedClip, 0.75f);
    public void PlayObjectiveComplete() => PlaySFX(objectiveClip,  0.85f);
    public void PlayButtonClick()       => PlaySFX(buttonClickClip, 0.9f);

    public void PlayGuardAlert()
    {
        if (guardAlertCooldown > 0f) return;   // don't spam
        guardAlertCooldown = 3f;
        PlaySFX(guardAlertClip, 1f);
    }

    public void PlayVictory()
    {
        StopAmbient();
        PlaySFX(victoryClip, 1f);
    }

    public void PlayGameOver()
    {
        StopAmbient();
        PlaySFX(gameOverClip, 1f);
    }

    public void StopAmbient()
    {
        if (ambientSource.isPlaying)
            ambientSource.Stop();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────
    void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    void PlayClip(AudioSource source, AudioClip clip, float volume, float pitch)
    {
        if (clip == null || source == null) return;
        source.pitch  = pitch;
        source.volume = volume;
        source.PlayOneShot(clip);
    }

    float RandomPitch(float min, float max) => Random.Range(min, max);
}
