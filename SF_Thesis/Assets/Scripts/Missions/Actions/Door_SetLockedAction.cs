using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Door/Set Locked (DoorRegistry)")]
public class Door_SetLockedAction : StepAction
{
    [Tooltip("Door ID like A-01, A-12, etc. Must match your DoorRegistry keys.")]
    public string doorId;

    [Tooltip("True = lock the door (ignore all interaction). False = unlock it.")]
    public bool locked = true;

    public override void Execute(MissionContext ctx)
    {
        if (!DoorRegistry.TryGetDoor(doorId, out var door) || door == null)
        {
            Debug.LogWarning($"[Door_SetLockedAction] Door not found: '{doorId}'");
            return;
        }

        door.SetLocked(locked);
    }
}
