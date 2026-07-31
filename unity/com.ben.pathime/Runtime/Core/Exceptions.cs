using System;

namespace PathimeSharp
{
    /// <summary>
    /// Base class for errors reported by libpathime.
    /// </summary>
    /// <remarks>
    /// Two classes of status exist, with different recovery semantics. The
    /// direct subclasses of this type (statuses 1–6) are <b>rejections</b>: the
    /// call did nothing, no callbacks fired, and every handle is exactly as it
    /// was. Subclasses of <see cref="PathimeFailureException"/> (statuses 7–8)
    /// are <b>failures</b>: the operation stopped partway through.
    /// </remarks>
    public class PathimeException : Exception
    {
        public PathimeException(PathimeStatus status, string message)
            : base(message)
        {
            Status = status;
        }

        /// <summary>The status code the library returned.</summary>
        public PathimeStatus Status { get; }
    }

    /// <summary>NULL handle, bad index, bad UTF-8, or bad struct_size.</summary>
    public sealed class PathimeInvalidArgumentException : PathimeException
    {
        public PathimeInvalidArgumentException(string message)
            : base(PathimeStatus.InvalidArgument, message) { }
    }

    /// <summary>The engine is not available in this library build or installation.</summary>
    public sealed class PathimeUnknownEngineException : PathimeException
    {
        public PathimeUnknownEngineException(string message)
            : base(PathimeStatus.UnknownEngine, message) { }
    }

    /// <summary>The client lacks a callback the engine requires.</summary>
    public sealed class PathimeMissingCallbackException : PathimeException
    {
        public PathimeMissingCallbackException(string message)
            : base(PathimeStatus.MissingCallback, message) { }
    }

    /// <summary>The engine does not implement this operation.</summary>
    public sealed class PathimeUnsupportedException : PathimeException
    {
        public PathimeUnsupportedException(string message)
            : base(PathimeStatus.Unsupported, message) { }
    }

    /// <summary><see cref="Pathime.Init"/> has not been called.</summary>
    public sealed class PathimeNotInitializedException : PathimeException
    {
        public PathimeNotInitializedException(string message)
            : base(PathimeStatus.NotInitialized, message) { }
    }

    /// <summary><see cref="Pathime.Init"/> has already succeeded.</summary>
    public sealed class PathimeAlreadyInitializedException : PathimeException
    {
        public PathimeAlreadyInitializedException(string message)
            : base(PathimeStatus.AlreadyInitialized, message) { }
    }

    /// <summary>
    /// The operation stopped partway through (statuses 7–8). Composition state
    /// on the affected context is indeterminate: call <see cref="Context.Reset"/>
    /// before trusting or displaying it.
    /// </summary>
    public class PathimeFailureException : PathimeException
    {
        public PathimeFailureException(PathimeStatus status, string message)
            : base(status, message) { }
    }

    /// <summary>Memory allocation failed inside the library or a backend.</summary>
    public sealed class PathimeOutOfMemoryException : PathimeFailureException
    {
        public PathimeOutOfMemoryException(string message)
            : base(PathimeStatus.OutOfMemory, message) { }
    }

    /// <summary>A backend library or data file failed.</summary>
    public sealed class PathimeBackendException : PathimeFailureException
    {
        public PathimeBackendException(string message)
            : base(PathimeStatus.Backend, message) { }
    }
}
