using UnityEngine;
using UnityEngine.UI;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class MapTrackerUI : MonoBehaviour
{
    [Header("Map Rect")]
    [SerializeField] private RectTransform mapRect;

    [Header("Dots")]
    [SerializeField] private RectTransform playerDot;
    [SerializeField] private RectTransform monsterDot;
    [SerializeField] private Color currentColor = Color.red;
    [SerializeField] private Color lastSeenColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("World Bounds (XZ)")]
    [Tooltip("World-space min XZ shown on the map.")]
    [SerializeField] private Vector2 worldMin = new Vector2(-20, -20);
    [Tooltip("World-space max XZ shown on the map.")]
    [SerializeField] private Vector2 worldMax = new Vector2(20, 20);

    // 🔹 expose these so the cone overlay can reuse them
    public Vector2 WorldMin => worldMin;
    public Vector2 WorldMax => worldMax;

    private void Update()
    {
        if (TrackingManager.Instance == null || mapRect == null) return;

        // Player
        UpdateDot(
            TrackingManager.TrackedType.Player,
            playerDot
        );

        // Monster
        UpdateDot(
            TrackingManager.TrackedType.Monster,
            monsterDot
        );
    }

    private void UpdateDot(TrackingManager.TrackedType type, RectTransform dot)
    {
        if (dot == null) return;

        if (!TrackingManager.Instance.TryGetDisplayInfo(type, out Vector3 worldPos, out bool isCurrent))
        {
            dot.gameObject.SetActive(false);
            return;
        }

        dot.gameObject.SetActive(true);

        // Map world XZ to normalized [0,1] range
        float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
        float nz = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.z); // use z for Y on map

        // Then to mapRect local space
        Vector2 size = mapRect.rect.size;
        Vector2 localPos = new Vector2(
            (nx - 0.5f) * size.x,
            (nz - 0.5f) * size.y
        );

        dot.anchoredPosition = localPos;

        // Color: strong red if currently visible, faded red if last-seen
        var img = dot.GetComponent<SpriteRenderer>();
        if (img != null)
            img.color = isCurrent ? currentColor : lastSeenColor;
    }
}
