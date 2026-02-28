using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Start Mission")]
public class StartMissionAction : StepAction
{
    [Header("Next mission")]
    public MissionDefinition nextMission;

    [Header("Optional")]
    public int startStepIndex = 0;

    public override void Execute(MissionContext ctx)
    {
        if (nextMission == null)
        {
            Debug.LogWarning("[StartMissionAction] nextMission is not assigned.");
            return;
        }

        MissionManager.Instance.StartMission(nextMission, startStepIndex);
    }
}
