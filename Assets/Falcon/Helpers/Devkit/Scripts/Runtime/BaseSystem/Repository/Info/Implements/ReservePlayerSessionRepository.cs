/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class ReservePlayerSessionRepository : IFPlayerSessionRepository, IInit
    {
        private const string ANALYTIC_DATA_PREFIX = "Analytic_SDK_Data_";
        private const string ACTIVE_DAYS_KEY = ANALYTIC_DATA_PREFIX + "Active_Days";
        private const string FIRST_LOGIN_MILLIS_KEY = "CREATE_DATE";
        private const string SESSION_ID_KEY = "Session_Count";
        private const string LATEST_LOGIN_DATE_KEY = "LATEST_DATE";
        private const string TIME_SYNCED_KEY = "TIME_SYNCED";
        private const string MODE_TOTAL_PREFIX = "Session_Total_";
        
        private readonly BasicPoolData<int> _activeDays;
        private readonly IDataPool _dataPool;
        
        private readonly ITimeRepository _timeRepository;
        private readonly BasicPoolData<long> _firstLogInMillis;
        private readonly BasicPoolData<DateTime?> _lastLogInDateTimeLocal;
        private readonly BasicPoolData<int> _sessionId;
        private readonly BasicPoolData<bool> _timeSynced;

        public ReservePlayerSessionRepository(IDataPool dataPool, ITimeRepository timeRepository)
        {
            _dataPool = dataPool;
            _timeRepository = timeRepository;

            _sessionId = new BasicPoolData<int>(dataPool, SESSION_ID_KEY, 0);
            _firstLogInMillis = new BasicPoolData<long>(dataPool, FIRST_LOGIN_MILLIS_KEY, timeRepository.CurrentTimeMillis);
            _activeDays = new BasicPoolData<int>(dataPool, ACTIVE_DAYS_KEY, 0);
            _lastLogInDateTimeLocal = new BasicPoolData<DateTime?>(dataPool, LATEST_LOGIN_DATE_KEY, null);
            _timeSynced = new BasicPoolData<bool>(dataPool, TIME_SYNCED_KEY, false);

            _sessionId.Compute(i => i + 1);
        }

        public ConcurrentDictionary<string, BasicPoolData<long>> ModeTimes { get; } = new();

        public long GetModeTotalSec(string gameMode)
        {
            return ModeTimes.Compute(gameMode, val =>
            {
                val ??= new BasicPoolData<long>(_dataPool, MODE_TOTAL_PREFIX + gameMode, 0);
                return val;
            }).Value;
        }

        public long IncreaseModeTotalSec(string gameMode, long seconds)
        {
            return ModeTimes.Compute(gameMode, val =>
            {
                val ??= new BasicPoolData<long>(_dataPool, MODE_TOTAL_PREFIX + gameMode, 0);
                return val;
            }).Compute(l => l + seconds);
        }

        public long FirstLogInMillis
        {
            get => _firstLogInMillis.Value;
            set => _firstLogInMillis.Value = value;
        }

        public int ActiveDays
        {
            get => _activeDays.Value;
            set => _activeDays.Value = value;
        }

        public int SessionId
        {
            get => _sessionId.Value;
            set => _sessionId.Value = value;
        }

        public DateTime? LastLoginInDateTimeLocal
        {
            get => _lastLogInDateTimeLocal.Value;
            set => _lastLogInDateTimeLocal.Value = value;
        }

        public bool RetentionChanged { get; private set; }

        public Task Init(CancellationToken cancellationToken = default)
        {
            if (!_timeSynced.Value)
            {
                _timeSynced.Value = true;
                _firstLogInMillis.Value += (long) _timeRepository.LocalDiffDelta.TotalMilliseconds;
            }
            var firstLoginDate = DateTime.UnixEpoch.AddMilliseconds(FirstLogInMillis);
            BaseSystemLogger.Instance.Info($"Player First Login Date: {firstLoginDate}");
            
            if (!LastLoginInDateTimeLocal.HasValue)
                RetentionChanged = true;
            else
                RetentionChanged = DateTime.Compare(_timeRepository.LocalNow().Date, LastLoginInDateTimeLocal.Value.Date) > 0;
            LastLoginInDateTimeLocal = _timeRepository.LocalNow();
            if (RetentionChanged) ActiveDays++;
            return Task.CompletedTask;
        }

        public void SetModeTotalSec(string gameMode, long value)
        {
            ModeTimes.Compute(gameMode, val =>
            {
                val ??= new BasicPoolData<long>(_dataPool, MODE_TOTAL_PREFIX + gameMode, 0);
                val.Value = value;
                return val;
            });
        }

        public long ComputeModeTotalSec(string gameMode, Func<long, long> computation)
        {
            return ModeTimes.Compute(gameMode, val =>
            {
                val ??= new BasicPoolData<long>(_dataPool, MODE_TOTAL_PREFIX + gameMode, 0);
                val.Compute(computation);
                return val;
            }).Value;
        }
    }
}