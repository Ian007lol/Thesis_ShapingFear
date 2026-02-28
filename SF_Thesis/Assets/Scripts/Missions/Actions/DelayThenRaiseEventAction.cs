using System.Collections;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Delay Then Raise Event")]
public class DelayThenRaiseEventAction : StepAction
{
    [Header("Delay")]
    public float delaySeconds = 5f;

    [Header("Event")]
    [Tooltip("Event key to raise after the delay")]
    public string eventKey;

    public override void Execute(MissionContext ctx)
    {
        if (string.IsNullOrWhiteSpace(eventKey))
        {
            Debug.LogWarning("[DelayThenRaiseEventAction] Event key is empty.");
            return;
        }

        MissionManager.Instance.StartCoroutine(DelayRoutine());
    }

    private IEnumerator DelayRoutine()
    {
        yield return new WaitForSeconds(delaySeconds);
        GameEvents.Raise(eventKey);
    }
}
