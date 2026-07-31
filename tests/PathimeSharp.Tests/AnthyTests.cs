using Xunit;

namespace PathimeSharp.Tests
{
    /// <summary>Anthy: romaji preedit, conversion, prediction, kana script.</summary>
    public class AnthyTests : IClassFixture<AnthyFixture>
    {
        private readonly AnthyFixture _fixture;

        public AnthyTests(AnthyFixture fixture)
        {
            _fixture = fixture;
        }

        private Context NewContext() => new Context(_fixture.Engine);

        [Fact]
        public void RomajiBecomesKanaPreedit()
        {
            using var ctx = NewContext();
            ctx.Type("nihongo");
            Assert.Equal("にほんご", ctx.Composition.Preedit);
        }

        [Fact]
        public void PredictionOffersCandidatesBeforeConvert()
        {
            using var ctx = NewContext();
            Assert.Equal(true, ctx.GetOption(Option.Prediction));
            ctx.Type("nihongo");
            Assert.Contains("日本語", ctx.Composition.Candidates);
            // Browsing before conversion leaves the preedit alone.
            ctx.SetCandidateCursor(1);
            Assert.Equal("にほんご", ctx.Composition.Preedit);
        }

        [Fact]
        public void SpaceConvertsAndPreviews()
        {
            using var ctx = NewContext();
            ctx.Type("nihongo");
            ctx.ProcessKey(Key.Space);
            Assert.Equal("日本語", ctx.Composition.Preedit);
            ctx.ProcessKey(Key.Return);
            Assert.Equal("日本語", ctx.TakeCommitted());
        }

        [Fact]
        public void ReturnCommitsKanaAsTyped()
        {
            using var ctx = NewContext();
            ctx.Type("nihongo");
            ctx.ProcessKey(Key.Return);
            Assert.Equal("にほんご", ctx.TakeCommitted());
        }

        [Fact]
        public void TrailingNNormalizesAtCommit()
        {
            using var ctx = NewContext();
            ctx.Type("hon");
            Assert.Equal("ほn", ctx.Composition.Preedit); // one more key still decides
            ctx.ProcessKey(Key.Return);
            Assert.Equal("ほん", ctx.TakeCommitted());
        }

        [Fact]
        public void KatakanaScript()
        {
            using var ctx = NewContext();
            ctx.SetOption(Option.AnthyKanaScript, AnthyScript.Katakana);
            ctx.Type("nihongo");
            Assert.Equal("ニホンゴ", ctx.Composition.Preedit);
        }

        [Fact]
        public void TypingMethodResetsComposition()
        {
            OptionInfo info = _fixture.Engine.GetOptionInfo(Option.AnthyTypingMethod);
            Assert.True(info.ResetsComposition);
            using var ctx = NewContext();
            ctx.Type("nihon");
            ctx.SetOption(Option.AnthyTypingMethod, AnthyTyping.Kana);
            Assert.Equal("", ctx.Composition.Preedit);
        }
    }
}
