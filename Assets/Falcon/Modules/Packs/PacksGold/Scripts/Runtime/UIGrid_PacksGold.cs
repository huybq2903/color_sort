/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-24
 */

using System.Collections.Generic;
using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Falcon.Modules.Packs.PacksGold.Runtime
{
    public class UIGrid_PacksGold : MonoBehaviour
    {
        [SerializeField] private Transform grid;
        private static readonly AssetReference _assetReference = new("UIPack_Gold_Grid");
        /// <summary>
        /// Map idPack → GameObject (quản lý các object đã khởi tạo)
        /// </summary>
        private readonly Dictionary<string, GameObject> _dictItem = new();

        private void Setup(string _)
        {
            var configs = PacksManager.Get<WrapperPacksGold>().DictConfigs;
            foreach (var config in configs)
            {
                if (_dictItem.TryGetValue(config.Key, out var item))
                {
                    item?.SetActive(false);
                }
                else
                {
                    _dictItem[config.Key] = null;
                    var handleIns = _assetReference.InstantiateAsync(grid);
                    handleIns.WaitForCompletion();
                    var goPack = handleIns.Result;
                    if (!goPack) return;
                    goPack.SendMessage("Setup", config.Key, SendMessageOptions.DontRequireReceiver);
#if UNITY_EDITOR
                    goPack.name = $"UIPack_{config.Key}";
#endif
                    _dictItem[config.Key] = goPack;
                }
            }
        }
    }
}