using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Raise Event")]
public class RaiseEventAction : StepAction
{
    public string eventKey;

    public override void Execute(MissionContext ctx)
    {
        GameEvents.Raise(eventKey);
    }
}
