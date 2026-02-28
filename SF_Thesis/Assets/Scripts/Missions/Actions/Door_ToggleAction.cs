using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Door/Toggle (DoorRegistry)")]
public class Door_ToggleAction : StepAction
{
    public string doorId;

    public override void Execute(MissionContext ctx)
    {
        if (!DoorRegistry.TryGetDoor(doorId, out var door) || door == null)
        {
            Debug.LogWarning($"[Door_ToggleAction] Door not found: '{doorId}'");
            return;
        }

        door.ToggleDoor();
    }
}
