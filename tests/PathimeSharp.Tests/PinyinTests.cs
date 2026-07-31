using Xunit;

namespace PathimeSharp.Tests
{
    /// <summary>Pinyin end to end: the README example, candidates, cursor, commits.</summary>
    public class PinyinTests : IClassFixture<PinyinFixture>
    {
        private readonly PinyinFixture _fixture;

        public PinyinTests(PinyinFixture fixture)
        {
            _fixture = fixture;
        }

        private Context NewContext() => new Context(_fixture.Engine);

        [Fact]
        public void NihaoFirstCandidate()
        {
            using var ctx = NewContext();
            ctx.Type("nihao");
            Composition comp = ctx.Composition;
            Assert.Equal("ni hao", comp.Preedit);
            Assert.Equal("你好", comp.Candidates[0]);
            ctx.SelectCandidate(0);
            Assert.Equal("你好", ctx.TakeCommitted());
            Assert.Equal("", ctx.Composition.Preedit);
        }

        [Fact]
        public void PrintableKeysAreHandled()
        {
            using var ctx = NewContext();
            Assert.True(ctx.ProcessKey('n'));
        }

        [Fact]
        public void ReturnCommitsAsTyped()
        {
            using var ctx = NewContext();
            ctx.Type("nihao");
            Assert.True(ctx.ProcessKey(Key.Return));
            // Separators between syllables are a commit-time normalization.
            Assert.Equal("nihao", ctx.TakeCommitted());
        }

        [Fact]
        public void GreedySelectionProducesFreshList()
        {
            using var ctx = NewContext();
            ctx.Type("nihao");
            int index = IndexOf(ctx.Composition, "你");
            ctx.SelectCandidate(index);
            Composition comp = ctx.Composition;
            Assert.StartsWith("你", comp.Preedit);
            Assert.Equal(1, comp.PreeditSettled); // 你 is settled, "hao" still open
            Assert.NotEmpty(comp.Candidates);     // alternatives for "hao"
            Assert.Equal("", ctx.Committed);      // nothing committed yet
        }

        [Fact]
        public void CandidateByIndexMatchesSnapshot()
        {
            using var ctx = NewContext();
            ctx.Type("ma");
            Composition comp = ctx.Composition;
            for (int i = 0; i < comp.CandidateCount && i < 10; i++)
            {
                Assert.Equal(comp.Candidates[i], ctx.GetCandidate(i));
            }
        }

        [Fact]
        public void CandidateCursorRoundTrips()
        {
            using var ctx = NewContext();
            ctx.Type("ma");
            ctx.SetCandidateCursor(2);
            Assert.Equal(2, ctx.Composition.CandidateCursor);
            Assert.Throws<PathimeInvalidArgumentException>(
                () => ctx.SetCandidateCursor(ctx.Composition.CandidateCount));
        }

        [Fact]
        public void BackspaceShrinksPreedit()
        {
            using var ctx = NewContext();
            ctx.Type("nihao");
            ctx.ProcessKey(Key.Backspace);
            Assert.Equal("ni ha", ctx.Composition.Preedit);
        }

        [Fact]
        public void EscapeAbandonsComposition()
        {
            using var ctx = NewContext();
            ctx.Type("nihao");
            ctx.ProcessKey(Key.Escape);
            Assert.Equal("", ctx.Composition.Preedit);
            Assert.Equal("", ctx.Committed);
        }

        [Fact]
        public void ExplicitCommitKeepsText()
        {
            using var ctx = NewContext();
            ctx.Type("nihao");
            ctx.Commit();
            Assert.Equal("nihao", ctx.TakeCommitted());
            ctx.Commit(); // empty composition: a documented no-op
            Assert.Equal("", ctx.TakeCommitted());
        }

        [Fact]
        public void ResetDiscardsSilently()
        {
            using var ctx = NewContext();
            ctx.Type("nihao");
            ctx.Reset();
            Assert.Equal("", ctx.Composition.Preedit);
            Assert.Equal("", ctx.Committed);
        }

        [Fact]
        public void MaxCandidatesCapsAndAppends()
        {
            using var ctx = NewContext();
            ctx.SetOption(Option.MaxCandidates, 5L);
            ctx.Type("ma");
            var first = ctx.Composition.Candidates;
            Assert.Equal(5, first.Count);
            ctx.SetOption(Option.MaxCandidates, 10L);
            var more = ctx.Composition.Candidates;
            Assert.True(more.Count > 5);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(first[i], more[i]); // appended, never renumbered
            }
        }

        [Fact]
        public void FullStopAfterDigitStaysPeriod()
        {
            using var ctx = NewContext();
            ctx.SetSurroundingText("1", 1);
            ctx.ProcessKey('.');
            Assert.Equal(".", ctx.TakeCommitted());
            ctx.SetSurroundingText("abc", 3);
            ctx.ProcessKey('.');
            Assert.Equal("。", ctx.TakeCommitted());
        }

        internal static int IndexOf(Composition comp, string candidate)
        {
            for (int i = 0; i < comp.CandidateCount; i++)
            {
                if (comp.Candidates[i] == candidate)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
