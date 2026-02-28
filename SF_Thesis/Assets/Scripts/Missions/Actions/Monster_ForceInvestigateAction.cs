using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Monster/Force Investigate (Registry)")]
public class Monster_ForceInvestigateAction : StepAction
{
    public string monsterId;
    public string markerId; // where to investigate

    public override void Execute(MissionContext ctx)
    {
        var monsterGO = WorldRegistry.Get(monsterId?.Trim());
        var markerGO  = WorldRegistry.Get(markerId?.Trim());

        if (monsterGO == null)
        {
            Debug.LogWarning($"[Monster_ForceInvestigateAction] Monster not found: '{monsterId}'");
            return;
        }
        if (markerGO == null)
        {
            Debug.LogWarning($"[Monster_ForceInvestigateAction] Marker not found: '{markerId}'");
            return;
        }

        var mc = monsterGO.GetComponent<MonsterController>();
        if (mc == null)
        {
            Debug.LogWarning($"[Monster_ForceInvestigateAction] MonsterController missing on '{monsterGO.name}'");
            return;
        }

        mc.ForceInvestigate(markerGO.transform.position);
    }
}
