/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class NoCrypto : ICryptographer
    {
        public static readonly NoCrypto Instance = new();

        public CryptoBlock Encrypt(CryptoBlock block)
        {
            return block;
        }

        public CryptoBlocks Encrypt(CryptoBlocks blocks)
        {
            return blocks;
        }

        public CryptoBlock Decrypt(CryptoBlock block)
        {
            return block;
        }

        public CryptoBlocks Decrypt(CryptoBlocks blocks)
        {
            return blocks;
        }
    }
}