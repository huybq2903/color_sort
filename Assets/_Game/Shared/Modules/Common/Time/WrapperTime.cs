/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-29
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Falcon.Shared.Common.Time
{
    public static class WrapperTime
    {
        public static long CurrentSecond { get; private set; }
        public static int CurrentDay => (int)TimeSpan.FromSeconds(CurrentSecond).TotalDays;
        public static long SecondNextDay => (long)(CurrentDay + 1) * SECOND_ONE_DAY;
        public static long SecondNextWeek => (long)(CurrentDay + 7) * SECOND_ONE_DAY;
        public const int SECOND_ONE_DAY = 86400;
        public static DayOfWeek CurrentDayOfWeek =>
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(CurrentSecond).DayOfWeek;

        public static Action onFocus = () => OverrideTime(-1);

        private static readonly Dictionary<string, Schedule> _schedules = new();
        private static readonly List<string> _keysToRemoveSchedules = new();
        private static readonly List<Schedule> _tickBuffer = new();
        public static event Action OnOverrideTime;

        // Mốc để suy ra CurrentSecond, tránh đếm tay bị trôi
        private static long _anchorSecond;
        private static double _anchorRealtime;

        public static void Initialize()
        {
            Application.focusChanged += OnFocusChanged;
            OverrideTime(-1);
            Tick();
        }

        private static void OnFocusChanged(bool hasFocus)
        {
            if (!Application.isPlaying)
            {
                Application.focusChanged -= OnFocusChanged;
                return;
            }
            if (hasFocus) onFocus?.Invoke();
        }

        public static void OverrideTime(long timeMs)
        {
            if (timeMs < 0)
            {
                timeMs = (long)UnbiasedTime.Instance.UtcNow().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            }
            CurrentSecond = timeMs / 1000;
            _anchorSecond = CurrentSecond;
            _anchorRealtime = UnityEngine.Time.realtimeSinceStartupAsDouble;
            CountTimeRemainNextDay();
            OnOverrideTime?.Invoke();
        }

        private static async void Tick()
        {
            while (Application.isPlaying)
            {
                // ignoreTimeScale, nếu không sẽ đứng khi timeScale = 0
                await UniTask.WaitForSeconds(1f, ignoreTimeScale: true);
                CurrentSecond = _anchorSecond + (long)(UnityEngine.Time.realtimeSinceStartupAsDouble - _anchorRealtime);
                InvokeSchedules();
            }
        }

        private static void InvokeSchedules()
        {
            // Duyệt trên bản copy vì callback có thể AddTick/RemoveTick
            _tickBuffer.Clear();
            _tickBuffer.AddRange(_schedules.Values);

            foreach (var schedule in _tickBuffer)
            {
                try
                {
                    if (schedule.tickEnd <= CurrentSecond || schedule.tickEnd < 0)
                    {
                        _keysToRemoveSchedules.Add(schedule.key);
                        schedule.actionEverySecond?.Invoke(0);
                        schedule.actionEnd?.Invoke();
                    }
                    else
                    {
                        schedule.actionEverySecond?.Invoke(schedule.tickEnd - CurrentSecond);
                    }
                }
                catch (Exception e)
                {
                    // Một schedule lỗi không được làm chết các schedule còn lại
                    Debug.LogException(e);
                }
            }

            if (_keysToRemoveSchedules.Count == 0) return;
            foreach (var key in _keysToRemoveSchedules)
            {
                _schedules.Remove(key);
            }
            _keysToRemoveSchedules.Clear();
        }

        public static void AddTick(string key, long tickEnd, Action<long> action, Action actionEnd)
        {
            if (tickEnd <= CurrentSecond)
            {
                actionEnd?.Invoke();
                return;
            }

            if (_keysToRemoveSchedules.Contains(key))
            {
                _keysToRemoveSchedules.Remove(key);
            }

            if (_schedules.ContainsKey(key))
            {
                _schedules[key].tickEnd = tickEnd;
                _schedules[key].actionEverySecond = _schedules[key].actionEverySecond.Add(action);
                _schedules[key].actionEnd = actionEnd;
                action?.Invoke(tickEnd - CurrentSecond);
                return;
            }

            _schedules.Add(key, new Schedule()
            {
                key = key,
                tickEnd = tickEnd,
                actionEverySecond = action,
                actionEnd = actionEnd,
            });
            action?.Invoke(Math.Max(0, tickEnd - CurrentSecond));
        }

        public static bool AddAction(string key, Action<long> action)
        {
            if (!_schedules.TryGetValue(key, out var schedule)) return false;
            schedule.actionEverySecond = schedule.actionEverySecond.Add(action);
            action?.Invoke(Math.Max(0, schedule.tickEnd - CurrentSecond));
            return true;
        }

        public static void OverrideTickEnd(string key, long tickEnd)
        {
            if (!_schedules.TryGetValue(key, out var schedule)) return;
            schedule.tickEnd = tickEnd;
        }

        public static void RemoveAction(string key, Action<long> action)
        {
            if (key == null || !_schedules.TryGetValue(key, out var schedule)) return;
            schedule.actionEverySecond = schedule.actionEverySecond.Remove(action);
        }

        public static void RemoveAndEndTick(string key)
        {
            if (!_schedules.TryGetValue(key, out var schedule)) return;
            schedule.actionEverySecond?.Invoke(0);
            schedule.actionEnd?.Invoke();
            _schedules.Remove(key);
        }

        public static void RemoveTick(string key)
        {
            if (!_keysToRemoveSchedules.Contains(key))
                _keysToRemoveSchedules.Add(key);
        }

        public static bool HasTick(string key) => _schedules.ContainsKey(key);

        private static void CountTimeRemainNextDay()
        {
            AddTick("TimeRemainNextDay", SecondNextDay, null, CountTimeRemainNextDay);
        }
    }

    public class Schedule
    {
        public string key;
        public Action<long> actionEverySecond;
        public Action actionEnd;
        public long tickEnd;
    }
}