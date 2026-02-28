using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Cutscene/Enter Mode")]
public class Cutscene_EnterModeAction : StepAction
{
    [Tooltip("WorldRegistry id to use as tracking target (monster, marker, etc).")]
    public string trackingTargetId;

    public bool showLetterbox = true;
    public bool lockPlayer = true;

    public override void Execute(MissionContext ctx)
    {
        if (CutsceneDirector.Instance == null)
        {
            Debug.LogWarning("[Cutscene_EnterModeAction] CutsceneDirector missing in scene.");
            return;
        }

        Transform t = null;

        var go = WorldRegistry.Get(trackingTargetId?.Trim());
        if (go != null) t = go.transform;

        if (t == null && ctx != null && ctx.playerTransform != null)
            t = ctx.playerTransform;

        CutsceneDirector.Instance.Enter(t, showLetterbox, lockPlayer);
    }
}
