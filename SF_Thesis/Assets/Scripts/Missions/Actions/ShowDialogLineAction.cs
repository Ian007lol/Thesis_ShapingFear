using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/UI/Show Dialog Line")]
public class ShowDialogLineAction : StepAction
{
    [TextArea(2, 8)]
    public string message;

    public bool clearPrevious = true;

    public override void Execute(MissionContext ctx)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var dialog = Object.FindFirstObjectByType<VoskDialogText>();
        if (dialog == null)
        {
            Debug.LogWarning("[ShowDialogLineAction] No VoskDialogText found in scene.");
            return;
        }

        dialog.ForceDialog(message, clearPrevious);
    }
}
