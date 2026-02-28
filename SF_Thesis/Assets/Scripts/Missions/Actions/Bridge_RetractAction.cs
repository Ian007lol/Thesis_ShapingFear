using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Bridge/Retract (BridgeRegistry)")]
public class Bridge_RetractAction : StepAction
{
    public string bridgeId;

    public override void Execute(MissionContext ctx)
    {
        if (!BridgeRegistry.TryGetBridge(bridgeId, out var bridge) || bridge == null)
        {
            Debug.LogWarning($"[Bridge_RetractAction] Bridge not found: '{bridgeId}'");
            return;
        }

        bridge.Retract();
    }
}
