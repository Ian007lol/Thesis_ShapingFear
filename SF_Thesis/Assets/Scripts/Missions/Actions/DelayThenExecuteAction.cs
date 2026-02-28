using System.Collections;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Delay Then Execute Action")]
public class DelayThenExecuteAction : StepAction
{
    [Header("Delay")]
    public float delaySeconds = 3f;

    [Header("Action to execute after delay")]
    public StepAction actionAfterDelay;

    public override void Execute(MissionContext ctx)
    {
        if (actionAfterDelay == null)
        {
            Debug.LogWarning("[DelayThenExecuteAction] actionAfterDelay is not assigned.");
            return;
        }

        // Use MissionManager as the coroutine runner
        MissionManager.Instance.StartCoroutine(Run(ctx));
    }

    private IEnumerator Run(MissionContext ctx)
    {
        yield return new WaitForSeconds(delaySeconds);

        // Execute the wrapped action after delay
        actionAfterDelay.Execute(ctx);
    }
}
