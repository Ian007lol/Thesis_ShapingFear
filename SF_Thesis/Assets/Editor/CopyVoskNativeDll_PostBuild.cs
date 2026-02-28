#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
//------------------------------------------------
// This class was generated with chat GPT
//------------------------------------------------
public static class CopyVoskNativeDll_PostBuild
{
    private const string SrcDir = "Assets/Plugins/Windows/x86_64"; // where the 4 dlls are

    [PostProcessBuild(999)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.StandaloneWindows64) return;

        if (!Directory.Exists(SrcDir))
        {
            Debug.LogError("[Vosk] Source dir not found: " + SrcDir);
            return;
        }

        var exeDir = Path.GetDirectoryName(pathToBuiltProject);
        var exeName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
        var dataDir = Path.Combine(exeDir, exeName + "_Data");
        var pluginsDir = Path.Combine(dataDir, "Plugins", "x86_64");
        Directory.CreateDirectory(pluginsDir);

        foreach (var dll in Directory.GetFiles(SrcDir, "*.dll"))
        {
            var name = Path.GetFileName(dll);

            // Copy next to exe (extra-safe for Windows loader)
            File.Copy(dll, Path.Combine(exeDir, name), true);

            // Copy to Plugins/x86_64 (where Unity tries to load it from)
            File.Copy(dll, Path.Combine(pluginsDir, name), true);

            Debug.Log("[Vosk] Copied: " + name);
        }
    }
}
#endif
