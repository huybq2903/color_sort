/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-07
 */
 
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Falcon.Modules.Core.Utils.Time.Tests
{
    using Falcon.Modules.Core.Network;
    using TimeUtils = Falcon.Modules.Core.Utils.Time.Runtime.TimeUtils;

    public class TimeUtilsPlayModeTests
    {
        [UnityTest]
        public IEnumerator TimeUtils_Initialization_SetsUtcNow()
        {
            // The initialization happens automatically via RuntimeInitializeOnLoadMethod.
            // We'll wait a frame to ensure all initialization logic has a chance to run.
            yield return null;

            // In the Editor, AntiTimeCheatUtils.GetDateTimeUTC() will return a value based on DateTime.UtcNow.
            // We expect TimeUtils.UTCNow to be very close to the current system UTC time.
            var now = DateTime.UtcNow;
            var timeUtilsNow = TimeUtils.UTCNow;
            
            // Check that the time is recent (within a 5-second tolerance for test execution)
            Assert.LessOrEqual((now - timeUtilsNow).TotalSeconds, 5, "TimeUtils.UTCNow should be close to DateTime.UtcNow after initialization.");
        }
        
        [UnityTest]
        public IEnumerator TimeUtils_Initialization_SetsUtcNowFromServer()
        {
            var serverIp      = NetworkSettings.GetServerIp();
            var serverPort    = NetworkSettings.GetServerPort();
            var serverContext = NetworkSettings.GetServerContext();
            FNetManager.Instance.Start($"http://{serverIp}:{serverPort}/{serverContext}/");
            
            // The initialization happens automatically via RuntimeInitializeOnLoadMethod.
            // We'll wait a frame to ensure all initialization logic has a chance to run.
            yield return new WaitForSeconds(5f);

            // In the Editor, AntiTimeCheatUtils.GetDateTimeUTC() will return a value based on DateTime.UtcNow.
            // We expect TimeUtils.UTCNow to be very close to the current system UTC time.
            var now          = DateTime.UtcNow;
            var timeUtilsNow = TimeUtils.UTCNow;
            
            // Check that the time is recent (within a 5-second tolerance for test execution)
            Assert.LessOrEqual((now - timeUtilsNow).TotalSeconds, 5, "TimeUtils.UTCNow should be close to DateTime.UtcNow after get from server.");
        }
        
        [UnityTest]
        public IEnumerator TimeUtils_UTCNow_Update()
        {
            Debug.Log(TimeUtils.UTCNow);
            
            yield return new WaitForSeconds(1f);
            Debug.Log(TimeUtils.UTCNow);
            
            yield return new WaitForSeconds(1f);
            Debug.Log(TimeUtils.UTCNow);
            
            yield return new WaitForSeconds(1f);
            Debug.Log(TimeUtils.UTCNow);
        }

        [Test]
        public void GetTimestampInSecondsOf_ReturnsCorrectTimestamp()
        {
            // Test with the epoch time
            long epochTimestamp = TimeUtils.GetTimestampInSecondsOf(TimeUtils.EpochTime);
            Assert.AreEqual(0, epochTimestamp, "Timestamp of epoch should be 0.");

            // Test with a specific date
            var testDate = new DateTime(2023, 10, 27, 10, 30, 0, DateTimeKind.Utc);
            long expectedTimestamp = (long)(testDate - TimeUtils.EpochTime).TotalSeconds;
            long actualTimestamp = TimeUtils.GetTimestampInSecondsOf(testDate);
            Assert.AreEqual(expectedTimestamp, actualTimestamp, "Timestamp for a specific UTC date is incorrect.");
            
            // Test with a local time date (should be converted correctly based on offset)
            var testLocalDate = new DateTime(2023, 10, 27, 10, 30, 0, DateTimeKind.Local);
            long expectedLocalTimestamp = (long)(testLocalDate.ToUniversalTime() - TimeUtils.EpochTime).TotalSeconds;
            long actualLocalTimestamp = TimeUtils.GetTimestampInSecondsOf(testLocalDate);
            Assert.AreEqual(expectedLocalTimestamp, actualLocalTimestamp, "Timestamp for a specific Local date is incorrect.");
        }

        [Test]
        public void GetDateTimeFromTimestamp_ReturnsCorrectDateTime()
        {
            // Test with timestamp 0
            DateTime epochDateTime = TimeUtils.GetDateTimeFromTimestampUTC(0);
            Assert.AreEqual(TimeUtils.EpochTime, epochDateTime, "DateTime for timestamp 0 should be the epoch time.");

            // Test with a specific timestamp
            long testTimestamp = 1698399000; // Corresponds to 2023-10-27 9:30:00 UTC
            var expectedDate = new DateTime(2023, 10, 27, 9, 30, 0, DateTimeKind.Utc);
            DateTime actualDate = TimeUtils.GetDateTimeFromTimestampUTC(testTimestamp);
            Assert.AreEqual(expectedDate, actualDate, "Did not convert timestamp to the correct DateTime.");
        }

        [Test]
        public void IsSameDay_CorrectlyComparesTimestamps()
        {
            // Two timestamps on the same day
            long timestamp1_day1 = 1698364800; // 2023-10-27 00:00:00 UTC
            long timestamp2_day1 = 1698451199; // 2023-10-27 23:59:59 UTC
            Assert.IsTrue(TimeUtils.IsSameDay(timestamp1_day1, timestamp2_day1), "Timestamps on the same day should be considered the same day.");

            // Two timestamps on different days
            long timestamp_day2 = 1698451200; // 2023-10-28 00:00:00 UTC
            Assert.IsFalse(TimeUtils.IsSameDay(timestamp1_day1, timestamp_day2), "Timestamps on different days should not be considered the same day.");

            // Timestamps crossing a month boundary
            long timestamp_month_end = 1698796799; // 2023-10-31 23:59:59 UTC
            long timestamp_month_start = 1698796800; // 2023-11-01 00:00:00 UTC
            Assert.IsFalse(TimeUtils.IsSameDay(timestamp_month_end, timestamp_month_start), "Timestamps across a month boundary should not be the same day.");
            
            // Timestamps crossing a year boundary
            long timestamp_year_end = 1704067199; // 2023-12-31 23:59:59 UTC
            long timestamp_year_start = 1704067200; // 2024-01-01 00:00:00 UTC
            Assert.IsFalse(TimeUtils.IsSameDay(timestamp_year_end, timestamp_year_start), "Timestamps across a year boundary should not be the same day.");
        }
    }
} 