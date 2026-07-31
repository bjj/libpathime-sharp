using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using PathimeSharp.Interop;

namespace PathimeSharp
{
    /// <summary>
    /// Handles a request from the engine to delete committed text near the
    /// insertion position. Both values are UTF-16 code-unit quantities against
    /// the text most recently given to <see cref="Context.SetSurroundingText"/>:
    /// <paramref name="utf16Offset"/> is the start of the range relative to
    /// that call's cursor (negative is before it), and
    /// <paramref name="utf16Count"/> is the number of code units to delete.
    /// </summary>
    public delegate void DeleteSurroundingTextHandler(int utf16Offset, int utf16Count);

    /// <summary>
    /// One independently editable client destination: composition state,
    /// surrounding text, and per-context options.
    /// </summary>
    /// <remarks>
    /// <para>Callbacks fire synchronously, on the calling thread, inside the
    /// library call that triggered them. An exception thrown by a callback is
    /// caught at the native boundary and rethrown after that call returns.</para>
    /// <para>Contexts are created with isolated options by default
    /// (<c>isolate: true</c>): every engine-level option value is copied in at
    /// creation, so later engine-level sets no longer reach this context —
    /// configure the engine first, or pass <c>isolate: false</c> to keep live
    /// inheritance.</para>
    /// <para>Dispose the context before its engine. No finalizer; a forgotten
    /// context leaks.</para>
    /// </remarks>
    public sealed class Context : OptionScope, IDisposable
    {
        private readonly Engine _engine;
        private readonly Action<string>? _onCommit;
        private readonly DeleteSurroundingTextHandler? _onDeleteSurrounding;
        private readonly Action<Composition>? _onCompositionChanged;
        private readonly StringBuilder _committed = new StringBuilder();

        private IntPtr _handle;
        private GCHandle _selfHandle;
        private IntPtr _clientBlock;
        private Composition _composition = Composition.Empty;
        private Exception? _pendingCallbackException;

        // The engine's frame of reference for delete-surrounding requests: the
        // text and cursor most recently accepted by SetSurroundingText. Only a
        // successful SetSurroundingText replaces it.
        private string? _surroundingText;
        private int _surroundingCursorUtf16;
        private int _surroundingCursorScalar;

        /// <param name="engine">The engine to serve this context. Must outlive it.</param>
        /// <param name="onCommit">
        /// Receives finalized text. When null, committed text accumulates in
        /// <see cref="Committed"/> and is drained with <see cref="TakeCommitted"/>.
        /// </param>
        /// <param name="onDeleteSurrounding">
        /// Handles delete-surrounding requests. When null, the context declares
        /// the capability absent; engines that require it fail context creation
        /// with <see cref="PathimeMissingCallbackException"/>.
        /// </param>
        /// <param name="onCompositionChanged">
        /// Invoked with each fresh <see cref="Composition"/> snapshot, after
        /// <see cref="Composition"/> has been updated.
        /// </param>
        /// <param name="isolate">
        /// Copy every engine-level option value into this context at creation
        /// (see the class remarks). Default true. Note the copies also pin
        /// values an engine would otherwise re-derive later — e.g. the
        /// wildcard a table declares when a different table file is loaded
        /// into this context afterward; <see cref="OptionScope.ResetOption"/>
        /// un-pins an individual option.
        /// </param>
        public Context(Engine engine,
                       Action<string>? onCommit = null,
                       DeleteSurroundingTextHandler? onDeleteSurrounding = null,
                       Action<Composition>? onCompositionChanged = null,
                       bool isolate = true)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            _engine = engine;
            _onCommit = onCommit;
            _onDeleteSurrounding = onDeleteSurrounding;
            _onCompositionChanged = onCompositionChanged;

            _selfHandle = GCHandle.Alloc(this);
            try
            {
                // The library borrows the client table by pointer for the
                // context's whole lifetime; it lives in unmanaged memory owned
                // by this object and freed only after pathime_context_destroy.
                var client = new PathimeClientNative
                {
                    StructSize = (UIntPtr)Marshal.SizeOf<PathimeClientNative>(),
                    CommitText = NativeCallbacks.CommitPtr,
                    DeleteSurroundingText = onDeleteSurrounding != null
                        ? NativeCallbacks.DeletePtr
                        : IntPtr.Zero,
                    CompositionChanged = NativeCallbacks.ChangedPtr,
                };
                _clientBlock = Marshal.AllocHGlobal(Marshal.SizeOf<PathimeClientNative>());
                Marshal.StructureToPtr(client, _clientBlock, false);

                StatusCheck.ThrowIfError(NativeMethods.pathime_context_create(
                    engine.Handle, _clientBlock, GCHandle.ToIntPtr(_selfHandle), out _handle));
            }
            catch
            {
                if (_clientBlock != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_clientBlock);
                    _clientBlock = IntPtr.Zero;
                }
                _selfHandle.Free();
                throw;
            }

            engine.Register(this);

