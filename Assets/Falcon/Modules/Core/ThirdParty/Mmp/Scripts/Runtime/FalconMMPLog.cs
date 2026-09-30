/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System.Collections.Generic;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
    public class FalconMmpLog
    {
        public const string MMP_INTERS_SHOW = "af_inters_show";
        public const string MMP_INTERS_DISPLAYED = "af_inters_displayed";
        public const string MMP_REWARDED_SHOW = "af_rewarded_show";
        public const string MMP_REWARDED_DISPLAYED = "af_rewarded_displayed";

        public const string MMP_LEVEL_ACHIEVED = "af_level_achieved";
        public const string MMP_COMPLETE_REGISTRATION = "af_complete_registration";
        public const string MMP_TUTORIAL_COMPLETION = "af_tutorial_completion";
        public const string MMP_ACHIEVEMENT_UNLOCKED = "af_achievement_unlocked";

        public const string MMP_REGISTRATION_METHOD = "af_registration_method";
        public const string MMP_SUCCESS = "af_success";
        public const string MMP_TUTORIAL_ID = "af_tutorial_id";
        public const string MMP_CONTENT_ID = "content_id";
        public const string MMP_CUSTOMER_USER_ID = "af_customer_user_id";
        public const string MMP_LEVEL = "af_level";
        public const string MMP_SCORE = "af_score";
        public const string EVENT_BUS_LEVEL_COMPLETE = "event_bus_level_complete";


        public static void LogIAP(string currencyCode, string contentId, string purchasePrice)
        {
            //appsflyer tự động log với purchase connector
            //chỉ làm thủ công với adjust
#if ADJUST_ENABLE
            FalconAdjustService.Instance.LogIAP(currencyCode, contentId, purchasePrice);
#endif
        }

        public static void LogFsn(string adFormat, string adSource, string adUnitId, long valueMicros,
            string currencyCode, string placement = "")
        {
#if APPSFLYER_ENABLE
            FalconAppsflyerService.Instance.LogRevenueFsn(adFormat, adSource, adUnitId, valueMicros, currencyCode,
                placement);
#endif
        }

        public static void LogRevenue(string network, string format, double value,
            string instance = null, string placement = null)
        {
#if APPSFLYER_ENABLE
            FalconAppsflyerService.Instance.LogRevenue(network, format, value, instance);
#endif
#if ADJUST_ENABLE
            FalconAdjustService.Instance.LogRevenue(network, value, instance, placement);
#endif
        }

        public static void LogEvent(string eventName, Dictionary<string, string> dictionary = null)
        {
#if APPSFLYER_ENABLE
            FalconAppsflyerService.Instance.LogEvent(eventName, dictionary);
#endif
#if ADJUST_ENABLE
            FalconAdjustService.Instance.LogEvent(eventName);
#endif
        }
    }
}