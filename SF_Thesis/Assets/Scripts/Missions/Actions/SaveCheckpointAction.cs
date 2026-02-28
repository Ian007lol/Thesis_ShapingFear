using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Save Checkpoint")]
public class SaveCheckpointAction : StepAction
{
    [Tooltip("Optional: purely for debugging, not required by the manager right now.")]
    public string checkpointLabel = "CP";

    public override void Execute(MissionContext ctx)
    {
        if (CheckpointManager.Instance == null)
        {
            Debug.LogWarning("[SaveCheckpointAction] No CheckpointManager in scene.");
            return;
        }

        CheckpointManager.Instance.CreateCheckpoint();
        Debug.Log($"[SaveCheckpointAction] Checkpoint saved: {checkpointLabel}");
    }
}