            if (isolate)
            {
                IsolateOptions();
            }
        }

        internal IntPtr Handle
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new ObjectDisposedException(nameof(Context));
                }

                return _handle;
            }
        }

        /// <summary>The engine serving this context.</summary>
        public Engine Engine => _engine;

        /// <summary>
        /// What this context needs from its client right now, resolved against
        /// current option values.
        /// </summary>
        public EngineRequirements Requirements =>
            (EngineRequirements)NativeMethods.pathime_context_requirements(Handle);

        /// <summary>
        /// The current composition snapshot. An owned, immutable copy, updated
        /// before each composition-changed callback; never null.
        /// </summary>
        public Composition Composition => _composition;

        /// <summary>
        /// Text committed while no commit callback was supplied.
        /// </summary>
        public string Committed => _committed.ToString();

        /// <summary>Return and clear <see cref="Committed"/>.</summary>
        public string TakeCommitted()
        {
            string text = _committed.ToString();
            _committed.Clear();
            return text;
        }

        internal override IntPtr OptionHandle => Handle;
        internal override IntPtr InfoEngineHandle => _engine.Handle;
        private protected override bool IsEngineScope => false;

        private protected override void ThrowPendingCallbackExceptions()
        {
            ThrowPendingCallbackException();
        }

        /* Input */

        /// <summary>
        /// Process one key press. Returns true when the engine consumed the
        /// key; false means the client should apply its ordinary behaviour.
        /// </summary>
        public bool ProcessKey(in KeyEvent keyEvent)
        {
            var native = new PathimeKeyEventNative
            {
                StructSize = (UIntPtr)Marshal.SizeOf<PathimeKeyEventNative>(),
                Keysym = keyEvent.Keysym,
                LayoutKey = keyEvent.LayoutKey,
                Modifiers = (uint)keyEvent.Modifiers,
            };
            bool handled;
            int status = NativeMethods.pathime_context_process_key(Handle, ref native, out handled);
            CompleteMutation(status);
            return handled;
        }

        /// <summary>Process a printable character key.</summary>
        public bool ProcessKey(char c, KeyModifiers modifiers = KeyModifiers.None)
        {
            return ProcessKey(KeyEvent.FromChar(c, modifiers));
        }

        /// <summary>Process a non-printable key.</summary>
        public bool ProcessKey(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            return ProcessKey(new KeyEvent(key, modifiers));
        }

        /// <summary>
        /// Feed each character of <paramref name="text"/> as an unmodified key
        /// press. Keys the engine declines are simply dropped; a real client
        /// routes declined keys to its own editing instead.
        /// </summary>
        public void Type(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int i = 0;
            while (i < text.Length)
            {
                int codePoint = char.ConvertToUtf32(text, i);
                ProcessKey(new KeyEvent(Pathime.KeysymForCodePoint(codePoint)));
                i += char.IsHighSurrogate(text[i]) ? 2 : 1;
            }
        }

        /* Candidates */

        /// <summary>
        /// One candidate by absolute position, as an owned copy. Prefer
        /// <see cref="Composition.Candidates"/>, which already holds them all.
        /// </summary>
        public string GetCandidate(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            PathimeStr native;
            StatusCheck.ThrowIfError(
                NativeMethods.pathime_context_candidate(Handle, (UIntPtr)index, out native));
            return Utf8.Decode(native);
        }

        /// <summary>Move the highlight. On some engines this rewrites the preedit.</summary>
        public void SetCandidateCursor(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            CompleteMutation(NativeMethods.pathime_context_set_candidate_cursor(Handle, (UIntPtr)index));
        }

        /// <summary>
        /// Choose a candidate: settles the span it covers and produces a fresh
        /// list for the remainder (possibly committing).
        /// </summary>
        public void SelectCandidate(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            CompleteMutation(NativeMethods.pathime_context_select_candidate(Handle, (UIntPtr)index));
        }

        /* Document interaction */

        /// <summary>
        /// Tell the engine what committed text surrounds the insertion point.
        /// </summary>
        /// <param name="text">The visible text around the insertion point.</param>
        /// <param name="utf16Cursor">
        /// The insertion point as a UTF-16 index into <paramref name="text"/>.
        /// </param>
        public void SetSurroundingText(string text, int utf16Cursor)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // Throws on out-of-range and on splitting a surrogate pair, before
            // anything crosses the boundary.
            int scalarCursor = UnicodeIndex.Utf16ToScalars(text, utf16Cursor);

            byte[] bytes = Utf8.GetBytes(text);
            byte[] pinTarget = bytes.Length > 0 ? bytes : new byte[1];
            GCHandle pin = GCHandle.Alloc(pinTarget, GCHandleType.Pinned);
            int status;
            try
            {
                var native = new PathimeStr
                {
                    Bytes = pin.AddrOfPinnedObject(),
                    Len = (UIntPtr)bytes.Length,
                };
                status = NativeMethods.pathime_context_set_surrounding_text(
                    Handle, native, (UIntPtr)scalarCursor);
            }
            finally
            {
                pin.Free();
            }

            if (status == 0)
            {
                // This snapshot is now the engine's frame of reference for
                // delete-surrounding requests — and therefore our conversion
                // table back to UTF-16. Replaced only by the next successful
                // call; commits and resets deliberately leave it in place.
                _surroundingText = text;
                _surroundingCursorUtf16 = utf16Cursor;
                _surroundingCursorScalar = scalarCursor;
            }

            CompleteMutation(status);
        }

        /// <summary>
        /// End the composition, committing the preedit as it stands.
        /// </summary>
        public void Commit()
        {
            CompleteMutation(NativeMethods.pathime_context_commit(Handle));
        }

        /// <summary>
        /// Discard all composition state without committing. Also the required
        /// recovery step after a <see cref="PathimeFailureException"/>.
        /// </summary>
        public void Reset()
        {
            CompleteMutation(NativeMethods.pathime_context_reset(Handle));
        }

        /* Options */

        /// <summary>
        /// Copy every engine-level option value into this context's own store,
        /// making it immune to later engine-level sets. Contexts do this at
        /// creation by default; calling it again re-snapshots. Reversible per
        /// option through <see cref="OptionScope.ResetOption"/>.
        /// </summary>
        public void IsolateOptions()
        {
            CompleteMutation(NativeMethods.pathime_context_isolate_options(Handle));
        }

        /* Callback plumbing (called from NativeCallbacks thunks) */

        internal void HandleCommit(string text)
        {
            if (_onCommit != null)
            {
                _onCommit(text);
            }
            else
            {
                _committed.Append(text);
            }
        }

        internal void HandleDeleteSurrounding(long scalarOffset, ulong scalarCount)
        {
            if (_onDeleteSurrounding == null)
            {
                return; // unreachable: the callback slot was null
            }

            // The engine expresses the range in scalar values against the last
            // surrounding-text snapshot; convert against that same snapshot.
            // The engine only asks to delete text it can see, so a range the
            // snapshot cannot resolve means frames of reference have diverged —
            // the header's own guidance is to safely ignore the request rather
            // than delete the wrong thing.
            string? text = _surroundingText;
            long startScalar = _surroundingCursorScalar + scalarOffset;
            long endScalar = startScalar + (long)scalarCount;
            if (text == null || startScalar < 0 || endScalar > UnicodeIndex.ScalarLength(text))
            {
                Debug.Assert(false, "delete_surrounding_text range outside the last surrounding-text snapshot");
                return;
            }

            int startUtf16 = UnicodeIndex.ScalarsToUtf16(text, (int)startScalar);
            int endUtf16 = UnicodeIndex.ScalarsToUtf16(text, (int)endScalar);
            _onDeleteSurrounding(startUtf16 - _surroundingCursorUtf16, endUtf16 - startUtf16);
        }

        internal void HandleCompositionChanged(IntPtr compositionPtr)
        {
            _composition = Snapshot(compositionPtr);
            _onCompositionChanged?.Invoke(_composition);
        }

        private Composition Snapshot(IntPtr compositionPtr)
        {
            var native = Marshal.PtrToStructure<PathimeCompositionNative>(compositionPtr);

            string preedit = Utf8.Decode(native.Preedit);
            int settledUtf16 = UnicodeIndex.ScalarsToUtf16(
                preedit, checked((int)native.PreeditSettled.ToUInt64()));

            int count = checked((int)native.CandidateCount.ToUInt64());
            string[] candidates;
            if (count == 0)
            {
                candidates = Array.Empty<string>();
            }
            else
            {
                candidates = new string[count];
                for (int i = 0; i < count; i++)
                {
                    // pathime_context_candidate is callback-safe.
                    PathimeStr str;
                    StatusCheck.ThrowIfError(
                        NativeMethods.pathime_context_candidate(_handle, (UIntPtr)i, out str));
                    candidates[i] = Utf8.Decode(str);
                }
            }

            return new Composition(preedit, settledUtf16, candidates,
                checked((int)native.CandidateCursor.ToUInt64()));
        }

        internal void StashCallbackException(Exception e)
        {
            if (_pendingCallbackException == null)
            {
                _pendingCallbackException = e;
            }
        }

        internal void ThrowPendingCallbackException()
        {
            if (_pendingCallbackException != null)
            {
                Exception e = _pendingCallbackException;
                _pendingCallbackException = null;
                ExceptionDispatchInfo.Capture(e).Throw();
            }
        }

        /// <summary>
        /// Destroy the context, discarding composition state without committing
        /// it. No callbacks fire. Idempotent.
        /// </summary>
        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            _engine.Unregister(this);
            NativeMethods.pathime_context_destroy(_handle);
            _handle = IntPtr.Zero;

            // Only after destroy: the library held the client block until now.
            Marshal.FreeHGlobal(_clientBlock);
            _clientBlock = IntPtr.Zero;
            _selfHandle.Free();
        }
    }
}
