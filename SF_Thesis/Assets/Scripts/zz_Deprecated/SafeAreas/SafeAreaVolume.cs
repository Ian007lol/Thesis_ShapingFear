using System.Collections.Generic;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(Collider))]
public class SafeAreaVolume : MonoBehaviour
{
    private static readonly List<Collider> _volumes = new List<Collider>();
    public static bool IsPointInside(Vector3 worldPos)
    {
        for (int i = 0; i < _volumes.Count; i++)
        {
            var col = _volumes[i];
            if (col == null) continue;
            // Fast check using ClosestPoint; inside if closest point is the point itself (or extremely close)
            Vector3 cp = col.ClosestPoint(worldPos);
            if ((cp - worldPos).sqrMagnitude < 0.0001f) return true;
        }
        return false;
    }

    private Collider _col;

    private void Reset()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true; // ensure this doesn't block the player
    }

    private void OnEnable()
    {
        if (_col == null) _col = GetComponent<Collider>();
        if (!_volumes.Contains(_col)) _volumes.Add(_col);
    }

    private void OnDisable()
    {
        _volumes.Remove(_col);
    }
    
public static class SafeAreaHelpers
{
    // Returns the nearest reachable NavMesh point outside a safe area, near 'insidePoint'.
    public static bool TryGetNearestBoundary(Vector3 insidePoint, float searchRadius, int areaMask, out UnityEngine.AI.NavMeshHit boundaryHit, out Vector3 guardPoint, float standOff = 0.25f)
    {
        // Grab nearest point on the NavMesh (this will be on the boundary of the carved hole)
        if (UnityEngine.AI.NavMesh.SamplePosition(insidePoint, out var nearest, searchRadius, areaMask))
        {
            // Find the closest edge & its normal (points outward from the walkable)
            if (UnityEngine.AI.NavMesh.FindClosestEdge(nearest.position, out boundaryHit, areaMask))
            {
                guardPoint = boundaryHit.position + boundaryHit.normal * standOff;
                return true;
            }
        }
        boundaryHit = default;
        guardPoint = Vector3.zero;
        return false;
    }
}
}
