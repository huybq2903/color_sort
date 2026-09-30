/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    // Required for Stopwatch
    // For StringBuilder in ToString() for better performance if needed

    // Optional: For Thread.Sleep if demonstrating delays

    /// <summary>
    ///     A simple timer class to measure elapsed time, similar to Java's System.currentTimeMillis() usage.
    ///     Uses System.Diagnostics.Stopwatch for high-resolution, accurate timing.
    /// </summary>
    public class Timer
    {
        // Stopwatch is designed for measuring elapsed time accurately.
        private readonly Stopwatch _totalStopwatch; // Measures total time from initialization
        private readonly Stopwatch _taskStopwatch; // Measures time for individual tasks/resets

        /// <summary>
        ///     Initializes a new instance of the Timer class.
        ///     Both total time and current task time start from now.
        /// </summary>
        public Timer()
        {
            _totalStopwatch = Stopwatch.StartNew(); // Starts and records initial time
            _taskStopwatch = Stopwatch.StartNew(); // Starts for the first task
        }

        /// <summary>
        ///     Resets the current task timer to zero and restarts it.
        ///     The total timer continues uninterrupted.
        /// </summary>
        public void Reset()
        {
            _taskStopwatch.Restart(); // Stops, resets to zero, and starts again
        }

        /// <summary>
        ///     Stops the current task timer and returns the elapsed milliseconds since the last reset or initialization.
        ///     The timer remains stopped.
        /// </summary>
        /// <returns>The elapsed time in TimeSpan.</returns>
        public TimeSpan Stop()
        {
            _taskStopwatch.Stop(); // Stop the timer
            return _taskStopwatch.Elapsed;
        }

        /// <summary>
        ///     Stops the current task timer, returns the elapsed milliseconds, and then resets and restarts it.
        /// </summary>
        /// <returns>The elapsed time in TimeSpan before resetting.</returns>
        public TimeSpan StopAndReset()
        {
            var elapsed = _taskStopwatch.Elapsed;
            _taskStopwatch.Restart(); // Get elapsed, then reset and start
            return elapsed;
        }

        /// <summary>
        ///     Stops the current task timer and returns the elapsed milliseconds since the last reset or initialization.
        ///     The timer remains stopped.
        /// </summary>
        /// <returns>The elapsed time in milliseconds.</returns>
        public long StopMillis()
        {
            _taskStopwatch.Stop(); // Stop the timer
            return _taskStopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        ///     Stops the current task timer, returns the elapsed milliseconds, and then resets and restarts it.
        /// </summary>
        /// <returns>The elapsed time in milliseconds before resetting.</returns>
        public long StopAndResetMillis()
        {
            var elapsed = _taskStopwatch.ElapsedMilliseconds;
            _taskStopwatch.Restart(); // Get elapsed, then reset and start
            return elapsed;
        }
        
        /// <summary>
        ///     Stops the current task timer, logs the elapsed time with a given task name, and then resets and restarts it.
        /// </summary>
        /// <param name="taskName">The name of the task to log.</param>
        /// <returns>A formatted string indicating the task name and time taken.</returns>
        public string LogAndReset(string taskName)
        {
            var elapsed = _taskStopwatch.ElapsedMilliseconds;
            _taskStopwatch.Restart();
            return $"{taskName} took {elapsed}ms";
        }
        
        /// <summary>
        ///     Stops the current task timer, logs total elapsed time in milliseconds since the Timer was initialized. with a given task name, and then resets and restarts it.
        /// </summary>
        /// <param name="taskName">The name of the task to log.</param>
        /// <returns>A formatted string indicating the task name and time taken.</returns>
        public string LogTotal(string taskName)
        {
            var elapsed = _totalStopwatch.ElapsedMilliseconds;
            return $"{taskName} took {elapsed}ms";
        }

        /// <summary>
        ///     Returns the total elapsed time in milliseconds since the Timer was initialized.
        /// </summary>
        /// <returns>The total elapsed time in milliseconds.</returns>
        public long TotalMillis()
        {
            return _totalStopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        ///     Returns the total elapsed time as a TimeSpan object since the Timer was initialized.
        /// </summary>
        /// <returns>A TimeSpan representing the total elapsed time.</returns>
        public TimeSpan TotalTime()
        {
            return _totalStopwatch.Elapsed; // Stopwatch.Elapsed returns a TimeSpan
        }
    }
}