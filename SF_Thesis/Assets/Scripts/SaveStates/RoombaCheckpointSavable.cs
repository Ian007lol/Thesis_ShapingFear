using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(SaveId))]
public class RoombaCheckpointSavable : MonoBehaviour, ICheckpointSavable
{
    private SaveId _id;
    private RoombaPatrolAndVision _ai;

    public string SaveKey => $"Roomba:{_id.Id}";

    private void Awake()
    {
        _id = GetComponent<SaveId>();
        _ai = GetComponent<RoombaPatrolAndVision>();

        if (_ai == null)
            Debug.LogError("[RoombaCheckpointSavable] RoombaPatrolAndVision missing!", this);
    }

    private void OnEnable() => CheckpointSavableRegistry.Register(this);
    private void OnDisable() => CheckpointSavableRegistry.Unregister(this);

    public string CaptureJson()
    {
        if (_ai == null) return "";
        var st = _ai.CaptureStateForCheckpoint();
        return JsonUtility.ToJson(st);
    }

    public void RestoreFromJson(string json)
    {
        if (_ai == null) return;
        if (string.IsNullOrWhiteSpace(json)) return;

        var st = JsonUtility.FromJson<RoombaPatrolAndVision.RoombaSaveState>(json);
        _ai.RestoreStateFromCheckpoint(st);
    }
}
