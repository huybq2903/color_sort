/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class ReserveTimeRepository : ITimeRepository
    {
        public async Task Init(CancellationToken cancellationToken = default)
        {
            var t1 = MyTime.CurrentTimeMillis;
            var response = await new GetRequest("https://dwhapi-v2.data4game.com/time/millis")
                .Execute(cancellationToken);
            var successStrBody = await response.SuccessStrBody();
            var serverMillis = long.Parse(successStrBody);
            var t4 = MyTime.CurrentTimeMillis;

            // ReSharper disable once PossibleLossOfFraction
            LocalDiffDelta = TimeSpan.FromMilliseconds(serverMillis - (t1 + t4) / 2);
        }

        public long CurrentTimeMillis => (DateTimeOffset.UtcNow + LocalDiffDelta).ToUnixTimeMilliseconds();
        public TimeSpan LocalDiffDelta { get; private set; } = TimeSpan.Zero;
    }
}