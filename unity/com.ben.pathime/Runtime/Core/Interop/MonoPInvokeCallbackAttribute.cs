using System;

namespace PathimeSharp.Interop
{
    /// <summary>
    /// Marks a static method as the target of a native function pointer so
    /// Unity's IL2CPP AOT compiler generates a reverse-P/Invoke wrapper for it.
    /// IL2CPP recognizes the attribute by type name alone, so this local
    /// definition works without referencing any Unity assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type delegateType)
        {
            DelegateType = delegateType;
        }

        public Type DelegateType { get; }
    }
}
