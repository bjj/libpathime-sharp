using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PathimeSharp.Unity.Editor
{
    /// <summary>
    /// Copies the package's pathime-data~ dictionaries beside the built
    /// desktop player (Unity copies the plugin DLLs itself, but knows nothing
    /// about the data tree the trailing ~ hides from the importer).
    /// </summary>
    public sealed class PathimeDataBuildProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            string platform;
            switch (report.summary.platform)
            {
                case BuildTarget.StandaloneWindows64:
                    platform = "Windows";
                    break;
                case BuildTarget.StandaloneLinux64:
                    platform = "Linux";
                    break;
                default:
                    return; // desktop-only for now; see the package README
            }

            string source = Path.GetFullPath(
                $"Packages/com.ben.pathime/Plugins/{platform}/x86_64/pathime-data~");
            if (!Directory.Exists(source))
            {
                Debug.LogWarning(
                    "PathimeSharp: no staged pathime-data~ in the package — the built " +
                    "player will report every engine unavailable. Run scripts/stage-native " +
                    "and rebuild, or ship pathime-data yourself.");
                return;
            }

            string destination = Path.Combine(
                Path.GetDirectoryName(report.summary.outputPath), "pathime-data");
            CopyTree(source, destination);
            Debug.Log($"PathimeSharp: staged pathime-data into {destination}");
        }

        private static void CopyTree(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(Path.Combine(destination, dir.Substring(source.Length + 1)));
            }

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(destination, file.Substring(source.Length + 1)), true);
            }
        }
    }
}
