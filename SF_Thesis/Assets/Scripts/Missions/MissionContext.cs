using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class MissionContext
{
    public readonly GameObject player;
    public readonly Transform playerTransform;

    public MissionContext(GameObject player)
    {
        this.player = player;
        this.playerTransform = player != null ? player.transform : null;
    }
}
