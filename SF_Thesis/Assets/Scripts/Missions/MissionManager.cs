using System.Collections;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Start")]
    [SerializeField] private MissionDefinition startingMission;
    [SerializeField] private int startingStepIndex = 0;
    [SerializeField] private MissionDefinition[] allMissions;
    public bool IsStepActive => stepActive;

    [Header("Refs")]
    [SerializeField] private GameObject player;

    public MissionDefinition CurrentMission { get; private set; }
    public int CurrentStepIndex { get; private set; } = -1;

    private MissionContext ctx;
    private bool stepActive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ctx = new MissionContext(player);
    }

    private void OnEnable()
    {
        GameEvents.OnEvent += OnGameEvent;
    }

    private void OnDisable()
    {
        GameEvents.OnEvent -= OnGameEvent;
    }

    private void Start()
{
    // If a checkpoint was just restored on this scene load, do NOT auto-start
    if (CheckpointManager.Instance != null && CheckpointManager.Instance.RestoreJustHappened)
        return;

    if (startingMission != null)
        StartMission(startingMission, startingStepIndex);
}


    public void StartMission(MissionDefinition mission, int stepIndex = 0)
    {
        CurrentMission = mission;
        CurrentStepIndex = Mathf.Clamp(stepIndex, 0, mission.steps.Length - 1);
        StartCurrentStep();
    }
  


    public MissionStepDefinition GetCurrentStep()
    {
        if (CurrentMission == null) return null;
        if (CurrentStepIndex < 0 || CurrentStepIndex >= CurrentMission.steps.Length) return null;
        return CurrentMission.steps[CurrentStepIndex];
    }

    private void StartCurrentStep()
    {
        var step = GetCurrentStep();
        if (step == null) { stepActive = false; return; }

        stepActive = true;

        Debug.Log($"[Mission] Step START: {step.stepId} | {step.objectiveText}");

        GameEvents.Raise("mission.step.started", step);

        ExecuteActions(step.onStart);
    }

    private void CompleteCurrentStep()
{
    var step = GetCurrentStep();
    if (step == null) return;

    stepActive = false;

    Debug.Log($"[Mission] Step COMPLETE: {step.stepId}");

    // Remember what we were completing
    var completingMission = CurrentMission;
    var completingIndex = CurrentStepIndex;

    ExecuteActions(step.onComplete);

    // If an action started a new mission (or changed step), stop here.
    if (CurrentMission != completingMission || CurrentStepIndex != completingIndex)
        return;

    GameEvents.Raise("mission.step.completed", step);

    CurrentStepIndex++;

    if (CurrentMission != null && CurrentStepIndex < CurrentMission.steps.Length)
    {
        StartCurrentStep();
    }
    else
    {
        Debug.Log($"[Mission] MISSION COMPLETE: {CurrentMission.missionId}");
        GameEvents.Raise("mission.completed", CurrentMission);
    }
}


    private void ExecuteActions(StepAction[] actions)
    {
        if (actions == null) return;
        foreach (var a in actions)
        {
            if (a == null) continue;
            a.Execute(ctx);
        }
    }

    private void OnGameEvent(GameEvents.EventData e)
    {
        var step = GetCurrentStep();
        if (step == null) return;

        if (!stepActive && step.strictWhileActive)
            return;

        if (!string.IsNullOrEmpty(step.completeOnEventKey) &&
            e.key == step.completeOnEventKey)
        {
            CompleteCurrentStep();
        }
    }
    // ===== DEBUG API =====
    public void DebugSkipStep()
    {
        if (GetCurrentStep() == null) return;
        CompleteCurrentStep();
    }

    public void DebugJumpToStep(int index)
    {
        if (CurrentMission == null) return;
        CurrentStepIndex = Mathf.Clamp(index, 0, CurrentMission.steps.Length - 1);
        StartCurrentStep();
    }

    public void DebugRestartMission()
    {
        if (CurrentMission == null) return;
        CurrentStepIndex = 0;
        StartCurrentStep();
    }
    public void RestoreMissionFromCheckpoint(string missionId, int stepIndex, bool wasStepActive)
{
    var mission = FindMissionById(missionId);
    if (mission == null)
    {
        Debug.LogWarning($"[Mission] Can't restore. Unknown missionId '{missionId}'.");
        return;
    }

    CurrentMission = mission;
    CurrentStepIndex = Mathf.Clamp(stepIndex, 0, mission.steps.Length - 1);

    // IMPORTANT: do NOT call StartCurrentStep() -> would re-run onStart actions
    stepActive = wasStepActive;

    Debug.Log($"[Mission] Restored to {CurrentMission.missionId} step {CurrentStepIndex} (active={stepActive})");

    StopCoroutine(nameof(NotifyObjectiveUiAfterRestore));
    StartCoroutine(NotifyObjectiveUiAfterRestore());
}

private IEnumerator NotifyObjectiveUiAfterRestore()
{
    // wait 1 frame so ObjectiveUI can subscribe in OnEnable
    yield return null;

    var step = GetCurrentStep();
    if (step != null)
        GameEvents.Raise("mission.step.started", step);
}


private MissionDefinition FindMissionById(string id)
{
    if (string.IsNullOrEmpty(id)) return null;

    if (allMissions != null)
    {
        for (int i = 0; i < allMissions.Length; i++)
        {
            var m = allMissions[i];
            if (m != null && m.missionId == id)
                return m;
        }
    }

    // fallback: if the current mission matches, allow it
    if (CurrentMission != null && CurrentMission.missionId == id)
        return CurrentMission;

    return null;
}

}
