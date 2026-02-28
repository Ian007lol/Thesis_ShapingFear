using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class PlayerTabletMap : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("The 3D tablet root (with the world-space map canvas as child).")]
    [SerializeField] private Transform tabletRoot;
    [Tooltip("Player camera transform. Tablet is positioned relative to this.")]
    [SerializeField] private Transform playerCamera;

    [Header("Tablet Poses (local to camera)")]
    [Tooltip("Where the tablet rests when hidden (local to camera).")]
    [SerializeField] private Transform hiddenPose;
    [Tooltip("Where the tablet appears when shown (local to camera).")]
    [SerializeField] private Transform shownPose;

    [Header("Input")]
    [Tooltip("Action used to toggle the map (e.g. Keyboard/M).")]
    [SerializeField] private InputActionReference mapToggleAction; // Player/Map

    [Header("Animation")]
    [Tooltip("Time in seconds to move between hidden and shown poses.")]
    [SerializeField] private float moveDuration = 0.25f;
    [Tooltip("Curve for the tablet movement (0..1).")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isShown;
    private Coroutine _moveRoutine;

    private void Reset()
    {
        if (Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    private void Awake()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (tabletRoot != null && playerCamera != null)
        {
            tabletRoot.SetParent(playerCamera, worldPositionStays: false);

            // Snap to hidden at start
            if (hiddenPose != null)
            {
                tabletRoot.localPosition = hiddenPose.localPosition;
                tabletRoot.localRotation = hiddenPose.localRotation;
                tabletRoot.gameObject.SetActive(false);
                _isShown = false;
            }
        }
    }

    private void OnEnable()
    {
        if (mapToggleAction != null)
        {
            mapToggleAction.action.performed += OnMapToggle;
            mapToggleAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (mapToggleAction != null)
        {
            mapToggleAction.action.performed -= OnMapToggle;
            mapToggleAction.action.Disable();
        }

        // ✅ If tablet is currently shown, hide it when this component is disabled.
        // If it’s already hidden, do nothing.
        if (_isShown)
            ForceHide(immediate: false);
    }

    private void OnMapToggle(InputAction.CallbackContext ctx)
    {
        Toggle();
    }

    // --- Public API (call this instead of trying to simulate Tab) ---

    public void Toggle()
    {
        if (tabletRoot == null || playerCamera == null || hiddenPose == null || shownPose == null)
            return;

        SetShown(!_isShown);
    }

    public void ForceHide(bool immediate = false)
    {
        if (tabletRoot == null || playerCamera == null || hiddenPose == null)
            return;

        if (!_isShown && !tabletRoot.gameObject.activeSelf)
            return; // already hidden

        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        _isShown = false;

        // Snap instantly if requested (useful for scene unloads)
        if (immediate || moveDuration <= 0.001f || shownPose == null)
        {
            if (tabletRoot.parent != playerCamera)
                tabletRoot.SetParent(playerCamera, worldPositionStays: false);

            tabletRoot.localPosition = hiddenPose.localPosition;
            tabletRoot.localRotation = hiddenPose.localRotation;
            tabletRoot.gameObject.SetActive(false);
            return;
        }

        // Otherwise animate to hidden
        _moveRoutine = StartCoroutine(AnimateTablet(false));
    }

    private void SetShown(bool show)
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        _moveRoutine = StartCoroutine(AnimateTablet(show));
    }

    private IEnumerator AnimateTablet(bool show)
    {
        _isShown = show;

        if (tabletRoot.parent != playerCamera)
            tabletRoot.SetParent(playerCamera, worldPositionStays: false);

        // Ensure active while animating
        if (!tabletRoot.gameObject.activeSelf)
            tabletRoot.gameObject.SetActive(true);

        Vector3 startPos = tabletRoot.localPosition;
        Quaternion startRot = tabletRoot.localRotation;

        Vector3 endPos = show ? shownPose.localPosition : hiddenPose.localPosition;
        Quaternion endRot = show ? shownPose.localRotation : hiddenPose.localRotation;

        float t = 0f;
        float duration = Mathf.Max(0.01f, moveDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = moveCurve.Evaluate(Mathf.Clamp01(t));

            tabletRoot.localPosition = Vector3.Lerp(startPos, endPos, k);
            tabletRoot.localRotation = Quaternion.Slerp(startRot, endRot, k);

            yield return null;
        }

        tabletRoot.localPosition = endPos;
        tabletRoot.localRotation = endRot;

        if (!show)
            tabletRoot.gameObject.SetActive(false);

        _moveRoutine = null;
    }
}
