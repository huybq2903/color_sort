/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-03-11
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfigCms
{
    public static class FConfig
    {
        public static Dictionary<string, object> Configs
        {
            get
            {
                try
                {
                    return FConfigControllerCms.Instance.Configs;
                }
                catch (Exception)
                {
                    return new Dictionary<string, object>();
                }
            }
        }

        public static Dictionary<string, object> ConfigEntries
        {
            get
            {
                try
                {
                    return FConfigControllerCms.Instance.ConfigEntries;
                }
                catch (Exception)
                {
                    return new Dictionary<string, object>();
                }
            }
        }

        public static ExecState InitState
        {
            get
            {
                try
                {
                    return FConfigControllerCms.Instance.InitState;
                }
                catch (Exception)
                {
                    return ExecState.NotStarted;
                }
            }
        }

        public static T Config<T>() where T : IFalconConfigCms, new()
        {
            try
            {
                return FConfigControllerCms.Instance.Config<T>();
            }
            catch (Exception)
            {
                return new T();
            }
        }

        public static event Action OnUpdateFromNet;

        public class ConfigUpdateCallback : IConfigUpdatedReact
        {
            public void OnConfigUpdated()
            {
                OnUpdateFromNet?.Invoke();
            }
        }
    }
}