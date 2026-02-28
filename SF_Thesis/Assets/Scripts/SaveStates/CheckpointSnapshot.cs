using System;
using System.Collections.Generic;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
[Serializable]
public class CheckpointSnapshot
{
    public string sceneName;

    // optional label / debug
    public string checkpointId;

    // mission
    public string missionId;
    public int missionStepIndex;
    public bool missionStepActive;

    public List<DoorSnapshot> doors = new();
    public List<BridgeSnapshot> bridges = new();
    public List<EntitySnapshot> entities = new();
}

[Serializable]
public struct DoorSnapshot
{
    public string id;
    public bool isOpen;
    public bool isLocked;
    public bool manualOverride;
}

[Serializable]
public struct BridgeSnapshot
{
    public string id;
    public bool isExtended;
}

[Serializable]
public struct EntitySnapshot
{
    public string key;
    public string json;
}
