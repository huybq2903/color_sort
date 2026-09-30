/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-10-28
*/

using System;
using Falcon.Modules.Core.RemoteConfig;

namespace Falcon.Modules.InAppUpdate.Scripts.Runtime
{
    [Serializable]
    public class InAppUpdateConfig : IFalconConfig
    {
        //Trả về chuỗi có dạng "1.0.0;1.1.1". Với 1.0.0 là version Force Update, 1.1.1 là version Optional Update
        public string force_and_optional_versions = string.Empty;
    }
}