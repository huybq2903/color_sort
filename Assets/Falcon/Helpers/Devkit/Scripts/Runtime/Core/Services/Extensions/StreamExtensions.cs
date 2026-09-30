/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System.IO;

namespace Falcon.Helpers.Devkit
{
    public static class StreamExtensions
    {
        public static Stream Concat(this Stream one, params Stream[] others)
        {
            return new ConcatenatedStream(new[] { one }.Concat(others));
        }
    }
}