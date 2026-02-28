using System.Collections.Generic;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public static class CheckpointSavableRegistry
{
    private static readonly List<ICheckpointSavable> _savables = new();

    public static void Register(ICheckpointSavable s)
    {
        if (s != null && !_savables.Contains(s)) _savables.Add(s);
    }

    public static void Unregister(ICheckpointSavable s)
    {
        if (s != null) _savables.Remove(s);
    }

    public static IReadOnlyList<ICheckpointSavable> All => _savables;
}
