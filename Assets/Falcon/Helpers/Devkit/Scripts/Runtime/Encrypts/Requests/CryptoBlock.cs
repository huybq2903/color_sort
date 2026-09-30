/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class CryptoBlock
    {
        public Stream Stream { get;}

        public CryptoBlock(Stream stream)
        {
            Stream = stream;
        }

        public CryptoBlock(byte[] bytes) : this(new MemoryStream(bytes))
        {
        }

        public CryptoBlock(String shortStr, Encoding encoding = null) : this((encoding??Encoding.UTF8).GetBytes(shortStr)) {
        }

        public byte[] AsBytes()
        {
            using MemoryStream memoryStream = new MemoryStream();
            // Copy all data from the input stream to the MemoryStream.
            Stream.CopyTo(memoryStream);

            // Convert the MemoryStream's content to a byte array and return it.
            return memoryStream.ToArray();
        }

        public String AsStr(Encoding encoding = null) {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(AsBytes());
        }
    }
}