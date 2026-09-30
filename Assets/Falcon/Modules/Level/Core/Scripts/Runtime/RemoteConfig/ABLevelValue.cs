/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11


using System;
using Falcon.Modules.Core.RemoteConfig;

namespace Falcon.Modules.Level.Core
{
    [Serializable]
    public class ABLevelValue : IFalconConfig
    {
        public string ab_levels_value = "";
        public static string GetABLevelsValue()
        {
            return FConfigController.Instance.Config<ABLevelValue>().ab_levels_value;
        }
    }
}