using UnityEngine;
using UnityEngine.InputSystem; // only used if you assign InputActionReferences (optional)
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class DebugCheckpointController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CheckpointManager checkpointManager;

    [Header("Keyboard (old Input) - optional")]
    [SerializeField] private bool enableLegacyKeyboard = true;
    [SerializeField] private KeyCode saveKey = KeyCode.PageDown;
    [SerializeField] private KeyCode loadKey = KeyCode.PageUp;

    [Header("Input System - optional")]
    [Tooltip("Optional: bind to an action like 'Debug/SaveCheckpoint'")]
    [SerializeField] private InputActionReference saveAction;
    [Tooltip("Optional: bind to an action like 'Debug/LoadCheckpoint'")]
    [SerializeField] private InputActionReference loadAction;

    [Header("Checkpoint Id")]
    [Tooltip("Used when saving via debug. Example: cp_intro, cp_lab, etc.")]
    [SerializeField] private string checkpointId = "debug";

    [Header("On-screen Debug UI (IMGUI)")]
    [SerializeField] private bool showOnScreenButtons = true;

    private void Awake()
    {
        if (checkpointManager == null)
            checkpointManager = CheckpointManager.Instance;
    }

    private void OnEnable()
    {
        if (saveAction != null) saveAction.action.Enable();
        if (loadAction != null) loadAction.action.Enable();
    }

    private void OnDisable()
    {
        if (saveAction != null) saveAction.action.Disable();
        if (loadAction != null) loadAction.action.Disable();
    }

    private void Update()
    {
        if (checkpointManager == null) return;

        // New Input System actions
        if (saveAction != null && saveAction.action.WasPerformedThisFrame())
            Save();

        if (loadAction != null && loadAction.action.WasPerformedThisFrame())
            Load();

        // Old keyboard fallback
        if (enableLegacyKeyboard)
        {
            if (Input.GetKeyDown(saveKey)) Save();
            if (Input.GetKeyDown(loadKey)) Load();
        }
    }

    private void Save()
    {
        // If you kept CreateCheckpoint(string) use it.
        // Otherwise call CreateCheckpoint().

        // Prefer a readable id, but add time so you can see it in logs.
        string id = $"{checkpointId}:{System.DateTime.UtcNow:HHmmss}";
        checkpointManager.CreateCheckpoint(id);

        Debug.Log($"[DebugCheckpoint] Saved '{id}'");
    }

    private void Load()
    {
        checkpointManager.LoadLastCheckpoint();
        Debug.Log("[DebugCheckpoint] Load requested");
    }

    private void OnGUI()
    {
        if (!showOnScreenButtons || checkpointManager == null) return;

        const int w = 220;
        const int h = 34;
        int x = 12;
        int y = 12;

        GUI.Box(new Rect(x, y, w, 120), "Checkpoint Debug");
        y += 28;

        GUI.Label(new Rect(x + 10, y, w - 20, 20), $"Save: {saveKey}   Load: {loadKey}");
        y += 22;

        checkpointId = GUI.TextField(new Rect(x + 10, y, w - 20, 22), checkpointId);
        y += 28;

        if (GUI.Button(new Rect(x + 10, y, w - 20, h), "Save Checkpoint"))
            Save();
        y += h + 6;

        if (GUI.Button(new Rect(x + 10, y, w - 20, h), "Load Last Checkpoint"))
            Load();
    }
}
