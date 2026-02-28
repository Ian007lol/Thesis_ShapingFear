using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
/// Put this on any Rigidbody prop to emit Noise on impacts (e.g., bottles/cans/crates).
[RequireComponent(typeof(Rigidbody))]
public class ImpactNoiseEmitter : MonoBehaviour
{
    [Header("Noise Mapping (meters)")]
    [Tooltip("Minimum collision impulse (N·s) required to make any sound.")]
    public float minImpulse = 2.0f;
    [Tooltip("Base loudness (meters) when crossing the threshold.")]
    public float baseLoudness = 4f;
    [Tooltip("Extra loudness per unit impulse above threshold.")]
    public float loudnessPerImpulse = 0.8f;
    [Tooltip("Clamp max loudness so extreme impacts don't alert the whole map.")]
    public float maxLoudness = 20f;

    [Header("Cooldown / Spam Control")]
    [Tooltip("Minimum time between emissions.")]
    public float emitCooldown = 0.15f;
    private float lastEmitTime = -999f;

    [Header("Surface Tweaks (optional)")]
    [Tooltip("Multiply loudness by surface factor based on PhysicMaterial name substrings.")]
    public SurfaceMultiplier[] surfaceMultipliers = new SurfaceMultiplier[]
    {
        new SurfaceMultiplier("metal", 1.3f),
        new SurfaceMultiplier("concrete", 1.0f),
        new SurfaceMultiplier("wood", 1.1f),
        new SurfaceMultiplier("carpet", 0.5f),
        new SurfaceMultiplier("glass", 1.6f)
    };

    [Header("Audio (optional, for player feedback)")]
    public AudioSource impactOneShot;  // assign a clip; we’ll randomize pitch/volume
    [Range(0.8f,1.2f)] public float pitchJitter = 1.05f;
    [Range(0.6f,1.0f)] public float volumeAtThreshold = 0.8f;

    [Header("Debug")]
    public bool drawGizmos = false;

    Rigidbody rb;

    [System.Serializable]
    public struct SurfaceMultiplier
    {
        public string materialNameContains;
        public float loudnessMultiplier;
        public SurfaceMultiplier(string s, float m) { materialNameContains = s; loudnessMultiplier = m; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision c)
    {
        // Unity reports total impulse for this collision pair
        float impulse = c.impulse.magnitude; // N·s (kg·m/s)

        if (impulse < minImpulse) return;
        if (Time.time < lastEmitTime + emitCooldown) return;

        // Pick a representative contact point (closest to average)
        Vector3 p = c.GetContact(0).point;

        // Respect safe areas: if the impact point is inside, do not emit
        if (SafeAreaVolume.IsPointInside(p)) return;

        // Map impulse -> loudness (meters)
        float loud = baseLoudness + (impulse - minImpulse) * loudnessPerImpulse;

        // Surface multiplier from the other collider's PhysicMaterial name
        float mult = 1f;
        var otherPM = c.collider.sharedMaterial;
        if (otherPM != null && surfaceMultipliers != null)
        {
            string name = otherPM.name.ToLowerInvariant();
            for (int i = 0; i < surfaceMultipliers.Length; i++)
            {
                if (!string.IsNullOrEmpty(surfaceMultipliers[i].materialNameContains) &&
                    name.Contains(surfaceMultipliers[i].materialNameContains.ToLowerInvariant()))
                {
                    mult = surfaceMultipliers[i].loudnessMultiplier;
                    break;
                }
            }
        }

        loud = Mathf.Clamp(loud * mult, 0f, maxLoudness);

        // Fire noise event
        Noise.MakeNoise(p, loud, gameObject);
        lastEmitTime = Time.time;

        // Optional audio feedback
        if (impactOneShot != null)
        {
            impactOneShot.pitch = Random.Range(1f / pitchJitter, pitchJitter);
            // Scale volume softly with loudness within [threshold..max]
            float t = Mathf.InverseLerp(baseLoudness, maxLoudness, loud);
            impactOneShot.volume = Mathf.Lerp(0.3f, volumeAtThreshold, t);
            impactOneShot.Play(); // assumes it's a 2D/3D AudioSource on the prop
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        // simple visual to sanity-check noise radius preview at last known point is not tracked here
    }
#endif
}
