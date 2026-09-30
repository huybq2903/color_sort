/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class AesParser : ICryptographer
    {
        private readonly byte[] _key;

        public AesParser(byte[] key)
        {
            _key = key;
        }

        public AesParser(string key)
        {
            _key = Convert.FromBase64String(key);
        }

        public CryptoBlock Encrypt(CryptoBlock block)
        {
            return new CryptoBlock(AesService.Encrypt(_key, block.Stream).Concat());
        }

        public CryptoBlocks Encrypt(CryptoBlocks blocks)
        {
            var result = new CryptoBlocks();
            result.AddRange(blocks.Select(Encrypt));
            return result;
        }

        public CryptoBlock Decrypt(CryptoBlock block)
        {
            return new CryptoBlock(AesService.Decrypt(_key, new AesEncrypted(block.Stream)));
        }

        public CryptoBlocks Decrypt(CryptoBlocks blocks)
        {
            var result = new CryptoBlocks();
            result.AddRange(blocks.Select(Decrypt));

            return result;
        }
    }
}