using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
/// <summary>
/// Draws Roomba vision cones on top of a map RectTransform,
/// clipped by walls using the same obstructionMask as the Roombas.
/// Attach this as a child of your map background.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RoombaVisionMapGraphic : Graphic
{
    [Header("Map")]
    [Tooltip("MapTrackerUI that defines world bounds and map rect.")]
    [SerializeField] private MapTrackerUI mapTracker;

    [Header("Style")]
    [Tooltip("Color of the Roomba vision cones on the map.")]
    [SerializeField] private Color coneColor = new Color(1f, 1f, 0f, 0.25f);
    [Tooltip("Number of segments for the arc (higher = smoother, but more raycasts).")]
    [SerializeField] private int segmentsPerCone = 20;

    private RectTransform _rectTransform;

    protected override void Awake()
    {
        base.Awake();
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // Roombas move, so refresh vertices every frame
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (mapTracker == null || RoombaPatrolAndVision.AllRoombas.Count == 0)
            return;

        Rect rect = _rectTransform.rect;
        Vector2 size = rect.size;

        Vector2 worldMin = mapTracker.WorldMin;
        Vector2 worldMax = mapTracker.WorldMax;

        // helper: world XZ → local UI pos (centered rect)
        Vector2 WorldToMapXZ(Vector3 world)
        {
            float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, world.x);
            float nz = Mathf.InverseLerp(worldMin.y, worldMax.y, world.z);

            float x = (nx - 0.5f) * size.x;
            float y = (nz - 0.5f) * size.y;

            return new Vector2(x, y);
        }

        foreach (var roomba in RoombaPatrolAndVision.AllRoombas)
        {
            if (roomba == null) continue;

            Vector3 eyeWorld   = roomba.EyePosition;
            Vector3 forwardXZ  = roomba.ForwardXZ;
            float   range      = roomba.VisionRange;
            float   angleDeg   = roomba.VisionAngle;
            var     wallMask   = roomba.ObstructionMask;

            float halfFov = angleDeg * 0.5f;

            // center of cone in UI space
            Vector2 center = WorldToMapXZ(eyeWorld);

            int baseIndex = vh.currentVertCount;

            // center vertex
            UIVertex cVert = UIVertex.simpleVert;
            cVert.position = center;
            cVert.color    = coneColor;
            vh.AddVert(cVert);

            // arc vertices, clipped by walls
            for (int i = 0; i <= segmentsPerCone; i++)
            {
                float t = (float)i / segmentsPerCone;
                float angle = Mathf.Lerp(-halfFov, halfFov, t);

                // world-space direction in XZ
                Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 worldDir = rot * forwardXZ;
                worldDir.y = 0f;
                if (worldDir.sqrMagnitude < 0.0001f)
                    worldDir = forwardXZ;

                worldDir.Normalize();

                float rayDist = range;

                // raycast against walls; if hit, clip cone at the hit point
                if (Physics.Raycast(eyeWorld, worldDir, out RaycastHit hit, range, wallMask, QueryTriggerInteraction.Ignore))
                {
                    rayDist = hit.distance;
                }

                Vector3 worldPoint = eyeWorld + worldDir * rayDist;
                Vector2 uiPoint    = WorldToMapXZ(worldPoint);

                UIVertex v = UIVertex.simpleVert;
                v.position = uiPoint;
                v.color    = coneColor;
                vh.AddVert(v);
            }

            // fan triangles (center → arc)
            for (int i = 0; i < segmentsPerCone; i++)
            {
                int i0 = baseIndex;         // center
                int i1 = baseIndex + 1 + i;
                int i2 = baseIndex + 1 + i + 1;

                vh.AddTriangle(i0, i1, i2);
            }
        }
    }
}
