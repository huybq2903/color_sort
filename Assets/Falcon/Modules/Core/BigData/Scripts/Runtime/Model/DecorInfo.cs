/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */
// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public struct DecorInfo
    {
        public readonly bool valid;
        public readonly string error;

        public DecorInfo(bool valid, string error)
        {
            this.valid = valid;
            this.error = error;
        }
    }
}