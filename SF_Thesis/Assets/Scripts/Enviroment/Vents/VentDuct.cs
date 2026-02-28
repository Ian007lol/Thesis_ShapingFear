using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
/// Represents one vent shaft with two ends (A and B).
/// You can talk into either end; the sound comes out the opposite end.
public class VentDuct : MonoBehaviour
{
    [Header("Ends")]
    public Transform endA;
    public Transform endB;

    public Transform GetClosestEnd(Vector3 worldPos, out Transform oppositeEnd)
    {
        oppositeEnd = null;
        if (endA == null || endB == null)
            return null;

        float dA = Vector3.SqrMagnitude(worldPos - endA.position);
        float dB = Vector3.SqrMagnitude(worldPos - endB.position);

        if (dA <= dB)
        {
            oppositeEnd = endB;
            return endA;
        }
        else
        {
            oppositeEnd = endA;
            return endB;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (endA)
        {
            DrawThickSphere(endA.position, 0.14f, Color.cyan);
            DrawThickArrow(endA.position, endA.forward, Color.cyan);
        }

        if (endB)
        {
            DrawThickSphere(endB.position, 0.14f, Color.magenta);
            DrawThickArrow(endB.position, endB.forward, Color.magenta);
        }

        if (endA && endB)
        {
            Handles.color = Color.yellow;
            Handles.DrawAAPolyLine(4f, endA.position, endB.position);
        }
    }

    // ----------------------
    // THICK ARROW DRAWING
    // ----------------------
    private void DrawThickArrow(Vector3 pos, Vector3 dir, Color color,
                                float length = 0.55f, float thickness = 6f)
    {
        dir.Normalize();
        Vector3 tip = pos + dir * length;

        Handles.color = color;

        // Arrow shaft
        Handles.DrawAAPolyLine(thickness, pos, tip);

        // Arrow head
        Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left  = Quaternion.LookRotation(dir) * Quaternion.Euler(0, -150, 0) * Vector3.forward;

        Handles.DrawAAPolyLine(thickness, tip, tip + right * (length * 0.28f));
        Handles.DrawAAPolyLine(thickness, tip, tip + left  * (length * 0.28f));
    }

    // ----------------------
    // THICK SPHERE DRAWING
    // ----------------------
    private void DrawThickSphere(Vector3 center, float radius, Color color, float thickness = 5f)
    {
        Handles.color = color;

        // Horizontal circle
        Handles.DrawWireDisc(center, Vector3.up, radius);

        // Vertical circles (forward & right planes)
        Handles.DrawWireDisc(center, Vector3.forward, radius);
        Handles.DrawWireDisc(center, Vector3.right, radius);

        // Reinforce with AA lines (thick version)
        Handles.DrawAAPolyLine(thickness,
            center + Vector3.right * radius,
            center - Vector3.right * radius);

        Handles.DrawAAPolyLine(thickness,
            center + Vector3.forward * radius,
            center - Vector3.forward * radius);

        Handles.DrawAAPolyLine(thickness,
            center + Vector3.up * radius,
            center - Vector3.up * radius);
    }
#endif
}
