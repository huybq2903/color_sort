/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.IO;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class AesEncrypted
    {
        public AesEncrypted(Stream data, byte[] iv)
        {
            Data = data;
            Iv = iv;
        }

        public AesEncrypted(Stream concat)
        {
            using var aesAlg = System.Security.Cryptography.Aes.Create();
            var blockSize = aesAlg.BlockSize/8;
            Iv = new byte[blockSize];
            var read = concat.Read(Iv, 0, blockSize);
            if (read != blockSize)
            {
                throw new IOException("Concat data corrupted");
            }
            Data = concat;
        }

        public byte[] Iv { get; }
        public Stream Data { get; }

        public Stream Concat()
        {

            return new MemoryStream(Iv).Concat(Data);
        }
    }
}