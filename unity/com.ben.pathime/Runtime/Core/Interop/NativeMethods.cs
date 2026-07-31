using System;
using System.Runtime.InteropServices;

namespace PathimeSharp.Interop
{
    /// <summary>
    /// A transcription of pathime.h, kept in its order.
    /// </summary>
    /// <remarks>
    /// Rules, per the C API contract:
    /// <list type="bullet">
    /// <item>Every <c>const char*</c> return is <see cref="IntPtr"/>, never
    /// <c>string</c> — default marshaling would free library-owned memory.</item>
    /// <item>C <c>bool</c> is one byte: <c>[MarshalAs(UnmanagedType.U1)]</c>.</item>
    /// <item><c>size_t</c> is <see cref="UIntPtr"/>, <c>ptrdiff_t</c> is
    /// <see cref="IntPtr"/> (32-bit Unity targets exist).</item>
    /// <item>Everything is Cdecl; names are undecorated.</item>
    /// </list>
    /// </remarks>
    internal static class NativeMethods
    {
        // DllImport probes "pathime.dll" on Windows and "libpathime.so" on
        // Linux under both Mono and .NET. LibraryLoader handles explicit paths,
        // PATHIME_LIBRARY, and the versioned soname.
        internal const string Lib = "pathime";

        /* Version */

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint pathime_version();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pathime_version_string();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pathime_status_string(int status);

        /* Library lifetime */

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_init(ref PathimeInitParams parameters);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pathime_shutdown();

        /* Engines */

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pathime_engine_name(int id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool pathime_has_engine(int id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_create(int id, out IntPtr outEngine);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pathime_engine_destroy(IntPtr engine);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_id(IntPtr engine);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint pathime_engine_requirements(IntPtr engine);

        /* Input context */

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_create(IntPtr engine,
                                                          IntPtr client,
                                                          IntPtr userData,
                                                          out IntPtr outCtx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pathime_context_destroy(IntPtr ctx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pathime_context_engine(IntPtr ctx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pathime_context_user_data(IntPtr ctx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint pathime_context_requirements(IntPtr ctx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_process_key(IntPtr ctx,
                                                               ref PathimeKeyEventNative ev,
                                                               [MarshalAs(UnmanagedType.U1)] out bool outHandled);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pathime_context_composition(IntPtr ctx); // const pathime_composition_t*

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_candidate(IntPtr ctx,
                                                             UIntPtr index,
                                                             out PathimeStr outStr);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_set_candidate_cursor(IntPtr ctx, UIntPtr index);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_select_candidate(IntPtr ctx, UIntPtr index);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_set_surrounding_text(IntPtr ctx,
                                                                        PathimeStr text,
                                                                        UIntPtr cursor);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_commit(IntPtr ctx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_reset(IntPtr ctx);

        /* Options */

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern UIntPtr pathime_option_count();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pathime_option_name(int option);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pathime_option_value_name(int option, long value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_option_info(IntPtr engine,
                                                              int option,
                                                              ref PathimeOptionInfoNative outInfo);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_set_option_bool(IntPtr engine,
                                                                  int option,
                                                                  [MarshalAs(UnmanagedType.U1)] bool value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_set_option_int(IntPtr engine, int option, long value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_set_option_string(IntPtr engine, int option, IntPtr value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_set_option_bool(IntPtr ctx,
                                                                   int option,
                                                                   [MarshalAs(UnmanagedType.U1)] bool value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_set_option_int(IntPtr ctx, int option, long value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_set_option_string(IntPtr ctx, int option, IntPtr value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_reset_option(IntPtr engine, int option);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_reset_option(IntPtr ctx, int option);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_isolate_options(IntPtr ctx);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_get_option_bool(IntPtr engine,
                                                                  int option,
                                                                  [MarshalAs(UnmanagedType.U1)] out bool outValue);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_get_option_int(IntPtr engine, int option, out long outValue);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_engine_get_option_string(IntPtr engine,
                                                                    int option,
                                                                    out PathimeStr outValue);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_get_option_bool(IntPtr ctx,
                                                                   int option,
                                                                   [MarshalAs(UnmanagedType.U1)] out bool outValue);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_get_option_int(IntPtr ctx, int option, out long outValue);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pathime_context_get_option_string(IntPtr ctx,
                                                                     int option,
                                                                     out PathimeStr outValue);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool pathime_engine_option_is_set(IntPtr engine, int option);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool pathime_context_option_is_set(IntPtr ctx, int option);
    }
}
