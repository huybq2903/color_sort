/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Diagnostics;
using System.Globalization;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class MyTime
    {
        public static long CurrentTimeMillis => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public static long CurrentTimeSec => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static long CurrentTimeNano
        {
            get
            {
                var nano = 10000L * Stopwatch.GetTimestamp();
                nano /= TimeSpan.TicksPerMillisecond;
                nano *= 100L;
                return nano;
            }
        }

        public static string DateToString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static DateTime StringToDate(string dateStr)
        {
            return DateTime.Parse(dateStr, CultureInfo.InvariantCulture);
        }
        
        public static long DateSinceEpoch(long millisSec)
        {
            return millisSec / (24 * 60 * 60 * 1000);
        }
    }
}