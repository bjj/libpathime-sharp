using System;

namespace PathimeSharp
{
    /// <summary>
    /// Conversion between UTF-16 code-unit indices (C# string indices) and
    /// Unicode scalar-value counts (libpathime's unit for every position).
    /// The two differ whenever text contains characters outside the Basic
    /// Multilingual Plane, which occupy two UTF-16 code units each.
    /// </summary>
    public static class UnicodeIndex
    {
        /// <summary>The number of Unicode scalar values in <paramref name="s"/>.</summary>
        public static int ScalarLength(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return ScalarsBefore(s, s.Length);
        }

        /// <summary>
        /// Convert a UTF-16 index into a scalar-value offset. Throws if the
        /// index is out of range or splits a surrogate pair.
        /// </summary>
        public static int Utf16ToScalars(string s, int utf16Index)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (utf16Index < 0 || utf16Index > s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(utf16Index));
            }
            if (utf16Index > 0 && utf16Index < s.Length
                && char.IsHighSurrogate(s[utf16Index - 1]) && char.IsLowSurrogate(s[utf16Index]))
            {
                throw new ArgumentException(
                    $"UTF-16 index {utf16Index} splits a surrogate pair.", nameof(utf16Index));
            }

            return ScalarsBefore(s, utf16Index);
        }

        /// <summary>
        /// Convert a scalar-value offset into a UTF-16 index. Throws if the
        /// offset exceeds the string's scalar length.
        /// </summary>
        public static int ScalarsToUtf16(string s, int scalarOffset)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (scalarOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scalarOffset));
            }

            int scalars = 0;
            int i = 0;
            while (i < s.Length)
            {
                if (scalars == scalarOffset)
                {
                    return i;
                }

                i += IsPair(s, i) ? 2 : 1;
                scalars++;
            }

            if (scalars == scalarOffset)
            {
                return i;
            }

            throw new ArgumentOutOfRangeException(nameof(scalarOffset),
                $"Scalar offset {scalarOffset} exceeds the string's scalar length {scalars}.");
        }

        private static int ScalarsBefore(string s, int utf16End)
        {
            int scalars = 0;
            for (int i = 0; i < utf16End; i += IsPair(s, i) ? 2 : 1)
            {
                scalars++;
            }

            return scalars;
        }

        // A lone surrogate counts as one scalar; it cannot survive UTF-8
        // encoding anyway, and the library rejects the resulting bytes.
        private static bool IsPair(string s, int i)
        {
            return char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]);
        }
    }
}
