/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public interface ITimeRepository : IInit
    {
        public long CurrentTimeMillis { get; }
        public TimeSpan LocalDiffDelta { get; }
    }

    public static class TimeRepositoryExtensions
    {
        
        public static long CurrentTimeSec(this ITimeRepository repository)
        {
            return repository.CurrentTimeMillis / 1000;
        }
        
        public static DateTime UtcNow(this ITimeRepository repository)
        {
            return DateTime.UtcNow + repository.LocalDiffDelta;
        }
        
        public static DateTime LocalNow(this ITimeRepository repository)
        {
            return UtcNow(repository).ToLocalTime();
        }
    }
}