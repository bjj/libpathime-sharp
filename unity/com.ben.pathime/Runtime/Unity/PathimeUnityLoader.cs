using System;
using System.IO;
using UnityEngine;

namespace PathimeSharp.Unity
{
    /// <summary>
    /// Unity-side setup: locates the native library and dictionary data that
    /// the staging script placed under this package's Plugins folder, then
    /// loads and initializes libpathime.
    /// </summary>
    /// <remarks>
    /// Desktop platforms only (Windows/Linux x64), and pending the IL2CPP
    /// smoke test tracked in the repo's TODO.md. Call
    /// <see cref="Initialize"/> once, before creating any
    /// <see cref="Engine"/> — off the main thread if startup time matters
    /// (dictionaries are tens of megabytes), with the caveat that all later
    /// libpathime calls must then happen on that same serialized path.
    /// </remarks>
    public static class PathimeUnity
    {
        /// <summary>
        /// Load the packaged native library and call <see cref="Pathime.Init"/>.
        /// </summary>
        /// <param name="dataDir">
        /// Writable directory for per-user learned state; defaults to
        /// "pathime" under <see cref="Application.persistentDataPath"/>.
        /// </param>
        public static void Initialize(string dataDir = null)
        {
            string pluginDir = PluginDirectory();
            string libraryName = Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer
                ? "pathime.dll"
                : "libpathime.so";

            string libraryPath = Path.Combine(pluginDir, libraryName);
            if (File.Exists(libraryPath))
            {
                Pathime.Load(libraryPath);
            }
            // else: fall back to PATHIME_LIBRARY / default search, so a
            // package without staged natives still works for developers who
            // point at their own libpathime build.

            string resourceDir = ResourceDirectory(pluginDir);
            Pathime.Init(
                dataDir ?? Path.Combine(Application.persistentDataPath, "pathime"),
                Directory.Exists(resourceDir) ? resourceDir : null);
        }

        private static string PluginDirectory()
        {
#if UNITY_EDITOR
            // Resolves through Packages/ for both local and PackageCache
            // installs; on-disk on desktop.
            string packagePlugins = Path.GetFullPath("Packages/com.ben.pathime/Plugins");
            string platform = Application.platform == RuntimePlatform.WindowsEditor
                ? "Windows"
                : "Linux";
            return Path.Combine(packagePlugins, platform, "x86_64");
#else
            // Players: Unity copies plugins into <Data>/Plugins/x86_64.
            return Path.Combine(Application.dataPath, "Plugins", "x86_64");
#endif
        }

        private static string ResourceDirectory(string pluginDir)
        {
#if UNITY_EDITOR
            // The trailing ~ keeps the 30 MB of dictionaries out of Unity's
            // asset importer; the folder is still an ordinary directory here.
            return Path.Combine(pluginDir, "pathime-data~");
#else
            // The editor build processor stages this beside the executable.
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "pathime-data");
#endif
        }
    }
}
