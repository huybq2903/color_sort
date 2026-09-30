/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-17
 */

using System.Collections.Generic;
using System.Threading;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class AppPauseLogCollector : ITerminal
    {
        private readonly List<IAppPauseLogGenerator> _appPauseLogGenerators;
        private readonly LogScheduleService _scheduleService;
        private readonly FalconBigDataController _controller;

        public AppPauseLogCollector(
            [SingletonSorting(SortingOrder.DESTROYING)] List<IAppPauseLogGenerator> appPauseLogGenerators,
            LogScheduleService scheduleService,
            FalconBigDataController controller
        )
        {
            _appPauseLogGenerators = appPauseLogGenerators;
            _scheduleService = scheduleService;
            _controller = controller;
        }

        public void OnPostStop()
        {
            foreach (var generator in _appPauseLogGenerators)
                _controller.SendAll(generator.GetLogsOnAppPause());

            new Thread(() => { _scheduleService.TryFlush(); })
            {
                IsBackground = false,
                Priority = ThreadPriority.Highest
            }.Start();
        }
    }
}