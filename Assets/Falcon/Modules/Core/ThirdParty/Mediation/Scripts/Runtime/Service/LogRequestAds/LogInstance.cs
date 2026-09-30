using System;
using System.Collections.Generic;
using Falcon.Modules.Core.RemoteConfigCms;
using Falcon.Modules.Core.Utils.Time.Runtime;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class LogInstance : MonoBehaviour
    {
        private const float _DEFAULT_PERIOD = 30f;
        private const float _MIN_PERIOD = 5f;

        private static LogInstance _instance;

        public static LogInstance Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject();
                    go.name = "LogInstance";
                    //DontDestroyOnLoad trước AddComponent để instance không phụ thuộc phần khởi tạo phía sau
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<LogInstance>();
                    _instance.StartTimer();
                }

                return _instance;
            }
        }

        private readonly List<AdRequestLogItem> _logItems = new();

        private long _startTime;
        private long _nextLogTime;
        private bool _running;

        private static long NowMs()
        {
            return new DateTimeOffset(TimeUtils.UTCNow).ToUnixTimeMilliseconds();
        }

        void StartTimer()
        {
            try
            {
                _startTime = NowMs();
                _nextLogTime = _startTime + (long)(GetPeriod() * 1000);
                _running = true;
            }
            catch (Exception e)
            {
                //vd TimeUtils chưa init xong, Update sẽ thử lại ở frame sau
                Debug.LogException(e);
            }
        }

        //Đếm bằng wall-clock trong Update thay vì Task.Delay:
        //Task.Delay resume theo player loop nên bị treo khi app xuống background / hiện full-screen ad,
        //và nếu task chết vì exception thì không có cách nào chạy lại. Update thì luôn tick lại ngay khi
        //game resume, không bị ảnh hưởng bởi Time.timeScale, và không có gì để bị cancel.
        private void Update()
        {
            if (!_running)
            {
                //tự khởi động lại nếu StartTimer lần đầu thất bại
                StartTimer();
                return;
            }

            var now = NowMs();

            //TimeUtils có thể nhảy lùi khi đồng bộ lại giờ server
            if (now < _startTime)
            {
                OpenNewWindow(now);
                return;
            }

            if (now < _nextLogTime)
                return;

            FlushLogs(true);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            //flush trước khi bị treo để không mất log nếu OS kill app lúc đang ở background
            if (pauseStatus)
            {
                FlushLogs(false);
            }
        }

        private void OnApplicationQuit()
        {
            FlushLogs(false);
        }

        private float GetPeriod()
        {
            var period = _DEFAULT_PERIOD;
            try
            {
                period = FConfigControllerCms.Instance.Config<AdRequestLogConfig>().periodLogAdRequest;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            //chặn giá trị 0 (gửi liên tục mỗi frame) và giá trị âm
            return period < _MIN_PERIOD ? _MIN_PERIOD : period;
        }

        private void OpenNewWindow(long now)
        {
            _startTime = now;
            _nextLogTime = now + (long)(GetPeriod() * 1000);
        }

        private void FlushLogs(bool deadlineReached)
        {
            if (!_running)
                return;

            var now = NowMs();
            var windowStart = _startTime;

            List<AdRequestLogItem> snapshot = null;
            try
            {
                snapshot = TakeSnapshot();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            var hasData = snapshot != null && snapshot.Count > 0;

            //tới hạn thì luôn mở window mới kể cả khi snapshot/gửi lỗi, nếu không Update sẽ retry mỗi frame.
            //flush ngoài chu kỳ (pause/quit) mà không có dữ liệu thì giữ nguyên window, tránh việc
            //pause/resume liên tục đẩy hạn gửi ra vô hạn
            if (deadlineReached || hasData)
            {
                OpenNewWindow(now);
            }

            if (!hasData)
                return;

            try
            {
                //gửi bản copy: CSMessageWaitLoginSuccess có thể queue lại chờ login, lúc đó list gốc đã bị reset
                new CSAdRequestLog(snapshot, windowStart, now).Send();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private List<AdRequestLogItem> TakeSnapshot()
        {
            var snapshot = new List<AdRequestLogItem>();
            for (int i = 0; i < _logItems.Count; i++)
            {
                var item = _logItems[i];
                if (item == null)
                    continue;

                if (item.numRequest == 0
                    && (item.errorDetail == null || item.errorDetail.Count == 0)
                    && (item.adSuccessInfos == null || item.adSuccessInfos.Count == 0))
                {
                    continue;
                }

                snapshot.Add(CloneAndReset(item));
            }

            return snapshot;
        }

        private static AdRequestLogItem CloneAndReset(AdRequestLogItem src)
        {
            var copy = new AdRequestLogItem
            {
                adUnitId = src.adUnitId,
                adType = src.adType,
                decisionPolicy = src.decisionPolicy,
                numRequest = src.numRequest,
                errorDetail = src.errorDetail == null
                    ? new Dictionary<string, int>()
                    : new Dictionary<string, int>(src.errorDetail),
                adSuccessInfos = src.adSuccessInfos == null
                    ? new List<AdSuccessInfo>()
                    : new List<AdSuccessInfo>(src.adSuccessInfos)
            };

            src.numRequest = 0;
            src.errorDetail?.Clear();
            src.adSuccessInfos?.Clear();
            return copy;
        }

        public AdRequestLogItem GetAdRequestLogItemByAdUnitId(string adUnitId)
        {
            for (int i = 0; i < _logItems.Count; i++)
            {
                if (string.Equals(_logItems[i].adUnitId, adUnitId, StringComparison.Ordinal))
                {
                    return _logItems[i];
                }
            }

            return null;
        }

        public void AddAdLog(AdRequestLogItem adRequestLogItem)
        {
            _logItems.Add(adRequestLogItem);
        }
    }
}
