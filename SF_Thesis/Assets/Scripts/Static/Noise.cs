using System;
using UnityEngine;

public static class Noise
{
    public struct NoiseEvent
    {
        public Vector3 position;
        public float loudness;   // interpreted as radius in meters
        public GameObject source;
    }

    public static event Action<NoiseEvent> OnNoise;

    // --- Debug visualization settings --- Generated with ChatGPT
    public static bool debugDraw = true;
    public static float debugDuration = 0.35f;    // seconds each circle stays visible
    public static Color debugColor = new Color(0.2f, 0.8f, 1f, 1f);
    public static int debugSegments = 32;         // how smooth the circle looks
    public static bool debugDrawVerticalRings = true; // show 3D-ish sphere outline

    
    public static void MakeNoise(Vector3 position, float loudness, GameObject source = null)
    {
        if (SafeAreaVolume.IsPointInside(position)) return;

        // Dispatch event to listeners
        OnNoise?.Invoke(new NoiseEvent
        {
            position = position,
            loudness = loudness,
            source = source
        });

        // Optional debug visualization
        if (debugDraw && loudness > 0f)
        {
            DrawNoiseDebug(position, loudness); // Method G. w. ChatGPT
        }
    }

    private static void DrawNoiseDebug(Vector3 center, float radius) // Method G. w. ChatGPT
    {
        int segments = Mathf.Max(4, debugSegments);
        float step = Mathf.PI * 2f / segments;

        // --- Horizontal ring (XZ plane) ---
        for (int i = 0; i < segments; i++)
        {
            float a0 = step * i;
            float a1 = step * (i + 1);

            Vector3 p0 = center + new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);

            Debug.DrawLine(p0, p1, debugColor, debugDuration);
        }

        if (!debugDrawVerticalRings) return;

        // --- Vertical ring (YZ plane) ---
        for (int i = 0; i < segments; i++)
        {
            float a0 = step * i;
            float a1 = step * (i + 1);

            Vector3 p0 = center + new Vector3(0f, Mathf.Sin(a0) * radius, Mathf.Cos(a0) * radius);
            Vector3 p1 = center + new Vector3(0f, Mathf.Sin(a1) * radius, Mathf.Cos(a1) * radius);

            Debug.DrawLine(p0, p1, debugColor, debugDuration);
        }

        // --- Vertical ring (XY plane) ---
        for (int i = 0; i < segments; i++)
        {
            float a0 = step * i;
            float a1 = step * (i + 1);

            Vector3 p0 = center + new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0f);
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0f);

            Debug.DrawLine(p0, p1, debugColor, debugDuration);
        }
    }
}
