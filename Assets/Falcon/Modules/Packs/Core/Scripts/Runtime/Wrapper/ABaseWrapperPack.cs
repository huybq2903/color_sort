/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-09
 */

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Newtonsoft.Json;
using UnityEngine;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Lớp wrapper cơ sở quản lý nhóm pack đồng thời dữ liệu cấu hình (config) và dữ liệu người dùng (userData).
    /// Cung cấp các hàm khởi tạo, lưu local, đồng bộ server, và thông báo sự kiện.
    /// </summary>
    public abstract class ABaseWrapperPack<C, D> : IWrapperPack 
        where C : ABaseElementPackConfig
        where D : ABasePackUserData, new()
    {
        /// <summary>
        /// Cấu hình nhóm pack hiện tại.
        /// </summary>
        public PacksConfig Config { get; protected set; }
        
        /// <summary>
        /// Cấu hình custom thêm
        /// </summary>
        public Dictionary<string, string> Params { get; protected set; }
        
        public Dictionary<string, C> DictConfigs { get; protected set; }
        /// <summary>
        /// Dữ liệu người dùng của nhóm pack.
        /// </summary>
        public D UserData { get; protected set; }

        /// <summary>
        /// Khóa định danh nhóm pack (idGroup).
        /// </summary>
        public abstract string Key { get; }

        public virtual void OnInitialize()
        {
            InitializeConfig();
            InitializeData();
        }

        /// <summary>
        /// Trả về ScriptableObject chứa cấu hình nhóm pack cục bộ, nếu có.
        /// </summary>
        public SOPacksConfig LocalConfig => Resources.Load<SOPacksConfig>($"SO_FCM_{Key}_Config");

        /// <summary>
        /// Đồng bộ cấu hình và dữ liệu người dùng của pack này với dữ liệu nhận từ server.
        /// Đồng thời quản lý lưu local và gọi các hành động sau khi đồng bộ.
        /// Được gọi bằng reflection
        /// </summary>
        /// <param name="configFromServer">Dữ liệu cấu hình từ server.</param>
        /// <param name="dataFromServer">Dữ liệu người dùng từ server.</param>
        public virtual void SyncFromServer(PacksConfig configFromServer, D dataFromServer)
        {
            if (configFromServer is { elements: { Length: > 0 } })
            {
                Config = configFromServer;
                DictConfigs.Clear();
                foreach (var elementPack in Config.elements)
                {
                    DictConfigs[elementPack.idPack] = elementPack as C;
                }

                SaveLoadHandler.Save(Key + "_config", JsonConvert.SerializeObject(Config));
            }
            
            if (UserData is NullPackUserData)
            {
                DelayToAfterGet();
                return;
            }

            if (UserData != null && UserData.sequence > dataFromServer.sequence)
            {
                SendToServer();
                return;
            }

            UserData = dataFromServer;
            UserData.idGroup = Key;
            SaveOnClient();
            DelayToAfterGet();
        }

        protected virtual void InitializeConfig()
        {
            DictConfigs = new Dictionary<string, C>();
            var configJson = SaveLoadHandler.Load<string>(Key + "_config");
            if (!string.IsNullOrEmpty(configJson))
            {
                Config = JsonConvert.DeserializeObject<PacksConfig>(configJson);
            }
            Params = SaveLoadHandler.Load<Dictionary<string, string>>(Key + "_param");
            var configSo = LocalConfig;
            if (Config == null && configSo)
            {
                Config = configSo.config;
            }
            if (Params == null && configSo)
            {
                Params = configSo.@params;
            }
            if (Config == null) return;
            foreach (var elementPack in Config.elements)
            {
                if (elementPack == null) continue;
                DictConfigs[elementPack.idPack] = elementPack as C;
            }
        }
        
        protected virtual void InitializeData()
        {
            if (UserData is NullPackUserData)
            {
                UserData = null;
                DelayToAfterGet();
                return;
            }
            
            var userDataJson = SaveLoadHandler.Load<string>(Key + "_user_data");
            if (!string.IsNullOrEmpty(userDataJson))
            {
                UserData = JsonConvert.DeserializeObject<D>(userDataJson);
            }
            else
            {
                UserData = new D { idGroup = Key };
            }
            DelayToAfterGet();
        }

        /// <summary>
        /// Được gọi sau khi dữ liệu người dùng được tải hoặc đồng bộ.
        /// Ghi đè phương thức này để thực hiện logic sau khi nhận dữ liệu (ví dụ: cập nhật UI).
        /// </summary>
        protected virtual void AfterGetData()
        {
        }

        protected virtual void SendToServer()
        {
            if (UserData is NullPackUserData) return;
            new CSUpdatePackUserData<D>() { data = UserData }.Send();
        }

        protected virtual void SaveOnClient()
        {
            if (UserData is NullPackUserData) return;
            SaveLoadHandler.Save(Key + "_user_data", JsonConvert.SerializeObject(UserData));
            OnChangeData();
        }

        protected virtual void AddSequenceAndSave()
        {
            UserData.sequence++;
            SaveOnClient();
        }

        protected virtual void SaveAndSend()
        {
            DoAllTheSameThingOneTime.DoAction(Key + "_SaveAndSend", AddSequenceAndSave, SendToServer);
        }
        
        /// <summary>
        /// Gửi sự kiện khi dữ liệu người dùng thay đổi.
        /// </summary>
        protected virtual void OnChangeData()
        {
            GameEvent<D>.Emit(PacksConstant.EVENT_CHANGE_DATA, UserData);
        }

        protected void DelayToAfterGet()
        {
            DoAllTheSameThingOneTime.DoAction(Key + "_AfterGetData", null, AfterGetData);
        }

        public void UpdateParam(string key, string value)
        {
            Params[key] = value;
            SaveLoadHandler.Save(Key + "_param", Params);
        }
    }
}