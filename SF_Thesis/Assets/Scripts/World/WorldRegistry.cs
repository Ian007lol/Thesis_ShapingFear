using System.Collections.Generic;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public static class WorldRegistry
{
    private static readonly Dictionary<string, GameObject> map = new();

    public static void Register(string id, GameObject go)
    {
        if (string.IsNullOrWhiteSpace(id) || go == null) return;
        map[id] = go;
    }

    public static void Unregister(string id, GameObject go)
    {
        if (string.IsNullOrWhiteSpace(id) || go == null) return;

        if (map.TryGetValue(id, out var existing) && existing == go)
            map.Remove(id);
    }

    public static GameObject Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        map.TryGetValue(id, out var go);
        return go;
    }
}
