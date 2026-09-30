/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    public class FConfigInitService : IInit
    {
#pragma warning disable S1075 // URIs should not be hardcoded    
        private const string SERVER_URL =
            "https://gateway.data4game.com/kapigateway/abtestingservice/sdk-request/get-config";
#pragma warning restore S1075 // URIs should not be hardcoded
        private readonly IFAppInfoRepository _appInfoRepository;

        private readonly FCentralUserParamService _centralUserParamService;
        private readonly IFConfigRepository _configRepository;

        public FConfigInitService(
            IFConfigRepository configRepository,
            FCentralUserParamService centralUserParamService,
            IFAppInfoRepository appInfoRepository
        )
        {
            _configRepository = configRepository;
            _centralUserParamService = centralUserParamService;
            _appInfoRepository = appInfoRepository;
        }

        public ExecState InitState { get; private set; } = ExecState.NotStarted;

        private readonly AtomicRef<bool> _fetching = new(false);

        public async Task Init(CancellationToken cancellationToken = default)
        {
            InitState = ExecState.Processing;
            InitState = await TryFetch(cancellationToken) ? ExecState.Succeed : ExecState.Failed;
        }

        /// <summary>
        /// Fetch lại remote config từ server theo yêu cầu (ngoài lần fetch lúc Init).
        /// Chỉ cho phép 1 fetch chạy tại 1 thời điểm để tránh xung đột đọc/ghi config;
        /// nếu đang có fetch khác chạy thì trả về false ngay.
        /// </summary>
        /// <returns>true nếu fetch + lưu config thành công (và OnUpdateFromNet đã được bắn).</returns>
        public async Task<bool> TryFetch(CancellationToken cancellationToken = default)
        {
            if (!_fetching.CompareAndSet(false, true)) return false;
            try
            {
                var success = await FetchAndApply(cancellationToken);
                if (success) InitState = ExecState.Succeed;
                return success;
            }
            finally
            {
                _fetching.Value = false;
            }
        }

        private async Task<bool> FetchAndApply(CancellationToken cancellationToken)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                BaseSystemLogger.Instance.Warning("Skip remote config: no network");
                return false;
            }
            try
            {
                var configResponse = await RequestConfig(cancellationToken);
                _configRepository.Save(configResponse);
                try
                {
                    OnUpdateFromNet?.Invoke();
                }
                catch (Exception e)
                {
                    BaseSystemLogger.Instance.Error(e);
                }

                return true;
            }
            catch (Exception e)
            {
                BaseSystemLogger.Instance.Error(e);
                return false;
            }
        }

        public event Action OnUpdateFromNet;

        private async Task<ConfigResponse> RequestConfig(CancellationToken cancellationToken = default)
        {
            var request = PrepareConfigRequest();
            BaseSystemLogger.Instance.Info($"Sending remote config request: {request.ToJson()}");
            var successStrBody = await (await new PostRequest(SERVER_URL)
                .SetJsonBody(request)
                .Execute(cancellationToken)).SuccessStrBody();
            BaseSystemLogger.Instance.Info($"Receive remote config response: {successStrBody}");
            return successStrBody.JsonToObj<ConfigResponse>();
        }

        private ConfigRequest PrepareConfigRequest()
        {
            return new ConfigRequest
            {
                properties = _centralUserParamService.GetUserParams(),
                abTestingConfigs = _configRepository.TestingConfigs,
                runningAbTesting = _configRepository.RunningAbTesting,
                packageName = _appInfoRepository.PackageName,
                campaignMeta = _configRepository.CampaignMeta
            };
        }
    }

    [NoLazy]
    public class FConfigUpdateReactCaller : IMySingleton
    {
        private readonly FConfigInitService _initService;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly List<IConfigUpdatedReact> _updatedReacts;

        public FConfigUpdateReactCaller(
            [SingletonSorting(SortingOrder.CREATING)]
            List<IConfigUpdatedReact> updatedReacts,
            FConfigInitService initService
        )
        {
            _updatedReacts = updatedReacts;
            _initService = initService;

            _initService.OnUpdateFromNet += () =>
            {
                foreach (var react in _updatedReacts)
                    react.OnConfigUpdated();
            };
        }
    }
}