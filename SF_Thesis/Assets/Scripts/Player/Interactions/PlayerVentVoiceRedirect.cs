using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerVentVoiceRedirect : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference interactAction;   // same E as lockers (Player/Interact)
    [Tooltip("Same look action you use in MouseLook (Player/Look).")]
    public InputActionReference lookAction;       // Player/Look

    [Header("Camera")]
    public Transform playerCamera;                // your camera

    [Header("Detection")]
    public float useRayLength = 2.2f;
    public LayerMask ventMask = ~0;               // set to your "Vent" layer

    [Header("Enter/Exit Motion")]
    public float enterLerpTime = 0.25f;
    public float exitLerpTime = 0.25f;
    public bool lockMovementWhileAtVent = true;

    [Header("Noise Redirect Settings")]
    [Range(0.1f, 2f)] public float ventRemoteMultiplier = 1.0f;
    [Range(0.0f, 1f)] public float ventLocalMultiplier = 0.4f;

    [Header("Vent Look")]
    [Tooltip("Allow small head movement while at the vent.")]
    public bool allowLookInsideVent = true;
    [Tooltip("Mouse sensitivity while peeking into vent (no deltaTime).")]
    public float ventLookSensitivity = 0.12f;
    [Tooltip("Maximum left/right look inside vent (degrees).")]
    public float ventYawLimit = 20f;
    [Tooltip("Maximum up look inside vent (degrees).")]
    public float ventPitchUpLimit = 15f;
    [Tooltip("Maximum down look inside vent (degrees).")]
    public float ventPitchDownLimit = 25f;

    [Header("Ground Snap")]
    [Tooltip("Layers considered as walkable ground when entering / exiting vent.")]
    public LayerMask groundMask = ~0;    // set to your floor/environment layers
    [Tooltip("How far down we raycast from the desired position to find the floor.")]
    public float groundCheckDistance = 3f;
    [Tooltip("Vertical offset for raycast start above the desired position.")]
    public float groundRayStartHeight = 1.0f;

    // Movement/look references
    public FirstPersonController movement;        // your movement script
    public MonoBehaviour lookController;          // your look script (e.g. MouseLook)
    private bool isTransitioning;
    private Coroutine ventRoutine;

    // Runtime
    public bool IsAtVent { get; private set; }
    private VentDuct currentVent;
    private Transform currentMouth;               // which end we are at (A or B)
    private Transform currentOpposite;            // the other end
    private CharacterController cc;
    private PlayerMicNoiseEmitter mic;

    // Vent head-look offsets (local camera rotation)
    private float ventYawOffset;                  // left/right around Y
    private float ventPitchOffset;                // up/down around X

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        mic = GetComponent<PlayerMicNoiseEmitter>();
        if (!playerCamera) playerCamera = Camera.main?.transform;
    }

    void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteract;
            interactAction.action.Enable();
        }
        // lookAction is enabled globally by MouseLook; we just read it.
    }

    void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteract;
            interactAction.action.Disable();
        }

        // Safety: reset redirect on disable
        if (mic != null)
        {
            mic.voiceRedirectTarget = null;
            mic.ventRemoteMultiplier = 1f;
            mic.ventLocalMultiplier = 1f;
        }

        IsAtVent = false;
        currentVent = null;
        currentMouth = null;
        currentOpposite = null;
        ventYawOffset = 0f;
        ventPitchOffset = 0f;

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.identity;
    }

    void Update()
    {
        // While at vent, allow small head movement independent of normal MouseLook
        if (IsAtVent && allowLookInsideVent && lookAction != null && playerCamera != null)
        {
            Vector2 look = lookAction.action.ReadValue<Vector2>();
            // Mouse / delta already per frame → no deltaTime
            Vector2 delta = look * ventLookSensitivity;

            // Horizontal (yaw) and vertical (pitch) offsets
            ventYawOffset += delta.x;
            ventPitchOffset -= delta.y;

            ventYawOffset = Mathf.Clamp(ventYawOffset, -ventYawLimit, ventYawLimit);

            // Positive pitchOffset = look up, negative = look down.
            float upLimit = Mathf.Abs(ventPitchUpLimit);
            float downLimit = Mathf.Abs(ventPitchDownLimit);
            ventPitchOffset = Mathf.Clamp(ventPitchOffset, -downLimit, upLimit);

            playerCamera.localRotation = Quaternion.Euler(ventPitchOffset, ventYawOffset, 0f);
        }
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        // Block spam while moving in/out
        if (isTransitioning) return;

        if (IsAtVent)
        {
            if (currentVent != null)
                StartVentRoutine(ExitVentRoutine());
            return;
        }

        if (TryFindVent(out VentDuct vent, out Transform mouth, out Transform opposite))
        {
            StartVentRoutine(EnterVentRoutine(vent, mouth, opposite));
        }
    }
    private void StartVentRoutine(IEnumerator routine)
    {
        if (ventRoutine != null)
            StopCoroutine(ventRoutine);

        ventRoutine = StartCoroutine(routine);
    }
    bool TryFindVent(out VentDuct vent, out Transform mouth, out Transform opposite)
    {
        vent = null;
        mouth = null;
        opposite = null;

        if (!playerCamera) return false;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, useRayLength, ventMask, QueryTriggerInteraction.Collide))
        {
            vent = hit.collider.GetComponentInParent<VentDuct>() ?? hit.collider.GetComponent<VentDuct>();
            if (vent == null) return false;

            // Decide which end (A or B) is closer to the hit point
            mouth = vent.GetClosestEnd(hit.point, out opposite);
            return (mouth != null && opposite != null);
        }
        return false;
    }

    IEnumerator EnterVentRoutine(VentDuct vent, Transform mouth, Transform opposite)
    {
        isTransitioning = true;

        currentVent = vent;
        currentMouth = mouth;
        currentOpposite = opposite;

        // Commit state immediately (so spam E won't start another Enter)
        IsAtVent = true;

        // Optional: clear redirect first (prevents leftovers if something was stuck)
        if (mic != null)
        {
            mic.voiceRedirectTarget = null;
            mic.ventRemoteMultiplier = 1f;
            mic.ventLocalMultiplier = 1f;
        }

        if (movement != null)
        {
            movement.ForceStandInstant();
            movement.ForceZeroHorizontalSpeed();
        }

        if (lockMovementWhileAtVent)
        {
            if (movement) movement.enabled = false;
            if (lookController) lookController.enabled = false;
        }

        cc.enabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = mouth.position;
        Quaternion endRot = mouth.rotation;

        Quaternion startCamRot = playerCamera ? playerCamera.localRotation : Quaternion.identity;
        Quaternion targetCamRot = Quaternion.identity;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, enterLerpTime);
            float k = Mathf.Clamp01(t);

            transform.position = Vector3.Lerp(startPos, endPos, k);
            transform.rotation = Quaternion.Slerp(startRot, endRot, k);

            if (playerCamera != null)
                playerCamera.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, k);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
        cc.enabled = true;

        ventYawOffset = 0f;
        ventPitchOffset = 0f;
        if (playerCamera != null)
            playerCamera.localRotation = targetCamRot;

        // Activate redirect
        if (mic != null)
        {
            mic.voiceRedirectTarget = opposite;
            mic.ventRemoteMultiplier = ventRemoteMultiplier;
            mic.ventLocalMultiplier = ventLocalMultiplier;
        }

        isTransitioning = false;
        ventRoutine = null;
    }

    IEnumerator ExitVentRoutine()
    {
        isTransitioning = true;

        if (currentVent == null || currentMouth == null)
        {
            FullResetVentState();
            isTransitioning = false;
            ventRoutine = null;
            yield break;
        }

        // While exiting, we still consider ourselves "at vent" until we're fully out.
        // (So you can't start an Enter again mid-exit.)
        // Keep IsAtVent = true here.

        cc.enabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 outPos = currentMouth.position - currentMouth.forward * 0.6f;

        if (cc != null)
        {
            Vector3 rayStart = outPos + Vector3.up * groundRayStartHeight;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
                                groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                float bottomOffset = cc.center.y - cc.height * 0.5f;
                float targetBottomY = hit.point.y + cc.skinWidth;
                outPos.y = targetBottomY - bottomOffset;
            }
        }

        Quaternion outRot = Quaternion.LookRotation(currentMouth.forward, Vector3.up);

        Quaternion startCamRot = playerCamera ? playerCamera.localRotation : Quaternion.identity;
        Quaternion targetCamRot = Quaternion.identity;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, exitLerpTime);
            float k = Mathf.Clamp01(t);

            transform.position = Vector3.Lerp(startPos, outPos, k);
            transform.rotation = Quaternion.Slerp(startRot, outRot, k);

            if (playerCamera != null)
                playerCamera.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, k);

            yield return null;
        }

        transform.position = outPos;
        transform.rotation = outRot;
        cc.enabled = true;

        ventYawOffset = 0f;
        ventPitchOffset = 0f;
        if (playerCamera != null)
            playerCamera.localRotation = targetCamRot;

        if (lockMovementWhileAtVent)
        {
            if (movement) movement.enabled = true;
            if (lookController)
            {
                lookController.enabled = true;
                if (lookController is MouseLook ml)
                    ml.SyncFromTransforms();
            }
        }

        // Disable redirect
        if (mic != null)
        {
            mic.voiceRedirectTarget = null;
            mic.ventRemoteMultiplier = 1f;
            mic.ventLocalMultiplier = 1f;
        }

        // Now we are truly out
        IsAtVent = false;
        currentVent = null;
        currentMouth = null;
        currentOpposite = null;

        isTransitioning = false;
        ventRoutine = null;
    }
    public void ResetVentStateAfterRespawn()
{
    // Stop any transition coroutine
    if (ventRoutine != null)
    {
        StopCoroutine(ventRoutine);
        ventRoutine = null;
    }

    isTransitioning = false;
    FullResetVentState();
}

    private void FullResetVentState()
    {
        if (mic != null)
        {
            mic.voiceRedirectTarget = null;
            mic.ventRemoteMultiplier = 1f;
            mic.ventLocalMultiplier = 1f;
        }

        IsAtVent = false;
        currentVent = null;
        currentMouth = null;
        currentOpposite = null;

        ventYawOffset = 0f;
        ventPitchOffset = 0f;

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.identity;

        if (lockMovementWhileAtVent)
        {
            if (movement) movement.enabled = true;
            if (lookController) lookController.enabled = true;
        }

        if (cc != null) cc.enabled = true;
    }

}
