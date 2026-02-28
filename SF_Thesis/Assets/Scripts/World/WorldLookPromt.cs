using TMPro;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class WorldLookPrompt : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Detection")]
    [Tooltip("Collider to be 'looked at'. If null, uses a collider on parent.")]
    [SerializeField] private Collider targetCollider;

    [Tooltip("Camera used for look ray. If null, Camera.main.")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("Max distance for prompt to show.")]
    [SerializeField] private float maxDistance = 3f;

    [Tooltip("Sphere radius for forgiving aim. 0 = raycast.")]
    [SerializeField] private float aimRadius = 0.15f;

    [Tooltip("Which layers can block line of sight.")]
    [SerializeField] private LayerMask occlusionMask = ~0;

    [Tooltip("If true, prompt won't show when something blocks view.")]
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Billboard")]
    [Tooltip("Keep text upright (ignore camera pitch).")]
    [SerializeField] private bool uprightOnly = true;

    [Header("Text")]
    [SerializeField] private string promptText = "Press E to interact";

    private bool _visible;

    private void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!worldCanvas) worldCanvas = GetComponentInChildren<Canvas>(true);
        if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (!label) label = GetComponentInChildren<TextMeshProUGUI>(true);

        if (targetCollider == null)
        {
            // Try parent first (this prefab is usually a child)
            targetCollider = GetComponentInParent<Collider>();
        }

        if (label) label.text = promptText;

        SetVisible(false);
    }
    private void OnEnable()
    {
        // start hidden (prevents a 1-frame flash if enabled mid-frame)
        SetVisible(false);
    }

    private void OnDisable()
    {
        // HARD reset even if another script disables us while visible
        _visible = false;

        if (worldCanvas) worldCanvas.enabled = false;

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }


    private void LateUpdate()
    {
        if (!playerCamera || targetCollider == null)
        {
            SetVisible(false);
            return;
        }

        // Billboard
        FaceCamera();

        // Look detection
        bool looking = IsPlayerLookingAtTarget();

        SetVisible(looking);
    }

    public void SetText(string text)
    {
        promptText = text;
        if (label) label.text = promptText;
    }

    private void SetVisible(bool v)
    {
        if (_visible == v) return;
        _visible = v;

        if (worldCanvas) worldCanvas.enabled = v;

        // If you prefer CanvasGroup instead:
        if (canvasGroup)
        {
            canvasGroup.alpha = v ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void FaceCamera()
    {
        var camT = playerCamera.transform;

        if (uprightOnly)
        {
            Vector3 toCam = camT.position - transform.position;
            toCam.y = 0f;

            if (toCam.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(camT.position - transform.position);
        }
    }

    private bool IsPlayerLookingAtTarget()
    {
        Vector3 camPos = playerCamera.transform.position;
        Vector3 camFwd = playerCamera.transform.forward;

        // distance gate (to the collider bounds)
        float dist = Vector3.Distance(camPos, targetCollider.bounds.ClosestPoint(camPos));
        if (dist > maxDistance) return false;

        Ray ray = new Ray(camPos, camFwd);

        // Aim test: spherecast or raycast
        RaycastHit hit;
        bool hitSomething = (aimRadius > 0f)
            ? Physics.SphereCast(ray, aimRadius, out hit, maxDistance, ~0, QueryTriggerInteraction.Collide)
            : Physics.Raycast(ray, out hit, maxDistance, ~0, QueryTriggerInteraction.Collide);

        if (!hitSomething) return false;

        // Must hit THIS collider (or a child of its transform)
        bool isTarget =
            hit.collider == targetCollider ||
            hit.collider.transform.IsChildOf(targetCollider.transform);

        if (!isTarget) return false;

        // Optional line-of-sight (occlusion)
        if (requireLineOfSight)
        {
            Vector3 targetPoint = targetCollider.bounds.center;
            Vector3 dir = (targetPoint - camPos);
            float len = dir.magnitude;
            if (len > 0.001f)
            {
                dir /= len;

                // If something blocks (excluding the target), hide
                if (Physics.Raycast(camPos, dir, out RaycastHit block, len, occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    // If blocker is not our target, occluded
                    if (block.collider != targetCollider && !block.collider.transform.IsChildOf(targetCollider.transform))
                        return false;
                }
            }
        }

        return true;
    }
}
