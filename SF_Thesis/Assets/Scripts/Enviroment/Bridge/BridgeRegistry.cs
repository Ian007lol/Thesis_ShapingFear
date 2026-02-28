using System.Collections.Generic;
using UnityEngine;

public static class BridgeRegistry
{
    private static readonly Dictionary<string, ExtendableBridge> _bridges =
        new Dictionary<string, ExtendableBridge>();

    ///--------------------------------------------------------
    /// Normalizes IDs like "b35", "B 35", "b-35" -> "B-35".
    ///---------------------------------------------------------
    public static string NormalizeBridgeId(string raw) //method generated with Chat GPT
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        string s = raw.Trim().ToUpper();

        // Remove spaces
        s = s.Replace(" ", "");

        // If already has a dash, we accept it as it is
        if (s.Contains("-"))
            return s;

        // If it doesn't have a dash already, add a dash
        if (s.Length > 1 && char.IsLetter(s[0]))
        {
            string letters = s[0].ToString();
            string digits = s.Substring(1);
            return $"{letters}-{digits}";
        }

        return s;
    }
    public static IEnumerable<ExtendableBridge> GetAllBridges()
{
    return _bridges.Values;
}

    public static void Register(ExtendableBridge bridge)
    {
        if (bridge == null) return;

        string id = NormalizeBridgeId(bridge.bridgeId);
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"[BridgeRegistry] Bridge '{bridge.name}' has empty bridgeId.");
            return;
        }

        if (_bridges.TryGetValue(id, out var existing) && existing != null && existing != bridge)
        {
            Debug.LogWarning($"[BridgeRegistry] Duplicate bridgeId '{id}'. Overwriting old reference: {existing.name} -> {bridge.name}");
        }

        _bridges[id] = bridge;
    }

    public static void Unregister(ExtendableBridge bridge)
    {
        if (bridge == null) return;
        string id = NormalizeBridgeId(bridge.bridgeId);

        if (_bridges.TryGetValue(id, out var existing) && existing == bridge)
        {
            _bridges.Remove(id);
        }
    }

    public static bool TryGetBridge(string rawId, out ExtendableBridge bridge)
    {
        bridge = null;
        string id = NormalizeBridgeId(rawId);
        if (string.IsNullOrEmpty(id)) return false;

        return _bridges.TryGetValue(id, out bridge) && bridge != null;
    }
}
