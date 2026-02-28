using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
/// Attach to the AudioSource that is playing your microphone clip.
/// It exposes live RMS/peak (per audio block) without starting the mic again.
[DisallowMultipleComponent]
public class MicDebugMeter : MonoBehaviour
{
    [Range(1, 8)] public int channelsHint = 1; // purely cosmetic
    public float rms { get; private set; }     // 0..1 (last block)
    public float peak { get; private set; }    // 0..1 (last block)
    public float dbFS { get; private set; }    // -80..0 dBFS approx

    // These are written from the audio thread; read in Update (main thread).
    volatile float _rms, _peak, _db;

    void OnAudioFilterRead(float[] data, int channels)
    {
        // Compute on the audio thread for freshest data.
        double sum = 0.0;
        float p = 0f;
        int n = data.Length;

        for (int i = 0; i < n; i++)
        {
            float s = data[i];
            sum += s * s;
            float a = (s >= 0f) ? s : -s;
            if (a > p) p = a;
        }

        float r = Mathf.Sqrt((float)(sum / Mathf.Max(1, n)));
        _rms = r;
        _peak = p;
        _db = Mathf.Clamp(20f * Mathf.Log10(Mathf.Max(1e-6f, r)), -80f, 0f);
    }

    void Update()
    {
        // Copy thread-safe snapshots for UI
        rms = _rms;
        peak = _peak;
        dbFS = _db;
    }
}
