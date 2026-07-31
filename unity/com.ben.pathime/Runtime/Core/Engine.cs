using System;
using System.Collections.Generic;
using PathimeSharp.Interop;

namespace PathimeSharp
{
    /// <summary>
    /// An input method engine: shared read-only method data plus the
    /// engine-level option tier its contexts inherit from.
    /// </summary>
    /// <remarks>
    /// <para>Dispose order is a contract: dispose every <see cref="Context"/>
    /// before its engine, and every engine before <see cref="Pathime.Shutdown"/>.
    /// <see cref="Dispose"/> defensively disposes any still-live contexts
    /// first.</para>
    /// <para>There are no finalizers anywhere in this binding: libpathime
    /// forbids overlapping calls, and a finalizer thread would race the input
    /// thread. A forgotten engine or context is a leak, not a crash.</para>
    /// </remarks>
    public sealed class Engine : OptionScope, IDisposable
    {
        private IntPtr _handle;
        private readonly List<WeakReference<Context>> _contexts = new List<WeakReference<Context>>();

        /// <summary>
        /// Create an engine. Throws <see cref="PathimeUnknownEngineException"/>
        /// when the engine is not available (not compiled in, backend missing,
        /// or its data files not found — check <see cref="Pathime.HasEngine"/>).
        /// </summary>
        public Engine(EngineId id)
        {
            LibraryLoader.EnsureLoaded();
            StatusCheck.ThrowIfError(NativeMethods.pathime_engine_create((int)id, out _handle));
        }

        internal IntPtr Handle
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new ObjectDisposedException(nameof(Engine));
                }

                return _handle;
            }
        }

        /// <summary>Which engine this is.</summary>
        public EngineId Id => (EngineId)NativeMethods.pathime_engine_id(Handle);

        /// <summary>What this engine needs from a client, before options are applied.</summary>
        public EngineRequirements Requirements =>
            (EngineRequirements)NativeMethods.pathime_engine_requirements(Handle);

        internal override IntPtr OptionHandle => Handle;
        internal override IntPtr InfoEngineHandle => Handle;
        private protected override bool IsEngineScope => true;

        internal void Register(Context context)
        {
            _contexts.Add(new WeakReference<Context>(context));
        }

        internal void Unregister(Context context)
        {
            _contexts.RemoveAll(weak =>
            {
                Context? target;
                return !weak.TryGetTarget(out target) || ReferenceEquals(target, context);
            });
        }

        /// <summary>
        /// An engine-level setter dispatches callbacks belonging to contexts it
        /// was never passed; their exceptions surface here, after the fact.
        /// </summary>
        private protected override void ThrowPendingCallbackExceptions()
        {
            for (int i = _contexts.Count - 1; i >= 0; i--)
            {
                Context? context;
                if (_contexts[i].TryGetTarget(out context))
                {
                    context.ThrowPendingCallbackException();
                }
                else
                {
                    _contexts.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Destroy the engine, defensively disposing any live contexts first.
        /// Idempotent.
        /// </summary>
        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            for (int i = _contexts.Count - 1; i >= 0; i--)
            {
                Context? context;
                if (_contexts[i].TryGetTarget(out context))
                {
                    context.Dispose(); // unregisters itself
                }
            }
            _contexts.Clear();

            NativeMethods.pathime_engine_destroy(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
