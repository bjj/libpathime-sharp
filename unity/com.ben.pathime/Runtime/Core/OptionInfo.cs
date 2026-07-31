using PathimeSharp.Interop;

namespace PathimeSharp
{
    /// <summary>
    /// What one engine says about one option (pathime_option_info_t), as an
    /// owned copy.
    /// </summary>
    public sealed class OptionInfo
    {
        internal OptionInfo(in PathimeOptionInfoNative native)
        {
            Type = (OptionType)native.Type;
            Supported = native.Supported != 0;
            ResetsComposition = native.ResetsComposition != 0;
            DefaultValue = native.DefaultValue;
            MinValue = native.MinValue;
            MaxValue = native.MaxValue;
            ValidValues = native.ValidValues;
            DefaultString = Utf8.Decode(native.DefaultString);
            ValidValueCount = checked((int)native.ValidValueCount.ToUInt64());
        }

        public OptionType Type { get; }

        /// <summary>
        /// False if the engine does not implement the option; every other
        /// member is then unspecified and setters throw
        /// <see cref="PathimeUnsupportedException"/>.
        /// </summary>
        public bool Supported { get; }

        /// <summary>True if setting this option discards composition state.</summary>
        public bool ResetsComposition { get; }

        /// <summary>Bool/Int/Enum/Flags: the library default.</summary>
        public long DefaultValue { get; }

        /// <summary>Int only: inclusive lower bound.</summary>
        public long MinValue { get; }

        /// <summary>Int only: inclusive upper bound (long.MaxValue = none).</summary>
        public long MaxValue { get; }

        /// <summary>
        /// Enum: bit i set means value i is legal. Flags: the honoured bits.
        /// </summary>
        public ulong ValidValues { get; }

        /// <summary>String only: the library default, "" when none.</summary>
        public string DefaultString { get; }

        /// <summary>
        /// String only: how many legal values can be enumerated via
        /// <see cref="Pathime.GetOptionValueName"/> (table files); 0 when the
        /// values are not a closed set.
        /// </summary>
        public int ValidValueCount { get; }

        /// <summary>The default, typed per <see cref="Type"/>.</summary>
        public object Default
        {
            get
            {
                switch (Type)
                {
                    case OptionType.Bool:
                        return DefaultValue != 0;
                    case OptionType.String:
                        return DefaultString;
                    default:
                        return DefaultValue;
                }
            }
        }
    }
}
