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
    public class CustomInfoService : IMySingleton
    {
        private readonly IFCustomInfoRepository[] _infoRepositories;

        public CustomInfoService(IFCustomInfoRepository[] infoRepositories)
        {
            _infoRepositories = infoRepositories;
        }

        public Dictionary<string, object> GetInfo()
        {
            var result = new Dictionary<string, object>();
            foreach (var repository in _infoRepositories)
            {
                result.AddAll(repository.GetInfo());
            }
            return result;
        }
    }
}