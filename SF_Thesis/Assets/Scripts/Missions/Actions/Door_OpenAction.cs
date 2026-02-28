using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Door/Open (DoorRegistry)")]
public class Door_OpenAction : StepAction
{
    public string doorId;

    public override void Execute(MissionContext ctx)
    {
        if (!DoorRegistry.TryGetDoor(doorId, out var door) || door == null)
        {
            Debug.LogWarning($"[Door_OpenAction] Door not found: '{doorId}'");
            return;
        }

        door.OpenDoor();
    }
}
