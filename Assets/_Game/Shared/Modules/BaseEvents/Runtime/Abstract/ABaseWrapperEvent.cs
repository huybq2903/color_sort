/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-10
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Helpers.FReflection;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.BaseEvents
{
    public abstract class ABaseWrapperEvent<C, D> : IWrapperEvent 
        where C : ABaseEventConfig
        where D : ABaseEventUserData, new()
    {
        private Dictionary<string, Func<int>> _notifies;

        // Gộp nhiều lần gọi liên tiếp thành một, tránh spam request.
        private readonly BufferedAction _saveAndSend = new();
        private readonly BufferedAction _afterGetData = new();

        public C Config { get; private set; }
        public D UserData { get; private set; }
        
        public abstract string Key { get; }
        public Dictionary<string, Func<int>> Notifies => _notifies ??= InitializedNotifies;
        public virtual ABaseEventConfig LocalConfig
        {
            get
            {
                if (_localConfig == null)
                {
                    var so = Resources.Load<SOEventConfig>($"SO_Config_Event_{Key}");
                    return _localConfig = !so ? null : so.configSO;
                }
                return _localConfig;
            }
        }

        private ABaseEventConfig _localConfig;

        public virtual void OnInitialize()
        {
            InitializeConfig();
            InitializeData();
        }
        
        public void RemoveSelf()
        {
            Center.Remove(GetType());
        }
        
        public void ReGetEvent(float delay = 5f)
        {
            new CSGetEventByKey() { key = Key }.SendAfter(delay);
        }

        protected virtual void InitializeConfig()
        {
            var configJson = SaveLoadHandler.Load<string>(Key + "_config");
            try
            {
                if (!string.IsNullOrEmpty(configJson))
                {
                    Config = JsonConvert.DeserializeObject<C>(configJson);
                }

                Config ??= LocalConfig as C;
            }
            catch
            {
            }
        }

        protected virtual void InitializeData()
        {
            var userDataJson = SaveLoadHandler.Load<string>(Key + "_user_data");
            UserData = !string.IsNullOrEmpty(userDataJson) ? JsonConvert.DeserializeObject<D>(userDataJson) : new D();
#if UNITY_EDITOR
            if (Config?.fakeDurationEvent > 0)
            {
                UserData.endTime = WrapperTime.CurrentSecond + Config.fakeDurationEvent;
            }
#endif
            DelayToAfterGet();
        }
        
        protected virtual Dictionary<string, Func<int>> InitializedNotifies => new();
        
        void IWrapperEvent.SyncFromServer(ABaseEventConfig configServer, ABaseEventUserData dataServer)
            => SyncFromServer(configServer as C, dataServer as D);

        /// <summary>
        /// Đồng bộ config + user data từ server. Gọi qua <see cref="IWrapperEvent"/>.
        /// </summary>
        /// <param name="configServer"></param>
        /// <param name="dataServer"></param>
        protected virtual void SyncFromServer(C configServer, D dataServer)
        {
            if (configServer != null)
            {
                Config = configServer;
                AfterGetConfig();
                SaveLoadHandler.Save(Key + "_config", JsonConvert.SerializeObject(Config));
            }

            if (dataServer == null)
                return;

            if (UserData == null ||
                UserData.sequence <= dataServer.sequence || 
                UserData.id != dataServer.id || 
                UserData.code != dataServer.code)
            {
                UserData = dataServer;
                DelayToAfterGet();
                return;
            }

            UserData.endTime = dataServer.endTime;
        }
        
        protected virtual void OnNewSeason() {}
        protected virtual void OnNewDay() {}
        protected virtual void OnEndEvent() {}

        protected virtual void SaveAndSend()
        {
            AddSequenceAndSave();
            _saveAndSend.Schedule(0.1f, SendToServer);
        }
        
        protected virtual void AfterGetConfig() { }

        protected virtual void AfterGetData()
        {
            if (UserData.day < WrapperTime.CurrentDay)
            {
                UserData.day = WrapperTime.CurrentDay;
                OnNewDay();
            }
            
            if (UserData.id != UserData.cacheId)
            {
                UserData.cacheId = UserData.id;
                OnNewSeason();
            }
            
            WrapperTime.AddTick(Key + "_end_time", UserData.endTime, null, OnEndEvent);
        }

        protected virtual void AfterGetDataAndSave()
        {
            AfterGetData();
            SaveOnClient();
        }
        
        protected virtual void SaveOnClient()
        {
            SaveLoadHandler.Save(Key + "_user_data", JsonConvert.SerializeObject(UserData));
            Messenger<D>.Emit(UserData);
            GameEvent<object>.Emit(Key, UserData);
            GameEvent.Emit(Key + "_notify");
        }
        
        protected virtual void AddSequenceAndSave()
        {
            UserData.sequence++;
            SaveOnClient();
        }

        protected virtual void SendToServer()
        {
            if (!Application.isPlaying) return;
            new CSUpdateEventData<D>(UserData, Key).Send();
        }
        
        protected virtual void DelayToAfterGet()
        {
            _afterGetData.Schedule(0.1f, AfterGetDataAndSave);
        }

        protected (string id, int amount)[] GrantRewards(IEnumerable<Reward> rewards, string where)
        {
            if (rewards == null) return Array.Empty<(string, int)>();

            var merged = new Dictionary<string, int>();
            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                var id = (string)reward.name;
                int amount = reward.amount;
                if (string.IsNullOrEmpty(id) || amount <= 0) continue;
                merged[id] = merged.GetValueOrDefault(id, 0) + amount;
            }
            if (merged.Count == 0) return Array.Empty<(string, int)>();

            var tuples = new (string, int, string)[merged.Count];
            var granted = new (string, int)[merged.Count];
            var i = 0;
            foreach (var kvp in merged)
            {
                tuples[i] = (kvp.Key, kvp.Value, string.Empty);
                granted[i] = (kvp.Key, kvp.Value);
                i++;
            }

            ResourceCollector.Instance.ResourcesAdd(tuples, where);
            // Thiếu SaveAndUpdateServerOfResourceData là resource chỉ tăng ở client rồi mất khi mở lại app.
            foreach (var kvp in merged)
            {
                ResourceCollector.Instance.SaveAndUpdateServerOfResourceData(kvp.Key);
            }
            return granted;
        }

        public bool IsUnlock => Config != null && Config.levelUnlock <= GameRequest<int>.Request("falcon.modules.core.gamedata.get_level");
    }

    public interface IWrapperEvent : IFReflection, IInitialize
    {
        string Key { get; }
        ABaseEventConfig LocalConfig { get; }
        Dictionary<string, Func<int>> Notifies { get; }
        void SyncFromServer(ABaseEventConfig configServer, ABaseEventUserData dataServer);
    }
}