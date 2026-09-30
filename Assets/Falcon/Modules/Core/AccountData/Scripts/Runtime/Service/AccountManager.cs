/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Falcon.Modules.Core.Network;
using Falcon.Modules.Core.RemoteConfig;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Newtonsoft.Json;

namespace Falcon.Modules.Core.AccountData
{
    using SystemInformation.Runtime;

    /**
    * AccountManager khởi tạo, lưu trữ giá trị cho ClientData
    */
    public class AccountManager : FSingleton<AccountManager>
    {
        public delegate void OnLoginHandler(bool success);

        public event OnLoginHandler OnLoginEvent;

        internal void OnLogin(bool success)
        {
            OnLoginEvent?.Invoke(success);
        }

        public bool IsLogin { get; set; } = false;
        public bool IsUpateDataFinished { get; set; } = false;

        private const string key_sequence = "sequence";
        private int _sequence = 0;

        public int Sequence
        {
            get
            {
                if (_sequence <= 0 && SaveLoadHandler.ExistsKey(key_sequence))
                {
                    _sequence = SaveLoadHandler.Load<int>(key_sequence);
                }

                return _sequence;
            }
            set
            {
                _sequence = value;
                SaveSequence();
                AccountData.sequence = _sequence;
            }
        }

        private void SaveSequence()
        {
            SaveLoadHandler.Save(key_sequence, _sequence);
        }

        private const string key_account_info = "account_info";
        private AccountInfo _accountInfo;

        internal AccountInfo AccountInfo
        {
            get
            {
                if (_accountInfo == null)
                {
                    var deviceId = SystemInformation.Device.DeviceUUID;
                    var advId = GetAdvertisingID();
                    if (SaveLoadHandler.ExistsKey(key_account_info))
                    {
                        _accountInfo = SaveLoadHandler.Load<AccountInfo>(key_account_info);
                        bool needSave = false;
                        if (_accountInfo.device_id != deviceId)
                        {
                            _accountInfo.device_id = deviceId;
                            needSave = true;
                        }

                        if (_accountInfo.advertising_id != advId)
                        {
                            _accountInfo.advertising_id = advId;
                            needSave = true;
                        }

                        if (needSave)
                        {
                            SaveAccountInfo();
                        }
                    }
                    else
                    {
                        _accountInfo = new AccountInfo(deviceId, advId);
                        SaveAccountInfo();
                    }
                }

                return _accountInfo;
            }
            set
            {
                // update data ở server thì không ghi đè fb, gg, apple id
                var fbId = FB_id;
                var ggId = Google_id;
                var appleId = Apple_id;
                _accountInfo = value;
                if (!string.IsNullOrEmpty(fbId))
                {
                    _accountInfo.fb_id = fbId;
                }

                if (!string.IsNullOrEmpty(ggId))
                {
                    _accountInfo.google_id = ggId;
                }

                if (!string.IsNullOrEmpty(appleId))
                {
                    _accountInfo.apple_id = appleId;
                }

                SaveAccountInfo();
                AccountData.accountInfo = _accountInfo;
            }
        }

        public void SaveAccountInfo()
        {
            SaveLoadHandler.Save(key_account_info, _accountInfo);
        }


        private AccountData _accountData;

        internal AccountData AccountData
        {
            get
            {
                if (_accountData == null)
                {
                    _accountData = new AccountData
                    {
                    };
                }
                _accountData.sequence = Sequence;
                _accountData.accountInfo = AccountInfo;
                return _accountData;
            }
        }

        private ClientData _clientData;

        public ClientData ClientData
        {
            get
            {
                if (_clientData == null)
                {
                    _clientData = new ClientData(AccountData);
                }

                _clientData.gameDatas = GameDatas;
                return _clientData;
            }
            set
            {
                _clientData = value;
                if (_clientData != null)
                {
                    Sequence = _clientData.sequence;
                    AccountInfo = _clientData.accountInfo;
                    GameDatas = _clientData.gameDatas;
                }
            }
        }

        #region save_load_game_datas

