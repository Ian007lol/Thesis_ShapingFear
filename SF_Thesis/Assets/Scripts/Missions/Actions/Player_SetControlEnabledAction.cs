using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Player/Set Control Enabled")]
public class Player_SetControlEnabledAction : StepAction
{
    public bool enabledState = false;

    public override void Execute(MissionContext ctx)
    {
        if (ctx == null || ctx.player == null)
        {
            Debug.LogWarning("[Player_SetControlEnabledAction] MissionContext or Player is null.");
            return;
        }

        var group = ctx.player.GetComponent<PlayerControlGroup>();
        if (group == null)
        {
            Debug.LogWarning("[Player_SetControlEnabledAction] PlayerControlGroup missing on Player.");
            return;
        }

        group.SetEnabled(enabledState);
    }
}
