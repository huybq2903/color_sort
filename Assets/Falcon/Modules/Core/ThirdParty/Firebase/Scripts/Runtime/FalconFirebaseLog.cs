/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

#if FIREBASE_ENABLE
using Firebase.Analytics;
#endif

namespace Falcon.Modules.Core.ThirdParty.Firebase.Scripts.Runtime
{
    public class FalconFirebaseLog
    {
        private const string AD_IMPRESSION = "ad_impression";

        public const string AD_INTER_LOAD_FAILED = "ad_inter_load_fail";
        public const string AD_INTER_LOAD_SUCCESS = "ad_inter_load_success";
        public const string AD_INTER_SHOW = "ad_inter_show";
        public const string AD_INTER_CLICKED = "ad_inter_click";

        public const string ADS_REWARD_LOAD = "ads_reward_load";
        public const string ADS_REWARD_CLICK = "ads_reward_click";
        public const string ADS_REWARD_SHOW_SUCCESS = "ads_reward_show_success";
        public const string ADS_REWARD_SHOW_FAIL = "ads_reward_show_fail";
        public const string ADS_REWARD_COMPLETE = "ads_reward_complete";

        public const string LEVEL_START = "level_start";
        public const string LEVEL_COMPLETE = "level_complete";
        public const string LEVEL_FAIL = "level_fail";
        public const string EARN_VIRTUAL_CURRENCY = "earn_virtual_currency";
        public const string SPEND_VIRTUAL_CURRENCY = "spend_virtual_currency";

        public static void LogImpression(string platform, string network, string unitName, string format, double value)
        {
#if !UNITY_EDITOR && FIREBASE_ENABLE
            Parameter[] parameters =
            {
                new Parameter("ad_platform", platform),
                new Parameter("ad_source", network),
                new Parameter("ad_unit_name", unitName),
                new Parameter("ad_format", format),
                new Parameter("currency", "USD"),
                new Parameter("value", value)
            };
            if (FirebaseInit.isInitialize)
                FirebaseAnalytics.LogEvent(AD_IMPRESSION, parameters);
#endif
        }

        public static void LogFsn(string adFormat, string adSource, string adUnitId, long valueMicros,
            string currencyCode)
        {
#if !UNITY_EDITOR && FIREBASE_ENABLE
            double revenue = valueMicros / 1_000_000.0;
            Parameter[] parameters =
            {
                new Parameter("ad_platform", "AdMob"),
                new Parameter("ad_source", adSource ?? ""),
                new Parameter("ad_unit_id", adUnitId ?? ""),
                new Parameter("ad_format", adFormat),
                new Parameter("currency", currencyCode ?? ""),
                new Parameter("value", revenue)
            };
            FirebaseAnalytics.LogEvent(AD_IMPRESSION, parameters);
#endif
        }

        public static void LogBamBoo(string eventName, double revenue)
        {
#if !UNITY_EDITOR && FIREBASE_ENABLE
            Parameter[] parameters =
            {
                new(FirebaseAnalytics.ParameterValue, revenue),
                new(FirebaseAnalytics.ParameterCurrency, "USD"),
            };
            if (FirebaseInit.isInitialize)
                FirebaseAnalytics.LogEvent(eventName, parameters);
#endif
        }

        public static void LogTaiChi(string eventName, double revenue)
        {
#if !UNITY_EDITOR && FIREBASE_ENABLE
            Parameter[] parameters =
            {
                new(FirebaseAnalytics.ParameterValue, revenue),
                new(FirebaseAnalytics.ParameterCurrency, "USD"),
            };
            if (FirebaseInit.isInitialize)
                FirebaseAnalytics.LogEvent(eventName, parameters);
#endif
        }

        public static void Log(string eventName)
        {
#if !UNITY_EDITOR && FIREBASE_ENABLE
            if (FirebaseInit.isInitialize)
                FirebaseAnalytics.LogEvent(eventName);
#endif
        }

        public static void Log(string name, string parameters, string value)
        {
#if !UNITY_EDITOR && FIREBASE_ENABLE
            if (FirebaseInit.isInitialize)
                FirebaseAnalytics.LogEvent(name, parameters, value);
#endif
        }

        public static void Log(string name, string parameters, long value = 0)
        {
#if !UNITY_EDITOR && FIREBASE_ENABLE
            if (FirebaseInit.isInitialize)
                FirebaseAnalytics.LogEvent(name, parameters, value);
#endif
        }

        public static void Log(string name, string virtualCurrencyName, double value, string source)
        {
#if !UNITY_EDITOR && FIREBASE_ENABLE
            if (!FirebaseInit.isInitialize) return;
            if (name.Contains("earn"))
            {
                //earn_virtual_currency
                Parameter[] param =
                {
                    new Parameter("virtual_currency_name", virtualCurrencyName),
                    new Parameter("value", value),
                    new Parameter("source", source)
                };
                FirebaseAnalytics.LogEvent(name, param);
            }
            else
            {
                //spend_virtual_currency
                Parameter[] param =
                {
                    new Parameter("virtual_currency_name", virtualCurrencyName),
                    new Parameter("value", value),
                    new Parameter("item_name", source)
                };
                FirebaseAnalytics.LogEvent(name, param);
            }
#endif
        }
    }
}