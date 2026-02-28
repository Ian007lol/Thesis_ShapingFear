using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
/// Simple on-screen overlay that reads from a MicLevelTap and shows dB.
public class MicDebugOverlay : MonoBehaviour
{
    public MicDebugMeter tap;                 // assign the component on your mic AudioSource
    public bool showOverlay = true;
    public Vector2 overlayPos = new Vector2(20, 20);
    public Vector2 overlaySize = new Vector2(240, 18);
    public float smoothing = 10f;

    float displayedDb;

    void Update()
    {
        if (!tap) return;
        displayedDb = Mathf.Lerp(displayedDb, tap.dbFS, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
    }

    void OnGUI()
    {
        if (!showOverlay || !tap) return;

        var rect = new Rect(overlayPos.x, overlayPos.y, overlaySize.x, overlaySize.y);
        float t = Mathf.InverseLerp(-60f, 0f, displayedDb); // -60..0 dB → 0..1

        // bg
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(rect, GUIContent.none);

        // bar
        GUI.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        GUI.Box(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(t), rect.height), GUIContent.none);

        // labels
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 6, rect.y - 18, rect.width, 18), "Mic (tap)");
        GUI.Label(new Rect(rect.x + 6, rect.y + 2, rect.width, rect.height), $"{displayedDb:0.0} dBFS  (peak {tap.peak:0.00})");

        GUI.color = old;
    }
}
