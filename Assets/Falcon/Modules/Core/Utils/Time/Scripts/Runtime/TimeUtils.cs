/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-06
     */


using UnityEngine;

namespace Falcon.Modules.Core.Utils.Time.Runtime
{
    using System;
    using System.Diagnostics;
    using Debug = UnityEngine.Debug;

    public class TimeUtils
    {
        public static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly int SecondsInHour = 3600;
        public static readonly int SecondsInDay = 86400;

        private static DateTime _utcNow;
        public static DateTime UTCNow
        {
            get
            {
                var utcNow = _utcNow;
                return utcNow.AddTicks(_stopwatch.ElapsedTicks);
            }
        }

        public static DateTime Now => UTCNow.ToLocalTime();

        private static Stopwatch _stopwatch;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            _stopwatch = new Stopwatch();
            _utcNow = AntiTimeCheatUtils.GetDateTimeUTC();
            RestartStopWatch();

            Debug.Log($"ATC: " + _utcNow);
            AntiTimeCheatUtils.onTimeServerUpdated = OnTimeServerUpdated;
            AntiTimeCheatUtils.ListenToUpdateTimeFromServer();
        }

        private static void OnTimeServerUpdated(DateTime d)
        {
            _utcNow = d;
            RestartStopWatch();

#if UNITY_EDITOR
            Debug.Log($"Time updated: " + _utcNow);
#endif
        }

        private static void RestartStopWatch()
        {
            _stopwatch.Reset();
            _stopwatch.Restart();
        }

        /// <summary>
        /// Get timestamp follows UTC Time
        /// </summary>
        public static long GetCurrentTimestampInSecondsUTC()
        {
            return GetTimestampInSecondsOf(UTCNow);
        }

        /// <summary>
        /// Get timestamp follows Local Time region
        /// </summary>
        public static long GetCurrentTimestampInSecondsLocal()
        {
            return GetTimestampInSecondsOf(Now);
        }

        /// <summary>
        /// Return timestamp from date
        /// </summary>
        public static long GetTimestampInSecondsOf(DateTime d)
        {
            if (d.Kind == DateTimeKind.Local)
            {
                // because Epoch is UTC kind
                d = d.ToUniversalTime();
            }

            var t = d - EpochTime;

            return (long)t.TotalSeconds;
        }

        /// <summary>
        /// Check whether timestamp (in-seconds) (in localTime or UTC) is in the same day as now
        /// </summary>
        public static bool IsSameDayAsNow(long timestampInSeconds, bool isLocalTime)
        {
            var now = isLocalTime ?
                GetCurrentTimestampInSecondsLocal() :
                GetCurrentTimestampInSecondsUTC();

            return IsSameDay(timestampInSeconds, now);
        }

        /// <summary>
        /// Check whether 2 timestamps (in-seconds) is in the same day
        /// </summary>
        public static bool IsSameDay(long timestampAInSeconds, long timestampBInSeconds)
        {
            var dateA = GetDateTimeFromTimestampUTC(timestampAInSeconds);
            var dateB = GetDateTimeFromTimestampUTC(timestampBInSeconds);

            return (dateA.Day == dateB.Day) && (dateA.Month == dateB.Month) && (dateA.Year == dateB.Year);
        }

        /// <summary>
        /// Return dateTime from timestamp (in-seconds) UTC
        /// </summary>
        public static DateTime GetDateTimeFromTimestampUTC(long timestampUTCInSeconds)
        {
            if (timestampUTCInSeconds > 9_999_999_999)
            {
                Debug.LogError("Timestamp is quite larger than 9_999_999_999.");
                timestampUTCInSeconds /= 10_000_000;
            }

            return EpochTime.Add(TimeSpan.FromSeconds(timestampUTCInSeconds));
        }
    }
}
