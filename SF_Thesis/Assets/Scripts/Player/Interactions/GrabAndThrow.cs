using UnityEngine;
using UnityEngine.InputSystem;

public class GrabAndThrow : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera playerCamera;                  // your player camera
    [SerializeField] private Transform holdPoint;                  // empty transform in front of camera

    // INPUTS — LMB = charge/throw, RMB = cancel
    [SerializeField] private InputActionReference grabAction;      // e.g., E
    [SerializeField] private InputActionReference throwChargeLMB;  // LMB (Hold interaction)
    [SerializeField] private InputActionReference throwCancelRMB;  // RMB (Tap interaction)

    [Header("Grab")]
    [SerializeField] private float grabDistance = 3.0f;
    [SerializeField] private float grabRadius   = 0.35f;
    [SerializeField] private LayerMask grabbableMask = ~0;
    [SerializeField] private float maxGrabMass = 25f;
    [SerializeField] private float maxHoldDistance = 4.0f; // drop if it drifts too far

    [Header("Physics Carry (collides while held)")]
    [SerializeField] private float followSpeed = 18f;         // m/s toward hold point
    [SerializeField] private float maxCarrySpeed = 12f;       // velocity cap
    [SerializeField] private bool  alignRotationToCamera = false;
    [SerializeField] private float rotationFollowSpeed = 12f;

    [Header("Stabilize While Held")]
    [SerializeField] private bool  stabilizeWhileHeld = true;
    [SerializeField] private float heldLinearDrag = 0.2f;
    [SerializeField] private float heldAngularDrag = 8f;
    [SerializeField] private float heldMaxAngularVelocity = 8f;
    [SerializeField] private bool  freezeRotationWhileHeld = false;

    [Header("Throw")]
    [SerializeField] private float minThrowPower = 3f;
    [SerializeField] private float maxThrowPower = 18f;
    [SerializeField] private AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0,0, 1,1);
    [SerializeField] private float maxChargeTime = 1.2f;
    [SerializeField] private float spinTorque = 2.5f;

    [Header("UX")]
    [SerializeField] private float cancelTapGrace = 0.1f; // ignore micro tap right after hold starts

    // runtime
    private Rigidbody heldRB;
    private Transform heldTransform;

    // 🔹 NEW: cached decoy reference (if the held object is a VoiceDecoy)
    private VoiceDecoy heldDecoy;

    // cached physics
    private RigidbodyInterpolation _prevInterpolation;
    private CollisionDetectionMode _prevCollisionMode;
    private bool _prevUseGravity;
    private bool _prevKinematic;
    private float _prevDrag, _prevAngularDrag, _prevMaxAngVel;
    private RigidbodyConstraints _prevConstraints;

    // charge state
    private bool isCharging;
    private bool chargeCanceled;
    private float chargeTimer;
    private float sinceChargeStart;

    private void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!holdPoint)
        {
            var hp = new GameObject("HoldPoint");
            hp.transform.SetParent(playerCamera.transform);
            hp.transform.localPosition = new Vector3(0f, 0f, 1f);
            hp.transform.localRotation = Quaternion.identity;
            holdPoint = hp.transform;
        }
    }

    private void OnEnable()
    {
        if (grabAction)
        {
            grabAction.action.performed += OnGrab;
            grabAction.action.Enable();
        }
        if (throwChargeLMB)
        {
            throwChargeLMB.action.started  += OnThrowChargeStarted;
            throwChargeLMB.action.canceled += OnThrowChargeReleased;
            throwChargeLMB.action.Enable();
        }
        if (throwCancelRMB)
        {
            throwCancelRMB.action.performed += OnThrowCancelTap;
            throwCancelRMB.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (grabAction)
        {
            grabAction.action.performed -= OnGrab;
            grabAction.action.Disable();
        }
        if (throwChargeLMB)
        {
            throwChargeLMB.action.started  -= OnThrowChargeStarted;
            throwChargeLMB.action.canceled -= OnThrowChargeReleased;
            throwChargeLMB.action.Disable();
        }
        if (throwCancelRMB)
        {
            throwCancelRMB.action.performed -= OnThrowCancelTap;
            throwCancelRMB.action.Disable();
        }

        if (heldRB) DropHeld();
    }

    private void Update()
    {
        if (isCharging)
        {
            chargeTimer = Mathf.Min(maxChargeTime, chargeTimer + Time.deltaTime);
            sinceChargeStart += Time.deltaTime;
        }

        // safety: if somehow pulled far from hand, drop it
        if (heldTransform && Vector3.Distance(heldTransform.position, holdPoint.position) > maxHoldDistance * 1.5f)
            DropHeld();
    }

   private void FixedUpdate()
{
    if (!heldRB) return;

    // Default target = hold point
    Vector3 target = holdPoint.position;

    // If the held object is a VoiceDecoy and it's currently
    // being recorded near the mouth, override target
    if (heldDecoy != null && heldDecoy.HasMouthTarget)
    {
        target = heldDecoy.MouthTargetPosition;
    }

    // Physics carry with collisions
    Vector3 to = target - heldRB.position;

    Vector3 desiredVel = to * followSpeed;
    if (desiredVel.magnitude > maxCarrySpeed)
        desiredVel = desiredVel.normalized * maxCarrySpeed;

    Vector3 velDelta = desiredVel - heldRB.linearVelocity;
    heldRB.AddForce(velDelta, ForceMode.VelocityChange);

    if (alignRotationToCamera)
    {
        Quaternion targetRot = holdPoint.rotation;
        Quaternion newRot = Quaternion.Slerp(
            heldRB.rotation,
            targetRot,
            1f - Mathf.Exp(-rotationFollowSpeed * Time.fixedDeltaTime)
        );
        heldRB.MoveRotation(newRot);
    }

    if (stabilizeWhileHeld)
    {
        // hard clamp spin spikes from grazing walls
        float maxAV2 = heldMaxAngularVelocity * heldMaxAngularVelocity;
        if (heldRB.angularVelocity.sqrMagnitude > maxAV2)
            heldRB.angularVelocity = Vector3.ClampMagnitude(
                heldRB.angularVelocity,
                heldMaxAngularVelocity
            );
    }
}

    // -------- Input handlers --------
    private void OnGrab(InputAction.CallbackContext ctx)
    {
        if (heldRB == null) TryPickup();
        else
        {
            if (isCharging) CancelCharge();
            DropHeld();
        }
    }

    private void OnThrowChargeStarted(InputAction.CallbackContext ctx)
    {
        if (!heldRB) return;
        isCharging = true;
        chargeCanceled = false;
        chargeTimer = 0f;
        sinceChargeStart = 0f;
    }

    private void OnThrowChargeReleased(InputAction.CallbackContext ctx)
    {
        if (!heldRB || !isCharging) return;

        if (!chargeCanceled)
        {
            float t = (maxChargeTime <= 0.001f) ? 1f : Mathf.Clamp01(chargeTimer / maxChargeTime);
            float power01 = chargeCurve.Evaluate(t);
            float power = Mathf.Lerp(minThrowPower, maxThrowPower, power01);
            DoThrow(power);
        }

        isCharging = false;
        chargeCanceled = false;
        chargeTimer = 0f;
    }

    private void OnThrowCancelTap(InputAction.CallbackContext ctx)
    {
        if (!isCharging) return;
        if (sinceChargeStart < cancelTapGrace) return; // ignore micro-tap at hold start
        CancelCharge(); // keep holding
    }

    private void CancelCharge()
    {
        isCharging = false;
        chargeCanceled = true;
        chargeTimer = 0f;
    }

    // -------- Core actions --------
    private void TryPickup()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.SphereCast(ray, grabRadius, out RaycastHit hit, grabDistance, grabbableMask, QueryTriggerInteraction.Ignore))
            return;

        var rb = hit.rigidbody;
        if (!rb || rb.mass > maxGrabMass) return;

        heldRB = rb;
        heldTransform = rb.transform;

        // 🔹 NEW: cache VoiceDecoy if present and mark it as held
        heldDecoy = heldTransform.GetComponent<VoiceDecoy>();
        if (heldDecoy != null)
        {
            heldDecoy.SetHeld(true);

            // If you haven't assigned mouthTransform in the prefab,
            // you can optionally auto-assign the player camera here:
            if (heldDecoy != null && heldDecoy.gameObject.TryGetComponent<VoiceDecoy>(out var decoyRef))
            {
                // If mouthTransform is left empty in the inspector, you can set it here
                // but only if you add a public setter in VoiceDecoy.
                // For now, just make sure you've assigned mouthTransform in the prefab.
            }
        }

        // cache physics
        _prevInterpolation   = rb.interpolation;
        _prevCollisionMode   = rb.collisionDetectionMode;
        _prevUseGravity      = rb.useGravity;
        _prevKinematic       = rb.isKinematic;
        _prevDrag            = rb.linearDamping;
        _prevAngularDrag     = rb.angularDamping;
        _prevMaxAngVel       = rb.maxAngularVelocity;
        _prevConstraints     = rb.constraints;

        // enable proper collisions while held
        rb.isKinematic = false;
        rb.useGravity  = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (stabilizeWhileHeld)
        {
            rb.linearDamping = heldLinearDrag;
            rb.angularDamping = heldAngularDrag;
            rb.maxAngularVelocity = heldMaxAngularVelocity;
        }
        if (freezeRotationWhileHeld)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        // place at hand cleanly
        rb.position = holdPoint.position;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void DropHeld()
    {
        if (!heldRB) return;

        // 🔹 Tell decoy it's no longer held (will also auto-stop recording if mid-record)
        if (heldDecoy != null)
        {
            heldDecoy.SetHeld(false);
            heldDecoy = null;
        }

        // restore physics
        heldRB.interpolation = _prevInterpolation;
        heldRB.collisionDetectionMode = _prevCollisionMode;
        heldRB.useGravity = _prevUseGravity;
        heldRB.isKinematic = _prevKinematic;
        heldRB.linearDamping = _prevDrag;
        heldRB.angularDamping = _prevAngularDrag;
        heldRB.maxAngularVelocity = _prevMaxAngVel;
        heldRB.constraints = _prevConstraints;

        heldRB = null;
        heldTransform = null;

        isCharging = false;
        chargeCanceled = false;
        chargeTimer = 0f;
    }

    private void DoThrow(float power)
    {
        if (!heldRB) return;

        // 🔹 On throw, also mark decoy as no longer held
        if (heldDecoy != null)
        {
            heldDecoy.SetHeld(false);
            heldDecoy = null;
        }

        // restore before flight
        heldRB.isKinematic = false;
        heldRB.useGravity = true;
        heldRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        heldRB.interpolation = RigidbodyInterpolation.Interpolate;
        heldRB.linearDamping = _prevDrag;
        heldRB.angularDamping = _prevAngularDrag;
        heldRB.maxAngularVelocity = _prevMaxAngVel;
        heldRB.constraints = _prevConstraints;

        Vector3 dir = playerCamera.transform.forward;
        Vector3 ang = Random.insideUnitSphere * spinTorque;
        // cap random torque a bit
        if (ang.magnitude > spinTorque) ang = ang.normalized * spinTorque;

        heldRB.AddForce(dir * power, ForceMode.VelocityChange);
        heldRB.AddTorque(ang, ForceMode.VelocityChange);

        heldRB = null;
        heldTransform = null;

        isCharging = false;
        chargeCanceled = false;
        chargeTimer = 0f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!playerCamera) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        var start = playerCamera.transform.position;
        var dir = playerCamera.transform.forward;
        Gizmos.DrawLine(start, start + dir * grabDistance);
        Gizmos.DrawWireSphere(start + dir * grabDistance, grabRadius);
    }
#endif
}
