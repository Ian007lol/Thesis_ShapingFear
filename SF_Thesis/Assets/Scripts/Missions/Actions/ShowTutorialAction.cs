using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Show Tutorial")]
public class ShowTutorialAction : StepAction
{
    public string tutorialId;

    public override void Execute(MissionContext ctx)
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
        {
            Debug.LogWarning("[ShowTutorialAction] tutorialId is empty.");
            return;
        }

        GameEvents.Raise($"tutorial.show.{tutorialId}");
    }
}
