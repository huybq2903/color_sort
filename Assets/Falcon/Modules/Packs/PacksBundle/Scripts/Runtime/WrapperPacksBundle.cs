/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Falcon.Modules.Packs.PacksBundle.Runtime
{
    /// <summary>
    /// Wrapper chính quản lý các gói vật phẩm trong hệ thống PacksBundle.
    /// Đảm nhiệm việc đăng ký lắng nghe sự kiện mở shop, và phát sự kiện tạo các pack UI tương ứng.
    /// </summary>
    public class WrapperPacksBundle : ABaseWrapperPack<BundleElementPackConfig, NullPackUserData>
    {
        private const string EVENT_CREATE_PACK_SHOP = "falcon.modules.shop.create_pack_shop";
        public override string Key => "PacksBundle";
        
        protected override void AfterGetData()
        {
            base.AfterGetData();
            foreach (var element in DictConfigs)
            {
                var overrideAddress = $"UIPack_Bundle_{element.Key}";
                var checkHandle = Addressables.LoadResourceLocationsAsync(overrideAddress);
                checkHandle.Completed += handle =>
                {
                    var address = handle is { Status: AsyncOperationStatus.Succeeded, Result: { Count: > 0 } }
                        ? overrideAddress
                        : "UIPack_Bundle";
                    GameEvent<(AssetReference, string)>.Emit(EVENT_CREATE_PACK_SHOP, (new AssetReference(address), element.Key));
                    Addressables.Release(handle);
                };
            }
        }
    }
}