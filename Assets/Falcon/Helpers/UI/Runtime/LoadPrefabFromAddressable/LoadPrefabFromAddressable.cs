/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-20
*/

using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Falcon.Helpers.UI
{
    public class LoadPrefabFromAddressable : MonoBehaviour
    {
        [Title("Path")]
        public bool loadOnAwake = false;
        public string path;

        [Title("Config Prefab")]
        public Vector2 anchorPos = Vector2.zero;
        public float localScale = 1f;

        public Action onLoadCompleted;
        public GameObject Prefab => _prefab;

        private GameObject _prefab;
        private AsyncOperationHandle<GameObject>? _handle;

        private void Awake()
        {
            if (loadOnAwake) Load();
        }

        private void OnDestroy()
        {
            Unload();
        }

        private async Task LoadPrefabAsync(string pathName)
        {
            try
            {
                _handle = Addressables.LoadAssetAsync<GameObject>(pathName);
                var prefab = await _handle.Value.Task;

                if (_handle.Value.Status == AsyncOperationStatus.Succeeded && prefab != null)
                    Spawn(prefab, pathName);
                else
                    Debug.LogError($"Failed to load prefab at path: {pathName}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void Spawn(GameObject prefab, string pathName)
        {
            _prefab = Instantiate(prefab, transform);
            _prefab.name = pathName;

            if (_prefab.TryGetComponent(out RectTransform rectTransform))
            {
                rectTransform.anchoredPosition = anchorPos;
                rectTransform.localScale = localScale * Vector3.one;
            }

            _prefab.SetActive(true);
            onLoadCompleted?.Invoke();
        }

        public void Load(string pathName)
        {
            if (_prefab != null || string.IsNullOrEmpty(pathName))
                return;

            path = pathName;

            _ = LoadPrefabAsync(pathName);
        }

        /// <summary>Nạp đồng bộ, chặn main thread tới khi xong.</summary>
        // ponytail: WaitForCompletion chỉ hợp bundle local; asset tải từ remote sẽ treo cả game, cần thì quay lại Load().
        public void LoadSync(string pathName)
        {
            if (_prefab != null || string.IsNullOrEmpty(pathName))
                return;

            path = pathName;

            _handle = Addressables.LoadAssetAsync<GameObject>(pathName);
            var prefab = _handle.Value.WaitForCompletion();

            if (_handle.Value.Status == AsyncOperationStatus.Succeeded && prefab != null)
                Spawn(prefab, pathName);
            else
                Debug.LogError($"Failed to load prefab at path: {pathName}");
        }

        [HorizontalGroup]
        [Button(ButtonSizes.Medium)]
        public void Load()
        {
            if (!string.IsNullOrEmpty(path)) Load(path);
        }

        [HorizontalGroup]
        [Button(ButtonSizes.Medium)]
        public void Unload()
        {
            if (_handle.HasValue && _handle.Value.IsValid())
            {
                Addressables.Release(_handle.Value);
                _handle = null;
            }

            // if (_prefab != null)
            // {
            //     Debug.Log($"Released Addressable: {gameObject.name} + {_prefab.name}");
            // }
            // else
            // {
            //     Debug.Log($"Released Addressable: {gameObject.name}");
            // }

            if (_prefab != null)
            {
                Destroy(_prefab);
                _prefab = null;
            }
        }
    }
}
