using UnityEngine;
using UnityEngine.InputSystem;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class MouseLook : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerBody; // drag the Player root here

    [Header("Look")]
    [SerializeField] private InputActionReference lookAction; // Player/Look
    [SerializeField] private float mouseSensitivity = 0.15f;      // tweak for feel (mouse)
    [SerializeField] private float controllerSensitivity = 120f;  // deg/sec for gamepad
    [SerializeField] private float minPitch = -90f;
    [SerializeField] private float maxPitch = 90f;
    [SerializeField] private float maxDegreesPerFrame = 25f;      // clamp spikes

    [Header("Cursor")]
    [SerializeField] private bool lockOnStart = true;

    private float yaw;   // horizontal angle around Y
    private float pitch; // vertical angle around X

    private void Awake()
    {
        // Make sure the look action is enabled globally so others (vent script) can also read it
        if (lookAction != null)
            lookAction.action.Enable();
    }

    private void Start()
    {
        if (lockOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Initialize yaw/pitch from current transforms
        SyncFromTransforms();
    }

    private void OnEnable()
    {
        // When re-enabled (e.g. after vent), resync to current orientation
        SyncFromTransforms();
    }

    /// <summary>
    /// Syncs internal yaw/pitch to the current transforms so there are no jumps
    /// when this script is re-enabled after external rotation changes.
    /// </summary>
    public void SyncFromTransforms()
    {
        if (playerBody == null) return;

        // Yaw from body
        yaw = playerBody.rotation.eulerAngles.y;

        // Pitch from camera local rotation, converted to signed angle
        float rawPitch = transform.localRotation.eulerAngles.x;
        if (rawPitch > 180f) rawPitch -= 360f;
        pitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);
    }

    private void Update()
    {
        if (lookAction == null || playerBody == null) return;

        Vector2 look = lookAction.action.ReadValue<Vector2>();

        // Separate mouse vs controller feeling
        bool isGamepad = Gamepad.current != null &&
                         lookAction.action.activeControl != null &&
                         lookAction.action.activeControl.device == Gamepad.current;

        Vector2 delta;
        if (isGamepad)
        {
            // Sticks: -1..1, scale by dt (deg/sec)
            delta = look * controllerSensitivity * Time.deltaTime;
        }
        else
        {
            // Mouse/delta: already per-frame delta; no deltaTime
            delta = look * mouseSensitivity;
        }

        // Clamp crazy spikes (alt-tab, focus change, etc.)
        float mag = delta.magnitude;
        if (mag > maxDegreesPerFrame && mag > 0.001f)
        {
            delta *= (maxDegreesPerFrame / mag);
        }

        // Apply to yaw/pitch
        yaw += delta.x;
        pitch -= delta.y;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Apply rotations
        playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
    public void SetLookAngles(float newYaw, float newPitch, bool clampPitch = true)
    {
        yaw = newYaw;
        pitch = clampPitch ? Mathf.Clamp(newPitch, minPitch, maxPitch) : newPitch;

        if (playerBody != null)
            playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void GetLookAngles(out float outYaw, out float outPitch)
    {
        outYaw = yaw;
        outPitch = pitch;
    }
}
