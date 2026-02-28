using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Bridge/Toggle (BridgeRegistry)")]
public class Bridge_ToggleAction : StepAction
{
    public string bridgeId;

    public override void Execute(MissionContext ctx)
    {
        if (!BridgeRegistry.TryGetBridge(bridgeId, out var bridge) || bridge == null)
        {
            Debug.LogWarning($"[Bridge_ToggleAction] Bridge not found: '{bridgeId}'");
            return;
        }

        bridge.Toggle();
    }
}
