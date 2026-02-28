using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Door/Close (DoorRegistry)")]
public class Door_CloseAction : StepAction
{
    public string doorId;

    public override void Execute(MissionContext ctx)
    {
        if (!DoorRegistry.TryGetDoor(doorId, out var door) || door == null)
        {
            Debug.LogWarning($"[Door_CloseAction] Door not found: '{doorId}'");
            return;
        }

        door.CloseDoor();
    }
}
