/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-09

using System;
using System.Linq;
using Falcon.Helpers.FReflection;
using Newtonsoft.Json;
using UnityEngine;

namespace Falcon.Modules.Core.AccountData
{
    [FReflection]
    public abstract class FGameData
    {
        [JsonProperty]
        public string __type { get; set; }

        // [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            foreach (var type in FReflection.Instance.GetTypes().Where(t => typeof(FGameData).IsAssignableFrom(t) && !t.IsInterface))
            {
                AccountManager.Instance.GetGameData(type);
            }
        }

        public void Save()
        {
            AccountManager.Instance.Sequence++;
            AccountManager.Instance.SaveGameData(this);
        }

        public void UpdateToServer(bool isCompressed = false)
        {
            Save();
            if (isCompressed)
                new CSUpdateGameData(this).Compress().Send();
            else
                new CSUpdateGameData(this).Send();
        }

        [Obsolete("Use UpdateToServer instead. This method will be removed in future versions.")]
        public void SaveAndUpdateToServer(bool isCompressed = false)
        {
            UpdateToServer(isCompressed);
        }

        public virtual void OnUpdateFromServer()
        {

        }

        public virtual void PostConstructor()
        {

        }
    }

    public abstract class FGameData<T> : FGameData where T : FGameData, new()
    {
        public static T Instance => AccountManager.Instance.GetGameData<T>();
    }
}