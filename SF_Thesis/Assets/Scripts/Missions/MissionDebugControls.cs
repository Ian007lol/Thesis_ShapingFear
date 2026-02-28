using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class MissionDebugControls : MonoBehaviour
{
    [Header("Enable/Disable")]
    public bool enableDebugControls = true;

    [Header("Bindings")]
    public KeyCode skipStepKey = KeyCode.F6;
    public KeyCode restartMissionKey = KeyCode.F5;

    [Tooltip("Hold Left Shift + press a number key (1..9) to jump to step index (0..8).")]
    public bool enableJumpWithShiftNumber = true;
    


    private MissionManager mm;

    private void Awake()
    {
        mm = MissionManager.Instance;
    }

    private void Update()
    {
        if (!enableDebugControls) return;

        if (mm == null) mm = MissionManager.Instance;
        if (mm == null) return;

        // Restart mission (F5)
        if (Input.GetKeyDown(restartMissionKey))
        {
            mm.DebugRestartMission();
            Debug.Log("[MissionDebug] Restart mission");
        }
       

        // Skip current step (F6)
        if (Input.GetKeyDown(skipStepKey))
        {
            mm.DebugSkipStep();
            Debug.Log("[MissionDebug] Skip step");
        }

        // Jump to step: Shift + 1..9 => step index 0..8
        if (enableJumpWithShiftNumber && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Jump(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) Jump(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) Jump(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) Jump(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) Jump(4);
            else if (Input.GetKeyDown(KeyCode.Alpha6)) Jump(5);
            else if (Input.GetKeyDown(KeyCode.Alpha7)) Jump(6);
            else if (Input.GetKeyDown(KeyCode.Alpha8)) Jump(7);
            else if (Input.GetKeyDown(KeyCode.Alpha9)) Jump(8);
        }
    }

    private void Jump(int stepIndex)
    {
        mm.DebugJumpToStep(stepIndex);
        Debug.Log($"[MissionDebug] Jump to step index {stepIndex}");
    }
}
