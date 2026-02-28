using UnityEngine;

[ExecuteAlways]
public class WaypointGizmo : MonoBehaviour
{
    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.cyan;
    public float radius = 0.25f;
    public bool drawLabel = true;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, radius);

#if UNITY_EDITOR
        if (drawLabel)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (radius * 2f),
                gameObject.name
            );
        }
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius * 1.5f);
    }
}
