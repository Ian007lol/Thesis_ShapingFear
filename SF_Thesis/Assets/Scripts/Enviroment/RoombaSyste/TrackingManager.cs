using System.Collections.Generic;
using UnityEngine;

public class TrackingManager : MonoBehaviour
{
    public enum TrackedType
    {
        Player,
        Monster
    }

    public static TrackingManager Instance { get; private set; }

    [Header("Last Seen Settings")]
    [Tooltip("How long a last-seen marker remains after the target is no longer seen.")]
    public float lastSeenDuration = 5f;

    private class TrackedInfo
    {
        public bool currentlyVisible;
        public Vector3 lastPosition;
        public float lastSeenTime;
    }

    private readonly Dictionary<TrackedType, TrackedInfo> _data =
        new Dictionary<TrackedType, TrackedInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize entries
        foreach (TrackedType t in System.Enum.GetValues(typeof(TrackedType)))
        {
            _data[t] = new TrackedInfo
            {
                currentlyVisible = false,
                lastPosition = Vector3.zero,
                lastSeenTime = -999f
            };
        }
    }

    private void Update()
    {
        float now = Time.time;

        // Decay "currentlyVisible" flags when last seen is too old
        foreach (var kv in _data)
        {
            TrackedInfo info = kv.Value;
            if (info.currentlyVisible && (now - info.lastSeenTime) > Time.deltaTime * 2f)
            {
                // if nobody reported this frame, drop the "currentlyVisible" (but keep last position)
                info.currentlyVisible = false;
            }
        }
    }

    /// <summary>
    /// Called by Roombas when they see a target.
    /// </summary>
    public void ReportSighting(TrackedType type, Vector3 position)
    {
        float now = Time.time;
        if (!_data.TryGetValue(type, out var info))
        {
            info = new TrackedInfo();
            _data[type] = info;
        }

        info.currentlyVisible = true;
        info.lastPosition = position;
        info.lastSeenTime = now;
    }

    /// <summary>
    /// UI calls this to know whether to show a dot for a given type.
    /// Returns true if there is a valid last-seen (still within lastSeenDuration).
    /// </summary>
    public bool TryGetDisplayInfo(TrackedType type, out Vector3 worldPos, out bool isCurrentlyVisible)
    {
        worldPos = Vector3.zero;
        isCurrentlyVisible = false;

        if (!_data.TryGetValue(type, out var info))
            return false;

        float age = Time.time - info.lastSeenTime;
        if (age > lastSeenDuration)
            return false; // too old, hide marker

        worldPos = info.lastPosition;
        isCurrentlyVisible = info.currentlyVisible;
        return true;
    }
}
