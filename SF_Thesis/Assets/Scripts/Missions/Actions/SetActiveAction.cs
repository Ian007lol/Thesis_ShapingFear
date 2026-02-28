using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Set Active (WorldRegistry)")]
public class SetActiveAction : StepAction
{
    public string targetId;
    public bool setActive = true;

    public override void Execute(MissionContext ctx)
    {
        var go = WorldRegistry.Get(targetId);
        if (go == null)
        {
            Debug.LogWarning($"[SetActiveAction] Target not found: '{targetId}'");
            return;
        }
        go.SetActive(setActive);
    }
}
