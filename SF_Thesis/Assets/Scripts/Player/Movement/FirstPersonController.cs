using UnityEngine;
using UnityEngine.InputSystem;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;    // Player/Move
    [SerializeField] private InputActionReference sprintAction;  // Player/Sprint
    [SerializeField] private InputActionReference crouchAction;  // Player/Crouch

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private bool allowSprint = true;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Crouch")]
    [SerializeField] private bool crouchIsToggle = false;   // false = hold-to-crouch
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float standHeight = 2.0f;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float heightSmoothTime = 0.06f;
    [SerializeField] private float standCenterY = 1.0f;     // usually height/2 - skin
    [SerializeField] private float crouchCenterY = 0.6f;
    [SerializeField] private LayerMask headObstructionMask = ~0; // everything by default

    private CharacterController controller;
    private Vector3 velocity;
    private float heightVel;     // for SmoothDamp
    private float centerYVel;    // for SmoothDamp
    private bool crouchHeld;
    private bool crouchToggled;
    private bool isCrouching;

    // Expose to HeadBob
    public bool IsCrouching => isCrouching;
    public float HorizontalSpeed { get; private set; }
    public bool IsGrounded => controller.isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
        if (crouchAction != null)
        {
            crouchAction.action.Enable();
            if (crouchIsToggle)
                crouchAction.action.performed += OnCrouchToggle;
        }
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        if (sprintAction != null) sprintAction.action.Disable();
        if (crouchAction != null)
        {
            if (crouchIsToggle)
                crouchAction.action.performed -= OnCrouchToggle;
            crouchAction.action.Disable();
        }
    }

    private void Update()
    {
        // --- Read input ---
        Vector2 move2D = moveAction.action.ReadValue<Vector2>();
        Vector3 move = transform.right * move2D.x + transform.forward * move2D.y;
        if (move.sqrMagnitude > 1f) move.Normalize();

        bool sprinting = allowSprint && sprintAction != null && sprintAction.action.IsPressed();

        // Crouch state
        crouchHeld = (crouchAction != null) && crouchAction.action.IsPressed();
        bool targetCrouch = crouchIsToggle ? crouchToggled : crouchHeld;

        // Prevent standing if there is something above head
        if (!targetCrouch && !CanStandUp())
            targetCrouch = true;

        isCrouching = targetCrouch;

        float speed = isCrouching ? walkSpeed * crouchSpeedMultiplier : (sprinting ? sprintSpeed : walkSpeed);
        Vector3 frameMove = move * speed * Time.deltaTime;
        controller.Move(frameMove);

        HorizontalSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;

        // --- Gravity ---
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = groundedGravity;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // --- Smooth height & center ---
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCenterY = isCrouching ? crouchCenterY : standCenterY;

        float newHeight = Mathf.SmoothDamp(controller.height, targetHeight, ref heightVel, heightSmoothTime);
        float newCenterY = Mathf.SmoothDamp(controller.center.y, targetCenterY, ref centerYVel, heightSmoothTime);

        // Apply
        controller.height = newHeight;
        controller.center = new Vector3(controller.center.x, newCenterY, controller.center.z);
    }

    private void OnCrouchToggle(InputAction.CallbackContext ctx)
    {
        crouchToggled = !crouchToggled;
    }

    private bool CanStandUp()
    {
        // Check capsule above current crouched head to see if standing height would collide
        float radius = controller.radius - 0.01f;
        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 desiredTop = bottom + Vector3.up * (standHeight - radius * 2f);
        // Use CheckCapsule to test proposed standing capsule
        return !Physics.CheckCapsule(bottom, desiredTop, radius, headObstructionMask, QueryTriggerInteraction.Ignore);
    }
     /// <summary>
    /// Force the player to stand up immediately (used by vent, etc.).
    /// Ignores CanStandUp() because we are about to teleport anyway.
    /// </summary>
    public void ForceStandInstant()
    {
        isCrouching = false;
        crouchToggled = false;
        crouchHeld = false;

        float targetHeight = standHeight;
        float targetCenterY = standCenterY;

        controller.height = targetHeight;
        controller.center = new Vector3(controller.center.x, targetCenterY, controller.center.z);
    }

    /// <summary>
    /// Force horizontal speed to zero so systems like HeadBob stop "running".
    /// </summary>
    public void ForceZeroHorizontalSpeed()
    {
        HorizontalSpeed = 0f;
        // Optional: also kill horizontal velocity so you don't keep sliding
        velocity.x = 0f;
        velocity.z = 0f;
    }
}
