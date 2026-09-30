/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.RemoteConfig;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class LogScheduleService : MySingleton<LogScheduleService>, IInit, IConfigUpdatedReact
    {
        private readonly IFAppInfoRepository _appInfoRepository;
        private readonly AnalyticConfigService _configService;
        private readonly LogDecorService _decorService;

        private readonly RepeatAction _repeatAction;
        private readonly IUnsentLogRepository _repository;
        private readonly LogSendService _sendService;
        private readonly MyLock _flushLock = new();

        public LogScheduleService(
            LogSendService sendService, GlobalThreadPool threadPool,
            IUnsentLogRepository repository, IFAppInfoRepository appInfoRepository,
            AnalyticConfigService configService, LogDecorService decorService
        )
        {
            _sendService = sendService;
            _repository = repository;
            _appInfoRepository = appInfoRepository;
            _configService = configService;
            _decorService = decorService;
            _repeatAction = new RepeatAction(() => TryFlush(), threadPool, TimeSpan.FromSeconds(_configService.BatchLogSendSec),
                TimeSpan.FromSeconds(15));
        }

        public void OnConfigUpdated()
        {
            _repeatAction.TimeSpan = TimeSpan.FromSeconds(_configService.BatchLogSendSec);
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            _repeatAction.Schedule();
            return Task.CompletedTask;
        }

        public bool TryFlush()
        {
            if (!_flushLock.TryLock(out var key)) return false;
            using (key)
            {
                try
                {
                    var unsent = _repository.PeekAll();
                    if (unsent.Count > 0) AnalyticLogger.Instance.Info(unsent.Count + " requests is waiting to be sent");
                    _sendService.SendBatch(unsent).Wait();
                    _repository.Drain(unsent.Count);
                }
                catch (Exception e)
                {
                    AnalyticLogger.Instance.Error(e);
                }
            }

            return true;

        }

        public void Enqueue(DataWrapper wrapper)
        {
            _repository.Enqueue(wrapper);
        }

        public void Enqueue(IDataLog log)
        {
            if (!IsSendable(log)) return;
            // Funnel decor (decorate-once): log đi cửa trước hay bị gọi thẳng vào đây đều được trang trí đúng 1 lần
            _decorService.Decor(log);
            var dataWrapper = new DataWrapper(log, _appInfoRepository.PackageName, _appInfoRepository.Platform);
            Enqueue(dataWrapper);
        }

        /// <summary>
        /// Log tự đánh trượt mình (validation-có-veto, vd bước funnel sai luật) thì dừng tại đây.
        /// Chặn ở funnel nên cả ba đường vào — log.Send(), Controller.Send(), Enqueue() — đều chịu.
        /// </summary>
        private static bool IsSendable(IDataLog log)
        {
            if (log is not PlainLog { IsSendable: false } plainLog) return true;

            AnalyticLogger.Instance.Info($"{plainLog.Event} bị chặn: log tự đánh trượt validation");
            return false;
        }

        public void EnqueueAll(IEnumerable<DataWrapper> wrappers)
        {
            var dataWrappers = wrappers.ToList();
            _repository.EnqueueAll(dataWrappers);
        }

        public void EnqueueAll(IEnumerable<IDataLog> wrappers)
        {
            var logs = wrappers.Where(IsSendable).ToList();
            foreach (var log in logs) _decorService.Decor(log);
            EnqueueAll(logs.Select(log =>
                new DataWrapper(log, _appInfoRepository.PackageName, _appInfoRepository.Platform)));
        }

        /// <summary>
        /// Gửi ngay trên thread phụ, bỏ qua chu kỳ batch (dời từ PlainLog.SendNow về đây —
        /// model không tự wrap nữa). Lỗi thì wrapper rơi về hàng đợi bền chờ chu kỳ sau;
        /// re-enqueue ở mức wrapper nên không bao giờ decor lại.
        /// </summary>
        public void SendNow(IDataLog log)
        {
            if (!IsSendable(log)) return;
            _decorService.Decor(log);
            var dataWrapper = new DataWrapper(log, _appInfoRepository.PackageName, _appInfoRepository.Platform);
            Task.Run(() =>
            {
                _sendService.Send(dataWrapper).ContinueWith(t =>
                {
                    if (t.IsFaulted) Enqueue(dataWrapper);
                });
            });
        }

        public bool Remove(DataWrapper wrapper)
        {
            return _repository.Remove(wrapper);
        }

        public bool RemoveAll(IEnumerable<DataWrapper> wrappers)
        {
            return _repository.RemoveAll(wrappers);
        }
    }
}