using Xunit;

namespace PathimeSharp.Tests
{
    /// <summary>Table engine: table selection by option, legends, tier-3 defaults.</summary>
    public class TableTests : IClassFixture<TableFixture>
    {
        private readonly TableFixture _fixture;

        public TableTests(TableFixture fixture)
        {
            _fixture = fixture;
        }

        // isolate: false — an isolated context pins tier-3 table-declared
        // values (e.g. the wildcard) as frozen overrides at creation, so the
        // table loaded afterward could not surface its own declarations.
        private Context NewCangjieContext()
        {
            var ctx = new Context(_fixture.Engine, isolate: false);
            ctx.SetOption(Option.TableFile, "cangjie5");
            return ctx;
        }

        [Fact]
        public void InstalledTablesAreEnumerable()
        {
            OptionInfo info = _fixture.Engine.GetOptionInfo(Option.TableFile);
            bool found = false;
            for (int i = 0; i < info.ValidValueCount; i++)
            {
                if (Pathime.GetOptionValueName(Option.TableFile, i) == "cangjie5")
                {
                    found = true;
                }
            }

            Assert.True(found, "cangjie5 should be among the installed tables");
        }

        [Fact]
        public void NoTableHandlesNothing()
        {
            using var ctx = new Context(_fixture.Engine);
            ctx.SetOption(Option.TableFile, "");
            Assert.Equal("", ctx.GetOption(Option.TableFile));
            Assert.False(ctx.ProcessKey('a'));
        }

        [Fact]
        public void BadTableIsBackendErrorAndKeepsPrevious()
        {
            using var ctx = NewCangjieContext();
            Assert.Throws<PathimeBackendException>(
                () => ctx.SetOption(Option.TableFile, "no-such-table"));
            Assert.Equal("cangjie5", ctx.GetOption(Option.TableFile));
        }

        [Fact]
        public void PreeditShowsKeyLegends()
        {
            using var ctx = NewCangjieContext();
            ctx.Type("a");
            Assert.Equal("日", ctx.Composition.Preedit); // cangjie legend for 'a'
            Assert.Contains("日", ctx.Composition.Candidates);
        }

        [Fact]
        public void ReturnCommitsTheLetters()
        {
            using var ctx = NewCangjieContext();
            ctx.Type("a");
            ctx.ProcessKey(Key.Return);
            Assert.Equal("a", ctx.TakeCommitted());
        }

        [Fact]
        public void SelectCommitsTheCharacter()
        {
            using var ctx = NewCangjieContext();
            ctx.Type("a");
            int index = PinyinTests.IndexOf(ctx.Composition, "日");
            ctx.SelectCandidate(index);
            Assert.Equal("日", ctx.TakeCommitted());
        }

        [Fact]
        public void TableDeclaresWildcard()
        {
            using var ctx = NewCangjieContext();
            // A compiled table is given a single wildcard where its alphabet
            // leaves room; the value is a tier-3 declaration, not a library
            // default (the library default is empty).
            OptionInfo info = ctx.GetOptionInfo(Option.TableSingleWildcard);
            Assert.Equal("", info.Default);
            Assert.NotEqual("", ctx.GetOption(Option.TableSingleWildcard));
        }
    }

    /// <summary>
    /// Bopomofo end to end. The Python reference has no keystroke test for
    /// bopomofo (its own TODO); this is the C# binding's minimal one.
    /// </summary>
    public class BopomofoTests : IClassFixture<BopomofoFixture>
    {
        private readonly BopomofoFixture _fixture;

        public BopomofoTests(BopomofoFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void StandardLayoutComposes()
        {
            using var ctx = new Context(_fixture.Engine);
            // Standard layout: s=ㄋ, u=ㄧ, 3=tone 3 → ㄋㄧˇ, candidates for 你.
            ctx.Type("su3");
            Assert.NotEqual("", ctx.Composition.Preedit);
            Assert.Contains("你", ctx.Composition.Candidates);
        }
    }
}
