using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Cutscene/Exit Mode")]
public class Cutscene_ExitModeAction : StepAction
{
    public bool hideLetterbox = true;
    public bool unlockPlayer = true;

    public override void Execute(MissionContext ctx)
    {
        if (CutsceneDirector.Instance == null)
        {
            Debug.LogWarning("[Cutscene_ExitModeAction] CutsceneDirector missing in scene.");
            return;
        }

        CutsceneDirector.Instance.Exit(hideLetterbox, unlockPlayer);
    }
}
