using System;
using System.Runtime.InteropServices;
using PathimeSharp.Interop;

namespace PathimeSharp
{
    /// <summary>
    /// Library-level surface of libpathime: loading, process-global lifetime,
    /// and the static introspection tables.
    /// </summary>
    /// <remarks>
    /// <para><b>Threading:</b> libpathime does no locking, and calls must never
    /// overlap — not even on different engines or contexts. This binding adds
    /// no locking either; drive everything from one thread, or serialize
    /// externally. Handing objects between threads is fine with proper
    /// happens-before ordering.</para>
    /// </remarks>
    public static class Pathime
    {
        /// <summary>
        /// The default candidate cap a new context starts with
        /// (PATHIME_DEFAULT_MAX_CANDIDATES).
        /// </summary>
        public const int DefaultMaxCandidates = 64;

        /// <summary>
        /// Load the native library from an explicit path. Optional; when not
        /// called, the library is found via the <c>PATHIME_LIBRARY</c>
        /// environment variable or the platform's default search on first use.
        /// Must be called before any other member.
        /// </summary>
        public static void Load(string path)
        {
            LibraryLoader.Load(path);
        }

        /// <summary>The library version, e.g. "0.1.0".</summary>
        public static string Version
        {
            get
            {
                LibraryLoader.EnsureLoaded();
                return Utf8.DecodeNulTerminated(NativeMethods.pathime_version_string());
            }
        }

        /// <summary>
        /// The encoded numeric version: major*1000000 + minor*1000 + patch.
        /// </summary>
        public static uint VersionNumber
        {
            get
            {
                LibraryLoader.EnsureLoaded();
                return NativeMethods.pathime_version();
            }
        }

        /// <summary>
        /// Initialize process-global state: dictionaries, databases, and other
        /// one-time backend setup. May take perceptible time (dictionaries are
        /// tens of megabytes); consider a worker thread for UI apps.
        /// </summary>
        /// <param name="dataDir">
        /// Writable directory for per-user learned state. Null selects a
        /// platform default under the user's configuration directory.
        /// </param>
        /// <param name="resourceDir">
        /// Directory holding the read-only <c>pathime-data</c> files. Null
        /// selects <c>pathime-data</c> beside the native library itself.
        /// </param>
        public static void Init(string? dataDir = null, string? resourceDir = null)
        {
            LibraryLoader.EnsureLoaded();
            IntPtr dataDirPtr = IntPtr.Zero;
            IntPtr resourceDirPtr = IntPtr.Zero;
            try
            {
                dataDirPtr = Utf8.AllocNulTerminated(dataDir);
                resourceDirPtr = Utf8.AllocNulTerminated(resourceDir);
                var parameters = new PathimeInitParams
                {
                    StructSize = (UIntPtr)Marshal.SizeOf<PathimeInitParams>(),
                    DataDir = dataDirPtr,
                    ResourceDir = resourceDirPtr,
                };
                StatusCheck.ThrowIfError(NativeMethods.pathime_init(ref parameters));
            }
            finally
            {
                Utf8.Free(resourceDirPtr);
                Utf8.Free(dataDirPtr);
            }
        }

        /// <summary>
        /// Tear down process-global state. Every engine and context must be
        /// disposed first.
        /// </summary>
        public static void Shutdown()
        {
            LibraryLoader.EnsureLoaded();
            NativeMethods.pathime_shutdown();
        }

        /// <summary>
        /// Whether an engine is available: compiled in, backend loadable, and
        /// its data files found. False for everything before <see cref="Init"/>.
        /// </summary>
        public static bool HasEngine(EngineId id)
        {
            LibraryLoader.EnsureLoaded();
            return NativeMethods.pathime_has_engine((int)id);
        }

        /// <summary>
        /// The stable lowercase name of an engine ("hangul", "anthy", …), or
        /// "" for an unknown id.
        /// </summary>
        public static string GetEngineName(EngineId id)
        {
            LibraryLoader.EnsureLoaded();
            return Utf8.DecodeNulTerminated(NativeMethods.pathime_engine_name((int)id));
        }

        /// <summary>Number of known options; option ids are dense from 0.</summary>
        public static int OptionCount
        {
            get
            {
                LibraryLoader.EnsureLoaded();
                return checked((int)NativeMethods.pathime_option_count().ToUInt64());
            }
        }

        /// <summary>
        /// The stable kebab-case name of an option ("chinese-variant", …), or
        /// "" for an unknown id.
        /// </summary>
        public static string GetOptionName(Option option)
        {
            LibraryLoader.EnsureLoaded();
            return Utf8.DecodeNulTerminated(NativeMethods.pathime_option_name((int)option));
        }

        /// <summary>
        /// The stable name of an enum option's value ("traditional-first", …),
        /// or "" when the option or value has none.
        /// </summary>
        public static string GetOptionValueName(Option option, long value)
        {
            LibraryLoader.EnsureLoaded();
            return Utf8.DecodeNulTerminated(NativeMethods.pathime_option_value_name((int)option, value));
        }

        /// <summary>
        /// The keysym for a printable character, per the X11 rule: code points
        /// below U+0100 are the keysym itself; anything else is
        /// 0x01000000 + code point.
        /// </summary>
        public static uint KeysymForChar(char c)
        {
            if (char.IsSurrogate(c))
            {
                throw new ArgumentException(
                    "Character is a UTF-16 surrogate half; use KeysymForCodePoint for " +
                    "characters outside the Basic Multilingual Plane.", nameof(c));
            }

            return KeysymForCodePoint(c);
        }

        /// <summary>
        /// The keysym for a Unicode code point, per the X11 rule. Use this for
        /// characters outside the Basic Multilingual Plane.
        /// </summary>
        public static uint KeysymForCodePoint(int codePoint)
        {
            if (codePoint < 0 || codePoint > 0x10FFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(codePoint));
            }

            return codePoint < 0x100 ? (uint)codePoint : 0x01000000u + (uint)codePoint;
        }
    }
}
