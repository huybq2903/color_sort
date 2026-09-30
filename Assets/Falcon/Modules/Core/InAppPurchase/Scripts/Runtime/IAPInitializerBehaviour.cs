/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-03
 */
namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// MonoBehaviour dùng để khởi tạo IAP.
    /// </summary>
    public class IAPInitializerBehaviour : PersistentSingleton<IAPInitializerBehaviour>
    {
        protected override void Awake()
        {
            base.Awake();
            IAPManager.Initialize();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            IAPManager.Dispose();
        }
    }
}