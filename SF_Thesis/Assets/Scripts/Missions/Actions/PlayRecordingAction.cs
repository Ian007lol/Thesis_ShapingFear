using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[CreateAssetMenu(menuName = "Missions/Actions/Audio/Play Recording")]
public class PlayRecording : StepAction
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;

    // Optional: reuse a single hidden AudioSource
    private static AudioSource _2dSource;

    public override void Execute(MissionContext ctx)
    {
        if (clip == null) return;

        if (_2dSource == null)
        {
            var go = new GameObject("2D_AudioSource_Runtime");
            Object.DontDestroyOnLoad(go);

            _2dSource = go.AddComponent<AudioSource>();
            _2dSource.spatialBlend = 0f; // 🔑 FULL 2D
            _2dSource.playOnAwake = false;
        }

        _2dSource.volume = volume;
        _2dSource.PlayOneShot(clip);
    }
}
