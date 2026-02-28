using UnityEngine;

public class MonsterAudio : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource footstepsSource;
    [SerializeField] private AudioSource vocalSource;

    [Header("Footsteps Clips")]
    [SerializeField] private AudioClip[] walkSteps;
    [SerializeField] private AudioClip[] runSteps; // heavier set

    [Header("Vocal / One-shots")]
    [SerializeField] private AudioClip screamHeardYou;     // when it commits to chase
    [SerializeField] private AudioClip growlAttack;        // entering Attack
    [SerializeField] private AudioClip frustratedSearchEnd; // didn't find anything
    [SerializeField] private AudioClip killClip;           // on kill

    [Header("Footstep Timing")]
    [SerializeField] private float walkStepInterval = 0.55f;
    [SerializeField] private float runStepInterval = 0.32f;

    [Header("Footstep Volume/Pitch")]
    [SerializeField] private float walkVolume = 0.55f;
    [SerializeField] private float runVolume = 0.85f;
    [SerializeField] private Vector2 walkPitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private Vector2 runPitchRange = new Vector2(0.85f, 0.95f);
    [Header("Presence / Breathing")]
    [SerializeField] private AudioSource breathingSource;

    [SerializeField] private float presenceStartDistance = 12f;
    [SerializeField] private float presenceFullDistance = 5f;
    [SerializeField] private float presenceFadeSpeed = 2.5f;

    [SerializeField, Range(0f, 1f)]
    private float maxPresenceVolume = 0.9f;

    private float presenceTarget = 0f;
    private float presenceCurrent = 0f;


    [Header("Gating")]
    [Tooltip("Minimum speed to consider the monster 'moving' for footsteps")]
    [SerializeField] private float minMoveSpeed = 0.2f;

    private float stepTimer;
    private bool inRunMode;
    private bool footstepsEnabled = true;

    public void SetFootstepsEnabled(bool enabled)
    {
        footstepsEnabled = enabled;
        if (!enabled && footstepsSource) footstepsSource.Stop();
    }

    public void UpdateFootsteps(Vector3 worldVelocity, bool runMode)
    {
        if (!footstepsEnabled || footstepsSource == null) return;

        float speed = new Vector3(worldVelocity.x, 0f, worldVelocity.z).magnitude;
        if (speed < minMoveSpeed)
        {
            if (footstepsSource.isPlaying) footstepsSource.Stop();
            stepTimer = 0f;
            return;
        }

        inRunMode = runMode;
        footstepsSource.loop = false;

        stepTimer -= Time.deltaTime;
        float interval = inRunMode ? runStepInterval : walkStepInterval;

        if (stepTimer <= 0f)
        {
            PlayStep(inRunMode);
            stepTimer = interval;
        }
    }

    private void PlayStep(bool run)
    {
        var clips = run
            ? runSteps
            : walkSteps;
        if (clips == null || clips.Length == 0) return;

        var clip = clips[Random.Range(0, clips.Length)];
        footstepsSource.pitch = run
            ? Random.Range(runPitchRange.x, runPitchRange.y)
            : Random.Range(walkPitchRange.x, walkPitchRange.y);

        footstepsSource.volume = run ? runVolume : walkVolume;
        footstepsSource.PlayOneShot(clip);
    }

    public void PlayHeardYouScream()
    {
        PlayOneShot(screamHeardYou);
    }

    public void PlayAttackGrowl()
    {
        PlayOneShot(growlAttack);
    }

    public void PlayFrustrated()
    {
        PlayOneShot(frustratedSearchEnd);
    }

    public void PlayKill()
    {
        PlayOneShot(killClip);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || vocalSource == null) return;
        vocalSource.PlayOneShot(clip);
    }
    public void UpdatePresence(
    Vector3 monsterPos,
    Vector3 playerPos,
    bool isActivelyChasing,
    bool isPlayerSafe
)//method generated with ChatGPT
    {
        if (!breathingSource || isPlayerSafe)
        {
            SetPresenceTarget(0f); //method generated with ChatGPT
            ApplyPresence(); //method generated with ChatGPT
            return;
        }

        float dist = Vector3.Distance(monsterPos, playerPos);


        if (dist > presenceStartDistance)
        {
            SetPresenceTarget(0f); //method generated with ChatGPT
            ApplyPresence(); //method generated with ChatGPT
            return;
        }


        float chaseDamp = isActivelyChasing ? 0.5f : 1f;

        float t = Mathf.InverseLerp(
            presenceStartDistance,
            presenceFullDistance,
            dist
        );

        SetPresenceTarget(t * chaseDamp); //method generated with ChatGPT
        ApplyPresence(); //method generated with ChatGPT
    }
    private void SetPresenceTarget(float value) //method generated with ChatGPT
    {
        presenceTarget = Mathf.Clamp01(value);
    }

    private void ApplyPresence() //method generated with ChatGPT
    {
        presenceCurrent = Mathf.MoveTowards(
            presenceCurrent,
            presenceTarget,
            presenceFadeSpeed * Time.deltaTime
        );

        if (presenceCurrent <= 0.001f)
        {
            if (breathingSource.isPlaying)
                breathingSource.Stop();
            return;
        }

        if (!breathingSource.isPlaying)
            breathingSource.Play();

        breathingSource.volume = presenceCurrent * maxPresenceVolume;
    }


}
