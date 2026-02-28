using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Audio/Play OneShot At Location")]
public class PlayOneShotAtLocationAction : StepAction
{
    [Header("Audio")]
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Location")]
    [Tooltip("WorldRegistry key (e.g. speaker_01, radio_hallway)")]
    public string locationId;

    public override void Execute(MissionContext ctx)
    {
        if (clip == null)
        {
            Debug.LogWarning("[PlayOneShotAtLocationAction] No AudioClip assigned.");
            return;
        }

        var go = WorldRegistry.Get(locationId?.Trim());
        if (go == null)
        {
            Debug.LogWarning($"[PlayOneShotAtLocationAction] Location not found: '{locationId}'");
            return;
        }

        AudioSource.PlayClipAtPoint(clip, go.transform.position, volume);
    }
}
