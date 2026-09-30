/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Falcon.Modules.Core.AccountData 
{
    /**
    * ClientData lưu trữ thông tin của người dùng, dữ liệu này sẽ được đẩy lên Server và lưu trữ trên Server Mongodb
    */
    [Serializable]
    public class ClientData : AccountData
    {

        [JsonConverter(typeof(FGameDataDictConverter))]
        public Dictionary<string, FGameData> gameDatas { get; set; }

        public ClientData()
        {
        }

        public ClientData(AccountData accountData)
        {
            UpdateData(accountData);
        }

        public void UpdateData(AccountData accountData)
        {
            this.sequence = accountData.sequence;
            this.accountInfo = accountData.accountInfo;
        }
    }
    
    
    public class AccountData
    {
        public int sequence;
        public AccountInfo accountInfo;
    }

    [Serializable]
    public class AccountInfo
    {
        public int code;
        public string token;
        public string device_id;
        public string fb_id;
        public string google_id;
        public string apple_id;
        public string advertising_id;
        public string bind_type;
        public string bind_id;
        public string bind_data;
        public string uuid;
        public int avatar_id;
        public string avatar_url;
        public string account_name;

        public AccountInfo()
        {
        }

        public AccountInfo(string device_id, string advertising_id)
        {
            this.device_id = device_id;
            this.advertising_id = advertising_id;

            code = 0;
            token = "";
            fb_id = "";
            google_id = "";
            apple_id = "";

            bind_type = "";
            bind_id = "";
            bind_data = "";
            uuid = "";
        }
    }

    [Serializable]
    public class BindData
    {
        public string fb_id;
        public string google_id;
        public string apple_id;
        public string avatar_url;

        public BindData(string fb_id = "", string google_id = "", string apple_id = "", string avatar_url = "")
        {
            this.fb_id = fb_id;
            this.google_id = google_id;
            this.apple_id = apple_id;
            this.avatar_url = avatar_url;
        }
    }
}