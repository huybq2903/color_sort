/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-06
     */


namespace Falcon.Modules.Core.Utils.Time.Runtime
{
    using System;
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Mainly use for UI
    /// </summary>
    public class AutoCountdownTimer
    {
        /// These properties will define how long every cycle is.
        /// If you want to make sure every 1 second you get notified despite a long period of time pass in,
        /// use these numbers: 9999, 9999, 9999
        
        private int _minMinutes; 
        private int _minHours;
        private int _minDays;

        private MonoBehaviour _runner;
        private Coroutine     _coroutine;
        
        public AutoCountdownTimer(int minDays = 30, int minHours = 24, int minMinutes = 60)
        {
            _minDays    = minDays;
            _minHours   = minHours;
            _minMinutes = minMinutes;
        }
        
        /// <summary>
        /// Run from future timestamp UTC on every cycle. Each cycle long is defined by <see cref="GetCycle"/> functin
        /// and <see cref="_minMinutes"/>, <see cref="_minHours"/> and <see cref="_minDays"/>
        /// </summary>
        public void StartCountDownFromFutureTimestampUTC(MonoBehaviour runner, long timestampUTC, bool unscaledTime, Action<long> onEveryCycle)
        {
            _runner = runner;
            
            long seconds = timestampUTC - TimeUtils.GetCurrentTimestampInSecondsUTC();
            if (seconds < 0)
            {
                Stop();
                return;
            }

            int cycle = GetCycle(seconds);
            if (_runner != null)
            {
                _coroutine = _runner.StartCoroutine(CoRun(seconds, cycle, unscaledTime, onEveryCycle));
            }
        }

        /// <summary>
        /// Run for a period of time. Each cycle long is defined by <see cref="GetCycle"/> functin
        /// and <see cref="_minMinutes"/>, <see cref="_minHours"/> and <see cref="_minDays"/>
        /// </summary>
        public void StartCountDownFromPeriodTime(MonoBehaviour runner, long seconds, bool unscaledTime, Action<long> onEveryCycle)
        {
            _runner = runner;
            
            if (seconds < 0)
            {
                Stop();
                return;
            }
            
            int cycle = GetCycle(seconds);
            if (_runner != null)
            {
                _coroutine = _runner.StartCoroutine(CoRun(seconds, cycle, unscaledTime, onEveryCycle));
            }
        }

        private IEnumerator CoRun(long seconds, int cycle, bool unscaledTime, Action<long> onEveryCycle)
        {
            while (seconds > 0)
            {
                if (unscaledTime)
                {
                    yield return new WaitForSecondsRealtime(cycle);
                }
                else
                {
                    yield return new WaitForSeconds(cycle);
                }

                seconds -= cycle;
                onEveryCycle?.Invoke(seconds);

                if (seconds > 0)
                {
                    int nextCycle = GetCycle(seconds);
                    if (nextCycle != cycle)
                    {
                        Stop();
                        if (_runner != null)
                        {
                            _coroutine = _runner.StartCoroutine(CoRun(seconds, cycle, unscaledTime, onEveryCycle));
                        }
                    }
                }
                else
                {
                    Stop();
                }
            }
        }

        private int GetCycle(long seconds)
        {
            if (seconds <= 0) return 0;
            
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            if (t.TotalDays >= _minDays)
            {
                return 86400;
            }
            if (t.TotalHours >= _minHours)
            {
                return 3600;
            }

            if (t.TotalMinutes >= _minMinutes)
            {
                return 60;
            }

            return 1;
        }

        public void Stop()
        {
            if (_runner != null && _coroutine != null)
            {
                _runner.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
    }
}