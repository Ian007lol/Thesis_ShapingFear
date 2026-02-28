using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[RequireComponent(typeof(SaveId))]
public class MonsterCheckpointSavable : MonoBehaviour, ICheckpointSavable
{
    private SaveId _id;
    private MonsterScript _ai;

    public string SaveKey => $"Monster:{_id.Id}";

    private void Awake()
    {
        _id = GetComponent<SaveId>();
        _ai = GetComponent<MonsterScript>();

        if (_ai == null)
            Debug.LogError("[MonsterCheckpointSavable] MonsterLayer2_HearingChase missing!", this);
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

        var st = JsonUtility.FromJson<MonsterScript.MonsterSaveState>(json);
        _ai.RestoreStateFromCheckpoint(st);
    }
}