        private Dictionary<string, FGameData> _gameDatas;
        internal Dictionary<string, FGameData> GameDatas
        {
            get
            {
                if (_gameDatas == null)
                {
                    _gameDatas = _loadGameDatas();
                }
                return _gameDatas;
            }
            set
            {
                _gameDatas = value;
                SaveGameDatas();
            }
        }

        private const string key_game_datas = "falcon_game_datas_";
        public void SaveGameDatas()
        {
            AccountManager.Instance.Sequence++;
            foreach (var fGameData in _gameDatas)
            {
                if (fGameData.Key == null || fGameData.Value == null || fGameData.Key.Equals("null"))
                    continue;
                SaveGameData(fGameData.Value);
            }
        }
        
        internal void SaveGameData(FGameData gameData)
        {
            Dictionary<string, FGameData> result = new Dictionary<string, FGameData>();
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.None
            };

            var attr = gameData.GetType().GetCustomAttribute<FGameDataTypeAttribute>();
            if (attr == null)
            {
                throw new Exception($"FGameData {gameData.GetType().Name} must have FGameDataTypeAttribute");
            }
            else
            {
                string typeName = attr.TypeName;
                string json = JsonConvert.SerializeObject(gameData, settings);
                SaveLoadHandler.Save(key_game_datas + typeName, json);
            }
            
        }
        
        private Dictionary<string, FGameData> _loadGameDatas()
        {
            Dictionary<string, FGameData> result = new Dictionary<string, FGameData>();
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.None
            };
            
            List<string> gameDataKeys = FGameDataRegistry.Instance.GetAllTypeNames();
            foreach (var gameDataKey in gameDataKeys)
            {
                if (gameDataKey == null || gameDataKey.Length <= 0 || gameDataKey.Equals("null"))
                    continue;
                
                string fullGameDataKey = key_game_datas + gameDataKey;
                if (SaveLoadHandler.ExistsKey(fullGameDataKey))
                {
                    string json = SaveLoadHandler.Load<string>(fullGameDataKey);
                    if (string.IsNullOrEmpty(json))
                    {
                        result[gameDataKey] = FGameDataRegistry.Instance.GetGameDataInstance(gameDataKey);
                    }
                    else
                    {
                        Type type = FGameDataRegistry.Instance.Get(gameDataKey);
                        FGameData fGameData = (FGameData)JsonConvert.DeserializeObject(json, type, settings);
                        result[gameDataKey] = fGameData;
                    }
                }
                else
                {
                    result[gameDataKey] = FGameDataRegistry.Instance.GetGameDataInstance(gameDataKey);
                }

                result[gameDataKey].PostConstructor();
            }

