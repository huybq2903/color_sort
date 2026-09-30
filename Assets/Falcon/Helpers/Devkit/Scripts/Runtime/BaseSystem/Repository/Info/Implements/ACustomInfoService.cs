/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public abstract class ACustomInfoService : IFCustomInfoRepository
    {
        public virtual Dictionary<string, object> GetInfo()
        {
            return FKeyService.Encode(this);
        }
    }
}