using System;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Set Component Enabled (WorldRegistry)")]
public class SetComponentEnabledAction : StepAction
{
    [Header("Target")]
    public string targetId;

    [Header("Component")]
    [Tooltip("Exact component type name, e.g. MonsterLayer2_HearingChase or NavMeshAgent")]
    public string componentTypeName;

    [Header("State")]
    public bool enabledState = true;

    public override void Execute(MissionContext ctx)
    {
        var go = WorldRegistry.Get(targetId?.Trim());
        if (go == null)
        {
            Debug.LogWarning($"[SetComponentEnabledAction] Target not found: '{targetId}'");
            return;
        }

        if (string.IsNullOrWhiteSpace(componentTypeName))
        {
            Debug.LogWarning("[SetComponentEnabledAction] componentTypeName is empty.");
            return;
        }

        // Try Unity type lookup first (works for fully-qualified names too)
        var type = Type.GetType(componentTypeName);

        // If not found, search loaded assemblies by name (handles non-qualified names)
        if (type == null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(componentTypeName);
                if (type != null) break;
            }
        }

        if (type == null)
        {
            Debug.LogWarning($"[SetComponentEnabledAction] Type not found: '{componentTypeName}'");
            return;
        }

        var behaviour = go.GetComponent(type) as Behaviour;
        if (behaviour == null)
        {
            Debug.LogWarning($"[SetComponentEnabledAction] Component '{type.Name}' not found on '{go.name}' (id '{targetId}')");
            return;
        }

        behaviour.enabled = enabledState;
    }
}
