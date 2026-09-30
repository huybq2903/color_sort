/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-16
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// Giao diện mini shop, hiển thị các gói nổi bật với vị trí cố định.
    /// </summary>
    public class UIMiniShop : MonoBehaviour
    {
        public Transform gridPacks;
        public Transform buttonMoreOffer;

        public GameObject itemWrapperPrefab;

        /// <summary>
        /// Map idPack → GameObject (quản lý các object đã khởi tạo)
        /// </summary>
        protected readonly Dictionary<string, GameObject> _packElementMap = new();

        /// <summary>
        /// Map positon → pack (GameObject, priority)
        /// </summary>
        protected readonly Dictionary<int, (UIWrapperItemBase go, int priority)> _packMap = new();
        protected ScrollRect _scrollRect;
        protected UIWrapperItemBase _wrapperButtonMoreOffer;

        protected virtual void Awake()
        {
            _scrollRect = GetComponentInChildren<ScrollRect>();

            _wrapperButtonMoreOffer = Instantiate(itemWrapperPrefab, gridPacks).GetComponent<UIWrapperItemBase>();
            _wrapperButtonMoreOffer.SetChild(buttonMoreOffer.gameObject);
            _wrapperButtonMoreOffer.gameObject.SetActive(false);
        }

        protected virtual void OnEnable()
        {
            _scrollRect.verticalNormalizedPosition = 1;
            GameEvent<string>.Register(ShopConstant.EVENT_HIDE_PACK_SHOP, HidePack, null);
            foreach (var pack in _packElementMap) pack.Value?.SetActive(false);
            CreatePacks();
        }

        protected virtual void OnDisable()
        {
            GameEvent<string>.Unregister(ShopConstant.EVENT_HIDE_PACK_SHOP, HidePack, null);
        }

        /// <summary>
        /// Tạo và hiển thị một pack trong mini shop dựa trên AssetReference và ID pack.
        /// </summary>
        /// <param name="pack">
        /// Tuple chứa:
        /// - asset: AssetReference tới prefab của pack.
        /// - idPack: ID duy nhất của pack dùng để tìm config từ MiniShopConfig.
        /// </param>
        /// <remarks>
        /// Logic xử lý:
        /// - Tìm config theo idPack trong ShopManager.miniShopConfig.
        /// - Kiểm tra tính hợp lệ của vị trí hiển thị (1–4).
        /// - Nếu tại vị trí đã có pack hiện tại có priority thấp hơn thì bỏ qua.
        /// - Nếu pack đã tồn tại, bật lại.
        /// - Nếu chưa tồn tại, load và khởi tạo prefab.
        /// - Cập nhật bản đồ pack theo vị trí và priority.
        /// - Sắp xếp lại UI theo thứ tự position.
        /// </remarks>
        protected virtual async Task CreatePack((AssetReference asset, string idPack) pack)
        {
            try
            {
                var elementConfig = GetConfigByIdPack(pack.idPack);
                if (elementConfig == null)
                {
                    return;
                }

                var position = elementConfig.position;
                if (position is < 1 or > 4)
                {
                    return;
                }

                var priority = elementConfig.priority;
                if (_packMap.TryGetValue(position, out var curPack) && priority >= curPack.priority)
                {
                    return;
                }

                if (_packElementMap.TryGetValue(pack.idPack, out var wrapperItem))
                {
                    wrapperItem.SetActive(false);
                }
                else
                {
                    wrapperItem = Instantiate(itemWrapperPrefab, gridPacks);
                    wrapperItem.SetActive(false);
                    var handleIns = pack.asset.InstantiateAsync(wrapperItem.transform);
                    await handleIns.Task;
                    await Task.Yield();
                    if (!handleIns.Result) return;

                    wrapperItem.SetActive(true);
                    handleIns.Result.SendMessage("Setup", pack.idPack, SendMessageOptions.DontRequireReceiver);
                    handleIns.Result.SendMessage("SetPlacement", "mini_shop", SendMessageOptions.DontRequireReceiver);
                    wrapperItem.SetActive(false);

                    wrapperItem.GetComponent<UIWrapperItemBase>().SetChild(handleIns.Result);
#if UNITY_EDITOR
                    wrapperItem.name = $"Wrapper - UIPack_{pack.idPack}";
                    handleIns.Result.name = $"UIPack_{pack.idPack}";
#endif
                    _packElementMap[pack.idPack] = wrapperItem;
                }

                if (_packMap.TryGetValue(position, out curPack))
                {
                    if (priority < curPack.priority)
                    {
                        curPack.go.gameObject.SetActive(false);
                        _packMap[position] = (wrapperItem.GetComponent<UIWrapperItemBase>(), priority);
                    }
                }
                else
                {
                    _packMap[position] = (wrapperItem.GetComponent<UIWrapperItemBase>(), priority);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        protected virtual async void CreatePacks()
        {
            foreach (var asset in ShopManager.kDictAsset)
            {
                await CreatePack((asset.Value, asset.Key));
            }

            SortElement();
            await Task.Delay(100);
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridPacks.GetComponent<RectTransform>());
        }

        /// <summary>
        /// Sắp xếp các pack và gắn animation slide theo vị trí
        /// </summary>
        protected virtual void SortElement()
        {
            var indexPos = 0;
            foreach (var pack in _packMap.OrderBy(i => i.Key))
            {
                pack.Value.go.gameObject.SetActive(true);
                pack.Value.go.transform.SetSiblingIndex(indexPos);
                pack.Value.go.SetUpAnimation();
                indexPos++;
            }

            _wrapperButtonMoreOffer.gameObject.SetActive(true);
            _wrapperButtonMoreOffer.transform.SetAsLastSibling();
            _wrapperButtonMoreOffer.SetUpAnimation();
        }

        /// <summary>
        /// Ẩn pack theo idPack (deactivate object)
        /// </summary>
        protected virtual void HidePack(string idPack)
        {
            if (!_packElementMap.TryGetValue(idPack, out var goPack) || !goPack) return;

            goPack.transform.SetAsLastSibling();
            goPack.SetActive(false);
        }

        /// <summary>
        /// Tìm config theo idPack
        /// </summary>
        protected virtual MiniShopElementConfig GetConfigByIdPack(string idPack)
        {
            return ShopManager.miniShopConfig.elementConfigs.FirstOrDefault(e => e.idPack == idPack);
        }
    }
}