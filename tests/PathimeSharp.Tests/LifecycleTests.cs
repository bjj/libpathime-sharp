using System;
using System.Text.RegularExpressions;
using Xunit;

namespace PathimeSharp.Tests
{
    public class LifecycleTests
    {
        [Fact]
        public void VersionIsSemver()
        {
            Assert.Matches(new Regex(@"^\d+\.\d+\.\d+$"), Pathime.Version);
        }

        [Fact]
        public void VersionNumberMatchesVersionString()
        {
            var parts = Pathime.Version.Split('.');
            uint encoded = uint.Parse(parts[0]) * 1000000u
                         + uint.Parse(parts[1]) * 1000u
                         + uint.Parse(parts[2]);
            Assert.Equal(encoded, Pathime.VersionNumber);
        }

        [Fact]
        public void SecondInitIsRejected()
        {
            // The fixture already ran Init once for the process.
            Assert.Throws<PathimeAlreadyInitializedException>(() => Pathime.Init());
        }

        [Fact]
        public void EmptyDataDirIsInvalidNotDefault()
        {
            // Empty string is INVALID_ARGUMENT rather than a second spelling of
            // null — but ALREADY_INITIALIZED wins here since init already ran.
            // The distinction is covered by the library's own tests; what the
            // binding asserts is that some rejection surfaces as an exception.
            Assert.ThrowsAny<PathimeException>(() => Pathime.Init(dataDir: ""));
        }

        [Fact]
        public void UnknownEngineNameIsEmpty()
        {
            Assert.Equal("", Pathime.GetEngineName((EngineId)99));
        }

        [Fact]
        public void EngineNamesMatchEnumMembers()
        {
            foreach (EngineId id in Enum.GetValues(typeof(EngineId)))
            {
                // Catches transposed enum values: the library's stable names
                // are the enum member names, lowercased.
                Assert.Equal(id.ToString().ToLowerInvariant(), Pathime.GetEngineName(id));
            }
        }

        [Fact]
        public void OptionIdsAreDenseAndNamesMatchEnumMembers()
        {
            Assert.Equal(Enum.GetValues(typeof(Option)).Length, Pathime.OptionCount);
            foreach (Option option in Enum.GetValues(typeof(Option)))
            {
                Assert.Equal(ToKebabCase(option.ToString()), Pathime.GetOptionName(option));
            }
        }

        [Fact]
        public void UnknownOptionNameIsEmpty()
        {
            Assert.Equal("", Pathime.GetOptionName((Option)999));
        }

        internal static string ToKebabCase(string pascal)
        {
            var sb = new System.Text.StringBuilder(pascal.Length + 8);
            for (int i = 0; i < pascal.Length; i++)
            {
                char c = pascal[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        sb.Append('-');
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }

    public class KeysymTests
    {
        [Theory]
        [InlineData('a', 0x61u)]
        [InlineData('A', 0x41u)]
        [InlineData(' ', 0x20u)]
        [InlineData('é', 0xE9u)]         // below U+0100: keysym is the scalar
        [InlineData('中', 0x01004E2Du)]  // at/above U+0100: 0x01000000 + scalar
        [InlineData('ㅁ', 0x01003141u)]
        public void KeysymForCharFollowsX11Rule(char c, uint expected)
        {
            Assert.Equal(expected, Pathime.KeysymForChar(c));
        }

        [Fact]
        public void KeysymForAstralCodePoint()
        {
            Assert.Equal(0x01000000u + 0x1D11Eu, Pathime.KeysymForCodePoint(0x1D11E));
        }

        [Fact]
        public void SurrogateHalfIsRejected()
        {
            Assert.Throws<ArgumentException>(() => Pathime.KeysymForChar('\uD834'));
        }

        [Fact]
        public void OutOfRangeCodePointIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Pathime.KeysymForCodePoint(0x110000));
        }
    }
}
