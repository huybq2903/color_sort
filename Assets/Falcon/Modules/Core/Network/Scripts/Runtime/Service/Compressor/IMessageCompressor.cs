/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-08


using System;

namespace Falcon.Modules.Core.Network
{
    public interface IMessageCompressor
    {
        public CSCompressedMessage Compress(CSMessage message);
        public SCMessage Decompress(SCCompressedMessage compressedMessage);
    }
}