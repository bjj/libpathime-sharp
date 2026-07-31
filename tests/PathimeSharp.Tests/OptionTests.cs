using System;
using Xunit;

namespace PathimeSharp.Tests
{
    /// <summary>
    /// Option resolution across the two levels, typed access, introspection,
    /// and isolation. Note: contexts isolate by default in this binding, so
    /// tests of live engine→context inheritance pass <c>isolate: false</c>.
    /// </summary>
    public class OptionTests : IClassFixture<PinyinFixture>
    {
        private readonly PinyinFixture _fixture;

        public OptionTests(PinyinFixture fixture)
        {
            _fixture = fixture;
        }

        private Engine Pinyin => _fixture.Engine;

        [Fact]
        public void EngineValueIsContextDefaultWhenNotIsolated()
        {
            using var ctx = new Context(Pinyin, isolate: false);
            Pinyin.SetOption(Option.LatinWidth, TextWidth.Full);
            try
            {
                Assert.Equal(TextWidth.Full, ctx.GetOption(Option.LatinWidth));
                Assert.False(ctx.IsOptionSet(Option.LatinWidth));
                Assert.True(Pinyin.IsOptionSet(Option.LatinWidth));
            }
            finally
            {
                Pinyin.ResetOption(Option.LatinWidth);
            }

            Assert.Equal(TextWidth.Half, ctx.GetOption(Option.LatinWidth));
        }

        [Fact]
        public void ContextOverridesEngine()
        {
            using var ctx = new Context(Pinyin, isolate: false);
            ctx.SetOption(Option.LatinWidth, TextWidth.Full);
            Assert.True(ctx.IsOptionSet(Option.LatinWidth));
            Assert.Equal(TextWidth.Half, Pinyin.GetOption(Option.LatinWidth));
            ctx.ResetOption(Option.LatinWidth);
            Assert.Equal(TextWidth.Half, ctx.GetOption(Option.LatinWidth));
        }

        [Fact]
        public void TypedValuesRoundTrip()
        {
            using var ctx = new Context(Pinyin);
            Assert.Equal(true, ctx.GetOption(Option.SpecialPhrases));
            Assert.IsType<ChineseVariant>(ctx.GetOption(Option.ChineseVariant));
            Assert.IsType<PinyinFuzzy>(ctx.GetOption(Option.PinyinFuzzy));
            ctx.SetOption(Option.PinyinFuzzy, PinyinFuzzy.ZZh | PinyinFuzzy.ZhZ);
            Assert.Equal(PinyinFuzzy.ZZh | PinyinFuzzy.ZhZ, ctx.GetOption(Option.PinyinFuzzy));
            ctx.ResetOption(Option.PinyinFuzzy);
        }

        [Fact]
        public void PyzySupportsOnlyExclusiveVariants()
        {
            OptionInfo info = Pinyin.GetOptionInfo(Option.ChineseVariant);
            Assert.NotEqual(0ul, info.ValidValues & (1ul << (int)ChineseVariant.SimplifiedOnly));
            Assert.NotEqual(0ul, info.ValidValues & (1ul << (int)ChineseVariant.TraditionalOnly));
            Assert.Equal(0ul, info.ValidValues & (1ul << (int)ChineseVariant.Any));
            using var ctx = new Context(Pinyin);
            Assert.Throws<PathimeInvalidArgumentException>(
                () => ctx.SetOption(Option.ChineseVariant, ChineseVariant.Any));
        }

        [Fact]
        public void WrongSetterTypeIsInvalid()
        {
            Assert.Throws<PathimeInvalidArgumentException>(
                () => Pinyin.SetOption(Option.LatinWidth, "full"));
        }

        [Fact]
        public void IntBounds()
        {
            OptionInfo info = Pinyin.GetOptionInfo(Option.MaxCandidates);
            Assert.Equal(OptionType.Int, info.Type);
            Assert.Equal(1, info.MinValue);
            Assert.Equal((long)Pathime.DefaultMaxCandidates, info.Default);
            using var ctx = new Context(Pinyin);
            Assert.Throws<PathimeInvalidArgumentException>(
                () => ctx.SetOption(Option.MaxCandidates, 0L));
        }

