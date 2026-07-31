using System;
using System.Runtime.InteropServices;

namespace PathimeSharp.Interop
{
    /// <summary>
    /// pathime_str_t: a borrowed UTF-8 slice. <c>Len</c> is the one
    /// byte-denominated quantity in the whole API; every other offset or count
    /// is in Unicode scalar values. Passed by value on hot paths; deliberately
    /// not struct_size-versioned.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PathimeStr
    {
        public IntPtr Bytes;
        public UIntPtr Len;
    }

    /// <summary>pathime_init_params_t. Caller sets StructSize.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PathimeInitParams
    {
        public UIntPtr StructSize;
        public IntPtr DataDir;      // const char*, NUL-terminated UTF-8, or NULL
        public IntPtr ResourceDir;  // const char*, NUL-terminated UTF-8, or NULL
    }

    /// <summary>pathime_key_event_t. Caller sets StructSize.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PathimeKeyEventNative
    {
        public UIntPtr StructSize;
        public uint Keysym;
        public uint LayoutKey;
        public uint Modifiers;
    }

    /// <summary>
    /// pathime_composition_t. Library-owned and library-written; StructSize is
    /// filled in by the library and bounds what may be read.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PathimeCompositionNative
    {
        public UIntPtr StructSize;
        public PathimeStr Preedit;
        public UIntPtr PreeditSettled;   // Unicode scalar values
        public UIntPtr CandidateCount;
        public UIntPtr CandidateCursor;
    }

    /// <summary>
    /// pathime_client_t. Borrowed by pointer for the context's whole lifetime —
    /// the library does not copy it — so the binding marshals it into an
    /// unmanaged block that is freed only after pathime_context_destroy.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PathimeClientNative
    {
        public UIntPtr StructSize;
        public IntPtr CommitText;              // required
        public IntPtr DeleteSurroundingText;   // optional; Zero = unsupported
        public IntPtr CompositionChanged;      // optional; Zero = unsupported
    }

    /// <summary>
    /// pathime_option_info_t. StructSize is both in and out: set before the
    /// call, reduced by the library to what it actually wrote. The two C bools
    /// are single bytes; keeping them as <c>byte</c> reproduces the C layout
    /// (offsets 12–13, then padding to align DefaultValue at 16 on 64-bit).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PathimeOptionInfoNative
    {
        public UIntPtr StructSize;
        public int Type;                 // pathime_option_type_t
        public byte Supported;
        public byte ResetsComposition;
        public long DefaultValue;
        public long MinValue;
        public long MaxValue;
        public ulong ValidValues;
        public PathimeStr DefaultString; // static lifetime, unlike getter strings
        public UIntPtr ValidValueCount;
    }
}
