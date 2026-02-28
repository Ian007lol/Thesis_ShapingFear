using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(AudioSource))]
public class VoiceDecoy : MonoBehaviour
{
    [Header("Voice Processor")]
    [Tooltip("Reference to the central VoiceProcessor (used also by Vosk, etc.). If left empty, it will be auto-found in the scene.")]
    [SerializeField] private VoiceProcessor voiceProcessor;

    [Header("Recording")]
    [Tooltip("Maximum recording length in seconds.")]
    [SerializeField] private int maxRecordSeconds = 5;

    [Tooltip("Input action used for both tap (play/stop) and hold (record/overwrite).")]
    [SerializeField] private InputActionReference recordAction; // e.g. Player/DecoyInteract

    [Tooltip("Only allow recording & playback when the decoy is held.")]
    [SerializeField] private bool requireHeldToUse = true;

    [Tooltip("Minimum hold duration (seconds) to count as 'record' instead of 'tap'.")]
    [SerializeField] private float holdThreshold = 0.25f;

    [Header("Mouth / Feedback")]
    [Tooltip("Transform representing the player's 'mouth' or head (usually the camera).")]
    [SerializeField] private Transform mouthTransform;

    [Tooltip("Offset from the mouth when recording (relative to mouth forward/up/right).")]
    [SerializeField] private Vector3 mouthOffset = new Vector3(0.0f, -0.05f, 0.25f);

    [Tooltip("How fast the decoy moves towards the mouth position while recording (used by GrabAndThrow).")]
    [SerializeField] private float moveToMouthSpeed = 10f;

    [Header("Playback & Decoy Noise")]
    [Tooltip("Automatically start playback on the first collision after being thrown.")]
    [SerializeField] private bool autoPlayOnFirstCollision = true;

    [Tooltip("Maximum noise radius (meters) when the recorded voice is very loud.")]
    [SerializeField] private float decoyNoiseLoudness = 10f;

    [Tooltip("Interval (seconds) between noise events while the decoy is playing.")]
    [SerializeField] private float decoyNoiseInterval = 0.25f;

    [Tooltip("Minimum fraction of decoyNoiseLoudness used for very quiet sounds.")]
    [SerializeField, Range(0f, 1f)]
    private float quietRadiusFraction = 0.2f;   // e.g. 0.2 → min radius = 20% of max

    [Tooltip("dBFS range we care about for mapping (-60 = quiet speech, -10 = very loud).")]
    [SerializeField] private Vector2 dbMappingRange = new Vector2(-60f, -10f);
    [Header("Destruction")]
    [Tooltip("Optional sound to play at the decoy's position when the monster destroys it.")]
    [SerializeField] private AudioClip destroyedByMonsterClip;

    [Tooltip("Optional prefab to spawn when the monster destroys the decoy (e.g. particles, broken pieces).")]
    [SerializeField] private GameObject destroyedEffectPrefab;
    [Header("Held Toggle")]
    [Tooltip("Scripts to DISABLE while held, and ENABLE again when released.")]
    [SerializeField] private Behaviour[] disableWhileHeld;

    private AudioSource audioSource;

    // Recording via VoiceProcessor
    private bool isRecordingDecoy;
    private bool isHeld;
    private bool hasPlayedOnce;

    private List<short> recordedSamples = new List<short>();
    private int sampleRate = 16000;   // fallback; overwritten by voiceProcessor.SampleRate

    // tap vs hold timing
    private float buttonDownTime;

    // buffer for analysis during playback
    private float[] analysisBuffer;

    // --- Public props used by GrabAndThrow for mouth targeting ---
    public bool HasMouthTarget
    {
        get { return isRecordingDecoy && isHeld && mouthTransform != null; }
    }

    public Vector3 MouthTargetPosition
    {
        get
        {
            if (mouthTransform == null)
                return transform.position;

            return mouthTransform.position
                   + mouthTransform.forward * mouthOffset.z
                   + mouthTransform.up * mouthOffset.y
                   + mouthTransform.right * mouthOffset.x;
        }
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (voiceProcessor == null)
        {
#pragma warning disable CS0618
            voiceProcessor = FindObjectOfType<VoiceProcessor>();
#pragma warning restore CS0618
        }

        if (voiceProcessor == null)
        {
            Debug.LogWarning("[VoiceDecoy] No VoiceProcessor found in scene. Decoy recording will not work.");
        }
        else
        {
            sampleRate = voiceProcessor.SampleRate > 0 ? voiceProcessor.SampleRate : sampleRate;
        }
    }

