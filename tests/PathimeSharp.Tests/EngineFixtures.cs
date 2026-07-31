using System;
using Xunit;

namespace PathimeSharp.Tests
{
    /// <summary>
    /// Per-class engine fixture. The engine is created lazily on first access
    /// so that <see cref="Assert.Skip(string)"/> fires from inside a test when
    /// the engine's backend or data is missing.
    /// </summary>
    public abstract class EngineFixture : IDisposable
    {
        private readonly EngineId _id;
        private Engine? _engine;

        protected EngineFixture(EngineId id)
        {
            _id = id;
        }

        public Engine Engine
        {
            get
            {
                if (!Pathime.HasEngine(_id))
                {
                    Assert.Skip($"Engine {_id} is not available in this libpathime build/installation.");
                }

                return _engine ??= new Engine(_id);
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }

    public sealed class HangulFixture : EngineFixture
    {
        public HangulFixture() : base(EngineId.Hangul) { }
    }

    public sealed class AnthyFixture : EngineFixture
    {
        public AnthyFixture() : base(EngineId.Anthy) { }
    }

    public sealed class PinyinFixture : EngineFixture
    {
        public PinyinFixture() : base(EngineId.Pinyin) { }
    }

    public sealed class BopomofoFixture : EngineFixture
    {
        public BopomofoFixture() : base(EngineId.Bopomofo) { }
    }

    public sealed class TableFixture : EngineFixture
    {
        public TableFixture() : base(EngineId.Table) { }
    }
}
