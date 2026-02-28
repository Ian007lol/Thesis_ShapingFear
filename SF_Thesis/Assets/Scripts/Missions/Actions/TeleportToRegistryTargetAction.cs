using UnityEngine;
using UnityEngine.AI;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Teleport To (WorldRegistry)")]
public class TeleportToRegistryTargetAction : StepAction
{
    [Header("Mover")]
    public string moverId;

    [Header("Destination")]
    public string destinationId;

    [Header("NavMesh")]
    public bool snapToNavMesh = true;
    public float snapRadius = 2f;

    [Header("Optional")]
    public bool matchRotation = true;

    public override void Execute(MissionContext ctx)
    {
        var mover = WorldRegistry.Get(moverId?.Trim());
        if (mover == null)
        {
            Debug.LogWarning($"[TeleportAction] Mover not found: '{moverId}'");
            return;
        }

        var dest = WorldRegistry.Get(destinationId?.Trim());
        if (dest == null)
        {
            Debug.LogWarning($"[TeleportAction] Destination not found: '{destinationId}'");
            return;
        }

        Vector3 targetPos = dest.transform.position;
        Quaternion targetRot = dest.transform.rotation;

        // If it has a NavMeshAgent, use Warp (best practice)
        var agent = mover.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            if (snapToNavMesh && NavMesh.SamplePosition(targetPos, out var hit, snapRadius, agent.areaMask))
                targetPos = hit.position;

            agent.Warp(targetPos);
        }
        else
        {
            mover.transform.position = targetPos;
        }

        if (matchRotation)
            mover.transform.rotation = targetRot;
    }
}
