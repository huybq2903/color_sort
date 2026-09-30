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
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public static class InitService
    {
        private static readonly ConcurrentDictionary<Type, ExecState> kExecStates = new();

        private static readonly LazyVal<string> kLazyInit = new(() =>
        {
            if (Application.isPlaying) MainGameObj.DoTask(CallInit);
            return "IInit is being called";
        });

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            var initializing = kLazyInit.Value;
            
            foreach (var singleton in MySingletonService.ChildInstancesInitOrder<ISingletonServiceReady>())
                singleton.OnSingletonServiceReady();
            
            SingletonLogger.Instance.Info(initializing);
        }

        public static ExecState AllInitState { get; private set; } = ExecState.NotStarted;
        private static async Task CallInit()
        {
            var instanceDestroyToken = MainGameObj.Instance.destroyCancellationToken;
            
            AllInitState = ExecState.Processing;
            var timer = new Timer();
            var childInstancesInitOrder = MySingletonService.ChildInstancesInitOrder<IInit>();
            SingletonLogger.Instance.Info(timer.LogAndReset("Init MySingletonService"));
            await Task.Yield();
            
            foreach (var init in childInstancesInitOrder)
                try
                {
                    SingletonLogger.Instance.Info($"Init {init.GetType().Name} started");
                    kExecStates[init.GetType()] = ExecState.Processing;
                    
                    using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                        timeoutSource.Token, 
                        instanceDestroyToken
                    );
                    
                    await init.Init(linkedSource.Token);
                    SingletonLogger.Instance.Info(timer.LogAndReset($"Init {init.GetType().Name}"));
                    kExecStates[init.GetType()] = ExecState.Succeed;
                }
                catch (OperationCanceledException) when (instanceDestroyToken.IsCancellationRequested)
                {
                    // Object bị destroy, thoát ngang không cần log lỗi nặng
                    SingletonLogger.Instance.Warning($"Init {init.GetType().Name} cancelled due to Object Destruction.");
                    return; 
                }
                catch (Exception e)
                {
                    kExecStates[init.GetType()] = ExecState.Failed;
                    SingletonLogger.Instance.Error($"Init {init.GetType().Name} failed after {timer.StopAndReset().TotalMilliseconds}ms", e);
                }
            AllInitState = ExecState.Succeed;
            SingletonLogger.Instance.Info(timer.LogTotal("Init all IInit"));
        }

        public static ExecState InitState(this IInit init)
        {
            return kExecStates.GetOrAdd(init.GetType(), ExecState.NotStarted);
        }
    }
}