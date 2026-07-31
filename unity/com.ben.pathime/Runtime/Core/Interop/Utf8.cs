using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PathimeSharp.Interop
{
    /// <summary>
    /// UTF-8 marshaling helpers. netstandard2.0 has no UTF-8 string marshaling
    /// and no pointer-based Encoding overloads, so everything goes through
    /// byte arrays and <see cref="Marshal"/>.
    /// </summary>
    internal static class Utf8
    {
        /// <summary>
        /// Encode a string for use as a length-delimited pathime_str_t slice.
        /// Embedded NULs are passed through; the library rejects them with
        /// PATHIME_ERROR_INVALID_ARGUMENT, which is the honest surface.
        /// </summary>
        public static byte[] GetBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        /// <summary>Decode a borrowed pathime_str_t into an owned string.</summary>
        public static string Decode(PathimeStr str)
        {
            int len = checked((int)str.Len.ToUInt64());
            if (str.Bytes == IntPtr.Zero || len == 0)
            {
                return string.Empty;
            }

            byte[] bytes = new byte[len];
            Marshal.Copy(str.Bytes, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>Decode a NUL-terminated const char* into an owned string.</summary>
        public static string DecodeNulTerminated(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
            {
                len++;
            }

            if (len == 0)
            {
                return string.Empty;
            }

            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Allocate an unmanaged NUL-terminated UTF-8 copy of <paramref name="s"/>.
        /// Free with <see cref="Free"/>. Returns <see cref="IntPtr.Zero"/> for null.
        /// Embedded NULs would silently truncate a C string, so they are rejected
        /// here rather than at the native boundary.
        /// </summary>
        public static IntPtr AllocNulTerminated(string? s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }

            if (s.IndexOf('\0') >= 0)
            {
                throw new ArgumentException("String must not contain an embedded NUL.", nameof(s));
            }

            byte[] bytes = Encoding.UTF8.GetBytes(s);
            IntPtr mem = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, mem, bytes.Length);
            Marshal.WriteByte(mem, bytes.Length, 0);
            return mem;
        }

        public static void Free(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
