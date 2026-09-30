/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;
using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    [FGameDataType("play_time_data")]
    [Serializable]
    public class PlaySessionData : FGameData<PlaySessionData>
    {
        public Dictionary<string, long> gameModeToTotalSec;

        public long? firstLogInMillis;
        public int? activeDays;
        public int? sessionId;
        public DateTime? lastLoginInDateTimeLocal;
    }
}