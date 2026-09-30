/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class CryptoBlocks : List<CryptoBlock>
    {
        public CryptoBlocks()
        {
        }

        public CryptoBlocks([NotNull] IEnumerable<CryptoBlock> collection) : base(collection)
        {
        }

        public CryptoBlocks(string longStr, Encoding encoding, int chunkSize)
        {
            var originalBytes = encoding.GetBytes(longStr);
            for (var i = 0; i < originalBytes.Length; i += chunkSize)
            {
                int length = Math.Min(originalBytes.Length - i , chunkSize);
                byte[] result = new byte[length];
                Array.Copy(originalBytes, i, result, 0 , result.Length);
                Add(new CryptoBlock(result));
            }
        }

        public CryptoBlocks(string longStr, int chunkSize) : this(longStr, Encoding.UTF8, chunkSize)
        {
        }

        public CryptoBlocks(IEnumerable<string> shortStrings) : this(shortStrings.Select(str => new CryptoBlock(str)))
        {
        }

        public CryptoBlocks(List<string> shortStrings, Encoding encoding) : this(
            shortStrings.Select(str => new CryptoBlock(str, encoding)))
        {
        }

        public string AsStr()
        {
            return AsStr(Encoding.UTF8);
        }

        public string AsStr(Encoding encoding)
        {
            return encoding.GetString(AsBytes());
        }

        public List<string> AsStringList()
        {
            return this.Select(str => str.AsStr()).ToList();
        }

        public List<string> AsStringList(Encoding encoding)
        {
            return this.Select(str => str.AsStr(encoding)).ToList();
        }

        public List<byte[]> AsBytesList()
        {
            return this.Select(str => str.AsBytes()).ToList();
        }

        public byte[] AsBytes()
        {
            using MemoryStream memoryStream = new MemoryStream();
            // Copy all data from the input stream to the MemoryStream.
            AsStream().CopyTo(memoryStream);

            // Convert the MemoryStream's content to a byte array and return it.
            return memoryStream.ToArray();
        }

        public Stream AsStream()
        {
            return new ConcatenatedStream(this.Select(b => b.Stream).ToArray());
        }
    }
}