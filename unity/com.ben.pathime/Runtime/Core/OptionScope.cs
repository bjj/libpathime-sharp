using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PathimeSharp.Interop;

namespace PathimeSharp
{
    /// <summary>
    /// Shared option access for <see cref="Engine"/> (defaults inherited by
    /// its contexts) and <see cref="Context"/> (per-context overrides).
    /// </summary>
    /// <remarks>
    /// An engine-level set takes effect immediately in every context that has
    /// not overridden that option — dispatching callbacks into those contexts,
    /// synchronously, inside the setter. A <see cref="Context"/> created with
    /// its default <c>isolate: true</c> is immune; see
    /// <see cref="Context.IsolateOptions"/>.
    /// </remarks>
    public abstract class OptionScope
    {
        private protected OptionScope()
        {
        }

        /// <summary>The native handle for option calls; throws when disposed.</summary>
        internal abstract IntPtr OptionHandle { get; }

        /// <summary>The engine handle used for pathime_engine_option_info.</summary>
        internal abstract IntPtr InfoEngineHandle { get; }

        private protected abstract bool IsEngineScope { get; }

        /// <summary>
        /// Surface exceptions thrown by callbacks that ran inside a setter.
        /// </summary>
        private protected abstract void ThrowPendingCallbackExceptions();

        // Options whose Enum/Flags values mirror a C# enum type, for GetOption.
        private static readonly Dictionary<Option, Type> ValueEnumTypes = new Dictionary<Option, Type>
        {
            { Option.LatinWidth, typeof(TextWidth) },
            { Option.PunctuationWidth, typeof(TextWidth) },
            { Option.ChineseVariant, typeof(ChineseVariant) },
            { Option.HangulLayout, typeof(HangulLayout) },
            { Option.HangulPreedit, typeof(HangulPreedit) },
            { Option.AnthyTypingMethod, typeof(AnthyTyping) },
            { Option.AnthyKanaScript, typeof(AnthyScript) },
            { Option.AnthyPeriodStyle, typeof(AnthyPeriod) },
            { Option.AnthySymbolStyle, typeof(AnthySymbol) },
            { Option.AnthyOnPeriod, typeof(AnthyOnPeriod) },
            { Option.PinyinScheme, typeof(PinyinScheme) },
            { Option.PinyinFuzzy, typeof(PinyinFuzzy) },
            { Option.PinyinCorrection, typeof(PinyinCorrect) },
            { Option.BopomofoLayout, typeof(BopomofoLayout) },
            { Option.TableInvalidInput, typeof(TableInvalidInput) },
        };

        /// <summary>What this scope's engine says about an option.</summary>
        public OptionInfo GetOptionInfo(Option option)
        {
            var native = new PathimeOptionInfoNative
            {
                StructSize = (UIntPtr)Marshal.SizeOf<PathimeOptionInfoNative>(),
            };
            StatusCheck.ThrowIfError(
                NativeMethods.pathime_engine_option_info(InfoEngineHandle, (int)option, ref native));
            return new OptionInfo(native);
        }

        public void SetOption(Option option, bool value)
        {
            int status = IsEngineScope
                ? NativeMethods.pathime_engine_set_option_bool(OptionHandle, (int)option, value)
                : NativeMethods.pathime_context_set_option_bool(OptionHandle, (int)option, value);
            CompleteMutation(status);
        }

        public void SetOption(Option option, long value)
        {
            int status = IsEngineScope
                ? NativeMethods.pathime_engine_set_option_int(OptionHandle, (int)option, value)
                : NativeMethods.pathime_context_set_option_int(OptionHandle, (int)option, value);
            CompleteMutation(status);
        }

        /// <summary>Set an Enum or Flags option from its mirroring C# enum.</summary>
        public void SetOption(Option option, Enum value)
        {
            SetOption(option, Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture));
        }

        public void SetOption(Option option, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            IntPtr valuePtr = IntPtr.Zero;
            int status;
            try
            {
                valuePtr = Utf8.AllocNulTerminated(value);
                status = IsEngineScope
                    ? NativeMethods.pathime_engine_set_option_string(OptionHandle, (int)option, valuePtr)
                    : NativeMethods.pathime_context_set_option_string(OptionHandle, (int)option, valuePtr);
            }
            finally
            {
                Utf8.Free(valuePtr);
            }

            CompleteMutation(status);
        }

        public bool GetOptionBool(Option option)
        {
            bool value;
            int status = IsEngineScope
                ? NativeMethods.pathime_engine_get_option_bool(OptionHandle, (int)option, out value)
                : NativeMethods.pathime_context_get_option_bool(OptionHandle, (int)option, out value);
            StatusCheck.ThrowIfError(status);
            return value;
        }

        public long GetOptionInt(Option option)
        {
            long value;
            int status = IsEngineScope
                ? NativeMethods.pathime_engine_get_option_int(OptionHandle, (int)option, out value)
                : NativeMethods.pathime_context_get_option_int(OptionHandle, (int)option, out value);
            StatusCheck.ThrowIfError(status);
            return value;
        }

        public string GetOptionString(Option option)
        {
            PathimeStr value;
            int status = IsEngineScope
                ? NativeMethods.pathime_engine_get_option_string(OptionHandle, (int)option, out value)
                : NativeMethods.pathime_context_get_option_string(OptionHandle, (int)option, out value);
            StatusCheck.ThrowIfError(status);
            return Utf8.Decode(value); // eager copy; borrowed until the next mutating call
        }

        /// <summary>
        /// The option's effective value, typed per its declared
        /// <see cref="OptionType"/>: bool, long, string, or the mirroring C#
        /// enum for Enum/Flags options.
        /// </summary>
        public object GetOption(Option option)
        {
            OptionInfo info = GetOptionInfo(option);
            switch (info.Type)
            {
                case OptionType.Bool:
                    return GetOptionBool(option);
                case OptionType.String:
                    return GetOptionString(option);
                case OptionType.Enum:
                case OptionType.Flags:
                    long value = GetOptionInt(option);
                    Type? enumType;
                    return ValueEnumTypes.TryGetValue(option, out enumType)
                        ? Enum.ToObject(enumType!, value)
                        : (object)value;
                default:
                    return GetOptionInt(option);
            }
        }

        /// <summary>Return the option to inheriting/default behaviour.</summary>
        public void ResetOption(Option option)
        {
            int status = IsEngineScope
                ? NativeMethods.pathime_engine_reset_option(OptionHandle, (int)option)
                : NativeMethods.pathime_context_reset_option(OptionHandle, (int)option);
            CompleteMutation(status);
        }

        /// <summary>Whether the option is explicitly set at this scope.</summary>
        public bool IsOptionSet(Option option)
        {
            return IsEngineScope
                ? NativeMethods.pathime_engine_option_is_set(OptionHandle, (int)option)
                : NativeMethods.pathime_context_option_is_set(OptionHandle, (int)option);
        }

        private protected void CompleteMutation(int status)
        {
            // A callback exception is the client's own bug and wins over the
            // library status, matching the reference binding.
            ThrowPendingCallbackExceptions();
            StatusCheck.ThrowIfError(status);
        }
    }
}
