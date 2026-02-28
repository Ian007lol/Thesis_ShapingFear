using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(Rigidbody))]
public class SlideNoiseEmitter : MonoBehaviour
{
    public float minSpeed = 0.8f;
    public float emitInterval = 0.25f; // how often to ping noise while sliding
    public float baseLoudness = 2f;
    public float loudnessPerSpeed = 0.8f;
    public float maxLoudness = 10f;

    private float timer;
    private Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < emitInterval) return;

        // Simple ground check: ray just below
        if (!Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, out var hit, 0.2f))
            return;

        float speed = rb.linearVelocity.magnitude;
        if (speed < minSpeed) return;

        Vector3 p = hit.point;
        if (SafeAreaVolume.IsPointInside(p)) { timer = 0f; return; }

        float loud = Mathf.Clamp(baseLoudness + (speed - minSpeed) * loudnessPerSpeed, 0f, maxLoudness);
        Noise.MakeNoise(p, loud, gameObject);
        timer = 0f;
    }
}
