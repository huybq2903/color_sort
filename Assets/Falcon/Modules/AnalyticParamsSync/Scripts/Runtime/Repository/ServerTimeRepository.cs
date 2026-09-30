/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.Network;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    [Primary]
    public class ServerTimeRepository : ITimeRepository
    {
        private readonly ReserveTimeRepository _timeRepository;

        public ServerTimeRepository(ReserveTimeRepository timeRepository)
        {
            _timeRepository = timeRepository;
        }

        public async Task Init(CancellationToken cancellationToken = default)
        {
            if(!await ServerSessionStateListener.WaitConnect()) return;

            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var t1 = MyTime.CurrentTimeMillis;
            new FTimePing().AddSCListener<FTimePong>((message, timeout, success) =>
            {
                try
                {
                    if (success)
                    {
                        var serverMillis = message.timeServer;
                        var t4 = MyTime.CurrentTimeMillis;
                        // ReSharper disable once PossibleLossOfFraction
                        LocalDiffDelta = TimeSpan.FromMilliseconds(serverMillis - (t1 + t4) / 2);
                    }
                    else
                    {
                        LocalDiffDelta = _timeRepository.LocalDiffDelta;
                    }

                    tcs.TrySetResult(true);
                } 
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }).Send();
            await using (cancellationToken.Register(() =>
                             tcs.TrySetCanceled(cancellationToken)))
            {
                await tcs.Task; // ✅ mọi exception quay về đây
            }
        }

        public long CurrentTimeMillis => (DateTimeOffset.UtcNow + LocalDiffDelta).ToUnixTimeMilliseconds();
        public TimeSpan LocalDiffDelta { get; private set; } = TimeSpan.Zero;
    }
}