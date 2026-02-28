using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Monster/Set Paused (Registry)")]
public class Monster_SetPausedAction : StepAction
{
    public string monsterId;
    public bool paused = true;

    public override void Execute(MissionContext ctx)
    {
        var monsterGO = WorldRegistry.Get(monsterId?.Trim());
        if (monsterGO == null)
        {
            Debug.LogWarning($"[Monster_SetPausedAction] Monster not found: '{monsterId}'");
            return;
        }

        var mc = monsterGO.GetComponent<MonsterController>();
        if (mc == null)
        {
            Debug.LogWarning($"[Monster_SetPausedAction] MonsterController missing on '{monsterGO.name}'");
            return;
        }

        mc.SetPaused(paused);
    }
}
