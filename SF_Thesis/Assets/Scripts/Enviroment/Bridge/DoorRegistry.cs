using System.Collections.Generic;
using UnityEngine;

public static class DoorRegistry
{
    private static readonly Dictionary<string, DoorSlideUp> _doors =
        new Dictionary<string, DoorSlideUp>();

    /// <summary>
    /// Normalizes IDs like "a45", "A 45", "a-45" -> "A-45".
    /// </summary>
    public static string NormalizeDoorId(string raw) //method generated with Chat GPT
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        string s = raw.Trim().ToUpper();

        // Remove spaces
        s = s.Replace(" ", "");

        // If already has a dash, accept it
        if (s.Contains("-"))
            return s;

        // If format is Letter + digits (A45) -> A-45
        if (s.Length > 1 && char.IsLetter(s[0]))
        {
            string letters = s[0].ToString();
            string digits = s.Substring(1);
            return $"{letters}-{digits}";
        }

        return s;
    }

    public static void Register(DoorSlideUp door)
    {
        if (door == null) return;

        string id = NormalizeDoorId(door.doorId);
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"[DoorRegistry] Door '{door.name}' has empty doorId.");
            return;
        }

        if (_doors.TryGetValue(id, out var existing) && existing != null && existing != door)
        {
            Debug.LogWarning(
                $"[DoorRegistry] Duplicate doorId '{id}'. Overwriting old reference: {existing.name} -> {door.name}"
            );
        }

        _doors[id] = door;
    }

    public static void Unregister(DoorSlideUp door)
    {
        if (door == null) return;

        string id = NormalizeDoorId(door.doorId);
        if (_doors.TryGetValue(id, out var existing) && existing == door)
        {
            _doors.Remove(id);
        }
    }

    public static bool TryGetDoor(string rawId, out DoorSlideUp door)
    {
        door = null;

        string id = NormalizeDoorId(rawId);
        if (string.IsNullOrEmpty(id)) return false;

        return _doors.TryGetValue(id, out door) && door != null;
    }
    public static IEnumerable<DoorSlideUp> GetAllDoors()
{
    foreach (var kv in _doors)
    {
        if (kv.Value != null)
            yield return kv.Value;
    }
}
}
