/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using Falcon.Helpers.Devkit;
using UnityEngine;

namespace Falcon.Modules.CDN
{
    public class CdnSetting : IMySingleton
    {
        private readonly SOCdnSettings _settings = Resources.Load<SOCdnSettings>("SOCdnSettings")?? throw new NullReferenceException("Cdn settings is empty, please check your settings at Falcon/Modules/Cdn/Settings.");

        public string GetKey()
        {
            var key = _settings.cdnKey;
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cdn key cannot be null or empty, please check your settings at Falcon/Modules/Cdn/Settings.");
            }
            return key;
        }
        
        public bool SyncAllFilesAutomatically => _settings.syncAllFilesAutomatically;
        public int RetryAttempts => Math.Max(_settings.retryAttempts, 0);
    }
}