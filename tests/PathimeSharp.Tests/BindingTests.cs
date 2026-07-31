using System;
using Xunit;

namespace PathimeSharp.Tests
{
    /// <summary>
    /// Behaviour the binding itself adds: copies, callbacks, units, lifetimes.
    /// (The library's own behaviour is covered by libpathime's test suite.)
    /// </summary>
    public class BindingTests : IClassFixture<PinyinFixture>, IClassFixture<TableFixture>
    {
        private readonly PinyinFixture _pinyin;
        private readonly TableFixture _table;

        public BindingTests(PinyinFixture pinyin, TableFixture table)
        {
            _pinyin = pinyin;
            _table = table;
        }

        [Fact]
        public void KeyEventFromCharCarriesLayoutKeyAndModifiers()
        {
            var e = KeyEvent.FromChar('Q', KeyModifiers.Shift, layoutKey: 'q');
            Assert.Equal((uint)'Q', e.Keysym);
            Assert.Equal((uint)'q', e.LayoutKey);
            Assert.Equal(KeyModifiers.Shift, e.Modifiers);
        }

        [Fact]
        public void SnapshotsAreIndependent()
        {
            using var ctx = new Context(_pinyin.Engine);
            ctx.Type("nihao");
            Composition before = ctx.Composition;
            var candidates = before.Candidates;
            ctx.SelectCandidate(0);
            // The snapshot taken before the mutating call is untouched by it.
            Assert.Equal("ni hao", before.Preedit);
            Assert.Same(candidates, before.Candidates);
            Assert.NotSame(before, ctx.Composition);
        }

        [Fact]
        public void CommitCallbackReplacesBuffer()
        {
            var committed = new System.Collections.Generic.List<string>();
            using var ctx = new Context(_pinyin.Engine, onCommit: committed.Add);
            ctx.Type("nihao");
            ctx.SelectCandidate(0);
            Assert.Equal(new[] { "你好" }, committed);
            Assert.Equal("", ctx.Committed); // buffer unused when a callback is given
        }

        [Fact]
        public void CompositionChangedSeesCurrentSnapshot()
        {
            Composition? seen = null;
            using var ctx = new Context(_pinyin.Engine, onCompositionChanged: c => seen = c);
            ctx.Type("ni");
            Assert.Same(ctx.Composition, seen);
            Assert.NotEmpty(seen!.Candidates); // candidates readable inside the callback
        }

        [Fact]
        public void CallbackExceptionIsDeferredNotLost()
        {
            bool explode = true;
            using var ctx = new Context(_pinyin.Engine, onCompositionChanged: _ =>
            {
                if (explode)
                {
                    throw new InvalidOperationException("client bug");
                }
            });
            var thrown = Assert.Throws<InvalidOperationException>(() => ctx.ProcessKey('n'));
            Assert.Equal("client bug", thrown.Message);
            // The library completed its dispatch; the context is still usable.
            explode = false;
            ctx.ProcessKey('i');
            Assert.Equal("ni", ctx.Composition.Preedit);
        }

        [Fact]
        public void SurroundingTextPositionsAreUtf16Indices()
        {
            using var ctx = new Context(_pinyin.Engine);
            // 𝄞 is one scalar value but two UTF-16 units; positions here are
            // C# string indices, converted to scalars at the boundary.
            string text = "\U0001D11Ex1";
            ctx.SetSurroundingText(text, 4); // cursor after the digit
            ctx.ProcessKey('.');
            Assert.Equal(".", ctx.TakeCommitted()); // digit look-behind saw "1"
            Assert.Throws<ArgumentOutOfRangeException>(() => ctx.SetSurroundingText(text, 5));
            Assert.Throws<ArgumentException>(() => ctx.SetSurroundingText(text, 1)); // splits 𝄞
        }

        [Fact]
        public void EmbeddedNulIsRejected()
        {
            using var ctx = new Context(_pinyin.Engine);
            Assert.Throws<PathimeInvalidArgumentException>(() => ctx.SetSurroundingText("a\0b", 0));
        }

        [Fact]
        public void DisposeIsIdempotentAndOrdered()
        {
            var engine = new Engine(EngineId.Pinyin);
            var ctx = new Context(engine);
            ctx.Dispose();
            ctx.Dispose();
            engine.Dispose();
            engine.Dispose();
        }

        [Fact]
        public void EngineDisposeSweepsLiveContexts()
        {
            var engine = new Engine(EngineId.Pinyin);
            var ctx = new Context(engine);
            engine.Dispose(); // defensively disposes the context first
            Assert.Throws<ObjectDisposedException>(() => ctx.ProcessKey('a'));
            ctx.Dispose(); // still idempotent
        }

        [Fact]
        public void UseAfterDisposeThrows()
        {
            var engine = new Engine(EngineId.Pinyin);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.Requirements);
        }

        [Fact]
        public void StringOptionRoundTrips()
        {
            using var ctx = new Context(_table.Engine);
            ctx.SetOption(Option.TableFile, "cangjie5");
            object value = ctx.GetOption(Option.TableFile);
            Assert.IsType<string>(value);
            Assert.Equal("cangjie5", value);
            ctx.SetOption(Option.TableFile, "");
        }

        [Fact]
        public void ModifierChordIsDeclined()
        {
            using var ctx = new Context(_pinyin.Engine);
            Assert.False(ctx.ProcessKey('c', KeyModifiers.Control));
            Assert.Equal("", ctx.Composition.Preedit);
        }
    }
}
