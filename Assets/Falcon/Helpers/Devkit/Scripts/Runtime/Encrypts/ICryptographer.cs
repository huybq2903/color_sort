/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ICryptographer
    {
        CryptoBlock Encrypt(CryptoBlock block);
        CryptoBlocks Encrypt(CryptoBlocks blocks);
        CryptoBlock Decrypt(CryptoBlock block);
        CryptoBlocks Decrypt(CryptoBlocks blocks);
    }
}