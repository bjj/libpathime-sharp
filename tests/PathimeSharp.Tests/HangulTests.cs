using System;
using System.Text;
using Xunit;

namespace PathimeSharp.Tests
{
    /// <summary>Hangul: syllable composition, requirements, the preedit-none client.</summary>
    public class HangulTests : IClassFixture<HangulFixture>
    {
        private readonly HangulFixture _fixture;

        public HangulTests(HangulFixture fixture)
        {
            _fixture = fixture;
        }

        private Engine Hangul => _fixture.Engine;

        [Fact]
        public void SyllableComposition()
        {
            using var ctx = new Context(Hangul);
            ctx.Type("gks"); // ㅎ ㅏ ㄴ on 2-set
            Assert.Equal("한", ctx.Composition.Preedit);
            ctx.Commit();
            Assert.Equal("한", ctx.TakeCommitted());
        }

        [Fact]
        public void SyllableCommitsWhenNextBegins()
        {
            using var ctx = new Context(Hangul);
            ctx.Type("gksrmf"); // 한글
            Assert.Equal("한", ctx.TakeCommitted());
            Assert.Equal("글", ctx.Composition.Preedit);
        }

        [Fact]
        public void NoCandidates()
        {
            using var ctx = new Context(Hangul);
            ctx.Type("gks");
            Assert.Empty(ctx.Composition.Candidates);
            Assert.Throws<PathimeInvalidArgumentException>(() => ctx.SelectCandidate(0));
        }

        [Fact]
        public void BackspaceRemovesOneJamo()
        {
            using var ctx = new Context(Hangul);
            ctx.Type("gks");
            ctx.ProcessKey(Key.Backspace);
            Assert.Equal("하", ctx.Composition.Preedit);
        }

        [Fact]
        public void MaxCandidatesUnsupported()
        {
            OptionInfo info = Hangul.GetOptionInfo(Option.MaxCandidates);
            Assert.False(info.Supported);
            Assert.Throws<PathimeUnsupportedException>(
                () => Hangul.SetOption(Option.MaxCandidates, 10L));
        }

        [Fact]
        public void WordPreeditAccumulates()
        {
            using var ctx = new Context(Hangul);
            ctx.SetOption(Option.HangulPreedit, HangulPreedit.Word);
            ctx.Type("gksrmf");
            Composition comp = ctx.Composition;
            Assert.Equal("한글", comp.Preedit);
            Assert.Equal(1, comp.PreeditSettled); // 한 is done, 글 still open
            Assert.Equal("", ctx.Committed);
            ctx.ProcessKey(Key.Space); // word boundary; hangul declines space
            Assert.Equal("한글", ctx.TakeCommitted());
        }

        [Fact]
        public void PreeditNoneRequiresDeleteCallback()
        {
            Assert.Equal(EngineRequirements.None, Hangul.Requirements);
            using var ctx = new Context(Hangul);
            Assert.Throws<PathimeMissingCallbackException>(
                () => ctx.SetOption(Option.HangulPreedit, HangulPreedit.None));
        }

        [Fact]
        public void PreeditNoneBuildsSyllableInDocument()
        {
            // The mode that exists for clients without a preedit: each
            // keystroke commits, and the syllable grows by delete-and-recommit.
            // The astral 𝄞 already in the document proves the binding's
            // scalar→UTF-16 conversion of the delete range: it is 1 scalar but
            // 2 UTF-16 units, so byte- or scalar-unit slips would corrupt it.
            var doc = new StringBuilder("\U0001D11E");
            int cursor = doc.Length; // UTF-16 index after 𝄞

            using var ctx = new Context(Hangul,
                onCommit: text =>
                {
                    doc.Insert(cursor, text);
                    cursor += text.Length;
                },
                onDeleteSurrounding: (offset, count) =>
                {
                    int start = cursor + offset;
                    doc.Remove(start, count);
                    cursor = start;
                });
            ctx.SetOption(Option.HangulPreedit, HangulPreedit.None);
            Assert.Equal(
                EngineRequirements.SurroundingText | EngineRequirements.DeleteSurrounding,
                ctx.Requirements);
            foreach (char key in "gks")
            {
                ctx.SetSurroundingText(doc.ToString(), cursor);
                ctx.ProcessKey(key);
            }

            Assert.Equal("\U0001D11E한", doc.ToString());
            Assert.Equal("", ctx.Composition.Preedit);
        }

        [Fact]
        public void EngineLevelNoneCapsForIncapableContext()
        {
            // An engine-level set succeeds; a context whose client lacks the
            // callback resolves to SYLLABLE instead, visibly.
            using var ctx = new Context(Hangul, isolate: false); // no delete callback
            Hangul.SetOption(Option.HangulPreedit, HangulPreedit.None);
            try
            {
                Assert.Equal(HangulPreedit.None, Hangul.GetOption(Option.HangulPreedit));
                Assert.Equal(HangulPreedit.Syllable, ctx.GetOption(Option.HangulPreedit));
            }
            finally
            {
                Hangul.ResetOption(Option.HangulPreedit);
            }
        }
    }
}
