/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.FReflection;
using Falcon.Shared.BaseInGame;
using Falcon.Shared.Tutorial;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseObstacle
{
    public abstract class AObstacleManager : MonoBehaviour, ISubManager, ILevelBeforeSpawn, ILevelAfterSpawn
    {
        [ShowInInspector, ReadOnly] protected readonly Dictionary<string, ABaseObstacleLogic> _dictLogics = new();

        [field: Inject] public ALevelManager LevelManager { get; }
        [field: Inject] protected ATutorialManager TutorialManager { get; }

        public virtual UniTask BeforeSpawn()
        {
            var levelData = LevelManager.LevelData;
            var logicTypes = FReflection.Instance.GetTypes().Where(type => typeof(ABaseObstacleLogic).IsAssignableFrom(type) && !type.IsAbstract);

            foreach (var logicType in logicTypes)
            {
                var obstacleKey = logicType.GetCustomAttribute<ObstacleAttribute>()?.Type;
                if (obstacleKey == null) continue;

                // Level không khai obstacle này thì không tạo logic. Đây là toàn bộ cơ chế bật/tắt obstacle theo màn.
                if (!levelData.properties.TryGetValue(obstacleKey, out var property)) continue;
                if (Activator.CreateInstance(logicType) is not ABaseObstacleLogic logic) continue;

                var runtime = LevelManager.LevelRuntime?.properties.GetValueOrDefault(obstacleKey);

                logic.SetData(this, property, runtime);
                _dictLogics[obstacleKey] = logic;
            }

            foreach (var (key, logic) in _dictLogics)
            {
                try
                {
                    logic.BeforeSpawn();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AObstacleManager] BeforeSpawn obstacle '{key}' lỗi: {e}");
                }
            }

            return UniTask.CompletedTask;
        }

        public virtual UniTask AfterSpawn()
        {
            foreach (var (key, logic) in _dictLogics)
            {
                try
                {
                    logic.AfterSpawn();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AObstacleManager] AfterSpawn obstacle '{key}' lỗi: {e}");
                }
            }

            OnUnlockObstacleIfHad();
            return UniTask.CompletedTask;
        }

        protected virtual void OnUnlockObstacleIfHad()
        {
            foreach (var logic in _dictLogics.Values)
            {
                if (!logic.NeedUnlockTutorial) continue;

                if (TutorialManager.PreloadTutorial($"unlock_{logic.Type}") is not ATutorialUnlockObstacle tutorial)
                    Debug.LogWarning($"[AObstacleManager] Thiếu tutorial 'unlock_{logic.Type}'.");
                else
                {
                    tutorial.ObstacleLogic = logic;
                    tutorial.StartTutorial();
                }
            }
        }

        protected virtual void OnDisable()
        {
            foreach (var logic in _dictLogics.Values)
                logic?.Dispose();

            _dictLogics.Clear();
        }
    }

    /// <summary>Khoá logic vào key của property trong level JSON. Cùng key với [PropertyDataType] và [PropertyRuntimeType].</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ObstacleAttribute : Attribute
    {
        public string Type { get; }

        public ObstacleAttribute(string type)
        {
            Type = type;
        }
    }
}
