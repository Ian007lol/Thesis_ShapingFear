using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Step")]
public class MissionStepDefinition : ScriptableObject
{
    [Header("IDs / UI")]
    public string stepId;
    [TextArea] public string objectiveText;

    [Header("Completion")]
    public string completeOnEventKey;   // e.g. "interact.used.security_pc"
    public bool strictWhileActive = true;

    [Header("Actions")]
    public StepAction[] onStart;
    public StepAction[] onComplete;
}
