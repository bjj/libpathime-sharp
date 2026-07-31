using System;

namespace PathimeSharp.Interop
{
    internal static class StatusCheck
    {
        /// <summary>
        /// Map a pathime_status_t to the exception hierarchy. The message text
        /// comes from the library's own pathime_status_string.
        /// </summary>
        public static void ThrowIfError(int status)
        {
            if (status == 0)
            {
                return;
            }

            string message = Utf8.DecodeNulTerminated(NativeMethods.pathime_status_string(status));
            if (message.Length == 0)
            {
                message = $"libpathime error {status}";
            }

            throw Create((PathimeStatus)status, message);
        }

        private static PathimeException Create(PathimeStatus status, string message)
        {
            switch (status)
            {
                case PathimeStatus.InvalidArgument:
                    return new PathimeInvalidArgumentException(message);
                case PathimeStatus.UnknownEngine:
                    return new PathimeUnknownEngineException(message);
                case PathimeStatus.MissingCallback:
                    return new PathimeMissingCallbackException(message);
                case PathimeStatus.Unsupported:
                    return new PathimeUnsupportedException(message);
                case PathimeStatus.NotInitialized:
                    return new PathimeNotInitializedException(message);
                case PathimeStatus.AlreadyInitialized:
                    return new PathimeAlreadyInitializedException(message);
                case PathimeStatus.OutOfMemory:
                    return new PathimeOutOfMemoryException(message);
                case PathimeStatus.Backend:
                    return new PathimeBackendException(message);
                default:
                    return new PathimeException(status, message);
            }
        }
    }
}
