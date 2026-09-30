/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-2
*/

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Falcon.Modules.Core.UI.Runtime
{
    public class UIPopupAddressableHandler : MonoBehaviour
    {
        private AsyncOperationHandle<GameObject> _handle;

        public void Init(AsyncOperationHandle<GameObject> handle)
        {
            _handle = handle;
            Debug.Log("Handler initialized for: " + handle.Result.name);
        }

        private void OnDestroy()
        {
            if (_handle.IsValid())
            {
                Debug.Log("Released Addressable Popup: " + gameObject.name);
                Addressables.Release(_handle);
            }
            else
            {
                Debug.LogWarning("Release skipped, handle invalid." + gameObject.name);
            }
        }
    }
}
