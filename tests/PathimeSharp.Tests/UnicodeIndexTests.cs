using System;
using Xunit;

namespace PathimeSharp.Tests
{
    /// <summary>
    /// Pure unit tests of the UTF-16 ↔ scalar-value conversion the binding
    /// applies at every position boundary. 𝄞 (U+1D11E) is one scalar value
    /// but two UTF-16 code units.
    /// </summary>
    public class UnicodeIndexTests
    {
        private const string Sample = "\U0001D11Ex1"; // 𝄞 x 1

        [Fact]
        public void ScalarLengthCountsAstralAsOne()
        {
            Assert.Equal(4, Sample.Length);           // UTF-16 units
            Assert.Equal(3, UnicodeIndex.ScalarLength(Sample));
            Assert.Equal(0, UnicodeIndex.ScalarLength(""));
            Assert.Equal(3, UnicodeIndex.ScalarLength("abc"));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 2)]
        [InlineData(4, 3)]
        public void Utf16ToScalarsRoundTrips(int utf16, int scalars)
        {
            Assert.Equal(scalars, UnicodeIndex.Utf16ToScalars(Sample, utf16));
            Assert.Equal(utf16, UnicodeIndex.ScalarsToUtf16(Sample, scalars));
        }

        [Fact]
        public void SplittingASurrogatePairThrows()
        {
            Assert.Throws<ArgumentException>(() => UnicodeIndex.Utf16ToScalars(Sample, 1));
        }

        [Fact]
        public void OutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeIndex.Utf16ToScalars(Sample, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeIndex.Utf16ToScalars(Sample, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeIndex.ScalarsToUtf16(Sample, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeIndex.ScalarsToUtf16(Sample, -1));
        }

        [Fact]
        public void LoneSurrogateCountsAsOneScalar()
        {
            string lone = "a\uD834b";
            Assert.Equal(3, UnicodeIndex.ScalarLength(lone));
            Assert.Equal(2, UnicodeIndex.Utf16ToScalars(lone, 2));
        }
    }
}