    private void OnEnable()
    {
        if (voiceProcessor != null)
        {
            voiceProcessor.OnFrameCapturedRaw += OnVoiceFrame;
        }

        if (recordAction != null)
        {
            recordAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (voiceProcessor != null)
        {
            voiceProcessor.OnFrameCapturedRaw -= OnVoiceFrame;
        }

        if (recordAction != null)
        {
            recordAction.action.Disable();
        }

        StopRecordingInternal();
    }
    public void ClearClip()
    {
        if (audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = null;
    }

    public bool GetHasPlayedOnce() => hasPlayedOnce;
    public void SetHasPlayedOnce(bool v) => hasPlayedOnce = v;

    public bool GetIsHeld() => isHeld;

    /// <summary>
    /// Called by checkpoint restore to stop playback/noise, stop recording,
    /// and leave the decoy in a clean idle state.
    /// </summary>
    public void ResetRuntimeForCheckpointRestore()
    {
        StopAllCoroutines();

        if (audioSource != null)
            audioSource.Stop();

        // stop recording and discard provisional buffer
        isRecordingDecoy = false;
        recordedSamples.Clear();

        // make sure it doesn't keep being "held to mouth" during restore
        isHeld = false;
    }

    private void Update()
    {
        if (recordAction == null) return;

        var act = recordAction.action;

        // Button pressed: start timing and provisional recording
        if (act.WasPressedThisFrame())
        {
            if (requireHeldToUse && !isHeld)
                return;

            buttonDownTime = Time.time;
            BeginRecordingInternal();  // provisional
        }

        // Button released: decide tap vs hold
        if (act.WasReleasedThisFrame())
        {
            if (requireHeldToUse && !isHeld)
                return;

            float duration = Time.time - buttonDownTime;

            if (duration >= holdThreshold)
            {
                // HOLD → finalize recording (overwrite previous clip)
                if (isRecordingDecoy)
                    EndRecording();
            }
            else
            {
                // TAP → cancel provisional recording & toggle playback
                if (isRecordingDecoy)
                    CancelRecordingWithoutSaving();

                TogglePlayback();
            }
        }
    }

    /// <summary>
    /// Called by GrabAndThrow when the player picks up or drops the decoy.
    /// </summary>
    public void SetHeld(bool held)
    {
        isHeld = held;

        // ✅ Toggle other scripts
        if (disableWhileHeld != null)
        {
            for (int i = 0; i < disableWhileHeld.Length; i++)
            {
                var b = disableWhileHeld[i];
                if (b == null) continue;
                b.enabled = !held; // held -> disable, released -> enable
            }
        }

        // If dropped during recording, finalize recording automatically
        if (!held && isRecordingDecoy)
        {
            EndRecording();
        }
    }


    // --- Recording logic ---

    private void BeginRecordingInternal()
    {
        if (voiceProcessor == null)
        {
            Debug.LogWarning("[VoiceDecoy] Cannot record: no VoiceProcessor available.");
            return;
        }

        if (!voiceProcessor.IsRecording)
        {
            Debug.LogWarning("[VoiceDecoy] VoiceProcessor is not recording. Ensure it's started elsewhere.");
        }

        recordedSamples.Clear();
        sampleRate = voiceProcessor.SampleRate > 0 ? voiceProcessor.SampleRate : sampleRate;

        isRecordingDecoy = true;
        hasPlayedOnce = false;

        Debug.Log("[VoiceDecoy] Provisional recording started via VoiceProcessor.");
    }

    /// <summary>
    /// Stop recording and bake the samples into an AudioClip on the AudioSource.
    /// Overwrites any previous recording.
    /// </summary>
    public void EndRecording()
    {
        if (!isRecordingDecoy)
            return;

        isRecordingDecoy = false;

        if (recordedSamples.Count == 0)
        {
            Debug.LogWarning("[VoiceDecoy] No samples recorded.");
            return;
        }

        int sampleCount = recordedSamples.Count;
        float[] floatSamples = new float[sampleCount];

        const float invMaxShort = 1f / 32768f;
        for (int i = 0; i < sampleCount; i++)
        {
            floatSamples[i] = recordedSamples[i] * invMaxShort;
        }

        AudioClip clip = AudioClip.Create(
            "VoiceDecoyClip",
            sampleCount,
            1,                  // mono
            sampleRate,
            false
        );
        clip.SetData(floatSamples, 0);

        audioSource.clip = clip;

        float duration = (float)sampleCount / sampleRate;
        Debug.Log("[VoiceDecoy] Recording finished. Length: " + duration.ToString("0.00") + "s");
    }

    private void CancelRecordingWithoutSaving()
    {
        if (!isRecordingDecoy) return;

        isRecordingDecoy = false;
        recordedSamples.Clear();
        Debug.Log("[VoiceDecoy] Provisional recording cancelled (tap detected).");
    }

    private void StopRecordingInternal()
    {
        isRecordingDecoy = false;
        recordedSamples.Clear();
    }

    /// <summary>
    /// Called every time VoiceProcessor has a new audio frame from the mic.
    /// We siphon frames into our buffer while isRecordingDecoy is true.
    /// </summary>
    private void OnVoiceFrame(short[] pcmFrame)
    {
        if (!isRecordingDecoy) return;
        if (requireHeldToUse && !isHeld) return;
        if (pcmFrame == null || pcmFrame.Length == 0) return;

        int maxSamples = sampleRate * maxRecordSeconds;
        if (recordedSamples.Count >= maxSamples)
        {
            // Reached max length, stop recording gracefully
            EndRecording();
            return;
        }

        int remaining = maxSamples - recordedSamples.Count;
        int copyCount = Mathf.Min(remaining, pcmFrame.Length);

        for (int i = 0; i < copyCount; i++)
        {
            recordedSamples.Add(pcmFrame[i]);
        }
    }

    // --- Playback & noise ---

    private void TogglePlayback()
    {
        if (audioSource.clip == null)
        {
            Debug.Log("[VoiceDecoy] No clip to play on tap.");
            return;
        }

        if (audioSource.isPlaying)
        {
            // Stop playback
            audioSource.Stop();
            StopAllCoroutines();
        }
        else
        {
            // Start playback from beginning
            PlayDecoy();
        }
    }

    /// <summary>
    /// Manually trigger decoy playback.
    /// </summary>
    public void PlayDecoy()
    {
        if (audioSource.clip == null)
        {
            Debug.LogWarning("[VoiceDecoy] No recorded clip to play.");
            return;
        }

        StopAllCoroutines();
        audioSource.Stop();
        audioSource.Play();
        StartCoroutine(NoiseWhilePlaying());
        hasPlayedOnce = true;
    }

    private IEnumerator NoiseWhilePlaying()
    {
        float timer = 0f;
        while (audioSource.isPlaying)
        {
            timer += Time.deltaTime;
            if (timer >= decoyNoiseInterval)
            {
                timer = 0f;

                float db = GetCurrentPlaybackDb();
                // Map dB to a radius between quiet and loud
                float minRadius = decoyNoiseLoudness * quietRadiusFraction;
                float maxRadius = decoyNoiseLoudness;

                // dbMappingRange.x = quiet, dbMappingRange.y = loud
                float factor = Mathf.InverseLerp(dbMappingRange.x, dbMappingRange.y, db);
                float loudness = Mathf.Lerp(minRadius, maxRadius, factor);

                if (loudness > 0.01f)
                {
                    Noise.MakeNoise(transform.position, loudness * 1.33f, gameObject);
                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// Sample a short window of the current playing clip around audioSource.time
    /// and estimate its dBFS.
    /// </summary>
    private float GetCurrentPlaybackDb()
    {
        if (audioSource.clip == null) return -80f;

        AudioClip clip = audioSource.clip;
        int totalSamples = clip.samples;

        if (totalSamples <= 0) return -80f;

        int windowSize = 1024; // small analysis window
        if (analysisBuffer == null || analysisBuffer.Length != windowSize)
            analysisBuffer = new float[windowSize];

        int currentSample = audioSource.timeSamples;
        if (currentSample <= 0) return -80f;

        int offset = currentSample - windowSize;
        if (offset < 0) offset = 0;
        if (offset + windowSize > totalSamples)
        {
            windowSize = totalSamples - offset;
            if (windowSize <= 0) return -80f;
        }

        clip.GetData(analysisBuffer, offset);

        double sum = 0.0;
        for (int i = 0; i < windowSize; i++)
        {
            float s = analysisBuffer[i];
            sum += s * s;
        }

        float rms = windowSize > 0 ? Mathf.Sqrt((float)(sum / windowSize)) : 0f;
        if (rms <= 0f) return -80f;

        float db = 20f * Mathf.Log10(Mathf.Max(1e-6f, rms));
        return Mathf.Clamp(db, -80f, 0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!autoPlayOnFirstCollision) return;
        if (isRecordingDecoy) return;

        // Auto-play only once from collision
        if (hasPlayedOnce) return;

        if (audioSource.clip != null)
        {
            PlayDecoy();
        }
    }

    /// <summary>
    /// Called by the monster when it reaches this decoy and destroys it.
    /// Stops playback, stops emitting noise, optionally plays a destruction sound / VFX, then destroys the GameObject.
    /// </summary>
    public void DestroyByMonster()
    {
        // Stop any playback & noise coroutines
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        StopAllCoroutines();

        // Optional: play destruction sound at this position
        if (destroyedByMonsterClip != null)
        {
            AudioSource.PlayClipAtPoint(destroyedByMonsterClip, transform.position);
        }

        // Optional: spawn VFX
        if (destroyedEffectPrefab != null)
        {
            Instantiate(destroyedEffectPrefab, transform.position, Quaternion.identity);
        }

        // Finally, destroy the decoy
        Destroy(gameObject);
    }

}
