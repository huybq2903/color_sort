/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.RemoteConfig
{
    public static class FConfig
    {
        
        public static string AbTestingString
        {
            get
            {
                try
                {
                    return FConfigController.Instance.AbTestingString;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static string RunningAbTesting
        {
            get
            {
                try
                {
                    return FConfigController.Instance.RunningAbTesting;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static Dictionary<string, object> Configs
        {
            get
            {
                try
                {
                    return FConfigController.Instance.Configs;
                }
                catch (Exception)
                {
                    return new Dictionary<string, object>();
                }
            }
        }

        public static Dictionary<string, object> NonTestConfigs
        {
            get
            {
                try
                {
                    return FConfigController.Instance.NonTestConfigs;
                }
                catch (Exception)
                {
                    return new Dictionary<string, object>();
                }
            }
        }

        public static Dictionary<string, object> TestingConfigs
        {
            get
            {
                try
                {
                    return FConfigController.Instance.TestingConfigs;
                }
                catch (Exception)
                {
                    return new Dictionary<string, object>();
                }
            }
        }

        public static Dictionary<string, bool> CampaignMeta
        {
            get
            {
                try
                {
                    return FConfigController.Instance.CampaignMeta;
                }
                catch (Exception)
                {
                    return new Dictionary<string, bool>();
                }
            }
        }

        public static Dictionary<string, object> ConfigEntries
        {
            get
            {
                try
                {
                    return FConfigController.Instance.ConfigEntries;
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
                    return FConfigController.Instance.InitState;
                }
                catch (Exception)
                {
                    return ExecState.NotStarted;
                }
            }
        }

        public static T Config<T>() where T : IFalconConfig, new()
        {
            try
            {
                return FConfigController.Instance.Config<T>();
            }
            catch (Exception)
            {
                return new T();
            }
        }

        /// <inheritdoc cref="FConfigInitService.TryFetch"/>
        public static async Task<bool> TryFetch()
        {
            try
            {
                return await FConfigController.Instance.TryFetch();
            }
            catch (Exception)
            {
                return false;
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