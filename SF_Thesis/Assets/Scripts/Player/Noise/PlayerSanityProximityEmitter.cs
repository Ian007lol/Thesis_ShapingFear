using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class PlayerSanityProximityEmitter : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("The monster transform (or any transform on the monster root).")]
    [SerializeField] private Transform monster;

    [Tooltip("Optional: a head transform to emit noise from (else uses player position + height).")]
    [SerializeField] private Transform head;

    [Tooltip("If set, breathing audio volume will follow anxiety.")]
    [SerializeField] private AudioSource breathingLoop;

    [Header("Anxiety (0..1)")]
    [SerializeField, Range(0f, 1f)] private float anxiety = 0f;

    [Tooltip("Distance where anxiety starts increasing.")]
    [SerializeField] private float startDistance = 6.0f;

    [Tooltip("Distance where anxiety increases fastest (max danger).")]
    [SerializeField] private float panicDistance = 2.2f;

    [Tooltip("Seconds to go from calm to full panic if you stay at panicDistance.")]
    [SerializeField] private float panicBuildTime = 5.0f;

    [Tooltip("Seconds to go from panic to calm when you're far away.")]
    [SerializeField] private float calmRecoverTime = 6.5f;

    [Header("Breathing Trigger")]
    [SerializeField, Range(0f, 1f)] private float breathingStartsAt = 0.35f;

    [SerializeField, Range(0f, 1f)] private float breathingFullAt = 0.85f;

    [Header("Passive Noise Emission")]
    [Tooltip("Emit noise only when anxiety >= this threshold.")]
    [SerializeField, Range(0f, 1f)] private float noiseStartsAt = 0.45f;

    [Tooltip("Seconds between passive noise events at low anxiety (near noiseStartsAt).")]
    [SerializeField] private float maxEmitInterval = 1.1f;

    [Tooltip("Seconds between passive noise events at high anxiety (near 1.0).")]
    [SerializeField] private float minEmitInterval = 0.25f;

    [Tooltip("Noise radius (meters) at low anxiety (near noiseStartsAt).")]
    [SerializeField] private float minNoiseMeters = 2.0f;

    [Tooltip("Noise radius (meters) at high anxiety (near 1.0).")]
    [SerializeField] private float maxNoiseMeters = 7.5f;

    [Header("Occlusion / Multipliers")]
    [HideInInspector] public float occlusionMultiplier = 1f;

    [Header("Tuning")]
    [Tooltip("If true, ignores vertical distance (XZ only).")]
    [SerializeField] private bool planarDistanceOnly = true;

    [Tooltip("Optional: extra 'spike' when monster is actively chasing/attacking. Leave null if you don't want coupling.")]
    [SerializeField] private MonsterScript monsterAI;

    [Tooltip("Additional anxiety gain when monster is in Chase/Attack (0..1 scaled).")]
    [SerializeField] private float chaseBonus = 0.15f;
    [Header("Debug – Anxiety Meter")]
    [SerializeField] private bool debugShowOverlay = true;
    [SerializeField] private Vector2 debugOverlayPos = new Vector2(20, 100);
    [SerializeField] private Vector2 debugOverlaySize = new Vector2(260, 18);
    [SerializeField] private Color debugLowColor = Color.green;
    [SerializeField] private Color debugHighColor = Color.red;

    // timers
    private float _emitTimer;

    public float Anxiety01 => anxiety;

    private void Reset()
    {
        head = Camera.main ? Camera.main.transform : null;
        breathingLoop = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (monster == null) return;

        Vector3 playerPos = transform.position;
        Vector3 monsterPos = monster.position;

        if (planarDistanceOnly)
        {
            playerPos.y = 0f;
            monsterPos.y = 0f;
        }

        float dist = Vector3.Distance(playerPos, monsterPos);

        // Safe areas: never build anxiety and never emit passive noise
        Vector3 headPosSafeCheck = head ? head.position : (transform.position + Vector3.up * 1.6f);
        if (SafeAreaVolume.IsPointInside(transform.position) || SafeAreaVolume.IsPointInside(headPosSafeCheck))
        {
            anxiety = Mathf.MoveTowards(anxiety, 0f, Time.deltaTime / Mathf.Max(0.01f, calmRecoverTime));
            UpdateBreathing();
            _emitTimer = 0f;
            return;
        }

        // Map distance -> "danger" [0..1]
        // dist >= startDistance => 0
        // dist <= panicDistance => 1
        float danger = Mathf.InverseLerp(startDistance, panicDistance, dist);
        danger = Mathf.Clamp01(danger);

        // Build/Recover speeds
        float buildRate = (panicBuildTime <= 0.01f) ? 999f : (1f / panicBuildTime);
        float recoverRate = (calmRecoverTime <= 0.01f) ? 999f : (1f / calmRecoverTime);

        // Optional: if monster is actively hunting, push anxiety a bit
        if (monsterAI != null)
        {
            // best-effort: infer "aggressive" from agent speed/state coupling; if you want exact, expose a public bool on the AI.
            // We'll just add a small bonus if close enough AND the monster has a target.
            if (danger > 0.01f && monsterAI.attackTarget != null)
                danger = Mathf.Clamp01(danger + chaseBonus);
        }

        if (danger > 0f)
            anxiety = Mathf.MoveTowards(anxiety, 1f, danger * buildRate * Time.deltaTime);
        else
            anxiety = Mathf.MoveTowards(anxiety, 0f, recoverRate * Time.deltaTime);

        UpdateBreathing();
        UpdatePassiveNoise();
    }

    private void UpdateBreathing()
    {
        if (breathingLoop == null) return;

        // breathe volume = 0 until breathingStartsAt, then ramps to 1 at breathingFullAt
        float t = Mathf.InverseLerp(breathingStartsAt, breathingFullAt, anxiety);
        t = Mathf.Clamp01(t);

        if (t <= 0.001f)
        {
            if (breathingLoop.isPlaying) breathingLoop.Stop();
            return;
        }

        if (!breathingLoop.isPlaying)
        {
            breathingLoop.loop = true;
            breathingLoop.Play();
        }

        breathingLoop.volume = t; // you can scale this if you want
        // Optional: slight pitch modulation feels alive
        breathingLoop.pitch = Mathf.Lerp(1.0f, 1.12f, t);
    }

    private void UpdatePassiveNoise()
    {
        float t = Mathf.InverseLerp(noiseStartsAt, 1f, anxiety);
        t = Mathf.Clamp01(t);

        if (t <= 0f)
        {
            _emitTimer = 0f;
            return;
        }

        _emitTimer += Time.deltaTime;

        float interval = Mathf.Lerp(maxEmitInterval, minEmitInterval, t);
        if (_emitTimer < interval) return;

        _emitTimer = 0f;

        Vector3 emitPos = head ? head.position : (transform.position + Vector3.up * 1.6f);

        // Loudness in meters, scaled by occlusion
        float meters = Mathf.Lerp(minNoiseMeters, maxNoiseMeters, t) * Mathf.Clamp01(occlusionMultiplier);

        // IMPORTANT: source must be the player's GO so Monster sets lastNoiseWasPlayer correctly.
        Noise.MakeNoise(emitPos, meters, gameObject);
    }
        private void OnGUI()
    {
        if (!debugShowOverlay) return;

        Rect bg = new Rect(
            debugOverlayPos.x,
            debugOverlayPos.y,
            debugOverlaySize.x,
            debugOverlaySize.y
        );

        float t = Mathf.Clamp01(anxiety);

        // Background
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.Box(bg, GUIContent.none);

        // Fill
        Color fillColor = Color.Lerp(debugLowColor, debugHighColor, t);
        GUI.color = fillColor;

        GUI.Box(
            new Rect(bg.x, bg.y, bg.width * t, bg.height),
            GUIContent.none
        );

        // Labels
        GUI.color = Color.white;
        GUI.Label(
            new Rect(bg.x + 6, bg.y - 18, bg.width, 18),
            "ANXIETY"
        );

        GUI.Label(
            new Rect(bg.x + 6, bg.y + 2, bg.width, bg.height),
            $"{(t * 100f):0}%"
        );

        GUI.color = old;
    }

}
