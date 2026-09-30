/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-16
 */

using Falcon.Helpers.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Shop.Runtime
{
    public class UIPopupShop : MonoBehaviour
    {
        [SerializeField] private Button btnExit, btnMoreOffer;
        
        private UIShop _uiShop;
        private UIMiniShop _uiMiniShop;

        private void Awake()
        {
            _uiShop = GetComponentInChildren<UIShop>(true);
            _uiMiniShop = GetComponentInChildren<UIMiniShop>(true);
            
            btnExit.onClick.RemoveAllListeners();
            btnExit.onClick.AddListener(() =>
            {
                GameEvent<Transform>.Emit(ShopConstant.EVENT_CLOSE_POPUP, transform);
            });
            
            btnMoreOffer.onClick.RemoveAllListeners();
            btnMoreOffer.onClick.AddListener(OnClickMoreOffer);
        }

        private void OnEnable()
        {
            if (ShopManager.miniShopConfig.elementConfigs is { Length: > 0 })
            {
                _uiShop.gameObject.SetActive(false);
                _uiMiniShop.gameObject.SetActive(true);
            }
            else
            {
                _uiShop.gameObject.SetActive(true);
                _uiShop.Setup();
                _uiMiniShop.gameObject.SetActive(false);                
            }
        }

        private void OnClickMoreOffer()
        {
            _uiShop.gameObject.SetActive(true);
            _uiShop.Setup();
            _uiMiniShop.gameObject.SetActive(false);
        }
    }
}