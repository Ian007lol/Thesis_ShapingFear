using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class PlayerFootstepNoiseEmitter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FirstPersonController controller;
    [SerializeField] private Transform feet;
    [SerializeField] private InputActionReference sprintAction;

    [Header("Loudness (meters)")]
    [SerializeField] private float walkLoudness = 7f;
    [SerializeField] private float sprintLoudness = 12f;
    [SerializeField] private float crouchLoudness = 3f;
    [HideInInspector] public float occlusionMultiplier = 1f;

    [Header("Activation")]
    [SerializeField] private float minSpeedForStep = 0.05f;

    [Header("Step Distance (meters per step)")]
    [SerializeField] private float walkStepDistance = 1.6f;
    [SerializeField] private float sprintStepDistance = 1.2f;
    [SerializeField] private float crouchStepDistance = 2.2f;

    [Header("Footstep Audio")]
    [SerializeField] private AudioSource stepSource;
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] sprintClips;
    [SerializeField] private AudioClip[] crouchClips;

    [Range(0f, 1f)] [SerializeField] private float baseStepVolume = 0.85f;
    [Range(0f, 0.5f)] [SerializeField] private float volumeJitter = 0.08f;
    [Range(0f, 0.5f)] [SerializeField] private float pitchJitter = 0.06f;

    [Header("Footstep Volume Mult")]
    [SerializeField] private float walkVolumeMultiplier = 1.0f;
    [SerializeField] private float sprintVolumeMultiplier = 1.35f;
    [SerializeField] private float crouchVolumeMultiplier = 0.45f;

    [SerializeField] private bool preventImmediateRepeat = true;
    [SerializeField] private bool useShuffleBag = true;

    private int lastClipIndex = -1;
    private readonly List<int> bag = new List<int>(32);

    private Vector3 lastFeetPos;
    private bool hasLastPos;
    private float distanceAccum;

    public static event Action<float> OnStep; 

    public static float CurrentStepIntervalEstimate { get; private set; } = -1f;

    private void Reset() => feet = transform;

    private void Awake()
    {
        if (stepSource == null)
            stepSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        stepSource.playOnAwake = false;
        stepSource.spatialBlend = 1f;
        stepSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    private void OnEnable()
    {
        sprintAction?.action.Enable();
        hasLastPos = false;
        distanceAccum = 0f;
        CurrentStepIntervalEstimate = -1f;
    }

    private void OnDisable()
    {
        sprintAction?.action.Disable();
    }

    private void Update()
    {
        if (controller == null) return;

        Vector3 feetPos = feet ? feet.position : transform.position;

        if (SafeAreaVolume.IsPointInside(feetPos))
        {
            distanceAccum = 0f;
            hasLastPos = false;
            CurrentStepIntervalEstimate = -1f;
            return;
        }

        bool groundedMoving = controller.IsGrounded && controller.HorizontalSpeed > minSpeedForStep;

        if (!hasLastPos)
        {
            lastFeetPos = feetPos;
            hasLastPos = true;
            CurrentStepIntervalEstimate = -1f;
            return;
        }

        Vector3 delta = feetPos - lastFeetPos;
        delta.y = 0f;
        float d = delta.magnitude;
        lastFeetPos = feetPos;

        if (!groundedMoving)
        {
            CurrentStepIntervalEstimate = -1f;
            return;
        }

        bool crouching = controller.IsCrouching;
        bool sprinting = sprintAction != null && sprintAction.action.IsPressed() && !crouching;

        float stepDist = crouching ? crouchStepDistance : (sprinting ? sprintStepDistance : walkStepDistance);
        float loudness = crouching ? crouchLoudness : (sprinting ? sprintLoudness : walkLoudness);

        float speed = Mathf.Max(0.01f, controller.HorizontalSpeed);

        CurrentStepIntervalEstimate = stepDist / speed;

        distanceAccum += d;

        while (distanceAccum >= stepDist)
        {
            distanceAccum -= stepDist;

            Noise.MakeNoise(feetPos, loudness * occlusionMultiplier, gameObject);
            PlayFootstepAudio(crouching, sprinting);

            OnStep?.Invoke(CurrentStepIntervalEstimate);
        }
    }

    private void PlayFootstepAudio(bool crouching, bool sprinting)
    {
        if (stepSource == null) return;

        AudioClip[] clips = crouching ? crouchClips : (sprinting ? sprintClips : walkClips);
        if (clips == null || clips.Length == 0) return;

        int idx = PickClipIndex(clips.Length); //method generated with ChatGPT

        float gaitMultiplier =
            crouching ? crouchVolumeMultiplier :
            sprinting ? sprintVolumeMultiplier :
            walkVolumeMultiplier;

        float vol =
            baseStepVolume *
            gaitMultiplier *
            UnityEngine.Random.Range(1f - volumeJitter, 1f + volumeJitter);

        vol *= Mathf.Clamp01(occlusionMultiplier);

        float pitch = UnityEngine.Random.Range(1f - pitchJitter, 1f + pitchJitter);

        stepSource.pitch = pitch;
        stepSource.PlayOneShot(clips[idx], vol);
    }

    private int PickClipIndex(int count) //method generated with ChatGPT
    {
        if (count <= 0) return 0;
        if (count == 1) return 0;

        if (useShuffleBag)
        {
            if (bag.Count == 0)
            {
                for (int i = 0; i < count; i++) bag.Add(i);
                if (preventImmediateRepeat && lastClipIndex >= 0)
                {
                    bag.Remove(lastClipIndex);
                    bag.Add(lastClipIndex);
                }
            }

            int pick = UnityEngine.Random.Range(0, bag.Count);
            int idx = bag[pick];
            bag.RemoveAt(pick);
            lastClipIndex = idx;
            return idx;
        }

        int r;
        int guard = 10;
        do r = UnityEngine.Random.Range(0, count);
        while (preventImmediateRepeat && r == lastClipIndex && guard-- > 0);

        lastClipIndex = r;
        return r;
    }
}
