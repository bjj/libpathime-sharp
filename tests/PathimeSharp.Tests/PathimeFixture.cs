using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PathimeSharp.Tests
{
    /// <summary>
    /// Process-wide libpathime setup, run once per test assembly.
    /// </summary>
    /// <remarks>
    /// The native library comes from the <c>PATHIME_LIBRARY</c> environment
    /// variable, falling back to the staged copy in
    /// <c>artifacts/native/&lt;rid&gt;/</c>. Init is process-global and
    /// one-shot; isolation between runs comes from a fresh data dir, and — like
    /// the Python suite — shutdown is deliberately skipped (contexts may still
    /// be finalizer-free and alive; the process exit cleans up).
    /// </remarks>
    public sealed class PathimeFixture
    {
        public PathimeFixture()
        {
            string? envPath = Environment.GetEnvironmentVariable("PATHIME_LIBRARY");
            if (string.IsNullOrEmpty(envPath))
            {
                string staged = StagedLibraryPath();
                if (File.Exists(staged))
                {
                    Pathime.Load(staged);
                }
                // else: let default probing produce its descriptive failure.
            }

            DataDir = Path.Combine(Path.GetTempPath(), "pathime-sharp-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DataDir);
            Pathime.Init(dataDir: DataDir);
        }

        public string DataDir { get; }

        private static string StagedLibraryPath()
        {
            string rid = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" : "linux-x64";
            string fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pathime.dll" : "libpathime.so";
            // tests/PathimeSharp.Tests/bin/<cfg>/<tfm>/ -> repo root
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            return Path.Combine(root, "artifacts", "native", rid, fileName);
        }
    }
}
