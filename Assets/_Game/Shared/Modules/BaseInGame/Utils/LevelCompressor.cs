using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Falcon.Shared.BaseInGame
{
    /// <summary>Nén/giải nén level bằng GZip + Base64. Runtime nên nằm ở đây, không ở asmdef editor-only.</summary>
    public static class LevelCompressor
    {
        public static string Compress(this string inputStr)
        {
            if (string.IsNullOrEmpty(inputStr)) return "";
            try
            {
                var inputBytes = Encoding.UTF8.GetBytes(inputStr);
                using var outputStream = new MemoryStream();
                using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                    gZipStream.Write(inputBytes, 0, inputBytes.Length);
                return Convert.ToBase64String(outputStream.ToArray());
            }
            catch
            {
                return "";
            }
        }

        public static string Decompress(this string inputStr)
        {
            if (string.IsNullOrEmpty(inputStr)) return "";
            if (!IsAlreadyCompressed(inputStr)) return inputStr;
            return TryDecompressString(inputStr, out var decompressed) ? decompressed : "";
        }

        public static bool IsAlreadyCompressed(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            // JSON thô thì khỏi thử giải nén.
            foreach (var c in input)
            {
                if (char.IsWhiteSpace(c)) continue;
                if (c is '{' or '[') return false;
                break;
            }

            return TryDecompressString(input, out _);
        }

        private static bool TryDecompressString(string input, out string decompressed)
        {
            decompressed = "";
            try
            {
                using var inputStream = new MemoryStream(Convert.FromBase64String(input));
                using var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                using var streamReader = new StreamReader(gZipStream);
                decompressed = streamReader.ReadToEnd();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}