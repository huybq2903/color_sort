/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    public class FConfigInitService : IMySingleton
    {
        private readonly IFConfigRepository _configRepository;

        public FConfigInitService(IFConfigRepository configRepository)
        {
            _configRepository = configRepository;
        }

        public ExecState InitState { get; private set; } = ExecState.NotStarted;


        public async Task Init(Dictionary<string, object> configs)
        {
            InitState = ExecState.Processing;
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                BaseSystemLogger.Instance.Warning("Skip remote config: no network");
                InitState = ExecState.Failed;
                return;
            }

            try
            {
                _configRepository.Save(configs);

                InitState = ExecState.Succeed;
                try
                {
                    OnUpdateFromNet?.Invoke();
                }
                catch (Exception e)
                {
                    BaseSystemLogger.Instance.Error(e);
                }
            }
            catch (Exception e)
            {
                BaseSystemLogger.Instance.Error(e);
                InitState = ExecState.Failed;
            }
        }

        public event Action OnUpdateFromNet;
        private TaskCompletionSource<string> _requestConfigTcs;
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