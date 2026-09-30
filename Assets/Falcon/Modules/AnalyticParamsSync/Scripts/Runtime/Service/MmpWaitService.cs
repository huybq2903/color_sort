/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-20
 */
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    [NoLazy]
    public class MmpWaitService : MonoSingleton, IPostConstruct
    {
        private const string FALCON_MMP_STARTED = "falcon_mmp_started";

        public void OnPostConstruct()
        {
            GameEvent.Register(FALCON_MMP_STARTED, CheckLog, this);
        }

        private static void CheckLog()
        {
            if (InitService.AllInitState.IsDone())
                new FMmpInfoLog().Send();
            else
                new WaitInit(() => new FMmpInfoLog().Send()).Schedule();
        }
    }
}