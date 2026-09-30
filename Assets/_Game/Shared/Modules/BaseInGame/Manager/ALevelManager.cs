/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-16
 */

using System;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Shared.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseInGame
{
    public abstract class ALevelManager : MonoBehaviour, ISubManager
    {
        [ShowInInspector, ReadOnly] public LevelData LevelData { get; protected set; }
        [ShowInInspector, ReadOnly] public LevelRuntime LevelRuntime { get; protected set; }
        [ShowInInspector, ReadOnly] protected int difficulty;
        [ShowInInspector, ReadOnly] protected int level;

        public bool IsResume { get; protected set; }

        protected const string EVENT_GET_LEVEL_DATA = "get_level_data";

        public virtual void Initialized()
        {
            level = GameRequest<int>.Request(GameKeys.GET_LEVEL);
            difficulty = GameRequest<int, int>.Request(GameKeys.GET_LEVEL_DIFFICULTY, level);

            GameRequest<LevelData>.Register(EVENT_GET_LEVEL_DATA, Clone, this);
            GameEvent.Register(GameKeys.LEVEL_RUNTIME, SaveLevelRuntime, this);
            GameEvent.Register(GameKeys.CLEAR_LEVEL_RUNTIME, ClearLevelRuntime, this);

            var levelString = DataTempExtensions<string>.Get("level_data");
            LevelData = !string.IsNullOrEmpty(levelString) ?
                LevelData.FromJson(DecompressLevel(levelString)) :
                null;

            LevelRuntime = DataTempExtensions<LevelRuntime>.Get(GameKeys.LEVEL_RUNTIME);
            if (LevelRuntime == null)
            {
                IsResume = false;
                LevelRuntime = new LevelRuntime { hash = GetHashLevel(levelString) };
            }
            else
            {
                IsResume = true;
            }
        }

        protected virtual void OnDisable()
        {
            GameRequest<LevelData>.Unregister(EVENT_GET_LEVEL_DATA);
            GameEvent.Unregister(GameKeys.LEVEL_RUNTIME, SaveLevelRuntime, this);
            GameEvent.Unregister(GameKeys.CLEAR_LEVEL_RUNTIME, ClearLevelRuntime, this);
        }

        protected virtual LevelData Clone() => LevelData.Clone();

        protected virtual void ClearLevelRuntime()
        {
            LevelRuntime = null;
            SaveLoadHandler.DeleteKey(GameKeys.LEVEL_RUNTIME);
            DataTempExtensions<LevelRuntime>.Remove(GameKeys.LEVEL_RUNTIME);
        }

        protected virtual void SaveLevelRuntime()
        {
            if (LevelRuntime == null) return;
            SaveLoadHandler.Save(GameKeys.LEVEL_RUNTIME, LevelRuntime.ToJson());
        }

        protected Func<string, string> DecompressLevel => LevelCompressor.Decompress;
        protected Func<string, string> GetHashLevel => Md5Utils.GetMd5First5Char;
    }
}