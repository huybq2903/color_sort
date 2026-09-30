using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Shared.Common;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.BaseBooster
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    public class UIBoosterItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtAmount, txtLevelUnlock;
        [SerializeField] private GameObject goLock, goUnlocked, goGetMore, goAmount, goFree;

        [ValueDropdown("GetBoosterTypes")]
        public string boosterType;
        private static IEnumerable<string> GetBoosterTypes()
        {
            var info = Resources.Load<BoosterInfoSO>("SO_BoosterInfo");
            return info != null ? info.Types : Enumerable.Empty<string>();
        }

        protected Button _button;
        protected RectTransform _rectTrans;
        protected ABoosterResource _resource;

        public virtual int LevelUnlock => BoosterConfig.LevelUnlock(boosterType);

        protected virtual void Awake()
        {
            _rectTrans = GetComponent<RectTransform>();
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
            _resource = ResourceCollector.Instance.GetResourceInCollector(boosterType) as ABoosterResource;
        }

        protected virtual void OnEnable()
        {
            GameEvent.Register($"falcon.game.ui_{boosterType}_update", UpdateUI, this);
            GameEvent<bool>.Register($"falcon.game.ui_{boosterType}_highlight", _rectTrans.SetHighlight, this);
            UpdateUI();
        }

        protected virtual void OnDisable()
        {
            GameEvent.Unregister($"falcon.game.ui_{boosterType}_update", UpdateUI, this);
            GameEvent<bool>.Unregister($"falcon.game.ui_{boosterType}_highlight", _rectTrans.SetHighlight, this);
        }

        protected virtual void UpdateUI()
        {
            if (GameRequest<int>.Request(GameKeys.GET_LEVEL) < LevelUnlock)
            {
                goLock.SetActive(true);
                goUnlocked.SetActive(false);
                goGetMore.SetActive(false);
                goAmount.SetActive(false);
                goFree.SetActive(false);
                txtLevelUnlock.SetText($"Lv {LevelUnlock}");
            }
            else
            {
                goLock.SetActive(false);
                goUnlocked.SetActive(true);
                goGetMore.SetActive(_resource.Quantity <= 0 && !_resource.IsFree);
                goAmount.SetActive(_resource.Quantity > 0 && !_resource.IsFree);
                goFree.SetActive(_resource.IsFree);
                txtAmount.SetText(_resource.Quantity.ToString());
            }
        }

        private void OnClick() => GameEvent<string>.Emit(GameKeys.USE_BOOSTER, boosterType);
    }
}
