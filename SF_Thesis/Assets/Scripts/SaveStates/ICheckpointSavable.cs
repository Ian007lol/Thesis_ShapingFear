//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public interface ICheckpointSavable
{
    string SaveKey { get; }
    string CaptureJson();
    void RestoreFromJson(string json);
}
