/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Falcon.Helpers.Devkit
{
    public class FAesSimpleFile : FFileWrapper
    {
        private byte[] _encryptKey;

        public FAesSimpleFile(IFFile baseFile)
        {
            BaseFile = baseFile;
        }

        protected override IFFile BaseFile { get; }

        public override Task SaveAsync(Stream data, int bufferSize = 4096, CancellationToken token = default)
        {
            return base.SaveAsync(Encrypt(data), bufferSize, token);
        }

        public override async Task AppendAsync(Stream data, int bufferSize = 4096, CancellationToken token = default)
        {
            using var memoryStream = new MemoryStream();
            await using (var concat = (await LoadAsync(token)).Concat(data))
            {
                await concat.CopyToAsync(memoryStream, token);
            }
            await base.AppendAsync(data, bufferSize, token);
        }

        public override async Task<Stream> LoadAsync(CancellationToken token = default)
        {
            return Decrypt(await base.LoadAsync(token));
        }

        private Stream Encrypt(Stream inputStream)
        {
            _encryptKey ??= AesService.GenerateAesKey();
            var encrypted = AesService.Encrypt(_encryptKey, inputStream).Concat();
            var keyLength = BitConverter.GetBytes(_encryptKey.Length);
            return new MemoryStream(keyLength).Concat(new MemoryStream(_encryptKey), encrypted);
        }

        private Stream Decrypt(Stream data)
        {
            if (data.Length == 0)
            {
                return new MemoryStream();
            }
            var keyLengthBytes = new byte[4];
            var read = data.Read(keyLengthBytes);
            if (read != keyLengthBytes.Length) throw new IOException("The data is corrupted.");
            var keyLength = BitConverter.ToInt32(keyLengthBytes);
            _encryptKey = new byte[keyLength];
            read = data.Read(_encryptKey);
            if (read != _encryptKey.Length) throw new IOException("The data is corrupted.");
            return AesService.Decrypt(_encryptKey, new AesEncrypted(data));
        }
    }

    public static class FAesSimpleFileExtensions
    {
        public static FAesSimpleFile AesSimpleEncrypt(this IFFile file)
        {
            return new FAesSimpleFile(file);
        }
    }
}