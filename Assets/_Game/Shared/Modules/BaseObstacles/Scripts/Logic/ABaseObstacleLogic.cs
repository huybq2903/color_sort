/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using System;
using System.Reflection;
using Falcon.Helpers.EventBus;
using Falcon.Helpers.FReflection;
using Falcon.Shared.BaseInGame;
using Sirenix.OdinInspector;
using Falcon.Shared.Common;

namespace Falcon.Shared.BaseObstacle
{
    public abstract class ABaseObstacleLogic : IFReflection, IDisposable
    {
        public string Type => GetType().GetCustomAttribute<ObstacleAttribute>()?.Type;

        public virtual int LevelUnlock => GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, $"level_unlock_{Type}");

        public virtual bool NeedUnlockTutorial => GameRequest<int>.Request(GameKeys.GET_LEVEL) == LevelUnlock;
        public abstract void SetData(AObstacleManager manager, PropertyData data, PropertyRuntime runtime);

        public abstract void BeforeSpawn();

        public abstract void AfterSpawn();

        public virtual void Dispose() { }
    }

    public abstract class ABaseObstacleLogic<D, R> : ABaseObstacleLogic
        where D : PropertyData
        where R : PropertyRuntime, new()
    {
        [ShowInInspector, ReadOnly] public D Data { get; private set; }
        [ShowInInspector, ReadOnly] public R Runtime { get; private set; }

        protected AObstacleManager _manager;

        public override void SetData(AObstacleManager manager, PropertyData data, PropertyRuntime runtime)
        {
            _manager = manager;
            Data = data as D;
            Runtime = runtime as R;

            // Chưa có runtime (màn mới) thì dựng từ Data rồi nhét vào LevelRuntime để nó được save.
            if (Runtime == null)
            {
                Runtime = DefaultRuntime();
                manager.LevelManager.LevelRuntime?.SetProperty(Runtime);
            }
        }

        /// <summary>Trạng thái ban đầu, thường là bản copy của Data.</summary>
        protected virtual R DefaultRuntime() => new();
    }
}
