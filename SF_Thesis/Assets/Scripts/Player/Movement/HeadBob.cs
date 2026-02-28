using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class HeadBob : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FirstPersonController controller;

    [Header("Base Offsets")]
    [SerializeField] private float standEyeHeight = 1.74f;
    [SerializeField] private float crouchEyeHeight = 1.20f;
    [SerializeField] private float eyeHeightSmoothTime = 0.08f;

    [Header("Bob Shape")]
    [SerializeField] private float walkAmplitude = 0.06f;
    [SerializeField] private float sprintAmplitude = 0.11f;
    [SerializeField] private float crouchAmplitude = 0.03f;
    [SerializeField] private float horizontalAmplitude = 0.015f;
    [SerializeField] private float returnSmooth = 10f;

    [Header("Step Sync (1 cycle per step)")]
    [SerializeField] private bool syncToFootsteps = true;
    [SerializeField, Range(0f, 1f)] private float phaseBlend = 0.35f;

    [Header("Activation")]
    [SerializeField] private float minSpeedForBob = 0.05f;

    [Header("Idle Breathing")]
    [SerializeField] private bool enableIdleBreathing = true;
    [SerializeField] private float idleFrequency = 0.25f;
    [SerializeField] private float idleAmplitude = 0.01f;
    [SerializeField] private float idleSwayAmplitude = 0.004f;
    [SerializeField] private float idleBlendIn = 3.5f;
    [SerializeField] private float idleNoiseDrift = 0.15f;

    private Vector3 localBasePos;
    private float eyeHeightVel;

    private float bobTimer; // cycles
    private float stepHz = -1f;

    private float idleTimer;
    private float idlePhaseOffset;
    private float idleBlend;

    private const float BottomPhase01 = 0.75f;

    private void OnEnable()
    {
        PlayerFootstepNoiseEmitter.OnStep += OnStep;
    }

    private void OnDisable()
    {
        PlayerFootstepNoiseEmitter.OnStep -= OnStep;
    }

    private void Start()
    {
        localBasePos = transform.localPosition;
        idlePhaseOffset = UnityEngine.Random.value * Mathf.PI * 2f;

        float startY = controller != null && controller.IsCrouching ? crouchEyeHeight : standEyeHeight;
        localBasePos.y = startY;
        transform.localPosition = localBasePos;
    }

    private void OnStep(float intervalSeconds)
    {
        if (!syncToFootsteps) return;

        stepHz = 1f / Mathf.Max(0.01f, intervalSeconds);

        // Soft-align to sine minimum on the step
        float phase01 = bobTimer - Mathf.Floor(bobTimer);
        float delta01 = Mathf.DeltaAngle(phase01 * 360f, BottomPhase01 * 360f) / 360f;

        bobTimer += delta01 * phaseBlend;
    }

    private void LateUpdate()
    {
        if (controller == null) return;

        float targetEye = controller.IsCrouching ? crouchEyeHeight : standEyeHeight;
        localBasePos.y = Mathf.SmoothDamp(localBasePos.y, targetEye, ref eyeHeightVel, eyeHeightSmoothTime);

        Vector3 target = localBasePos;

        bool moving = controller.HorizontalSpeed > minSpeedForBob && controller.IsGrounded;

        if (moving)
        {
            idleBlend = Mathf.MoveTowards(idleBlend, 0f, Time.deltaTime * idleBlendIn);

            // ✅ immediate cadence even before first step:
            if (syncToFootsteps)
            {
                float est = PlayerFootstepNoiseEmitter.CurrentStepIntervalEstimate;
                if (est > 0f) stepHz = 1f / est;
            }

            if (stepHz > 0f)
            {
                bobTimer += Time.deltaTime * stepHz;

                float amp = controller.IsCrouching ? crouchAmplitude :
                            controller.HorizontalSpeed > 6.5f ? sprintAmplitude :
                            walkAmplitude;

                float phase = bobTimer * Mathf.PI * 2f;
                float bobY = Mathf.Sin(phase) * amp;
                float bobX = Mathf.Cos(phase) * horizontalAmplitude;

                target += new Vector3(bobX, bobY, 0f);
            }
        }
        else if (enableIdleBreathing)
        {
            idleBlend = Mathf.MoveTowards(idleBlend, 1f, Time.deltaTime * idleBlendIn);

            float drift = 1f + Mathf.Sin(Time.time * 0.7f) * idleNoiseDrift;

            idleTimer += Time.deltaTime * idleFrequency * drift;
            float phase = (idleTimer * Mathf.PI * 2f) + idlePhaseOffset;

            float breathY = Mathf.Sin(phase) * idleAmplitude;
            float breathX = Mathf.Sin(phase + Mathf.PI * 0.5f) * idleSwayAmplitude;

            target += new Vector3(breathX, breathY, 0f) * idleBlend;
        }
        else
        {
            idleBlend = Mathf.MoveTowards(idleBlend, 0f, Time.deltaTime * idleBlendIn);
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * returnSmooth);
    }
}
