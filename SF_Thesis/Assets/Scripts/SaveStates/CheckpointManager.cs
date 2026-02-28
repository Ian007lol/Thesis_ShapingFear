using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Collections;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Multi-save settings")]
    [SerializeField] private int maxCheckpointFiles = 5;

    private CheckpointSnapshot _last;

    private string SaveDir => Path.Combine(Application.persistentDataPath, "checkpoints");
    private string IndexPath => Path.Combine(SaveDir, "checkpoint_index.json");

    // Used to stop MissionManager.Start() from overwriting restored state
    public bool RestoreJustHappened { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Directory.CreateDirectory(SaveDir);
    }
    [System.Serializable]
    public struct CheckpointInfo
    {
        public string checkpointId;
        public string sceneName;
        public string utcStamp;
        public string fileName;
    }

    /// <summary>
    /// Returns checkpoints in chronological order: oldest -> newest.
    /// </summary>
    public List<CheckpointInfo> GetCheckpointListChronological()
    {
        var index = LoadIndex();

        var result = new List<CheckpointInfo>(index.entries.Count);

        // index.entries is newest-first. Reverse for chronological (oldest -> newest)
        for (int i = index.entries.Count - 1; i >= 0; i--)
        {
            var e = index.entries[i];
            result.Add(new CheckpointInfo
            {
                checkpointId = e.checkpointId,
                sceneName = e.sceneName,
                utcStamp = e.utcStamp,
                fileName = e.fileName
            });
        }

        return result;
    }

    /// <summary>
    /// Loads a checkpoint by its fileName stored in the index.
    /// </summary>
    public void LoadCheckpointByFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogWarning("[Checkpoint] LoadCheckpointByFileName called with empty fileName.");
            return;
        }

        var entry = new CheckpointIndexEntry { fileName = fileName };
        LoadCheckpointByEntry(entry);
    }
    // -----------------------
    // Public API
    // -----------------------

    public void CreateCheckpoint(string checkpointId)
    {
        Directory.CreateDirectory(SaveDir);

        var snap = BuildSnapshot(checkpointId);
        _last = snap;

        // File per checkpoint
        string safeId = SanitizeFilePart(checkpointId);
        string stamp = System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string fileName = $"checkpoint_{stamp}_{safeId}.json";
        string filePath = Path.Combine(SaveDir, fileName);

        AtomicWriteAllText(filePath, JsonUtility.ToJson(snap, true));

        // Update index (newest first)
        var index = LoadIndex();
        index.entries.Insert(0, new CheckpointIndexEntry
        {
            checkpointId = snap.checkpointId,
            sceneName = snap.sceneName,
            utcStamp = stamp,
            fileName = fileName
        });

        // Remove duplicates by fileName (paranoia) and trim
        DeduplicateIndex(index);
        TrimIndexAndDeleteFiles(index, maxCheckpointFiles);

        AtomicWriteAllText(IndexPath, JsonUtility.ToJson(index, true));

        Debug.Log($"[Checkpoint] Saved '{checkpointId}' to {filePath}");
    }

    public void CreateCheckpoint()
    {
        CreateCheckpoint($"auto:{System.DateTime.UtcNow:yyyyMMdd_HHmmss}");
    }

    public void LoadLastCheckpoint()
    {
        var index = LoadIndex();
        if (index.entries.Count == 0)
        {
            Debug.LogWarning("[Checkpoint] No checkpoint index entries found.");
            return;
        }

        LoadCheckpointByEntry(index.entries[0]);
    }

    public void LoadPreviousCheckpoint()
    {
        var index = LoadIndex();
        if (index.entries.Count < 2)
        {
            Debug.LogWarning("[Checkpoint] No previous checkpoint available.");
            return;
        }

        LoadCheckpointByEntry(index.entries[1]);
    }

    /// <summary>
    /// 0 = newest, 1 = previous, etc.
    /// </summary>
    public void LoadCheckpointAt(int indexFromNewest)
    {
        var index = LoadIndex();
        if (indexFromNewest < 0 || indexFromNewest >= index.entries.Count)
        {
            Debug.LogWarning($"[Checkpoint] Invalid checkpoint index: {indexFromNewest}. Available: 0..{index.entries.Count - 1}");
            return;
        }

        LoadCheckpointByEntry(index.entries[indexFromNewest]);
    }

    // -----------------------
    // Internals
    // -----------------------

    private CheckpointSnapshot BuildSnapshot(string checkpointId)
    {
        var snap = new CheckpointSnapshot();
        snap.sceneName = SceneManager.GetActiveScene().name;
        snap.checkpointId = checkpointId;

        // ===== Mission =====
        var mm = MissionManager.Instance;
        if (mm != null && mm.CurrentMission != null)
        {
            snap.missionId = mm.CurrentMission.missionId;
            snap.missionStepIndex = mm.CurrentStepIndex;
            snap.missionStepActive = mm.IsStepActive;
        }
        else
        {
            snap.missionId = "";
            snap.missionStepIndex = -1;
            snap.missionStepActive = false;
        }

        // ===== Doors =====
        foreach (var door in DoorRegistry.GetAllDoors())
        {
            if (door == null) continue;

            snap.doors.Add(new DoorSnapshot
            {
                id = door.NormalizedId,
                isOpen = door.IsOpen,
                isLocked = door.IsLocked,
                manualOverride = door.ManualOverride

            });
        }

        // ===== Bridges =====
        foreach (var bridge in BridgeRegistry.GetAllBridges())
        {
            if (bridge == null) continue;

            snap.bridges.Add(new BridgeSnapshot
            {
                id = bridge.NormalizedId,
                isExtended = bridge.IsExtended
            });
        }

        // ===== Entities =====
        foreach (var s in CheckpointSavableRegistry.All)
        {
            if (s == null) continue;
            snap.entities.Add(new EntitySnapshot
            {
                key = s.SaveKey,
                json = s.CaptureJson()
            });
        }

        return snap;
    }

    private void LoadCheckpointByEntry(CheckpointIndexEntry entry)
    {
        string filePath = Path.Combine(SaveDir, entry.fileName);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[Checkpoint] Missing checkpoint file: {filePath}");
            return;
        }

        _last = JsonUtility.FromJson<CheckpointSnapshot>(File.ReadAllText(filePath));

        if (_last == null)
        {
            Debug.LogWarning($"[Checkpoint] Failed to parse checkpoint file: {filePath}");
            return;
        }

        if (SceneManager.GetActiveScene().name != _last.sceneName)
        {
            SceneManager.sceneLoaded += OnSceneLoadedRestore;
            SceneManager.LoadScene(_last.sceneName);
            return;
        }

        Restore(_last);
    }

    private void OnSceneLoadedRestore(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedRestore;
        Restore(_last);
    }

    private void Restore(CheckpointSnapshot snap)
    {
        RestoreJustHappened = true;
        StartCoroutine(ClearRestoreFlagEndOfFrame());


        // ===== Restore doors =====
        foreach (var d in snap.doors)
        {
            if (DoorRegistry.TryGetDoor(d.id, out var door))
            {
                door.SetLocked(d.isLocked);
                door.SetManualOverride(d.manualOverride);
                door.SetOpenInstant(d.isOpen);

                // ✅ resync sensors after the world has moved
                StartCoroutine(RebuildDoorsNextFrame());

            }
        }
        var death = FindFirstObjectByType<PlayerDeathHandler>();
        if (death != null)
            death.ClearDeathUIAndReenableControls();

        var vent = FindFirstObjectByType<PlayerVentVoiceRedirect>();
        if (vent != null)
            vent.ResetVentStateAfterRespawn();
        // ===== Restore bridges =====
        foreach (var b in snap.bridges)
        {
            if (BridgeRegistry.TryGetBridge(b.id, out var bridge))
                bridge.SetExtendedInstant(b.isExtended);
        }

        // ===== Restore entities =====
        var map = new Dictionary<string, ICheckpointSavable>();
        foreach (var s in CheckpointSavableRegistry.All)
        {
            if (s != null) map[s.SaveKey] = s;
        }

        foreach (var e in snap.entities)
        {
            if (map.TryGetValue(e.key, out var savable))
                savable.RestoreFromJson(e.json);
            else
                Debug.LogWarning($"[Checkpoint] Missing entity in scene for key: {e.key}");
        }

        // ===== Restore mission (LAST) =====
        if (!string.IsNullOrEmpty(snap.missionId) && MissionManager.Instance != null)
        {
            MissionManager.Instance.RestoreMissionFromCheckpoint(
                snap.missionId,
                snap.missionStepIndex,
                snap.missionStepActive
            );
        }

        Debug.Log($"[Checkpoint] Restored '{snap.checkpointId}'");
    }
    private IEnumerator RebuildDoorsNextFrame()
    {
        yield return null; // wait 1 frame so player/CC transforms are final

        foreach (var door in DoorRegistry.GetAllDoors())
            if (door) door.RebuildOccupantsFromSensors();
    }

    private IEnumerator ClearRestoreFlagEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        RestoreJustHappened = false;
    }

    // -----------------------
    // Index + housekeeping
    // -----------------------

    [System.Serializable]
    private class CheckpointIndex
    {
        public List<CheckpointIndexEntry> entries = new List<CheckpointIndexEntry>();
    }

    [System.Serializable]
    private class CheckpointIndexEntry
    {
        public string checkpointId;
        public string sceneName;
        public string utcStamp;
        public string fileName;
    }

    private CheckpointIndex LoadIndex()
    {
        Directory.CreateDirectory(SaveDir);

        if (!File.Exists(IndexPath))
            return new CheckpointIndex();

        try
        {
            var parsed = JsonUtility.FromJson<CheckpointIndex>(File.ReadAllText(IndexPath));
            return parsed ?? new CheckpointIndex();
        }
        catch
        {
            return new CheckpointIndex();
        }
    }

    private void DeduplicateIndex(CheckpointIndex index)
    {
        var seen = new HashSet<string>();
        for (int i = index.entries.Count - 1; i >= 0; i--)
        {
            var e = index.entries[i];
            if (e == null || string.IsNullOrEmpty(e.fileName) || !seen.Add(e.fileName))
                index.entries.RemoveAt(i);
        }
    }

    private void TrimIndexAndDeleteFiles(CheckpointIndex index, int maxCount)
    {
        if (maxCount < 1) maxCount = 1;

        // delete anything beyond maxCount
        for (int i = index.entries.Count - 1; i >= maxCount; i--)
        {
            string fp = Path.Combine(SaveDir, index.entries[i].fileName);
            if (File.Exists(fp))
            {
                try { File.Delete(fp); } catch { /* ignore */ }
            }
            index.entries.RemoveAt(i);
        }
    }

    private static string SanitizeFilePart(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "checkpoint";
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        s = s.Replace(":", "_");
        return s.Length > 32 ? s.Substring(0, 32) : s;
    }

    private static void AtomicWriteAllText(string path, string content)
    {
        // write temp then replace/move to avoid partial writes
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tmp, path);
    }
}
