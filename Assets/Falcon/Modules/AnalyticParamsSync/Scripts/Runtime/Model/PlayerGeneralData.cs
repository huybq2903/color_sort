/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;
using Falcon.Modules.Core.AccountData;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    [FGameDataType("player_general_data")]
    [Serializable]
    public class PlayerGeneralData : FGameData<PlayerGeneralData>
    {
        public string bigDataAccountId;
        public string installVersion;
    }
}