using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Monster/Teleport To Marker (Registry)")]
public class Monster_TeleportToMarkerAction : StepAction
{
    public string monsterId;
    public string markerId;
    public bool matchRotation = true;

    public override void Execute(MissionContext ctx)
    {
        var monsterGO = WorldRegistry.Get(monsterId?.Trim());
        var markerGO  = WorldRegistry.Get(markerId?.Trim());

        if (monsterGO == null)
        {
            Debug.LogWarning($"[Monster_TeleportToMarkerAction] Monster not found: '{monsterId}'");
            return;
        }
        if (markerGO == null)
        {
            Debug.LogWarning($"[Monster_TeleportToMarkerAction] Marker not found: '{markerId}'");
            return;
        }

        var mc = monsterGO.GetComponent<MonsterController>();
        if (mc == null)
        {
            Debug.LogWarning($"[Monster_TeleportToMarkerAction] MonsterController missing on '{monsterGO.name}'");
            return;
        }

        mc.TeleportTo(markerGO.transform, matchRotation);
    }
}
