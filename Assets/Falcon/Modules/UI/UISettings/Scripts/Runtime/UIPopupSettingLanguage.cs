/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
*/

using System.Collections.Generic;
using Falcon.Modules.Core.UI.Runtime;
using I2.Loc;
using SuperScrollView;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Settings.Runtime
{
    public class UIPopupSettingLanguage : LoopListViewManager
    {
        [Header("UI")]
        public Button btnExit;

        protected List<string> _listLanguageCode = new();

        protected virtual void Start()
        {
            btnExit.onClick.RemoveAllListeners();
            btnExit.onClick.AddListener(() => UIWrapper.ClosePopup(transform));
            UpdateUI();
        }

        protected virtual void OnEnable()
        {
            LocalizationManager.OnLocalizeEvent += UpdateUI;
        }

        protected virtual void OnDisable()
        {
            LocalizationManager.OnLocalizeEvent -= UpdateUI;
        }

        protected virtual void UpdateUI()
        {
            // OnLocalizeEvent goi lai UpdateUI moi lan doi ngon ngu, MoveToIndex se keo list ve dau
            var isFirstSetup = !IsInit;
            _listLanguageCode = LocalizationManager.GetAllLanguagesCode();
            Setup(_listLanguageCode.Count);
            RefreshListView();
            if (isFirstSetup) MoveToIndex(0);
        }

        protected override void SetItemData(Transform item, int itemIndex)
        {
            item.GetComponent<UIPopupSettingLanguageItem>().SetItemData(_listLanguageCode[itemIndex]);
        }
    }
}