            return result;
        }


        #endregion

        internal void AddGameData(FGameData gameData)
        {
            var attr = gameData.GetType().GetCustomAttribute<FGameDataTypeAttribute>();
            if (GameDatas.ContainsKey(attr.TypeName))
            {
                gameData.__type = attr.TypeName;
                GameDatas[attr.TypeName] = gameData;
            }
            else
            {
                gameData.__type = attr.TypeName;   
                GameDatas.Add(attr.TypeName, gameData);
            }
        }

        internal T GetGameData<T>() where T : FGameData, new()
        {
            Type type = typeof(T);
            var attr = type.GetCustomAttribute<FGameDataTypeAttribute>();
            if (GameDatas.TryGetValue(attr.TypeName, out var gameData))
            {
                gameData.__type = attr.TypeName;
                return (T)gameData;
            }
            else
            {
                T t = new T();
                t.__type = attr.TypeName;
                AddGameData(t);
                return t;
            }
        }

        public FGameData GetGameData(Type type)
        {
            var attr = type.GetCustomAttribute<FGameDataTypeAttribute>();
            if (attr == null) return null;
            
            if (GameDatas.TryGetValue(attr.TypeName, out var gameData))
            {
                gameData.__type = attr.TypeName;
                return gameData;
            }
            else
            {
                FGameData t = (FGameData)Activator.CreateInstance(type);
                t.__type = attr.TypeName;
                AddGameData(t);
                return t;
            }
        }

        public event Action<ClientData> OnUpdateFromServer;
        internal void UpdateDataFromServer(ClientData clientData)
        {
            ClientData = clientData;
            foreach (var gameData in GameDatas.Values)
            {
                gameData.OnUpdateFromServer();
            }
            OnUpdateFromServer?.Invoke(clientData);
        }
        public void OnBindData(string bind_type, string bind_id, string bind_data)
        {
            AccountInfo.bind_data = bind_data;
            AccountInfo.bind_id = bind_id;
            AccountInfo.bind_data = bind_data;
            SaveAccountInfo();
        }

        public string Google_id
        {
            set
            {
                AccountInfo.google_id = value;
                SaveAccountInfo();
            }
            get { return AccountInfo.google_id; }
        }

        public string FB_id
        {
            set
            {
                AccountInfo.fb_id = value;
                SaveAccountInfo();
            }
            get { return AccountInfo.fb_id; }
        }

        public string Apple_id
        {
            set
            {
                AccountInfo.apple_id = value;
                SaveAccountInfo();
            }
            get { return AccountInfo.apple_id; }
        }

        public void OnFbLogin(string id)
        {
            if (FB_id != id)
            {
                FB_id = id;
                RestartConnection();
            }
        }

        public void OnGoogleLogin(string id)
        {
            if (Google_id != id)
            {
                Google_id = id;
                RestartConnection();
            }
        }

        public void OnAppleLogin(string id)
        {
            if (Apple_id != id)
            {
                Apple_id = id;
                RestartConnection();
            }
        }

        private void RestartConnection()
        {
            FNetManager.Instance.Restart();
        }

        public bool DataBinded
        {
            private set { }
            get
            {
                if (!AccountInfo.apple_id.Equals("") || !AccountInfo.fb_id.Equals("") ||
                    !AccountInfo.google_id.Equals(""))
                    return true;
                return false;
            }
        }

        public int Code
        {
            get { return AccountInfo.code; }
        }

        public string CodeString
        {
            get => Code.ToString();
        }

        private const string key_hack_suspicion = "check_hack";
        private bool _hackSuspicion = false;

        public bool HackSuspicion
        {
            get
            {
                if (SaveLoadHandler.ExistsKey(key_hack_suspicion))
                {
                    _hackSuspicion = SaveLoadHandler.Load<bool>(key_hack_suspicion);
                }

                return _hackSuspicion;
            }
            set
            {
                _hackSuspicion = value;
                SaveLoadHandler.Save(key_hack_suspicion, _hackSuspicion);
            }
        }

        public void UpdateToServer()
        {
            AccountManager.Instance.Sequence++;
            new CSUpdateData(ClientData).Send();
        }


        #region Init

        bool isInit = false;
        public void Init()
        {
            if (isInit) return;
            isInit = true;
            try
            {
                RemoteConfig4Network remoteConfig = FConfigController.Instance.Config<RemoteConfig4Network>();
                if (!string.IsNullOrEmpty(remoteConfig.connection_uri) && remoteConfig.connection_uri.Length >= 5)
                {
                    FNetManager.Instance.Start(remoteConfig.connection_uri);
                }
                else
                {
                    var serverIp = NetworkSettings.GetServerIp();
                    var serverPort = NetworkSettings.GetServerPort();
                    var serverContext = NetworkSettings.GetServerContext();
                    FNetManager.Instance.Start($"http://{serverIp}:{serverPort}/{serverContext}/");     
                }
            }
            catch (Exception ex)
            {
                var serverIp = NetworkSettings.GetServerIp();
                var serverPort = NetworkSettings.GetServerPort();
                var serverContext = NetworkSettings.GetServerContext();
                FNetManager.Instance.Start($"http://{serverIp}:{serverPort}/{serverContext}/");      
            }


        }

        private string GetAdvertisingID()
        {
            return SystemInformation.Device.AdvertisingID;
        }

        #endregion
    }
}