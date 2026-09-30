/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System.Collections.Concurrent;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class BaseLogDecorService : MySingleton<BaseLogDecorService>, ILogDecorator<AFalconLog>
    {
        private readonly FCentralUserParamService _centralUserParamService;
        private readonly ITimeRepository _timeRepository;
        private readonly IDataPool _dataPool;
        
        private readonly ConcurrentDictionary<string, BasicPoolData<int>> _createId = new();
        private readonly ConcurrentDictionary<string, BasicPoolData<int>> _sendId = new();

        public BaseLogDecorService(
            FCentralUserParamService centralUserParamService,
            ITimeRepository timeRepository,
            IDataPool dataPool
        )
        {
            _dataPool = dataPool;
            _centralUserParamService = centralUserParamService;
            _timeRepository = timeRepository;
        }

        /// <summary>
        /// Central param nào là định danh của MỘT khoảnh khắc thì không được dán lên bản tin tổng
        /// hợp: giá trị lúc gửi chỉ đúng cho khoảnh khắc cuối cùng trong cụm.
        /// </summary>
        private static readonly HashSet<string> kMomentOnlyKeys = new()
        {
            TurnCustomInfoRepository.PLAY_TURN_ID_KEY
        };

        public int Priority => -100;

        public void Decor(AFalconLog log)
        {
            log.logTypeCreateId = GetCreateId(log);
            log.createdDateLocal = MyTime.DateToString(_timeRepository.LocalNow());
            log.logTypeSendId = GetSendId(log);
        }

        /// <summary>
        /// Chèn central user params vào payload. Bản tin TỔNG HỢP (<see cref="PlainLog.IsSpanRecord"/>)
        /// thì bỏ qua các key chỉ đúng cho MỘT khoảnh khắc — xem <see cref="kMomentOnlyKeys"/>.
        /// </summary>
        public Dictionary<string, object> InjectDictionary(PlainLog log, Dictionary<string, object> dictionary)
        {
            var spanRecord = log?.IsSpanRecord ?? false;
            foreach (var (key, value) in _centralUserParamService.GetUserParams())
            {
                if (spanRecord && kMomentOnlyKeys.Contains(key)) continue;
                dictionary.PutIfAbsent(key, value);
            }

            return dictionary;
        }

        private int GetCreateId(AFalconLog log)
        {
            return _createId
                .GetOrAdd(log.Event + "app_id", key => new BasicPoolData<int>(_dataPool, key, -1))
                .Compute(val => val + 1);
        }

        public int GetSendId(AFalconLog log)
        {
            return _sendId
                .GetOrAdd(log.Event + "_send_id", key => new BasicPoolData<int>(_dataPool, key, -1))
                .Compute(val => val + 1);
        }
    }
}