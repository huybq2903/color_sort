/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-26
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Falcon.Shared.Addressable
{
    public static class AddressableExtensions
    {
        private static readonly Dictionary<string, AsyncOperationHandle> kOperations = new();
        
        public static async Task<T> LoadAsync<T>(string path) where T : Object
        {
            var has = await HasLocationAsync<T>(path);
            if (!has)
            {
                Debug.LogWarning($"AddressableExtensions: Asset '{path}' not found.");
                return null;
            }
            if (!kOperations.TryGetValue(path, out var handle))
            {
                handle = Addressables.LoadAssetAsync<T>(path);
                kOperations[path] = handle;
            }
            
            await handle.Task;

            if (handle.IsValid())
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result as T;
                }
                Addressables.Release(handle);
            }

            kOperations.Remove(path);
            return null;
        }

        public static T Load<T>(string path) where T : Object
        {
            try
            {
                if (!HasLocation<T>(path))
                {
                    Debug.LogWarning($"AddressableExtensions: Asset '{path}' not found.");
                    return null;
                }
                if (!kOperations.TryGetValue(path, out var handle))
                {
                    handle = Addressables.LoadAssetAsync<T>(path);
                    kOperations[path] = handle;
                }
            
                handle.WaitForCompletion();

                if (handle.IsValid())
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        return handle.Result as T;
                    }
                    Addressables.Release(handle);
                }

                kOperations.Remove(path);
                return null;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }
        
        private static async Task<bool> HasLocationAsync<T>(string address) where T : Object
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(address, typeof(T));
            await locationsHandle.Task;
            var has = locationsHandle is { Status: AsyncOperationStatus.Succeeded, Result: { Count: > 0 } };
            Addressables.Release(locationsHandle);
            return has;
        }

        private static bool HasLocation<T>(string address) where T : Object
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(address, typeof(T));
            locationsHandle.WaitForCompletion();
            var has = locationsHandle is { Status: AsyncOperationStatus.Succeeded, Result: { Count: > 0 } };
            Addressables.Release(locationsHandle);
            return has;
        }
    }
}
