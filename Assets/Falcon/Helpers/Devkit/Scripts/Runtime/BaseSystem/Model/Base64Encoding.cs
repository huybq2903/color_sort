/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */

using System;
using System.Text;

namespace Falcon.Helpers.Devkit
{
    public sealed class Base64Encoding : Encoding
    {
        private static readonly Encoding Inner = UTF8;

        public override int GetByteCount(char[] chars, int index, int count)
        {
            var s = new string(chars, index, count);
            string b64 = System.Convert.ToBase64String(Inner.GetBytes(s));
            return Inner.GetByteCount(b64);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            // Interpret incoming bytes as UTF8 string containing base64
            var b64 = Inner.GetString(bytes, index, count);
            var decoded = System.Convert.FromBase64String(b64);
            return Inner.GetCharCount(decoded);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
            char[] chars, int charIndex)
        {
            var b64 = Inner.GetString(bytes, byteIndex, byteCount);
            byte[] decoded = System.Convert.FromBase64String(b64);
            var resultChars = Inner.GetChars(decoded);
            Array.Copy(resultChars, 0, chars, charIndex, resultChars.Length);
            return resultChars.Length;
        }

        public override int GetMaxByteCount(int charCount)
        {
            // Worst case: each char -> 1 byte in UTF8, then base64 increases size by ~4/3,
            // plus some safety margin. Use a safe upper bound.
            var max = (long)Inner.GetMaxByteCount(charCount);
            max = (max * 4 + 2) / 3; // rough base64 growth
            if (max > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(charCount));
            return (int)max;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            // Worst-case: base64 decodes to fewer bytes; decoded bytes decode to chars (UTF8)
            long max = Inner.GetMaxCharCount(byteCount); // safe but loose
            return (int)max;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount,
            byte[] bytes, int byteIndex)
        {
            var s = new string(chars, charIndex, charCount);
            string b64 = System.Convert.ToBase64String(Inner.GetBytes(s));
            var b64Bytes = Inner.GetBytes(b64);
            Buffer.BlockCopy(b64Bytes, 0, bytes, byteIndex, b64Bytes.Length);
            return b64Bytes.Length;
        }

        // Convenience overrides for strings (not required but useful)
        public override byte[] GetBytes(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            string b64 = System.Convert.ToBase64String(Inner.GetBytes(s));
            return Inner.GetBytes(b64);
        }

        public override string GetString(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            var b64 = Inner.GetString(bytes);
            byte[] decoded = System.Convert.FromBase64String(b64);
            return Inner.GetString(decoded);
        }

        // Provide basic Encoder/Decoder that simply call back to the above conversions.
        public override Encoder GetEncoder()
        {
            return new Base64Encoder(this);
        }

        public override Decoder GetDecoder()
        {
            return new Base64Decoder(this);
        }

        private sealed class Base64Encoder : Encoder
        {
            private readonly Base64Encoding _enc;

            public Base64Encoder(Base64Encoding enc)
            {
                _enc = enc;
            }

            public override int GetByteCount(char[] chars, int index, int count, bool flush)
            {
                return _enc.GetByteCount(chars, index, count);
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount,
                byte[] bytes, int byteIndex, bool flush)
            {
                return _enc.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
            }
        }

        private sealed class Base64Decoder : Decoder
        {
            private readonly Base64Encoding _enc;

            public Base64Decoder(Base64Encoding enc)
            {
                _enc = enc;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return _enc.GetCharCount(bytes, index, count);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                char[] chars, int charIndex)
            {
                return _enc.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }
        }
    }
}