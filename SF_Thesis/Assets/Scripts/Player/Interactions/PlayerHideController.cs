using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerHideController : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference interactAction;   // Player/Interact (E)

    [Header("Camera / Look")]
    public Transform playerCamera;                // your camera (for raycast & seating alignment)

    [Header("Detection")]
    public float useRayLength = 2.2f;             // how far we raycast to find a locker
    public LayerMask lockerMask = ~0;             // layer of lockers

    [Header("Enter/Exit Motion")]
    public float enterLerpTime = 0.35f;           // time to slide into seatPoint
    public float exitLerpTime  = 0.25f;

    [Header("Noise Dampening While Hidden")]
    [Range(0.05f, 1f)] public float lockerNoiseMultiplier = 0.35f; // <1 = quieter

    // Refs to your movement scripts (disable while hidden)
    public FirstPersonController movement;        // your existing controller
    public MonoBehaviour lookController;          // whatever script handles look

    // Runtime
    public bool IsHidden { get; private set; }
    private Locker currentLocker;
    private CharacterController cc;

    // Optional: cache your emitters to dampen noise at source
    private PlayerMicNoiseEmitter mic;
    private PlayerFootstepNoiseEmitter steps; // your footsteps script name

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        mic = GetComponent<PlayerMicNoiseEmitter>();
        steps = GetComponent<PlayerFootstepNoiseEmitter>();
        if (!playerCamera) playerCamera = Camera.main?.transform;
    }

    void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteract;
            interactAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteract;
            interactAction.action.Disable();
        }
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (IsHidden)
        {
            if (currentLocker != null) StartCoroutine(ExitLockerRoutine(currentLocker));
            return;
        }

        if (TryFindLocker(out Locker locker))
        {
            if (!locker.IsOccupied && locker.CanUse(transform))
                StartCoroutine(EnterLockerRoutine(locker));
        }
    }

    bool TryFindLocker(out Locker locker)
    {
        locker = null;
        if (!playerCamera) return false;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, useRayLength, lockerMask, QueryTriggerInteraction.Collide))
        {
            locker = hit.collider.GetComponentInParent<Locker>() ?? hit.collider.GetComponent<Locker>();
            return locker != null;
        }
        return false;
    }

    IEnumerator EnterLockerRoutine(Locker locker)
    {
        locker.MarkOccupied(true);
        currentLocker = locker;

        // Open
        locker.SetOpen(true);
        yield return new WaitForSeconds(locker.openTime);

        // Disable movement/look and lerp into seat
        if (movement) movement.enabled = false;
        if (lookController) lookController.enabled = false;

        // Disable CC while we teleport/lerp to avoid pushing/physics interference
        cc.enabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = locker.seatPoint.position;
        Quaternion endRot = locker.seatPoint.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, enterLerpTime);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.position = endPos; transform.rotation = endRot;

        // Close
        locker.SetOpen(false);
        yield return new WaitForSeconds(locker.closeTime);

        // Re-enable CC (keep movement disabled while hidden)
        cc.enabled = true;
        IsHidden = true;

        ApplyNoiseDampening(true);
    }

    IEnumerator ExitLockerRoutine(Locker locker)
    {
        // Open first
        locker.SetOpen(true);
        yield return new WaitForSeconds(locker.openTime);

        // Slide a bit out
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 outPos = locker.seatPoint.position + locker.seatPoint.forward * 0.6f;
        Quaternion outRot = Quaternion.LookRotation(locker.seatPoint.forward, Vector3.up);

        cc.enabled = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, exitLerpTime);
            transform.position = Vector3.Lerp(startPos, outPos, t);
            transform.rotation = Quaternion.Slerp(startRot, outRot, t);
            yield return null;
        }

        transform.position = outPos; transform.rotation = outRot;

        // Re-enable movement/look
        cc.enabled = true;
        if (movement) movement.enabled = true;
        if (lookController) lookController.enabled = true;

        // Close
        locker.SetOpen(false);
        yield return new WaitForSeconds(locker.closeTime);

        // Clear state
        IsHidden = false;
        ApplyNoiseDampening(false);
        locker.MarkOccupied(false);
        currentLocker = null;
    }

    void ApplyNoiseDampening(bool on)
    {
        float mult = on ? lockerNoiseMultiplier : 1f;

        if (mic)   mic.occlusionMultiplier   = mult; // field you added earlier
        if (steps) steps.occlusionMultiplier = mult; // field you added earlier
    }
}
