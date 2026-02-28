using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Bridge/Extend (BridgeRegistry)")]
public class Bridge_ExtendAction : StepAction
{
    public string bridgeId;

    public override void Execute(MissionContext ctx)
    {
        if (!BridgeRegistry.TryGetBridge(bridgeId, out var bridge) || bridge == null)
        {
            Debug.LogWarning($"[Bridge_ExtendAction] Bridge not found: '{bridgeId}'");
            return;
        }

        bridge.Extend();
    }
}
