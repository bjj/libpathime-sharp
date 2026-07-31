using System;

namespace PathimeSharp
{
    /// <summary>Status codes returned by libpathime (pathime_status_t).</summary>
    /// <remarks>
    /// Codes 1–6 are rejections: the call did nothing and no callbacks fired.
    /// Codes 7–8 are failures: the operation stopped partway and composition
    /// state is indeterminate until <see cref="Context.Reset"/> is called.
    /// </remarks>
    public enum PathimeStatus
    {
        Ok = 0,
        InvalidArgument = 1,
        UnknownEngine = 2,
        MissingCallback = 3,
        Unsupported = 4,
        NotInitialized = 5,
        AlreadyInitialized = 6,
        OutOfMemory = 7,
        Backend = 8,
    }

    /// <summary>Input method engines (pathime_engine_id_t).</summary>
    public enum EngineId
    {
        /// <summary>Korean Hangul composition.</summary>
        Hangul = 0,
        /// <summary>Japanese kana-kanji conversion.</summary>
        Anthy = 1,
        /// <summary>Chinese, Pinyin phonetic input.</summary>
        Pinyin = 2,
        /// <summary>Chinese, Bopomofo/Zhuyin phonetic input.</summary>
        Bopomofo = 3,
        /// <summary>Table-driven input from a loaded table.</summary>
        Table = 4,
    }

    /// <summary>What an engine needs from its client (PATHIME_REQUIRES_*).</summary>
    [Flags]
    public enum EngineRequirements : uint
    {
        None = 0,
        /// <summary>The client must supply surrounding text.</summary>
        SurroundingText = 1u << 0,
        /// <summary>The client must handle delete-surrounding-text requests.</summary>
        DeleteSurrounding = 1u << 1,
    }

    /// <summary>Keyboard modifier state (PATHIME_MOD_*).</summary>
    [Flags]
    public enum KeyModifiers : uint
    {
        None = 0,
        Shift = 1u << 0,
        Control = 1u << 1,
        Alt = 1u << 2,
        /// <summary>Windows / Command key.</summary>
        Super = 1u << 3,
        /// <summary>CapsLock latched.</summary>
        CapsLock = 1u << 4,
        NumLock = 1u << 5,
    }

    /// <summary>
    /// Non-printable key symbols (PATHIME_KEY_*), as X11 keysym values.
    /// Printable characters use <see cref="Pathime.KeysymForChar(char)"/>.
    /// </summary>
    public enum Key : uint
    {
        Backspace = 0xff08,
        Tab = 0xff09,
        Return = 0xff0d,
        Escape = 0xff1b,
        /// <summary>Printable, but the usual convert key.</summary>
        Space = 0x0020,
        Delete = 0xffff,
        Home = 0xff50,
        Left = 0xff51,
        Up = 0xff52,
        Right = 0xff53,
        Down = 0xff54,
        PageUp = 0xff55,
        PageDown = 0xff56,
        End = 0xff57,
        /// <summary>Cancel conversion (Japanese).</summary>
        Muhenkan = 0xff22,
        /// <summary>Begin/advance conversion (Japanese).</summary>
        Henkan = 0xff23,
    }

    /// <summary>How an option's value is typed (pathime_option_type_t).</summary>
    public enum OptionType
    {
        Bool = 0,
        Int = 1,
        Enum = 2,
        Flags = 3,
        String = 4,
    }

    /// <summary>Engine options (pathime_option_t).</summary>
    public enum Option
    {
        MaxCandidates = 0,
        Learning = 1,
        LatinWidth = 2,
        PunctuationWidth = 3,
        ChineseVariant = 4,
        Prediction = 5,
        SpecialPhrases = 6,
        IncompleteInput = 7,
        HangulLayout = 8,
        HangulAutoReorder = 9,
        HangulDoubleStrokeCombine = 10,
        HangulNonChoseongCombine = 11,
        HangulPreedit = 12,
        AnthyTypingMethod = 13,
        AnthyKanaScript = 14,
        AnthyPeriodStyle = 15,
        AnthySymbolStyle = 16,
        AnthyOnPeriod = 17,
        AnthyLatinWithShift = 18,
        PinyinScheme = 19,
        PinyinFuzzy = 20,
        PinyinCorrection = 21,
        BopomofoLayout = 22,
        TableFile = 23,
        TableAutoCommit = 24,
        TableAutoSelect = 25,
        TableSingleWildcard = 26,
        TableMultiWildcard = 27,
        TableSingleCharOnly = 28,
        TableInvalidInput = 29,
        TablePinyinFallback = 30,
    }

    /// <summary>Half- or full-width character output (pathime_width_t).</summary>
    public enum TextWidth
    {
        Half = 0,
        Full = 1,
    }

    /// <summary>Chinese script variant preference (pathime_chinese_variant_t).</summary>
    public enum ChineseVariant
    {
        SimplifiedOnly = 0,
        TraditionalOnly = 1,
        SimplifiedFirst = 2,
        TraditionalFirst = 3,
        Any = 4,
    }

