/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
 */

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using System.Threading.Tasks;
using I2.Loc;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// Giao diện chính hiển thị danh sách pack trong shop, chia theo nhóm.
    /// </summary>
    public class UIShop : MonoBehaviour
    {
        public Transform gridPacks;
        public GameObject itemWrapperPrefab;
        public GameObject[] lineGroupTitlePrefabs;
        [FormerlySerializedAs("isAnimationSlide")] public bool isAnimation;

        protected readonly Dictionary<int, TMP_Text> _titleTextMap = new();

        /// <summary>
        /// Map idPack → GameObject (quản lý các object đã khởi tạo)
        /// </summary>
        public Dictionary<string, GameObject> PackElementMap { get; } = new();

        /// <summary>
        /// Map group type → title object
        /// </summary>
        public Dictionary<int, UIWrapperItemBase> TitleMap { get; } = new();

        /// <summary>
        /// Map group type → list pack (GameObject, priority)
        /// </summary>
        public Dictionary<int, List<(UIWrapperItemBase go, int priority)>> PackMap { get; } = new();

        public ScrollRect ScrollRect { get; set; }

        protected virtual void Start()
        {
            ScrollRect = GetComponentInChildren<ScrollRect>();
            PreloadTitleGroup();
            Setup();
        }

        protected virtual void OnEnable()
        {
            GameEvent<(AssetReference asset, string idPack)>.Register(ShopConstant.EVENT_CREATE_PACK_SHOP, CreatePackAsync, this);
            GameEvent<string>.Register(ShopConstant.EVENT_HIDE_PACK_SHOP, HidePack, this);
            LocalizationManager.OnLocalizeEvent += SetTextTitleGroup;
        }

        protected virtual void OnDisable()
        {
            GameEvent<(AssetReference asset, string idPack)>.Unregister(ShopConstant.EVENT_CREATE_PACK_SHOP, CreatePackAsync, this);
            GameEvent<string>.Unregister(ShopConstant.EVENT_HIDE_PACK_SHOP, HidePack, this);
            LocalizationManager.OnLocalizeEvent -= SetTextTitleGroup;
        }

        public virtual void Setup()
        {
            if (ScrollRect == null) return;
            ScrollRect.verticalNormalizedPosition = 1;
            foreach (var pack in PackElementMap) pack.Value?.SetActive(false);
            foreach (var title in TitleMap) title.Value.gameObject.SetActive(false);
            SetTextTitleGroup();
            CreatePacks();
        }

        protected virtual bool IsCanceled() => !this || !gameObject;

        /// <summary>Khởi tạo các object tiêu đề.</summary>
        protected virtual void PreloadTitleGroup()
        {
            if (ShopManager.dictKeyGroupTitle == null) return;
            foreach (var group in ShopManager.dictKeyGroupTitle)
            {
                var wrapperItem = Instantiate(itemWrapperPrefab, gridPacks).GetComponent<UIWrapperItemBase>();
                var goTitle = Instantiate(lineGroupTitlePrefabs[group.Key - 1], gridPacks);
                Canvas.ForceUpdateCanvases();
                wrapperItem.SetChild(goTitle);
                wrapperItem.gameObject.SetActive(false);
                TitleMap[group.Key] = wrapperItem;
                _titleTextMap[group.Key] = wrapperItem.GetComponentInChildren<TMP_Text>();
                PackMap[group.Key] = new List<(UIWrapperItemBase, int)>();
            }

            ReorderTitleObjects();
        }

        protected virtual void SetTextTitleGroup()
        {
            foreach (var item in _titleTextMap)
            {
                item.Value.text = ShopManager.dictKeyGroupTitle[item.Key].Invoke();
            }
        }

        protected virtual async void CreatePackAsync((AssetReference asset, string idPack) pack)
        {
            try
            {
                await CreatePack(pack);
                foreach (var type in PackMap.Keys)
                {
                    SortElementInGroup(type);
                }
            }
            catch { /* ignored*/ }
        }

        /// <summary>
        /// Tạo và hiển thị một pack trong UI shop chính dựa trên AssetReference và ID của pack.
        /// </summary>
        /// <param name="pack">
        /// Tuple chứa:
        /// - asset: AssetReference tới prefab của pack.
        /// - idPack: ID duy nhất để tìm config pack tương ứng trong ShopConfig.
        /// </param>
        /// <remarks>
        /// Logic xử lý:
        /// - Tìm config theo idPack trong ShopManager.shopConfig.
        /// - Nếu pack đã được khởi tạo trước đó, bật lại.
        /// - Nếu chưa khởi tạo, load prefab bằng Addressables và gắn vào UI.
        /// - Gán pack vào group tương ứng dựa trên type (ví dụ: Bundle, Currency...).
        /// - Sắp xếp lại vị trí theo priority.
        /// - Nếu bật slide animation, gọi hiệu ứng.
        /// </remarks>
        protected virtual async Task CreatePack((AssetReference asset, string idPack) pack)
        {
            try
            {
                // Tìm config
                var elementConfig = GetConfigById(pack.idPack);
                if (elementConfig == null)
                {
                    return;
                }

                var type = elementConfig.type;
                var priority = elementConfig.priority;

                //Nếu đã tạo thì chỉ active lên thôi
                if (PackElementMap.TryGetValue(pack.idPack, out var wrapperItem))
                {
                    wrapperItem?.SetActive(false);
                }
                else //Chưa thì khởi tạo qua addressable
                {
                    PackElementMap[pack.idPack] = null;
                    wrapperItem = Instantiate(itemWrapperPrefab, gridPacks);
                    wrapperItem.SetActive(false);
                    var handleIns = pack.asset.InstantiateAsync(wrapperItem.transform);
                    await handleIns.Task;
                    await Task.Yield();

                    if (IsCanceled()) return;
                    var goPack = handleIns.Result;
                    if (!goPack) return;

                    var uiWrapper = wrapperItem.GetComponent<UIWrapperItemBase>();
                    uiWrapper.SetChild(goPack);
#if UNITY_EDITOR
                    wrapperItem.name = $"Wrapper - UIPack_{pack.idPack}";
                    goPack.name = $"UIPack_{pack.idPack}";
#endif
                    // Gán trước khi Setup để pack mất config tự ẩn được qua hide_pack_shop
                    PackElementMap[pack.idPack] = wrapperItem;

                    wrapperItem.SetActive(true);
                    goPack.SendMessage("Setup", pack.idPack, SendMessageOptions.DontRequireReceiver);
                    goPack.SendMessage("SetPlacement", "shop", SendMessageOptions.DontRequireReceiver);
                    wrapperItem.SetActive(false);

                    // Pack tự ẩn ngay trong Setup (mất config) → đừng đưa vào PackMap kẻo SortElementInGroup bật lại
                    if (!ShopManager.kDictAsset.ContainsKey(pack.idPack)) return;

                    if (!PackMap.ContainsKey(type))
                    {
                        PackMap[type] = new List<(UIWrapperItemBase, int)>();
                    }
                    PackMap[type].Add((uiWrapper, priority));
                }
            }
            catch { /* ignored*/ }
        }

        protected virtual async void CreatePacks()
        {
            try
            {
                var dictAsset = ShopManager.kDictAsset.ToDictionary(d => d.Key, d => d.Value);
                foreach (var asset in dictAsset)
                {
                    if (IsCanceled()) return;
                    await CreatePack((asset.Value, asset.Key));
                }

                foreach (var type in PackMap.Keys)
                {
                    SortElementInGroup(type);
                    SetAnimationForElements();
                }

                await Task.Delay(500);
                if (IsCanceled()) return;
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridPacks.GetComponent<RectTransform>());
            }
            catch { /* ignored*/ }
        }

        /// <summary>
        /// Ẩn pack theo idPack (deactivate object)
        /// </summary>
        protected virtual void HidePack(string idPack)
        {
            if (!PackElementMap.TryGetValue(idPack, out var goPack) || !goPack) return;

            goPack.transform.SetAsLastSibling();
            goPack.SetActive(false);

            // Tìm type từ config
            var elementConfig = GetConfigById(idPack);
            if (elementConfig == null) return;

            // Kiểm tra và ẩn title nếu không còn pack nào active
            var packs = PackMap[elementConfig.type];
            if (!packs.Any(p => p.go.gameObject.activeSelf) && TitleMap.TryGetValue(elementConfig.type, out var goTitle))
            {
                goTitle.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Tìm config theo idPack
        /// </summary>
        protected virtual ShopElementConfig GetConfigById(string idPack)
        {
            return ShopManager.shopConfig.elementConfigs.FirstOrDefault(e => e.idPack == idPack);
        }

        /// <summary>
        /// Sắp xếp pack trong nhóm theo priority, lấy vị trí của tiêu đề làm chuẩn, các pack trong nhóm sẽ lần lượt
        /// thêm vào bên dưới tiêu đề
        /// </summary>
        /// <param name="typeGroup">type nhóm</param>
        protected virtual void SortElementInGroup(int typeGroup)
        {
            if (IsCanceled()) return;

            var idxTitle = TitleMap.TryGetValue(typeGroup, out var item) ?
                item.transform.GetSiblingIndex() :
                -1;
            PackMap[typeGroup].Sort((a, b) => a.priority.CompareTo(b.priority));

            // Đưa vào đúng vị trí sau title group
            for (var i = 0; i < PackMap[typeGroup].Count; i++)
            {
                PackMap[typeGroup][i].go.gameObject.SetActive(true);
                PackMap[typeGroup][i].go.transform.SetSiblingIndex(idxTitle + i + 1);
            }

            if (!item) return;

            if (PackMap[typeGroup].Count > 0 && !string.IsNullOrEmpty(_titleTextMap[typeGroup].text))
            {
                item.gameObject.SetActive(true);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }

        protected virtual void SetAnimationForElements()
        {
            if (!isAnimation) return;
            foreach (var group in PackMap)
            {
                foreach (var (wrapperGo, _) in group.Value)
                {
                    wrapperGo.SetUpAnimation();
                }
            }

            foreach (var title in TitleMap.Where(title => title.Value.gameObject.activeInHierarchy))
            {
                title.Value.SetUpAnimation();
            }
        }

        /// <summary>Sắp lại thứ tự tiêu đề</summary>
        protected virtual void ReorderTitleObjects()
        {
            foreach (var title in TitleMap.OrderBy(kv => kv.Key))
            {
                title.Value.transform.SetSiblingIndex(title.Key - 1);
            }
        }
    }
}