        [Fact]
        public void FullInventoryWalk()
        {
            // The promise a settings UI relies on: every option in
            // [0, OptionCount) describes itself completely.
            for (int i = 0; i < Pathime.OptionCount; i++)
            {
                var option = (Option)i;
                Assert.NotEqual("", Pathime.GetOptionName(option));
                OptionInfo info = Pinyin.GetOptionInfo(option);
                if (!info.Supported)
                {
                    continue;
                }

                if (info.Type == OptionType.Enum || info.Type == OptionType.Flags)
                {
                    Assert.NotEqual(0ul, info.ValidValues);
                    for (int bit = 0; bit < 64; bit++)
                    {
                        if ((info.ValidValues & (1ul << bit)) == 0)
                        {
                            continue;
                        }

                        long value = info.Type == OptionType.Flags ? 1L << bit : bit;
                        Assert.NotEqual("", Pathime.GetOptionValueName(option, value));
                    }
                }
            }
        }

        [Fact]
        public void IsolatedContextIsPassedBy()
        {
            int changes = 0;
            using var ctx = new Context(Pinyin, onCompositionChanged: _ => changes++, isolate: false);
            ctx.IsolateOptions();
            // Every implemented option is now an ordinary override here...
            Assert.True(ctx.IsOptionSet(Option.LatinWidth));
            // ...and only those: options this engine does not implement stay unset.
            Assert.False(ctx.IsOptionSet(Option.HangulLayout));

            int before = changes;
            Pinyin.SetOption(Option.LatinWidth, TextWidth.Full);
            try
            {
                Assert.Equal(TextWidth.Half, ctx.GetOption(Option.LatinWidth));
                Assert.Equal(before, changes); // the broadcast skipped this context
            }
            finally
            {
                Pinyin.ResetOption(Option.LatinWidth);
            }

            // ResetOption drops one copy and re-attaches that option.
            ctx.ResetOption(Option.LatinWidth);
            Pinyin.SetOption(Option.LatinWidth, TextWidth.Full);
            try
            {
                Assert.Equal(TextWidth.Full, ctx.GetOption(Option.LatinWidth));
            }
            finally
            {
                Pinyin.ResetOption(Option.LatinWidth);
            }
        }

        [Fact]
        public void IsolateAtConstructionReadsEngineAsTemplate()
        {
            Pinyin.SetOption(Option.ChineseVariant, ChineseVariant.TraditionalOnly);
            try
            {
                using var ctx = new Context(Pinyin); // isolate: true is the default
                Pinyin.ResetOption(Option.ChineseVariant);
                Assert.Equal(ChineseVariant.TraditionalOnly, ctx.GetOption(Option.ChineseVariant));
                Assert.True(ctx.IsOptionSet(Option.ChineseVariant));
            }
            finally
            {
                Pinyin.ResetOption(Option.ChineseVariant);
            }
        }

        [Fact]
        public void EngineSetUpdatesOpenNonIsolatedContextImmediately()
        {
            int changes = 0;
            using var ctx = new Context(Pinyin, onCompositionChanged: _ => changes++, isolate: false);
            ctx.Type("ma");
            Assert.Contains("马", ctx.Composition.Candidates);
            int before = changes;
            Pinyin.SetOption(Option.ChineseVariant, ChineseVariant.TraditionalOnly);
            try
            {
                Assert.True(changes > before); // announced, not silent
                Assert.Contains("馬", ctx.Composition.Candidates);
                Assert.DoesNotContain("马", ctx.Composition.Candidates);
            }
            finally
            {
                Pinyin.ResetOption(Option.ChineseVariant);
            }
        }

        [Fact]
        public void EngineSetterSurfacesCallbackExceptionsFromItsContexts()
        {
            // An engine-level set of a resets_composition option dispatches
            // callbacks into contexts it was never passed; the binding routes
            // their exceptions out of the engine call.
            bool explode = false;
            using var ctx = new Context(Pinyin,
                onCompositionChanged: _ =>
                {
                    if (explode)
                    {
                        throw new InvalidOperationException("fan-out bug");
                    }
                },
                isolate: false);
            ctx.Type("ma");
            explode = true;
            try
            {
                var thrown = Assert.Throws<InvalidOperationException>(
                    () => Pinyin.SetOption(Option.ChineseVariant, ChineseVariant.TraditionalOnly));
                Assert.Equal("fan-out bug", thrown.Message);
            }
            finally
            {
                explode = false; // the reset dispatches callbacks too
                Pinyin.ResetOption(Option.ChineseVariant);
            }
        }
    }
}