    /// <summary>Hangul keyboard layouts (pathime_hangul_layout_t).</summary>
    public enum HangulLayout
    {
        /// <summary>Dubeolsik; the common layout.</summary>
        Set2 = 0,
        /// <summary>Dubeolsik Yetgeul, with Old Hangul.</summary>
        Set2Yet = 1,
        /// <summary>Sebeolsik on a two-set keyboard.</summary>
        Set3_2 = 2,
        Set3_390 = 3,
        Set3Final = 4,
        Set3NoShift = 5,
        /// <summary>Sebeolsik Yetgeul, with Old Hangul.</summary>
        Set3Yet = 6,
        /// <summary>Latin transliteration.</summary>
        Romaja = 7,
        Ahnmatae = 8,
    }

    /// <summary>How much Hangul composition is held in the preedit (pathime_hangul_preedit_t).</summary>
    public enum HangulPreedit
    {
        /// <summary>Commit each syllable as it finishes.</summary>
        Syllable = 0,
        /// <summary>Hold whole words before committing.</summary>
        Word = 1,
        /// <summary>Hold nothing; build the syllable in the document.</summary>
        None = 2,
    }

    /// <summary>Anthy typing method (pathime_anthy_typing_t).</summary>
    public enum AnthyTyping
    {
        /// <summary>Spell kana in Latin letters.</summary>
        Romaji = 0,
        /// <summary>Strike kana directly.</summary>
        Kana = 1,
    }

    /// <summary>Anthy kana script (pathime_anthy_script_t).</summary>
    public enum AnthyScript
    {
        Hiragana = 0,
        Katakana = 1,
        HalfwidthKatakana = 2,
    }

    /// <summary>Anthy period style (pathime_anthy_period_t).</summary>
    public enum AnthyPeriod
    {
        /// <summary>。 and 、</summary>
        Kuten = 0,
        /// <summary>． and ，</summary>
        Fullwidth = 1,
    }

    /// <summary>Anthy symbol style (pathime_anthy_symbol_t).</summary>
    public enum AnthySymbol
    {
        /// <summary>「 」 ／</summary>
        CornerSlash = 0,
        /// <summary>「 」 ・</summary>
        CornerMiddot = 1,
        /// <summary>［ ］ ／</summary>
        BracketSlash = 2,
        /// <summary>［ ］ ・</summary>
        BracketMiddot = 3,
    }

    /// <summary>What Anthy does when a period is typed (pathime_anthy_on_period_t).</summary>
    public enum AnthyOnPeriod
    {
        /// <summary>Insert it and carry on.</summary>
        Nothing = 0,
        /// <summary>Begin conversion.</summary>
        Convert = 1,
        /// <summary>Commit the composition.</summary>
        Commit = 2,
    }

    /// <summary>Pinyin input scheme (pathime_pinyin_scheme_t).</summary>
    public enum PinyinScheme
    {
        /// <summary>Syllables spelled out in full.</summary>
        Full = 0,
        /// <summary>Microsoft double pinyin.</summary>
        DoubleMspy = 1,
        /// <summary>Ziranma.</summary>
        DoubleZrm = 2,
        /// <summary>Zhineng ABC.</summary>
        DoubleAbc = 3,
        /// <summary>Zhongwen Zhixing.</summary>
        DoubleZgpy = 4,
        /// <summary>Pinyin Jiajia.</summary>
        DoublePyjj = 5,
        /// <summary>Xiaohe.</summary>
        DoubleXhe = 6,
    }

    /// <summary>Bopomofo keyboard layouts (pathime_bopomofo_layout_t).</summary>
    public enum BopomofoLayout
    {
        Standard = 0,
        ChingYeah = 1,
        Eten = 2,
        Ibm = 3,
    }

    /// <summary>What a table engine does with invalid input (pathime_table_invalid_t).</summary>
    public enum TableInvalidInput
    {
        /// <summary>Commit the current candidate.</summary>
        CommitCandidate = 0,
        /// <summary>Commit the keys as typed.</summary>
        CommitRaw = 1,
    }

    /// <summary>Pinyin fuzzy-matching pairs (PATHIME_PINYIN_FUZZY_*).</summary>
    [Flags]
    public enum PinyinFuzzy : uint
    {
        None = 0,
        CCh = 1u << 0,
        ChC = 1u << 1,
        ZZh = 1u << 2,
        ZhZ = 1u << 3,
        SSh = 1u << 4,
        ShS = 1u << 5,
        LN = 1u << 6,
        NL = 1u << 7,
        FH = 1u << 8,
        HF = 1u << 9,
        LR = 1u << 10,
        RL = 1u << 11,
        KG = 1u << 12,
        GK = 1u << 13,
        /// <summary>Also governs ian/iang and uan/uang.</summary>
        AnAng = 1u << 14,
        /// <summary>Also governs iang/ian and uang/uan.</summary>
        AngAn = 1u << 15,
        EnEng = 1u << 16,
        EngEn = 1u << 17,
        InIng = 1u << 18,
        IngIn = 1u << 19,
    }

    /// <summary>Pinyin auto-correction rules (PATHIME_PINYIN_CORRECT_*).</summary>
    [Flags]
    public enum PinyinCorrect : uint
    {
        None = 0,
        GnNg = 1u << 0,
        MgNg = 1u << 1,
        IouIu = 1u << 2,
        UeiUi = 1u << 3,
        UenUn = 1u << 4,
        UeVe = 1u << 5,
        VU = 1u << 6,
        OnOng = 1u << 7,
    }
}
