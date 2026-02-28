using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Set Collider Enabled (WorldRegistry)")]
public class SetColliderEnabledAction : StepAction
{
    public string targetId;
    public bool enabledState = true;

    public override void Execute(MissionContext ctx)
    {
        var go = WorldRegistry.Get(targetId);
        if (go == null)
        {
            Debug.LogWarning($"[SetColliderEnabledAction] Target not found: '{targetId}'");
            return;
        }

        var col = go.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[SetColliderEnabledAction] No Collider on '{targetId}'");
            return;
        }

        col.enabled = enabledState;
    }
}
