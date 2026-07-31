using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PathimeSharp.Interop
{
    /// <summary>
    /// Locates and loads the native libpathime library before the first
    /// DllImport call binds. Search order:
    /// <list type="number">
    /// <item>An explicit path given to <see cref="Load"/> (surfaced as
    /// <c>Pathime.Load</c>).</item>
    /// <item>The <c>PATHIME_LIBRARY</c> environment variable — a full path to
    /// the shared library.</item>
    /// <item>The platform's default library search
    /// (<c>pathime.dll</c> / <c>libpathime.so.0</c> / <c>libpathime.so</c>).</item>
    /// </list>
    /// On Windows an explicit path is loaded with
    /// LOAD_WITH_ALTERED_SEARCH_PATH so the vendored backend DLLs sitting
    /// beside pathime.dll resolve; the subsequent <c>DllImport("pathime")</c>
    /// then binds to the already-loaded module by name.
    /// </summary>
    internal static class LibraryLoader
    {
        private static readonly object Gate = new object();
        private static bool _loaded;
#if NET8_0_OR_GREATER
        private static bool _resolverRegistered;
#endif

        /// <summary>The path the library was loaded from, if an explicit one was used.</summary>
        internal static string? LoadedPath { get; private set; }

        /// <summary>
        /// Load from an explicit path. Throws if the library is already loaded.
        /// </summary>
        internal static void Load(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            lock (Gate)
            {
                if (_loaded)
                {
                    throw new InvalidOperationException(
                        "libpathime is already loaded" +
                        (LoadedPath != null ? $" (from \"{LoadedPath}\")" : string.Empty) +
                        "; Pathime.Load must be called before any other Pathime API.");
                }

                RegisterResolver();
                LoadFrom(path);
                // Set before Sanity: on Linux the DllImport("pathime") probe
                // does not find a library loaded by absolute path — the
                // resolver bridges by this path.
                LoadedPath = path;
                try
                {
                    Sanity(path);
                }
                catch
                {
                    LoadedPath = null;
                    throw;
                }

                _loaded = true;
            }
        }

        /// <summary>
        /// Called lazily by the first API member that touches native code.
        /// Idempotent.
        /// </summary>
        internal static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            lock (Gate)
            {
                if (_loaded)
                {
                    return;
                }

                RegisterResolver();
                string? envPath = Environment.GetEnvironmentVariable("PATHIME_LIBRARY");
                if (!string.IsNullOrEmpty(envPath))
                {
                    LoadFrom(envPath!);
                    LoadedPath = envPath;
                }

                // With no explicit path, DllImport's own probing takes over on
                // the first native call (and on net8.0 the resolver above adds
                // the versioned soname). Failure surfaces there as
                // DllNotFoundException.
                Sanity(envPath);
                _loaded = true;
            }
        }

        private static void RegisterResolver()
        {
#if NET8_0_OR_GREATER
            if (!_resolverRegistered)
            {
                NativeLibrary.SetDllImportResolver(typeof(LibraryLoader).Assembly, Resolve);
                _resolverRegistered = true;
            }
#endif
        }

        private static void Sanity(string? path)
        {
            try
            {
                NativeMethods.pathime_version();
            }
            catch (DllNotFoundException e)
            {
                throw new DllNotFoundException(
                    "Could not load the native libpathime library" +
                    (path != null ? $" from \"{path}\"" : string.Empty) +
                    ". Build libpathime (see README.md) and set the PATHIME_LIBRARY " +
                    "environment variable to the full path of pathime.dll / libpathime.so, " +
                    "or call Pathime.Load(path) first.", e);
            }
        }

#if NET8_0_OR_GREATER
        private static IntPtr Resolve(string libraryName, System.Reflection.Assembly assembly,
                                      DllImportSearchPath? searchPath)
        {
            if (libraryName != NativeMethods.Lib)
            {
                return IntPtr.Zero;
            }

            string? explicitPath = LoadedPath ?? Environment.GetEnvironmentVariable("PATHIME_LIBRARY");
            if (!string.IsNullOrEmpty(explicitPath) && NativeLibrary.TryLoad(explicitPath, out IntPtr fromPath))
            {
                return fromPath;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Default probing tries libpathime.so; installs commonly ship
                // only the versioned soname.
                if (NativeLibrary.TryLoad("libpathime.so.0", assembly, searchPath, out IntPtr soname))
                {
                    return soname;
                }
            }

            return IntPtr.Zero; // fall back to default probing
        }
#endif

        private static void LoadFrom(string path)
        {
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"Native libpathime library not found at \"{fullPath}\".", fullPath);
            }

#if NET8_0_OR_GREATER
            NativeLibrary.Load(fullPath);
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr module = LoadLibraryExW(fullPath, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
                if (module == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new DllNotFoundException(
                        $"LoadLibraryEx failed for \"{fullPath}\" (Win32 error {error}). " +
                        "If pathime.dll loaded but a dependency did not, make sure the vendored " +
                        "backend DLLs and the MSVC runtime are present beside it.");
                }
            }
            else
            {
                IntPtr handle = DlOpen(fullPath);
                if (handle == IntPtr.Zero)
                {
                    throw new DllNotFoundException($"dlopen failed for \"{fullPath}\".");
                }
            }
#endif
        }

#if !NET8_0_OR_GREATER
        private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 0x100;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen_glibc(string fileName, int flags);

        [DllImport("libdl", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen_libdl(string fileName, int flags);

        private static IntPtr DlOpen(string fileName)
        {
            try
            {
                return dlopen_glibc(fileName, RTLD_NOW | RTLD_GLOBAL);
            }
            catch (DllNotFoundException)
            {
                return dlopen_libdl(fileName, RTLD_NOW | RTLD_GLOBAL);
            }
        }
#endif
    }
}
