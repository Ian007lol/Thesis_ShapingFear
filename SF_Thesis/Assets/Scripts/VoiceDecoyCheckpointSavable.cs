using System;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(SaveId))]
[RequireComponent(typeof(VoiceDecoy))]
public class VoiceDecoyCheckpointSavable : MonoBehaviour, ICheckpointSavable
{
    [Serializable]
    public struct DecoySaveState
    {
        // transform
        public float px, py, pz;
        public float rx, ry, rz, rw;

        // minimal behaviour flags (NO audio)
        public bool hasPlayedOnce;
        public bool isHeld; // optional; you can ignore on restore
    }

    private SaveId _id;
    private VoiceDecoy _decoy;

    public string SaveKey => $"VoiceDecoy:{_id.Id}";

    private void Awake()
    {
        _id = GetComponent<SaveId>();
        _decoy = GetComponent<VoiceDecoy>();
    }

    private void OnEnable()  => CheckpointSavableRegistry.Register(this);
    private void OnDisable() => CheckpointSavableRegistry.Unregister(this);

    public string CaptureJson()
    {
        var st = new DecoySaveState();

        var p = transform.position;
        var r = transform.rotation;
        st.px = p.x; st.py = p.y; st.pz = p.z;
        st.rx = r.x; st.ry = r.y; st.rz = r.z; st.rw = r.w;

        st.hasPlayedOnce = _decoy.GetHasPlayedOnce();
        st.isHeld = _decoy.GetIsHeld(); // optional

        return JsonUtility.ToJson(st);
    }

    public void RestoreFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        var st = JsonUtility.FromJson<DecoySaveState>(json);

        // Make sure it won't keep doing old runtime stuff after restore
        _decoy.ResetRuntimeForCheckpointRestore();

        transform.position = new Vector3(st.px, st.py, st.pz);
        transform.rotation = new Quaternion(st.rx, st.ry, st.rz, st.rw);

        // We restore this mainly to avoid collision autoplay quirks
        _decoy.SetHasPlayedOnce(st.hasPlayedOnce);

        // SAFEST: decoy is always not-held after restore
        _decoy.SetHeld(false);

        // SAFEST: always empty clip (no audio persistence)
        _decoy.ClearClip();
    }
}
