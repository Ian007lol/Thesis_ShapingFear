using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Audio/Play OneShot At Player")]
public class PlayOneShotAtPlayerAction : StepAction
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;

    public override void Execute(MissionContext ctx)
    {
        if (clip == null) return;
        if (ctx.playerTransform == null)
        {
            Debug.LogWarning("[PlayOneShotAtPlayerAction] Player transform missing in MissionContext.");
            return;
        }

        AudioSource.PlayClipAtPoint(clip, ctx.playerTransform.position, volume);
    }
}
