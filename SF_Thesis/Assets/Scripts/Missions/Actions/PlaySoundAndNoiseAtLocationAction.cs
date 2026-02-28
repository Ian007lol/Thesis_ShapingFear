using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Audio/Play Sound + Make Noise At Location")]
public class PlaySoundAndNoiseAtLocationAction : StepAction
{
    [Header("Audio")]
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Location")]
    [Tooltip("WorldRegistry key (e.g. speaker_corridor_01)")]
    public string locationId;

    [Header("Noise (monster hearing)")]
    [Tooltip("How loud the noise is (interpreted as radius in meters).")]
    public float loudness = 8f;

    [Tooltip("If true, uses the location GameObject as the noise source (useful for decoy logic).")]
    public bool useLocationAsSource = true;

    public override void Execute(MissionContext ctx)
    {
        if (clip == null)
        {
            Debug.LogWarning("[PlaySoundAndNoiseAtLocationAction] No AudioClip assigned.");
            return;
        }

        var go = WorldRegistry.Get(locationId?.Trim());
        if (go == null)
        {
            Debug.LogWarning($"[PlaySoundAndNoiseAtLocationAction] Location not found: '{locationId}'");
            return;
        }

        Vector3 pos = go.transform.position;

        // 1) Play audible sound for the player
        AudioSource.PlayClipAtPoint(clip, pos, volume);

        // 2) Emit "Noise" for the monster AI
        float r = Mathf.Max(0f, loudness);
        if (r > 0f)
        {
            Noise.MakeNoise(pos, r, useLocationAsSource ? go : null);
        }
    }
}
