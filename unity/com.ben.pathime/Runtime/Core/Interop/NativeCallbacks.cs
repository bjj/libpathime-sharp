using System;
using System.Runtime.InteropServices;

namespace PathimeSharp.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void CommitTextFn(IntPtr userData, PathimeStr text);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DeleteSurroundingFn(IntPtr userData, IntPtr offset, UIntPtr count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void CompositionChangedFn(IntPtr userData, IntPtr composition);

    /// <summary>
    /// The three reverse-P/Invoke thunks shared by every context. All are
    /// static methods (IL2CPP cannot marshal instance delegates) held in
    /// static readonly fields so the delegates are rooted for the process
    /// lifetime; per-context identity travels through <c>user_data</c>, a
    /// <see cref="GCHandle"/> to the owning <see cref="Context"/>.
    /// </summary>
    internal static class NativeCallbacks
    {
        private static readonly CommitTextFn CommitFn = OnCommitText;
        private static readonly DeleteSurroundingFn DeleteFn = OnDeleteSurrounding;
        private static readonly CompositionChangedFn ChangedFn = OnCompositionChanged;

        internal static readonly IntPtr CommitPtr = Marshal.GetFunctionPointerForDelegate(CommitFn);
        internal static readonly IntPtr DeletePtr = Marshal.GetFunctionPointerForDelegate(DeleteFn);
        internal static readonly IntPtr ChangedPtr = Marshal.GetFunctionPointerForDelegate(ChangedFn);

        // Exceptions must never cross the native frame: each thunk body is
        // fully wrapped, and the first exception is stashed on the context to
        // be rethrown after the triggering call returns.

        [MonoPInvokeCallback(typeof(CommitTextFn))]
        private static void OnCommitText(IntPtr userData, PathimeStr text)
        {
            var context = (Context)GCHandle.FromIntPtr(userData).Target!;
            try
            {
                context.HandleCommit(Utf8.Decode(text));
            }
            catch (Exception e)
            {
                context.StashCallbackException(e);
            }
        }

        [MonoPInvokeCallback(typeof(DeleteSurroundingFn))]
        private static void OnDeleteSurrounding(IntPtr userData, IntPtr offset, UIntPtr count)
        {
            var context = (Context)GCHandle.FromIntPtr(userData).Target!;
            try
            {
                context.HandleDeleteSurrounding((long)offset, count.ToUInt64());
            }
            catch (Exception e)
            {
                context.StashCallbackException(e);
            }
        }

        [MonoPInvokeCallback(typeof(CompositionChangedFn))]
        private static void OnCompositionChanged(IntPtr userData, IntPtr composition)
        {
            var context = (Context)GCHandle.FromIntPtr(userData).Target!;
            try
            {
                context.HandleCompositionChanged(composition);
            }
            catch (Exception e)
            {
                context.StashCallbackException(e);
            }
        }
    }
}
