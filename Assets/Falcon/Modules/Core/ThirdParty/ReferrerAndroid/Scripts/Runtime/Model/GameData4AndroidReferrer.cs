/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-17
 */

using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
    [FGameDataType("android_referrer_data")]
    public class GameData4AndroidReferrer : FGameData<GameData4AndroidReferrer>
    {
        public string installReferrer;
        public long referrerClickTimestampSeconds;
        public long installBeginTimestampSeconds;
        public long referrerClickTimestampServerSeconds;
        public long installBeginTimestampServerSeconds;
        public string installVersion;
        public bool googlePlayInstant;
    }
}